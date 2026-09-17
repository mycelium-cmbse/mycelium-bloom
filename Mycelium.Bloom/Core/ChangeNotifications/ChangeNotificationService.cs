// ------------------------------------------------------------------------------------------------
// <copyright file="ChangeNotificationService.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ChangeNotifications
{
    using System.Collections.Concurrent;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;

    /// <summary>Serializes committed changes into shared, automatically released circuit-scoped target streams.</summary>
    public sealed class ChangeNotificationService : IChangeNotificationService
    {
        /// <summary>Defines the batch delivery interval.</summary>
        private static readonly TimeSpan BatchWindow = TimeSpan.FromMilliseconds(75);

        /// <summary>Defines the maximum age of a remembered committed element change.</summary>
        private static readonly TimeSpan DeduplicationWindow = TimeSpan.FromSeconds(2);

        /// <summary>Bounds retained duplicate keys even when the input rate exceeds the expiry window.</summary>
        private const int MaximumRememberedChanges = 4096;

        /// <summary>Protects ingestion, target lifetimes and delivery snapshots without running consumer callbacks.</summary>
        private readonly object gate = new();

        /// <summary>Stores canonical lazily materialized subjects by their validated value keys.</summary>
        private readonly ConcurrentDictionary<ChangeTarget, Lazy<ChangeObservable>> observables = new();

        /// <summary>Tracks admitted changes until expiry or capacity eviction.</summary>
        private readonly Dictionary<CommitElement, PendingChange> recentChanges = new();

        /// <summary>Orders duplicate-cache eviction by admission time.</summary>
        private readonly Queue<PendingChange> expirationOrder = new();

        /// <summary>Accepts changes only under the service gate before scheduling them into Rx batching.</summary>
        private readonly Subject<PendingChange> ingress = new();

        /// <summary>Owns scheduled ingestion, buffering and batch delivery.</summary>
        private readonly IDisposable batching;

        /// <summary>Provides both scheduling and the clock used for duplicate expiry.</summary>
        private readonly IScheduler scheduler;

        /// <summary>Routes full reload requests to the current-model owner.</summary>
        private readonly IModelRefreshCoordinator refreshCoordinator;

        /// <summary>Cancels refresh requests when the notification service's lifetime ends.</summary>
        private readonly CancellationTokenSource lifetime = new();

        /// <summary>Retains detached subjects until their serialized scheduler completion.</summary>
        private List<(ChangeObservable Channel, IDisposable Lease)> completingObservables = [];

        /// <summary>Invalidates pending batches whenever notifications are disabled.</summary>
        private long generation;

        /// <summary>Controls admission and dispatch without replacing existing target streams.</summary>
        private bool isEnabled = true;

        /// <summary>Marks the final service lifetime boundary.</summary>
        private bool isDisposed;

        /// <summary>Creates the notification bus using an asynchronous or virtual-time Rx scheduler.</summary>
        /// <param name="refreshCoordinator">The circuit's full model refresh coordinator.</param>
        /// <param name="scheduler">The queued scheduler and clock, defaulting to the Rx task pool.</param>
        public ChangeNotificationService(IModelRefreshCoordinator refreshCoordinator, IScheduler scheduler = null)
        {
            this.refreshCoordinator = refreshCoordinator ?? throw new ArgumentNullException(nameof(refreshCoordinator));
            this.scheduler = scheduler ?? TaskPoolScheduler.Default;

            if (this.scheduler is ImmediateScheduler or CurrentThreadScheduler)
            {
                throw new ArgumentException("Notification delivery requires a queued asynchronous or virtual-time scheduler.", nameof(scheduler));
            }

            this.batching = this.ingress
                .ObserveOn(this.scheduler)
                .Buffer(BatchWindow, this.scheduler)
                .ObserveOn(this.scheduler)
                .Subscribe(this.DispatchBatch, this.CompleteObservables);
        }

        /// <summary>Gets or sets notification admission, discarding pending changes when disabled.</summary>
        public bool IsEnabled
        {
            get
            {
                lock (this.gate)
                {
                    return this.isEnabled && !this.isDisposed;
                }
            }
            set
            {
                lock (this.gate)
                {
                    ObjectDisposedException.ThrowIf(this.isDisposed, this);

                    if (this.isEnabled == value)
                    {
                        return;
                    }

                    this.isEnabled = value;

                    if (!value)
                    {
                        this.generation++;
                        this.recentChanges.Clear();
                        this.expirationOrder.Clear();
                    }
                }
            }
        }

        /// <summary>Gets the number of materialized subjects currently shared by subscribers.</summary>
        public int ActiveObservableCount
        {
            get
            {
                lock (this.gate)
                {
                    return this.observables.Count;
                }
            }
        }

        /// <summary>Admits a change once per bounded commit/element window and queues it for batch delivery.</summary>
        /// <param name="changeEvent">The immutable committed element change.</param>
        /// <exception cref="ArgumentException">A pending duplicate disagrees on kind, metaclass or containment.</exception>
        public void Publish(ChangeEvent changeEvent)
        {
            ArgumentNullException.ThrowIfNull(changeEvent);

            lock (this.gate)
            {
                ObjectDisposedException.ThrowIf(this.isDisposed, this);

                if (!this.isEnabled)
                {
                    return;
                }

                var now = this.scheduler.Now;
                this.RemoveExpiredChanges(now);
                var key = new CommitElement(changeEvent.CommitId, changeEvent.ElementId);

                if (this.recentChanges.TryGetValue(key, out var existing))
                {
                    if (existing.IsPending)
                    {
                        existing.Event = existing.Event.Coalesce(changeEvent);
                    }

                    return;
                }

                if (this.recentChanges.Count == MaximumRememberedChanges)
                {
                    this.RemoveOldestChange();
                }

                var pending = new PendingChange(key, changeEvent, now + DeduplicationWindow, this.generation);
                this.recentChanges.Add(key, pending);
                this.expirationOrder.Enqueue(pending);
                this.ingress.OnNext(pending);
            }
        }

        /// <summary>Returns a deferred stream that attaches to the current canonical target subject on every subscription.</summary>
        /// <param name="target">The target, or null for global changes.</param>
        /// <returns>A target stream with serialized callbacks on the injected scheduler and no renderer affinity.</returns>
        public IObservable<ChangeEvent> Listen(ChangeTarget target = null)
        {
            lock (this.gate)
            {
                ObjectDisposedException.ThrowIf(this.isDisposed, this);
            }

            var key = target ?? ChangeTarget.Global;

            return Observable.Create<ChangeEvent>(observer =>
            {
                lock (this.gate)
                {
                    ObjectDisposedException.ThrowIf(this.isDisposed, this);
                    var channel = this.observables.GetOrAdd(key,
                        targetKey => new Lazy<ChangeObservable>(() => this.CreateObservable(targetKey))).Value;
                    var subscription = channel.Observable.Subscribe(observer);

                    return Disposable.Create(() =>
                    {
                        lock (this.gate)
                        {
                            subscription.Dispose();
                        }
                    });
                }
            });
        }

        /// <summary>Delegates full model refresh independently of the notification circuit breaker.</summary>
        /// <param name="cancellationToken">Cancellation for the full reload.</param>
        /// <returns>The model owner's reload completion, cancellation or failure.</returns>
        public async Task RequestRefresh(CancellationToken cancellationToken = default)
        {
            CancellationTokenSource requestCancellation;

            lock (this.gate)
            {
                ObjectDisposedException.ThrowIf(this.isDisposed, this);
                requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.lifetime.Token);
            }

            using (requestCancellation)
            {
                await this.refreshCoordinator.RequestRefresh(requestCancellation.Token).ConfigureAwait(false);
            }
        }

        /// <summary>Stops ingestion immediately and queues serialized listener completion without waiting for callbacks.</summary>
        public void Dispose()
        {
            lock (this.gate)
            {
                if (this.isDisposed)
                {
                    return;
                }

                this.isDisposed = true;
                this.generation++;
                this.recentChanges.Clear();
                this.expirationOrder.Clear();

                foreach (var entry in this.observables.Values)
                {
                    var channel = entry.Value;
                    this.completingObservables.Add((channel, channel.Lifetime.GetDisposable()));
                    channel.Lifetime.Dispose();
                }

                this.observables.Clear();
                this.ingress.OnCompleted();
            }

            this.ingress.Dispose();

            using (this.lifetime)
            {
                this.lifetime.Cancel();
            }
        }

        /// <summary>Composes target sharing and final-subscriber cleanup.</summary>
        /// <param name="target">The canonical target key.</param>
        /// <returns>The materialized subject and reference-counted stream.</returns>
        private ChangeObservable CreateObservable(ChangeTarget target)
        {
            var subject = new Subject<ChangeEvent>();
            var subjectLifetime = new RefCountDisposable(subject);
            var observable = Observable.Create<ChangeEvent>(observer =>
                new CompositeDisposable(subject.Subscribe(observer),
                    Disposable.Create(() => this.RemoveObservable(target, subject))))
                .Publish()
                .RefCount();

            return new ChangeObservable(subject, observable, subjectLifetime);
        }

        /// <summary>Removes only the disconnected subject and releases its resources.</summary>
        /// <param name="target">The disconnected target.</param>
        /// <param name="subject">The subject whose last subscriber has detached.</param>
        private void RemoveObservable(ChangeTarget target, Subject<ChangeEvent> subject)
        {
            lock (this.gate)
            {
                if (this.observables.TryGetValue(target, out var current)
                    && ReferenceEquals(current.Value.Subject, subject))
                {
                    this.observables.TryRemove(target, out _);
                    current.Value.Lifetime.Dispose();
                }
            }
        }

        /// <summary>Delivers each admitted event to its matching targets after the batching interval.</summary>
        /// <param name="batch">The buffered committed changes.</param>
        private void DispatchBatch(IList<PendingChange> batch)
        {
            List<(ChangeEvent Event, long Generation)> changes;

            lock (this.gate)
            {
                if (this.isDisposed)
                {
                    return;
                }

                this.RemoveExpiredChanges(this.scheduler.Now);
                changes = new List<(ChangeEvent, long)>(batch.Count);

                foreach (var pending in batch)
                {
                    pending.IsPending = false;
                    changes.Add((pending.Event, pending.Generation));
                }
            }

            foreach (var change in changes)
            {
                foreach (var target in GetTargets(change.Event))
                {
                    ChangeObservable channel;
                    IDisposable lease;

                    lock (this.gate)
                    {
                        if (this.isDisposed || !this.isEnabled || change.Generation != this.generation)
                        {
                            break;
                        }

                        if (!this.observables.TryGetValue(target, out var entry))
                        {
                            continue;
                        }

                        channel = entry.Value;
                        lease = channel.Lifetime.GetDisposable();
                    }

                    using (lease)
                    {
                        channel.Subject.OnNext(change.Event);
                    }
                }
            }
        }

        /// <summary>Completes retired streams outside the gate after all scheduled batch callbacks have returned.</summary>
        private void CompleteObservables()
        {
            List<(ChangeObservable Channel, IDisposable Lease)> completing;

            lock (this.gate)
            {
                completing = this.completingObservables;
                this.completingObservables = [];
            }

            try
            {
                foreach (var completion in completing)
                {
                    completion.Channel.Subject.OnCompleted();
                }
            }
            finally
            {
                foreach (var (_, lease) in completing)
                {
                    lease.Dispose();
                }

                this.batching.Dispose();
            }
        }

        /// <summary>Builds direct lookup keys from immutable event identity, containment and metaclass ancestry.</summary>
        /// <param name="changeEvent">The event being dispatched.</param>
        /// <returns>Distinct global, element, subtree and metaclass targets.</returns>
        private static IEnumerable<ChangeTarget> GetTargets(ChangeEvent changeEvent)
        {
            yield return ChangeTarget.Global;
            yield return ChangeTarget.Element(changeEvent.ElementId);
            yield return ChangeTarget.Subtree(changeEvent.ElementId);

            var namespaces = changeEvent.Containment.NamespaceIds.AsEnumerable();

            if (changeEvent.PreviousContainment != null)
            {
                namespaces = namespaces.Concat(changeEvent.PreviousContainment.NamespaceIds);
            }

            foreach (var namespaceId in namespaces.Distinct())
            {
                yield return ChangeTarget.Subtree(namespaceId);
            }

            foreach (var type in SysmlMetaclass.GetHierarchy(changeEvent.ElementType))
            {
                yield return ChangeTarget.ForType(type);
            }
        }

        /// <summary>Expires remembered keys during admission and each scheduled batch flush.</summary>
        /// <param name="now">The injected scheduler's current time.</param>
        private void RemoveExpiredChanges(DateTimeOffset now)
        {
            while (this.expirationOrder.TryPeek(out var oldest) && oldest.ExpiresAt <= now)
            {
                this.RemoveOldestChange();
            }
        }

        /// <summary>Evicts the oldest remembered key from both duplicate-cache indexes.</summary>
        private void RemoveOldestChange()
        {
            this.recentChanges.Remove(this.expirationOrder.Dequeue().Key);
        }

        /// <summary>Identifies one committed element mutation across transports.</summary>
        /// <param name="CommitId">The backend commit identifier.</param>
        /// <param name="ElementId">The stable element identifier.</param>
        private readonly record struct CommitElement(Guid CommitId, Guid ElementId);

        /// <summary>Owns the mutable pending aggregate until its scheduled dispatch.</summary>
        /// <param name="Key">The duplicate identity.</param>
        /// <param name="Event">The immutable current event aggregate.</param>
        /// <param name="ExpiresAt">The duplicate identity's expiry time.</param>
        /// <param name="Generation">The notification generation at admission.</param>
        private sealed record PendingChange(CommitElement Key, ChangeEvent Event, DateTimeOffset ExpiresAt, long Generation)
        {
            /// <summary>Gets or sets the pending immutable event aggregate under the service gate.</summary>
            public ChangeEvent Event { get; set; } = Event;

            /// <summary>Gets or sets whether echoes can still contribute property information.</summary>
            internal bool IsPending { get; set; } = true;
        }

        /// <summary>Pairs the owned subject with its shared subscriber lifecycle.</summary>
        /// <param name="Subject">The owned target subject.</param>
        /// <param name="Observable">The reference-counted stream.</param>
        /// <param name="Lifetime">Defers physical subject disposal until active delivery leases are released.</param>
        private sealed record ChangeObservable(Subject<ChangeEvent> Subject, IObservable<ChangeEvent> Observable, RefCountDisposable Lifetime);
    }
}
