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
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;

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
        [
            NavigationRailPresentationMode.Expanded,
            NavigationRailPresentationMode.Collapsed,
            NavigationRailPresentationMode.ExpandOnHover
        ];

        /// <summary>
        /// The read-only destination projection currently observed by this component.
        /// </summary>
        private ReadOnlyObservableCollection<NavigationRailItem> observedNavigationItems;

        /// <summary>
        /// A value indicating whether component disposal has begun.
        /// </summary>
        private bool isDisposed;

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
        /// Gets or sets the accessible label of the navigation region.
        /// </summary>
        [Parameter]
        public string AriaLabel { get; set; } = "Workspace navigation";

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            this.ObserveNavigationItems(this.ViewModel?.NavigationItems);
        }

        /// <summary>
        /// Detaches component-owned collection observation without disposing the caller-owned ViewModel.
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

            if (disposing)
            {
                this.ObserveNavigationItems(null);
            }

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
                CssClassBuilder.When("mb-navigation-rail--collapsed", this.RequiredViewModel.IsCollapsed));
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
            return this.RequiredViewModel.IsCollapsed ? item.Label : null;
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
            var action = this.RequiredViewModel.IsCollapsed ? "Expand" : "Collapse";

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
                this.RequiredViewModel.SelectedItemId,
                StringComparison.Ordinal);
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
        /// Replaces the component-owned collection subscription when its ViewModel changes.
        /// </summary>
        /// <param name="navigationItems">The destination projection to observe, or <see langword="null" />.</param>
        private void ObserveNavigationItems(ReadOnlyObservableCollection<NavigationRailItem> navigationItems)
        {
            if (ReferenceEquals(this.observedNavigationItems, navigationItems))
            {
                return;
            }

            if (this.observedNavigationItems is INotifyCollectionChanged previousItems)
            {
                previousItems.CollectionChanged -= this.HandleNavigationItemsChanged;
            }

            this.observedNavigationItems = navigationItems;

            if (this.observedNavigationItems is INotifyCollectionChanged currentItems)
            {
                currentItems.CollectionChanged += this.HandleNavigationItemsChanged;
            }
        }

        /// <summary>
        /// Queues a render when the stable destination projection changes.
        /// </summary>
        /// <param name="sender">The observed destination projection.</param>
        /// <param name="args">The collection change.</param>
        private void HandleNavigationItemsChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (this.isDisposed)
            {
                return;
            }

            _ = this.InvokeAsync(() =>
            {
                if (!this.isDisposed)
                {
                    this.StateHasChanged();
                }
            });
        }
    }
}
