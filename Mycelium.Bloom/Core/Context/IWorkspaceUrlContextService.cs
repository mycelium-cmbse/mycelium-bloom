// ------------------------------------------------------------------------------------------------
// <copyright file="IWorkspaceUrlContextService.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Context
{
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Projects browser locations and shared element selection into workspace URL context.
    /// </summary>
    public interface IWorkspaceUrlContextService : IDisposable
    {
        /// <summary>
        /// Gets replayed selected-element restorations derived from authoritative browser locations.
        /// </summary>
        IObservable<WorkspaceUrlContextRestoration> Restorations { get; }

        /// <summary>
        /// Gets client-side replacement navigation requests derived from selection and canonicalization.
        /// </summary>
        IObservable<string> NavigationRequests { get; }

        /// <summary>
        /// Creates a destination URI from a canonical NavigationRail route and current shared selection.
        /// </summary>
        /// <param name="canonicalHref">The canonical destination route.</param>
        /// <returns>The destination URI containing only transferable selected-element context.</returns>
        string GetDestinationUri(string canonicalHref);
    }
}
