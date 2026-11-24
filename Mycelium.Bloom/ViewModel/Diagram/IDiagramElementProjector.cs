// ------------------------------------------------------------------------------------------------
// <copyright file="IDiagramElementProjector.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram
{
    using Mycelium.Bloom.Model.Diagram;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>Extends graphical slices through registered notation without changing canonical elements.</summary>
    public interface IDiagramElementProjector
    {
        /// <summary>Adds an independent occurrence and supported existing relationships at a model-space position.</summary>
        /// <param name="projection">The retained immutable slice.</param>
        /// <param name="element">The exact canonical element dropped by the Browser.</param>
        /// <param name="position">The finite model-space drop position.</param>
        /// <returns>The extended slice, or null when the element or projection limit prohibits a node.</returns>
        DiagramProjection AddElement(DiagramProjection projection, IElement element, DiagramPoint position);
    }
}
