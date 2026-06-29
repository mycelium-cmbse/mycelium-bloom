// ------------------------------------------------------------------------------------------------
// <copyright file="Breadcrumbs.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Molecules.Breadcrumbs
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Reusable Bloom breadcrumbs component for compact workspace navigation.
    /// </summary>
    public partial class Breadcrumbs : ComponentBase
    {
        /// <summary>
        /// Gets or sets the breadcrumb items.
        /// </summary>
        [Parameter]
        public IReadOnlyList<BreadcrumbItem> Items { get; set; } = [];

        /// <summary>
        /// Gets or sets the callback invoked when a non-current breadcrumb item is selected.
        /// </summary>
        [Parameter]
        public EventCallback<string> SelectedValueChanged { get; set; }

        /// <summary>
        /// Gets or sets additional CSS classes.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets unmatched attributes passed to the breadcrumb navigation element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the final CSS class list applied to the breadcrumbs.
        /// </summary>
        /// <returns>The breadcrumb CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-breadcrumbs",
                this.Class);

            return cssClass;
        }

        /// <summary>
        /// Selects the provided breadcrumb item when it is not current.
        /// </summary>
        /// <param name="item">The breadcrumb item to select.</param>
        private async Task SelectItemAsync(BreadcrumbItem item)
        {
            if (!item.IsCurrent)
            {
                await this.SelectedValueChanged.InvokeAsync(item.Value);
            }
        }
    }
}
