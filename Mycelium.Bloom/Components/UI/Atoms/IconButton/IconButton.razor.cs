// // ------------------------------------------------------------------------------------------------
// // <copyright file="IconButton.razor.cs" company="Starion Group S.A.">
// //
// //   Copyright 2026 Starion Group S.A.
// //   SPDX-License-Identifier: Apache-2.0
// //
// // </copyright>
// // ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Atoms.IconButton
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.Components.Common;

    /// <summary>
    /// Represents a reusable icon-only button component with accessibility support.
    /// </summary>
    public partial class IconButton : ComponentBase
    {
        /// <summary>
        /// Gets or sets the HTML button type.
        /// </summary>
        [Parameter]
        public string Type { get; set; } = "button";

        /// <summary>
        /// Gets or sets the accessible label of the icon button.
        /// </summary>
        [Parameter]
        [EditorRequired]
        public string AriaLabel { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional title used for tooltip display.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the icon button is disabled.
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets additional CSS classes applied to the icon button.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the icon content rendered inside the button.
        /// </summary>
        [Parameter]
        [EditorRequired]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the icon button is clicked.
        /// </summary>
        [Parameter]
        public EventCallback<MouseEventArgs> OnClick { get; set; }

        /// <summary>
        /// Gets or sets additional unmatched attributes applied to the button element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the final CSS class list applied to the icon button.
        /// </summary>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-icon-button",
                this.Class);

            return cssClass;
        }
    }
}
