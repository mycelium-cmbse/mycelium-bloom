// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramNodeModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram
{
    using Blazor.Diagrams.Core.Geometry;
    using Blazor.Diagrams.Core.Models;

    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.Diagram;

    /// <summary>Adapts one graphical occurrence and its boundaries to native renderer geometry.</summary>
    public sealed class DiagramNodeModel : NodeModel
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
        public DiagramNodeModel(DiagramNodePresentation presentation, IDiagramViewModel owner)
            : base(presentation.Occurrence.Id.ToString(), new Point(presentation.Occurrence.X, presentation.Occurrence.Y))
        {
            this.ViewModel = owner.GetNodeViewModel(presentation.Occurrence.Id);
            this.Position = new Point(this.ViewModel.Position.X, this.ViewModel.Position.Y);
            this.Locked = !presentation.Occurrence.IsMovable;
            this.Size = new Size(this.ViewModel.Size.Width, this.ViewModel.Size.Height);
            this.ControlledSize = true;

            for (var index = 0; index < this.ViewModel.BoundaryFeatures.Length; index++)
            {
                var boundary = this.ViewModel.BoundaryFeatures[index];
                var feature = boundary.Presentation;
                var alignment = feature.Side switch
                {
                    BoundarySide.Left => PortAlignment.Left,
                    BoundarySide.Top => PortAlignment.Top,
                    BoundarySide.Bottom => PortAlignment.Bottom,
                    _ => PortAlignment.Right
                };
                this.AddPort(new DiagramBoundaryPortModel($"{this.Id}-boundary-{index}", this, alignment,
                    this.GetBoundaryPosition(feature), new Size(12, 12), boundary));
            }
        }

        /// <summary>Synchronizes native bounds and boundary anchors with retained occurrence dimensions.</summary>
        /// <param name="size">The tab-owned model-space dimensions.</param>
        public void ApplySize(DiagramSize size)
        {
            if ((this.Size.Width, this.Size.Height).Equals((size.Width, size.Height)))
            {
                return;
            }

            this.Size = new Size(size.Width, size.Height);

            foreach (var port in this.Ports)
            {
                if (port is IDiagramBoundaryPortModel boundary)
                {
                    port.Position = this.GetBoundaryPosition(boundary.ViewModel.Presentation);
                    port.RefreshAll();
                }
            }

            this.Refresh();
            this.RefreshLinks();
        }

        /// <summary>Locates a boundary symbol relative to the current native rectangle.</summary>
        /// <param name="feature">The boundary's retained side and fractional offset.</param>
        /// <returns>The model-space origin of the twelve-pixel boundary symbol.</returns>
        private Point GetBoundaryPosition(DiagramBoundaryPresentation feature)
        {
            var x = feature.Side switch { BoundarySide.Left => 0, BoundarySide.Right => this.Size.Width, _ => this.Size.Width * feature.Offset };
            var y = feature.Side switch { BoundarySide.Top => 0, BoundarySide.Bottom => this.Size.Height, _ => this.Size.Height * feature.Offset };
            return new Point(this.Position.X + x - 6, this.Position.Y + y - 6);
        }
    }
}
