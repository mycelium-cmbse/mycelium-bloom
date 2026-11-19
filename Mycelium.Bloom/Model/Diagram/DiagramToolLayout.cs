// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramToolLayout.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    using System.Collections.Immutable;

    /// <summary>Places tools in display order independently of their implementations.</summary>
    /// <param name="Items">The ordered toolbox placements.</param>
    /// <param name="DefaultToolId">The preferred tool when a tab first opens.</param>
    /// <param name="NavigationToolId">The tool invoked by temporary navigation gestures, when available.</param>
    public sealed record DiagramToolLayout(ImmutableArray<DiagramToolLayout.Item> Items, string DefaultToolId, string NavigationToolId)
    {
        /// <summary>Places one tool in an ordered layout with an optional group and shortcut override.</summary>
        /// <param name="ToolId">The registered tool to present.</param>
        /// <param name="Group">Adjacent tools with different groups get a visual separator.</param>
        /// <param name="Shortcut">The shortcut override for this placement, when supplied.</param>
        /// <param name="Parameters">The immutable tool configuration for this placement.</param>
        public sealed record Item(string ToolId, string Group = null, string Shortcut = null, IDiagramToolParameters Parameters = null);
    }
}
