// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramDataCache.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ModelLoading
{
    using System.Text;
    using Microsoft.Extensions.Caching.Memory;
    using Mycelium.Bloom.Core.Configuration;

    /// <summary>Owns one model session's bounded payload cache and shared in-flight requests.</summary>
    public sealed class DiagramDataCache : IDisposable
    {
        private readonly object gate = new();
        private readonly IDiagramModelSource source;
        private readonly DiagramReadScope scope;
        private int activeFetches;
        private bool resourcesDisposed;
        private readonly DiagramDataOptions options;
        private readonly MemoryCache cache;
        private readonly SemaphoreSlim requests;
        private readonly CancellationTokenSource lifetime = new();
        private readonly Dictionary<string, TaskCompletionSource<string>> pending = new(StringComparer.Ordinal);
        private long generation;
        private bool disposed;
        private long hits;
        private long misses;
        private long evictions;

        /// <summary>Creates an independently sized read cache for one immutable source scope.</summary>
        /// <param name="source">The authorized source.</param>
        /// <param name="options">The read bounds.</param>
        /// <param name="clock">An optional clock for deterministic expiration checks.</param>
        public DiagramDataCache(IDiagramModelSource source, DiagramDataOptions options,
            Microsoft.Extensions.Internal.ISystemClock clock = null)
        {
            this.source = source;
            this.scope = source.Scope;
            this.options = options;
            if (options.CapacityBytes <= 0 || options.ExpirationSeconds <= 0 || options.BatchSize is < 1 or > 32 ||
                options.ConcurrentRequests is < 1 or > 4)
            {
                throw new ArgumentOutOfRangeException(nameof(options));
            }

            this.cache = new MemoryCache(new MemoryCacheOptions
            { SizeLimit = options.CapacityBytes, TrackStatistics = true, Clock = clock });
            this.requests = new SemaphoreSlim(options.ConcurrentRequests);
        }

        /// <summary>Gets completed cache-hit observations.</summary>
        public long Hits => Interlocked.Read(ref this.hits);

        /// <summary>Gets source-wait observations, including shared requests.</summary>
        public long Misses => Interlocked.Read(ref this.misses);

        /// <summary>Gets evicted payload observations.</summary>
        public long Evictions => Interlocked.Read(ref this.evictions);

        /// <summary>Gets current payload-cache statistics, excluding SDK objects.</summary>
        public MemoryCacheStatistics Statistics => this.cache.GetCurrentStatistics();

        /// <summary>Reads bounded batches without letting one consumer cancel shared source work.</summary>
        /// <param name="bundleIds">The required canonical bundle roots.</param>
        /// <param name="cacheResults">Whether to populate the interactive read cache.</param>
        /// <param name="cancellationToken">The consumer lifetime.</param>
        /// <returns>The complete requested payload set.</returns>
        public async Task<IReadOnlyDictionary<string, string>> ReadAsync(IEnumerable<string> bundleIds,
            bool cacheResults, CancellationToken cancellationToken)
        {
            await this.CheckAccessAsync(cancellationToken);
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var batch in bundleIds.Distinct(StringComparer.Ordinal).Chunk(this.options.BatchSize))
            {
                var waits = new Dictionary<string, Task<string>>(StringComparer.Ordinal);
                var owned = new Dictionary<string, TaskCompletionSource<string>>(StringComparer.Ordinal);
                long revision;
                lock (this.gate)
                {
                    ObjectDisposedException.ThrowIf(this.disposed, this);
                    revision = this.generation;
                    foreach (var id in batch)
                    {
                        if (this.cache.TryGetValue((this.scope, id), out string payload))
                        {
                            Interlocked.Increment(ref this.hits);
                            result.Add(id, payload);
                            continue;
                        }

                        Interlocked.Increment(ref this.misses);
                        if (!this.pending.TryGetValue(id, out var completion))
                        {
                            completion =
                                new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                            this.pending.Add(id, completion);
                            owned.Add(id, completion);
                        }

                        waits.Add(id, completion.Task);
                    }

                    if (owned.Count > 0)
                    {
                        this.activeFetches++;
                    }
                }

                var fetch = owned.Count == 0 ? Task.CompletedTask : this.FetchAsync(owned, revision, cacheResults);
                foreach (var (id, wait) in waits)
                    result.Add(id, await wait.WaitAsync(cancellationToken));
                await fetch;
                await this.CheckAccessAsync(cancellationToken);
                lock (this.gate)
                {
                    ObjectDisposedException.ThrowIf(this.disposed, this);
                    if (revision != this.generation)
                    {
                        throw new OperationCanceledException("The model read was invalidated.");
                    }
                }
            }

            return result;
        }

        private async Task FetchAsync(Dictionary<string, TaskCompletionSource<string>> owned, long revision,
            bool cacheResults)
        {
            var entered = false;
            try
            {
                await this.requests.WaitAsync(this.lifetime.Token);
                entered = true;
                await this.CheckAccessAsync(this.lifetime.Token);
                var payloads = await this.source.FetchAsync(this.scope, owned.Keys.ToArray(), this.lifetime.Token);
                await this.CheckAccessAsync(this.lifetime.Token);
                lock (this.gate)
                {
                    if (this.disposed || revision != this.generation)
                    {
                        throw new OperationCanceledException("The model read was invalidated.");
                    }

                    if (owned.Keys.Any(id =>
                            !payloads.TryGetValue(id, out var payload) || string.IsNullOrWhiteSpace(payload)))
                    {
                        throw new InvalidDataException("The source omitted requested model data.");
                    }

                    foreach (var (id, completion) in owned)
                    {
                        var payload = payloads[id];
                        if (cacheResults && Encoding.UTF8.GetByteCount(payload) <= this.options.CapacityBytes)
                        {
                            while (this.cache.GetCurrentStatistics() is { CurrentEntryCount: > 0 } statistics
                                   && statistics.CurrentEstimatedSize + Encoding.UTF8.GetByteCount(payload) >
                                   this.options.CapacityBytes)
                            {
                                this.cache.Compact(Math.Max(0.25, 1d / statistics.CurrentEntryCount));
                            }

                            var entryOptions = new MemoryCacheEntryOptions()
                                .SetSize(Encoding.UTF8.GetByteCount(payload))
                                .SetSlidingExpiration(TimeSpan.FromSeconds(this.options.ExpirationSeconds))
                                .RegisterPostEvictionCallback((_, _, _, _) =>
                                    Interlocked.Increment(ref this.evictions));
                            this.cache.Set((this.scope, id), payload, entryOptions);
                        }

                        completion.TrySetResult(payload);
                    }
                }
            }
            catch (Exception error)
            {
                foreach (var completion in owned.Values)
                {
                    completion.TrySetException(error);
                }
            }
            finally
            {
                lock (this.gate)
                {
                    foreach (var (id, completion) in owned)
                        if (this.pending.TryGetValue(id, out var current) && ReferenceEquals(current, completion))
                        {
                            this.pending.Remove(id);
                        }
                }

                if (entered)
                {
                    this.requests.Release();
                }

                lock (this.gate)
                {
                    this.activeFetches--;
                    this.ReleaseResources();
                }
            }
        }

        private async ValueTask CheckAccessAsync(CancellationToken cancellationToken)
        {
            lock (this.gate)
            {
                ObjectDisposedException.ThrowIf(this.disposed, this);
            }

            if (this.source.Scope != this.scope)
            {
                throw new OperationCanceledException("The source revision changed.");
            }

            await this.source.AuthorizeAsync(this.scope, cancellationToken);
            if (this.source.Scope != this.scope)
            {
                throw new OperationCanceledException("The source revision changed.");
            }
        }

        private void ReleaseResources()
        {
            if (!this.disposed || this.activeFetches != 0 || this.resourcesDisposed)
            {
                return;
            }

            this.resourcesDisposed = true;
            this.requests.Dispose();
            this.lifetime.Dispose();
        }

        /// <summary>Invalidates cached reads and rejects outstanding completions after model or permission changes.</summary>
        public void Invalidate()
        {
            lock (this.gate)
            {
                ObjectDisposedException.ThrowIf(this.disposed, this);
                this.generation++;
                this.cache.Clear();
                foreach (var completion in this.pending.Values)
                {
                    completion.TrySetCanceled();
                }

                this.pending.Clear();
            }
        }

        /// <summary>Cancels source work and releases the session's payload cache.</summary>
        public void Dispose()
        {
            lock (this.gate)
            {
                if (this.disposed)
                {
                    return;
                }

                this.Invalidate();
                this.disposed = true;
                this.lifetime.Cancel();
                this.cache.Dispose();
                this.ReleaseResources();
            }
        }
    }
}