// ------------------------------------------------------------------------------------------------
// <copyright file="StatusBar.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.StatusBar
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Common;

    /// <summary>
    /// Represents a compact compositional status area for the bottom of a workspace.
    /// </summary>
    public partial class StatusBar : BloomComponentBase
    {
        /// <summary>
        /// Gets or sets the accessible label of the status area.
        /// </summary>
        [Parameter]
        public string AriaLabel { get; set; } = "Workspace status";

        /// <summary>
        /// Gets or sets a value indicating whether compact spacing is used.
        /// </summary>
        [Parameter]
        public bool Compact { get; set; }

        /// <summary>
        /// Gets or sets content rendered at the leading edge of the status bar.
        /// </summary>
        [Parameter]
        public RenderFragment LeadingContent { get; set; }

        /// <summary>
        /// Gets or sets the central status content.
        /// </summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets content rendered at the trailing edge of the status bar.
        /// </summary>
        [Parameter]
        public RenderFragment TrailingContent { get; set; }

        /// <summary>
        /// Gets the final CSS class list applied to the status bar.
        /// </summary>
        /// <returns>The status-bar CSS class list.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass(
                "mb-status-bar",
                CssClassBuilder.When("mb-status-bar--compact", this.Compact));
        }
    }
}
