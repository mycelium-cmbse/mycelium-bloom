// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramProjectionValidator.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram
{
    using System.Collections.Immutable;

    using Mycelium.Bloom.Model.Diagram;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>Validates reusable graphical occurrence, endpoint and geometry contracts.</summary>
    public static class DiagramProjectionValidator
    {
        /// <summary>Rejects invalid graphical contracts rather than silently dropping edges or producing invalid SVG.</summary>
        /// <param name="projection">The presentation to validate independently of semantic ElementIds.</param>
        /// <exception cref="ArgumentException">Graphical identities, endpoints, boundaries, or geometry are invalid.</exception>
        public static void Validate(DiagramProjection projection)
        {
            ArgumentNullException.ThrowIfNull(projection);

            if (projection.Nodes.IsDefault || projection.Edges.IsDefault)
            {
                throw new ArgumentException("Diagram collections must be initialized.", nameof(projection));
            }

            var ids = new HashSet<Guid>();

            ValidateNodes(projection, ids);

            var edgeIds = new HashSet<Guid>();

            ValidateEdges(projection, edgeIds, ids);
            ValidateEndpointFeatures(projection);
        }

        /// <summary>Rejects boundary associations that do not belong to the selected graphical endpoint.</summary>
        /// <param name="projection">The presentation with already validated occurrence endpoints.</param>
        private static void ValidateEndpointFeatures(DiagramProjection projection)
        {
            var nodes = projection.Nodes.ToDictionary(node => node.Occurrence.Id);

            if (projection.Edges.Any(edge => !IsEndpointFeature(nodes[edge.Source], edge.SourceFeature)
                || !IsEndpointFeature(nodes[edge.Target], edge.TargetFeature)))
            {
                throw new ArgumentException("A diagram edge feature must be the canonical endpoint element or one of its boundary features.", nameof(projection));
            }
        }

        /// <summary>Gets boundary notation only for node types that support shell boundaries.</summary>
        /// <param name="node">The typed node presentation.</param>
        /// <returns>The shell's boundaries, or an empty collection for other node types.</returns>
        private static ImmutableArray<DiagramBoundaryPresentation> Boundaries(DiagramNodePresentation node) => node is DiagramShellPresentation shell ? shell.Boundary : [];

        /// <summary>Checks the canonical association without confusing equal semantic IDs across models.</summary>
        /// <param name="node">The graphical endpoint occurrence.</param>
        /// <param name="feature">The optional canonical element or boundary feature.</param>
        /// <returns>Whether the association belongs to this exact occurrence.</returns>
        private static bool IsEndpointFeature(DiagramNodePresentation node, IElement feature)
        {
            return feature is null || ReferenceEquals(node.Occurrence.Element, feature)
                || Boundaries(node).Any(boundary => ReferenceEquals(boundary.Element, feature));
        }

        /// <summary>Checks that SVG dimensions and coordinates are finite and positive where required.</summary>
        /// <param name="occurrence">The graphical occurrence geometry.</param>
        /// <returns>Whether the occurrence can be rendered as finite SVG geometry.</returns>
        private static bool HasValidGeometry(DiagramOccurrence occurrence)
        {
            return double.IsFinite(occurrence.X) && double.IsFinite(occurrence.Y)
                && double.IsFinite(occurrence.Width) && double.IsFinite(occurrence.Height) && occurrence.Width > 0 && occurrence.Height > 0;
        }

        /// <summary>Rejects ambiguous occurrence identities, invalid geometry and malformed boundary placement.</summary>
        /// <param name="projection">The immutable presentation being validated.</param>
        /// <param name="ids">Receives unique graphical occurrence identities.</param>
        private static void ValidateNodes(DiagramProjection projection, HashSet<Guid> ids)
        {
            foreach (var node in projection.Nodes)
            {
                var occurrence = node.Occurrence;

                if (occurrence.Id == Guid.Empty || occurrence.Element is null || !ids.Add(occurrence.Id)
                    || !HasValidGeometry(occurrence)
                    || node is DiagramShellPresentation shell && (shell.Boundary.IsDefault || shell.Compartments.IsDefault
                        || shell.Boundary.Any(feature => feature.Element is null || !double.IsFinite(feature.Offset) || feature.Offset < 0 || feature.Offset > 1)))
                {
                    throw new ArgumentException("Diagram occurrences require unique graphical identities and finite positive geometry with normalized boundaries.",
                        nameof(projection));
                }
            }
        }

        /// <summary>Rejects missing endpoints, ambiguous edge identities and invalid polyline geometry.</summary>
        /// <param name="projection">The immutable presentation being validated.</param>
        /// <param name="edgeIds">Receives unique graphical edge identities.</param>
        /// <param name="ids">The validated graphical node identities.</param>
        private static void ValidateEdges(DiagramProjection projection, HashSet<Guid> edgeIds, HashSet<Guid> ids)
        {
            foreach (var edge in projection.Edges)
            {
                if (edge.Id == Guid.Empty || edge.Element is null || !edgeIds.Add(edge.Id) || !ids.Contains(edge.Source) || !ids.Contains(edge.Target)
                    || edge.Notation is null || string.IsNullOrWhiteSpace(edge.Notation.Key) || edge.SourceMarker is null || edge.TargetMarker is null
                    || !HasValidMarkerGeometry(edge.SourceMarker) || !HasValidMarkerGeometry(edge.TargetMarker) || !double.IsFinite(edge.Notation.LabelOffset)
                    || !edge.Bends.IsDefaultOrEmpty && edge.Bends.Any(point => !double.IsFinite(point.X) || !double.IsFinite(point.Y)))
                {
                    throw new ArgumentException("Diagram edges require unique graphical identities, represented endpoints, finite markers and bends.", nameof(projection));
                }
            }
        }

        /// <summary>Checks finite marker bounds and positive SVG viewport dimensions before rendering.</summary>
        /// <param name="marker">The non-null marker descriptor.</param>
        /// <returns>Whether the marker can define a finite positive SVG viewport.</returns>
        private static bool HasValidMarkerGeometry(DiagramMarker marker)
        {
            var bounds = marker.ViewBox;

            return double.IsFinite(bounds.X) && double.IsFinite(bounds.Y) && double.IsFinite(bounds.Width) && double.IsFinite(bounds.Height)
                && bounds.Width > 0 && bounds.Height > 0 && double.IsFinite(bounds.X + bounds.Width) && double.IsFinite(bounds.Y + bounds.Height);
        }
    }
}
