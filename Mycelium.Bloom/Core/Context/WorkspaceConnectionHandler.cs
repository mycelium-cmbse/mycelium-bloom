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
        private readonly object gate = new();
        private CancellationTokenSource connection = new();
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

        /// <inheritdoc />
        public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            lock (this.gate)
            {
                return this.disposed ? Task.CompletedTask : this.connection.CancelAsync();
            }
        }

        /// <inheritdoc />
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