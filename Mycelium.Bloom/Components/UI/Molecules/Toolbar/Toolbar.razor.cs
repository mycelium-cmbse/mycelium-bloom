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
    using Mycelium.Bloom.Components.UI.Common;

    /// <summary>
    /// Represents a reusable horizontal action area composed from render fragments.
    /// </summary>
    public partial class Toolbar : BloomComponentBase
    {
        /// <summary>
        /// Gets or sets the accessible toolbar label.
        /// </summary>
        [Parameter]
        public string AriaLabel { get; set; } = "Toolbar";

        /// <summary>
        /// Gets or sets a value indicating whether the toolbar uses compact spacing.
        /// </summary>
        [Parameter]
        public bool Compact { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether toolbar content may wrap.
        /// </summary>
        [Parameter]
        public bool AllowWrap { get; set; } = true;

        /// <summary>
        /// Gets or sets content rendered at the leading edge of the toolbar.
        /// </summary>
        [Parameter]
        public RenderFragment LeadingContent { get; set; }

        /// <summary>
        /// Gets or sets the main toolbar content.
        /// </summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets content rendered at the trailing edge of the toolbar.
        /// </summary>
        [Parameter]
        public RenderFragment TrailingContent { get; set; }

        /// <summary>
        /// Gets the final CSS class list applied to the toolbar.
        /// </summary>
        /// <returns>The toolbar CSS class list.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass(
                "mb-toolbar",
                CssClassBuilder.When("mb-toolbar--compact", this.Compact),
                CssClassBuilder.When("mb-toolbar--wrap", this.AllowWrap));
        }
    }
}
