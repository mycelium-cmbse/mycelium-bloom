// ------------------------------------------------------------------------------------------------
// <copyright file="Panel.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Atoms.Panel
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Represents a reusable panel component used to wrap content with consistent spacing and layout behavior.
    /// </summary>
    public partial class Panel : BloomComponentBase
    {
        /// <summary>
        /// Gets or sets the padding applied inside the panel.
        /// </summary>
        [Parameter]
        public PanelPadding Padding { get; set; } = PanelPadding.Medium;

        /// <summary>
        /// Gets or sets a value indicating whether the panel should take the full available height.
        /// </summary>
        [Parameter]
        public bool FullHeight { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether overflowing content should be hidden.
        /// </summary>
        [Parameter]
        public bool OverflowHidden { get; set; }

        /// <summary>
        /// Gets or sets the content rendered inside the panel.
        /// </summary>
        [Parameter]
        [EditorRequired]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets the final CSS class list applied to the panel.
        /// </summary>
        private string GetCssClass()
        {
            var cssClass = this.BuildRootCssClass(
                "mb-panel",
                this.GetPaddingClass(),
                CssClassBuilder.When("mb-panel--full-height", this.FullHeight),
                CssClassBuilder.When("mb-panel--overflow-hidden", this.OverflowHidden));

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class matching the selected panel padding.
        /// </summary>
        /// <returns>The CSS class for the selected panel padding.</returns>
        private string GetPaddingClass()
        {
            var cssClass = this.Padding switch
            {
                PanelPadding.None => "mb-panel--padding-none",
                PanelPadding.Small => "mb-panel--padding-small",
                PanelPadding.Large => "mb-panel--padding-large",
                _ => "mb-panel--padding-medium"
            };

            return cssClass;
        }
    }
}
