// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramPresentationState.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    using System.Collections.Immutable;

    /// <summary>Stores versioned prototype presentation independently of the semantic model.</summary>
    /// <param name="Version">The presentation schema version.</param>
    /// <param name="DiagramId">The presentation identity.</param>
    /// <param name="ModelScope">The canonical root ElementId.</param>
    /// <param name="Occurrences">The displayed graphical occurrences.</param>
    /// <param name="Routes">The displayed semantic connection occurrences.</param>
    /// <param name="Viewport">The restorable canvas transform.</param>
    public sealed record DiagramPresentationState(
        int Version,
        Guid DiagramId,
        string ModelScope,
        ImmutableArray<DiagramOccurrence> Occurrences,
        ImmutableArray<DiagramRoute> Routes,
        DiagramViewport Viewport);

    /// <summary>Identifies one graphical occurrence of a canonical semantic element.</summary>
    /// <param name="Id">The graphical identity, distinct from ElementId.</param>
    /// <param name="ElementId">The stable semantic identity.</param>
    /// <param name="Geometry">The absolute model-space geometry.</param>
    /// <param name="ParentId">The containing graphical occurrence, when present.</param>
    public sealed record DiagramOccurrence(Guid Id, string ElementId, DiagramGeometry Geometry, Guid? ParentId);

    /// <summary>Stores the geometry of a displayed occurrence.</summary>
    /// <param name="X">The left coordinate.</param>
    /// <param name="Y">The top coordinate.</param>
    /// <param name="Width">The width.</param>
    /// <param name="Height">The height.</param>
    public sealed record DiagramGeometry(double X, double Y, double Width, double Height);

    /// <summary>Stores one model-space waypoint.</summary>
    /// <param name="X">The horizontal coordinate.</param>
    /// <param name="Y">The vertical coordinate.</param>
    public sealed record DiagramPoint(double X, double Y);

    /// <summary>Stores a semantic connection's occurrence endpoints and manually edited bends.</summary>
    /// <param name="Id">The graphical connection identity.</param>
    /// <param name="ElementId">The canonical connection ElementId.</param>
    /// <param name="SourceId">The source node occurrence.</param>
    /// <param name="SourcePortId">The source semantic port.</param>
    /// <param name="TargetId">The target node occurrence.</param>
    /// <param name="TargetPortId">The target semantic port.</param>
    /// <param name="Bends">The manual waypoints, or an empty array for automatic routing.</param>
    public sealed record DiagramRoute(
        Guid Id, string ElementId, Guid SourceId, string SourcePortId,
        Guid TargetId, string TargetPortId, ImmutableArray<DiagramPoint> Bends);

    /// <summary>Stores the viewport independently of the current pane dimensions.</summary>
    /// <param name="X">The horizontal pan in CSS pixels.</param>
    /// <param name="Y">The vertical pan in CSS pixels.</param>
    /// <param name="Zoom">The scale factor.</param>
    public sealed record DiagramViewport(double X, double Y, double Zoom);
}
