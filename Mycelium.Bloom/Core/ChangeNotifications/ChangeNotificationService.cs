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
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;

    /// <summary>Coordinates circuit-scoped admission, scheduled delivery and full-model refresh delegation.</summary>
    public sealed class ChangeNotificationService : IChangeNotificationService
    {
        /// <summary>Defines the first-event-triggered delivery interval.</summary>
        private static readonly TimeSpan BatchWindow = TimeSpan.FromMilliseconds(75);

        /// <summary>Owns pending, duplicate and circuit-breaker state.</summary>
        private readonly ChangeAccumulator accumulator;

        /// <summary>Owns target subjects and subscription lifetimes.</summary>
        private readonly ChangeObservableRegistry registry;

        /// <summary>Serializes window requests and termination before scheduled processing.</summary>
        private readonly IObserver<Unit> ingress;

        /// <summary>Owns demand-driven timing and serialized delivery.</summary>
        private readonly IDisposable batching;

        /// <summary>Delegates reloads without owning the connected model.</summary>
        private readonly IModelRefreshCoordinator refreshCoordinator;

        /// <summary>Cancels only refresh requests made through this service.</summary>
        private readonly CancellationTokenSource lifetime = new();

        /// <summary>Links requests without accessing a disposed cancellation source.</summary>
        private readonly CancellationToken lifetimeToken;

        /// <summary>Creates the notification boundary with a queued Rx scheduler and application logger.</summary>
        /// <param name="refreshCoordinator">The circuit's full-model refresh coordinator.</param>
        /// <param name="scheduler">The scheduling and expiry clock, defaulting to the Rx task pool.</param>
        /// <param name="logger">The application logger for subscriber failures.</param>
        public ChangeNotificationService(IModelRefreshCoordinator refreshCoordinator, IScheduler scheduler = null,
            ILogger<ChangeNotificationService> logger = null)
        {
            this.refreshCoordinator = refreshCoordinator ?? throw new ArgumentNullException(nameof(refreshCoordinator));
            scheduler ??= TaskPoolScheduler.Default;
            if (scheduler is ImmediateScheduler or CurrentThreadScheduler)
            {
                throw new ArgumentException("Notification delivery requires a queued asynchronous or virtual-time scheduler.", nameof(scheduler));
            }
            this.accumulator = new ChangeAccumulator(scheduler);
            this.registry = new ChangeObservableRegistry(logger ?? NullLogger<ChangeNotificationService>.Instance);
            this.lifetimeToken = this.lifetime.Token;

            IObserver<Unit> input = null;
            var requests = Observable.Create<Unit>(observer =>
            {
                input = Observer.Synchronize(observer);
                return Disposable.Empty;
            });
            this.batching = requests.ObserveOn(scheduler)
                .Publish(windows => windows.SelectMany(_ => Observable.Timer(BatchWindow, scheduler))
                    .TakeUntil(windows.IgnoreElements().Materialize()))
                .Select(_ => this.accumulator.TakeBatch())
                .Where(batch => batch.Count > 0)
                .ObserveOn(scheduler)
                .Subscribe(this.DispatchBatch, this.Complete);
            this.ingress = input;
        }

        /// <summary>Gets or sets admission, discarding pending notifications when disabled.</summary>
        public bool IsEnabled
        {
            get => this.accumulator.IsEnabled;
            set => this.accumulator.IsEnabled = value;
        }

        /// <summary>Gets the number of materialized subjects with active subscribers.</summary>
        public int ActiveObservableCount => this.registry.Count;

        /// <summary>Validates and enqueues a change before requesting its bounded batching window.</summary>
        /// <param name="changeEvent">The immutable committed change.</param>
        /// <exception cref="ArgumentException">A pending duplicate has incompatible structural metadata.</exception>
        public void Publish(ChangeEvent changeEvent)
        {
            ArgumentNullException.ThrowIfNull(changeEvent);
            if (this.accumulator.Accept(changeEvent))
            {
                this.ingress.OnNext(Unit.Default);
            }
        }

        /// <summary>Returns a deferred target stream with registry-owned subject lifetime.</summary>
        /// <param name="target">The subscription target, or null for all changes.</param>
        /// <returns>The shared stream, reacquired on every subscription.</returns>
        public IObservable<ChangeEvent> Listen(ChangeTarget target = null)
        {
            this.accumulator.ThrowIfDisposed();
            return this.registry.Listen(target ?? ChangeTarget.Global);
        }

        /// <summary>Delegates a full reload with caller and service-lifetime cancellation.</summary>
        /// <param name="cancellationToken">Cancellation for the request.</param>
        /// <returns>The model owner's reload completion, cancellation or failure.</returns>
        public async Task RequestRefresh(CancellationToken cancellationToken = default)
        {
            this.accumulator.ThrowIfDisposed();
            using var request = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.lifetimeToken);
            await this.refreshCoordinator.RequestRefresh(request.Token).ConfigureAwait(false);
        }

        /// <summary>Stops admission and registration immediately, then schedules completion behind active delivery.</summary>
        public void Dispose()
        {
            if (!this.accumulator.Stop())
            {
                return;
            }
            this.registry.Stop();
            this.ingress.OnCompleted();
            using (this.lifetime)
            {
                this.lifetime.Cancel();
            }
        }

        /// <summary>Delegates frozen batch delivery without retaining a synchronization gate.</summary>
        /// <param name="batch">The frozen batch in admission order.</param>
        private void DispatchBatch(List<PendingChange> batch)
        {
            foreach (var change in batch)
            {
                this.registry.Deliver(change.Event, () => this.accumulator.CanDeliver(change));
                this.accumulator.Remember(change);
            }
        }

        /// <summary>Completes subscribers after scheduled notification callbacks return.</summary>
        private void Complete()
        {
            this.registry.Complete();
            this.batching.Dispose();
        }
    }
}
