// ------------------------------------------------------------------------------------------------
// <copyright file="Toolbar.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Molecules.Toolbar
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Reusable Bloom toolbar used to group compact actions and contextual controls.
    /// </summary>
    public partial class Toolbar : ComponentBase
    {
        /// <summary>
        /// Gets or sets the toolbar density.
        /// </summary>
        [Parameter]
        public ToolbarDensity Density { get; set; } = ToolbarDensity.Compact;

        /// <summary>
        /// Gets or sets whether the toolbar should render a bottom border.
        /// </summary>
        [Parameter]
        public bool HasBottomBorder { get; set; }

        /// <summary>
        /// Gets or sets whether the toolbar should render a top border.
        /// </summary>
        [Parameter]
        public bool HasTopBorder { get; set; }

        /// <summary>
        /// Gets or sets content rendered at the start of the toolbar.
        /// </summary>
        [Parameter]
        public RenderFragment StartContent { get; set; }

        /// <summary>
        /// Gets or sets content rendered in the center of the toolbar.
        /// </summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets content rendered at the end of the toolbar.
        /// </summary>
        [Parameter]
        public RenderFragment EndContent { get; set; }

        /// <summary>
        /// Gets or sets additional CSS classes.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets unmatched attributes passed to the toolbar element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-toolbar",
                this.GetDensityClass(),
                CssClassBuilder.When("mb-toolbar--bottom-border", this.HasBottomBorder),
                CssClassBuilder.When("mb-toolbar--top-border", this.HasTopBorder),
                this.Class);

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class for the selected toolbar density.
        /// </summary>
        /// <returns>The toolbar density CSS class.</returns>
        private string GetDensityClass()
        {
            var cssClass = this.Density switch
            {
                ToolbarDensity.Comfortable => "mb-toolbar--comfortable",
                _ => "mb-toolbar--compact"
            };

            return cssClass;
        }
    }
}
