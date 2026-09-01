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
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    /// <summary>
    /// Renders a reusable tree browser for a loaded SysML project model.
    /// </summary>
    public sealed partial class ProjectBrowser : BloomReactiveComponentBase<IProjectBrowserViewModel>
    {
        /// <summary>
        /// A value indicating whether the component has been disposed.
        /// </summary>
        private bool isDisposed;

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

            if (node.HasChildren)
            {
                viewModel.ToggleNode(node);
            }

            viewModel.SelectNode(node);
            await this.SelectedNodeChanged.InvokeAsync(node);
        }
    }
}
