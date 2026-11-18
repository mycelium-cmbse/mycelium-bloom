// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramProjection.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    using System.Collections.Immutable;

    /// <summary>Describes an immutable graphical slice and any explicitly omitted content.</summary>
    /// <param name="Nodes">The initialized node presentation collection.</param>
    /// <param name="Edges">The initialized edge presentation collection.</param>
    /// <param name="ViewName">The optional name displayed in the view frame.</param>
    /// <param name="HasOmittedContent">Whether requested content was unavailable or omitted.</param>
    public sealed record DiagramProjection(ImmutableArray<DiagramNodePresentation> Nodes, ImmutableArray<DiagramEdgePresentation> Edges,
        string ViewName = null, bool HasOmittedContent = false);
}
