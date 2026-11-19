// ------------------------------------------------------------------------------------------------
// <copyright file="PanDiagramTool.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram.Tools
{
    using Blazor.Diagrams.Core.Events;
    using Blazor.Diagrams.Core.Geometry;
    using Blazor.Diagrams.Core.Models.Base;

    /// <summary>Translates the native viewport without selecting or moving occurrences.</summary>
    /// <param name="context">The native diagram and its owner.</param>
    public sealed class PanDiagramTool(DiagramToolContext context) : IDiagramTool
    {
        /// <summary>The last screen-space position for the gesture.</summary>
        private Point previous;

        /// <summary>Gets whether a navigation gesture is in progress.</summary>
        internal bool IsPanning => this.previous is not null;

        /// <summary>Gets the hand cursor for the current gesture.</summary>
        /// <param name="dragging">Whether the pointer is captured.</param>
        /// <returns>The CSS cursor value.</returns>
        public string Cursor(bool dragging) => dragging ? "grabbing" : "grab";

        /// <summary>Starts panning from any hit on the canvas.</summary>
        /// <param name="model">The native hit model.</param>
        /// <param name="args">The starting pointer event.</param>
        public void PointerDown(Model model, PointerEventArgs args) => this.previous = new Point(args.ClientX, args.ClientY);

        /// <summary>Applies a screen-space delta using the native viewport.</summary>
        /// <param name="model">The native hit model.</param>
        /// <param name="args">The current pointer event.</param>
        public void PointerMove(Model model, PointerEventArgs args)
        {
            if (this.previous is { } position)
            {
                this.previous = new Point(args.ClientX, args.ClientY);
                context.Diagram.UpdatePan(args.ClientX - position.X, args.ClientY - position.Y);
            }
        }

        /// <summary>Completes the viewport gesture.</summary>
        /// <param name="model">The native hit model.</param>
        /// <param name="args">The final pointer event.</param>
        public void PointerUp(Model model, PointerEventArgs args) => this.previous = null;

        /// <summary>Releases the gesture without changing the retained viewport.</summary>
        /// <param name="pointerStart">The dispatcher-owned starting event for the interrupted gesture, if any.</param>
        public void Cancel(PointerEventArgs pointerStart) => this.previous = null;
    }
}
