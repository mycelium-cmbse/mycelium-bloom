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
    using System.Collections.Specialized;
    using System.Reactive.Linq;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using ReactiveUI;
    using ReactiveUI.Primitives;

    /// <summary>
    /// Renders a reusable tree browser for a loaded SysML project model.
    /// </summary>
    public sealed partial class ProjectBrowser : BloomReactiveComponentBase<IProjectBrowserViewModel>
    {
        /// <summary>
        /// Cancels component-owned initialization when the component is disposed.
        /// </summary>
        private CancellationTokenSource initializationCancellation;

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
        /// Creates component-owned initialization cancellation and observes the stable root collection while active.
        /// </summary>
        protected override void OnInitialized()
        {
            this.initializationCancellation = new CancellationTokenSource();

            this.WhenActivated(disposables =>
            {
                INotifyCollectionChanged rootNodes = this.RequiredViewModel.RootNodes;

                var rootChanges = Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                    handler => rootNodes.CollectionChanged += handler,
                    handler => rootNodes.CollectionChanged -= handler);
                var renderRequests = System.Reactive.Linq.Observable.SelectMany(
                    rootChanges,
                    _ => Observable.FromAsync(this.QueueRenderAsync));

                System.ObservableExtensions.Subscribe(renderRequests)
                    .DisposeWith(disposables);
            });

            base.OnInitialized();
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
                await viewModel.InitializeAsync(this.initializationCancellation.Token);
            }
            catch (Exception)
            {
                // The ViewModel owns genuine loading errors; this boundary protects the Blazor circuit.
            }
        }

        /// <summary>
        /// Cancels initialization and releases component-owned reactive subscriptions.
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

            if (!disposing)
            {
                base.Dispose(false);

                return;
            }

            try
            {
                this.initializationCancellation?.Cancel();
            }
            finally
            {
                try
                {
                    base.Dispose(true);
                }
                finally
                {
                    this.initializationCancellation?.Dispose();
                }
            }
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
        /// Dispatches a root-collection refresh through the Blazor renderer while the component remains active.
        /// </summary>
        /// <returns>A task representing the dispatched render.</returns>
        private Task QueueRenderAsync()
        {
            if (this.isDisposed)
            {
                return Task.CompletedTask;
            }

            return this.InvokeAsync(() =>
            {
                if (!this.isDisposed)
                {
                    this.StateHasChanged();
                }
            });
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
