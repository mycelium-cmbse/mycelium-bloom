// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramEdgeNotations.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    /// <summary>Supplies the existing generic graphical vocabulary; additional notations need no changes here.</summary>
    public static class DiagramEdgeNotations
    {
        /// <summary>Gets the compositemembership notation defaults.</summary>
        public static DiagramEdgeNotation CompositeMembership { get; } = new(DiagramRelationshipKind.CompositeMembership, DiagramMarker.FilledDiamond, DiagramMarker.None);

        /// <summary>Gets the noncompositemembership notation defaults.</summary>
        public static DiagramEdgeNotation NoncompositeMembership { get; } = new(DiagramRelationshipKind.NoncompositeMembership, DiagramMarker.HollowDiamond, DiagramMarker.None);

        /// <summary>Gets the subclassification notation defaults.</summary>
        public static DiagramEdgeNotation Subclassification { get; } = new(DiagramRelationshipKind.Subclassification, DiagramMarker.None, DiagramMarker.Triangle);

        /// <summary>Gets the definition notation defaults.</summary>
        public static DiagramEdgeNotation Definition { get; } = new(DiagramRelationshipKind.Definition, DiagramMarker.None, DiagramMarker.TypingTriangle);

        /// <summary>Gets the subsetting notation defaults.</summary>
        public static DiagramEdgeNotation Subsetting { get; } = new(DiagramRelationshipKind.Subsetting, DiagramMarker.None, DiagramMarker.Triangle);

        /// <summary>Gets the redefinition notation defaults.</summary>
        public static DiagramEdgeNotation Redefinition { get; } = new(DiagramRelationshipKind.Redefinition, DiagramMarker.None, DiagramMarker.BarredTriangle);

        /// <summary>Gets the connection notation defaults.</summary>
        public static DiagramEdgeNotation Connection { get; } = new(DiagramRelationshipKind.Connection, DiagramMarker.None, DiagramMarker.None, LabelOffset: 38);

        /// <summary>Gets the binding notation defaults.</summary>
        public static DiagramEdgeNotation Binding { get; } = new(DiagramRelationshipKind.Binding, DiagramMarker.None, DiagramMarker.None, Label: "=");

        /// <summary>Gets the dependency notation defaults.</summary>
        public static DiagramEdgeNotation Dependency { get; } = new(DiagramRelationshipKind.Dependency, DiagramMarker.None, DiagramMarker.OpenArrow, DashPattern: "5 4");

        /// <summary>Gets the annotation notation defaults.</summary>
        public static DiagramEdgeNotation Annotation { get; } = new(DiagramRelationshipKind.Annotation, DiagramMarker.None, DiagramMarker.None, DashPattern: "5 4");
    }
}
