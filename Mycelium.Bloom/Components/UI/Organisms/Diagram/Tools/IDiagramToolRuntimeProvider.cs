// ------------------------------------------------------------------------------------------------
// <copyright file="IDiagramToolRuntimeProvider.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram.Tools
{
    using Mycelium.Bloom.ViewModel.Diagram.Tools;

    /// <summary>Creates tool runtimes without retaining their renderer-owned state.</summary>
    public interface IDiagramToolRuntimeProvider
    {
        /// <summary>Creates the registered runtime for a configured placement.</summary>
        /// <param name="placement">The applicable tool and its parameters.</param>
        /// <param name="context">The requesting renderer's context.</param>
        /// <returns>A runtime owned and disposed by the renderer.</returns>
        IDiagramTool Create(DiagramToolPlacement placement, DiagramToolContext context);
    }
}
