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
    using System.Diagnostics.CodeAnalysis;

    using DynamicData;
    using DynamicData.Binding;

    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    using ReactiveUI;

    using SysML2.NET.Core.POCO.Root.Elements;

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
        private readonly Func<ProjectLifecycleState, IElement, IReadOnlyList<NavigationRailItem>> navigationItemSelector;

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
        /// The selected destination from the current inventory.
        /// </summary>
        [AllowNull]
        [MaybeNull]
        private NavigationRailItem selectedItem;

        /// <summary>
        /// A value indicating whether final disposal has occurred.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationRailViewModel" /> class.
        /// </summary>
        /// <param name="contextAwareService">The shared reactive application context.</param>
        /// <param name="navigationItemSelector">Derives the complete destination inventory from contextual values.</param>
        public NavigationRailViewModel(
            IContextAwareService contextAwareService,
            Func<ProjectLifecycleState, IElement, IReadOnlyList<NavigationRailItem>> navigationItemSelector)
        {
            ArgumentNullException.ThrowIfNull(contextAwareService);
            ArgumentNullException.ThrowIfNull(navigationItemSelector);

            this.navigationItemSelector = navigationItemSelector;
            this.navigationItemsBinding = System.ObservableExtensions.Subscribe(
                this.navigationItemSource.Connect().Bind(
                    out var boundNavigationItems,
                    new BindingOptions(0, true, true)));
            this.navigationItems = boundNavigationItems;
            this.contextSubscription = System.ObservableExtensions.Subscribe(
                contextAwareService.WhenAnyValue(
                    context => context.LifecycleState,
                    context => context.SelectedElement),
                context => this.ApplyContext(context.Item1, context.Item2));
        }

        /// <inheritdoc />
        public ReadOnlyObservableCollection<NavigationRailItem> NavigationItems => this.navigationItems;

        /// <inheritdoc />
        [AllowNull]
        [MaybeNull]
        public NavigationRailItem SelectedItem
        {
            get => this.selectedItem;
            set
            {
                if (this.isDisposed)
                {
                    return;
                }

                if (value is null)
                {
                    this.RaiseAndSetIfChanged(ref this.selectedItem, null);
                    return;
                }

                var canonicalItem = this.NavigationItems.FirstOrDefault(item => string.Equals(
                    item.Id,
                    value.Id,
                    StringComparison.Ordinal));

                if (canonicalItem is null)
                {
                    throw new ArgumentException(
                        $"Navigation item '{value.Id}' is not available in the current inventory.",
                        nameof(value));
                }

                this.RaiseAndSetIfChanged(ref this.selectedItem, canonicalItem);
            }
        }

        /// <inheritdoc />
        public NavigationRailPresentationMode PresentationMode
        {
            get;
            set
            {
                if (this.isDisposed)
                {
                    return;
                }

                if (!Enum.IsDefined(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }

                this.RaiseAndSetIfChanged(ref field, value);
            }
        } = NavigationRailPresentationMode.Collapsed;

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

        /// <summary>
        /// Derives and publishes one coherent destination and selection snapshot from contextual state.
        /// </summary>
        /// <param name="lifecycleState">The current project lifecycle state.</param>
        /// <param name="selectedElement">The currently selected model element, or <see langword="null" />.</param>
        private void ApplyContext(
            ProjectLifecycleState lifecycleState,
            [AllowNull] IElement selectedElement)
        {
            if (this.isDisposed)
            {
                return;
            }

            var selectedItems = this.navigationItemSelector(lifecycleState, selectedElement);
            ArgumentNullException.ThrowIfNull(selectedItems);

            var nextItems = selectedItems.ToArray();
            var nextSelectedItem = GetReconciledSelectedItem(nextItems, this.SelectedItem);
            var selectionChanged = !ReferenceEquals(this.SelectedItem, nextSelectedItem);

            if (selectionChanged)
            {
                this.RaisePropertyChanging(nameof(this.SelectedItem));
                // Direct assignment is intentional so selection notifications bracket inventory replacement as one coherent snapshot.
                this.selectedItem = nextSelectedItem;
            }

            this.navigationItemSource.Edit(items =>
            {
                items.Clear();
                items.AddRange(nextItems);
            });

            if (selectionChanged)
            {
                this.RaisePropertyChanged(nameof(this.SelectedItem));
            }
        }

        /// <summary>
        /// Reconciles selection against one materialized destination snapshot.
        /// </summary>
        /// <param name="items">The complete next destination snapshot.</param>
        /// <param name="currentSelectedItem">The currently selected destination.</param>
        /// <returns>The canonical selected item for the next snapshot, or <see langword="null" />.</returns>
        [return: MaybeNull]
        private static NavigationRailItem GetReconciledSelectedItem(
            NavigationRailItem[] items,
            [AllowNull] NavigationRailItem currentSelectedItem)
        {
            if (items.Length == 0)
            {
                return null;
            }

            if (currentSelectedItem is not null)
            {
                var canonicalItem = items.FirstOrDefault(item => string.Equals(
                    item.Id,
                    currentSelectedItem.Id,
                    StringComparison.Ordinal));

                if (canonicalItem is not null)
                {
                    return canonicalItem;
                }
            }

            return items[0];
        }
    }
}
