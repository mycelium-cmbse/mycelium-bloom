// ------------------------------------------------------------------------------------------------
// <copyright file="CanvasToolbar.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Molecules.CanvasToolbar
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Represents a compact toolbar suitable for composition near a model canvas.
    /// </summary>
    public partial class CanvasToolbar : BloomComponentBase
    {
        /// <summary>
        /// Gets or sets the accessible label of the underlying toolbar.
        /// </summary>
        [Parameter]
        public string AriaLabel { get; set; } = "Canvas tools";

        /// <summary>
        /// Gets or sets the toolbar orientation.
        /// </summary>
        [Parameter]
        public ToolbarOrientation Orientation { get; set; } = ToolbarOrientation.Horizontal;

        /// <summary>
        /// Gets or sets a value indicating whether compact spacing is used.
        /// </summary>
        [Parameter]
        public bool Compact { get; set; }

        /// <summary>
        /// Gets or sets the consumer-provided toolbar content.
        /// </summary>
        [Parameter]
        [EditorRequired]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets the final CSS class list applied to the canvas-toolbar root.
        /// </summary>
        /// <returns>The canvas-toolbar CSS class list.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass(
                "mb-canvas-toolbar",
                CssClassBuilder.When(
                    "mb-canvas-toolbar--vertical",
                    this.Orientation == ToolbarOrientation.Vertical),
                CssClassBuilder.When("mb-canvas-toolbar--compact", this.Compact));
        }

        /// <summary>
        /// Gets the ARIA orientation exposed by the underlying toolbar.
        /// </summary>
        /// <returns>The lowercase ARIA orientation value.</returns>
        private string GetAriaOrientation()
        {
            return this.Orientation == ToolbarOrientation.Vertical ? "vertical" : "horizontal";
        }
    }
}
