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

        /// <summary>Serializes full model replacements without blocking caller threads.</summary>
        private readonly SemaphoreSlim refreshGate = new(1, 1);

        /// <summary>Holds the currently registered model owner and its cancellation lifetime.</summary>
        private Registration registration;

        /// <summary>Counts accepted requests until they release their asynchronous serialization resources.</summary>
        private int activeRequests;

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

                var current = new Registration(handler);
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
            Registration current;
            CancellationTokenSource requestCancellation;

            lock (this.gate)
            {
                ObjectDisposedException.ThrowIf(this.isDisposed, this);
                cancellationToken.ThrowIfCancellationRequested();
                current = this.registration ?? throw new InvalidOperationException("No current-model refresh handler is registered.");
                requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, current.Cancellation.Token);
                this.activeRequests++;
            }

            try
            {
                using (requestCancellation)
                {
                    await this.refreshGate.WaitAsync(requestCancellation.Token).ConfigureAwait(false);

                    try
                    {
                        requestCancellation.Token.ThrowIfCancellationRequested();
                        await current.Handler.ReloadAsync(requestCancellation.Token).ConfigureAwait(false);
                        requestCancellation.Token.ThrowIfCancellationRequested();
                    }
                    finally
                    {
                        this.refreshGate.Release();
                    }
                }
            }
            finally
            {
                lock (this.gate)
                {
                    this.activeRequests--;

                    if (this.isDisposed && this.activeRequests == 0)
                    {
                        this.refreshGate.Dispose();
                    }
                }
            }
        }

        /// <summary>Detaches the current model owner and cancels pending refreshes idempotently.</summary>
        public void Dispose()
        {
            Registration current;

            lock (this.gate)
            {
                if (this.isDisposed)
                {
                    return;
                }

                this.isDisposed = true;
                current = this.registration;
                this.registration = null;

                if (this.activeRequests == 0)
                {
                    this.refreshGate.Dispose();
                }
            }

            Cancel(current);
        }

        /// <summary>Detaches only the owner associated with the disposed registration.</summary>
        /// <param name="current">The registration being disposed.</param>
        private void Unregister(Registration current)
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
        private static void Cancel(Registration current)
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

        /// <summary>Pairs a model owner with the lifetime of its accepted requests.</summary>
        /// <param name="Handler">The connected current-model owner.</param>
        private sealed record Registration(IModelRefreshHandler Handler)
        {
            /// <summary>Gets cancellation shared by requests for this registration.</summary>
            internal CancellationTokenSource Cancellation { get; } = new();
        }
    }
}
