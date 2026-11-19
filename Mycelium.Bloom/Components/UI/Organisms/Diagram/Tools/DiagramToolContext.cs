// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramToolContext.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram.Tools
{
    using Blazor.Diagrams.Core.Models.Base;

    using Mycelium.Bloom.ViewModel.Diagram;

    using NativeDiagram = Blazor.Diagrams.Core.Diagram;

    /// <summary>Supplies a mounted native graph and its tab-owned presentation to a runtime tool.</summary>
    /// <param name="Diagram">The native diagram used for hit models and viewport operations.</param>
    /// <param name="Owner">The mounted diagram's tab-owned presentation authority.</param>
    /// <param name="FindElement">Resolves the tab-owned element presentation represented by a native hit.</param>
    public sealed record DiagramToolContext(NativeDiagram Diagram, IDiagramViewModel Owner, Func<Model, DiagramElementViewModel> FindElement);
}
