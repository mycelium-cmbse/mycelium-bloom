// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramBenchmarkSnapshot.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    /// <summary>Returns bounded authoritative readback without adding presentation mutations.</summary>
    /// <param name="SessionId">The current diagnostic mount identity.</param>
    /// <param name="DiagramId">The tab-owned presentation identity.</param>
    /// <param name="Revision">The observed presentation revision within this mount.</param>
    /// <param name="Timestamp">The server's monotonic observation timestamp.</param>
    /// <param name="Frequency">The monotonic clock frequency.</param>
    /// <param name="Geometry">The requested occurrence's accepted geometry.</param>
    /// <param name="Viewport">The accepted viewport.</param>
    /// <param name="Nodes">The number of displayed occurrences.</param>
    /// <param name="Edges">The number of displayed connections.</param>
    public sealed record DiagramBenchmarkSnapshot(Guid SessionId, Guid DiagramId, long Revision, long Timestamp, long Frequency,
        DiagramGeometry Geometry, DiagramViewport Viewport, int Nodes, int Edges)
    {
        /// <summary>Gets the transport classification marker used to exclude probe traffic.</summary>
        public string Kind => "diagram-benchmark";
    }
}
