// ------------------------------------------------------------------------------------------------
// <copyright file="IDiagramBoundaryPortModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram
{
    using Mycelium.Bloom.ViewModel.Diagram;

    /// <summary>Maps a native boundary hit to its tab-owned presentation.</summary>
    public interface IDiagramBoundaryPortModel
    {
        /// <summary>Gets the semantic selection target represented by this port.</summary>
        DiagramBoundaryViewModel ViewModel { get; }
    }
}
