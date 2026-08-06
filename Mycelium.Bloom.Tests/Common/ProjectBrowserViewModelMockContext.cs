// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserViewModelMockContext.cs" company="Starion Group S.A.">
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
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Threading;
    using System.Threading.Tasks;

    using DynamicData;
    using DynamicData.Binding;

    using Moq;

    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using ReactiveUI;
    using ReactiveUI.Primitives;

    /// <summary>
    /// Configures a mocked Project Browser contract with reactive test state and real commands.
    /// </summary>
    internal sealed class ProjectBrowserViewModelMockContext : IDisposable
    {
        /// <summary>
        /// Owns the root nodes exposed by the mocked contract.
        /// </summary>
        private readonly SourceList<ProjectBrowserNodeViewModel> rootNodeSource = new();

        /// <summary>
        /// Binds <see cref="rootNodeSource" /> to the read-only exposed collection.
        /// </summary>
        private readonly IDisposable rootNodeBinding;

        /// <summary>
        /// The reactive interface added to the Moq proxy.
        /// </summary>
        private readonly IReactiveObject reactiveObject;

        /// <summary>
        /// The root nodes exposed by the mocked contract.
        /// </summary>
        private readonly ReadOnlyObservableCollection<ProjectBrowserNodeViewModel> rootNodes;

        /// <summary>
        /// The ViewModel activator exposed by the mock.
        /// </summary>
        private readonly ViewModelActivator activator;

        /// <summary>
        /// The real initialization command exposed by the mock.
        /// </summary>
        private readonly ReactiveCommand<RxVoid, bool> initializeCommand;

        /// <summary>
        /// The real toggle command exposed by the mock.
        /// </summary>
        private readonly ReactiveCommand<ProjectBrowserNodeViewModel, RxVoid> toggleNodeCommand;

        /// <summary>
        /// The real selection command exposed by the mock.
        /// </summary>
        private readonly ReactiveCommand<ProjectBrowserNodeViewModel, RxVoid> selectNodeCommand;

        /// <summary>
        /// The selected node backing value.
        /// </summary>
        private ProjectBrowserNodeViewModel selectedNode;

        /// <summary>
        /// The loading-state backing value.
        /// </summary>
        private bool isLoading;

        /// <summary>
        /// The loaded-state backing value.
        /// </summary>
        private bool isLoaded;

        /// <summary>
        /// The error-message backing value.
        /// </summary>
        private string errorMessage = string.Empty;

        /// <summary>
        /// A value indicating whether this context has been disposed.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectBrowserViewModelMockContext" /> class.
        /// </summary>
        internal ProjectBrowserViewModelMockContext()
        {
            this.Mock = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            var reactiveMock = this.Mock.As<IReactiveObject>();

            reactiveMock
                .Setup(x => x.RaisePropertyChanging(It.IsAny<PropertyChangingEventArgs>()))
                .Callback<PropertyChangingEventArgs>(args =>
                    reactiveMock.Raise(x => x.PropertyChanging += null, args));

            reactiveMock
                .Setup(x => x.RaisePropertyChanged(It.IsAny<PropertyChangedEventArgs>()))
                .Callback<PropertyChangedEventArgs>(args =>
                    this.Mock.Raise(x => x.PropertyChanged += null, args));

            this.reactiveObject = reactiveMock.Object;
            this.reactiveObject.SubscribePropertyChangingEvents();
            this.reactiveObject.SubscribePropertyChangedEvents();

            this.activator = new ViewModelActivator();
            this.initializeCommand = ReactiveCommand.CreateFromTask(this.InitializeAsync);
            this.toggleNodeCommand = ReactiveCommand.Create<ProjectBrowserNodeViewModel>(this.ToggleNode);
            this.selectNodeCommand = ReactiveCommand.Create<ProjectBrowserNodeViewModel>(this.SelectNode);

            this.rootNodeBinding = System.ObservableExtensions.Subscribe(
                this.rootNodeSource.Connect().Bind(out this.rootNodes));

            this.Mock.SetupGet(x => x.Activator).Returns(this.activator);
            this.Mock.SetupGet(x => x.RootNodes).Returns(() => this.rootNodes);
            this.Mock.SetupGet(x => x.SelectedNode).Returns(() => this.selectedNode);
            this.Mock.SetupGet(x => x.IsLoading).Returns(() => this.isLoading);
            this.Mock.SetupGet(x => x.IsLoaded).Returns(() => this.isLoaded);
            this.Mock.SetupGet(x => x.ErrorMessage).Returns(() => this.errorMessage);
            this.Mock.SetupGet(x => x.InitializeCommand).Returns(this.initializeCommand);
            this.Mock.SetupGet(x => x.ToggleNodeCommand).Returns(this.toggleNodeCommand);
            this.Mock.SetupGet(x => x.SelectNodeCommand).Returns(this.selectNodeCommand);
            this.Mock.Setup(x => x.Dispose());
        }

        /// <summary>
        /// Gets the configured mock.
        /// </summary>
        internal Mock<IProjectBrowserViewModel> Mock { get; }

        /// <summary>
        /// Gets the mocked Project Browser contract.
        /// </summary>
        internal IProjectBrowserViewModel Object => this.Mock.Object;

        /// <summary>
        /// Gets or sets the controlled initialization handler.
        /// </summary>
        internal Func<CancellationToken, Task<bool>> InitializeHandler { get; set; } =
            _ => Task.FromResult(true);

        /// <summary>
        /// Gets or sets the test action invoked by the toggle command.
        /// </summary>
        internal Action<ProjectBrowserNodeViewModel> ToggleHandler { get; set; }

        /// <summary>
        /// Gets or sets the test action invoked by the selection command.
        /// </summary>
        internal Action<ProjectBrowserNodeViewModel> SelectHandler { get; set; }

        /// <summary>
        /// Gets the number of initialization command invocations.
        /// </summary>
        internal int InitializeCallCount { get; private set; }

        /// <summary>
        /// Gets the number of toggle command invocations.
        /// </summary>
        internal int ToggleCallCount { get; private set; }

        /// <summary>
        /// Gets the number of selection command invocations.
        /// </summary>
        internal int SelectCallCount { get; private set; }

        /// <summary>
        /// Gets the last node passed to the toggle command.
        /// </summary>
        internal ProjectBrowserNodeViewModel LastToggledNode { get; private set; }

        /// <summary>
        /// Gets the last node passed to the selection command.
        /// </summary>
        internal ProjectBrowserNodeViewModel LastSelectedNode { get; private set; }

        /// <summary>
        /// Replaces the mocked root collection in one SourceList transaction.
        /// </summary>
        /// <param name="nodes">The replacement root nodes.</param>
        internal void ReplaceRootNodes(params ProjectBrowserNodeViewModel[] nodes)
        {
            this.reactiveObject.RaisePropertyChanging(nameof(IProjectBrowserViewModel.RootNodes));

            this.rootNodeSource.Edit(items =>
            {
                items.Clear();
                items.AddRange(nodes);
            });

            this.reactiveObject.RaisePropertyChanged(nameof(IProjectBrowserViewModel.RootNodes));
        }

        /// <summary>
        /// Changes the loading state and emits the full ReactiveUI notification contract.
        /// </summary>
        /// <param name="value">The replacement state.</param>
        internal void SetIsLoading(bool value)
        {
            this.SetReactiveValue(ref this.isLoading, value, nameof(IProjectBrowserViewModel.IsLoading));
        }

        /// <summary>
        /// Changes the loaded state and emits the full ReactiveUI notification contract.
        /// </summary>
        /// <param name="value">The replacement state.</param>
        internal void SetIsLoaded(bool value)
        {
            this.SetReactiveValue(ref this.isLoaded, value, nameof(IProjectBrowserViewModel.IsLoaded));
        }

        /// <summary>
        /// Changes the loading error and emits the full ReactiveUI notification contract.
        /// </summary>
        /// <param name="value">The replacement error message.</param>
        internal void SetErrorMessage(string value)
        {
            this.SetReactiveValue(ref this.errorMessage, value, nameof(IProjectBrowserViewModel.ErrorMessage));
        }

        /// <summary>
        /// Changes the selected visual node and emits the full ReactiveUI notification contract.
        /// </summary>
        /// <param name="value">The replacement selected node.</param>
        internal void SetSelectedNode(ProjectBrowserNodeViewModel value)
        {
            if (ReferenceEquals(this.selectedNode, value))
            {
                return;
            }

            this.reactiveObject.RaisePropertyChanging(nameof(IProjectBrowserViewModel.SelectedNode));
            this.selectedNode = value;
            this.reactiveObject.RaisePropertyChanged(nameof(IProjectBrowserViewModel.SelectedNode));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;
            this.activator.Dispose();
            this.rootNodeBinding.Dispose();
            this.rootNodeSource.Dispose();
            this.initializeCommand.Dispose();
            this.toggleNodeCommand.Dispose();
            this.selectNodeCommand.Dispose();
        }

        /// <summary>
        /// Executes the controlled initialization handler.
        /// </summary>
        /// <param name="cancellationToken">Cancels initialization.</param>
        /// <returns>The controlled initialization result.</returns>
        private Task<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            this.InitializeCallCount++;

            return this.InitializeHandler(cancellationToken);
        }

        /// <summary>
        /// Captures a toggle command invocation without reproducing ViewModel behavior.
        /// </summary>
        /// <param name="node">The command input.</param>
        private void ToggleNode(ProjectBrowserNodeViewModel node)
        {
            this.ToggleCallCount++;
            this.LastToggledNode = node;
            this.ToggleHandler?.Invoke(node);
        }

        /// <summary>
        /// Captures a selection command invocation without reproducing ViewModel behavior.
        /// </summary>
        /// <param name="node">The command input.</param>
        private void SelectNode(ProjectBrowserNodeViewModel node)
        {
            this.SelectCallCount++;
            this.LastSelectedNode = node;
            this.SelectHandler?.Invoke(node);
        }

        /// <summary>
        /// Changes one backing value with ReactiveUI changing and changed notifications.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="field">The backing field.</param>
        /// <param name="value">The replacement value.</param>
        /// <param name="propertyName">The reactive property name.</param>
        private void SetReactiveValue<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            this.reactiveObject.RaisePropertyChanging(propertyName);
            field = value;
            this.reactiveObject.RaisePropertyChanged(propertyName);
        }
    }
}
