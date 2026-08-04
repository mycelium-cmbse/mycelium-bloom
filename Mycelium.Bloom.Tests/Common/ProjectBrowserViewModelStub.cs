// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserViewModelStub.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Common
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using ReactiveUI;
    using ReactiveUI.Primitives;

    /// <summary>
    /// Provides controllable reactive Project Browser state for component tests.
    /// </summary>
    internal sealed class ProjectBrowserViewModelStub : ReactiveObject, IProjectBrowserViewModel
    {
        /// <summary>
        /// The root nodes.
        /// </summary>
        private IReadOnlyList<ProjectBrowserNodeViewModel> rootNodes = [];

        /// <summary>
        /// The selected node.
        /// </summary>
        private ProjectBrowserNodeViewModel selectedNode;

        /// <summary>
        /// A value indicating whether the view model is loading.
        /// </summary>
        private bool isLoading;

        /// <summary>
        /// A value indicating whether the view model has loaded.
        /// </summary>
        private bool isLoaded;

        /// <summary>
        /// The loading error message.
        /// </summary>
        private string errorMessage = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectBrowserViewModelStub" /> class.
        /// </summary>
        internal ProjectBrowserViewModelStub()
        {
            this.Activator = new ViewModelActivator();
            this.InitializeCommand = ReactiveCommand.CreateFromTask(this.InitializeAsync);
            this.ToggleNodeCommand = ReactiveCommand.Create<ProjectBrowserNodeViewModel>(this.ToggleNode);
            this.SelectNodeCommand = ReactiveCommand.Create<ProjectBrowserNodeViewModel>(this.SelectNode);
        }

        /// <inheritdoc />
        public ViewModelActivator Activator { get; }

        /// <inheritdoc />
        public IReadOnlyList<ProjectBrowserNodeViewModel> RootNodes
        {
            get => this.rootNodes;
            set => this.RaiseAndSetIfChanged(ref this.rootNodes, value);
        }

        /// <inheritdoc />
        public ProjectBrowserNodeViewModel SelectedNode
        {
            get => this.selectedNode;
            private set => this.RaiseAndSetIfChanged(ref this.selectedNode, value);
        }

        /// <inheritdoc />
        public bool IsLoading
        {
            get => this.isLoading;
            set => this.RaiseAndSetIfChanged(ref this.isLoading, value);
        }

        /// <inheritdoc />
        public bool IsLoaded
        {
            get => this.isLoaded;
            set => this.RaiseAndSetIfChanged(ref this.isLoaded, value);
        }

        /// <inheritdoc />
        public string ErrorMessage
        {
            get => this.errorMessage;
            set => this.RaiseAndSetIfChanged(ref this.errorMessage, value);
        }

        /// <inheritdoc />
        public ReactiveCommand<RxVoid, RxVoid> InitializeCommand { get; }

        /// <inheritdoc />
        public ReactiveCommand<ProjectBrowserNodeViewModel, RxVoid> ToggleNodeCommand { get; }

        /// <inheritdoc />
        public ReactiveCommand<ProjectBrowserNodeViewModel, RxVoid> SelectNodeCommand { get; }

        /// <summary>
        /// Gets the number of times initialization was requested.
        /// </summary>
        public int InitializeAsyncCallCount { get; private set; }

        /// <summary>
        /// Gets or sets the handler invoked during initialization.
        /// </summary>
        public Func<Task> InitializeHandler { get; set; } = () => Task.CompletedTask;

        /// <summary>
        /// Applies a selected node for test setup.
        /// </summary>
        /// <param name="node">The node to select.</param>
        public void ApplySelection(ProjectBrowserNodeViewModel node)
        {
            this.SelectNode(node);
        }

        /// <summary>
        /// Runs controlled asynchronous initialization.
        /// </summary>
        private async Task InitializeAsync()
        {
            this.InitializeAsyncCallCount++;
            this.IsLoading = true;

            try
            {
                await this.InitializeHandler();
            }
            finally
            {
                this.IsLoading = false;
            }
        }

        /// <summary>
        /// Toggles a node for component interaction tests.
        /// </summary>
        /// <param name="node">The node to toggle.</param>
        private void ToggleNode(ProjectBrowserNodeViewModel node)
        {
            ArgumentNullException.ThrowIfNull(node);

            if (node.HasChildren)
            {
                node.IsExpanded = !node.IsExpanded;
            }
        }

        /// <summary>
        /// Selects a node for component interaction tests.
        /// </summary>
        /// <param name="node">The node to select.</param>
        private void SelectNode(ProjectBrowserNodeViewModel node)
        {
            ArgumentNullException.ThrowIfNull(node);

            if (this.SelectedNode != null)
            {
                this.SelectedNode.IsSelected = false;
            }

            node.IsSelected = true;
            this.SelectedNode = node;
        }
    }
}
