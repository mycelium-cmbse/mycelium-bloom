// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramToolDefaults.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram.Tools
{
    using System.Collections.Immutable;

    using Mycelium.Bloom.Model.Diagram;

    /// <summary>Registers the first two diagram tools and the default toolbox arrangement.</summary>
    public static class DiagramToolDefaults
    {
        /// <summary>The stable identifier for selection and permitted occurrence movement.</summary>
        public const string Move = "move";

        /// <summary>The stable identifier for viewport panning.</summary>
        public const string Pan = "pan";

        /// <summary>Gets the built-in tool registrations shared by DI and directly constructed diagrams.</summary>
        public static ImmutableArray<DiagramToolDefinition> Definitions { get; } =
        [
            new(Move, "Move", "mouse-pointer-2", (context, _) => new MoveDiagramTool(context))
            {
                AllowsSelection = true,
                Tooltip = "Select / Move"
            },
            new(Pan, "Pan", "hand", (context, _) => new PanDiagramTool(context))
            {
                CapturesNestedInput = true,
                Tooltip = "Pan (hold Space for temporary navigation)"
            }
        ];

        /// <summary>Gets the default placement and temporary navigation tool.</summary>
        public static DiagramToolLayout Layout { get; } = new([new(Move, "editing"), new(Pan, "navigation")], Move, Pan);
    }
}
