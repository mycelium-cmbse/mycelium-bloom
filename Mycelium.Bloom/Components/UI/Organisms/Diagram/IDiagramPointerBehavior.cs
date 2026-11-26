// ------------------------------------------------------------------------------------------------
// <copyright file="IDiagramPointerBehavior.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.ViewModel.Diagram;

    /// <summary>Exposes mount-owned input routing without replacing native behavior inheritance.</summary>
    public interface IDiagramPointerBehavior : IDisposable
    {
        /// <summary>Notifies the canvas when runtime presentation changes.</summary>
        event Action Changed;

        /// <summary>Gets the active runtime's cursor.</summary>
        string Cursor { get; }

        /// <summary>Gets the active runtime's preview.</summary>
        RenderFragment Preview { get; }

        /// <summary>Gets whether a gesture or navigation override owns input.</summary>
        bool IsInteracting { get; }

        /// <summary>Routes a resize handle to the active runtime using the native pointer lifecycle.</summary>
        /// <param name="node">The native node represented by the mounted handle.</param>
        /// <param name="args">The starting browser pointer event.</param>
        void BeginResize(DiagramNodeModel node, Blazor.Diagrams.Core.Events.PointerEventArgs args);

        /// <summary>Routes a nonmovable semantic row through the current native tool and gesture lifetime.</summary>
        /// <param name="node">The mounted parent occurrence.</param>
        /// <param name="row">The current row state owned by that occurrence.</param>
        /// <param name="args">The starting browser pointer event.</param>
        void BeginSelection(DiagramNodeModel node, DiagramCompartmentRowViewModel row, Blazor.Diagrams.Core.Events.PointerEventArgs args);

        /// <summary>Records a focused target for a possible Space tap.</summary>
        /// <param name="args">The key press on the selectable widget.</param>
        /// <param name="target">The tab-owned selection target.</param>
        void PrepareSpaceActivation(KeyboardEventArgs args, DiagramElementViewModel target);

        /// <summary>Discards pending activation for a widget.</summary>
        /// <param name="target">The widget's selection target.</param>
        void CancelSpaceActivation(DiagramElementViewModel target);

        /// <summary>Routes keyboard input from the canvas.</summary>
        /// <param name="key">The browser key.</param>
        /// <param name="code">The physical key code.</param>
        /// <param name="pressed">Whether the key is pressed.</param>
        /// <param name="modified">Whether a shortcut modifier is held.</param>
        void KeyChanged(string key, string code, bool pressed, bool modified = false);

        /// <summary>Cancels only the specified captured pointer.</summary>
        /// <param name="pointer">The interrupted pointer identifier.</param>
        /// <returns>Whether the pointer belonged to this mount.</returns>
        bool CancelPointer(long pointer);

        /// <summary>Cancels the current gesture and restores the retained tool.</summary>
        void Cancel();
    }
}
