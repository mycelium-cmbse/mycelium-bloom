// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramRendererModels.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Interconnection
{
    using Blazor.Diagrams.Core;
    using Blazor.Diagrams.Core.Anchors;
    using Blazor.Diagrams.Core.Geometry;
    using Blazor.Diagrams.Core.Models;
    using Blazor.Diagrams.Core.Models.Base;
    using Blazor.Diagrams.Core.Routers;
    using Blazor.Diagrams.Models;
    using Mycelium.Bloom.Model.Diagram;

    /// <summary>Adapts a tab-owned occurrence to a mounted native SVG node.</summary>
    internal sealed class DiagramNodeModel : SvgNodeModel
    {
        /// <summary>Creates one disposable rendering projection.</summary>
        /// <param name="occurrence">The tab-owned identity and geometry.</param>
        /// <param name="part">The canonical element's immutable presentation.</param>
        public DiagramNodeModel(DiagramOccurrence occurrence, DiagramPart part)
            : base(occurrence.Id.ToString(), new Point(occurrence.Geometry.X, occurrence.Geometry.Y))
        {
            this.Part = part;
            this.ControlledSize = true;
            this.Size = new Size(occurrence.Geometry.Width, occurrence.Geometry.Height);
        }

        /// <summary>Gets the immutable semantic presentation.</summary>
        public DiagramPart Part { get; }
    }

    /// <summary>Renders a fixed-size nested container while the library moves its children.</summary>
    internal sealed class DiagramGroupModel : SvgGroupModel
    {
        /// <summary>Creates a container with absolute model-space child geometry.</summary>
        /// <param name="occurrence">The container occurrence and retained bounds.</param>
        /// <param name="part">The canonical container's immutable presentation.</param>
        /// <param name="children">The mounted child nodes moved with this container.</param>
        public DiagramGroupModel(DiagramOccurrence occurrence, DiagramPart part, IEnumerable<NodeModel> children)
            : base(children, autoSize: false)
        {
            this.OccurrenceId = occurrence.Id;
            this.Part = part;
            this.Position = new Point(occurrence.Geometry.X, occurrence.Geometry.Y);
            this.Size = new Size(occurrence.Geometry.Width, occurrence.Geometry.Height);
            this.ControlledSize = true;
        }

        /// <summary>Gets the tab-owned graphical identity.</summary>
        public Guid OccurrenceId { get; }

        /// <summary>Gets the semantic presentation.</summary>
        public DiagramPart Part { get; }

        /// <summary>Retains the presentation owner's explicit container bounds.</summary>
        /// <returns>False because automatic child-based sizing is disabled.</returns>
        protected override bool UpdateDimensions() => false;
    }

    /// <summary>Adds immutable semantic notation to a mounted connection.</summary>
    internal sealed class DiagramLinkModel : LinkModel
    {
        /// <summary>Creates a connection occurrence between existing boundary ports.</summary>
        /// <param name="route">The occurrence identity and manual bends.</param>
        /// <param name="connection">The canonical relationship's notation.</param>
        /// <param name="source">The source occurrence's boundary port.</param>
        /// <param name="target">The target occurrence's boundary port.</param>
        public DiagramLinkModel(DiagramRoute route, DiagramConnection connection, PortModel source, PortModel target)
            : base(route.Id.ToString(), source, target)
        {
            this.Connection = connection;
            this.Router = new PresentationRouter();
            foreach (var point in route.Bends)
            {
                this.AddVertex(new Point(point.X, point.Y));
            }
        }

        /// <summary>Gets the supported canonical relationship notation.</summary>
        public DiagramConnection Connection { get; }
    }

    /// <summary>Uses the released endpoint router unless manual waypoints explicitly override it.</summary>
    internal sealed class PresentationRouter : Router
    {
        /// <summary>Provides the library's endpoint-only automatic routing.</summary>
        private readonly OrthogonalRouter automatic = new();

        /// <summary>Expands manual waypoints into axis-aligned segments without obstacle-avoidance claims.</summary>
        /// <param name="diagram">The mounted library graph.</param>
        /// <param name="link">The connection whose port geometry and bends determine routing.</param>
        /// <returns>The route points excluding endpoint anchors.</returns>
        public override Point[] GetRoute(Diagram diagram, BaseLinkModel link)
        {
            if (link.Vertices.Count == 0)
            {
                return this.automatic.GetRoute(diagram, link);
            }

            var source = ((SinglePortAnchor)link.Source).Port.MiddlePosition;
            var target = ((SinglePortAnchor)link.Target).Port.MiddlePosition;
            var points = new List<Point>();
            var previous = source;

            foreach (var vertex in link.Vertices)
            {
                points.Add(new Point(vertex.Position.X, previous.Y));
                points.Add(vertex.Position);
                previous = vertex.Position;
            }

            points.Add(new Point(previous.X, target.Y));
            return points.Where((point, index) => index == 0 || !point.Equals(points[index - 1])).ToArray();
        }
    }
}