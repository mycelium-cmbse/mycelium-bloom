// ------------------------------------------------------------------------------------------------
// <copyright file="IDiagramToolCatalogue.cs" company="Starion Group S.A.">
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

    /// <summary>Discovers the tool metadata applicable to a diagram projection.</summary>
    public interface IDiagramToolCatalogue
    {
        /// <summary>Finds registered tools supported by the supplied diagram kind and capabilities.</summary>
        /// <param name="projection">The current diagram projection.</param>
        /// <returns>The supported definitions in registration order.</returns>
        ImmutableArray<DiagramToolDefinition> For(DiagramProjection projection);
    }
}
