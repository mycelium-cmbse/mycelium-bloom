// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.ProjectBrowser
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Bunit;

    using Microsoft.AspNetCore.Components;
    using Moq;

    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using ProjectBrowserComponent = Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser.ProjectBrowser;
    using ProjectBrowserNodeComponent = Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser.ProjectBrowserNode;

    /// <summary>
    /// Tests the <see cref="ProjectBrowserComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ProjectBrowserTestFixture : BunitContext
    {
        /// <summary>
        /// The expected parent-node interaction order.
        /// </summary>
        private static readonly string[] ExpectedParentNodeInteractions = ["toggle", "select", "callback"];

        /// <summary>
        /// The expected leaf-node interaction order.
        /// </summary>
        private static readonly string[] ExpectedLeafNodeInteractions = ["select", "callback"];

        /// <summary>
        /// The caller-owned ViewModel supplied to the component under test.
        /// </summary>
        private IProjectBrowserViewModel registeredViewModel;

        /// <summary>
        /// Disposes the bUnit test context after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            this.Dispose();
        }

        /// <summary>
        /// Verifies that the project browser renders a loading state while the ViewModel is loading.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysLoadingState()
        {
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel>();
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var viewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            viewModel.SetupGet(x => x.RootNodes).Returns(roots);
            viewModel.SetupGet(x => x.IsLoaded).Returns(false);
            viewModel.SetupGet(x => x.IsLoading).Returns(true);
            viewModel.Setup(x => x.Dispose());
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Markup, Does.Contain("Loading Quantities model"));
                Assert.That(component.Markup, Does.Contain("Preparing the SysML project browser..."));
                Assert.That(component.Markup, Does.Contain("mb-project-browser__state"));
                viewModel.Verify(
                    x => x.InitializeAsync(It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        /// <summary>
        /// Verifies that the project browser renders its error state when loading fails.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysErrorState()
        {
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel>();
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var viewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            viewModel.SetupGet(x => x.RootNodes).Returns(roots);
            viewModel.SetupGet(x => x.IsLoaded).Returns(false);
            viewModel.SetupGet(x => x.IsLoading).Returns(false);
            viewModel.SetupGet(x => x.ErrorMessage).Returns("Model load failed");
            viewModel.Setup(x => x.Dispose());
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Markup, Does.Contain("Unable to load project browser"));
                Assert.That(component.Markup, Does.Contain("Model load failed"));
                Assert.That(component.Find("[role='alert']"), Is.Not.Null);
                viewModel.Verify(
                    x => x.InitializeAsync(It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        /// <summary>
        /// Verifies that the project browser renders tree nodes when the ViewModel has loaded.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysLoadedTree()
        {
            var node = ProjectBrowserNodeTestFactory.CreateNamespaceNode("quantities", "Quantities");
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel> { node };
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var viewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            viewModel.SetupGet(x => x.RootNodes).Returns(roots);
            viewModel.SetupGet(x => x.IsLoaded).Returns(true);
            viewModel.SetupGet(x => x.IsLoading).Returns(false);
            viewModel.SetupGet(x => x.ErrorMessage).Returns(string.Empty);
            viewModel.Setup(x => x.Dispose());
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Markup, Does.Contain("Quantities"));
                Assert.That(component.Find(".mb-project-browser__tree").GetAttribute("role"), Is.EqualTo("tree"));
                Assert.That(component.Markup, Does.Not.Contain("Loading Quantities model"));
                viewModel.Verify(
                    x => x.InitializeAsync(It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        /// <summary>
        /// Verifies the tree retains its accessible semantics inside the independent scroll viewport.
        /// </summary>
        [Test]
        public void VerifyTreeViewportPreservesAccessibleTreeAndIntrinsicWidthContract()
        {
            var node = ProjectBrowserNodeTestFactory.CreateNamespaceNode(
                "quantities/long",
                "ExtremelyLongElementNameThatMustRemainAvailableToHorizontalScrolling");
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel> { node };
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var viewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            viewModel.SetupGet(x => x.RootNodes).Returns(roots);
            viewModel.SetupGet(x => x.IsLoaded).Returns(true);
            viewModel.SetupGet(x => x.IsLoading).Returns(false);
            viewModel.SetupGet(x => x.ErrorMessage).Returns(string.Empty);
            viewModel.Setup(x => x.Dispose());
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            var viewport = component.Find(".mb-project-browser__tree-viewport");
            var tree = component.Find(".mb-project-browser__tree");
            var repositoryRoot = TestRepository.GetRootPath();
            var componentDirectory = Path.Combine(
                repositoryRoot,
                "Mycelium.Bloom",
                "Components",
                "UI",
                "Organisms",
                "ProjectBrowser");
            var style = File.ReadAllText(Path.Combine(componentDirectory, "ProjectBrowser.razor.css"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewport.Children, Has.Count.EqualTo(1));
                Assert.That(viewport.Children[0].ClassList, Does.Contain("mb-project-browser__tree"));
                Assert.That(tree.GetAttribute("role"), Is.EqualTo("tree"));
                Assert.That(tree.GetAttribute("aria-label"), Is.EqualTo("Project browser"));
                Assert.That(component.Markup, Does.Contain("ExtremelyLongElementNameThatMustRemainAvailableToHorizontalScrolling"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-project-browser__tree-viewport\s*\{[^}]*overflow:\s*auto;[^}]*scrollbar-width:\s*thin;[^}]*scrollbar-color:\s*var\(--mb-project-browser-scrollbar-thumb\)\s+transparent;"));
                Assert.That(
                    style,
                    Does.Contain("background-attachment: local, local, local, local, scroll, scroll, scroll, scroll;"));
                Assert.That(style, Does.Contain("@supports (scrollbar-width: none)"));
                Assert.That(style, Does.Contain("scrollbar-width: none;"));
                Assert.That(style, Does.Contain("@media (forced-colors: active)"));
                Assert.That(style, Does.Contain("scrollbar-color: ButtonText Canvas;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-project-browser__tree\s*\{[^}]*flex:\s*0\s+0\s+auto;[^}]*width:\s*max-content;[^}]*min-width:\s*100%;[^}]*background:\s*transparent;"));
                Assert.That(style, Does.Contain(".mb-project-browser__tree-viewport::-webkit-scrollbar-button"));
            }
        }

        /// <summary>
        /// Verifies the project browser renders parameters inherited from the caller-supplied Bloom reactive base.
        /// </summary>
        [Test]
        public void VerifyRenderUsesInheritedBloomParameters()
        {
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel>();
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var viewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            viewModel.SetupGet(x => x.RootNodes).Returns(roots);
            viewModel.SetupGet(x => x.IsLoaded).Returns(true);
            viewModel.SetupGet(x => x.IsLoading).Returns(false);
            viewModel.SetupGet(x => x.ErrorMessage).Returns(string.Empty);
            viewModel.Setup(x => x.Dispose());
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser(parameters => parameters
                .Add(browser => browser.Class, "custom-project-browser")
                .AddUnmatched("data-testid", "project-browser"));

            var root = component.Find(".mb-project-browser");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root.ClassList.Contains("custom-project-browser"), Is.True);
                Assert.That(root.GetAttribute("data-testid"), Is.EqualTo("project-browser"));
            }
        }

        /// <summary>
        /// Verifies component initialization directly invokes <see cref="IProjectBrowserViewModel.InitializeAsync" />.
        /// </summary>
        [Test]
        public void VerifyOnInitializedAsyncInvokesInitializeAsync()
        {
            var node = ProjectBrowserNodeTestFactory.CreateNamespaceNode("quantities", "Quantities");
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel>();
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var isLoaded = false;
            var selectedNodeCallbackCount = 0;
            CancellationToken capturedToken = default;
            var viewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            viewModel.SetupGet(x => x.RootNodes).Returns(roots);
            viewModel.SetupGet(x => x.IsLoaded).Returns(() => isLoaded);
            viewModel.SetupGet(x => x.IsLoading).Returns(false);
            viewModel.SetupGet(x => x.ErrorMessage).Returns(string.Empty);
            viewModel
                .Setup(x => x.InitializeAsync(It.IsAny<CancellationToken>()))
                .Returns<CancellationToken>(token =>
                {
                    capturedToken = token;
                    mutableRoots.Add(node);
                    isLoaded = true;

                    return Task.FromResult(true);
                });
            viewModel.Setup(x => x.Dispose());
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser(parameters => parameters
                .Add(browser => browser.SelectedNodeChanged, _ => selectedNodeCallbackCount++));

            component.WaitForAssertion(() => Assert.That(component.Markup, Does.Contain("Quantities")));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(capturedToken.CanBeCanceled, Is.True);
                Assert.That(capturedToken.IsCancellationRequested, Is.False);
                Assert.That(selectedNodeCallbackCount, Is.Zero);
                viewModel.Verify(x => x.InitializeAsync(capturedToken), Times.Once);
            }
        }

        /// <summary>
        /// Verifies parent-node interaction invokes ordinary ViewModel methods before the local callback.
        /// </summary>
        [Test]
        public void VerifyHandleNodeSelectedInvokesToggleSelectAndCallbackInOrder()
        {
            var child = ProjectBrowserNodeTestFactory.CreateNamespaceNode("quantities/length", "Length");
            var node = ProjectBrowserNodeTestFactory.CreateNamespaceNode("quantities", "Quantities", child);
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel> { node };
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var interactions = new List<string>();
            var viewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            viewModel.SetupGet(x => x.RootNodes).Returns(roots);
            viewModel.SetupGet(x => x.IsLoaded).Returns(true);
            viewModel.SetupGet(x => x.IsLoading).Returns(false);
            viewModel.SetupGet(x => x.ErrorMessage).Returns(string.Empty);
            viewModel.Setup(x => x.ToggleNode(node)).Callback(() => interactions.Add("toggle"));
            viewModel.Setup(x => x.SelectNode(node)).Callback(() => interactions.Add("select"));
            viewModel.Setup(x => x.Dispose());
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser(parameters => parameters
                .Add(browser => browser.SelectedNodeChanged, selectedNode =>
                {
                    Assert.That(selectedNode, Is.SameAs(node));
                    interactions.Add("callback");
                }));

            component.Find("button").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(interactions, Is.EqualTo(ExpectedParentNodeInteractions));
                viewModel.Verify(x => x.ToggleNode(node), Times.Once);
                viewModel.Verify(x => x.SelectNode(node), Times.Once);
            }
        }

        /// <summary>
        /// Verifies leaf interaction invokes selection and the callback without toggling.
        /// </summary>
        [Test]
        public void VerifyHandleNodeSelectedSkipsToggleForLeaf()
        {
            var node = ProjectBrowserNodeTestFactory.CreateNamespaceNode("quantities", "Quantities");
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel> { node };
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var interactions = new List<string>();
            var viewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            viewModel.SetupGet(x => x.RootNodes).Returns(roots);
            viewModel.SetupGet(x => x.IsLoaded).Returns(true);
            viewModel.SetupGet(x => x.IsLoading).Returns(false);
            viewModel.SetupGet(x => x.ErrorMessage).Returns(string.Empty);
            viewModel.Setup(x => x.SelectNode(node)).Callback(() => interactions.Add("select"));
            viewModel.Setup(x => x.Dispose());
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser(parameters => parameters
                .Add(browser => browser.SelectedNodeChanged, _ => interactions.Add("callback")));

            component.Find("button").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(interactions, Is.EqualTo(ExpectedLeafNodeInteractions));
                viewModel.Verify(x => x.ToggleNode(It.IsAny<ProjectBrowserNodeViewModel>()), Times.Never);
                viewModel.Verify(x => x.SelectNode(node), Times.Once);
            }
        }

        /// <summary>
        /// Verifies a runtime loading-state notification rerenders the real component.
        /// </summary>
        [Test]
        public void VerifyIsLoadingPropertyChangedRerendersComponent()
        {
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel>();
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var isLoading = true;
            var viewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            var notifyingViewModel = viewModel.As<INotifyPropertyChanged>();
            viewModel.SetupGet(x => x.RootNodes).Returns(roots);
            viewModel.SetupGet(x => x.IsLoaded).Returns(true);
            viewModel.SetupGet(x => x.IsLoading).Returns(() => isLoading);
            viewModel.SetupGet(x => x.ErrorMessage).Returns(string.Empty);
            viewModel.Setup(x => x.Dispose());
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            Assert.That(component.Markup, Does.Contain("Loading Quantities model"));

            isLoading = false;
            notifyingViewModel.Raise(
                x => x.PropertyChanged += null,
                new PropertyChangedEventArgs(nameof(IProjectBrowserViewModel.IsLoading)));

            component.WaitForAssertion(() =>
                Assert.That(component.Markup, Does.Contain("No model elements available.")));
        }

        /// <summary>
        /// Verifies a runtime loaded-state notification rerenders the real component.
        /// </summary>
        [Test]
        public void VerifyIsLoadedPropertyChangedRerendersComponent()
        {
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel>();
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var isLoaded = true;
            var viewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            var notifyingViewModel = viewModel.As<INotifyPropertyChanged>();
            viewModel.SetupGet(x => x.RootNodes).Returns(roots);
            viewModel.SetupGet(x => x.IsLoaded).Returns(() => isLoaded);
            viewModel.SetupGet(x => x.IsLoading).Returns(false);
            viewModel.SetupGet(x => x.ErrorMessage).Returns(string.Empty);
            viewModel.Setup(x => x.Dispose());
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            Assert.That(component.Markup, Does.Contain("No model elements available."));

            isLoaded = false;
            notifyingViewModel.Raise(
                x => x.PropertyChanged += null,
                new PropertyChangedEventArgs(nameof(IProjectBrowserViewModel.IsLoaded)));

            component.WaitForAssertion(() =>
                Assert.That(component.Markup, Does.Contain("Loading Quantities model")));
        }

        /// <summary>
        /// Verifies a runtime error-state notification rerenders the real component.
        /// </summary>
        [Test]
        public void VerifyErrorMessagePropertyChangedRerendersComponent()
        {
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel>();
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var errorMessage = string.Empty;
            var viewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            var notifyingViewModel = viewModel.As<INotifyPropertyChanged>();
            viewModel.SetupGet(x => x.RootNodes).Returns(roots);
            viewModel.SetupGet(x => x.IsLoaded).Returns(true);
            viewModel.SetupGet(x => x.IsLoading).Returns(false);
            viewModel.SetupGet(x => x.ErrorMessage).Returns(() => errorMessage);
            viewModel.Setup(x => x.Dispose());
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();

            errorMessage = "Reactive model failure";
            notifyingViewModel.Raise(
                x => x.PropertyChanged += null,
                new PropertyChangedEventArgs(nameof(IProjectBrowserViewModel.ErrorMessage)));

            component.WaitForAssertion(() =>
                Assert.That(component.Find("[role='alert']").TextContent, Does.Contain("Reactive model failure")));
        }

        /// <summary>
        /// Verifies a runtime selected-node notification rerenders without synthesizing the local callback.
        /// </summary>
        [Test]
        public void VerifySelectedNodePropertyChangedRerendersComponentWithoutCallback()
        {
            var node = ProjectBrowserNodeTestFactory.CreateNamespaceNode("quantities", "Quantities");
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel> { node };
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            ProjectBrowserNodeViewModel selectedNode = null;
            var callbackCount = 0;
            var viewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            var notifyingViewModel = viewModel.As<INotifyPropertyChanged>();
            viewModel.SetupGet(x => x.RootNodes).Returns(roots);
            viewModel.SetupGet(x => x.SelectedNode).Returns(() => selectedNode);
            viewModel.SetupGet(x => x.IsLoaded).Returns(true);
            viewModel.SetupGet(x => x.IsLoading).Returns(false);
            viewModel.SetupGet(x => x.ErrorMessage).Returns(string.Empty);
            viewModel.Setup(x => x.Dispose());
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser(parameters => parameters
                .Add(browser => browser.SelectedNodeChanged, _ => callbackCount++));
            var renderCount = component.RenderCount;

            node.IsSelected = true;
            selectedNode = node;
            notifyingViewModel.Raise(
                x => x.PropertyChanged += null,
                new PropertyChangedEventArgs(nameof(IProjectBrowserViewModel.SelectedNode)));

            component.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.RenderCount, Is.GreaterThan(renderCount));
                    Assert.That(component.Find("[role='treeitem']").GetAttribute("aria-selected"), Is.EqualTo("true"));
                    Assert.That(callbackCount, Is.Zero);
                }
            });
        }

        /// <summary>
        /// Verifies roots rerender only through the ViewModel's coherent top-level publication.
        /// </summary>
        [Test]
        public void VerifyRootNodesTopLevelPublicationRerendersComponent()
        {
            var firstNode = ProjectBrowserNodeTestFactory.CreateNamespaceNode("first", "First");
            var secondNode = ProjectBrowserNodeTestFactory.CreateNamespaceNode("second", "Second");
            var thirdNode = ProjectBrowserNodeTestFactory.CreateNamespaceNode("third", "Third");
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel> { firstNode };
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var viewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            var notifyingViewModel = viewModel.As<INotifyPropertyChanged>();
            viewModel.SetupGet(x => x.RootNodes).Returns(roots);
            viewModel.SetupGet(x => x.IsLoaded).Returns(true);
            viewModel.SetupGet(x => x.IsLoading).Returns(false);
            viewModel.SetupGet(x => x.ErrorMessage).Returns(string.Empty);
            viewModel.Setup(x => x.Dispose());
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            Assert.That(component.Find(".mb-project-browser-node__title").TextContent, Is.EqualTo("First"));
            var initialRenderCount = component.RenderCount;

            mutableRoots.Clear();
            mutableRoots.Add(secondNode);
            mutableRoots.Add(thirdNode);

            Assert.That(component.RenderCount, Is.EqualTo(initialRenderCount));

            notifyingViewModel.Raise(
                x => x.PropertyChanged += null,
                new PropertyChangedEventArgs(nameof(IProjectBrowserViewModel.RootNodes)));

            component.WaitForAssertion(() =>
            {
                var titles = component.FindAll(".mb-project-browser-node__title");

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(titles, Has.Count.EqualTo(2));
                    Assert.That(titles[0].TextContent, Is.EqualTo("Second"));
                    Assert.That(titles[1].TextContent, Is.EqualTo("Third"));
                }
            });
        }

        /// <summary>
        /// Verifies changing test-local state without publishing PropertyChanged does not rerender.
        /// </summary>
        [Test]
        public void VerifyMissingPropertyChangedNotificationDoesNotRerenderComponent()
        {
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel>();
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var isLoaded = true;
            var viewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            viewModel.As<INotifyPropertyChanged>();
            viewModel.SetupGet(x => x.RootNodes).Returns(roots);
            viewModel.SetupGet(x => x.IsLoaded).Returns(() => isLoaded);
            viewModel.SetupGet(x => x.IsLoading).Returns(false);
            viewModel.SetupGet(x => x.ErrorMessage).Returns(string.Empty);
            viewModel.Setup(x => x.Dispose());
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            var renderCount = component.RenderCount;

            isLoaded = false;

            Assert.That(component.RenderCount, Is.EqualTo(renderCount));
        }

        /// <summary>
        /// Verifies initialization failure state produced by the ViewModel renders after lifecycle completion.
        /// </summary>
        [Test]
        public void VerifyInitializationFailureRendersViewModelError()
        {
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel>();
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var errorMessage = string.Empty;
            var viewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            viewModel.SetupGet(x => x.RootNodes).Returns(roots);
            viewModel.SetupGet(x => x.IsLoaded).Returns(false);
            viewModel.SetupGet(x => x.IsLoading).Returns(false);
            viewModel.SetupGet(x => x.ErrorMessage).Returns(() => errorMessage);
            viewModel
                .Setup(x => x.InitializeAsync(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    errorMessage = "Model load failed";

                    return Task.FromResult(false);
                });
            viewModel.Setup(x => x.Dispose());
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();

            Assert.That(component.Find("[role='alert']").TextContent, Does.Contain("Model load failed"));
        }

        /// <summary>
        /// Verifies component disposal cancels a blocked initialization without disposing the caller-owned ViewModel.
        /// </summary>
        [Test]
        public void VerifyDisposeDuringInitializationCancelsAndQuarantinesCompletion()
        {
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel>();
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var initialization = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationToken capturedToken = default;
            var viewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            viewModel.SetupGet(x => x.RootNodes).Returns(roots);
            viewModel.SetupGet(x => x.IsLoaded).Returns(false);
            viewModel.SetupGet(x => x.IsLoading).Returns(false);
            viewModel.SetupGet(x => x.ErrorMessage).Returns(string.Empty);
            viewModel
                .Setup(x => x.InitializeAsync(It.IsAny<CancellationToken>()))
                .Returns<CancellationToken>(token =>
                {
                    capturedToken = token;

                    return initialization.Task;
                });
            viewModel.Setup(x => x.Dispose());
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            Assert.That(capturedToken.CanBeCanceled, Is.True);
            var renderCount = component.RenderCount;

            component.Instance.Dispose();
            initialization.SetResult(true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(capturedToken.IsCancellationRequested, Is.True);
                Assert.That(component.RenderCount, Is.EqualTo(renderCount));
                viewModel.Verify(x => x.Dispose(), Times.Never);
            }
        }

        /// <summary>
        /// Verifies a captured child callback cannot invoke methods or callbacks after component disposal.
        /// </summary>
        [Test]
        public async Task VerifyDisposedComponentIgnoresCapturedNodeSelection()
        {
            var node = ProjectBrowserNodeTestFactory.CreateNamespaceNode("quantities", "Quantities");
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel> { node };
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var callbackCount = 0;
            var viewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            viewModel.SetupGet(x => x.RootNodes).Returns(roots);
            viewModel.SetupGet(x => x.IsLoaded).Returns(true);
            viewModel.SetupGet(x => x.IsLoading).Returns(false);
            viewModel.SetupGet(x => x.ErrorMessage).Returns(string.Empty);
            viewModel.Setup(x => x.Dispose());
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser(parameters => parameters
                .Add(browser => browser.SelectedNodeChanged, _ => callbackCount++));
            var nodeCallback = component.FindComponent<ProjectBrowserNodeComponent>().Instance.OnNodeSelected;

            await this.DisposeComponentsAsync();
            var renderCountAfterDisposal = component.RenderCount;
            await this.Renderer.Dispatcher.InvokeAsync(() => nodeCallback.InvokeAsync(node));

            using (Assert.EnterMultipleScope())
            {
                viewModel.Verify(x => x.ToggleNode(It.IsAny<ProjectBrowserNodeViewModel>()), Times.Never);
                viewModel.Verify(x => x.SelectNode(It.IsAny<ProjectBrowserNodeViewModel>()), Times.Never);
                Assert.That(callbackCount, Is.Zero);
                Assert.That(component.RenderCount, Is.EqualTo(renderCountAfterDisposal));
            }
        }

        /// <summary>
        /// Verifies ViewModel replacement detaches the old top-level root publisher.
        /// </summary>
        [Test]
        public void VerifyViewModelReplacementDetachesOldRootPublication()
        {
            var firstRootsSource = new ObservableCollection<ProjectBrowserNodeViewModel>
            {
                ProjectBrowserNodeTestFactory.CreateNamespaceNode("first", "First")
            };
            var secondRootsSource = new ObservableCollection<ProjectBrowserNodeViewModel>
            {
                ProjectBrowserNodeTestFactory.CreateNamespaceNode("second", "Second")
            };
            var firstViewModel = CreateLoadedViewModel(firstRootsSource);
            var secondViewModel = CreateLoadedViewModel(secondRootsSource);
            this.RegisterViewModel(firstViewModel.Object);

            using var component = this.RenderProjectBrowser();
            component.Render(parameters => parameters
                .Add(browser => browser.ViewModel, secondViewModel.Object));
            var replacementRenderCount = component.RenderCount;

            firstRootsSource.Clear();
            firstViewModel.Raise(
                x => x.PropertyChanged += null,
                new PropertyChangedEventArgs(nameof(IProjectBrowserViewModel.RootNodes)));

            Assert.That(component.RenderCount, Is.EqualTo(replacementRenderCount));

            secondRootsSource.Clear();
            secondViewModel.Raise(
                x => x.PropertyChanged += null,
                new PropertyChangedEventArgs(nameof(IProjectBrowserViewModel.RootNodes)));

            component.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.RenderCount, Is.GreaterThan(replacementRenderCount));
                    Assert.That(component.Markup, Does.Contain("No model elements available."));
                }
            });
        }

        /// <summary>
        /// Verifies top-level root publication cannot rerender the component after reactive deactivation.
        /// </summary>
        [Test]
        public async Task VerifyDisposedComponentIgnoresRootNodesPublication()
        {
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel>();
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var viewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            var notifyingViewModel = viewModel.As<INotifyPropertyChanged>();
            viewModel.SetupGet(x => x.RootNodes).Returns(roots);
            viewModel.SetupGet(x => x.IsLoaded).Returns(true);
            viewModel.SetupGet(x => x.IsLoading).Returns(false);
            viewModel.SetupGet(x => x.ErrorMessage).Returns(string.Empty);
            viewModel.Setup(x => x.Dispose());
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();

            await this.DisposeComponentsAsync();
            var renderCountAfterDisposal = component.RenderCount;

            mutableRoots.Add(ProjectBrowserNodeTestFactory.CreateNamespaceNode("first", "First"));
#pragma warning disable S6966 // Moq cannot asynchronously raise a synchronous event after its handlers are detached.
            notifyingViewModel.Raise(
                x => x.PropertyChanged += null,
                new PropertyChangedEventArgs(nameof(IProjectBrowserViewModel.RootNodes)));
#pragma warning restore S6966

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.RenderCount, Is.EqualTo(renderCountAfterDisposal));
                viewModel.Verify(x => x.Dispose(), Times.Never);
            }
        }

        /// <summary>
        /// Verifies an unexpected interface implementation failure does not escape the component lifecycle.
        /// </summary>
        [Test]
        public void VerifyUnexpectedInitializationExceptionDoesNotEscapeLifecycle()
        {
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel>();
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var viewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            viewModel.SetupGet(x => x.RootNodes).Returns(roots);
            viewModel.SetupGet(x => x.IsLoaded).Returns(false);
            viewModel.SetupGet(x => x.IsLoading).Returns(false);
            viewModel.SetupGet(x => x.ErrorMessage).Returns(string.Empty);
            viewModel
                .Setup(x => x.InitializeAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Unexpected implementation failure"));
            viewModel.Setup(x => x.Dispose());
            this.RegisterViewModel(viewModel.Object);

            Assert.That(
                () =>
                {
                    using var component = this.RenderProjectBrowser();
                    Assert.That(component.Markup, Does.Contain("Loading Quantities model"));
                },
                Throws.Nothing);
        }

        /// <summary>
        /// Verifies the component does not dispose its caller-owned ViewModel boundary.
        /// </summary>
        [Test]
        public void VerifyComponentDoesNotDisposeCallerOwnedViewModelBoundary()
        {
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel>();
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var viewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            viewModel.SetupGet(x => x.RootNodes).Returns(roots);
            viewModel.SetupGet(x => x.IsLoaded).Returns(true);
            viewModel.SetupGet(x => x.IsLoading).Returns(false);
            viewModel.SetupGet(x => x.ErrorMessage).Returns(string.Empty);
            viewModel.Setup(x => x.Dispose());
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            component.Instance.Dispose();
            component.Instance.Dispose();

            viewModel.Verify(x => x.Dispose(), Times.Never);
        }

        /// <summary>
        /// Creates a loaded reactive contract over a caller-owned mutable root source.
        /// </summary>
        /// <param name="rootSource">The roots exposed through the stable read-only projection.</param>
        /// <returns>The configured ViewModel mock.</returns>
        private static Mock<IProjectBrowserViewModel> CreateLoadedViewModel(
            ObservableCollection<ProjectBrowserNodeViewModel> rootSource)
        {
            var viewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            viewModel.SetupGet(x => x.RootNodes)
                .Returns(new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(rootSource));
            viewModel.SetupGet(x => x.IsLoaded).Returns(true);
            viewModel.SetupGet(x => x.IsLoading).Returns(false);
            viewModel.SetupGet(x => x.ErrorMessage).Returns(string.Empty);
            viewModel.Setup(x => x.Dispose());

            return viewModel;
        }

        /// <summary>
        /// Stores the already configured Project Browser contract for the component under test.
        /// </summary>
        /// <param name="viewModel">The scenario-local mocked ViewModel.</param>
        private void RegisterViewModel(IProjectBrowserViewModel viewModel)
        {
            this.registeredViewModel = viewModel;
        }

        /// <summary>
        /// Renders the Project Browser with the caller-owned ViewModel and optional scenario parameters.
        /// </summary>
        /// <param name="configure">The optional scenario-specific parameter configuration.</param>
        /// <returns>The rendered Project Browser component.</returns>
        private IRenderedComponent<ProjectBrowserComponent> RenderProjectBrowser(
            Action<ComponentParameterCollectionBuilder<ProjectBrowserComponent>> configure = null)
        {
            return this.Render<ProjectBrowserComponent>(parameters =>
            {
                parameters.Add(component => component.ViewModel, this.registeredViewModel);
                configure?.Invoke(parameters);
            });
        }
    }
}
