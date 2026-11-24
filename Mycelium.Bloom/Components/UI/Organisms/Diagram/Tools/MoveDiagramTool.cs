// ------------------------------------------------------------------------------------------------
// <copyright file="MoveDiagramTool.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram.Tools
{
    using Blazor.Diagrams.Core.Events;
    using Blazor.Diagrams.Core.Geometry;
    using Blazor.Diagrams.Core.Models;
    using Blazor.Diagrams.Core.Models.Base;

    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.ViewModel.Diagram;

    /// <summary>Selects canonical occurrences and handles permitted native movement and presentation resizing.</summary>
    /// <param name="context">The native diagram and its owner.</param>
    public sealed class MoveDiagramTool(DiagramToolContext context) : IDiagramTool, IDiagramResizeTool
    {
        /// <summary>Reuses viewport navigation for drags starting in empty space.</summary>
        private readonly PanDiagramTool background = new(context);

        /// <summary>The native target selected solely for a permitted drag.</summary>
        private NodeModel dragTarget;

        /// <summary>The mounted occurrence whose dimensions are being changed.</summary>
        private DiagramNodeModel resizeTarget;

        /// <summary>The native model-space coordinate at the start of resizing.</summary>
        private Point resizeOrigin;

        /// <summary>The retained dimensions at the start of resizing.</summary>
        private DiagramSize resizeSize;

        /// <summary>Gets the resize, navigation or default selection cursor.</summary>
        /// <param name="dragging">Whether the runtime owns a pointer gesture.</param>
        /// <returns>The CSS cursor value, or null to use native styling.</returns>
        public string Cursor(bool dragging)
        {
            if (this.resizeTarget is not null)
            {
                return "nwse-resize";
            }

            return dragging && (this.background.IsPanning || this.dragTarget is not null) ? "grabbing" : null;
        }

        /// <summary>Starts an occurrence-local resize without selecting a native movement target.</summary>
        /// <param name="node">The mounted occurrence represented by the handle.</param>
        /// <param name="args">The initial browser coordinate.</param>
        /// <returns>Whether resizing is currently permitted.</returns>
        public bool BeginResize(DiagramNodeModel node, PointerEventArgs args)
        {
            if (!context.Owner.CanMoveNodes || !node.Presentation.Occurrence.IsResizable || context.Diagram.Container is null
                || !double.IsFinite(args.ClientX) || !double.IsFinite(args.ClientY))
            {
                return false;
            }

            this.resizeTarget = node;
            this.resizeOrigin = context.Diagram.GetRelativeMousePoint(args.ClientX, args.ClientY);
            this.resizeSize = node.ViewModel.Size;
            return true;
        }

        /// <summary>Selects the canonical node and prepares the native drag where editing allows it.</summary>
        /// <param name="model">The native hit model.</param>
        /// <param name="args">The starting pointer event.</param>
        public void PointerDown(Model model, PointerEventArgs args)
        {
            if (model is null)
            {
                this.background.PointerDown(null, args);
            }
            else if (context.FindElement(model) is { } element)
            {
                context.Owner.Select(element.Element);

                if (context.Owner.CanMoveNodes && model is NodeModel { Locked: false } node && element is DiagramNodeViewModel { Presentation.Occurrence.IsMovable: true })
                {
                    this.dragTarget = node;
                    context.Diagram.SelectModel(node, true);
                }
            }
        }

        /// <summary>Updates retained dimensions or navigates while native behavior handles ordinary movement.</summary>
        /// <param name="model">The native hit model.</param>
        /// <param name="args">The current pointer event.</param>
        public void PointerMove(Model model, PointerEventArgs args)
        {
            if (this.resizeTarget is not null)
            {
                this.Resize(args);
            }
            else
            {
                this.background.PointerMove(model, args);
            }
        }

        /// <summary>Applies final resize coordinates and releases the current gesture before processing another.</summary>
        /// <param name="model">The native hit model.</param>
        /// <param name="args">The final pointer event.</param>
        public void PointerUp(Model model, PointerEventArgs args)
        {
            if (this.resizeTarget is not null)
            {
                this.Resize(args);
                this.resizeTarget = null;
            }

            this.background.PointerUp(model, args);
            this.ReleaseDragTarget();
        }

        /// <summary>Completes the native behavior through its public pointer-up event when a browser gesture is interrupted.</summary>
        /// <param name="pointerStart">The dispatcher-owned starting event for the interrupted gesture, if any.</param>
        public void Cancel(PointerEventArgs pointerStart)
        {
            this.resizeTarget = null;
            var target = this.dragTarget;
            this.PointerUp(target, pointerStart);

            if (target is not null && pointerStart is not null)
            {
                context.Diagram.TriggerPointerUp(target, pointerStart);
            }
        }

        /// <summary>Disposes background navigation and releases any remaining native drag selection after dispatcher cancellation.</summary>
        public void Dispose()
        {
            this.resizeTarget = null;
            this.background.Dispose();
            this.ReleaseDragTarget();
        }

        /// <summary>Converts the current browser coordinate through native zoom and pan before retaining dimensions.</summary>
        /// <param name="args">The latest pointer coordinate.</param>
        private void Resize(PointerEventArgs args)
        {
            if (args is null || !double.IsFinite(args.ClientX) || !double.IsFinite(args.ClientY))
            {
                return;
            }

            var point = context.Diagram.GetRelativeMousePoint(args.ClientX, args.ClientY);
            context.Owner.ResizeNode(this.resizeTarget.Presentation.Occurrence.Id,
                this.resizeSize.Width + point.X - this.resizeOrigin.X, this.resizeSize.Height + point.Y - this.resizeOrigin.Y);
        }

        /// <summary>Clears the tool's native drag selection.</summary>
        private void ReleaseDragTarget()
        {
            if (this.dragTarget is { } target)
            {
                this.dragTarget = null;
                context.Diagram.UnselectModel(target);
            }
        }
    }
}
