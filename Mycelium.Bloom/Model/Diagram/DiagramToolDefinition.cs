// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramToolDefinition.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    using System.Collections.Immutable;

    using Mycelium.Bloom.ViewModel.Diagram.Tools;

    /// <summary>Describes one registered diagram tool independently of its placement and runtime lifetime.</summary>
    /// <param name="Id">The stable identifier used by layouts.</param>
    /// <param name="Label">The accessible display label.</param>
    /// <param name="Icon">The existing icon identifier.</param>
    /// <param name="CreateRuntime">Creates a tool for a mounted diagram using its context and this definition's parameters.</param>
    public sealed record DiagramToolDefinition(string Id, string Label, string Icon, Func<DiagramToolContext, IDiagramToolParameters, IDiagramTool> CreateRuntime)
    {
        /// <summary>Gets the default unmodified key, when supplied.</summary>
        public string DefaultShortcut { get; init; }

        /// <summary>Gets the supported projection kinds. An empty array supports any kind.</summary>
        public ImmutableArray<string> DiagramKinds { get; init; } = [];

        /// <summary>Gets the capabilities required from the projection.</summary>
        public ImmutableArray<string> RequiredCapabilities { get; init; } = [];

        /// <summary>Gets the immutable parameters passed to this registration's runtime factory.</summary>
        public IDiagramToolParameters Parameters { get; init; }

        /// <summary>Gets whether the tool requires an editable diagram to be enabled.</summary>
        public bool RequiresEditableDiagram { get; init; }

        /// <summary>Gets whether the tool permits selection through diagram content.</summary>
        public bool AllowsSelection { get; init; }

        /// <summary>Gets whether gestures over nested canvases should route to the owning canvas.</summary>
        public bool CapturesNestedInput { get; init; }

        /// <summary>Gets the optional tooltip displayed in addition to the accessible label.</summary>
        public string Tooltip { get; init; }
    }
}
