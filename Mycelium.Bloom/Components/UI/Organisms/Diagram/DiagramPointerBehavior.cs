// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramPointerBehavior.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram
{
    using Blazor.Diagrams.Core;
    using Blazor.Diagrams.Core.Behaviors;
    using Blazor.Diagrams.Core.Events;
    using Blazor.Diagrams.Core.Geometry;
    using Blazor.Diagrams.Core.Models.Base;

    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.Diagram;

    /// <summary>Routes mode-aware navigation and selection while native behavior owns node dragging.</summary>
    public sealed class DiagramPointerBehavior : Behavior
    {
        /// <summary>The tab-owned mode and policy authority.</summary>
        private readonly IDiagramViewModel owner;

        /// <summary>The pointer owned by this mount's current gesture.</summary>
        private long? pointerId;

        /// <summary>The last screen-space pan coordinate.</summary>
        private Point panPoint;

        /// <summary>Prevents late browser callbacks from reinstalling native behavior.</summary>
        private bool disposed;

        /// <summary>Gets or sets whether the browser holds temporary navigation.</summary>
        public bool TemporaryPan { get; set; }

        /// <summary>Installs mode routing before the library's movable behavior.</summary>
        /// <param name="diagram">The mounted native graph.</param>
        /// <param name="owner">The tab-owned presentation authority.</param>
        public DiagramPointerBehavior(Diagram diagram, IDiagramViewModel owner) : base(diagram)
        {
            this.owner = owner;
            diagram.PointerDown += this.PointerDown;
            diagram.PointerMove += this.PointerMove;
            diagram.PointerUp += this.PointerUp;
        }

        /// <summary>Ends navigation and resets private native drag state.</summary>
        public void Cancel()
        {
            if (this.disposed)
            {
                return;
            }

            this.pointerId = null;
            this.panPoint = null;
            this.Diagram.UnregisterBehavior<DragMovablesBehavior>();
            this.Diagram.RegisterBehavior(new DragMovablesBehavior(this.Diagram));
        }

        /// <summary>Detaches this mount's pointer routing.</summary>
        public override void Dispose()
        {
            this.disposed = true;
            this.pointerId = null;
            this.panPoint = null;
            this.Diagram.PointerDown -= this.PointerDown;
            this.Diagram.PointerMove -= this.PointerMove;
            this.Diagram.PointerUp -= this.PointerUp;
        }

        /// <summary>Starts navigation or prepares one permitted native movable.</summary>
        /// <param name="model">The native hit-test result.</param>
        /// <param name="args">The pointer coordinates and buttons.</param>
        private void PointerDown(Model model, PointerEventArgs args)
        {
            if (this.pointerId.HasValue)
            {
                return;
            }

            foreach (var node in this.Diagram.Nodes)
            {
                node.Locked = true;
            }

            if (args.Button is not (0 or 1))
            {
                return;
            }

            this.pointerId = args.PointerId;

            if (args.Button == 1 || this.TemporaryPan || this.owner.ActiveTool == DiagramTool.Pan || model is null)
            {
                this.panPoint = new Point(args.ClientX, args.ClientY);
            }
            else if (model is DiagramNodeModel node)
            {
                this.Diagram.SelectModel(node, true);
                node.Locked = !this.owner.CanMoveNodes || !node.Presentation.Occurrence.IsMovable;
                this.owner.Select(node.Presentation.Occurrence.Element);
            }
        }

        /// <summary>Translates the native viewport in screen space.</summary>
        /// <param name="model">The current hit-test result.</param>
        /// <param name="args">The pointer coordinates.</param>
        private void PointerMove(Model model, PointerEventArgs args)
        {
            if (this.pointerId == args.PointerId && this.panPoint is { } previous)
            {
                this.panPoint = new Point(args.ClientX, args.ClientY);
                this.Diagram.UpdatePan(args.ClientX - previous.X, args.ClientY - previous.Y);
            }
        }

        /// <summary>Ends the matching navigation gesture.</summary>
        /// <param name="model">The final hit-test result.</param>
        /// <param name="args">The pointer identity.</param>
        private void PointerUp(Model model, PointerEventArgs args)
        {
            if (this.pointerId == args.PointerId)
            {
                this.pointerId = null;
                this.panPoint = null;
            }
        }
    }
}
