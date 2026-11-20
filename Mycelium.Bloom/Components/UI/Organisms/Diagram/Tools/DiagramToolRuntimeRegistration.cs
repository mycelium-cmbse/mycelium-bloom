// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramToolRuntimeRegistration.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram.Tools
{
    using Mycelium.Bloom.Model.Diagram;

    /// <summary>Registers a mounted runtime factory against stable tool metadata.</summary>
    /// <param name="ToolId">The identifier shared with the definition.</param>
    /// <param name="Create">Creates an instance owned by the requesting renderer.</param>
    public sealed record DiagramToolRuntimeRegistration(string ToolId, Func<DiagramToolContext, IDiagramToolParameters, IDiagramTool> Create);
}
