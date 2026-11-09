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

    using SysML2.NET.Core.Core.Types;

    /// <summary>Describes a supported part without serializing the SDK object graph.</summary>
    /// <param name="ElementId">The semantic identity.</param>
    /// <param name="Name">The compact name and type header.</param>
    /// <param name="ParentElementId">The containing semantic part, when present.</param>
    /// <param name="Attributes">The explicitly supported literal attribute values.</param>
    /// <param name="Ports">The owned boundary ports.</param>
    /// <param name="DetailStatus">Transient loading status, separate from semantic values.</param>
    public sealed record DiagramPart(
        string ElementId, string Name, string ParentElementId,
        ImmutableArray<string> Attributes, ImmutableArray<DiagramPort> Ports, string DetailStatus = null);

    /// <summary>Describes one canonical boundary port.</summary>
    /// <param name="ElementId">The semantic identity.</param>
    /// <param name="Name">The port name.</param>
    /// <param name="Direction">The SDK direction, or null when unspecified.</param>
    public sealed record DiagramPort(string ElementId, string Name, FeatureDirectionKind? Direction);

    /// <summary>Describes a resolved binary connection between owned ports.</summary>
    /// <param name="ElementId">The semantic connection identity.</param>
    /// <param name="Name">The connection name.</param>
    /// <param name="SourcePartId">The part owning the first end's referenced port.</param>
    /// <param name="SourcePortId">The first referenced port.</param>
    /// <param name="TargetPartId">The part owning the second end's referenced port.</param>
    /// <param name="TargetPortId">The second referenced port.</param>
    /// <param name="SourceMultiplicity">The first end's literal multiplicity, when supported.</param>
    /// <param name="TargetMultiplicity">The second end's literal multiplicity, when supported.</param>
    public sealed record DiagramConnection(
        string ElementId, string Name, string SourcePartId, string SourcePortId,
        string TargetPartId, string TargetPortId, string SourceMultiplicity, string TargetMultiplicity);

    /// <summary>Captures the supported projection and explicit diagnostic messages.</summary>
    /// <param name="ModelScope">The canonical root identity.</param>
    /// <param name="Parts">The supported parts keyed by ElementId.</param>
    /// <param name="Connections">The resolved binary connections keyed by ElementId.</param>
    /// <param name="Diagnostics">The relationships or notation omitted from the slice.</param>
    public sealed record DiagramProjection(
        string ModelScope,
        ImmutableDictionary<string, DiagramPart> Parts,
        ImmutableDictionary<string, DiagramConnection> Connections,
        ImmutableArray<string> Diagnostics);
}
