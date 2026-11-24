// ------------------------------------------------------------------------------------------------
// <copyright file="IDiagramResizeTool.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram.Tools
{
    using Blazor.Diagrams.Core.Events;

    /// <summary>Accepts resize handles in a runtime that uses the dispatcher's ordinary pointer lifecycle.</summary>
    public interface IDiagramResizeTool
    {
        /// <summary>Begins resizing a permitted native occurrence.</summary>
        /// <param name="node">The mounted occurrence represented by the handle.</param>
        /// <param name="args">The starting browser pointer event.</param>
        /// <returns>Whether the runtime accepted the resize gesture.</returns>
        bool BeginResize(DiagramNodeModel node, PointerEventArgs args);
    }
}
