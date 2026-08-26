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
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.NavigationRail;

    /// <summary>
    /// Presents data-driven application destinations from reactive navigation state.
    /// </summary>
    public sealed partial class NavigationRail : BloomReactiveComponentBase<INavigationRailViewModel>
    {
        /// <summary>
        /// Gets the available presentation modes in display order.
        /// </summary>
        private static readonly NavigationRailPresentationMode[] PresentationModes =
            Enum.GetValues<NavigationRailPresentationMode>();

        /// <summary>
        /// A value indicating whether the pointer is currently over this component instance.
        /// </summary>
        private bool isPointerOver;

        /// <summary>
        /// A value indicating whether the trigger-anchored sidebar control menu is open.
        /// </summary>
        private bool isSidebarControlMenuOpen;

        /// <summary>
        /// A value indicating whether component disposal has begun.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// The effective collapse state most recently reported to the owning composition.
        /// </summary>
        private bool? reportedCollapsedState;

        /// <summary>
        /// Gets the ViewModel required while rendering an assigned rail.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when rail interaction is attempted without an assigned ViewModel.
        /// </exception>
        private INavigationRailViewModel RequiredViewModel =>
            this.ViewModel
            ?? throw new InvalidOperationException(
                $"{nameof(NavigationRail)} requires an {nameof(INavigationRailViewModel)}.");

        /// <summary>
        /// Gets a value indicating whether this component currently uses its icon-first presentation.
        /// </summary>
        private bool IsCollapsed => this.RequiredViewModel.PresentationMode switch
        {
            NavigationRailPresentationMode.Expanded => false,
            NavigationRailPresentationMode.Collapsed => true,
            NavigationRailPresentationMode.ExpandOnHover => !this.isPointerOver,
            _ => throw CreateInvalidPresentationModeException(this.RequiredViewModel.PresentationMode)
        };

        /// <summary>
        /// Gets or sets the accessible label of the navigation region.
        /// </summary>
        [Parameter]
        public string AriaLabel { get; set; } = "Workspace navigation";

        /// <summary>
        /// Gets or sets the callback invoked when fixed or transient presentation changes the rail's effective width.
        /// </summary>
        [Parameter]
        public EventCallback<bool> EffectiveCollapsedChanged { get; set; }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (this.isDisposed
                || this.ViewModel is null
                || !this.EffectiveCollapsedChanged.HasDelegate)
            {
                return;
            }

            var isCollapsed = this.IsCollapsed;

            if (this.reportedCollapsedState == isCollapsed)
            {
                return;
            }

            this.reportedCollapsedState = isCollapsed;
            await this.EffectiveCollapsedChanged.InvokeAsync(isCollapsed);
        }

        /// <summary>
        /// Releases component-owned reactive observation without disposing the caller-owned ViewModel.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true" /> to release managed resources; otherwise, <see langword="false" />.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;

            base.Dispose(disposing);
        }

        /// <summary>
        /// Gets the final CSS class list applied to the rail.
        /// </summary>
        /// <returns>The navigation-rail CSS class list.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass(
                "mb-navigation-rail",
                CssClassBuilder.When("mb-navigation-rail--collapsed", this.IsCollapsed));
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
            return this.IsCollapsed ? item.Label : null;
        }

        /// <summary>
        /// Gets the label of a presentation mode.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <returns>The user-facing mode label.</returns>
        private static string GetPresentationModeLabel(NavigationRailPresentationMode mode)
        {
            return mode switch
            {
                NavigationRailPresentationMode.Expanded => "Expanded",
                NavigationRailPresentationMode.Collapsed => "Collapsed",
                NavigationRailPresentationMode.ExpandOnHover => "Expand on hover",
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }

        /// <summary>
        /// Gets the accessible label and pointer hint for the sidebar-control button.
        /// </summary>
        /// <returns>The primary toggle action and context-menu hint.</returns>
        private string GetSidebarControlLabel()
        {
            var action = this.IsCollapsed ? "Expand" : "Collapse";

            return $"{action} workspace navigation; right-click for sidebar controls";
        }

        /// <summary>
        /// Determines whether a destination owns the selected state.
        /// </summary>
        /// <param name="item">The destination.</param>
        /// <returns>True when the destination is selected.</returns>
        private bool IsSelected(NavigationRailItem item)
        {
            return string.Equals(
                item.Id,
                this.RequiredViewModel.SelectedItem?.Id,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether a divider belongs immediately before an item.
        /// </summary>
        /// <param name="itemIndex">The item index in selector-defined order.</param>
        /// <returns>True when the item's group differs from the preceding item's group.</returns>
        private bool StartsNewGroup(int itemIndex)
        {
            var items = this.RequiredViewModel.NavigationItems;

            return itemIndex > 0
                && !string.Equals(
                    items[itemIndex - 1].GroupKey,
                    items[itemIndex].GroupKey,
                    StringComparison.Ordinal);
        }

        /// <summary>
        /// Assigns the selected destination through the ViewModel contract.
        /// </summary>
        /// <param name="item">The destination selected by the user.</param>
        private void SelectItem(NavigationRailItem item)
        {
            this.RequiredViewModel.SelectedItem = item;
        }

        /// <summary>
        /// Switches the persistent presentation between expanded and collapsed.
        /// </summary>
        private void TogglePresentation()
        {
            this.RequiredViewModel.PresentationMode = this.IsCollapsed
                ? NavigationRailPresentationMode.Expanded
                : NavigationRailPresentationMode.Collapsed;
        }

        /// <summary>
        /// Applies the primary sidebar-control action without opening its secondary menu.
        /// </summary>
        private void HandleSidebarControlPrimaryClick()
        {
            this.TogglePresentation();
            this.isSidebarControlMenuOpen = false;
        }

        /// <summary>
        /// Opens the trigger-anchored sidebar control menu for a context-menu request.
        /// </summary>
        private void HandleSidebarControlMenuRequested()
        {
            this.isSidebarControlMenuOpen = true;
        }

        /// <summary>
        /// Reconciles menu dismissal and keyboard-open requests from Blueprint.
        /// </summary>
        /// <param name="isOpen">Whether the sidebar control menu should be open.</param>
        private void HandleSidebarControlMenuOpenChanged(bool isOpen)
        {
            this.isSidebarControlMenuOpen = isOpen;
        }

        /// <summary>
        /// Assigns the persistent presentation mode through the ViewModel contract.
        /// </summary>
        /// <param name="mode">The presentation mode selected by the user.</param>
        private void SetPresentationMode(NavigationRailPresentationMode mode)
        {
            this.RequiredViewModel.PresentationMode = mode;
        }

        /// <summary>
        /// Applies this component instance's transient pointer-enter state.
        /// </summary>
        private void HandlePointerEntered()
        {
            this.isPointerOver = true;
        }

        /// <summary>
        /// Applies this component instance's transient pointer-leave state.
        /// </summary>
        private void HandlePointerExited()
        {
            this.isPointerOver = false;
        }

        /// <summary>
        /// Gets the CSS class list for a sidebar-control option.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <returns>The option CSS class list.</returns>
        private string GetSidebarControlItemCssClass(NavigationRailPresentationMode mode)
        {
            return CssClassBuilder.Build(
                "mb-navigation-rail__control-option",
                CssClassBuilder.When(
                    "mb-navigation-rail__control-option--selected",
                    this.IsPresentationModeSelected(mode)));
        }

        /// <summary>
        /// Determines whether a presentation mode represents the reactive state.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <returns>True when the mode is selected.</returns>
        private bool IsPresentationModeSelected(NavigationRailPresentationMode mode)
        {
            return mode == this.RequiredViewModel.PresentationMode;
        }

        /// <summary>
        /// Creates the exception used when a presentation mode is unsupported.
        /// </summary>
        /// <param name="presentationMode">The unsupported presentation mode.</param>
        /// <returns>The exception describing the unsupported presentation mode.</returns>
        private static ArgumentOutOfRangeException CreateInvalidPresentationModeException(
            NavigationRailPresentationMode presentationMode)
        {
            return new ArgumentOutOfRangeException(nameof(presentationMode), presentationMode, null);
        }

    }
}
