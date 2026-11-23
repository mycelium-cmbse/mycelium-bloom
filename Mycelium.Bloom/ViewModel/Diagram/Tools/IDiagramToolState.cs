// ------------------------------------------------------------------------------------------------
// <copyright file="IDiagramToolState.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram.Tools
{
    using System.Collections.Immutable;
    using System.ComponentModel;

    using Mycelium.Bloom.Model.Diagram;

    /// <summary>Exposes one tab's reactive tool selection to mounted views.</summary>
    public interface IDiagramToolState : INotifyPropertyChanged, IDisposable
    {
        /// <summary>Gets applicable registered definitions.</summary>
        ImmutableArray<DiagramToolDefinition> Available { get; }

        /// <summary>Gets compatible placements in layout order.</summary>
        ImmutableArray<DiagramToolPlacement> Toolbox { get; }

        /// <summary>Gets the retained toolbox choice.</summary>
        DiagramToolPlacement Active { get; }

        /// <summary>Gets or sets the navigation override owned by the current mount.</summary>
        DiagramToolPlacement Temporary { get; set; }

        /// <summary>Gets the placement currently handling input.</summary>
        DiagramToolPlacement Effective { get; }

        /// <summary>Gets whether editing tools are permitted.</summary>
        bool CanEdit { get; }

        /// <summary>Gets the enabled temporary navigation placement.</summary>
        DiagramToolPlacement Navigation { get; }

        /// <summary>Checks whether an exact toolbox placement remains enabled.</summary>
        /// <param name="placement">The candidate placement.</param>
        /// <returns>Whether activation is permitted.</returns>
        bool CanActivate(DiagramToolPlacement placement);

        /// <summary>Activates the first enabled placement for an identifier.</summary>
        /// <param name="id">The stable tool identifier.</param>
        /// <returns>Whether activation succeeded.</returns>
        bool TryActivate(string id);

        /// <summary>Activates an exact enabled placement.</summary>
        /// <param name="placement">The candidate placement.</param>
        /// <returns>Whether activation succeeded.</returns>
        bool TryActivate(DiagramToolPlacement placement);

        /// <summary>Refreshes applicability for the tab's current projection.</summary>
        /// <param name="projection">The validated projection.</param>
        void ForProjection(DiagramProjection projection);
    }
}
