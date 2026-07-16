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

    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Represents a compact semantic breadcrumb trail.
    /// </summary>
    public partial class Breadcrumbs : BloomComponentBase
    {
        /// <summary>
        /// Gets or sets the breadcrumb items.
        /// </summary>
        [Parameter]
        public IReadOnlyList<BreadcrumbItem> Items { get; set; } = [];

        /// <summary>
        /// Gets or sets the accessible label of the breadcrumb navigation.
        /// </summary>
        [Parameter]
        public string AriaLabel { get; set; } = "Breadcrumb";

        /// <summary>
        /// Gets or sets the text rendered between breadcrumb items when no custom separator is supplied.
        /// </summary>
        [Parameter]
        public string Separator { get; set; } = "/";

        /// <summary>
        /// Gets or sets optional custom separator content.
        /// </summary>
        [Parameter]
        public RenderFragment SeparatorContent { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when an enabled non-current item is selected.
        /// </summary>
        [Parameter]
        public EventCallback<BreadcrumbItem> ItemSelected { get; set; }

        /// <summary>
        /// Gets the final CSS class list applied to the navigation element.
        /// </summary>
        /// <returns>The breadcrumb CSS class list.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass("mb-breadcrumbs");
        }

        /// <summary>
        /// Selects an enabled, non-current breadcrumb item.
        /// </summary>
        /// <param name="item">The selected breadcrumb item.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SelectItemAsync(BreadcrumbItem item)
        {
            if (item.Disabled || item.IsCurrent)
            {
                return;
            }

            await this.ItemSelected.InvokeAsync(item);
        }
    }
}
