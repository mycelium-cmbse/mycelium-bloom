// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramBoundaryPortModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram
{
    using Blazor.Diagrams.Core.Geometry;
    using Blazor.Diagrams.Core.Models;

    using Mycelium.Bloom.ViewModel.Diagram;

    /// <summary>Shares one native port identity between hit testing and link geometry.</summary>
    internal sealed class DiagramBoundaryPortModel : PortModel, IDiagramBoundaryPortModel
    {
        /// <summary>Gets the occurrence-owned boundary presentation represented by this port.</summary>
        public DiagramBoundaryViewModel ViewModel { get; }

        /// <summary>Adapts a boundary feature whose position and size have already been calculated.</summary>
        /// <param name="id">The native port identifier.</param>
        /// <param name="parent">The native parent occurrence.</param>
        /// <param name="alignment">The port's side on the parent.</param>
        /// <param name="position">The native port position.</param>
        /// <param name="size">The size of the boundary glyph used for anchors.</param>
        /// <param name="viewModel">The boundary presentation borrowed from the owning tab.</param>
        internal DiagramBoundaryPortModel(string id, NodeModel parent, PortAlignment alignment, Point position, Size size, DiagramBoundaryViewModel viewModel)
            : base(id, parent, alignment, position, size)
        {
            this.ViewModel = viewModel;
            this.Initialized = true;
        }
    }
}
