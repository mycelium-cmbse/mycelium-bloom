// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramToolCatalogue.cs" company="Starion Group S.A.">
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

    /// <summary>Resolves registered tool definitions for a diagram projection.</summary>
    public sealed class DiagramToolCatalogue : IDiagramToolCatalogue
    {
        /// <summary>The immutable registration order.</summary>
        private readonly ImmutableArray<DiagramToolDefinition> definitions;

        /// <summary>Creates the catalogue from independently registered definitions.</summary>
        /// <param name="definitions">The available DI registrations.</param>
        public DiagramToolCatalogue(IEnumerable<DiagramToolDefinition> definitions)
        {
            this.definitions = definitions.ToImmutableArray();

            if (this.definitions.Any(definition => string.IsNullOrWhiteSpace(definition.Id))
                || this.definitions.Select(definition => definition.Id).Distinct(StringComparer.Ordinal).Count() != this.definitions.Length)
            {
                throw new ArgumentException("Diagram tool IDs must be non-empty and unique.", nameof(definitions));
            }

            if (this.definitions.Any(definition => definition.ParameterType is { } type && !typeof(IDiagramToolParameters).IsAssignableFrom(type)))
            {
                throw new ArgumentException("Diagram tool parameter types must implement IDiagramToolParameters.", nameof(definitions));
            }
        }

        /// <summary>Finds registered tools supported by the supplied diagram kind and capabilities.</summary>
        /// <param name="projection">The current diagram projection.</param>
        /// <returns>The supported definitions in registration order.</returns>
        public ImmutableArray<DiagramToolDefinition> For(DiagramProjection projection) => this.definitions
            .Where(tool => (tool.DiagramKinds.IsEmpty || tool.DiagramKinds.Contains(projection.Kind))
                && tool.RequiredCapabilities.All(projection.Capabilities.Contains))
            .ToImmutableArray();
    }
}
