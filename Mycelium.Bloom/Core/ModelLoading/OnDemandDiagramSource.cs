// ------------------------------------------------------------------------------------------------
// <copyright file="OnDemandDiagramSource.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ModelLoading
{
    using System.Collections.Immutable;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using Mycelium.Bloom.Model.Diagram;

    /// <summary>Generates complete SDK fixture bundles only when requested, separately from lightweight scene metadata.</summary>
    public sealed class OnDemandDiagramSource : IDiagramModelSource
    {
        private readonly string template;
        private readonly string[] identifiers;
        private readonly Dictionary<string, int> tiles = new(StringComparer.Ordinal);
        private long reads;
        private long bytes;

        /// <summary>Builds a metadata-only index of distinct deterministic fixture elements.</summary>
        /// <param name="template">The small SDK JSON fixture template.</param>
        /// <param name="projection">The template's canonical projection.</param>
        /// <param name="presentation">The template geometry.</param>
        /// <param name="tileCount">The number of independent dependency bundles.</param>
        public OnDemandDiagramSource(string template, DiagramProjection projection,
            DiagramPresentationState presentation, int tileCount)
        {
            if (tileCount is < 1 or > 1428)
            {
                throw new ArgumentOutOfRangeException(nameof(tileCount));
            }

            this.template = template;
            this.identifiers = Regex.Matches(template, "[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}")
                .Select(match => match.Value).Distinct(StringComparer.Ordinal).ToArray();
            var revision = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(template)));
            this.Scope = new DiagramReadScope("generated-sdk-fixture", "interconnection", revision,
                "local-diagnostic-session");
            var parts = ImmutableDictionary.CreateBuilder<string, DiagramPart>(StringComparer.Ordinal);
            var connections = ImmutableDictionary.CreateBuilder<string, DiagramConnection>(StringComparer.Ordinal);
            var bundles = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
            var nodes = ImmutableArray.CreateBuilder<DiagramOccurrence>();
            var routes = ImmutableArray.CreateBuilder<DiagramRoute>();
            for (var tile = 0; tile < tileCount; tile++)
            {
                var current = tile;
                string Map(string id) => id is null ? null : this.MapIdentity(current, id);
                var root = Map(projection.ModelScope);
                this.tiles.Add(root, tile);
                bundles.Add(root, root);
                foreach (var part in projection.Parts.Values)
                {
                    var id = Map(part.ElementId);
                    parts.Add(id, new DiagramPart(id, string.Empty, Map(part.ParentElementId), [],
                    [
                        .. part.Ports.Select(port => new DiagramPort(Map(port.ElementId), string.Empty, port.Direction))
                    ]));
                    bundles.Add(id, root);
                    foreach (var port in part.Ports)
                    {
                        bundles.Add(Map(port.ElementId), root);
                    }
                }

                foreach (var connection in projection.Connections.Values)
                {
                    var id = Map(connection.ElementId);
                    connections.Add(id, new DiagramConnection(id, string.Empty, Map(connection.SourcePartId),
                        Map(connection.SourcePortId),
                        Map(connection.TargetPartId), Map(connection.TargetPortId), null, null));
                    bundles.Add(id, root);
                }

                var occurrences = presentation.Occurrences.ToDictionary(node => node.Id,
                    node => Guid.Parse(Map("occurrence-" + node.ElementId)));
                var dx = tile % 40 * 1000d;
                var dy = tile / 40 * 850d;
                nodes.AddRange(presentation.Occurrences.Select(node => node with
                {
                    Id = occurrences[node.Id],
                    ElementId = Map(node.ElementId),
                    ParentId = node.ParentId.HasValue ? occurrences[node.ParentId.Value] : null,
                    Geometry = node.Geometry with { X = node.Geometry.X + dx, Y = node.Geometry.Y + dy }
                }));
                routes.AddRange(presentation.Routes.Select(route => route with
                {
                    Id = Guid.Parse(Map("occurrence-" + route.ElementId)),
                    ElementId = Map(route.ElementId),
                    SourceId = occurrences[route.SourceId],
                    TargetId = occurrences[route.TargetId],
                    SourcePortId = Map(route.SourcePortId),
                    TargetPortId = Map(route.TargetPortId),
                    Bends = [.. route.Bends.Select(point => new DiagramPoint(point.X + dx, point.Y + dy))]
                }));
            }

            this.Metadata = new DiagramProjection("on-demand-" + revision, parts.ToImmutable(),
                connections.ToImmutable(), projection.Diagnostics);
            this.Presentation = new DiagramPresentationState(1, Guid.NewGuid(), this.Metadata.ModelScope,
                nodes.ToImmutable(), routes.ToImmutable(), presentation.Viewport);
            this.Bundles = bundles.ToImmutable();
        }

        /// <summary>Gets the fixture revision and diagnostic-only authorization scope.</summary>
        public DiagramReadScope Scope { get; }

        /// <summary>Gets identity and geometry dependencies without part details.</summary>
        public DiagramProjection Metadata { get; }

        /// <summary>Gets full-scene presentation metadata.</summary>
        public DiagramPresentationState Presentation { get; }

        /// <summary>Gets canonical bundle membership.</summary>
        public ImmutableDictionary<string, string> Bundles { get; }

        /// <summary>Gets generated bundle reads, not network requests.</summary>
        public long Reads => Interlocked.Read(ref this.reads);

        /// <summary>Gets generated UTF-8 payload bytes, not network traffic.</summary>
        public long Bytes => Interlocked.Read(ref this.bytes);

        /// <summary>Restricts this fixture to its exact immutable diagnostic scope.</summary>
        /// <param name="scope">The requested scope.</param>
        /// <param name="cancellationToken">The caller lifetime.</param>
        /// <returns>A completed access check.</returns>
        public ValueTask AuthorizeAsync(DiagramReadScope scope, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (scope != this.Scope)
            {
                throw new UnauthorizedAccessException("The model read scope is unavailable.");
            }

            return ValueTask.CompletedTask;
        }

        /// <summary>Constructs requested complete dependency payloads without allocating unrequested SDK objects.</summary>
        /// <param name="scope">The immutable fixture scope.</param>
        /// <param name="bundleIds">Requested root identifiers.</param>
        /// <param name="cancellationToken">The session lifetime.</param>
        /// <returns>The requested SDK JSON payloads.</returns>
        public async Task<IReadOnlyDictionary<string, string>> FetchAsync(DiagramReadScope scope,
            IReadOnlyList<string> bundleIds, CancellationToken cancellationToken)
        {
            await this.AuthorizeAsync(scope, cancellationToken);
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var id in bundleIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!this.tiles.TryGetValue(id, out var tile))
                {
                    throw new InvalidDataException("Unknown fixture bundle.");
                }

                var payload = this.template;
                foreach (var original in this.identifiers)
                {
                    payload = payload.Replace(original, this.MapIdentity(tile, original), StringComparison.Ordinal);
                }

                result.Add(id, payload);
                Interlocked.Increment(ref this.reads);
                Interlocked.Add(ref this.bytes, Encoding.UTF8.GetByteCount(payload));
            }

            return result;
        }

        private string MapIdentity(int tile, string original) => new Guid(SHA256.HashData(
                Encoding.UTF8.GetBytes($"diagram-fixture/{this.Scope.Revision}/{tile}/{original}")).AsSpan(0, 16))
            .ToString();
    }
}