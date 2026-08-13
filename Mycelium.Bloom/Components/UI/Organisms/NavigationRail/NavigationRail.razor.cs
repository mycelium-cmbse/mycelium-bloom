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
        /// Gets the available sidebar-control modes in display order.
        /// </summary>
        private static readonly SidebarControlMode[] SidebarControlModes =
        [
            SidebarControlMode.Expanded,
            SidebarControlMode.Collapsed,
            SidebarControlMode.ExpandOnHover
        ];

        /// <summary>
        /// The stable identifier of the destination list.
        /// </summary>
        private readonly string itemsId = CreateGeneratedId("mb-navigation-rail-items");

        /// <summary>
        /// Tracks whether the current expansion request originated from hovering a collapsed rail.
        /// </summary>
        private bool hoverExpansionActive;

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
        /// Gets or sets a value indicating whether hovering a collapsed rail temporarily requests expansion.
        /// </summary>
        /// <remarks>
        /// The caller remains responsible for applying <see cref="CollapsedChanged" /> requests to the rail and its containing layout.
        /// </remarks>
        [Parameter]
        public bool ExpandOnHover { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when a different hover-expansion preference is requested.
        /// </summary>
        [Parameter]
        public EventCallback<bool> ExpandOnHoverChanged { get; set; }

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
        /// Gets the label of a sidebar-control mode.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <returns>The user-facing mode label.</returns>
        private static string GetSidebarControlModeLabel(SidebarControlMode mode)
        {
            return mode switch
            {
                SidebarControlMode.Expanded => "Expanded",
                SidebarControlMode.Collapsed => "Collapsed",
                SidebarControlMode.ExpandOnHover => "Expand on hover",
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }

        /// <summary>
        /// Gets the accessible label and pointer hint for the sidebar-control button.
        /// </summary>
        /// <returns>The primary toggle action and context-menu hint.</returns>
        private string GetSidebarControlLabel()
        {
            var action = this.Collapsed ? "Expand" : "Collapse";

            return $"{action} workspace navigation; right-click for sidebar controls";
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
        /// Gets the CSS class list for a sidebar-control option.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <returns>The option CSS class list.</returns>
        private string GetSidebarControlItemCssClass(SidebarControlMode mode)
        {
            return CssClassBuilder.Build(
                "mb-navigation-rail__control-option",
                CssClassBuilder.When(
                    "mb-navigation-rail__control-option--selected",
                    this.IsSidebarControlModeSelected(mode)));
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
        /// Determines whether the caller supplied the callback required by a sidebar-control mode.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <returns>True when the mode can be requested.</returns>
        private bool CanRequestSidebarControlMode(SidebarControlMode mode)
        {
            if (!this.CollapsedChanged.HasDelegate)
            {
                return false;
            }

            return mode == SidebarControlMode.ExpandOnHover
                ? this.ExpandOnHover || this.ExpandOnHoverChanged.HasDelegate
                : !this.ExpandOnHover || this.ExpandOnHoverChanged.HasDelegate;
        }

        /// <summary>
        /// Determines whether a sidebar-control mode represents the caller-owned state.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <returns>True when the mode is selected.</returns>
        private bool IsSidebarControlModeSelected(SidebarControlMode mode)
        {
            return mode == this.GetSidebarControlMode();
        }

        /// <summary>
        /// Gets the sidebar-control mode represented by the caller-owned parameters.
        /// </summary>
        /// <returns>The current mode.</returns>
        private SidebarControlMode GetSidebarControlMode()
        {
            if (this.ExpandOnHover)
            {
                return SidebarControlMode.ExpandOnHover;
            }

            return this.Collapsed
                ? SidebarControlMode.Collapsed
                : SidebarControlMode.Expanded;
        }

        /// <summary>
        /// Determines whether the caller supplied a controlled collapse callback.
        /// </summary>
        /// <returns>True when the sidebar control can request a state change.</returns>
        private bool ShouldRenderSidebarControl()
        {
            return this.CollapsedChanged.HasDelegate;
        }

        /// <summary>
        /// Requests the opposite fixed collapse state without opening the options menu.
        /// </summary>
        /// <returns>A task representing the controlled callbacks.</returns>
        private async Task ToggleCollapsedAsync()
        {
            var collapsed = !this.Collapsed;

            this.hoverExpansionActive = false;

            if (this.ExpandOnHover && this.ExpandOnHoverChanged.HasDelegate)
            {
                await this.ExpandOnHoverChanged.InvokeAsync(false);
            }

            await this.CollapsedChanged.InvokeAsync(collapsed);
        }

        /// <summary>
        /// Requests a sidebar-control mode without mutating controlled state.
        /// </summary>
        /// <param name="mode">The requested mode.</param>
        /// <returns>A task representing the callbacks.</returns>
        private async Task RequestSidebarControlModeAsync(SidebarControlMode mode)
        {
            if (!this.CanRequestSidebarControlMode(mode))
            {
                return;
            }

            this.hoverExpansionActive = false;

            var expandOnHover = mode == SidebarControlMode.ExpandOnHover;
            var collapsed = mode != SidebarControlMode.Expanded;

            if (this.ExpandOnHover != expandOnHover)
            {
                await this.ExpandOnHoverChanged.InvokeAsync(expandOnHover);
            }

            if (this.Collapsed != collapsed)
            {
                await this.CollapsedChanged.InvokeAsync(collapsed);
            }
        }

        /// <summary>
        /// Requests temporary expansion when the pointer enters an opted-in collapsed rail.
        /// </summary>
        /// <returns>A task representing the callback.</returns>
        private Task HandleMouseEnterAsync()
        {
            if (!this.ExpandOnHover || this.hoverExpansionActive || !this.Collapsed || !this.CollapsedChanged.HasDelegate)
            {
                return Task.CompletedTask;
            }

            this.hoverExpansionActive = true;

            return this.CollapsedChanged.InvokeAsync(false);
        }

        /// <summary>
        /// Restores the collapsed state when a temporary hover expansion ends.
        /// </summary>
        /// <returns>A task representing the callback.</returns>
        private Task HandleMouseLeaveAsync()
        {
            if (!this.hoverExpansionActive)
            {
                return Task.CompletedTask;
            }

            this.hoverExpansionActive = false;

            return this.ExpandOnHover && this.CollapsedChanged.HasDelegate
                ? this.CollapsedChanged.InvokeAsync(true)
                : Task.CompletedTask;
        }

        /// <summary>
        /// Identifies the mutually exclusive sidebar presentation preferences.
        /// </summary>
        private enum SidebarControlMode
        {
            /// <summary>
            /// The rail remains expanded.
            /// </summary>
            Expanded,

            /// <summary>
            /// The rail remains collapsed.
            /// </summary>
            Collapsed,

            /// <summary>
            /// The rail rests collapsed and expands temporarily on hover.
            /// </summary>
            ExpandOnHover
        }
    }
}
