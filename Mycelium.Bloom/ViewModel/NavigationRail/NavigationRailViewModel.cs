// ------------------------------------------------------------------------------------------------
// <copyright file="NavigationRailViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.NavigationRail
{
    using System.Collections.ObjectModel;

    using DynamicData;
    using DynamicData.Binding;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    using ReactiveUI;

    /// <summary>
    /// Owns reactive destination, selection, and presentation state for one navigation rail.
    /// </summary>
    public sealed class NavigationRailViewModel : ReactiveObject, INavigationRailViewModel
    {
        /// <summary>
        /// The mutable destination source owned exclusively by this ViewModel.
        /// </summary>
        private readonly SourceList<NavigationRailItem> navigationItemSource = new();

        /// <summary>
        /// The function that derives destinations from navigation-local context.
        /// </summary>
        private readonly Func<NavigationRailContext, IReadOnlyList<NavigationRailItem>> navigationItemSelector;

        /// <summary>
        /// The read-only destination projection bound from <see cref="navigationItemSource" />.
        /// </summary>
        private readonly ReadOnlyObservableCollection<NavigationRailItem> navigationItems;

        /// <summary>
        /// Keeps the DynamicData rendering projection alive until final disposal.
        /// </summary>
        private readonly IDisposable navigationItemsBinding;

        /// <summary>
        /// Reacts to navigation-local context until final disposal.
        /// </summary>
        private readonly IDisposable contextSubscription;

        /// <summary>
        /// The selected destination identifier.
        /// </summary>
        private string selectedItemId = string.Empty;

        /// <summary>
        /// A value indicating whether hover mode is temporarily expanded.
        /// </summary>
        private bool hoverExpansionActive;

        /// <summary>
        /// A value indicating whether final disposal has occurred.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationRailViewModel" /> class.
        /// </summary>
        /// <param name="contextChanges">The observable navigation-local context.</param>
        /// <param name="navigationItemSelector">Derives the complete destination inventory for a context.</param>
        public NavigationRailViewModel(
            IObservable<NavigationRailContext> contextChanges,
            Func<NavigationRailContext, IReadOnlyList<NavigationRailItem>> navigationItemSelector)
        {
            ArgumentNullException.ThrowIfNull(contextChanges);
            ArgumentNullException.ThrowIfNull(navigationItemSelector);

            this.navigationItemSelector = navigationItemSelector;
            this.navigationItemsBinding = System.ObservableExtensions.Subscribe(
                this.navigationItemSource.Connect().Bind(
                    out var boundNavigationItems,
                    new BindingOptions(0, true, true)));
            this.navigationItems = boundNavigationItems;
            this.contextSubscription = System.ObservableExtensions.Subscribe(
                contextChanges,
                this.ApplyContext);
        }

        /// <inheritdoc />
        public ReadOnlyObservableCollection<NavigationRailItem> NavigationItems => this.navigationItems;

        /// <inheritdoc />
        public string SelectedItemId
        {
            get => this.selectedItemId;
            private set => this.RaiseAndSetIfChanged(ref this.selectedItemId, value);
        }

        /// <inheritdoc />
        public NavigationRailPresentationMode PresentationMode
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        } = NavigationRailPresentationMode.Collapsed;

        /// <inheritdoc />
        public bool IsCollapsed => this.PresentationMode switch
        {
            NavigationRailPresentationMode.Expanded => false,
            NavigationRailPresentationMode.Collapsed => true,
            NavigationRailPresentationMode.ExpandOnHover => !this.hoverExpansionActive,
            _ => throw CreateInvalidPresentationModeException(this.PresentationMode)
        };

        /// <inheritdoc />
        public void SelectItem(string itemId)
        {
            if (this.isDisposed || string.IsNullOrWhiteSpace(itemId))
            {
                return;
            }

            if (this.NavigationItems.Any(item => string.Equals(item.Id, itemId, StringComparison.Ordinal)))
            {
                this.SelectedItemId = itemId;
            }
        }

        /// <inheritdoc />
        public void TogglePresentation()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.SetPresentationMode(
                this.IsCollapsed
                    ? NavigationRailPresentationMode.Expanded
                    : NavigationRailPresentationMode.Collapsed);
        }

        /// <inheritdoc />
        public void SetPresentationMode(NavigationRailPresentationMode mode)
        {
            if (this.isDisposed)
            {
                return;
            }

            if (!Enum.IsDefined(mode))
            {
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }

            var wasCollapsed = this.IsCollapsed;
            this.hoverExpansionActive = false;
            this.PresentationMode = mode;

            if (wasCollapsed != this.IsCollapsed)
            {
                this.RaisePropertyChanged(nameof(this.IsCollapsed));
            }
        }

        /// <inheritdoc />
        public void HandlePointerEntered()
        {
            if (this.isDisposed
                || this.PresentationMode != NavigationRailPresentationMode.ExpandOnHover
                || this.hoverExpansionActive)
            {
                return;
            }

            this.hoverExpansionActive = true;
            this.RaisePropertyChanged(nameof(this.IsCollapsed));
        }

        /// <inheritdoc />
        public void HandlePointerExited()
        {
            if (this.isDisposed || !this.hoverExpansionActive)
            {
                return;
            }

            this.hoverExpansionActive = false;
            this.RaisePropertyChanged(nameof(this.IsCollapsed));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;
            this.contextSubscription.Dispose();
            this.navigationItemsBinding.Dispose();
            this.navigationItemSource.Dispose();
        }

        private static ArgumentOutOfRangeException CreateInvalidPresentationModeException(
            NavigationRailPresentationMode presentationMode)
        {
            return new ArgumentOutOfRangeException(nameof(presentationMode), presentationMode, null);
        }

        /// <summary>
        /// Derives and publishes one coherent destination and selection snapshot from contextual state.
        /// </summary>
        /// <param name="context">The updated navigation-local context.</param>
        private void ApplyContext(NavigationRailContext context)
        {
            if (this.isDisposed)
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(context);

            var selectedItems = this.navigationItemSelector(context);
            ArgumentNullException.ThrowIfNull(selectedItems);

            var nextItems = selectedItems.ToArray();
            var nextSelectedItemId = GetReconciledSelectedItemId(nextItems, this.SelectedItemId);
            var selectionChanged = !string.Equals(
                this.SelectedItemId,
                nextSelectedItemId,
                StringComparison.Ordinal);

            if (selectionChanged)
            {
                this.RaisePropertyChanging(nameof(this.SelectedItemId));
                // Assign directly so selection notifications bracket the collection update as one coherent snapshot.
                this.selectedItemId = nextSelectedItemId;
            }

            this.navigationItemSource.Edit(items =>
            {
                items.Clear();
                items.AddRange(nextItems);
            });

            if (selectionChanged)
            {
                this.RaisePropertyChanged(nameof(this.SelectedItemId));
            }
        }

        /// <summary>
        /// Reconciles selection against one materialized destination snapshot.
        /// </summary>
        /// <param name="items">The complete next destination snapshot.</param>
        /// <param name="currentSelectedItemId">The currently selected destination identifier.</param>
        /// <returns>The selected identifier valid for the next snapshot.</returns>
        private static string GetReconciledSelectedItemId(
            IReadOnlyList<NavigationRailItem> items,
            string currentSelectedItemId)
        {
            if (items.Count == 0)
            {
                return string.Empty;
            }

            if (items.Any(item => string.Equals(
                    item.Id,
                    currentSelectedItemId,
                    StringComparison.Ordinal)))
            {
                return currentSelectedItemId;
            }

            return items[0].Id;
        }
    }
}
