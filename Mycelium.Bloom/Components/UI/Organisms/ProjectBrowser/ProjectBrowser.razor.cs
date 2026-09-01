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
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    /// <summary>
    /// Renders a reusable tree browser for a loaded SysML project model.
    /// </summary>
    public sealed partial class ProjectBrowser : BloomReactiveComponentBase<IProjectBrowserViewModel>
    {
        /// <summary>
        /// The complete set of broad element-kind filter choices.
        /// </summary>
        private static readonly IReadOnlyList<SysmlModelElementKind> elementKindOptions =
            Enum.GetValues<SysmlModelElementKind>();

        /// <summary>
        /// The unique identifier of the filter drawer heading.
        /// </summary>
        private readonly string filterDrawerHeadingId = $"mb-project-browser-filter-heading-{Guid.NewGuid():N}";

        /// <summary>
        /// The unique identifier of the Type filter section heading.
        /// </summary>
        private readonly string typeFilterHeadingId = $"mb-project-browser-type-filter-heading-{Guid.NewGuid():N}";

        /// <summary>
        /// The focus target inside the portalled filter drawer.
        /// </summary>
        private ElementReference filterDrawerReference;

        /// <summary>
        /// A value indicating whether the component has been disposed.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// A value indicating whether this rendered browser's filter drawer is open.
        /// </summary>
        private bool isFilterDrawerOpen;

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
        /// <returns>The number of selected element kinds.</returns>
        private int GetActiveDrawerFilterCount()
        {
            return this.RequiredViewModel.SelectedElementKinds.Count;
        }

        /// <summary>
        /// Gets the Figma-aligned notation for a broad element kind.
        /// </summary>
        /// <param name="elementKind">The broad element kind.</param>
        /// <returns>The element-kind chip label.</returns>
        private static string GetElementKindLabel(SysmlModelElementKind elementKind)
        {
            return $"«{elementKind.ToString().ToLowerInvariant()}»";
        }

        /// <summary>
        /// Gets a value indicating whether an element kind is selected in the owning ViewModel.
        /// </summary>
        /// <param name="elementKind">The element kind to inspect.</param>
        /// <returns><see langword="true" /> when the kind is selected; otherwise, <see langword="false" />.</returns>
        private bool IsElementKindSelected(SysmlModelElementKind elementKind)
        {
            return this.RequiredViewModel.SelectedElementKinds.Contains(elementKind);
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
        /// Forwards a text-filter change to the owning ViewModel.
        /// </summary>
        /// <param name="filterText">The updated text filter.</param>
        /// <returns>A completed task.</returns>
        private Task HandleFilterTextChangedAsync(string filterText)
        {
            if (!this.isDisposed)
            {
                this.RequiredViewModel.FilterText = filterText ?? string.Empty;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Toggles one selected element-kind value through the owning ViewModel.
        /// </summary>
        /// <param name="elementKind">The element kind requested by the chip.</param>
        /// <returns>A completed task.</returns>
        private Task HandleElementKindFilterToggledAsync(SysmlModelElementKind elementKind)
        {
            if (!this.isDisposed)
            {
                this.RequiredViewModel.ToggleElementKindFilter(elementKind);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Clears both filter criteria through the owning ViewModel.
        /// </summary>
        /// <returns>A completed task.</returns>
        private Task HandleClearFilterAsync()
        {
            if (!this.isDisposed)
            {
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
