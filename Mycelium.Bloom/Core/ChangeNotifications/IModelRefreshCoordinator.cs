// ------------------------------------------------------------------------------------------------
// <copyright file="IModelRefreshCoordinator.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ChangeNotifications
{
    /// <summary>Routes manual refresh to the circuit's explicitly registered current-model owner.</summary>
    public interface IModelRefreshCoordinator
    {
        /// <summary>Registers the sole reload owner until the returned registration is disposed.</summary>
        /// <param name="handler">The backend adapter responsible for full retrieval and coherent model replacement.</param>
        /// <returns>A registration that detaches the handler and cancels its pending refreshes when disposed.</returns>
        /// <exception cref="InvalidOperationException">A current-model owner is already registered.</exception>
        IDisposable Register(IModelRefreshHandler handler);

        /// <summary>Runs a full reload through the registered owner.</summary>
        /// <param name="cancellationToken">Cancellation for this request.</param>
        /// <returns>The reload completion, cancellation or failure.</returns>
        /// <exception cref="InvalidOperationException">No current-model reload owner is registered.</exception>
        Task RequestRefresh(CancellationToken cancellationToken = default);
    }
}
