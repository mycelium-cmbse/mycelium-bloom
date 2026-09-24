// ------------------------------------------------------------------------------------------------
// <copyright file="ModelRefreshCoordinator.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ChangeNotifications
{
    using System.Reactive.Disposables;

    /// <summary>Owns a circuit-scoped reload registration without owning a backend, project or model.</summary>
    public sealed class ModelRefreshCoordinator : IModelRefreshCoordinator, IDisposable
    {
        /// <summary>Protects registration replacement, cancellation-token acquisition and disposal across caller threads.</summary>
        private readonly object gate = new();

        /// <summary>Reserves reload order without owning disposable synchronization primitives.</summary>
        private Task previousReload = Task.CompletedTask;

        /// <summary>Holds the currently registered model owner and its cancellation lifetime.</summary>
        private ModelRefreshRegistration registration;

        /// <summary>Indicates that no more registrations or refresh requests can be accepted.</summary>
        private bool isDisposed;

        /// <summary>Registers the sole current-model reload owner.</summary>
        /// <param name="handler">The backend model owner.</param>
        /// <returns>The registration lifetime, which cancels requests when disposed.</returns>
        /// <exception cref="InvalidOperationException">Another model owner is already registered.</exception>
        public IDisposable Register(IModelRefreshHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

            lock (this.gate)
            {
                ObjectDisposedException.ThrowIf(this.isDisposed, this);

                if (this.registration != null)
                {
                    throw new InvalidOperationException("A current-model refresh handler is already registered.");
                }

                var current = new ModelRefreshRegistration(handler);
                this.registration = current;
                return Disposable.Create(() => this.Unregister(current));
            }
        }

        /// <summary>Awaits the model owner's serialized full reload and propagates its outcome.</summary>
        /// <param name="cancellationToken">Cancellation for this request.</param>
        /// <returns>A task completing after the full model replacement.</returns>
        /// <exception cref="InvalidOperationException">No model owner is registered.</exception>
        public async Task RequestRefresh(CancellationToken cancellationToken = default)
        {
            ModelRefreshRegistration current;
            CancellationTokenSource requestCancellation;
            Task predecessor;
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            lock (this.gate)
            {
                ObjectDisposedException.ThrowIf(this.isDisposed, this);
                cancellationToken.ThrowIfCancellationRequested();
                current = this.registration ?? throw new InvalidOperationException("No current-model refresh handler is registered.");
                requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, current.Cancellation.Token);
                predecessor = this.previousReload;
                this.previousReload = Task.WhenAll(predecessor, completion.Task);
            }

            try
            {
                using (requestCancellation)
                {
                    await predecessor.WaitAsync(requestCancellation.Token).ConfigureAwait(false);
                    requestCancellation.Token.ThrowIfCancellationRequested();
                    await current.Handler.ReloadAsync(requestCancellation.Token).ConfigureAwait(false);
                    requestCancellation.Token.ThrowIfCancellationRequested();
                }
            }
            finally
            {
                completion.SetResult();
            }
        }

        /// <summary>Detaches the current model owner and cancels pending refreshes idempotently.</summary>
        public void Dispose()
        {
            ModelRefreshRegistration current;

            lock (this.gate)
            {
                if (this.isDisposed)
                {
                    return;
                }

                this.isDisposed = true;
                current = this.registration;
                this.registration = null;
            }

            Cancel(current);
        }

        /// <summary>Detaches only the owner associated with the disposed registration.</summary>
        /// <param name="current">The registration being disposed.</param>
        private void Unregister(ModelRefreshRegistration current)
        {
            lock (this.gate)
            {
                if (!ReferenceEquals(this.registration, current))
                {
                    return;
                }

                this.registration = null;
            }

            Cancel(current);
        }

        /// <summary>Cancels handler work outside the registration lock.</summary>
        /// <param name="current">The detached registration, if any.</param>
        private static void Cancel(ModelRefreshRegistration current)
        {
            if (current == null)
            {
                return;
            }

            using (current.Cancellation)
            {
                current.Cancellation.Cancel();
            }
        }
    }
}
