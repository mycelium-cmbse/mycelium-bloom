// ------------------------------------------------------------------------------------------------
// <copyright file="IChangeNotificationService.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ChangeNotifications
{
    /// <summary>Provides circuit-scoped ingestion, subscriptions and full-model refresh requests.</summary>
    public interface IChangeNotificationService : IDisposable
    {
        /// <summary>Gets or sets whether changes are accepted, discarding pending notifications when disabled.</summary>
        bool IsEnabled { get; set; }

        /// <summary>Gets the number of materialized target subjects with active subscribers.</summary>
        int ActiveObservableCount { get; }

        /// <summary>Accepts an immutable committed change for deduplication and scheduled batch delivery.</summary>
        /// <param name="changeEvent">The committed element change.</param>
        /// <exception cref="ArgumentException">A pending duplicate disagrees on kind, metaclass or containment.</exception>
        void Publish(ChangeEvent changeEvent);

        /// <summary>Returns a shared target stream whose consumers own their subscriptions and receive scheduled completion on service disposal.</summary>
        /// <param name="target">The subscription target, or null for all changes.</param>
        /// <returns>A stream with serialized callbacks on the injected Rx scheduler, outside internal synchronization and without renderer affinity.</returns>
        IObservable<ChangeEvent> Listen(ChangeTarget target = null);

        /// <summary>Requests complete current model retrieval and replacement by the registered backend model owner.</summary>
        /// <param name="cancellationToken">Cancellation for the full reload.</param>
        /// <returns>A task completing only when the model owner finishes the reload.</returns>
        /// <exception cref="InvalidOperationException">No current-model reload owner is registered.</exception>
        Task RequestRefresh(CancellationToken cancellationToken = default);
    }
}
