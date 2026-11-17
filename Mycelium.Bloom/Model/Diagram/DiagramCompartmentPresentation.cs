// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramCompartmentPresentation.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    using System.Collections.Immutable;

    /// <summary>Describes a titled textual or graphical compartment without owning semantic data.</summary>
    /// <param name="Title">The caller-selected compartment heading.</param>
    /// <param name="Rows">The already-formatted textual member rows.</param>
    /// <param name="IsTruncated">Whether additional textual members are omitted.</param>
    /// <param name="GraphicalView">The optional nested graphical presentation.</param>
    public sealed record DiagramCompartmentPresentation(string Title, ImmutableArray<string> Rows, bool IsTruncated = false, DiagramProjection GraphicalView = null);
}
