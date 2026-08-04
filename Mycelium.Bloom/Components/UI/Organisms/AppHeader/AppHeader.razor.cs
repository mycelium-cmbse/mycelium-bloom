// ------------------------------------------------------------------------------------------------
// <copyright file="AppHeader.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.AppHeader
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Common;

    /// <summary>
    /// Represents the compact application header used by engineering workspaces.
    /// </summary>
    public partial class AppHeader : BloomComponentBase
    {
        /// <summary>
        /// Gets or sets the accessible label of the application header.
        /// </summary>
        [Parameter]
        public string AriaLabel { get; set; } = "Application header";

        /// <summary>
        /// Gets or sets the accessible label of the optional navigation region.
        /// </summary>
        [Parameter]
        public string NavigationAriaLabel { get; set; } = "Application navigation";

        /// <summary>
        /// Gets or sets a value indicating whether compact spacing is used.
        /// </summary>
        [Parameter]
        public bool Compact { get; set; }

        /// <summary>
        /// Gets or sets the product or application identity content.
        /// </summary>
        [Parameter]
        public RenderFragment BrandContent { get; set; }

        /// <summary>
        /// Gets or sets optional leading navigation content.
        /// </summary>
        [Parameter]
        public RenderFragment NavigationContent { get; set; }

        /// <summary>
        /// Gets or sets optional title or workspace context content.
        /// </summary>
        [Parameter]
        public RenderFragment ContextContent { get; set; }

        /// <summary>
        /// Gets or sets optional project-selection content.
        /// </summary>
        [Parameter]
        public RenderFragment ProjectContent { get; set; }

        /// <summary>
        /// Gets or sets optional central content such as search.
        /// </summary>
        [Parameter]
        public RenderFragment CenterContent { get; set; }

        /// <summary>
        /// Gets or sets optional application action content.
        /// </summary>
        [Parameter]
        public RenderFragment ActionsContent { get; set; }

        /// <summary>
        /// Gets or sets optional user or account content.
        /// </summary>
        [Parameter]
        public RenderFragment UserContent { get; set; }

        /// <summary>
        /// Gets the final CSS class list applied to the header.
        /// </summary>
        /// <returns>The application-header CSS class list.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass(
                "mb-app-header",
                CssClassBuilder.When("mb-app-header--compact", this.Compact));
        }
    }
}
