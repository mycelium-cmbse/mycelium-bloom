// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowser.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser
{
    using System.Reactive;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    using DynamicData.Binding;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    /// <summary>
    /// Renders a reusable tree browser for a loaded SysML project model.
    /// </summary>
    public sealed partial class ProjectBrowser : BloomReactiveComponentBase<IProjectBrowserViewModel>
    {
        /// <summary>
        /// The unique identifier of the filter drawer heading.
        /// </summary>
        private readonly string filterDrawerHeadingId = $"mb-project-browser-filter-heading-{Guid.NewGuid():N}";

        /// <summary>
        /// The unique identifier of the Type filter section heading.
        /// </summary>
        private readonly string typeFilterHeadingId = $"mb-project-browser-type-filter-heading-{Guid.NewGuid():N}";

        /// <summary>
        /// Replaces the collection-change subscription when the caller supplies another ViewModel.
        /// </summary>
        private readonly SerialDisposable collectionChangesSubscription = new();

        /// <summary>
        /// The focus target inside the portalled filter drawer.
        /// </summary>
        private ElementReference filterDrawerReference;

        /// <summary>
        /// The presentation-only reset signal for the search assistant's transient draft.
        /// </summary>
        private int searchAssistantDraftResetVersion;

        /// <summary>
        /// A value indicating whether the component has been disposed.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// A value indicating whether this rendered browser's filter drawer is open.
        /// </summary>
        private bool isFilterDrawerOpen;

        /// <summary>
        /// A value indicating whether this rendered browser's transient search assistant is open.
        /// </summary>
        private bool isSearchAssistantOpen;

        /// <summary>
        /// The ViewModel whose observable collections currently drive rendering.
        /// </summary>
        private IProjectBrowserViewModel observedViewModel;

        /// <summary>
        /// Gets the caller-supplied ViewModel required by this component.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the Project Browser ViewModel has not been supplied.
        /// </exception>
        private IProjectBrowserViewModel RequiredViewModel =>
            this.ViewModel
            ?? throw new InvalidOperationException(
                $"{nameof(ProjectBrowser)} requires an {nameof(IProjectBrowserViewModel)}.");

        /// <summary>
        /// Gets or sets the callback invoked when the selected node changes.
        /// </summary>
        [Parameter]
        public EventCallback<ProjectBrowserNodeViewModel> SelectedNodeChanged { get; set; }

        /// <summary>
        /// Connects rendering to the observable collections exposed by the current ViewModel.
        /// </summary>
        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (ReferenceEquals(this.observedViewModel, this.ViewModel))
            {
                return;
            }

            this.observedViewModel = this.ViewModel;
            this.collectionChangesSubscription.Disposable = this.ViewModel is null
                ? Disposable.Empty
                : Observable.Merge(
                        this.ViewModel.RootNodes.ToObservableChangeSet().Skip(1).Select(_ => Unit.Default),
                        this.ViewModel.AvailableElementTypes.ToObservableChangeSet().Skip(1).Select(_ => Unit.Default),
                        this.ViewModel.SelectedElementTypes.ToObservableChangeSet().Skip(1).Select(_ => Unit.Default))
                    .Select(_ => Observable.FromAsync(() => this.InvokeAsync(this.StateHasChanged)))
                    .Concat()
                    .Subscribe();
        }

        /// <summary>
        /// Initializes the configured project browser ViewModel.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async Task OnInitializedAsync()
        {
            if (this.isDisposed)
            {
                return;
            }

            var viewModel = this.RequiredViewModel;

            if (viewModel.IsLoaded
                || viewModel.IsLoading
                || !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))
            {
                return;
            }

            try
            {
                await viewModel.InitializeAsync(CancellationToken.None);
            }
            catch (Exception)
            {
                // The ViewModel owns genuine loading errors; this boundary protects the Blazor circuit.
            }
        }

        /// <summary>
        /// Releases component-owned reactive subscriptions.
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
            this.isFilterDrawerOpen = false;
            this.isSearchAssistantOpen = false;
            this.collectionChangesSubscription.Dispose();

            base.Dispose(disposing);
        }

        /// <summary>
        /// Gets the final CSS class list applied to the project browser.
        /// </summary>
        /// <returns>The project browser CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = this.BuildRootCssClass("mb-project-browser");

            return cssClass;
        }

        /// <summary>
        /// Checks whether the loading state should be rendered.
        /// </summary>
        /// <returns>A value indicating whether the loading state should be shown.</returns>
        private bool ShouldShowLoadingState()
        {
            var viewModel = this.RequiredViewModel;

            return viewModel.IsLoading
                   || (!viewModel.IsLoaded && string.IsNullOrWhiteSpace(viewModel.ErrorMessage));
        }

        /// <summary>
        /// Checks whether the error state should be rendered.
        /// </summary>
        /// <returns>A value indicating whether the error state should be shown.</returns>
        private bool ShouldShowErrorState()
        {
            return !string.IsNullOrWhiteSpace(this.RequiredViewModel.ErrorMessage);
        }

        /// <summary>
        /// Gets the canonical root nodes visible in the supplied filter presentation.
        /// </summary>
        /// <param name="filterPresentation">The immutable filter presentation to apply.</param>
        /// <returns>The visible root nodes in canonical order.</returns>
        private IReadOnlyList<ProjectBrowserNodeViewModel> GetVisibleRootNodes(
            ProjectBrowserFilterPresentation filterPresentation)
        {
            var rootNodes = this.RequiredViewModel.RootNodes;

            if (!filterPresentation.IsActive)
            {
                return rootNodes;
            }

            return rootNodes.Where(filterPresentation.IsVisible).ToArray();
        }

        /// <summary>
        /// Gets the number of active selectable values represented in the filter drawer.
        /// </summary>
        /// <returns>The number of selected element types.</returns>
        private int GetActiveDrawerFilterCount()
        {
            return this.RequiredViewModel.SelectedElementTypes.Count;
        }

        /// <summary>
        /// Gets the Figma-aligned notation for a concrete model element type.
        /// </summary>
        /// <param name="elementType">The concrete model element type.</param>
        /// <returns>The element-type chip label.</returns>
        internal static string GetElementTypeLabel(Type elementType)
        {
            ArgumentNullException.ThrowIfNull(elementType);

            return $"«{elementType.Name.ToLowerInvariant()}»";
        }

        /// <summary>
        /// Gets a value indicating whether an element type is selected in the owning ViewModel.
        /// </summary>
        /// <param name="elementType">The element type to inspect.</param>
        /// <returns><see langword="true" /> when the type is selected; otherwise, <see langword="false" />.</returns>
        private bool IsElementTypeSelected(Type elementType)
        {
            return this.RequiredViewModel.SelectedElementTypes.Contains(elementType);
        }

        /// <summary>
        /// Tracks controlled open-state changes requested by the anchored filter popover.
        /// </summary>
        /// <param name="isOpen">Whether the filter drawer should be open.</param>
        private void HandleFilterDrawerOpenChanged(bool isOpen)
        {
            if (!this.isDisposed)
            {
                this.isFilterDrawerOpen = isOpen;

                if (isOpen)
                {
                    this.isSearchAssistantOpen = false;
                }
            }
        }

        /// <summary>
        /// Closes the search assistant before Blueprint toggles the complete filter drawer.
        /// </summary>
        /// <returns>A completed task.</returns>
        private Task HandleFilterDrawerTriggerClickAsync()
        {
            if (!this.isDisposed)
            {
                this.isSearchAssistantOpen = false;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Tracks controlled open-state changes requested by the search assistant.
        /// </summary>
        /// <param name="isOpen">Whether the search assistant should be open.</param>
        private void HandleSearchAssistantOpenChanged(bool isOpen)
        {
            if (!this.isDisposed)
            {
                this.isSearchAssistantOpen = isOpen;

                if (isOpen)
                {
                    this.isFilterDrawerOpen = false;
                }
            }
        }

        /// <summary>
        /// Moves keyboard focus into the drawer after Blueprint has mounted and positioned its content.
        /// </summary>
        /// <returns>A task representing the focus operation.</returns>
        private async Task HandleFilterDrawerContentReadyAsync()
        {
            if (!this.isDisposed && this.isFilterDrawerOpen)
            {
                await this.filterDrawerReference.FocusAsync(preventScroll: true);
            }
        }

        /// <summary>
        /// Closes the transient filter drawer while retaining ViewModel-owned criteria.
        /// </summary>
        /// <returns>A completed task.</returns>
        private Task HandleFilterDrawerCloseAsync()
        {
            if (!this.isDisposed)
            {
                this.isFilterDrawerOpen = false;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Commits one Contains criterion to the owning ViewModel.
        /// </summary>
        /// <param name="filterText">The normalized committed text.</param>
        /// <returns>A completed task.</returns>
        private Task HandleContainsCommittedAsync(string filterText)
        {
            if (!this.isDisposed)
            {
                this.isSearchAssistantOpen = false;
                this.RequiredViewModel.FilterText = filterText ?? string.Empty;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Removes the committed Contains criterion from the owning ViewModel.
        /// </summary>
        /// <returns>A completed task.</returns>
        private Task HandleContainsRemovedAsync()
        {
            if (!this.isDisposed)
            {
                this.RequiredViewModel.FilterText = string.Empty;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Toggles one selected element type through the owning ViewModel.
        /// </summary>
        /// <param name="elementType">The element type requested by the chip.</param>
        /// <returns>A completed task.</returns>
        private Task HandleElementTypeFilterToggledAsync(Type elementType)
        {
            if (!this.isDisposed)
            {
                this.isSearchAssistantOpen = false;
                this.RequiredViewModel.ToggleElementTypeFilter(elementType);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Clears both filter criteria through the owning ViewModel.
        /// </summary>
        /// <returns>A task representing the coherent reset.</returns>
        private Task HandleClearFilterAsync()
        {
            if (!this.isDisposed)
            {
                this.isSearchAssistantOpen = false;
                this.searchAssistantDraftResetVersion++;
                this.RequiredViewModel.ClearFilter();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles node selection and forwards the selected node to the parent component.
        /// </summary>
        /// <param name="node">The selected project browser node.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleNodeSelectedAsync(ProjectBrowserNodeViewModel node)
        {
            if (node == null || this.isDisposed)
            {
                return;
            }

            var viewModel = this.RequiredViewModel;

            if (node.HasChildren && !viewModel.FilterPresentation.IsActive)
            {
                viewModel.ToggleNode(node);
            }

            viewModel.SelectNode(node);
            await this.SelectedNodeChanged.InvokeAsync(node);
        }
    }
}
