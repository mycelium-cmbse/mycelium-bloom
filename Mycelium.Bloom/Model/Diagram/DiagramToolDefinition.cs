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

    /// <summary>Describes one registered diagram tool independently of its placement and runtime lifetime.</summary>
    /// <param name="Id">The stable identifier used by layouts.</param>
    /// <param name="Label">The accessible display label.</param>
    /// <param name="Icon">The existing icon identifier.</param>
    public sealed record DiagramToolDefinition(string Id, string Label, string Icon)
    {
        /// <summary>Gets the default unmodified key, when supplied.</summary>
        public string DefaultShortcut { get; init; }

        /// <summary>Gets the supported projection kinds. An empty array supports any kind.</summary>
        public ImmutableArray<string> DiagramKinds { get; init; } = [];

        /// <summary>Gets the capabilities required from the projection.</summary>
        public ImmutableArray<string> RequiredCapabilities { get; init; } = [];

        /// <summary>Gets the required immutable parameter contract, or null for a parameterless tool.</summary>
        public Type ParameterType { get; init; }

        /// <summary>Gets whether the tool requires an editable diagram to be enabled.</summary>
        public bool RequiresEditableDiagram { get; init; }

        /// <summary>Gets whether the tool permits selection through diagram content.</summary>
        public bool AllowsSelection { get; init; }

        /// <summary>Gets whether the tool accepts placement of existing elements from the Project Browser.</summary>
        public bool AllowsElementDrop { get; init; }

        /// <summary>Gets whether gestures over nested canvases should route to the owning canvas.</summary>
        public bool CapturesNestedInput { get; init; }

        /// <summary>Gets the optional tooltip displayed in addition to the accessible label.</summary>
        public string Tooltip { get; init; }

        /// <summary>Checks whether a layout entry supplies the required parameter type.</summary>
        /// <param name="parameters">The immutable configuration for one placement.</param>
        /// <returns>Whether the tool can accept the supplied parameter shape.</returns>
        internal bool SupportsParameters(IDiagramToolParameters parameters) =>
            this.ParameterType is null ? parameters is null : this.ParameterType.IsInstanceOfType(parameters);
    }
}
