// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramCanvasSnapshot.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    using System.Collections.Immutable;

    /// <summary>Projects authoritative state to one mounted browser canvas.</summary>
    /// <param name="SessionId">The mounted renderer identity.</param>
    /// <param name="Revision">The current presentation revision.</param>
    /// <param name="Sequence">The last processed browser command.</param>
    /// <param name="State">The renderer-neutral presentation.</param>
    /// <param name="Projection">The bounded semantic projection.</param>
    /// <param name="CanEdit">Whether presentation changes are permitted.</param>
    /// <param name="Selection">The tab's local occurrence selection.</param>
    public sealed record DiagramCanvasSnapshot(Guid SessionId, long Revision, long Sequence,
        DiagramPresentationState State, DiagramProjection Projection, bool CanEdit, ImmutableHashSet<Guid> Selection);

    /// <summary>Describes completed geometry changes without accepting semantic identities.</summary>
    /// <param name="Id">The existing occurrence identity.</param>
    /// <param name="Geometry">The proposed absolute geometry.</param>
    public sealed record DiagramGeometryChange(Guid Id, DiagramGeometry Geometry);

    /// <summary>Describes completed manual waypoint changes.</summary>
    /// <param name="Id">The existing connection occurrence identity.</param>
    /// <param name="Bends">The proposed waypoints.</param>
    public sealed record DiagramRouteChange(Guid Id, ImmutableArray<DiagramPoint> Bends);

    /// <summary>Submits one completed browser gesture for authoritative validation.</summary>
    /// <param name="SessionId">The mounted renderer identity.</param>
    /// <param name="Revision">The revision on which the gesture was based.</param>
    /// <param name="Sequence">The next browser command sequence.</param>
    /// <param name="Nodes">The changed graphical occurrences.</param>
    /// <param name="Routes">The changed manual routes.</param>
    /// <param name="Viewport">The proposed viewport, when changed.</param>
    public sealed record DiagramCanvasChange(Guid SessionId, long Revision, long Sequence,
        ImmutableArray<DiagramGeometryChange> Nodes, ImmutableArray<DiagramRouteChange> Routes, DiagramViewport Viewport);

    /// <summary>Returns accepted or rejected commands with the authoritative recovery snapshot.</summary>
    /// <param name="Accepted">Whether validation accepted the change.</param>
    /// <param name="Snapshot">The current authoritative snapshot.</param>
    public sealed record DiagramCanvasResult(bool Accepted, DiagramCanvasSnapshot Snapshot);
}
