// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramToolPlacement.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram.Tools
{
    using Mycelium.Bloom.Model.Diagram;

    /// <summary>Identifies a configured tool placement within one diagram toolbox.</summary>
    /// <param name="Tool">The tool metadata.</param>
    /// <param name="Item">The placement's configuration.</param>
    /// <param name="Index">The original layout position, or -1 for a navigation tool outside the toolbox.</param>
    public sealed record DiagramToolPlacement(DiagramToolDefinition Tool, DiagramToolLayout.Item Item, int Index);
}
