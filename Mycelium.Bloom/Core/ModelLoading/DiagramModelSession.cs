// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramModelSession.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ModelLoading
{
    using System.Text;
    using System.Reactive.Disposables;
    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.ViewModel.Diagram;
    using SysML2.NET.Dal;
    using SysML2.NET.Serializer.Json;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>Owns a scoped read cache while the circuit's SDK assembler retains canonical identities.</summary>
    public sealed class DiagramModelSession : IDisposable
    {
        private readonly object gate = new();
        private readonly IAssembler assembler;
        private readonly IDeSerializer serializer;
        private readonly DiagramDataOptions options;
        private readonly HashSet<string> assembled = new(StringComparer.Ordinal);
        private bool disposed;
        private int consumers;
        private bool cacheClosed;

        /// <summary>Creates one caller-owned model read session.</summary>
        /// <param name="source">The metadata and authorized fetch boundary.</param>
        /// <param name="options">The dedicated cache policy.</param>
        /// <param name="assembler">The circuit's canonical identity owner.</param>
        /// <param name="serializer">The installed SDK deserializer.</param>
        public DiagramModelSession(IDiagramModelSource source, DiagramDataOptions options, IAssembler assembler,
            IDeSerializer serializer)
        {
            this.Source = source;
            this.assembler = assembler;
            this.serializer = serializer;
            this.options = options;
            this.Cache = new DiagramDataCache(source, options);
        }

        /// <summary>Gets the metadata and fetch source.</summary>
        public IDiagramModelSource Source { get; }

        /// <summary>Gets the session-owned fetched-payload cache.</summary>
        public DiagramDataCache Cache { get; private set; }

        /// <summary>Gets the number of complete bundles assembled in this session.</summary>
        public int AssembledBundles
        {
            get
            {
                lock (this.gate)
                {
                    return this.assembled.Count;
                }
            }
        }

        /// <summary>Gets the circuit SDK cache count, which is not bounded by the read cache.</summary>
        public int CanonicalObjects => this.assembler.Cache.Count;

        /// <summary>Keeps shared fetch resources alive until the last tab releases this model.</summary>
        /// <returns>A lease owned by the requesting tab.</returns>
        public IDisposable Acquire()
        {
            lock (this.gate)
            {
                ObjectDisposedException.ThrowIf(this.disposed, this);
                if (this.cacheClosed)
                {
                    this.Cache = new DiagramDataCache(this.Source, this.options);
                    this.cacheClosed = false;
                }

                this.consumers++;
            }

            return Disposable.Create(() =>
            {
                lock (this.gate)
                {
                    if (--this.consumers == 0)
                    {
                        this.Cache.Dispose();
                        this.cacheClosed = true;
                    }
                }
            });
        }

        /// <summary>Loads only the requested elements' bounded dependency bundles.</summary>
        /// <param name="elementIds">Required semantic identities.</param>
        /// <param name="cacheResults">Whether to populate the interactive payload cache.</param>
        /// <param name="cancellationToken">The requesting consumer lifetime.</param>
        /// <returns>Canonical projections for the complete fetched bundles.</returns>
        public async Task<IReadOnlyList<DiagramProjection>> LoadAsync(IEnumerable<string> elementIds, bool cacheResults,
            CancellationToken cancellationToken)
        {
            var ids = elementIds.Select(id => this.Source.Bundles.TryGetValue(id, out var bundle)
                    ? bundle
                    : throw new InvalidDataException("Unknown semantic identity.")).Distinct(StringComparer.Ordinal)
                .ToArray();
            var payloads = await this.Cache.ReadAsync(ids, cacheResults, cancellationToken);
            await this.Source.AuthorizeAsync(this.Source.Scope, cancellationToken);
            var result = new List<DiagramProjection>();
            lock (this.gate)
            {
                ObjectDisposedException.ThrowIf(this.disposed, this);
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var (id, payload) in payloads)
                {
                    if (!this.assembled.Contains(id))
                    {
                        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));
                        var data = this.serializer.DeSerialize(stream, SerializationModeKind.JSON,
                            SerializationTargetKind.PSM, false).ToArray();
                        var elements = data.OfType<SysML2.NET.Core.DTO.Root.Elements.IElement>().ToArray();
                        if (elements.Length != data.Length || !elements.Any(element => element.Id.ToString() == id))
                        {
                            throw new InvalidDataException("The source returned an invalid SDK bundle.");
                        }

                        if (!this.assembler.Cache.ContainsKey(Guid.Parse(id)))
                        {
                            this.assembler.Synchronize(elements);
                        }

                        this.assembled.Add(id);
                    }

                    if (!this.assembler.Cache.TryGetValue(Guid.Parse(id), out var root) ||
                        root.Value is not INamespace model)
                    {
                        throw new InvalidDataException("The SDK bundle has no canonical root.");
                    }

                    result.Add(InterconnectionProjection.Create(model));
                }
            }

            return result;
        }

        /// <summary>Resolves a requested canonical object only after an authorized detail read.</summary>
        /// <param name="elementId">The semantic identity.</param>
        /// <param name="cancellationToken">The selection lifetime.</param>
        /// <returns>The SDK-owned object.</returns>
        public async Task<IElement> ResolveAsync(string elementId, CancellationToken cancellationToken)
        {
            await this.LoadAsync([elementId], true, cancellationToken);
            lock (this.gate)
            {
                ObjectDisposedException.ThrowIf(this.disposed, this);
                return this.assembler.Cache.TryGetValue(Guid.Parse(elementId), out var element) ? element.Value : null;
            }
        }

        /// <summary>Releases read resources without deleting circuit-owned semantic objects.</summary>
        public void Dispose()
        {
            lock (this.gate)
            {
                if (this.disposed)
                {
                    return;
                }

                this.disposed = true;
                this.Cache.Dispose();
                this.assembled.Clear();
            }
        }
    }
}