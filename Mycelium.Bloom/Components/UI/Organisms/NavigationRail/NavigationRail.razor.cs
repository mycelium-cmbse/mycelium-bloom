// ------------------------------------------------------------------------------------------------
// <copyright file="NavigationRail.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.NavigationRail
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Presents data-driven application destinations in a compact navigation rail.
    /// </summary>
    public sealed partial class NavigationRail : BloomComponentBase
    {
        /// <summary>
        /// The stable identifier of the destination list.
        /// </summary>
        private readonly string itemsId = CreateGeneratedId("mb-navigation-rail-items");

        /// <summary>
        /// Gets or sets the available navigation destinations.
        /// </summary>
        [Parameter]
        public IReadOnlyList<NavigationRailItem> Items { get; set; } = [];

        /// <summary>
        /// Gets or sets the selected destination identifier.
        /// </summary>
        [Parameter]
        public string SelectedItemId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the callback invoked when a destination is requested.
        /// </summary>
        [Parameter]
        public EventCallback<string> SelectedItemIdChanged { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the rail uses its icon-first presentation.
        /// </summary>
        [Parameter]
        public bool Collapsed { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the opposite collapsed state is requested.
        /// </summary>
        [Parameter]
        public EventCallback<bool> CollapsedChanged { get; set; }

        /// <summary>
        /// Gets or sets the accessible label of the navigation region.
        /// </summary>
        [Parameter]
        public string AriaLabel { get; set; } = "Workspace navigation";

        /// <summary>
        /// Gets the final CSS class list applied to the rail.
        /// </summary>
        /// <returns>The navigation-rail CSS class list.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass(
                "mb-navigation-rail",
                CssClassBuilder.When("mb-navigation-rail--collapsed", this.Collapsed));
        }

        /// <summary>
        /// Gets the final CSS class list applied to a destination.
        /// </summary>
        /// <param name="item">The destination.</param>
        /// <returns>The destination CSS class list.</returns>
        private string GetItemCssClass(NavigationRailItem item)
        {
            return CssClassBuilder.Build(
                "mb-navigation-rail__link",
                CssClassBuilder.When("mb-navigation-rail__link--active", this.IsSelected(item)));
        }

        /// <summary>
        /// Gets the current-page state for a destination.
        /// </summary>
        /// <param name="item">The destination.</param>
        /// <returns>Page for the selected destination; otherwise, null.</returns>
        private string GetAriaCurrent(NavigationRailItem item)
        {
            return this.IsSelected(item) ? "page" : null;
        }

        /// <summary>
        /// Gets the supplementary pointer hint for an icon-only destination.
        /// </summary>
        /// <param name="item">The destination.</param>
        /// <returns>The destination label when collapsed; otherwise, null.</returns>
        private string GetItemTitle(NavigationRailItem item)
        {
            return this.Collapsed ? item.Label : null;
        }

        /// <summary>
        /// Gets the accessible label of the collapse toggle.
        /// </summary>
        /// <returns>The action requested by the toggle.</returns>
        private string GetToggleAriaLabel()
        {
            return this.Collapsed ? "Expand workspace navigation" : "Collapse workspace navigation";
        }

        /// <summary>
        /// Determines whether a destination owns the controlled selected state.
        /// </summary>
        /// <param name="item">The destination.</param>
        /// <returns>True when the destination is selected.</returns>
        private bool IsSelected(NavigationRailItem item)
        {
            return string.Equals(item.Id, this.SelectedItemId, StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether the caller supplied a controlled collapse callback.
        /// </summary>
        /// <returns>True when the collapse control can request a state change.</returns>
        private bool ShouldRenderCollapseToggle()
        {
            return this.CollapsedChanged.HasDelegate;
        }

        /// <summary>
        /// Requests selection of a destination without mutating controlled state.
        /// </summary>
        /// <param name="item">The requested destination.</param>
        /// <returns>A task representing the callback.</returns>
        private Task SelectItemAsync(NavigationRailItem item)
        {
            return this.SelectedItemIdChanged.InvokeAsync(item.Id);
        }

        /// <summary>
        /// Requests the opposite collapsed state without mutating controlled state.
        /// </summary>
        /// <returns>A task representing the callback.</returns>
        private Task ToggleCollapsedAsync()
        {
            return this.CollapsedChanged.InvokeAsync(!this.Collapsed);
        }
    }
}
