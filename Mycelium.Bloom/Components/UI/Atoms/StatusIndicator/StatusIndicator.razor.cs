// ------------------------------------------------------------------------------------------------
// <copyright file="StatusIndicator.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Atoms.StatusIndicator
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Represents a compact status indicator with a colored dot and optional label.
    /// </summary>
    public partial class StatusIndicator : ComponentBase
    {
        /// <summary>
        /// Gets or sets the visual variant of the status indicator.
        /// </summary>
        [Parameter]
        public StatusIndicatorVariant Variant { get; set; } = StatusIndicatorVariant.Neutral;

        /// <summary>
        /// Gets or sets the visible status label.
        /// </summary>
        [Parameter]
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the status label should be displayed.
        /// </summary>
        [Parameter]
        public bool ShowLabel { get; set; } = true;

        /// <summary>
        /// Gets or sets additional CSS classes.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets unmatched attributes passed to the status indicator element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the final CSS class list applied to the status indicator.
        /// </summary>
        /// <returns>The status indicator CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-status-indicator",
                this.GetVariantClass(),
                this.Class);

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class matching the selected status indicator variant.
        /// </summary>
        /// <returns>The CSS class for the selected status indicator variant.</returns>
        private string GetVariantClass()
        {
            var cssClass = this.Variant switch
            {
                StatusIndicatorVariant.Success => "mb-status-indicator--success",
                StatusIndicatorVariant.Warning => "mb-status-indicator--warning",
                StatusIndicatorVariant.Danger => "mb-status-indicator--danger",
                StatusIndicatorVariant.Info => "mb-status-indicator--info",
                _ => "mb-status-indicator--neutral"
            };

            return cssClass;
        }
    }
}
