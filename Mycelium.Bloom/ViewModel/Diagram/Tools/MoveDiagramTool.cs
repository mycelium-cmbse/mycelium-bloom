// ------------------------------------------------------------------------------------------------
// <copyright file="MoveDiagramTool.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram.Tools
{
    using Blazor.Diagrams.Core.Events;
    using Blazor.Diagrams.Core.Models;
    using Blazor.Diagrams.Core.Models.Base;

    /// <summary>Selects canonical occurrences and gives the native drag behavior a permitted target.</summary>
    /// <param name="context">The native diagram and its owner.</param>
    public sealed class MoveDiagramTool(DiagramToolContext context) : IDiagramTool
    {
        /// <summary>Reuses viewport navigation for drags starting in empty space.</summary>
        private readonly PanDiagramTool background = new(context);

        /// <summary>The native target selected solely for a permitted drag.</summary>
        private NodeModel dragTarget;

        /// <summary>Gets the active drag cursor or the default selection cursor.</summary>
        /// <param name="dragging">Whether the runtime owns a pointer gesture.</param>
        /// <returns>The CSS cursor value, or null to use native styling.</returns>
        public string Cursor(bool dragging) => dragging && (this.background.IsPanning || this.dragTarget is not null) ? "grabbing" : null;

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

        /// <summary>Navigates empty canvas space while the native behavior handles occurrence movement.</summary>
        /// <param name="model">The native hit model.</param>
        /// <param name="args">The current pointer event.</param>
        public void PointerMove(Model model, PointerEventArgs args) => this.background.PointerMove(model, args);

        /// <summary>Releases the native drag target before processing another gesture.</summary>
        /// <param name="model">The native hit model.</param>
        /// <param name="args">The final pointer event.</param>
        public void PointerUp(Model model, PointerEventArgs args)
        {
            this.background.PointerUp(model, args);

            if (this.dragTarget is { } target)
            {
                this.dragTarget = null;
                context.Diagram.UnselectModel(target);
            }
        }

        /// <summary>Completes the native behavior through its public pointer-up event when a browser gesture is interrupted.</summary>
        /// <param name="pointerStart">The dispatcher-owned starting event for the interrupted gesture, if any.</param>
        public void Cancel(PointerEventArgs pointerStart)
        {
            var target = this.dragTarget;
            this.PointerUp(target, pointerStart);

            if (target is not null && pointerStart is not null)
            {
                context.Diagram.TriggerPointerUp(target, pointerStart);
            }
        }
    }
}
