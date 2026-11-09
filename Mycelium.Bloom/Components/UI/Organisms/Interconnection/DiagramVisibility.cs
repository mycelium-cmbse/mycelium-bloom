// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramVisibility.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Interconnection
{
    using Blazor.Diagrams;
    using Blazor.Diagrams.Core.Geometry;
    using Blazor.Diagrams.Core.Models;
    using Blazor.Diagrams.Core.Models.Base;

    /// <summary>Uses native visibility and model-space bounds without requiring mounted SVG measurements.</summary>
    internal sealed class DiagramVisibility : IDisposable
    {
        /// <summary>Owns the external graph whose native visibility is projected.</summary>
        private readonly BlazorDiagram diagram;
        /// <summary>Caches external model bounds, including unmounted elements.</summary>
        private readonly Dictionary<Model, Rectangle> bounds = [];
        /// <summary>Controls screen-space mounting hysteresis.</summary>
        private readonly double margin;
        /// <summary>Rejects notifications after the graph lifetime ends.</summary>
        private bool disposed;
        /// <summary>Caches complete bounds until geometry changes.</summary>
        private Rectangle sceneBounds;

        /// <summary>Tracks the bounds of one completed rendering graph.</summary>
        public DiagramVisibility(BlazorDiagram diagram, double margin)
        {
            this.diagram = diagram;
            this.margin = margin;
            foreach (var model in diagram.OrderedSelectables)
            {
                this.UpdateBounds(model);
                model.Changed += this.UpdateBounds;
            }
        }

        /// <summary>Gets the complete renderer bounds independently of visibility.</summary>
        public Rectangle Bounds => this.sceneBounds ??= this.bounds.Values.DefaultIfEmpty(Rectangle.Zero).Aggregate((left, right) => left.Union(right));

        /// <summary>Updates native visibility with hysteresis and preserves the active interaction.</summary>
        /// <returns>The number of nodes intersecting the viewport without its mounting margin.</returns>
        public int Apply(bool interactionActive)
        {
            if (this.disposed || this.diagram.Container is null)
            {
                return 0;
            }

            var viewport = new Rectangle(-this.diagram.Pan.X / this.diagram.Zoom, -this.diagram.Pan.Y / this.diagram.Zoom,
                (this.diagram.Container.Width - this.diagram.Pan.X) / this.diagram.Zoom,
                (this.diagram.Container.Height - this.diagram.Pan.Y) / this.diagram.Zoom);
            var near = viewport.Inflate(this.margin / this.diagram.Zoom, this.margin / this.diagram.Zoom);
            var retained = viewport.Inflate(this.margin * 2 / this.diagram.Zoom, this.margin * 2 / this.diagram.Zoom);
            var count = 0;
            foreach (var (model, box) in this.bounds)
            {
                if (model is NodeModel && Intersects(box, viewport))
                {
                    count++;
                }
                var pinned = interactionActive && model is SelectableModel { Selected: true };
                model.Visible = pinned || Intersects(box, model.Visible ? retained : near);
            }
            foreach (var node in this.diagram.Nodes.Where(node => node.Visible))
            {
                for (var parent = node.Group; parent is not null; parent = parent.Group)
                {
                    parent.Visible = true;
                }
            }
            return count;
        }

        /// <summary>Tests inclusive bounds, including horizontal and vertical connections.</summary>
        internal static bool Intersects(Rectangle left, Rectangle right) =>
            left.Right >= right.Left && left.Left <= right.Right && left.Bottom >= right.Top && left.Top <= right.Bottom;

        /// <summary>Updates only the changed external model geometry.</summary>
        private void UpdateBounds(Model model)
        {
            if (!this.disposed && model is IHasBounds bounded && bounded.GetBounds() is { } rectangle)
            {
                this.sceneBounds = null;
                this.bounds[model] = model is DiagramLinkModel link
                    ? rectangle.Inflate(Math.Max(24, link.Connection.Name.Length * 3 + 10), 24) : rectangle;
            }
        }

        /// <summary>Detaches model notifications without changing authoritative presentation.</summary>
        public void Dispose()
        {
            this.disposed = true;
            foreach (var model in this.diagram.OrderedSelectables)
            {
                model.Changed -= this.UpdateBounds;
            }
            this.bounds.Clear();
        }
    }
}
