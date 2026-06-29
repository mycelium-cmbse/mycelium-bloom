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
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Reusable Bloom status bar for compact workspace and project state.
    /// </summary>
    public partial class StatusBar : ComponentBase
    {
        /// <summary>
        /// Gets or sets the status bar items.
        /// </summary>
        [Parameter]
        public IReadOnlyList<StatusBarItem> Items { get; set; } = [];

        /// <summary>
        /// Gets or sets optional content rendered at the start of the status bar.
        /// </summary>
        [Parameter]
        public RenderFragment StartContent { get; set; }

        /// <summary>
        /// Gets or sets optional content rendered at the end of the status bar.
        /// </summary>
        [Parameter]
        public RenderFragment EndContent { get; set; }

        /// <summary>
        /// Gets or sets additional CSS classes.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets unmatched attributes passed to the status bar element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the final CSS class list applied to the status bar.
        /// </summary>
        /// <returns>The status bar CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-status-bar",
                this.Class);

            return cssClass;
        }
    }
}
