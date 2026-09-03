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
    using BlazorBlueprint.Primitives.Services;

    using Bunit;

    using Microsoft.AspNetCore.Components.Web;
    using Moq;

    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Parts;

    using ProjectBrowserComponent = Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser.ProjectBrowser;
    using ProjectBrowserNodeComponent = Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser.ProjectBrowserNode;
    using ProjectBrowserSearchAssistantComponent = Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser.ProjectBrowserSearchAssistant;
    using SearchInputComponent = Mycelium.Bloom.Components.UI.Atoms.SearchInput.SearchInput;

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
        /// The selected type-chip labels expected from the active type criteria.
        /// </summary>
        private static readonly string[] ExpectedSelectedTypeChipLabels =
            ["«partdefinition»", "«partusage»", "«namespace»"];

        /// <summary>
        /// The visible node titles expected for a deep filtered match.
        /// </summary>
        private static readonly string[] ExpectedVisibleFilterPathTitles = ["Root", "Branch", "Needle"];

        /// <summary>
        /// The immutable all-visible presentation used by ordinary strict interface scenarios.
        /// </summary>
        private static readonly ProjectBrowserFilterPresentation InactiveFilterPresentation =
            CreateInactiveFilterPresentation();

        /// <summary>
        /// The concrete model types exposed by ordinary Project Browser test doubles.
        /// </summary>
        private static readonly ReadOnlyObservableCollection<Type> DefaultAvailableElementTypes =
            new(new ObservableCollection<Type>
            {
                typeof(PartDefinition),
                typeof(PartUsage),
                typeof(Namespace)
            });

        /// <summary>
        /// The empty selected-Type state exposed by ordinary Project Browser test doubles.
        /// </summary>
        private static readonly ReadOnlyObservableCollection<Type> EmptySelectedElementTypes =
            new(new ObservableCollection<Type>());

        /// <summary>
        /// The Blueprint portal host that owns the Project Browser filter drawer.
        /// </summary>
        private readonly IRenderedComponent<BbPortalHost> portalHost;

        /// <summary>
        /// The caller-owned ViewModel supplied to the component under test.
        /// </summary>
        private IProjectBrowserViewModel registeredViewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectBrowserTestFixture" /> class.
        /// </summary>
        public ProjectBrowserTestFixture()
        {
            this.portalHost = BlueprintTestSetup.ConfigureWithPortalHost(this);
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
            var overlayStyle = File.ReadAllText(Path.Combine(
                repositoryRoot,
                "Mycelium.Bloom",
                "Styles",
                "blueprint-overlays.css"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rootChildren, Has.Count.EqualTo(2));
                Assert.That(rootChildren[0].ClassList, Does.Contain("mb-project-browser__toolbar"));
                Assert.That(rootChildren[1].ClassList, Does.Contain("mb-project-browser__tree-viewport"));
                Assert.That(
                    style,
                    Does.Match(@"(?s)\.mb-project-browser__toolbar\s*\{[^}]*flex:\s*0\s+0\s+auto;"));
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
                Assert.That(
                    overlayStyle,
                    Does.Match(
                        @"(?s)\.mb-project-browser__filter-popover\s*\{[^}]*width:\s*min\(296px,\s*calc\(100vw\s*-\s*\(2\s*\*\s*var\(--mb-spacing-2\)\)\)\);"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-project-browser__filter-drawer\s*\{[^}]*max-height:\s*min\(676px,"));
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
            var filterRegion = component.Find(".mb-project-browser__toolbar");
            var searchInput = component.FindComponent<SearchInputComponent>();
            var filterTrigger = component.Find("button[aria-label='Open project browser filters']");
            var searchAssistantPopover = component.FindComponent<ProjectBrowserSearchAssistantComponent>()
                .FindComponent<BbPopover>();
            var filterDrawerPopover = GetFilterDrawerPopover(component);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rootChildren, Has.Count.EqualTo(2));
                Assert.That(rootChildren[0].ClassList, Does.Contain("mb-project-browser__toolbar"));
                Assert.That(rootChildren[1].ClassList, Does.Contain("mb-project-browser__tree-viewport"));
                Assert.That(filterRegion.GetAttribute("role"), Is.EqualTo("search"));
                Assert.That(filterRegion.GetAttribute("aria-label"), Is.EqualTo("Project browser filters"));
                Assert.That(filterRegion.Children, Has.Count.EqualTo(2));
                Assert.That(filterRegion.Children[0].ClassList, Does.Contain("mb-project-browser__search"));
                Assert.That(filterRegion.Children[1].ClassList, Does.Contain("mb-project-browser__filter-trigger-host"));
                Assert.That(searchInput.Instance.Value, Is.Empty);
                Assert.That(searchInput.Instance.FullWidth, Is.True);
                Assert.That(searchInput.Instance.Placeholder, Is.EqualTo("Search elements..."));
                Assert.That(
                    searchInput.Instance.AriaLabel,
                    Is.EqualTo("Filter project browser by name or qualified name"));
                Assert.That(searchInput.Instance.EnableShortcut, Is.False);
                Assert.That(filterTrigger.GetAttribute("title"), Is.EqualTo("Open project browser filters"));
                Assert.That(filterTrigger.GetAttribute("data-filter-active"), Is.EqualTo("false"));
                Assert.That(searchAssistantPopover.Instance.Open, Is.False);
                Assert.That(searchAssistantPopover.Instance.RestoreFocusOnClose, Is.False);
                Assert.That(filterDrawerPopover.Instance.Open, Is.False);
                Assert.That(filterDrawerPopover.Instance.RestoreFocusOnClose, Is.True);
                Assert.That(this.portalHost.FindAll(".mb-project-browser__filter-drawer"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies committed search and Type controls write directly to the caller-owned ViewModel state.
        /// </summary>
        [Test]
        public async Task VerifyFilterControlsWriteDirectlyToViewModel()
        {
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel>();
            var viewModel = CreateLoadedViewModel(mutableRoots);
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            var searchInput = component.FindComponent<SearchInputComponent>();

            await component.InvokeAsync(() => searchInput.Instance.ValueChanged.InvokeAsync("  needle  "));
            await component.Find("input[role='combobox']").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });
            await this.OpenFilterDrawerAsync(component);
            await this.portalHost.FindAll("button[aria-pressed]")
                .Single(button => button.TextContent.Contains("«partdefinition»", StringComparison.Ordinal))
                .ClickAsync();

            Assert.That(viewModel.Object.FilterText, Is.EqualTo("needle"));
            await this.portalHost.WaitForAssertionAsync(() =>
                viewModel.Verify(
                    owner => owner.ToggleElementTypeFilter(typeof(PartDefinition)),
                    Times.Once));
        }

        /// <summary>
        /// Verifies useful suggestions open only for a populated draft and derive Type rows from the concrete type model.
        /// </summary>
        [Test]
        public async Task VerifySearchAssistantOpensWithContainsAndRealTypeSuggestions()
        {
            var viewModel = CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>());
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            var input = component.Find("input[role='combobox']");
            var assistantPopover = GetSearchAssistantPopover(component);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(input.GetAttribute("type"), Is.EqualTo("text"));
                Assert.That(input.GetAttribute("aria-expanded"), Is.EqualTo("false"));
                Assert.That(assistantPopover.Instance.Open, Is.False);
                Assert.That(this.portalHost.FindAll(".mb-project-browser-search-assistant__surface"), Is.Empty);
            }

            await this.OpenSearchAssistantAsync(component, "def");
            var surface = this.portalHost.Find(".mb-project-browser-search-assistant__surface");
            var options = surface.QuerySelectorAll("[role='option']");
            var popoverContent = component.FindComponent<ProjectBrowserSearchAssistantComponent>()
                .FindComponent<BbPopoverContent>();
            var controlledListbox = this.portalHost.Find($"#{input.GetAttribute("aria-controls")}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(input.GetAttribute("aria-expanded"), Is.EqualTo("true"));
                Assert.That(input.GetAttribute("aria-controls"), Is.Not.Empty);
                Assert.That(surface.TextContent, Does.Contain("Contains \"def\""));
                Assert.That(options, Has.Count.EqualTo(2));
                Assert.That(options[0].TextContent, Does.Contain("Contains \"def\""));
                Assert.That(options[1].TextContent, Does.Contain("«partdefinition»"));
                Assert.That(controlledListbox.GetAttribute("role"), Is.EqualTo("listbox"));
                Assert.That(controlledListbox.GetAttribute("aria-label"), Is.EqualTo("Search suggestions"));
                Assert.That(popoverContent.Instance.CloseOnEscape, Is.True);
                Assert.That(popoverContent.Instance.CloseOnClickOutside, Is.True);
                Assert.That(this.portalHost.Find("[role='region']").GetAttribute("aria-label"),
                    Is.EqualTo("Project browser search suggestions"));
                Assert.That(viewModel.Object.FilterText, Is.Empty);
            }
        }

        /// <summary>
        /// Verifies an empty Space opens discovery over the loaded model's concrete non-relationship types.
        /// </summary>
        [Test]
        public async Task VerifySearchAssistantEmptySpaceDiscoversAvailableElementTypes()
        {
            var firstDefinition = ProjectBrowserNodeTestFactory.CreateElement<PartDefinition>(
                "first-definition",
                "First definition");
            var secondDefinition = ProjectBrowserNodeTestFactory.CreateElement<PartDefinition>(
                "second-definition",
                "Second definition");
            var relationship = ProjectBrowserNodeTestFactory.CreateElement<Membership>(
                "membership",
                "Membership");
            var root = ProjectBrowserNodeTestFactory.CreateElement<Namespace>(
                "root",
                "Root",
                firstDefinition,
                secondDefinition,
                relationship);
            var modelLoader = new Mock<IModelLoaderService>(MockBehavior.Strict);
            modelLoader.Setup(loader => loader.LoadQuantitiesModel()).Returns(root);
            using var viewModel = new ProjectBrowserViewModel(modelLoader.Object, new ContextAwareService());
            Assert.That(await viewModel.InitializeAsync(CancellationToken.None), Is.True);
            this.RegisterViewModel(viewModel);

            using var component = this.RenderProjectBrowser();
            var stableSearchInput = component.FindComponent<SearchInputComponent>().Instance;
            var input = component.Find("input[role='combobox']");

            await component.InvokeAsync(() => stableSearchInput.FocusAsync().AsTask());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(GetSearchAssistantPopover(component).Instance.Open, Is.False);
                Assert.That(this.portalHost.FindAll(".mb-project-browser-search-assistant__surface"), Is.Empty);
            }

            await input.KeyDownAsync(new KeyboardEventArgs { Key = " " });
            await this.portalHost.WaitForAssertionAsync(() =>
                Assert.That(this.portalHost.FindAll(".mb-project-browser-search-assistant__surface"),
                    Has.Count.EqualTo(1)));

            var surface = this.portalHost.Find(".mb-project-browser-search-assistant__surface");
            var options = surface.QuerySelectorAll("[role='option']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindComponent<SearchInputComponent>().Instance, Is.SameAs(stableSearchInput));
                Assert.That(input.GetAttribute("value"), Is.Empty);
                Assert.That(viewModel.FilterText, Is.Empty);
                Assert.That(options, Has.Count.EqualTo(2));
                Assert.That(surface.TextContent, Does.Contain("«namespace»"));
                Assert.That(surface.TextContent, Does.Contain("«partdefinition»"));
                Assert.That(surface.TextContent, Does.Not.Contain("Contains \"\""));
                Assert.That(surface.TextContent, Does.Not.Contain("«partusage»"));
                Assert.That(surface.TextContent, Does.Not.Contain("«membership»"));
                Assert.That(viewModel.AvailableElementTypes, Does.Not.Contain(typeof(Membership)));
            }
        }

        /// <summary>
        /// Verifies discovery keeps keyboard dismissal and selection on the stable search input.
        /// </summary>
        [Test]
        public async Task VerifySearchAssistantDiscoverySupportsRepeatedKeyboardInteractionWithoutRemount()
        {
            var viewModel = CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>());
            var selectedTypes = ConfigureSelectedElementTypes(viewModel);
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            var stableSearchInput = component.FindComponent<SearchInputComponent>().Instance;
            var stableInputId = component.Find("input[role='combobox']").Id;

            for (var iteration = 0; iteration < 3; iteration++)
            {
                await component.Find("input[role='combobox']")
                    .KeyDownAsync(new KeyboardEventArgs { Key = " " });
                await this.portalHost.WaitForAssertionAsync(() =>
                    Assert.That(GetSearchAssistantPopover(component).Instance.Open, Is.True));

                await component.Find("input[role='combobox']")
                    .KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
                await this.portalHost.WaitForAssertionAsync(() =>
                    Assert.That(GetSearchAssistantPopover(component).Instance.Open, Is.False));
            }

            await component.Find("input[role='combobox']")
                .KeyDownAsync(new KeyboardEventArgs { Key = " " });
            await component.Find("input[role='combobox']")
                .KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
            await this.portalHost.WaitForAssertionAsync(() =>
                Assert.That(this.portalHost.Find("[role='option'][data-focused='true']").TextContent,
                    Does.Contain("«partdefinition»")));
            await component.Find("input[role='combobox']")
                .KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectedTypes, Is.EquivalentTo(new[] { typeof(PartDefinition) }));
                Assert.That(component.FindComponent<SearchInputComponent>().Instance, Is.SameAs(stableSearchInput));
                Assert.That(component.Find("input[role='combobox']").Id, Is.EqualTo(stableInputId));
                Assert.That(GetSearchAssistantPopover(component).Instance.Open, Is.False);
            }
        }

        /// <summary>
        /// Verifies Space remains ordinary searchable text after a draft has started.
        /// </summary>
        [Test]
        public async Task VerifySearchAssistantNonEmptyDraftAcceptsNormalSpace()
        {
            var viewModel = CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>());
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            await this.OpenSearchAssistantAsync(component, "multi");
            var searchInput = component.FindComponent<SearchInputComponent>();

            await component.Find("input[role='combobox']")
                .KeyDownAsync(new KeyboardEventArgs { Key = " " });
            await component.InvokeAsync(() => searchInput.Instance.ValueChanged.InvokeAsync("multi word"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("input[role='combobox']").GetAttribute("value"), Is.EqualTo("multi word"));
                Assert.That(GetSearchAssistantPopover(component).Instance.Open, Is.True);
                Assert.That(this.portalHost.Find(".mb-project-browser-search-assistant__surface").TextContent,
                    Does.Contain("Contains \"multi word\""));
                Assert.That(viewModel.Object.FilterText, Is.Empty);
            }
        }

        /// <summary>
        /// Verifies Arrow keys use Blueprint command focus and Enter accepts the highlighted real Type suggestion.
        /// </summary>
        [Test]
        public async Task VerifySearchAssistantKeyboardNavigationAcceptsHighlightedType()
        {
            var viewModel = CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>());
            var selectedTypes = ConfigureSelectedElementTypes(viewModel);
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            await this.OpenSearchAssistantAsync(component, "definition");
            var input = component.Find("input[role='combobox']");

            await input.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
            await this.portalHost.WaitForAssertionAsync(() =>
                Assert.That(this.portalHost.Find("[role='option'][data-focused='true']").TextContent,
                    Does.Contain("Contains")));

            await input.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
            await this.portalHost.WaitForAssertionAsync(() =>
                Assert.That(this.portalHost.Find("[role='option'][data-focused='true']").TextContent,
                    Does.Contain("«partdefinition»")));

            await input.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowUp" });
            await this.portalHost.WaitForAssertionAsync(() =>
                Assert.That(this.portalHost.Find("[role='option'][data-focused='true']").TextContent,
                    Does.Contain("Contains")));

            await input.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
            await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

            await this.portalHost.WaitForAssertionAsync(() =>
                Assert.That(this.portalHost.FindAll(".mb-project-browser-search-assistant__surface"), Is.Empty));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.Object.FilterText, Is.Empty);
                Assert.That(selectedTypes, Is.EquivalentTo(new[] { typeof(PartDefinition) }));
                Assert.That(component.Find("button[aria-label='Open project browser filters']")
                    .GetAttribute("data-filter-active"), Is.EqualTo("true"));
                Assert.That(component.Find(".mb-project-browser-search-assistant__criterion--type").TextContent,
                    Does.Contain("Type «partdefinition»"));
                viewModel.Verify(
                    owner => owner.ToggleElementTypeFilter(typeof(PartDefinition)),
                    Times.Once);
            }
        }

        /// <summary>
        /// Verifies Escape retains only the transient draft and refocus cannot reopen stale suggestions.
        /// </summary>
        [Test]
        public async Task VerifySearchAssistantEscapeRetainsDraftWithoutRefocusFlash()
        {
            var viewModel = CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>());
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            await this.OpenSearchAssistantAsync(component, "thruster");
            var input = component.Find("input[role='combobox']");

            await input.KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

            await this.portalHost.WaitForAssertionAsync(() =>
                Assert.That(this.portalHost.FindAll(".mb-project-browser-search-assistant__surface"), Is.Empty));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.Object.FilterText, Is.Empty);
                Assert.That(input.GetAttribute("value"), Is.EqualTo("thruster"));
                Assert.That(input.GetAttribute("aria-expanded"), Is.EqualTo("false"));
            }

            await input.TriggerEventAsync("onblur", new FocusEventArgs());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(GetSearchAssistantPopover(component).Instance.Open, Is.False);
                Assert.That(this.portalHost.FindAll(".mb-project-browser-search-assistant__surface"), Is.Empty);
                Assert.That(input.GetAttribute("value"), Is.EqualTo("thruster"));
            }

            await component.InvokeAsync(() => component.FindComponent<SearchInputComponent>()
                .Instance.ValueChanged.InvokeAsync("thrusters"));

            await this.portalHost.WaitForAssertionAsync(() =>
                Assert.That(this.portalHost.FindAll(".mb-project-browser-search-assistant__surface"),
                    Has.Count.EqualTo(1)));
        }

        /// <summary>
        /// Verifies plain Enter commits the default Contains interpretation and renders its removable token.
        /// </summary>
        [Test]
        public async Task VerifySearchAssistantPlainEnterCommitsContainsCriterion()
        {
            var viewModel = CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>());
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            await this.OpenSearchAssistantAsync(component, "thruster");
            var input = component.Find("input[role='combobox']");

            await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

            await this.portalHost.WaitForAssertionAsync(() =>
                Assert.That(this.portalHost.FindAll(".mb-project-browser-search-assistant__surface"), Is.Empty));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.Object.FilterText, Is.EqualTo("thruster"));
                Assert.That(input.GetAttribute("value"), Is.Empty);
                Assert.That(input.GetAttribute("aria-expanded"), Is.EqualTo("false"));
                Assert.That(component.Find(".mb-project-browser-search-assistant__criterion").TextContent,
                    Does.Contain("Contains \"thruster\""));
                Assert.That(component.Find("button[aria-label='Remove Contains thruster search criterion']"),
                    Is.Not.Null);
            }
        }

        /// <summary>
        /// Verifies plain Enter chooses Contains even when the draft also has a matching Type suggestion.
        /// </summary>
        [Test]
        public async Task VerifySearchAssistantPlainEnterPrefersContainsWithoutExplicitNavigation()
        {
            var viewModel = CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>());
            var selectedTypes = ConfigureSelectedElementTypes(viewModel);
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            await this.OpenSearchAssistantAsync(component, "definition");
            await component.Find("input[role='combobox']").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.Object.FilterText, Is.EqualTo("definition"));
                Assert.That(selectedTypes, Is.Empty);
                Assert.That(component.Find(".mb-project-browser-search-assistant__criterion").TextContent,
                    Does.Contain("Contains \"definition\""));
                viewModel.Verify(
                    owner => owner.ToggleElementTypeFilter(It.IsAny<Type>()),
                    Times.Never);
            }
        }

        /// <summary>
        /// Verifies focusing a search that already owns a committed token never opens the transient assistant.
        /// </summary>
        [Test]
        public void VerifySearchAssistantFocusDoesNotOpenFromCommittedCriterion()
        {
            var viewModel = CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>());
            viewModel.Object.FilterText = "thruster";
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            var input = component.Find("input[role='combobox']");
            var searchInput = component.FindComponent<SearchInputComponent>().Instance;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(searchInput.ForwardTriggerFocus, Is.False);
                Assert.That(searchInput.ForwardTriggerKeyDown, Is.False);
                Assert.That(GetSearchAssistantPopover(component).Instance.Open, Is.False);
                Assert.That(this.portalHost.FindAll(".mb-project-browser-search-assistant__surface"), Is.Empty);
                Assert.That(input.GetAttribute("value"), Is.Empty);
                Assert.That(component.Find(".mb-project-browser-search-assistant__criterion").TextContent,
                    Does.Contain("Contains \"thruster\""));
            }
        }

        /// <summary>
        /// Verifies criterion remove buttons update only their authoritative ViewModel state and restore the editable field.
        /// </summary>
        [Test]
        public void VerifySearchCriterionTokensRemoveContainsAndTypeIndependently()
        {
            var viewModel = CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>());
            viewModel.Object.FilterText = "thruster";
            var selectedTypes = ConfigureSelectedElementTypes(viewModel, typeof(PartDefinition));
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            var criteriaGroup = component.Find("[aria-label='Active project browser search criteria']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(criteriaGroup.GetAttribute("role"), Is.EqualTo("group"));
                Assert.That(component.FindAll(".mb-project-browser-search-assistant__criterion"), Has.Count.EqualTo(2));
                Assert.That(component.Find("input[role='combobox']").GetAttribute("value"), Is.Empty);
            }

            component.Find("button[aria-label='Remove Contains thruster search criterion']").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.Object.FilterText, Is.Empty);
                Assert.That(selectedTypes, Is.EquivalentTo(new[] { typeof(PartDefinition) }));
                Assert.That(component.FindAll(".mb-project-browser-search-assistant__criterion"), Has.Count.EqualTo(1));
            }

            component.FindAll("button.mb-project-browser-search-assistant__criterion-remove")
                .Single()
                .Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectedTypes, Is.Empty);
                Assert.That(component.FindAll(".mb-project-browser-search-assistant__criterion"), Is.Empty);
                viewModel.Verify(
                    owner => owner.ToggleElementTypeFilter(typeof(PartDefinition)),
                    Times.Once);
            }
        }

        /// <summary>
        /// Verifies repeated default acceptance cannot accumulate duplicate equivalent Contains tokens.
        /// </summary>
        [Test]
        public async Task VerifySearchAssistantRepeatedContainsAcceptanceRemainsSingleCriterion()
        {
            var viewModel = CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>());
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            await this.OpenSearchAssistantAsync(component, "thruster");
            await component.Find("input[role='combobox']").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });
            await this.OpenSearchAssistantAsync(component, "thruster");
            await component.Find("input[role='combobox']").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.Object.FilterText, Is.EqualTo("thruster"));
                Assert.That(component.FindAll(".mb-project-browser-search-assistant__criterion"), Has.Count.EqualTo(1));
                Assert.That(component.Find(".mb-project-browser-search-assistant__criterion").TextContent,
                    Does.Contain("Contains \"thruster\""));
            }
        }

        /// <summary>
        /// Verifies a committed Type never prevents repeated Contains composition on the same search input.
        /// </summary>
        [Test]
        public async Task VerifySearchAssistantComposesContainsAfterTypeWithoutRemount()
        {
            const int compositionCount = 5;
            var viewModel = CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>());
            var selectedTypes = ConfigureSelectedElementTypes(viewModel);
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            var stableSearchInput = component.FindComponent<SearchInputComponent>().Instance;
            var stableInputId = component.Find("input[role='combobox']").Id;

            await this.OpenSearchAssistantAsync(component, "usage");
            await this.portalHost.FindAll("[role='option']")
                .Single(option => option.TextContent.Contains("«partusage»", StringComparison.Ordinal))
                .ClickAsync();

            for (var iteration = 0; iteration < compositionCount; iteration++)
            {
                var committedDraft = $"mre-{iteration}";
                await this.OpenSearchAssistantAsync(component, committedDraft);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(GetSearchAssistantPopover(component).Instance.Open, Is.True);
                    Assert.That(this.portalHost.Find(".mb-project-browser-search-assistant__surface").TextContent,
                        Does.Contain($"Contains \"{committedDraft}\""));
                }

                await component.Find("input[role='combobox']")
                    .KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

                var nextDraft = $"third-{iteration}";
                await this.OpenSearchAssistantAsync(component, nextDraft);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(viewModel.Object.FilterText, Is.EqualTo(committedDraft));
                    Assert.That(selectedTypes, Is.EquivalentTo(new[] { typeof(PartUsage) }));
                    Assert.That(component.FindAll(".mb-project-browser-search-assistant__criterion"), Has.Count.EqualTo(2));
                    Assert.That(component.FindComponent<SearchInputComponent>().Instance, Is.SameAs(stableSearchInput));
                    Assert.That(component.Find("input[role='combobox']").Id, Is.EqualTo(stableInputId));
                    Assert.That(this.portalHost.Find(".mb-project-browser-search-assistant__surface").TextContent,
                        Does.Contain($"Contains \"{nextDraft}\""));
                }

                await component.Find("input[role='combobox']")
                    .KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
            }
        }

        /// <summary>
        /// Verifies a committed Contains criterion remains active while Type is added and another draft begins.
        /// </summary>
        [Test]
        public async Task VerifySearchAssistantComposesTypeAfterContainsWithoutRemount()
        {
            var viewModel = CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>());
            var selectedTypes = ConfigureSelectedElementTypes(viewModel);
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            var stableSearchInput = component.FindComponent<SearchInputComponent>().Instance;

            await this.OpenSearchAssistantAsync(component, "thruster");
            await component.Find("input[role='combobox']")
                .KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });
            await this.OpenSearchAssistantAsync(component, "usage");
            await this.portalHost.FindAll("[role='option']")
                .Single(option => option.TextContent.Contains("«partusage»", StringComparison.Ordinal))
                .ClickAsync();
            await this.OpenSearchAssistantAsync(component, "next criterion");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.Object.FilterText, Is.EqualTo("thruster"));
                Assert.That(selectedTypes, Is.EquivalentTo(new[] { typeof(PartUsage) }));
                Assert.That(component.FindAll(".mb-project-browser-search-assistant__criterion"), Has.Count.EqualTo(2));
                Assert.That(component.FindComponent<SearchInputComponent>().Instance, Is.SameAs(stableSearchInput));
                Assert.That(GetSearchAssistantPopover(component).Instance.Open, Is.True);
                Assert.That(this.portalHost.Find(".mb-project-browser-search-assistant__surface").TextContent,
                    Does.Contain("Contains \"next criterion\""));
            }
        }

        /// <summary>
        /// Verifies repeated commit, compose, remove, and clear interactions never desynchronize durable criteria.
        /// </summary>
        [Test]
        public async Task VerifySearchAssistantCompositionStressMaintainsCanonicalState()
        {
            const int iterationCount = 50;
            using var viewModel = await ProjectBrowserNodeTestFactory.CreateFilterTreeViewModelAsync();
            var canonicalRoot = viewModel.RootNodes.Single();
            var canonicalChildren = canonicalRoot.Children.ToArray();
            canonicalRoot.IsExpanded = true;
            var selectedNode = viewModel.SelectedNode;
            this.RegisterViewModel(viewModel);

            using var component = this.RenderProjectBrowser();
            var stableSearchInput = component.FindComponent<SearchInputComponent>().Instance;

            for (var iteration = 0; iteration < iterationCount; iteration++)
            {
                var firstDraft = $"alpha-{iteration}";
                await this.OpenSearchAssistantAsync(component, firstDraft);
                await component.Find("input[role='combobox']")
                    .KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

                await this.OpenSearchAssistantAsync(component, "usage");
                await this.portalHost.FindAll("[role='option']")
                    .Single(option => option.TextContent.Contains("«partusage»", StringComparison.Ordinal))
                    .ClickAsync();

                var thirdDraft = $"gamma-{iteration}";
                await this.OpenSearchAssistantAsync(component, thirdDraft);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(GetSearchAssistantPopover(component).Instance.Open, Is.True);
                    Assert.That(viewModel.FilterText, Is.EqualTo(firstDraft));
                    Assert.That(viewModel.SelectedElementTypes, Is.EquivalentTo(new[] { typeof(PartUsage) }));
                    Assert.That(component.FindAll(".mb-project-browser-search-assistant__criterion"), Has.Count.EqualTo(2));
                }

                await component.Find($"button[aria-label='Remove Contains {firstDraft} search criterion']")
                    .ClickAsync();

                await component.WaitForAssertionAsync(() =>
                    Assert.That(GetSearchAssistantPopover(component).Instance.Open, Is.False));

                await this.OpenSearchAssistantAsync(component, $"delta-{iteration}");
                await this.OpenFilterDrawerAsync(component);
                await this.portalHost.Find("button.mb-project-browser__clear-all").ClickAsync();

                var finalDraft = $"epsilon-{iteration}";
                await this.OpenSearchAssistantAsync(component, finalDraft);
                await component.Find("input[role='combobox']")
                    .KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(viewModel.FilterText, Is.EqualTo(finalDraft));
                    Assert.That(viewModel.SelectedElementTypes, Is.Empty);
                    Assert.That(viewModel.FilterPresentation.IsActive, Is.True);
                    Assert.That(component.FindAll(".mb-project-browser-search-assistant__criterion"), Has.Count.EqualTo(1));
                    Assert.That(component.FindComponent<SearchInputComponent>().Instance, Is.SameAs(stableSearchInput));
                    Assert.That(viewModel.RootNodes.Single(), Is.SameAs(canonicalRoot));
                    Assert.That(canonicalRoot.Children, Is.EqualTo(canonicalChildren));
                    Assert.That(canonicalRoot.IsExpanded, Is.True);
                    Assert.That(viewModel.SelectedNode, Is.SameAs(selectedNode));
                }
            }

            viewModel.ClearFilter();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.FilterText, Is.Empty);
                Assert.That(viewModel.SelectedElementTypes, Is.Empty);
                Assert.That(viewModel.FilterPresentation.IsActive, Is.False);
                Assert.That(viewModel.RootNodes.Single(), Is.SameAs(canonicalRoot));
                Assert.That(canonicalRoot.Children, Is.EqualTo(canonicalChildren));
                Assert.That(canonicalRoot.IsExpanded, Is.True);
                Assert.That(viewModel.SelectedNode, Is.SameAs(selectedNode));
            }
        }

        /// <summary>
        /// Verifies the assistant and complete drawer edit and immediately render the same ViewModel-owned Type set.
        /// </summary>
        [Test]
        public async Task VerifySearchAssistantAndDrawerShareElementTypeState()
        {
            var viewModel = CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>());
            var selectedTypes = ConfigureSelectedElementTypes(viewModel);
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            await this.OpenSearchAssistantAsync(component, "definition");
            await this.portalHost.FindAll("[role='option']")
                .Single(option => option.TextContent.Contains("«partdefinition»", StringComparison.Ordinal))
                .ClickAsync();

            Assert.That(component.Find(".mb-project-browser-search-assistant__criterion--type").TextContent,
                Does.Contain("Type «partdefinition»"));

            await this.OpenSearchAssistantAsync(component, "definition");
            var selectedDefinitionSuggestion = this.portalHost.FindAll("[role='option']")
                .Single(option => option.TextContent.Contains("«partdefinition»", StringComparison.Ordinal));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectedDefinitionSuggestion.ClassList,
                    Does.Contain("mb-project-browser-search-suggestion-list__item--selected"));
                Assert.That(selectedDefinitionSuggestion.TextContent, Does.Contain("Selected type filter"));
            }

            await this.OpenFilterDrawerAsync(component);
            var definitionChip = this.portalHost.FindAll("button[aria-pressed]")
                .Single(button => button.TextContent.Contains("«partdefinition»", StringComparison.Ordinal));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(definitionChip.GetAttribute("aria-pressed"), Is.EqualTo("true"));
                Assert.That(this.portalHost.FindAll(".mb-project-browser-search-assistant__surface"), Is.Empty);
            }

            await definitionChip.ClickAsync();
            await this.portalHost.Find("button[aria-label='Close project browser filters']").ClickAsync();

            await component.WaitForAssertionAsync(() =>
                Assert.That(component.FindAll(".mb-project-browser-search-assistant__criterion--type"), Is.Empty));

            await this.OpenSearchAssistantAsync(component, "definition");
            var definitionSuggestion = this.portalHost.FindAll("[role='option']")
                .Single(option => option.TextContent.Contains("«partdefinition»", StringComparison.Ordinal));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectedTypes, Is.Empty);
                Assert.That(definitionSuggestion.ClassList,
                    Does.Not.Contain("mb-project-browser-search-suggestion-list__item--selected"));
                Assert.That(definitionSuggestion.TextContent, Does.Not.Contain("Selected type filter"));
                viewModel.Verify(
                    owner => owner.ToggleElementTypeFilter(typeof(PartDefinition)),
                    Times.Exactly(2));
            }
        }

        /// <summary>
        /// Verifies accepting an already-active Type suggestion predictably toggles that ViewModel criterion off.
        /// </summary>
        [Test]
        public async Task VerifySearchAssistantActiveTypeAcceptanceTogglesCriterionOff()
        {
            var viewModel = CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>());
            var selectedTypes = ConfigureSelectedElementTypes(viewModel, typeof(PartDefinition));
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            await this.OpenSearchAssistantAsync(component, "definition");
            await this.portalHost.FindAll("[role='option']")
                .Single(option => option.TextContent.Contains("«partdefinition»", StringComparison.Ordinal))
                .ClickAsync();

            await this.portalHost.WaitForAssertionAsync(() =>
                Assert.That(this.portalHost.FindAll(".mb-project-browser-search-assistant__surface"), Is.Empty));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectedTypes, Is.Empty);
                Assert.That(viewModel.Object.FilterText, Is.Empty);
                viewModel.Verify(
                    owner => owner.ToggleElementTypeFilter(typeof(PartDefinition)),
                    Times.Once);
            }
        }

        /// <summary>
        /// Verifies transient drafts and committed text criteria remain isolated per browser component.
        /// </summary>
        [Test]
        public async Task VerifyMultipleProjectBrowserSearchAssistantsRemainIndependent()
        {
            var firstViewModel = CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>());
            var secondViewModel = CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>());
            this.RegisterViewModel(firstViewModel.Object);

            using var firstComponent = this.RenderProjectBrowser();
            this.RegisterViewModel(secondViewModel.Object);
            using var secondComponent = this.RenderProjectBrowser();
            var firstSuggestionListId = firstComponent.Find("input[role='combobox']").GetAttribute("aria-controls");
            var secondSuggestionListId = secondComponent.Find("input[role='combobox']").GetAttribute("aria-controls");

            await firstComponent.Find("input[role='combobox']")
                .KeyDownAsync(new KeyboardEventArgs { Key = " " });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(GetSearchAssistantPopover(firstComponent).Instance.Open, Is.True);
                Assert.That(GetSearchAssistantPopover(secondComponent).Instance.Open, Is.False);
                Assert.That(firstComponent.Find("input[role='combobox']").GetAttribute("value"), Is.Empty);
                Assert.That(secondComponent.Find("input[role='combobox']").GetAttribute("value"), Is.Empty);
            }

            await firstComponent.Find("input[role='combobox']")
                .KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

            await this.OpenSearchAssistantAsync(firstComponent, "first");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(GetSearchAssistantPopover(firstComponent).Instance.Open, Is.True);
                Assert.That(GetSearchAssistantPopover(secondComponent).Instance.Open, Is.False);
                Assert.That(firstViewModel.Object.FilterText, Is.Empty);
                Assert.That(secondViewModel.Object.FilterText, Is.Empty);
                Assert.That(firstComponent.Find("input[role='combobox']").GetAttribute("value"), Is.EqualTo("first"));
                Assert.That(secondComponent.Find("input[role='combobox']").GetAttribute("value"), Is.Empty);
                Assert.That(firstSuggestionListId, Is.Not.EqualTo(secondSuggestionListId));
                Assert.That(this.portalHost.FindAll(".mb-project-browser-search-assistant__surface"),
                    Has.Count.EqualTo(1));
            }

            await firstComponent.Find("input[role='combobox']")
                .KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstViewModel.Object.FilterText, Is.EqualTo("first"));
                Assert.That(secondViewModel.Object.FilterText, Is.Empty);
                Assert.That(firstComponent.FindAll(".mb-project-browser-search-assistant__criterion"), Has.Count.EqualTo(1));
                Assert.That(secondComponent.FindAll(".mb-project-browser-search-assistant__criterion"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies durable criteria are retrieved and transient assistant state is reset across repeated remounts.
        /// </summary>
        [Test]
        public async Task VerifySearchCriteriaSurviveRepeatedComponentRemountsWithoutTransientState()
        {
            const int remountCount = 25;
            var viewModel = CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>());
            viewModel.Object.FilterText = "thruster";
            var selectedTypes = ConfigureSelectedElementTypes(viewModel, typeof(PartDefinition));
            this.RegisterViewModel(viewModel.Object);

            for (var iteration = 0; iteration < remountCount; iteration++)
            {
                using var component = this.RenderProjectBrowser();
                var input = component.Find("input[role='combobox']");

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.FindAll(".mb-project-browser-search-assistant__criterion"), Has.Count.EqualTo(2));
                    Assert.That(input.GetAttribute("value"), Is.Empty);
                    Assert.That(GetSearchAssistantPopover(component).Instance.Open, Is.False);
                }

                await this.OpenSearchAssistantAsync(component, $"draft-{iteration}");
                await input.KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
                component.Instance.Dispose();

                await this.portalHost.WaitForAssertionAsync(() =>
                    Assert.That(this.portalHost.FindAll(".mb-project-browser-search-assistant__surface"), Is.Empty));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.Object.FilterText, Is.EqualTo("thruster"));
                Assert.That(selectedTypes, Is.EquivalentTo(new[] { typeof(PartDefinition) }));
            }
        }

        /// <summary>
        /// Verifies the clear action delegates the coherent filter reset to the ViewModel owner.
        /// </summary>
        [Test]
        public async Task VerifyClearFilterInvokesViewModelOwnerOperation()
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
            await this.OpenSearchAssistantAsync(component, "transient draft");
            await this.OpenFilterDrawerAsync(component);
            var clearButton = this.portalHost.Find("button.mb-project-browser__clear-all");

            await clearButton.ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                Assert.That(component.Find("input[role='combobox']").GetAttribute("value"), Is.Empty);
                viewModel.Verify(x => x.ClearFilter(), Times.Once);
            });

            Assert.That(clearButton.HasAttribute("disabled"), Is.False);
        }

        /// <summary>
        /// Verifies the Figma filter drawer opens as a labelled overlay and supports both close affordances.
        /// </summary>
        [Test]
        public async Task VerifyFilterDrawerOpensAndClosesAccessibly()
        {
            var roots = new ObservableCollection<ProjectBrowserNodeViewModel>();
            var viewModel = CreateLoadedViewModel(roots);
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            await this.OpenFilterDrawerAsync(component);
            var drawer = this.portalHost.Find(".mb-project-browser__filter-drawer");
            var popover = GetFilterDrawerPopover(component);
            var popoverContent = component.FindComponent<BbPopoverContent>();
            var heading = this.portalHost.Find(".mb-project-browser__filter-drawer-title");
            var dialog = this.portalHost.Find("[role='dialog']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(drawer.TextContent, Does.Contain("FILTERS"));
                Assert.That(drawer.TextContent, Does.Contain("BROWSER VIEW"));
                Assert.That(dialog.GetAttribute("aria-labelledby"), Is.EqualTo(heading.Id));
                Assert.That(drawer.GetAttribute("tabindex"), Is.EqualTo("-1"));
                Assert.That(drawer.GetAttribute("aria-labelledby"), Is.EqualTo(heading.Id));
                Assert.That(popover.Instance.Open, Is.True);
                Assert.That(popover.Instance.RestoreFocusOnClose, Is.True);
                Assert.That(popoverContent.Instance.CloseOnEscape, Is.True);
                Assert.That(popoverContent.Instance.CloseOnClickOutside, Is.True);
            }

            await this.portalHost.Find("button[aria-label='Close project browser filters']").ClickAsync();
            await this.portalHost.WaitForAssertionAsync(() =>
                Assert.That(this.portalHost.FindAll(".mb-project-browser__filter-drawer"), Is.Empty));

            await this.OpenFilterDrawerAsync(component);
            await this.portalHost.Find(".mb-project-browser__filter-popover")
                .KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
            await this.portalHost.WaitForAssertionAsync(() =>
                Assert.That(this.portalHost.FindAll(".mb-project-browser__filter-drawer"), Is.Empty));
        }

        /// <summary>
        /// Verifies drawer state remains transient and independent across Project Browser components.
        /// </summary>
        [Test]
        public async Task VerifyMultipleProjectBrowserDrawersRemainIndependent()
        {
            var firstViewModel = CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>());
            var secondViewModel = CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>());
            ConfigureSelectedElementTypes(secondViewModel, typeof(Namespace));
            this.RegisterViewModel(firstViewModel.Object);

            using var firstComponent = this.RenderProjectBrowser();
            this.RegisterViewModel(secondViewModel.Object);
            using var secondComponent = this.RenderProjectBrowser();

            await this.OpenFilterDrawerAsync(firstComponent);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(GetFilterDrawerPopover(firstComponent).Instance.Open, Is.True);
                Assert.That(GetFilterDrawerPopover(secondComponent).Instance.Open, Is.False);
                Assert.That(firstComponent.Find("button[aria-label='Open project browser filters']")
                    .GetAttribute("data-filter-active"), Is.EqualTo("false"));
                Assert.That(secondComponent.Find("button[aria-label='Open project browser filters']")
                    .GetAttribute("data-filter-active"), Is.EqualTo("true"));
                Assert.That(this.portalHost.FindAll(".mb-project-browser__filter-drawer"), Has.Count.EqualTo(1));
            }
        }

        /// <summary>
        /// Verifies the active badge and selected chips derive from ViewModel-owned Type criteria only.
        /// </summary>
        [Test]
        public async Task VerifyFilterDrawerDerivesActiveCountAndSelectedTypeChips()
        {
            var roots = new ObservableCollection<ProjectBrowserNodeViewModel>();
            var viewModel = CreateLoadedViewModel(roots);
            viewModel.Object.FilterText = "search text is not counted";
            ConfigureSelectedElementTypes(
                viewModel,
                typeof(PartDefinition),
                typeof(PartUsage),
                typeof(Namespace));
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            await this.OpenFilterDrawerAsync(component);
            var selectedChips = this.portalHost.FindAll("button.mb-project-browser__type-chip[aria-pressed='true']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("button[aria-label='Open project browser filters']")
                    .GetAttribute("data-filter-active"), Is.EqualTo("true"));
                Assert.That(this.portalHost.Find(".mb-project-browser__filter-active-count").TextContent.Trim(),
                    Is.EqualTo("3 active"));
                Assert.That(this.portalHost.Find(".mb-project-browser__filter-section-count").TextContent.Trim(),
                    Is.EqualTo("3 selected"));
                Assert.That(selectedChips, Has.Count.EqualTo(3));
                Assert.That(selectedChips.Select(chip => chip.TextContent.Trim()),
                    Is.EquivalentTo(ExpectedSelectedTypeChipLabels));
            }
        }

        /// <summary>
        /// Verifies the real drawer supports Type multi-select and clears all ViewModel-owned criteria coherently.
        /// </summary>
        [Test]
        public async Task VerifyFilterDrawerMultiSelectAndClearAllUseRealViewModelState()
        {
            using var viewModel = await ProjectBrowserNodeTestFactory.CreateFilterTreeViewModelAsync();
            viewModel.FilterText = "needle";
            this.RegisterViewModel(viewModel);

            using var component = this.RenderProjectBrowser();
            await this.OpenFilterDrawerAsync(component);
            await this.portalHost.FindAll("button[aria-pressed]")
                .Single(button => button.TextContent.Contains("«namespace»", StringComparison.Ordinal))
                .ClickAsync();
            await this.portalHost.FindAll("button[aria-pressed]")
                .Single(button => button.TextContent.Contains("«partusage»", StringComparison.Ordinal))
                .ClickAsync();

            await this.portalHost.WaitForAssertionAsync(() =>
                Assert.That(
                    this.portalHost.Find(".mb-project-browser__filter-active-count").TextContent.Trim(),
                    Is.EqualTo("2 active")));

            Assert.That(component.FindAll(".mb-project-browser-search-assistant__criterion"), Has.Count.EqualTo(3));

            await this.portalHost.Find("button.mb-project-browser__clear-all").ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(viewModel.FilterText, Is.Empty);
                    Assert.That(viewModel.SelectedElementTypes, Is.Empty);
                    Assert.That(viewModel.FilterPresentation.IsActive, Is.False);
                    Assert.That(component.Find("input[role='combobox']").GetAttribute("value"), Is.Empty);
                    Assert.That(component.FindAll(".mb-project-browser-search-assistant__criterion"), Is.Empty);
                    Assert.That(this.portalHost.FindAll(".mb-project-browser__filter-active-count"), Is.Empty);
                    Assert.That(
                        this.portalHost.FindAll("button.mb-project-browser__type-chip[aria-pressed='true']"),
                        Is.Empty);
                    Assert.That(this.portalHost.Find("button.mb-project-browser__clear-all")
                        .HasAttribute("disabled"), Is.True);
                }
            });
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

            var emptyComponent = this.RenderProjectBrowser();
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
                Assert.That(titles, Is.EqualTo(ExpectedVisibleFilterPathTitles));
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

            await branchComponent.Find("button").ClickAsync();

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
            using var viewModel = await ProjectBrowserNodeTestFactory.CreateFilterTreeViewModelAsync();
            this.RegisterViewModel(viewModel);

            using var component = this.RenderProjectBrowser();
            var initialRenderCount = component.RenderCount;
            Assert.That(component.Markup, Does.Contain("Sibling"));

            viewModel.FilterText = "needle";

            await component.WaitForAssertionAsync(() =>
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
            using var firstViewModel = await ProjectBrowserNodeTestFactory.CreateFilterTreeViewModelAsync();
            using var secondViewModel = await ProjectBrowserNodeTestFactory.CreateFilterTreeViewModelAsync();
            this.RegisterViewModel(firstViewModel);

            using var component = this.RenderProjectBrowser();
            component.Render(parameters => parameters
                .Add(browser => browser.ViewModel, secondViewModel));
            var replacementRenderCount = component.RenderCount;

            firstViewModel.FilterText = "needle";

            Assert.That(component.RenderCount, Is.EqualTo(replacementRenderCount));

            secondViewModel.FilterText = "needle";

            await component.WaitForAssertionAsync(() =>
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
        public async Task VerifyMultipleInstancesGenerateUniqueFilterControlIds()
        {
            var roots = new ObservableCollection<ProjectBrowserNodeViewModel>();
            var viewModel = CreateLoadedViewModel(roots);
            this.RegisterViewModel(viewModel.Object);

            using var firstComponent = this.RenderProjectBrowser();
            using var secondComponent = this.RenderProjectBrowser();
            var firstSearch = firstComponent.Find("input[role='combobox']");
            var secondSearch = secondComponent.Find("input[role='combobox']");
            var firstSearchId = firstSearch.Id;
            var secondSearchId = secondSearch.Id;
            var firstSuggestionListId = firstSearch.GetAttribute("aria-controls");
            var secondSuggestionListId = secondSearch.GetAttribute("aria-controls");

            await this.OpenFilterDrawerAsync(firstComponent);
            var firstDrawerHeadingId = this.portalHost.Find(".mb-project-browser__filter-drawer-title").Id;
            await this.portalHost.Find("button[aria-label='Close project browser filters']").ClickAsync();
            await this.portalHost.WaitForAssertionAsync(() =>
                Assert.That(this.portalHost.FindAll(".mb-project-browser__filter-drawer"), Is.Empty));

            await this.OpenFilterDrawerAsync(secondComponent);
            var secondDrawerHeadingId = this.portalHost.Find(".mb-project-browser__filter-drawer-title").Id;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstSearchId, Is.Not.Empty);
                Assert.That(secondSearchId, Is.Not.Empty);
                Assert.That(firstDrawerHeadingId, Is.Not.Empty);
                Assert.That(secondDrawerHeadingId, Is.Not.Empty);
                Assert.That(firstSuggestionListId, Is.Not.Empty);
                Assert.That(secondSuggestionListId, Is.Not.Empty);
                Assert.That(secondSearchId, Is.Not.EqualTo(firstSearchId));
                Assert.That(secondSuggestionListId, Is.Not.EqualTo(firstSuggestionListId));
                Assert.That(secondDrawerHeadingId, Is.Not.EqualTo(firstDrawerHeadingId));
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
        /// Verifies observable root collection changes rerender without property notification choreography.
        /// </summary>
        [Test]
        public void VerifyRootNodesCollectionChangesRerenderComponent()
        {
            var firstNode = ProjectBrowserNodeTestFactory.CreateNamespaceNode("first", "First");
            var secondNode = ProjectBrowserNodeTestFactory.CreateNamespaceNode("second", "Second");
            var thirdNode = ProjectBrowserNodeTestFactory.CreateNamespaceNode("third", "Third");
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel> { firstNode };
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var viewModel = CreateProjectBrowserViewModelMock();
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

            component.WaitForAssertion(() =>
            {
                var titles = component.FindAll(".mb-project-browser-node__title");

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.RenderCount, Is.GreaterThan(initialRenderCount));
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

                await replacementComponent.WaitForAssertionAsync(() =>
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
        /// Verifies ViewModel replacement detaches the old observable root collection.
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

            Assert.That(component.RenderCount, Is.EqualTo(replacementRenderCount));

            secondRootsSource.Clear();

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
        /// Verifies root collection changes cannot rerender the component after reactive deactivation.
        /// </summary>
        [Test]
        public async Task VerifyDisposedComponentIgnoresRootNodesPublication()
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

            await this.DisposeComponentsAsync();
            var renderCountAfterDisposal = component.RenderCount;

            mutableRoots.Add(ProjectBrowserNodeTestFactory.CreateNamespaceNode("first", "First"));

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
            viewModel.SetupGet(x => x.AvailableElementTypes).Returns(DefaultAvailableElementTypes);
            viewModel.SetupGet(x => x.SelectedElementTypes)
                .Returns(EmptySelectedElementTypes);
            viewModel.SetupGet(x => x.SelectedNode).Returns((ProjectBrowserNodeViewModel)null);
            viewModel.SetupGet(x => x.FilterPresentation).Returns(InactiveFilterPresentation);
            viewModel.Setup(x => x.ClearFilter());
            viewModel.Setup(x => x.ToggleElementTypeFilter(It.IsAny<Type>()));

            return viewModel;
        }

        /// <summary>
        /// Configures observable selected-Type state on a Project Browser mock.
        /// </summary>
        /// <param name="viewModel">The Project Browser mock.</param>
        /// <param name="initialTypes">The initially selected element types.</param>
        /// <returns>The mutable source retained by the test.</returns>
        private static ObservableCollection<Type> ConfigureSelectedElementTypes(
            Mock<IProjectBrowserViewModel> viewModel,
            params Type[] initialTypes)
        {
            var selectedTypes = new ObservableCollection<Type>(initialTypes);
            var exposedTypes = new ReadOnlyObservableCollection<Type>(selectedTypes);
            viewModel.SetupGet(owner => owner.SelectedElementTypes).Returns(exposedTypes);
            viewModel.Setup(owner => owner.ToggleElementTypeFilter(It.IsAny<Type>()))
                .Callback<Type>(elementType =>
                {
                    if (!selectedTypes.Remove(elementType))
                    {
                        selectedTypes.Add(elementType);
                    }
                });

            return selectedTypes;
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
        /// Opens the Project Browser filter drawer through its accessible trigger.
        /// </summary>
        /// <param name="component">The rendered Project Browser instance.</param>
        /// <returns>A task representing the drawer interaction.</returns>
        private async Task OpenFilterDrawerAsync(IRenderedComponent<ProjectBrowserComponent> component)
        {
            await component.Find("button[aria-label='Open project browser filters']").ClickAsync();
            await this.portalHost.WaitForAssertionAsync(() =>
                Assert.That(this.portalHost.FindAll(".mb-project-browser__filter-drawer"), Has.Count.EqualTo(1)));
        }

        /// <summary>
        /// Writes draft text and waits for the anchored assistant surface.
        /// </summary>
        /// <param name="component">The rendered Project Browser instance.</param>
        /// <param name="query">The useful draft query.</param>
        /// <returns>A task representing the input interaction.</returns>
        private async Task OpenSearchAssistantAsync(
            IRenderedComponent<ProjectBrowserComponent> component,
            string query)
        {
            var searchInput = component.FindComponent<SearchInputComponent>();

            await component.InvokeAsync(() => searchInput.Instance.ValueChanged.InvokeAsync(query));

            await this.portalHost.WaitForAssertionAsync(() =>
                Assert.That(
                    this.portalHost.FindAll(".mb-project-browser-search-assistant__surface"),
                    Has.Count.EqualTo(1)));
        }

        /// <summary>
        /// Gets the fast search-assistant popover rather than the sibling complete filter drawer.
        /// </summary>
        /// <param name="component">The rendered Project Browser instance.</param>
        /// <returns>The search-assistant popover.</returns>
        private static IRenderedComponent<BbPopover> GetSearchAssistantPopover(
            IRenderedComponent<ProjectBrowserComponent> component)
        {
            return component.FindComponent<ProjectBrowserSearchAssistantComponent>()
                .FindComponent<BbPopover>();
        }

        /// <summary>
        /// Gets the complete filter drawer popover rather than the sibling search-assistant popover.
        /// </summary>
        /// <param name="component">The rendered Project Browser instance.</param>
        /// <returns>The filter drawer popover.</returns>
        private static IRenderedComponent<BbPopover> GetFilterDrawerPopover(
            IRenderedComponent<ProjectBrowserComponent> component)
        {
            return component.FindComponents<BbPopover>()
                .Single(candidate => candidate.Instance.RestoreFocusOnClose);
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
