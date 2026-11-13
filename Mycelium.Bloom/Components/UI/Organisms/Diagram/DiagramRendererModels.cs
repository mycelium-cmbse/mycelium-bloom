// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramRendererModels.cs" company="Starion Group S.A.">
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

    /// <summary>Adapts one graphical occurrence and its boundaries to native renderer geometry.</summary>
    public sealed class GeneralDiagramNodeModel : NodeModel
    {
        /// <summary>Gets the tab-owned reactive presentation borrowed by this native occurrence.</summary>
        public DiagramNodeViewModel ViewModel { get; }

        /// <summary>Gets the immutable notation borrowed by this native model.</summary>
        public DiagramNodePresentation Presentation => this.ViewModel.Presentation;

        /// <summary>Gets whether canonical shared selection refers to this exact semantic object.</summary>
        public bool IsSelected => this.ViewModel.IsSelected;

        /// <summary>Creates the native adapter over borrowed presentation and caller-owned state.</summary>
        /// <param name="presentation">The immutable occurrence or relationship notation.</param>
        /// <param name="owner">The caller-owned presentation authority.</param>
        public GeneralDiagramNodeModel(DiagramNodePresentation presentation, IGeneralDiagramViewModel owner)
            : base(presentation.Occurrence.Id.ToString(), new Point(presentation.Occurrence.X, presentation.Occurrence.Y))
        {
            this.ViewModel = owner.GetNodeViewModel(presentation.Occurrence.Id);
            this.Size = new Size(presentation.Occurrence.Width, presentation.Occurrence.Height);
            this.ControlledSize = true;

            for (var index = 0; index < presentation.Boundary.Length; index++)
            {
                var feature = presentation.Boundary[index];
                var alignment = feature.Side switch
                {
                    BoundarySide.Left => PortAlignment.Left,
                    BoundarySide.Top => PortAlignment.Top,
                    BoundarySide.Bottom => PortAlignment.Bottom,
                    _ => PortAlignment.Right
                };
                var offset = feature.Offset;
                var x = feature.Side switch { BoundarySide.Left => 0, BoundarySide.Right => this.Size.Width, _ => this.Size.Width * offset };
                var y = feature.Side switch { BoundarySide.Top => 0, BoundarySide.Bottom => this.Size.Height, _ => this.Size.Height * offset };
                this.AddPort(new PortModel($"{this.Id}-boundary-{index}", this, alignment,
                    new Point(this.Position.X + x - 6, this.Position.Y + y - 6), new Size(12, 12))
                { Initialized = true });
            }
        }
    }

    /// <summary>Adapts one graphical relationship to native anchors and bends.</summary>
    public sealed class GeneralDiagramLinkModel : LinkModel
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
        public GeneralDiagramLinkModel(DiagramEdgePresentation presentation, NodeModel source, NodeModel target, IGeneralDiagramViewModel owner)
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
            var occurrence = (GeneralDiagramNodeModel)node;

            for (var index = 0; index < occurrence.Presentation.Boundary.Length; index++)
            {
                if (ReferenceEquals(occurrence.Presentation.Boundary[index].Element, feature))
                {
                    return new SinglePortAnchor(node.Ports[index]);
                }
            }

            return new ShapeIntersectionAnchor(node);
        }
    }
}
