// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramEdgeNotation.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    /// <summary>Describes relationship notation independently of SDK metaclasses and renderer resources.</summary>
    /// <param name="Key">The extensible notation identity.</param>
    /// <param name="SourceMarker">The canonical source-end geometry.</param>
    /// <param name="TargetMarker">The canonical target-end geometry.</param>
    /// <param name="DashPattern">The optional SVG dash pattern; null selects a solid line.</param>
    /// <param name="Label">The already-formatted optional relationship label.</param>
    /// <param name="LabelOffset">The model-space label clearance above the line.</param>
    public sealed record DiagramEdgeNotation(string Key, DiagramMarker SourceMarker, DiagramMarker TargetMarker,
        string DashPattern = null, string Label = null, double LabelOffset = 10)
    {
        /// <summary>Gets whether the notation supplies a nonsolid line pattern.</summary>
        public bool IsDashed => !string.IsNullOrWhiteSpace(this.DashPattern);
    }
}
