// ------------------------------------------------------------------------------------------------
// <copyright file="IDiagramTool.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram.Tools
{
    using Blazor.Diagrams.Core.Events;
    using Blazor.Diagrams.Core.Models.Base;

    using Microsoft.AspNetCore.Components;

    /// <summary>Owns the behavior and disposable resources of one tool in a mounted diagram.</summary>
    public interface IDiagramTool : IDisposable
    {
        /// <summary>Gets an optional SVG preview rendered in the native diagram layer.</summary>
        RenderFragment Preview => null;

        /// <summary>Returns the cursor for the current interaction, or null to use the default.</summary>
        /// <param name="dragging">Whether the runtime owns a pointer gesture.</param>
        /// <returns>The CSS cursor value.</returns>
        string Cursor(bool dragging) => null;

        /// <summary>Activates the mounted runtime.</summary>
        void Activate() { }

        /// <summary>Deactivates the runtime after its active pointer gesture is released.</summary>
        void Deactivate() { }

        /// <summary>Begins a gesture using the native hit model.</summary>
        /// <param name="model">The hit model, or null for empty space.</param>
        /// <param name="args">The native pointer event.</param>
        void PointerDown(Model model, PointerEventArgs args) { }

        /// <summary>Routes a pointer move, including moves without an active gesture for previews.</summary>
        /// <param name="model">The current native hit model, when supplied.</param>
        /// <param name="args">The native pointer event.</param>
        void PointerMove(Model model, PointerEventArgs args) { }

        /// <summary>Completes the active pointer gesture.</summary>
        /// <param name="model">The last native hit model, when supplied.</param>
        /// <param name="args">The native pointer event.</param>
        void PointerUp(Model model, PointerEventArgs args) { }

        /// <summary>Cancels in-progress interaction and releases its preview.</summary>
        /// <param name="pointerStart">The dispatcher-owned starting event, or null when no pointer gesture was active.</param>
        void Cancel(PointerEventArgs pointerStart) { }

        /// <summary>Handles a keyboard gesture delivered by the canvas.</summary>
        /// <param name="key">The keyboard key.</param>
        /// <param name="pressed">Whether the key was pressed or released.</param>
        void KeyChanged(string key, bool pressed) { }
    }
}
