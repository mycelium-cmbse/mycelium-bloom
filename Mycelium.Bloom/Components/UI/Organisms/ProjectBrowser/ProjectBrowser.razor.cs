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
    using System.ComponentModel;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    /// <summary>
    /// Renders a reusable tree browser for a loaded SysML project model.
    /// </summary>
    public sealed partial class ProjectBrowser : BloomComponentBase, IDisposable
    {
        /// <summary>
        /// Cancels component-owned initialization when the component is disposed.
        /// </summary>
        private CancellationTokenSource initializationCancellation;

        /// <summary>
        /// The runtime scalar-state notification source, when the injected implementation provides one.
        /// </summary>
        private INotifyPropertyChanged notifyingViewModel;

        /// <summary>
        /// The stable root collection notification source.
        /// </summary>
        private INotifyCollectionChanged notifyingRootNodes;

        /// <summary>
        /// A value indicating whether the component has been disposed.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// Gets or sets the project browser ViewModel.
        /// </summary>
        [Inject]
        public IProjectBrowserViewModel ViewModel { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the selected node changes.
        /// </summary>
        [Parameter]
        public EventCallback<ProjectBrowserNodeViewModel> SelectedNodeChanged { get; set; }

        /// <summary>
        /// Subscribes once to scalar ViewModel state and the stable root collection.
        /// </summary>
        protected override void OnInitialized()
        {
            this.initializationCancellation = new CancellationTokenSource();

            this.notifyingViewModel = this.ViewModel as INotifyPropertyChanged;

            if (this.notifyingViewModel != null)
            {
                this.notifyingViewModel.PropertyChanged += this.HandleViewModelPropertyChanged;
            }

            this.notifyingRootNodes = this.ViewModel.RootNodes;

            if (this.notifyingRootNodes != null)
            {
                this.notifyingRootNodes.CollectionChanged += this.HandleRootNodesCollectionChanged;
            }

            base.OnInitialized();
        }

        /// <summary>
        /// Initializes the project browser view model owned by this component.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async Task OnInitializedAsync()
        {
            if (this.isDisposed
                || this.ViewModel.IsLoaded
                || this.ViewModel.IsLoading
                || !string.IsNullOrWhiteSpace(this.ViewModel.ErrorMessage))
            {
                return;
            }

            try
            {
                await this.ViewModel.InitializeAsync(this.initializationCancellation.Token);
            }
            catch (Exception)
            {
                // The ViewModel owns genuine loading errors; this boundary protects the Blazor circuit.
            }
        }

        /// <summary>
        /// Cancels initialization and releases component-owned subscriptions.
        /// </summary>
        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;

            try
            {
                this.initializationCancellation?.Cancel();
            }
            finally
            {
                if (this.notifyingViewModel != null)
                {
                    this.notifyingViewModel.PropertyChanged -= this.HandleViewModelPropertyChanged;
                }

                if (this.notifyingRootNodes != null)
                {
                    this.notifyingRootNodes.CollectionChanged -= this.HandleRootNodesCollectionChanged;
                }

                try
                {
                    this.ViewModel?.Dispose();
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
        /// Queues a renderer-safe refresh for relevant scalar ViewModel state.
        /// </summary>
        /// <param name="sender">The notification source.</param>
        /// <param name="eventArgs">The changed property.</param>
        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            if (string.IsNullOrEmpty(eventArgs.PropertyName)
                || eventArgs.PropertyName is nameof(IProjectBrowserViewModel.IsLoading)
                    or nameof(IProjectBrowserViewModel.IsLoaded)
                    or nameof(IProjectBrowserViewModel.ErrorMessage)
                    or nameof(IProjectBrowserViewModel.SelectedNode))
            {
                this.QueueRender();
            }
        }

        /// <summary>
        /// Queues a renderer-safe refresh when the stable root collection changes.
        /// </summary>
        /// <param name="sender">The notification source.</param>
        /// <param name="eventArgs">The collection change.</param>
        private void HandleRootNodesCollectionChanged(object sender, NotifyCollectionChangedEventArgs eventArgs)
        {
            this.QueueRender();
        }

        /// <summary>
        /// Dispatches a render only while this component remains alive.
        /// </summary>
        private void QueueRender()
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

        /// <summary>
        /// Checks whether the loading state should be rendered.
        /// </summary>
        /// <returns>A value indicating whether the loading state should be shown.</returns>
        private bool ShouldShowLoadingState()
        {
            return this.ViewModel.IsLoading
                   || (!this.ViewModel.IsLoaded && string.IsNullOrWhiteSpace(this.ViewModel.ErrorMessage));
        }

        /// <summary>
        /// Checks whether the error state should be rendered.
        /// </summary>
        /// <returns>A value indicating whether the error state should be shown.</returns>
        private bool ShouldShowErrorState()
        {
            return !string.IsNullOrWhiteSpace(this.ViewModel.ErrorMessage);
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

            if (node.HasChildren)
            {
                this.ViewModel.ToggleNode(node);
            }

            this.ViewModel.SelectNode(node);
            await this.SelectedNodeChanged.InvokeAsync(node);
        }
    }
}
