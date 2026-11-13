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

        /// <summary>Creates an absolute reopenable URL for the displayed canonical element without history metadata.</summary>
        /// <param name="element">The canonical element currently displayed in Properties.</param>
        /// <returns>The canonical absolute URI, or <see langword="null" /> when unavailable.</returns>
        string GetElementUri(SysML2.NET.Core.POCO.Root.Elements.IElement element);

        /// <summary>Follows a reference with a new browser history entry.</summary>
        /// <param name="element">The canonical target.</param>
        void Inspect(SysML2.NET.Core.POCO.Root.Elements.IElement element);

        /// <summary>Reconciles the URI after explicit Browser selection without adding an inspection entry.</summary>
        void ClearInspection();

        /// <summary>Returns within Properties, falling back to selection for direct links.</summary>
        /// <returns>The asynchronous browser navigation operation.</returns>
        Task GoBackAsync();

        /// <summary>Pushes a new inspection entry ending at the chosen earlier trail step.</summary>
        /// <param name="index">The index in the current entry's trail.</param>
        /// <param name="elementId">The rendered identifier, guarding against stale menu actions.</param>
        /// <returns>The canonical resolution and navigation operation.</returns>
        Task InspectTrailStepAsync(int index, string elementId);
    }
}
