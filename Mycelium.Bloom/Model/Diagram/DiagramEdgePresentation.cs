// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramEdgePresentation.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    using System.Collections.Immutable;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>Connects graphical occurrences while retaining the canonical relationship or annotation.</summary>
    /// <param name="Id">The view-local graphical identity.</param>
    /// <param name="Element">The borrowed canonical SDK object.</param>
    /// <param name="Source">The source graphical occurrence identity.</param>
    /// <param name="Target">The target graphical occurrence identity.</param>
    /// <param name="Notation">The registered graphical notation descriptor.</param>
    /// <param name="Label">The already-formatted visible notation.</param>
    /// <param name="SourceLabel">The optional source-end notation.</param>
    /// <param name="TargetLabel">The optional target-end notation.</param>
    /// <param name="SourceFeature">The optional canonical boundary endpoint or source node element.</param>
    /// <param name="TargetFeature">The optional canonical boundary endpoint or target node element.</param>
    /// <param name="Bends">The optional model-space polyline bends.</param>
    public sealed record DiagramEdgePresentation(Guid Id, IElement Element, Guid Source, Guid Target, DiagramEdgeNotation Notation,
        string Label, DiagramEndLabel SourceLabel = null, DiagramEndLabel TargetLabel = null, IElement SourceFeature = null, IElement TargetFeature = null,
            ImmutableArray<DiagramPoint> Bends = default)
    {
        /// <summary>Gets the extensible notation identity.</summary>
        public string Kind => this.Notation.Key;

        /// <summary>Gets the canonical source-end marker.</summary>
        public DiagramMarker SourceMarker => this.Notation.SourceMarker;

        /// <summary>Gets the canonical target-end marker.</summary>
        public DiagramMarker TargetMarker => this.Notation.TargetMarker;

        /// <summary>Gets whether the relationship uses a dashed line.</summary>
        public bool IsDashed => this.Notation.IsDashed;
    }
}
