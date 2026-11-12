// ------------------------------------------------------------------------------------------------
// <copyright file="ListObservableExtensions.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Extensions
{
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;

    /// <summary>
    /// Provides the reactive steps that every list of the ViewModels shares: changes on the context of the renderer and the
    /// cancellation of an outdated load.
    /// </summary>
    public static class ListObservableExtensions
    {
        /// <summary>
        /// Defines the steps that load a list for each query.
        /// </summary>
        /// <typeparam name="TQuery">The type of the query, which is compared by value.</typeparam>
        /// <param name="queries">The queries of the list.</param>
        extension<TQuery>(IObservable<TQuery> queries)
        {
            /// <summary>
            /// Moves the notifications to the synchronization context that the caller runs on when it calls this method, which is
            /// the one of the renderer for a ViewModel created by a component, so that the rows are never changed while the view
            /// reads them; without a synchronization context the notifications stay where they arrive.
            /// </summary>
            /// <returns>The notifications, on the context of the caller.</returns>
            public IObservable<TQuery> ObserveOnRenderContext()
            {
                var renderContext = SynchronizationContext.Current;

                return queries.ObserveOn(renderContext is null ? Scheduler.Immediate : new SynchronizationContextScheduler(renderContext));
            }

            /// <summary>
            /// Runs a load for each query that differs from the previous one. The running load is cancelled when a newer
            /// query arrives or when the subscription is disposed, so that only the latest query is shown. Every load starts on
            /// the synchronization context that the caller runs on when it calls this method, which is the one of the
            /// renderer for a ViewModel created by a component, so that the rows are never changed while the view reads them;
            /// without a synchronization context the loads start where the query arrives.
            /// </summary>
            /// <param name="load">Loads and shows the list of a query; it stops when its token is cancelled.</param>
            /// <returns>The completions of the loads.</returns>
            public IObservable<Unit> LoadLatest(Func<TQuery, CancellationToken, Task> load)
            {
                return queries
                    .DistinctUntilChanged()
                    .ObserveOnRenderContext()
                    .Select(query => Observable.FromAsync(cancellationToken => load(query, cancellationToken)))
                    .Switch();
            }
        }
    }
}
