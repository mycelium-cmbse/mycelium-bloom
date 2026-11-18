// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramLinkModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram
{
    using Blazor.Diagrams.Core.Anchors;
    using Blazor.Diagrams.Core.Geometry;
    using Blazor.Diagrams.Core.Models;
    using Blazor.Diagrams.Core.PathGenerators;

    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.ViewModel.Diagram;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>Adapts one graphical relationship to native anchors and bends.</summary>
    public sealed class DiagramLinkModel : LinkModel
    {
        /// <summary>Gets the tab-owned reactive relationship presentation borrowed by this native link.</summary>
        public DiagramEdgeViewModel ViewModel { get; }

        /// <summary>Gets a mount-unique prefix for SVG marker identities.</summary>
        public string MarkerPrefix { get; } = $"diagram-marker-{Guid.NewGuid():N}";

        /// <summary>Gets whether canonical shared selection refers to this exact semantic object.</summary>
        public bool IsSelected => this.ViewModel.IsSelected;

        /// <summary>Gets the immutable notation borrowed by this native model.</summary>
        public DiagramEdgePresentation Presentation => this.ViewModel.Presentation;

        /// <summary>Creates the native adapter over borrowed presentation and caller-owned state.</summary>
        /// <param name="presentation">The immutable occurrence or relationship notation.</param>
        /// <param name="source">The native source occurrence.</param>
        /// <param name="target">The native target occurrence.</param>
        /// <param name="owner">The caller-owned presentation authority.</param>
        public DiagramLinkModel(DiagramEdgePresentation presentation, NodeModel source, NodeModel target, IDiagramViewModel owner)
            : base(presentation.Id.ToString(), CreateAnchor(source, presentation.SourceFeature), CreateAnchor(target, presentation.TargetFeature))
        {
            this.ViewModel = owner.GetEdgeViewModel(presentation.Id);
            this.PathGenerator = new StraightPathGenerator();

            if (!presentation.Bends.IsDefaultOrEmpty)
            {
                foreach (var bend in presentation.Bends)
                {
                    this.AddVertex(new Point(bend.X, bend.Y));
                }
            }
        }

        /// <summary>Locates a canonical boundary feature or intersects the node shape.</summary>
        /// <param name="node">The native occurrence whose boundary is queried.</param>
        /// <param name="feature">The canonical boundary feature, or null for the node shape.</param>
        /// <returns>The native boundary anchor or shape intersection anchor.</returns>
        private static Anchor CreateAnchor(NodeModel node, IElement feature)
        {
            var occurrence = (DiagramNodeModel)node;

            for (var index = 0; index < occurrence.ViewModel.BoundaryFeatures.Length; index++)
            {
                if (ReferenceEquals(occurrence.ViewModel.BoundaryFeatures[index].Element, feature))
                {
                    return new SinglePortAnchor(node.Ports[index]);
                }
            }

            return new ShapeIntersectionAnchor(node);
        }
    }
}
