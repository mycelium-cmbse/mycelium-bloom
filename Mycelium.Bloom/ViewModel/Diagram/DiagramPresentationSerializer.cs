// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramPresentationSerializer.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram
{
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using Mycelium.Bloom.Model.Diagram;

    /// <summary>Validates prototype presentation snapshots against their canonical projection.</summary>
    public static class DiagramPresentationSerializer
    {
        /// <summary>Defines deterministic, strict presentation JSON conventions.</summary>
        private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            MaxDepth = 16
        };

        /// <summary>Exports presentation only, with no semantic SDK objects.</summary>
        /// <param name="state">The coherent tab-owned snapshot.</param>
        /// <returns>The versioned JSON representation.</returns>
        public static string Export(DiagramPresentationState state)
        {
            using var trace = Mycelium.Bloom.Core.DiagramDiagnostics.Source.StartActivity("serialize-presentation");
            return JsonSerializer.Serialize(state, Options);
        }

        /// <summary>Parses and validates a bounded snapshot without mutating its owner.</summary>
        /// <param name="json">The imported presentation JSON.</param>
        /// <param name="projection">The current canonical model scope.</param>
        /// <param name="state">The validated snapshot, or null.</param>
        /// <param name="error">The actionable validation error.</param>
        /// <param name="limits">The tab's capacity, or ordinary limits when omitted.</param>
        /// <returns>Whether the snapshot is valid for the current model.</returns>
        public static bool TryImport(string json, DiagramProjection projection, out DiagramPresentationState state,
            out string error,
            DiagramPresentationLimits limits = null)
        {
            limits ??= DiagramPresentationLimits.Default;
            state = null;
            error = "Invalid presentation JSON.";

            if (string.IsNullOrWhiteSpace(json) || System.Text.Encoding.UTF8.GetByteCount(json) > limits.JsonBytes)
            {
                error = $"Choose a presentation JSON file smaller than {limits.JsonBytes / 1_048_576} MiB.";
                return false;
            }

            try
            {
                var candidate = JsonSerializer.Deserialize<DiagramPresentationState>(json, Options);

                if (!Validate(candidate, projection, out error, limits))
                {
                    return false;
                }

                state = candidate;
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>Checks identity, scope, endpoints, geometry and nesting before publication.</summary>
        /// <param name="state">The candidate snapshot.</param>
        /// <param name="projection">The canonical model projection.</param>
        /// <param name="error">The first validation error.</param>
        /// <param name="limits">The tab's capacity, or ordinary limits when omitted.</param>
        /// <returns>Whether the snapshot can be restored safely.</returns>
        public static bool Validate(DiagramPresentationState state, DiagramProjection projection, out string error,
            DiagramPresentationLimits limits = null)
        {
            using var trace = Mycelium.Bloom.Core.DiagramDiagnostics.Source.StartActivity("validate-presentation");
            limits ??= DiagramPresentationLimits.Default;
            error = "Unsupported version, model scope, or presentation size.";

            if (state is null || state.Version != 1 || state.DiagramId == Guid.Empty
                || state.ModelScope != projection.ModelScope
                || state.Occurrences.IsDefault || state.Routes.IsDefault
                || state.Occurrences.Length > limits.Nodes || state.Routes.Length > limits.Routes
                || state.Viewport is not { Zoom: <= 4 } viewport || !double.IsFinite(viewport.Zoom) ||
                viewport.Zoom < limits.MinimumZoom
                || !double.IsFinite(viewport.X) || !double.IsFinite(viewport.Y)
                || Math.Abs(viewport.X) > limits.Pan || Math.Abs(viewport.Y) > limits.Pan)
            {
                return false;
            }

            var nodes = new Dictionary<Guid, DiagramOccurrence>();
            var ids = new HashSet<Guid>();
            error = "Invalid or duplicate occurrence identity, semantic element, or geometry.";

            foreach (var node in state.Occurrences)
            {
                if (node is null || node.Id == Guid.Empty || !ids.Add(node.Id)
                    || node.ElementId is null || !projection.Parts.ContainsKey(node.ElementId)
                    || node.Geometry is not { Width: >= 160, Height: >= 100 } geometry
                    || !double.IsFinite(geometry.Width) || !double.IsFinite(geometry.Height)
                    || geometry.Width > limits.Dimension || geometry.Height > limits.Dimension
                    || !Coordinate(geometry.X) || !Coordinate(geometry.Y))
                {
                    return false;
                }

                nodes.Add(node.Id, node);
            }

            error = "Invalid containment, cyclic parents, or excessive nesting depth.";

            foreach (var node in nodes.Values.Where(node => node.ParentId.HasValue))
            {
                if (!nodes.TryGetValue(node.ParentId.Value, out var parent)
                    || parent.Id == node.Id || projection.Parts[node.ElementId].ParentElementId != parent.ElementId
                    || !Contains(parent.Geometry, node.Geometry))
                {
                    return false;
                }

                var ancestor = parent;
                var depth = 1;
                while (ancestor.ParentId is { } parentId)
                {
                    if (++depth > limits.NestingDepth || !nodes.TryGetValue(parentId, out ancestor))
                    {
                        return false;
                    }
                }
            }

            error = "Invalid connection identity, canonical endpoints, or manual route.";

            foreach (var route in state.Routes)
            {
                if (route is null || route.Id == Guid.Empty || !ids.Add(route.Id)
                    || route.ElementId is null ||
                    !projection.Connections.TryGetValue(route.ElementId, out var connection)
                    || !nodes.TryGetValue(route.SourceId, out var source) ||
                    !nodes.TryGetValue(route.TargetId, out var target)
                    || source.ElementId != connection.SourcePartId || target.ElementId != connection.TargetPartId
                    || route.SourcePortId != connection.SourcePortId || route.TargetPortId != connection.TargetPortId
                    || route.Bends.IsDefault || route.Bends.Length > 32
                    || route.Bends.Any(point => point is null || !Coordinate(point.X) || !Coordinate(point.Y)))
                {
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        /// <summary>Bounds finite coordinates to a useful prototype workspace.</summary>
        private static bool Coordinate(double value) => double.IsFinite(value) && Math.Abs(value) <= 100_000;

        /// <summary>Checks that the child fits below the container's compartments.</summary>
        private static bool Contains(DiagramGeometry parent, DiagramGeometry child) =>
            child.X >= parent.X + 8 && child.Y >= parent.Y + 100
                                    && child.X + child.Width <= parent.X + parent.Width - 8
                                    && child.Y + child.Height <= parent.Y + parent.Height - 8;
    }
}