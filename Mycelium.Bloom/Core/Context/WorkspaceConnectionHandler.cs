// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceConnectionHandler.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Context
{
    using Microsoft.AspNetCore.Components.Server.Circuits;

    /// <summary>Cancels browser-dependent uploads when the owning circuit loses its connection.</summary>
    public sealed class WorkspaceConnectionHandler : CircuitHandler, IDisposable
    {
        /// <summary>Serializes connection-token replacement and final disposal.</summary>
        private readonly object gate = new();
        /// <summary>Owns cancellation for uploads using the current circuit connection.</summary>
        private CancellationTokenSource connection = new();
        /// <summary>Prevents token creation and replacement after circuit disposal.</summary>
        private bool disposed;

        /// <summary>Creates an operation-owned cancellation source tied to the current connection.</summary>
        /// <returns>The cancellation source owned by the upload operation.</returns>
        public CancellationTokenSource CreateUploadCancellation()
        {
            lock (this.gate)
            {
                ObjectDisposedException.ThrowIf(this.disposed, this);
                return CancellationTokenSource.CreateLinkedTokenSource(this.connection.Token);
            }
        }

        /// <summary>Cancels uploads when the circuit connection is lost.</summary>
        /// <param name="circuit">The disconnected circuit.</param>
        /// <param name="cancellationToken">The circuit notification cancellation token.</param>
        /// <returns>Completion of upload cancellation callbacks.</returns>
        public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            lock (this.gate)
            {
                return this.disposed ? Task.CompletedTask : this.connection.CancelAsync();
            }
        }

        /// <summary>Creates a fresh upload lifetime when the circuit reconnects.</summary>
        /// <param name="circuit">The reconnected circuit.</param>
        /// <param name="cancellationToken">The circuit notification cancellation token.</param>
        /// <returns>Completion of connection-token renewal.</returns>
        public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            lock (this.gate)
            {
                if (!this.disposed && this.connection.IsCancellationRequested)
                {
                    this.connection.Dispose();
                    this.connection = new CancellationTokenSource();
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>Releases the circuit-owned connection token.</summary>
        public void Dispose()
        {
            CancellationTokenSource owned;
            lock (this.gate)
            {
                if (this.disposed)
                {
                    return;
                }

                this.disposed = true;
                owned = this.connection;
            }

            owned.Cancel();
            owned.Dispose();
        }
    }
}