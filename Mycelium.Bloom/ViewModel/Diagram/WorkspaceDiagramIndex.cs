// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceDiagramIndex.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram
{
    using System.Collections.Immutable;
    using System.Text;
    using Microsoft.Extensions.Caching.Memory;
    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Model.Diagram;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Systems.Attributes;
    using SysML2.NET.Core.POCO.Systems.Connections;
    using SysML2.NET.Core.POCO.Systems.Parts;
    using SysML2.NET.Core.POCO.Systems.Ports;

    /// <summary>Indexes an already-loaded canonical scope without retaining complete renderer details.</summary>
    internal sealed class WorkspaceDiagramIndex : IDisposable
    {
        private readonly MemoryCache details;
        private readonly DiagramDataOptions options;

        private WorkspaceDiagramIndex(DiagramDataOptions options, Microsoft.Extensions.Internal.ISystemClock clock)
        {
            this.options = options;
            this.details = new MemoryCache(new MemoryCacheOptions
            { SizeLimit = options.CapacityBytes, TrackStatistics = true, Clock = clock });
        }

        /// <summary>Gets references to the existing canonical objects for scope-local selection.</summary>
        public Dictionary<string, IElement> Elements { get; } = new(StringComparer.Ordinal);

        /// <summary>Gets navigation and endpoint metadata with unformatted details.</summary>
        public DiagramProjection Metadata { get; private set; }

        /// <summary>Gets the complete lightweight initial presentation.</summary>
        public DiagramPresentationState Presentation { get; private set; }

        /// <summary>Gets bounded formatted-detail reuse statistics, excluding the canonical SDK graph.</summary>
        public MemoryCacheStatistics Statistics => this.details.GetCurrentStatistics();

        /// <summary>Builds bounded metadata cooperatively without traversing semantic reference targets.</summary>
        /// <param name="root">The existing canonical scope root.</param>
        /// <param name="options">The bounded formatted-detail cache configuration.</param>
        /// <param name="progress">Receives throttled indexed-element counts.</param>
        /// <param name="cancellationToken">Cancels obsolete scope preparation.</param>
        /// <param name="clock">The optional expiration clock.</param>
        /// <returns>The validated, tab-owned scope index.</returns>
        public static async Task<WorkspaceDiagramIndex> CreateAsync(IElement root, DiagramDataOptions options,
            Action<int> progress, CancellationToken cancellationToken,
            Microsoft.Extensions.Internal.ISystemClock clock = null)
        {
            var result = new WorkspaceDiagramIndex(options, clock);
            try
            {
                var parts = ImmutableDictionary.CreateBuilder<string, DiagramPart>(StringComparer.Ordinal);
                var connectionElements = new List<IConnectionUsage>();
                var ports = new Dictionary<string, string>(StringComparer.Ordinal);
                var pending = new Stack<(IElement Element, string Parent)>();
                var progressClock = System.Diagnostics.Stopwatch.StartNew();
                pending.Push((root, null));
                while (pending.TryPop(out var item))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!result.Elements.TryAdd(item.Element.ElementId, item.Element))
                    {
                        continue;
                    }

                    if (result.Elements.Count > 1_000_000)
                    {
                        throw new InvalidDataException("The scope exceeds the supported semantic index size.");
                    }

                    var parent = item.Parent;
                    if (item.Element is IConnectionUsage connection)
                    {
                        connectionElements.Add(connection);
                    }
                    else if (item.Element is IPartUsage part)
                    {
                        if (parts.Count >= DiagramPresentationLimits.Workspace.Nodes)
                        {
                            throw new InvalidDataException("The scope exceeds 20,000 supported parts.");
                        }

                        var ownedPorts = part.ownedElement.OfType<IPortUsage>().Select(port =>
                        {
                            ports.Add(port.ElementId, part.ElementId);
                            return new DiagramPort(port.ElementId, string.Empty, port.Direction);
                        }).ToImmutableArray();
                        parts.Add(part.ElementId,
                            new DiagramPart(part.ElementId, part.DeclaredName ?? string.Empty, parent, [], ownedPorts));
                        parent = part.ElementId;
                    }

                    foreach (var child in item.Element.ownedElement.Concat(item.Element.OwnedRelationship).Reverse())
                    {
                        pending.Push((child, parent));
                    }

                    if (result.Elements.Count % 256 == 0)
                    {
                        if (progressClock.ElapsedMilliseconds >= 100)
                        {
                            progress(result.Elements.Count);
                            progressClock.Restart();
                        }

                        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
                    }
                }

                var connections = ImmutableDictionary.CreateBuilder<string, DiagramConnection>(StringComparer.Ordinal);
                var diagnostics = ImmutableArray.CreateBuilder<string>();
                foreach (var connection in connectionElements)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var ends = connection.ownedEndFeature;
                    if (ends.Count != 2 || ends[0].ownedReferenceSubsetting?.ReferencedFeature is not IPortUsage source
                                        || ends[1].ownedReferenceSubsetting?.ReferencedFeature is not IPortUsage target
                                        || !ports.TryGetValue(source.ElementId, out var sourcePart) ||
                                        !ports.TryGetValue(target.ElementId, out var targetPart))
                    {
                        diagnostics.Add(
                            $"Connection {connection.ElementId}: requires two resolved references to owned ports in this scope.");
                        continue;
                    }

                    if (connections.Count >= DiagramPresentationLimits.Workspace.Routes)
                    {
                        throw new InvalidDataException("The scope exceeds 60,000 supported connections.");
                    }

                    connections.Add(connection.ElementId, new DiagramConnection(connection.ElementId, string.Empty,
                        sourcePart, source.ElementId, targetPart, target.ElementId, string.Empty, string.Empty));
                }

                result.Metadata = new DiagramProjection(root.ElementId, parts.ToImmutable(), connections.ToImmutable(),
                    diagnostics.ToImmutable());
                result.Presentation = result.CreatePresentation(cancellationToken);
                if (!DiagramPresentationSerializer.Validate(result.Presentation, result.Metadata, out var error,
                        DiagramPresentationLimits.Workspace))
                {
                    throw new InvalidDataException(error);
                }

                return result;
            }
            catch
            {
                result.Dispose();
                throw;
            }
        }

        /// <summary>Formats only the requested canonical parts and connections, with a bounded tab-owned read cache.</summary>
        /// <param name="region">The required occurrences and endpoint dependencies.</param>
        /// <param name="cancellationToken">Cancels superseded detail work.</param>
        /// <returns>Full navigation metadata with regional details.</returns>
        public DiagramProjection Project(DiagramPresentationState region, CancellationToken cancellationToken)
        {
            var parts = this.Metadata.Parts.ToBuilder();
            var connections = this.Metadata.Connections.ToBuilder();
            var diagnostics = this.Metadata.Diagnostics.ToBuilder();
            foreach (var id in region.Occurrences.Select(node => node.ElementId).Distinct(StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!this.details.TryGetValue(id, out DiagramPart detail))
                {
                    var part = (IPartUsage)this.Elements[id];
                    detail = parts[id] with
                    {
                        Name = InterconnectionProjection.FormatHeader(part),
                        Attributes = part.ownedElement.OfType<IAttributeUsage>().Select(attribute =>
                            InterconnectionProjection.FormatAttribute(attribute, diagnostics)).ToImmutableArray(),
                        Ports = part.ownedElement.OfType<IPortUsage>().Select(port =>
                                new DiagramPort(port.ElementId, InterconnectionProjection.Name(port), port.Direction))
                            .ToImmutableArray()
                    };
                    var bytes = 128 + Encoding.UTF8.GetByteCount(detail.Name) +
                                detail.Attributes.Sum(Encoding.UTF8.GetByteCount)
                                + detail.Ports.Sum(port => 64 + Encoding.UTF8.GetByteCount(port.Name));
                    this.details.Set(id, detail,
                        new MemoryCacheEntryOptions
                        { Size = bytes, SlidingExpiration = TimeSpan.FromSeconds(this.options.ExpirationSeconds) });
                }

                parts[id] = detail;
            }

            foreach (var id in region.Routes.Select(route => route.ElementId).Distinct(StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var connection = (IConnectionUsage)this.Elements[id];
                connections[id] = connections[id] with
                {
                    Name = InterconnectionProjection.Name(connection),
                    SourceMultiplicity =
                    InterconnectionProjection.FormatMultiplicity(connection.ownedEndFeature[0], diagnostics),
                    TargetMultiplicity =
                    InterconnectionProjection.FormatMultiplicity(connection.ownedEndFeature[1], diagnostics)
                };
            }

            return this.Metadata with
            {
                Parts = parts.ToImmutable(),
                Connections = connections.ToImmutable(),
                Diagnostics = diagnostics.ToImmutable()
            };
        }

        private DiagramPresentationState CreatePresentation(CancellationToken cancellationToken)
        {
            var children = this.Metadata.Parts.Values.OrderBy(part => part.ElementId, StringComparer.Ordinal)
                .ToLookup(part => part.ParentElementId ?? string.Empty);
            var geometry = new Dictionary<string, DiagramGeometry>(StringComparer.Ordinal);
            var ordered = new List<(string Id, int Depth)>();
            var pending = new Stack<(string Id, int Depth)>();
            foreach (var part in children[string.Empty].Reverse())
            {
                pending.Push((part.ElementId, 0));
            }

            while (pending.TryPop(out var item))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (item.Depth > DiagramPresentationLimits.Workspace.NestingDepth)
                {
                    throw new InvalidDataException("The scope exceeds 128 levels of part nesting.");
                }

                ordered.Add(item);
                foreach (var child in children[item.Id].Reverse())
                {
                    pending.Push((child.ElementId, item.Depth + 1));
                }
            }

            if (ordered.Count != this.Metadata.Parts.Count)
            {
                throw new InvalidDataException("Cyclic part containment.");
            }

            foreach (var item in ordered.AsEnumerable().Reverse())
            {
                var x = 16d;
                var y = 140d;
                var rowHeight = 0d;
                var width = 260d;
                var rowWidth = Math.Max(1600, Math.Ceiling(Math.Sqrt(children[item.Id].Count())) * 320);
                foreach (var child in children[item.Id])
                {
                    var size = geometry[child.ElementId];
                    if (x > 16 && x + size.Width > rowWidth)
                    {
                        x = 16;
                        y += rowHeight + 32;
                        rowHeight = 0;
                    }

                    geometry[child.ElementId] = size with { X = x, Y = y };
                    width = Math.Max(width, x + size.Width + 16);
                    rowHeight = Math.Max(rowHeight, size.Height);
                    x += size.Width + 32;
                }

                geometry[item.Id] = new DiagramGeometry(0, 0, width, Math.Max(270, y + rowHeight + 16));
            }

            var rootX = 64d;
            var rootY = 66d;
            var rootHeight = 0d;
            foreach (var part in children[string.Empty])
            {
                var size = geometry[part.ElementId];
                if (rootX > 64 && rootX + size.Width > 32_000)
                {
                    rootX = 64;
                    rootY += rootHeight + 50;
                    rootHeight = 0;
                }

                geometry[part.ElementId] = size with { X = rootX, Y = rootY };
                rootX += size.Width + 40;
                rootHeight = Math.Max(rootHeight, size.Height);
            }

            var occurrences = new Dictionary<string, DiagramOccurrence>(StringComparer.Ordinal);
            foreach (var item in ordered)
            {
                var parentId = this.Metadata.Parts[item.Id].ParentElementId;
                var parent = parentId is null ? null : occurrences[parentId];
                var size = geometry[item.Id];
                occurrences.Add(item.Id, new DiagramOccurrence(Guid.NewGuid(), item.Id,
                    size with { X = size.X + (parent?.Geometry.X ?? 0), Y = size.Y + (parent?.Geometry.Y ?? 0) },
                    parent?.Id));
            }

            return new DiagramPresentationState(1, Guid.NewGuid(), this.Metadata.ModelScope, [.. occurrences.Values],
            [
                .. this.Metadata.Connections.Values.Select(connection => new DiagramRoute(Guid.NewGuid(),
                    connection.ElementId,
                    occurrences[connection.SourcePartId].Id, connection.SourcePortId,
                    occurrences[connection.TargetPartId].Id, connection.TargetPortId, []))
            ], new DiagramViewport(0, 0, 1));
        }

        /// <summary>Releases tab-owned formatted details and canonical references without clearing the SDK owner.</summary>
        public void Dispose()
        {
            this.details.Dispose();
            this.Elements.Clear();
        }
    }
}