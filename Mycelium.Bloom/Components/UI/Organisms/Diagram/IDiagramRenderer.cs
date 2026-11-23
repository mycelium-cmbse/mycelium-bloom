// ------------------------------------------------------------------------------------------------
// <copyright file="IDiagramRenderer.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram
{
    using Blazor.Diagrams;

    /// <summary>Exposes one disposable native mount borrowing tab-owned presentation.</summary>
    public interface IDiagramRenderer : IDisposable
    {
        /// <summary>Gets the native graph owned by the mount.</summary>
        BlazorDiagram Diagram { get; }

        /// <summary>Gets the mount's input and runtime presentation contract.</summary>
        IDiagramPointerBehavior Pointer { get; }
    }
}
