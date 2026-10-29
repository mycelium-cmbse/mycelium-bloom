// ------------------------------------------------------------------------------------------------
// <copyright file="IDetailsPanelViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.DetailsPanel
{
    using ReactiveUI;
    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>Owns the reactive read-only presentation for one Properties Panel.</summary>
    public interface IDetailsPanelViewModel : IReactiveObject, IDisposable
    {
        /// <summary>Gets the current immutable presentation, or null for an empty selection.</summary>
        DetailsPanelPresentation Presentation { get; }

        /// <summary>Inspects a canonical element through the shared selection authority.</summary>
        /// <param name="element">The canonical element to inspect.</param>
        void Inspect(IElement element);

        /// <summary>Clears the canonical selection without changing workspace region visibility.</summary>
        void ClearSelection();

        /// <summary>Reprojects the current selection after an explicit model invalidation.</summary>
        void Refresh();
    }
}
