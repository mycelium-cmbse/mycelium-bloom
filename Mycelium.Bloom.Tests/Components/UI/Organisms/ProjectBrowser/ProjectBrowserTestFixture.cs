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

    using BlazorBlueprint.Components;

    using Bunit;

    using Microsoft.AspNetCore.Components;
    using Moq;

    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using SysML2.NET.Core.POCO.Root.Namespaces;

    using ProjectBrowserComponent = Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser.ProjectBrowser;
    using ProjectBrowserNodeComponent = Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser.ProjectBrowserNode;
    using SearchInputComponent = Mycelium.Bloom.Components.UI.Atoms.SearchInput.SearchInput;
    using SelectInputComponent = Mycelium.Bloom.Components.UI.Atoms.SelectInput.SelectInput;

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
        /// The complete option-value order exposed by the element-kind filter.
        /// </summary>
        private static readonly string[] ExpectedElementKindOptionValues =
        [
            "all",
            .. Enum.GetNames<SysmlModelElementKind>()
        ];

        /// <summary>
        /// The immutable all-visible presentation used by ordinary strict interface scenarios.
        /// </summary>
        private static readonly ProjectBrowserFilterPresentation InactiveFilterPresentation =
            CreateInactiveFilterPresentation();

        /// <summary>
        /// The caller-owned ViewModel supplied to the component under test.
        /// </summary>
        private IProjectBrowserViewModel registeredViewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectBrowserTestFixture" /> class.
        /// </summary>
        public ProjectBrowserTestFixture()
        {
            BlueprintTestSetup.Configure(this);
        }

        /// <summary>
        /// Disposes the bUnit test context after each test.
        /// </summary>
        [TearDown]
        public Task TearDown()
        {
            return this.DisposeAsync().AsTask();
        }

        /// <summary>
        /// Verifies that the project browser renders a loading state while the ViewModel is loading.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysLoadingState()
        {
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel>();
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var viewModel = CreateProjectBrowserViewModelMock();
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
            var viewModel = CreateProjectBrowserViewModelMock();
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
            var viewModel = CreateProjectBrowserViewModelMock();
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
            var viewModel = CreateLoadedViewModel(mutableRoots);
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            var rootChildren = component.Find(".mb-project-browser").Children;
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
                Assert.That(rootChildren, Has.Count.EqualTo(2));
                Assert.That(rootChildren[0].ClassList, Does.Contain("mb-project-browser__filters"));
                Assert.That(rootChildren[1].ClassList, Does.Contain("mb-project-browser__tree-viewport"));
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
        /// Verifies loaded browsers render the accessible filter controls before the independently scrolling tree.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysAccessibleFilterControlsAboveTree()
        {
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel>
            {
                ProjectBrowserNodeTestFactory.CreateNamespaceNode("root", "Root")
            };
            var viewModel = CreateLoadedViewModel(mutableRoots);
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            var rootChildren = component.Find(".mb-project-browser").Children;
            var filterRegion = component.Find(".mb-project-browser__filters");
            var searchInput = component.FindComponent<SearchInputComponent>();
            var selectInput = component.FindComponent<SelectInputComponent>();
            var clearButton = component.FindComponent<BbButton>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rootChildren, Has.Count.EqualTo(2));
                Assert.That(rootChildren[0].ClassList, Does.Contain("mb-project-browser__filters"));
                Assert.That(rootChildren[1].ClassList, Does.Contain("mb-project-browser__tree-viewport"));
                Assert.That(filterRegion.GetAttribute("role"), Is.EqualTo("search"));
                Assert.That(filterRegion.GetAttribute("aria-label"), Is.EqualTo("Project browser filters"));
                Assert.That(searchInput.Instance.Value, Is.Empty);
                Assert.That(searchInput.Instance.FullWidth, Is.True);
                Assert.That(searchInput.Instance.Placeholder, Is.EqualTo("Search elements"));
                Assert.That(
                    searchInput.Instance.AriaLabel,
                    Is.EqualTo("Filter project browser by name or qualified name"));
                Assert.That(searchInput.Instance.EnableShortcut, Is.False);
                Assert.That(selectInput.Find("button").GetAttribute("aria-label"),
                    Is.EqualTo("Filter project browser by element kind"));
                Assert.That(selectInput.Instance.Value, Is.EqualTo("all"));
                Assert.That(
                    selectInput.Instance.Options.Select(option => option.Value),
                    Is.EqualTo(ExpectedElementKindOptionValues));
                Assert.That(selectInput.Instance.Options.First().Label, Is.EqualTo("All element kinds"));
                Assert.That(clearButton.Instance.Disabled, Is.True);
                Assert.That(clearButton.Markup, Does.Contain("Clear"));
            }
        }

        /// <summary>
        /// Verifies both filter controls write directly to the caller-owned ViewModel state.
        /// </summary>
        [Test]
        public async Task VerifyFilterControlsWriteDirectlyToViewModel()
        {
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel>();
            var viewModel = CreateLoadedViewModel(mutableRoots);
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            var searchInput = component.FindComponent<SearchInputComponent>();
            var selectInput = component.FindComponent<SelectInputComponent>();

            await component.InvokeAsync(() => searchInput.Instance.ValueChanged.InvokeAsync("  needle  "));
            await component.InvokeAsync(() => selectInput.Instance.ValueChanged.InvokeAsync("Definition"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.Object.FilterText, Is.EqualTo("  needle  "));
                Assert.That(viewModel.Object.ElementKindFilter, Is.EqualTo(SysmlModelElementKind.Definition));
            }

            await component.InvokeAsync(() => selectInput.Instance.ValueChanged.InvokeAsync("all"));

            Assert.That(viewModel.Object.ElementKindFilter, Is.Null);
        }

        /// <summary>
        /// Verifies the clear action delegates the coherent filter reset to the ViewModel owner.
        /// </summary>
        [Test]
        public void VerifyClearFilterInvokesViewModelOwnerOperation()
        {
            using var presentationOwner = new ProjectBrowserViewModel(
                new Mock<IModelLoaderService>(MockBehavior.Strict).Object,
                new ContextAwareService());
            presentationOwner.FilterText = "missing";
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel>
            {
                ProjectBrowserNodeTestFactory.CreateNamespaceNode("root", "Root")
            };
            var viewModel = CreateLoadedViewModel(mutableRoots);
            viewModel.SetupGet(x => x.FilterPresentation).Returns(presentationOwner.FilterPresentation);
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            var clearButton = component.FindComponent<BbButton>();

            clearButton.Find("button").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(clearButton.Instance.Disabled, Is.False);
                viewModel.Verify(x => x.ClearFilter(), Times.Once);
            }
        }

        /// <summary>
        /// Verifies an empty source model remains distinct from an active filter with no matches.
        /// </summary>
        [Test]
        public void VerifyRenderDistinguishesModelEmptyAndFilteredEmptyStates()
        {
            var emptyRoots = new ObservableCollection<ProjectBrowserNodeViewModel>();
            var emptyViewModel = CreateLoadedViewModel(emptyRoots);
            this.RegisterViewModel(emptyViewModel.Object);

            using var emptyComponent = this.RenderProjectBrowser();
            var modelEmptyState = emptyComponent.Find(".mb-project-browser__empty");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(modelEmptyState.TextContent, Does.Contain("No model elements available."));
                Assert.That(modelEmptyState.GetAttribute("role"), Is.Null);
                Assert.That(modelEmptyState.GetAttribute("aria-live"), Is.Null);
            }

            emptyComponent.Dispose();

            using var presentationOwner = new ProjectBrowserViewModel(
                new Mock<IModelLoaderService>(MockBehavior.Strict).Object,
                new ContextAwareService());
            presentationOwner.FilterText = "missing";
            var filteredRoots = new ObservableCollection<ProjectBrowserNodeViewModel>
            {
                ProjectBrowserNodeTestFactory.CreateNamespaceNode("root", "Root")
            };
            var filteredViewModel = CreateLoadedViewModel(filteredRoots);
            filteredViewModel.SetupGet(x => x.FilterPresentation).Returns(presentationOwner.FilterPresentation);
            this.RegisterViewModel(filteredViewModel.Object);

            using var filteredComponent = this.RenderProjectBrowser();
            var filteredEmptyState = filteredComponent.Find(".mb-project-browser__empty");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    filteredEmptyState.TextContent,
                    Does.Contain("No model elements match the current filters."));
                Assert.That(filteredEmptyState.GetAttribute("role"), Is.EqualTo("status"));
                Assert.That(filteredEmptyState.GetAttribute("aria-live"), Is.EqualTo("polite"));
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
            var viewModel = CreateProjectBrowserViewModelMock();
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
            var viewModel = CreateProjectBrowserViewModelMock();
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
                Assert.That(capturedToken.CanBeCanceled, Is.False);
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
            var viewModel = CreateProjectBrowserViewModelMock();
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

            component.Find(".mb-project-browser-node__row").Click();

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
            var viewModel = CreateProjectBrowserViewModelMock();
            viewModel.SetupGet(x => x.RootNodes).Returns(roots);
            viewModel.SetupGet(x => x.IsLoaded).Returns(true);
            viewModel.SetupGet(x => x.IsLoading).Returns(false);
            viewModel.SetupGet(x => x.ErrorMessage).Returns(string.Empty);
            viewModel.Setup(x => x.SelectNode(node)).Callback(() => interactions.Add("select"));
            viewModel.Setup(x => x.Dispose());
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser(parameters => parameters
                .Add(browser => browser.SelectedNodeChanged, _ => interactions.Add("callback")));

            component.Find(".mb-project-browser-node__row").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(interactions, Is.EqualTo(ExpectedLeafNodeInteractions));
                viewModel.Verify(x => x.ToggleNode(It.IsAny<ProjectBrowserNodeViewModel>()), Times.Never);
                viewModel.Verify(x => x.SelectNode(node), Times.Once);
            }
        }

        /// <summary>
        /// Verifies a filtered recursive render contains the complete match path and no unrelated nodes.
        /// </summary>
        [Test]
        public async Task VerifyActiveFilterRendersMatchAncestorsAndExcludesNonmatches()
        {
            using var viewModel = await ProjectBrowserNodeTestFactory.CreateFilterTreeViewModelAsync();
            var root = viewModel.RootNodes[0];
            var branch = root.Children[0];
            branch.IsExpanded = false;
            viewModel.FilterText = "needle";
            this.RegisterViewModel(viewModel);

            using var component = this.RenderProjectBrowser();
            var titles = component.FindAll(".mb-project-browser-node__title")
                .Select(title => title.TextContent)
                .ToArray();
            var treeItems = component.FindAll("[role='treeitem']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(titles, Is.EqualTo(new[] { "Root", "Branch", "Needle" }));
                Assert.That(component.Markup, Does.Not.Contain("Sibling"));
                Assert.That(component.Markup, Does.Not.Contain("Hidden descendant"));
                Assert.That(treeItems, Has.Count.EqualTo(3));
                Assert.That(treeItems[0].GetAttribute("aria-expanded"), Is.EqualTo("true"));
                Assert.That(treeItems[1].GetAttribute("aria-expanded"), Is.EqualTo("true"));
                Assert.That(treeItems[2].GetAttribute("aria-expanded"), Is.Null);
                Assert.That(branch.IsExpanded, Is.False);
            }
        }

        /// <summary>
        /// Verifies selecting a visible filtered parent preserves durable expansion while retaining selection flow.
        /// </summary>
        [Test]
        public async Task VerifyFilteredNodeSelectionSkipsDurableToggle()
        {
            using var presentationOwner = await ProjectBrowserNodeTestFactory.CreateFilterTreeViewModelAsync();
            presentationOwner.FilterText = "needle";
            var root = presentationOwner.RootNodes[0];
            var branch = root.Children[0];
            branch.IsExpanded = false;
            var roots = new ObservableCollection<ProjectBrowserNodeViewModel> { root };
            var viewModel = CreateLoadedViewModel(roots);
            viewModel.SetupGet(x => x.FilterText).Returns("needle");
            viewModel.SetupGet(x => x.FilterPresentation).Returns(presentationOwner.FilterPresentation);
            viewModel.Setup(x => x.SelectNode(branch));
            var selectedNode = default(ProjectBrowserNodeViewModel);
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser(parameters => parameters
                .Add(browser => browser.SelectedNodeChanged, node => selectedNode = node));
            var branchComponent = component.FindComponents<ProjectBrowserNodeComponent>()
                .Single(candidate => ReferenceEquals(candidate.Instance.ViewModel, branch));

            branchComponent.Find("button").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectedNode, Is.SameAs(branch));
                Assert.That(branch.IsExpanded, Is.False);
                viewModel.Verify(x => x.ToggleNode(It.IsAny<ProjectBrowserNodeViewModel>()), Times.Never);
                viewModel.Verify(x => x.SelectNode(branch), Times.Once);
            }
        }

        /// <summary>
        /// Verifies the owner-published filter presentation rerenders the recursive tree through the reactive base.
        /// </summary>
        [Test]
        public async Task VerifyFilterPresentationPropertyChangedRerendersComponent()
        {
            using var presentationOwner = await ProjectBrowserNodeTestFactory.CreateFilterTreeViewModelAsync();
            presentationOwner.FilterText = "needle";
            var activePresentation = presentationOwner.FilterPresentation;
            var currentPresentation = InactiveFilterPresentation;
            var roots = new ObservableCollection<ProjectBrowserNodeViewModel>
            {
                presentationOwner.RootNodes[0]
            };
            var viewModel = CreateLoadedViewModel(roots);
            var notifyingViewModel = viewModel.As<INotifyPropertyChanged>();
            viewModel.SetupGet(x => x.FilterPresentation).Returns(() => currentPresentation);
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            var initialRenderCount = component.RenderCount;
            Assert.That(component.Markup, Does.Contain("Sibling"));

            currentPresentation = activePresentation;
            notifyingViewModel.Raise(
                x => x.PropertyChanged += null,
                new PropertyChangedEventArgs(nameof(IProjectBrowserViewModel.FilterPresentation)));

            component.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.RenderCount, Is.GreaterThan(initialRenderCount));
                    Assert.That(component.Markup, Does.Contain("Needle"));
                    Assert.That(component.Markup, Does.Not.Contain("Sibling"));
                }
            });
        }

        /// <summary>
        /// Verifies ViewModel replacement detaches the old top-level filter presentation publisher.
        /// </summary>
        [Test]
        public async Task VerifyViewModelReplacementDetachesOldFilterPresentationPublication()
        {
            using var firstPresentationOwner = await ProjectBrowserNodeTestFactory.CreateFilterTreeViewModelAsync();
            using var secondPresentationOwner = await ProjectBrowserNodeTestFactory.CreateFilterTreeViewModelAsync();
            firstPresentationOwner.FilterText = "needle";
            secondPresentationOwner.FilterText = "needle";
            var firstActivePresentation = firstPresentationOwner.FilterPresentation;
            var secondActivePresentation = secondPresentationOwner.FilterPresentation;
            var firstCurrentPresentation = InactiveFilterPresentation;
            var secondCurrentPresentation = InactiveFilterPresentation;
            var firstViewModel = CreateLoadedViewModel(
                new ObservableCollection<ProjectBrowserNodeViewModel> { firstPresentationOwner.RootNodes[0] });
            var secondViewModel = CreateLoadedViewModel(
                new ObservableCollection<ProjectBrowserNodeViewModel> { secondPresentationOwner.RootNodes[0] });
            var firstNotifyingViewModel = firstViewModel.As<INotifyPropertyChanged>();
            var secondNotifyingViewModel = secondViewModel.As<INotifyPropertyChanged>();
            firstViewModel.SetupGet(x => x.FilterPresentation).Returns(() => firstCurrentPresentation);
            secondViewModel.SetupGet(x => x.FilterPresentation).Returns(() => secondCurrentPresentation);
            this.RegisterViewModel(firstViewModel.Object);

            using var component = this.RenderProjectBrowser();
            component.Render(parameters => parameters
                .Add(browser => browser.ViewModel, secondViewModel.Object));
            var replacementRenderCount = component.RenderCount;

            firstCurrentPresentation = firstActivePresentation;
            firstNotifyingViewModel.Raise(
                x => x.PropertyChanged += null,
                new PropertyChangedEventArgs(nameof(IProjectBrowserViewModel.FilterPresentation)));

            Assert.That(component.RenderCount, Is.EqualTo(replacementRenderCount));

            secondCurrentPresentation = secondActivePresentation;
            secondNotifyingViewModel.Raise(
                x => x.PropertyChanged += null,
                new PropertyChangedEventArgs(nameof(IProjectBrowserViewModel.FilterPresentation)));

            component.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.RenderCount, Is.GreaterThan(replacementRenderCount));
                    Assert.That(component.Markup, Does.Contain("Needle"));
                    Assert.That(component.Markup, Does.Not.Contain("Sibling"));
                }
            });
        }

        /// <summary>
        /// Verifies generated field identifiers remain isolated across Project Browser instances.
        /// </summary>
        [Test]
        public void VerifyMultipleInstancesGenerateUniqueFilterControlIds()
        {
            var roots = new ObservableCollection<ProjectBrowserNodeViewModel>();
            var viewModel = CreateLoadedViewModel(roots);
            this.RegisterViewModel(viewModel.Object);

            using var firstComponent = this.RenderProjectBrowser();
            using var secondComponent = this.RenderProjectBrowser();
            var firstSearchId = firstComponent.Find("input[type='search']").Id;
            var secondSearchId = secondComponent.Find("input[type='search']").Id;
            var firstSelectId = firstComponent.Find(".mb-select-input__trigger").Id;
            var secondSelectId = secondComponent.Find(".mb-select-input__trigger").Id;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstSearchId, Is.Not.Empty);
                Assert.That(secondSearchId, Is.Not.Empty);
                Assert.That(firstSelectId, Is.Not.Empty);
                Assert.That(secondSelectId, Is.Not.Empty);
                Assert.That(secondSearchId, Is.Not.EqualTo(firstSearchId));
                Assert.That(secondSelectId, Is.Not.EqualTo(firstSelectId));
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
            var viewModel = CreateProjectBrowserViewModelMock();
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
            var viewModel = CreateProjectBrowserViewModelMock();
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
            var viewModel = CreateProjectBrowserViewModelMock();
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
            var viewModel = CreateProjectBrowserViewModelMock();
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
            var viewModel = CreateProjectBrowserViewModelMock();
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
            var viewModel = CreateProjectBrowserViewModelMock();
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
            var viewModel = CreateProjectBrowserViewModelMock();
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
        /// Verifies replacing a disposed component does not cancel initialization owned by its caller-owned ViewModel.
        /// </summary>
        [Test]
        public async Task VerifyComponentReplacementKeepsRealViewModelInitializationAlive()
        {
            using var loadStarted = new ManualResetEventSlim();
            using var releaseLoad = new ManualResetEventSlim();
            var model = new Mock<INamespace>();
            model.SetupGet(x => x.ElementId).Returns("root");
            model.SetupGet(x => x.DeclaredName).Returns("Root");
            model.SetupGet(x => x.ownedElement).Returns([]);
            var modelLoaderService = new Mock<IModelLoaderService>();
            modelLoaderService
                .Setup(x => x.LoadQuantitiesModel())
                .Returns(() =>
                {
                    loadStarted.Set();

                    if (!releaseLoad.Wait(TimeSpan.FromSeconds(10)))
                    {
                        throw new TimeoutException("The test did not release model loading.");
                    }

                    return model.Object;
                });
            using var viewModel = new ProjectBrowserViewModel(
                modelLoaderService.Object,
                new ContextAwareService());
            this.RegisterViewModel(viewModel);

            using var firstComponent = this.RenderProjectBrowser();
            Assert.That(loadStarted.Wait(TimeSpan.FromSeconds(10)), Is.True);
            IRenderedComponent<ProjectBrowserComponent> replacementComponent = null;

            await this.Renderer.Dispatcher.InvokeAsync(() =>
            {
                replacementComponent = this.RenderProjectBrowser();
                firstComponent.Instance.Dispose();
            });

            using (replacementComponent)
            {
                var initializationCompleted = new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                PropertyChangedEventHandler loadedHandler = (_, args) =>
                {
                    if (args.PropertyName == nameof(ProjectBrowserViewModel.IsLoaded)
                        && viewModel.IsLoaded)
                    {
                        initializationCompleted.TrySetResult(true);
                    }
                };
                viewModel.PropertyChanged += loadedHandler;

                try
                {
                    releaseLoad.Set();
                    await initializationCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));
                }
                finally
                {
                    viewModel.PropertyChanged -= loadedHandler;
                }

                replacementComponent.WaitForAssertion(() =>
                    Assert.That(
                        replacementComponent.FindAll(".mb-project-browser-node__row"),
                        Has.Count.EqualTo(1)),
                    TimeSpan.FromSeconds(10));

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(viewModel.IsLoaded, Is.True);
                    Assert.That(viewModel.IsLoading, Is.False);
                    Assert.That(viewModel.ErrorMessage, Is.Empty);
                    modelLoaderService.Verify(x => x.LoadQuantitiesModel(), Times.Once);
                }
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
            var viewModel = CreateProjectBrowserViewModelMock();
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
            var viewModel = CreateProjectBrowserViewModelMock();
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
            var viewModel = CreateProjectBrowserViewModelMock();
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
            var viewModel = CreateProjectBrowserViewModelMock();
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
            var viewModel = CreateProjectBrowserViewModelMock();
            viewModel.SetupGet(x => x.RootNodes)
                .Returns(new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(rootSource));
            viewModel.SetupGet(x => x.IsLoaded).Returns(true);
            viewModel.SetupGet(x => x.IsLoading).Returns(false);
            viewModel.SetupGet(x => x.ErrorMessage).Returns(string.Empty);
            viewModel.Setup(x => x.Dispose());

            return viewModel;
        }

        /// <summary>
        /// Creates a strict Project Browser contract with ordinary inactive filter state.
        /// </summary>
        /// <returns>The configured strict ViewModel mock.</returns>
        private static Mock<IProjectBrowserViewModel> CreateProjectBrowserViewModelMock()
        {
            var viewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            viewModel.SetupProperty(x => x.FilterText, string.Empty);
            viewModel.SetupProperty(x => x.ElementKindFilter, null);
            viewModel.SetupGet(x => x.FilterPresentation).Returns(InactiveFilterPresentation);
            viewModel.Setup(x => x.ClearFilter());

            return viewModel;
        }

        /// <summary>
        /// Gets an inactive immutable snapshot from its real production owner.
        /// </summary>
        /// <returns>The all-visible filter presentation.</returns>
        private static ProjectBrowserFilterPresentation CreateInactiveFilterPresentation()
        {
            using var viewModel = new ProjectBrowserViewModel(
                new Mock<IModelLoaderService>(MockBehavior.Strict).Object,
                new ContextAwareService());

            return viewModel.FilterPresentation;
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
