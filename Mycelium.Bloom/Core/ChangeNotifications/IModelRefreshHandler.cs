// ------------------------------------------------------------------------------------------------
// <copyright file="IModelRefreshHandler.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ChangeNotifications
{
    /// <summary>Defines the connected model owner's complete backend retrieval and model replacement operation.</summary>
    public interface IModelRefreshHandler
    {
        /// <summary>Retrieves the complete current model from its backend and coherently replaces the owned model state.</summary>
        /// <param name="cancellationToken">Cancellation for retrieval and replacement.</param>
        /// <returns>A task completing after replacement and the owner's required state notifications finish.</returns>
        Task ReloadAsync(CancellationToken cancellationToken);
    }
}
