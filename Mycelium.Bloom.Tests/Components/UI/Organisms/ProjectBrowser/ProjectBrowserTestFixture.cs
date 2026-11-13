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

    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.JSInterop;

    using Moq;

    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Parts;

    using ProjectBrowserComponent = Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser.ProjectBrowser;
    using ProjectBrowserNodeComponent = Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser.ProjectBrowserNode;

    using ProjectBrowserSearchAssistantComponent =
        Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser.ProjectBrowserSearchAssistant;
    using SearchInputComponent = Mycelium.Bloom.Components.UI.Atoms.SearchInput.SearchInput;

    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ProjectBrowserTestFixture : BunitContext
    {
        private static readonly string[] ExpectedParentNodeInteractions = ["select", "callback", "select", "callback"];

        private static readonly string[] ExpectedLeafNodeInteractions = ["select", "callback"];

        private static readonly string[] ExpectedDisclosureToggleInteractions = ["toggle"];

        private static readonly string[] ExpectedSelectedTypeChipLabels =
            ["«part def»", "«part»", "«namespace»"];

        private static readonly string[] ExpectedVisibleFilterPathTitles = ["Root", "Branch", "Needle"];

        private static readonly ProjectBrowserFilterPresentation InactiveFilterPresentation =
            CreateInactiveFilterPresentation();

        private static readonly ReadOnlyObservableCollection<Type> DefaultAvailableElementTypes =
            new(new ObservableCollection<Type>
            {
                typeof(PartDefinition),
                typeof(PartUsage),
                typeof(Namespace)
            });

        private static readonly ReadOnlyObservableCollection<Type> EmptySelectedElementTypes =
            new(new ObservableCollection<Type>());

        private readonly IRenderedComponent<BbPortalHost> portalHost;

        private IProjectBrowserViewModel registeredViewModel;

        public ProjectBrowserTestFixture()
        {
            this.portalHost = BlueprintTestSetup.ConfigureWithPortalHost(this);
        }

        [TearDown]
        public Task TearDown()
        {
            return this.DisposeAsync().AsTask();
        }

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
                viewModel.Verify(x => x.InitializeAsync(It.IsAny<CancellationToken>()), Times.Never);
            }
        }

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
                viewModel.Verify(x => x.InitializeAsync(It.IsAny<CancellationToken>()), Times.Never);
            }
        }

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
                viewModel.Verify(x => x.InitializeAsync(It.IsAny<CancellationToken>()), Times.Never);
            }
        }

        [Test]
        public void VerifyTreeViewportPreservesAccessibleTreeAndConstrainedWidth()
        {
            var node = ProjectBrowserNodeTestFactory.CreateNamespaceNode("quantities/long", "ExtremelyLongElementNameThatMustRemainAvailableToHorizontalScrolling");
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel> { node };
            var viewModel = CreateLoadedViewModel(mutableRoots);
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            var rootChildren = component.Find(".mb-project-browser").Children;
            var viewport = component.Find(".mb-project-browser__tree-viewport");
            var tree = component.Find(".mb-project-browser__tree");
            var repositoryRoot = TestRepository.GetRootPath();
            var componentDirectory = Path.Combine(repositoryRoot, "Mycelium.Bloom", "Components", "UI", "Organisms", "ProjectBrowser");
            var style = File.ReadAllText(Path.Combine(componentDirectory, "ProjectBrowser.razor.css"));
            var overlayStyle = File.ReadAllText(Path.Combine(repositoryRoot, "Mycelium.Bloom", "Styles", "blueprint-overlays.css"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rootChildren, Has.Count.EqualTo(3));
                Assert.That(rootChildren[0].ClassList, Does.Contain("mb-project-browser__toolbar"));
                Assert.That(rootChildren[1].ClassList, Does.Contain("mb-project-browser__column-header"));
                Assert.That(rootChildren[1].TextContent, Does.Contain("NAME").And.Not.Contain("OWN"));
                Assert.That(rootChildren[2].ClassList, Does.Contain("mb-project-browser__tree-viewport"));
                Assert.That(style, Does.Match(@"(?s)\.mb-project-browser__toolbar\s*\{[^}]*flex:\s*0\s+0\s+auto;"));
                Assert.That(viewport.Children, Has.Count.EqualTo(1));
                Assert.That(viewport.Children[0].ClassList, Does.Contain("mb-project-browser__tree"));
                Assert.That(tree.GetAttribute("role"), Is.EqualTo("tree"));
                Assert.That(tree.GetAttribute("aria-label"), Is.EqualTo("Project browser"));
                Assert.That(component.Markup, Does.Contain("ExtremelyLongElementNameThatMustRemainAvailableToHorizontalScrolling"));
                Assert.That(style, Does.Match(
                    @"(?s)\.mb-project-browser__tree-viewport\s*\{[^}]*overflow:\s*auto;[^}]*scrollbar-width:\s*thin;"
                    + @"[^}]*scrollbar-color:\s*var\(--project-browser-scrollbar-thumb\)\s+transparent;"));
                Assert.That(style, Does.Contain("background-attachment: local, local, local, local, scroll, scroll, scroll, scroll;"));
                Assert.That(style, Does.Contain("@supports (scrollbar-width: none)"));
                Assert.That(style, Does.Contain("scrollbar-width: none;"));
                Assert.That(style, Does.Contain("@media (forced-colors: active)"));
                Assert.That(style, Does.Contain("scrollbar-color: ButtonText Canvas;"));
                Assert.That(style, Does.Match(
                    @"(?s)\.mb-project-browser__tree\s*\{[^}]*flex:\s*0\s+0\s+auto;[^}]*width:\s*100%;[^}]*min-width:\s*0;[^}]*background:\s*transparent;"));
                Assert.That(style, Does.Contain(".mb-project-browser__tree-viewport::-webkit-scrollbar-button"));
                Assert.That(overlayStyle, Does.Match(
                    @"(?s)\.mb-project-browser__filter-popover\s*\{[^}]*width:\s*min\(296px,\s*calc\(100vw\s*-\s*\(2\s*\*\s*var\(--spacing-8\)\)\)\);"));
            }
        }

        [Test]
        public void VerifyRenderDisplaysAccessibleFilterControlsAboveTree()
        {
            var viewModel = CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>());
            this.RegisterViewModel(viewModel.Object);
            using var component = this.RenderProjectBrowser();
            var input = component.Find("input[role='combobox']");
            var searchInput = component.FindComponent<SearchInputComponent>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("[role='search']").Children, Has.Count.EqualTo(1));
                Assert.That(component.FindAll(".mb-project-browser__filter-trigger-host"), Is.Empty);
                Assert.That(component.FindComponents<BbPopover>(), Has.Count.EqualTo(1));
                Assert.That(searchInput.Instance.Placeholder, Is.EqualTo("Filter elements..."));
                Assert.That(searchInput.Instance.FullWidth, Is.True);
                Assert.That(searchInput.Instance.EnableShortcut, Is.False);
                Assert.That(input.GetAttribute("aria-label"), Is.EqualTo("Filter project browser by name or qualified name"));
                Assert.That(input.GetAttribute("aria-expanded"), Is.EqualTo("false"));
                Assert.That(input.GetAttribute("aria-controls"), Is.Not.Empty);
            }
        }

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
            await this.OpenUnifiedFiltersAsync(component);
            await this.portalHost.FindAll("[role='option']")
                .Single(button => button.TextContent.Contains("«part def»", StringComparison.Ordinal))
                .ClickAsync();

            Assert.That(viewModel.Object.FilterText, Is.EqualTo("needle"));
            await this.portalHost.WaitForAssertionAsync(() =>
                viewModel.Verify(owner => owner.ToggleElementTypeFilter(typeof(PartDefinition)), Times.Once));
        }

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
                Assert.That(options[1].TextContent, Does.Contain("«part def»"));
                Assert.That(controlledListbox.GetAttribute("role"), Is.EqualTo("listbox"));
                Assert.That(controlledListbox.GetAttribute("aria-label"), Is.EqualTo("Search suggestions"));
                Assert.That(popoverContent.Instance.CloseOnEscape, Is.True);
                Assert.That(popoverContent.Instance.CloseOnClickOutside, Is.True);
                Assert.That(this.portalHost.Find("[role='region']").GetAttribute("aria-label"), Is.EqualTo("Project browser search suggestions"));
                Assert.That(viewModel.Object.FilterText, Is.Empty);
            }
        }

        [Test]
        public async Task VerifySearchAssistantEmptySpaceDiscoversAvailableElementTypes()
        {
            var firstDefinition = ProjectBrowserNodeTestFactory.CreateElement<PartDefinition>("first-definition", "First definition");
            var secondDefinition = ProjectBrowserNodeTestFactory.CreateElement<PartDefinition>("second-definition", "Second definition");
            var relationship = ProjectBrowserNodeTestFactory.CreateElement<Membership>("membership", "Membership");
            var root = ProjectBrowserNodeTestFactory.CreateElement<Namespace>("root", "Root", firstDefinition, secondDefinition, relationship);
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
                Assert.That(this.portalHost.FindAll(".mb-project-browser-search-assistant__surface"), Has.Count.EqualTo(1)));

            var surface = this.portalHost.Find(".mb-project-browser-search-assistant__surface");
            var options = surface.QuerySelectorAll("[role='option']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindComponent<SearchInputComponent>().Instance, Is.SameAs(stableSearchInput));
                Assert.That(input.GetAttribute("value"), Is.Empty);
                Assert.That(viewModel.FilterText, Is.Empty);
                Assert.That(options, Has.Count.EqualTo(2));
                Assert.That(surface.TextContent, Does.Contain("«namespace»"));
                Assert.That(surface.TextContent, Does.Contain("«part def»"));
                Assert.That(surface.TextContent, Does.Not.Contain("Contains \"\""));
                Assert.That(surface.TextContent, Does.Not.Contain("«part»"));
                Assert.That(surface.TextContent, Does.Not.Contain("«membership»"));
                Assert.That(viewModel.AvailableElementTypes, Does.Not.Contain(typeof(Membership)));
            }
        }

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
                Assert.That(this.portalHost.Find("[role='option'][data-focused='true']").TextContent, Does.Contain("«part def»")));
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
                Assert.That(this.portalHost.Find(".mb-project-browser-search-assistant__surface").TextContent, Does.Contain("Contains \"multi word\""));
                Assert.That(viewModel.Object.FilterText, Is.Empty);
            }
        }

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
                Assert.That(this.portalHost.Find("[role='option'][data-focused='true']").TextContent, Does.Contain("Contains")));

            await input.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
            await this.portalHost.WaitForAssertionAsync(() =>
                Assert.That(this.portalHost.Find("[role='option'][data-focused='true']").TextContent, Does.Contain("«part def»")));

            await input.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowUp" });
            await this.portalHost.WaitForAssertionAsync(() =>
                Assert.That(this.portalHost.Find("[role='option'][data-focused='true']").TextContent, Does.Contain("Contains")));

            await input.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
            await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

            await this.portalHost.WaitForAssertionAsync(() =>
                Assert.That(this.portalHost.FindAll(".mb-project-browser-search-assistant__surface"), Is.Empty));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.Object.FilterText, Is.Empty);
                Assert.That(selectedTypes, Is.EquivalentTo(new[] { typeof(PartDefinition) }));
                Assert.That(component.Find(".mb-project-browser-search-assistant__criterion--type").TextContent, Does.Contain("Type «part def»"));
                viewModel.Verify(owner => owner.ToggleElementTypeFilter(typeof(PartDefinition)), Times.Once);
            }
        }

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
                Assert.That(this.portalHost.FindAll(".mb-project-browser-search-assistant__surface"), Has.Count.EqualTo(1)));
        }

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
                Assert.That(component.Find(".mb-project-browser-search-assistant__criterion").TextContent, Does.Contain("Contains \"thruster\""));
                Assert.That(component.Find("button[aria-label='Remove Contains thruster search criterion']"), Is.Not.Null);
            }
        }

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
                Assert.That(component.Find(".mb-project-browser-search-assistant__criterion").TextContent, Does.Contain("Contains \"definition\""));
                viewModel.Verify(owner => owner.ToggleElementTypeFilter(It.IsAny<Type>()), Times.Never);
            }
        }

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
                Assert.That(component.Find(".mb-project-browser-search-assistant__criterion").TextContent, Does.Contain("Contains \"thruster\""));
            }
        }

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
                viewModel.Verify(owner => owner.ToggleElementTypeFilter(typeof(PartDefinition)), Times.Once);
            }
        }

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
                Assert.That(component.Find(".mb-project-browser-search-assistant__criterion").TextContent, Does.Contain("Contains \"thruster\""));
            }
        }

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
                .Single(option => option.TextContent.Contains("«part»", StringComparison.Ordinal))
                .ClickAsync();

            for (var iteration = 0; iteration < compositionCount; iteration++)
            {
                var committedDraft = $"mre-{iteration}";
                await this.OpenSearchAssistantAsync(component, committedDraft);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(GetSearchAssistantPopover(component).Instance.Open, Is.True);
                    Assert.That(this.portalHost.Find(".mb-project-browser-search-assistant__surface").TextContent, Does.Contain($"Contains \"{committedDraft}\""));
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
                    Assert.That(this.portalHost.Find(".mb-project-browser-search-assistant__surface").TextContent, Does.Contain($"Contains \"{nextDraft}\""));
                }

                await component.Find("input[role='combobox']")
                    .KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
            }
        }

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
                .Single(option => option.TextContent.Contains("«part»", StringComparison.Ordinal))
                .ClickAsync();
            await this.OpenSearchAssistantAsync(component, "next criterion");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.Object.FilterText, Is.EqualTo("thruster"));
                Assert.That(selectedTypes, Is.EquivalentTo(new[] { typeof(PartUsage) }));
                Assert.That(component.FindAll(".mb-project-browser-search-assistant__criterion"), Has.Count.EqualTo(2));
                Assert.That(component.FindComponent<SearchInputComponent>().Instance, Is.SameAs(stableSearchInput));
                Assert.That(GetSearchAssistantPopover(component).Instance.Open, Is.True);
                Assert.That(this.portalHost.Find(".mb-project-browser-search-assistant__surface").TextContent, Does.Contain("Contains \"next criterion\""));
            }
        }

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
                    .Single(option => option.TextContent.Contains("«part»", StringComparison.Ordinal))
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
                await this.OpenUnifiedFiltersAsync(component);
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

        [Test]
        public async Task VerifyUnifiedSuggestionsAndTokensShareElementTypeState()
        {
            var viewModel = CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>());
            var selectedTypes = ConfigureSelectedElementTypes(viewModel, typeof(PartDefinition));
            this.RegisterViewModel(viewModel.Object);
            using var component = this.RenderProjectBrowser();
            await this.OpenUnifiedFiltersAsync(component);
            var option = this.portalHost.FindAll("[role='option']")
                .Single(item => item.TextContent.Contains("«part def»", StringComparison.Ordinal));
            Assert.That(option.TextContent, Does.Contain("Selected type filter"));
            await option.ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectedTypes, Is.Empty);
                Assert.That(component.FindAll(".mb-project-browser-search-assistant__criterion--type"), Is.Empty);
            }

            await this.OpenUnifiedFiltersAsync(component);
            option = this.portalHost.FindAll("[role='option']")
                .Single(item => item.TextContent.Contains("«part def»", StringComparison.Ordinal));
            Assert.That(option.TextContent, Does.Not.Contain("Selected type filter"));
            await option.ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectedTypes, Is.EquivalentTo(new[] { typeof(PartDefinition) }));
                Assert.That(component.FindAll(".mb-project-browser-search-assistant__criterion--type"), Has.Count.EqualTo(1));
            }
        }

        [Test]
        public async Task VerifySearchAssistantActiveTypeAcceptanceTogglesCriterionOff()
        {
            var viewModel = CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>());
            var selectedTypes = ConfigureSelectedElementTypes(viewModel, typeof(PartDefinition));
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            await this.OpenSearchAssistantAsync(component, "definition");
            await this.portalHost.FindAll("[role='option']")
                .Single(option => option.TextContent.Contains("«part def»", StringComparison.Ordinal))
                .ClickAsync();

            await this.portalHost.WaitForAssertionAsync(() =>
                Assert.That(this.portalHost.FindAll(".mb-project-browser-search-assistant__surface"), Is.Empty));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectedTypes, Is.Empty);
                Assert.That(viewModel.Object.FilterText, Is.Empty);
                viewModel.Verify(owner => owner.ToggleElementTypeFilter(typeof(PartDefinition)), Times.Once);
            }
        }

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
                Assert.That(this.portalHost.FindAll(".mb-project-browser-search-assistant__surface"), Has.Count.EqualTo(1));
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

        [Test]
        public async Task VerifyClearFilterInvokesViewModelOwnerOperation()
        {
            using var presentationOwner = new ProjectBrowserViewModel(new Mock<IModelLoaderService>(MockBehavior.Strict).Object, new ContextAwareService());
            presentationOwner.FilterText = "missing";
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel>
            {
                ProjectBrowserNodeTestFactory.CreateNamespaceNode("root", "Root")
            };

            var viewModel = CreateLoadedViewModel(mutableRoots);
            viewModel.SetupGet(x => x.FilterPresentation).Returns(presentationOwner.FilterPresentation);
            viewModel.Object.FilterText = "missing";
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();
            await this.OpenSearchAssistantAsync(component, "transient draft");
            await this.OpenUnifiedFiltersAsync(component);
            var clearButton = this.portalHost.Find("button.mb-project-browser__clear-all");

            await clearButton.ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                Assert.That(component.Find("input[role='combobox']").GetAttribute("value"), Is.Empty);
                viewModel.Verify(x => x.ClearFilter(), Times.Once);
            });

            Assert.That(GetSearchAssistantPopover(component).Instance.Open, Is.False);
        }

        [Test]
        public async Task VerifyRapidReopenSurvivesPreviousPortalCleanup()
        {
            await using var context = new BunitContext();
            BlueprintTestSetup.Configure(context);
            var cleanup = new DeferredPositioningCleanup();
            var positioning = new Mock<IPositioningService>();
            positioning.Setup(service => service.ComputePositionAsync(It.IsAny<ElementReference>(), It.IsAny<ElementReference>(), It.IsAny<PositioningOptions>()))
                .ReturnsAsync(new PositionResult { Placement = "bottom-start" });
            positioning.Setup(service => service.ApplyPositionAsync(It.IsAny<ElementReference>(), It.IsAny<PositionResult>(), It.IsAny<bool>())).Returns(Task.CompletedTask);
            positioning.Setup(service => service.HidePositionAsync(It.IsAny<ElementReference>())).Returns(Task.CompletedTask);
            positioning.Setup(service => service.AutoUpdateAsync(It.IsAny<ElementReference>(), It.IsAny<ElementReference>(), It.IsAny<PositioningOptions>())).ReturnsAsync(cleanup);
            context.Services.AddSingleton(positioning.Object);
            using var portal = context.Render<BbPortalHost>();
            using var viewModel = await ProjectBrowserNodeTestFactory.CreateFilterTreeViewModelAsync();

            using var component = context.Render<ProjectBrowserComponent>(parameters => parameters.Add(browser => browser.ViewModel, viewModel));
            var persistentInput = component.FindComponent<SearchInputComponent>().Instance;
            await component.Find("input[role='combobox']").ClickAsync();
            await portal.WaitForElementAsync("[role='option']");
            var previousId = component.FindComponent<Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser.ProjectBrowserSearchAnchor>()
                .Instance.Popover.ContentId;
            var previousCallback = GetSearchAssistantPopover(component).Instance.OpenChanged;
            await portal.FindAll("[role='option']")
                .Single(option => option.TextContent.Contains("«part»", StringComparison.Ordinal)).ClickAsync();

            try
            {
                await cleanup.Started.Task.WaitAsync(TimeSpan.FromSeconds(3));
                await component.Find("input[role='combobox']").ClickAsync();
                var currentId = component.FindComponent<Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser.ProjectBrowserSearchAnchor>()
                    .Instance.Popover.ContentId;

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(currentId, Is.Not.EqualTo(previousId));
                    Assert.That(component.Find(".mb-project-browser-search-assistant").Id,
                        Is.EqualTo(component.FindComponent<Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser.ProjectBrowserSearchAnchor>()
                            .Instance.Popover.TriggerId));
                }

                await portal.WaitForAssertionAsync(() =>
                    Assert.That(portal.FindAll($"#{currentId}"), Has.Count.EqualTo(1)));
                cleanup.Released.TrySetResult();
                await portal.WaitForAssertionAsync(() =>
                {
                    using (Assert.EnterMultipleScope())
                    {
                        Assert.That(portal.FindAll($"#{previousId}"), Is.Empty);
                        Assert.That(portal.FindAll($"#{currentId}"), Has.Count.EqualTo(1));
                    }
                });

                await component.InvokeAsync(() => previousCallback.InvokeAsync(false));

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.Find("input[role='combobox']").GetAttribute("aria-expanded"), Is.EqualTo("true"));
                    Assert.That(component.FindComponent<SearchInputComponent>().Instance, Is.SameAs(persistentInput));
                }

                await portal.Find($"#{currentId} button.mb-project-browser__clear-all").ClickAsync();
                Assert.That(viewModel.SelectedElementTypes, Is.Empty);
            }
            finally
            {
                cleanup.Released.TrySetResult();
            }
        }

        private sealed class DeferredPositioningCleanup : IAsyncDisposable
        {
            public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource Released { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public ValueTask DisposeAsync()
            {
                this.Started.TrySetResult();

                return new ValueTask(this.Released.Task);
            }
        }

        [Test]
        public async Task VerifyUnifiedFieldAcceptsInputWhileCriterionOwnerCompletes()
        {
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var assistant = this.Render<ProjectBrowserSearchAssistantComponent>(parameters => parameters
                .Add(component => component.ElementTypeOptions, DefaultAvailableElementTypes)
                .Add(component => component.SelectedElementTypes, EmptySelectedElementTypes)
                .Add(component => component.ContainsCommitted, (string _) => completion.Task));
            var search = assistant.FindComponent<SearchInputComponent>();
            await assistant.InvokeAsync(() => search.Instance.ValueChanged.InvokeAsync("needle"));
            var acceptance = assistant.Find("input[role='combobox']")
                .KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

            try
            {
                Assert.That(assistant.Find("input[role='combobox']").HasAttribute("disabled"), Is.False);
                await assistant.InvokeAsync(() => search.Instance.ValueChanged.InvokeAsync("next criterion"));
            }
            finally
            {
                completion.SetResult();
            }

            await acceptance;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(assistant.Find("input[role='combobox']").HasAttribute("disabled"), Is.False);
                Assert.That(assistant.FindComponent<SearchInputComponent>().Instance, Is.SameAs(search.Instance));
                Assert.That(assistant.Find("input[role='combobox']").GetAttribute("value"), Is.EqualTo("next criterion"));
            }
        }

        [Test]
        public async Task VerifyUnifiedFiltersOpenAndCloseAccessibly()
        {
            var viewModel = CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>());
            this.RegisterViewModel(viewModel.Object);
            using var component = this.RenderProjectBrowser();
            await this.OpenUnifiedFiltersAsync(component);
            var content = component.FindComponent<BbPopoverContent>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("input[role='combobox']").GetAttribute("aria-expanded"), Is.EqualTo("true"));
                Assert.That(this.portalHost.Find("[role='region']").GetAttribute("aria-label"), Is.EqualTo("Project browser search suggestions"));
                Assert.That(this.portalHost.FindAll("[role='option']"), Has.Count.EqualTo(DefaultAvailableElementTypes.Count));
                Assert.That(content.Instance.CloseOnEscape, Is.True);
                Assert.That(content.Instance.CloseOnClickOutside, Is.True);
            }

            await this.portalHost.Find("button[aria-label='Close project browser filters']").ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(GetSearchAssistantPopover(component).Instance.Open, Is.False);
                Assert.That(this.portalHost.FindAll(".mb-project-browser-search-assistant__surface"), Is.Empty);
            }

            await component.Find("input[role='combobox']").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
            await this.portalHost.WaitForAssertionAsync(() =>
                Assert.That(this.portalHost.FindAll("[role='option'][data-focused='true']"), Has.Count.EqualTo(1)));
            await component.Find("input[role='combobox']").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
            Assert.That(GetSearchAssistantPopover(component).Instance.Open, Is.False);
        }

        [Test]
        public async Task VerifyMultipleUnifiedFiltersRemainIndependent()
        {
            var firstViewModel = CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>());
            var secondViewModel = CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>());
            ConfigureSelectedElementTypes(secondViewModel, typeof(Namespace));
            this.RegisterViewModel(firstViewModel.Object);
            using var firstComponent = this.RenderProjectBrowser();
            this.RegisterViewModel(secondViewModel.Object);
            using var secondComponent = this.RenderProjectBrowser();
            await this.OpenUnifiedFiltersAsync(firstComponent);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(GetSearchAssistantPopover(firstComponent).Instance.Open, Is.True);
                Assert.That(GetSearchAssistantPopover(secondComponent).Instance.Open, Is.False);
                Assert.That(firstComponent.FindAll(".mb-project-browser-search-assistant__criterion"), Is.Empty);
                Assert.That(secondComponent.FindAll(".mb-project-browser-search-assistant__criterion--type"), Has.Count.EqualTo(1));
                Assert.That(this.portalHost.FindAll(".mb-project-browser-search-assistant__surface"), Has.Count.EqualTo(1));
            }
        }

        [Test]
        public async Task VerifyUnifiedFiltersDeriveActiveCountAndSelectedTypes()
        {
            var viewModel = CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>());
            viewModel.Object.FilterText = "search text is not counted";
            ConfigureSelectedElementTypes(viewModel, typeof(PartDefinition), typeof(PartUsage), typeof(Namespace));
            this.RegisterViewModel(viewModel.Object);
            using var component = this.RenderProjectBrowser();
            await this.OpenUnifiedFiltersAsync(component);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.portalHost.Find(".mb-project-browser-search-assistant__footer").TextContent, Does.Contain("3 type filters"));
                Assert.That(this.portalHost.FindAll(".mb-project-browser-search-suggestion-list__item--selected"), Has.Count.EqualTo(3));
                Assert.That(component.FindAll(".mb-project-browser-search-assistant__criterion--type").Select(chip => chip.TextContent.Trim()),
                    Is.EquivalentTo(ExpectedSelectedTypeChipLabels.Select(label => $"Type {label}")));
            }
        }

        [Test]
        public async Task VerifyUnifiedFiltersMultiSelectAndClearAllUseRealViewModelState()
        {
            using var viewModel = await ProjectBrowserNodeTestFactory.CreateFilterTreeViewModelAsync();
            viewModel.FilterText = "needle";
            this.RegisterViewModel(viewModel);
            using var component = this.RenderProjectBrowser();

            foreach (var label in new[] { "«namespace»", "«part»" })
            {
                await this.OpenUnifiedFiltersAsync(component);
                await this.portalHost.FindAll("[role='option']")
                    .Single(option => option.TextContent.Contains(label, StringComparison.Ordinal)).ClickAsync();
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.SelectedElementTypes, Has.Count.EqualTo(2));
                Assert.That(component.FindAll(".mb-project-browser-search-assistant__criterion"), Has.Count.EqualTo(3));
            }

            await this.OpenUnifiedFiltersAsync(component);
            await this.portalHost.Find("button.mb-project-browser__clear-all").ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.FilterText, Is.Empty);
                Assert.That(viewModel.SelectedElementTypes, Is.Empty);
                Assert.That(viewModel.FilterPresentation.IsActive, Is.False);
                Assert.That(component.Find("input[role='combobox']").GetAttribute("value"), Is.Empty);
                Assert.That(component.FindAll(".mb-project-browser-search-assistant__criterion"), Is.Empty);
                Assert.That(GetSearchAssistantPopover(component).Instance.Open, Is.False);
            }

            await this.OpenUnifiedFiltersAsync(component);
            Assert.That(this.portalHost.Find("button.mb-project-browser__clear-all").HasAttribute("disabled"), Is.True);
        }

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

            using var presentationOwner = new ProjectBrowserViewModel(new Mock<IModelLoaderService>(MockBehavior.Strict).Object, new ContextAwareService());
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
                Assert.That(filteredEmptyState.TextContent, Does.Contain("No model elements match the current filters."));
                Assert.That(filteredEmptyState.GetAttribute("role"), Is.EqualTo("status"));
                Assert.That(filteredEmptyState.GetAttribute("aria-live"), Is.EqualTo("polite"));
            }
        }

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

        [Test]
        public void VerifyHandleNodeSelectedPreservesExpansionAndInvokesSelectionAndCallbackInOrder()
        {
            var urls = new Mock<IWorkspaceUrlContextService>();
            var child = ProjectBrowserNodeTestFactory.CreateNamespaceNode("quantities/length", "Length");
            var node = ProjectBrowserNodeTestFactory.CreateNamespaceNode("quantities", "Quantities", child);
            node.IsExpanded = true;
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
                .AddCascadingValue(urls.Object)
                .Add(browser => browser.SelectedNodeChanged, selectedNode =>
                {
                    Assert.That(selectedNode, Is.SameAs(node));
                    interactions.Add("callback");
                }));

            component.Find("[data-testid='project-browser-node-selection']").Click();
            component.Find("[data-testid='project-browser-node-selection']").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(interactions, Is.EqualTo(ExpectedParentNodeInteractions));
                Assert.That(node.IsExpanded, Is.True);
                viewModel.Verify(x => x.ToggleNode(It.IsAny<ProjectBrowserNodeViewModel>()), Times.Never);
                viewModel.Verify(x => x.SelectNode(node), Times.Exactly(2));
                urls.Verify(service => service.ClearInspection(), Times.Exactly(2));
            }
        }

        [Test]
        public void VerifyDisclosureTogglesWithoutSelectingOrInvokingSelectionCallback()
        {
            var child = ProjectBrowserNodeTestFactory.CreateNamespaceNode("quantities/length", "Length");
            var node = ProjectBrowserNodeTestFactory.CreateNamespaceNode("quantities", "Quantities", child);
            var interactions = new List<string>();
            var viewModel = CreateProjectBrowserViewModelMock();
            viewModel.SetupGet(x => x.RootNodes).Returns(new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(
                new ObservableCollection<ProjectBrowserNodeViewModel> { node }));
            viewModel.SetupGet(x => x.IsLoaded).Returns(true);
            viewModel.SetupGet(x => x.IsLoading).Returns(false);
            viewModel.SetupGet(x => x.ErrorMessage).Returns(string.Empty);
            viewModel.Setup(x => x.ToggleNode(node)).Callback(() =>
            {
                interactions.Add("toggle");
                node.IsExpanded = !node.IsExpanded;
            });

            viewModel.Setup(x => x.SelectNode(It.IsAny<ProjectBrowserNodeViewModel>()))
                .Callback(() => interactions.Add("select"));
            viewModel.Setup(x => x.Dispose());
            this.RegisterViewModel(viewModel.Object);

            using var component = this.RenderProjectBrowser();

            var disclosure = component.Find("[data-testid='project-browser-node-disclosure']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(disclosure.GetAttribute("aria-expanded"), Is.EqualTo("false"));
                Assert.That(disclosure.GetAttribute("aria-label"), Is.EqualTo("Expand Quantities"));
            }

            disclosure.Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(interactions, Is.EqualTo(ExpectedDisclosureToggleInteractions));
                Assert.That(node.IsExpanded, Is.True);
                Assert.That(component.Find("[role='treeitem']").GetAttribute("aria-selected"), Is.EqualTo("false"));
                Assert.That(component.Find("[data-testid='project-browser-node-disclosure']").GetAttribute("aria-label"), Is.EqualTo("Collapse Quantities"));
                viewModel.Verify(x => x.SelectNode(It.IsAny<ProjectBrowserNodeViewModel>()), Times.Never);
                viewModel.Verify(x => x.ToggleNode(node), Times.Once);
            }
        }

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

        [Test]
        public async Task VerifyMultipleInstancesGenerateUniqueFilterControlIds()
        {
            var firstViewModel = CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>());
            this.RegisterViewModel(firstViewModel.Object);
            using var firstComponent = this.RenderProjectBrowser();
            this.RegisterViewModel(CreateLoadedViewModel(new ObservableCollection<ProjectBrowserNodeViewModel>()).Object);
            using var secondComponent = this.RenderProjectBrowser();
            var firstInput = firstComponent.Find("input[role='combobox']");
            var secondInput = secondComponent.Find("input[role='combobox']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstInput.Id, Is.Not.Empty);
                Assert.That(secondInput.Id, Is.Not.EqualTo(firstInput.Id));
                Assert.That(firstInput.GetAttribute("aria-controls"), Is.Not.Empty);
                Assert.That(secondInput.GetAttribute("aria-controls"), Is.Not.EqualTo(firstInput.GetAttribute("aria-controls")));
            }
        }

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
            notifyingViewModel.Raise(x => x.PropertyChanged += null, new PropertyChangedEventArgs(nameof(IProjectBrowserViewModel.IsLoading)));

            component.WaitForAssertion(() =>
                Assert.That(component.Markup, Does.Contain("No model elements available.")));
        }

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
            notifyingViewModel.Raise(x => x.PropertyChanged += null, new PropertyChangedEventArgs(nameof(IProjectBrowserViewModel.IsLoaded)));

            component.WaitForAssertion(() =>
                Assert.That(component.Markup, Does.Contain("Loading Quantities model")));
        }

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
            notifyingViewModel.Raise(x => x.PropertyChanged += null, new PropertyChangedEventArgs(nameof(IProjectBrowserViewModel.ErrorMessage)));

            component.WaitForAssertion(() =>
                Assert.That(component.Find("[role='alert']").TextContent, Does.Contain("Reactive model failure")));
        }

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
            notifyingViewModel.Raise(x => x.PropertyChanged += null, new PropertyChangedEventArgs(nameof(IProjectBrowserViewModel.SelectedNode)));

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

            using var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object, new ContextAwareService());
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
                var initializationCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
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

                await replacementComponent.WaitForAssertionAsync(
                    () => Assert.That(replacementComponent.FindAll(".mb-project-browser-node__row"), Has.Count.EqualTo(1)), TimeSpan.FromSeconds(10));

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(viewModel.IsLoaded, Is.True);
                    Assert.That(viewModel.IsLoading, Is.False);
                    Assert.That(viewModel.ErrorMessage, Is.Empty);
                    modelLoaderService.Verify(x => x.LoadQuantitiesModel(), Times.Once);
                }
            }
        }

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

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        public async Task VerifyTreeDisposalIsIdempotent(bool disposeSynchronouslyFirst, bool disconnected)
        {
            var module = this.JSInterop.SetupModule("./Components/UI/Organisms/ProjectBrowser/ProjectBrowser.razor.js");
            var registration = module.SetupVoid("registerTree", _ => true);
            registration.SetVoidResult();
            var disposal = module.SetupVoid("disposeTree", _ => true);

            if (disconnected)
            {
                disposal.SetException(new JSDisconnectedException("Circuit disconnected"));
            }
            else
            {
                disposal.SetVoidResult();
            }

            var roots = new ObservableCollection<ProjectBrowserNodeViewModel> { ProjectBrowserNodeTestFactory.CreateNamespaceNode("root", "Root") };
            var viewModel = CreateLoadedViewModel(roots);
            this.RegisterViewModel(viewModel.Object);
            var component = this.RenderProjectBrowser();
            await component.WaitForAssertionAsync(() => Assert.That(registration.Invocations, Is.Not.Empty));
            var registrationCount = registration.Invocations.Count;

            if (disposeSynchronouslyFirst)
            {
                ((IDisposable)component.Instance).Dispose();
                ((IDisposable)component.Instance).Dispose();
                Assert.That(disposal.Invocations, Is.Empty, "Synchronous disposal must not block on JavaScript cleanup.");
            }

            await ((IAsyncDisposable)component.Instance).DisposeAsync();
            await ((IAsyncDisposable)component.Instance).DisposeAsync();
            var renderCount = component.RenderCount;
            roots.Clear();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(disposal.Invocations, Has.Count.EqualTo(1));
                Assert.That(registration.Invocations, Has.Count.EqualTo(registrationCount));
                Assert.That(component.RenderCount, Is.EqualTo(renderCount));
                viewModel.Verify(owner => owner.Dispose(), Times.Never);
            }
        }

        [Test]
        public async Task VerifyTreeDisposalAwaitsPendingImportWithoutRegistering()
        {
            await using var context = new BunitContext();
            BlueprintTestSetup.Configure(context);
            var import = new TaskCompletionSource<IJSObjectReference>(TaskCreationOptions.RunContinuationsAsynchronously);
            var runtime = new Mock<IJSRuntime>();
            runtime.Setup(js => js.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()))
                .Returns((string identifier, object[] arguments) => arguments[0] as string == "./Components/UI/Organisms/ProjectBrowser/ProjectBrowser.razor.js"
                    ? new ValueTask<IJSObjectReference>(import.Task)
                    : context.JSInterop.JSRuntime.InvokeAsync<IJSObjectReference>(identifier, arguments));
            context.Services.AddSingleton(runtime.Object);
            var module = new Mock<IJSObjectReference>(MockBehavior.Strict);
            module.Setup(reference => reference.DisposeAsync()).Returns(ValueTask.CompletedTask);
            var roots = new ObservableCollection<ProjectBrowserNodeViewModel> { ProjectBrowserNodeTestFactory.CreateNamespaceNode("root", "Root") };
            var viewModel = CreateLoadedViewModel(roots);
            var component = context.Render<ProjectBrowserComponent>(parameters => parameters.Add(browser => browser.ViewModel, viewModel.Object));
            await component.WaitForAssertionAsync(() => runtime.Verify(js => js.InvokeAsync<IJSObjectReference>("import",
                It.Is<object[]>(arguments => arguments[0] as string == "./Components/UI/Organisms/ProjectBrowser/ProjectBrowser.razor.js")), Times.Once));
            Task disposal = null;

            try
            {
                disposal = ((IAsyncDisposable)component.Instance).DisposeAsync().AsTask();
                Assert.That(disposal.IsCompleted, Is.False);
            }
            finally
            {
                import.TrySetResult(module.Object);

                if (disposal != null)
                {
                    await disposal;
                }
            }

            await ((IAsyncDisposable)component.Instance).DisposeAsync();

            using (Assert.EnterMultipleScope())
            {
                module.Verify(reference => reference.DisposeAsync(), Times.Once);
                module.VerifyNoOtherCalls();
                viewModel.Verify(owner => owner.Dispose(), Times.Never);
            }
        }

        private static Mock<IProjectBrowserViewModel> CreateLoadedViewModel(ObservableCollection<ProjectBrowserNodeViewModel> rootSource)
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

        private static Mock<IProjectBrowserViewModel> CreateProjectBrowserViewModelMock()
        {
            var viewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            viewModel.SetupProperty(browser => browser.IsInspectionActive);
            viewModel.SetupGet(browser => browser.InspectedNode).Returns((ProjectBrowserNodeViewModel)null);
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

        private static ObservableCollection<Type> ConfigureSelectedElementTypes(Mock<IProjectBrowserViewModel> viewModel, params Type[] initialTypes)
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

        private static ProjectBrowserFilterPresentation CreateInactiveFilterPresentation()
        {
            using var viewModel = new ProjectBrowserViewModel(new Mock<IModelLoaderService>(MockBehavior.Strict).Object, new ContextAwareService());

            return viewModel.FilterPresentation;
        }

        [Test]
        public async Task VerifyInspectionRowIsAccessibleAndScrollsWithoutChangingSelection()
        {
            var b = ProjectBrowserNodeTestFactory.CreateElement<PartUsage>("B", "Beta");
            var d = ProjectBrowserNodeTestFactory.CreateElement<PartUsage>("D", "Delta");
            var model = ProjectBrowserNodeTestFactory.CreateElement<Namespace>("A", "Alpha");
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(model, b);
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(model, d);
            var context = new ContextAwareService();
            var loader = new Mock<IModelLoaderService>();
            loader.Setup(service => service.LoadQuantitiesModel()).Returns(model);
            using var browser = new ProjectBrowserViewModel(loader.Object, context);
            await browser.InitializeAsync(CancellationToken.None);
            browser.SelectNode(browser.RootNodes[0]);
            var module = this.JSInterop.SetupModule("./Components/UI/Organisms/ProjectBrowser/ProjectBrowser.razor.js");
            module.Mode = JSRuntimeMode.Loose;
            this.RegisterViewModel(browser);
            var component = this.RenderProjectBrowser(parameters => parameters.Add(browser => browser.FollowInspection, true));
            await component.InvokeAsync(() => context.InspectedElement = b);
            await component.WaitForAssertionAsync(() =>
            {
                var row = component.Find("[data-inspected='true']");
                Assert.That(row.GetAttribute("data-element-id"), Is.EqualTo("B"));
                Assert.That(row.GetAttribute("aria-selected"), Is.EqualTo("false"));
                Assert.That(row.GetAttribute("aria-description"), Does.Contain("inspected in Properties"));
                Assert.That(row.QuerySelector(".mb-project-browser-node__row--inspected"), Is.Not.Null);
                Assert.That(component.Find("[aria-selected='true']").GetAttribute("data-element-id"), Is.EqualTo("A"));
                Assert.That(module.Invocations.Any(invocation => invocation.Identifier == "scrollInspectionIntoView"), Is.True);
                Assert.That(context.SelectedElement, Is.SameAs(model));
            });
            var scrolls = module.Invocations.Count(invocation => invocation.Identifier == "scrollInspectionIntoView");
            component.Render();
            Assert.That(module.Invocations.Count(invocation => invocation.Identifier == "scrollInspectionIntoView"), Is.EqualTo(scrolls));
            await component.Find("[data-element-id='D'] [data-testid='project-browser-node-selection']").ClickAsync();
            await component.WaitForAssertionAsync(() => Assert.That(component.FindAll("[data-inspected='true']"), Is.Empty));
            Assert.That(context.SelectedElement, Is.SameAs(d));
            await this.DisposeComponentsAsync();
            Assert.That(browser.IsInspectionActive, Is.False);
        }

        private void RegisterViewModel(IProjectBrowserViewModel viewModel)
        {
            this.registeredViewModel = viewModel;
        }

        private async Task OpenUnifiedFiltersAsync(IRenderedComponent<ProjectBrowserComponent> component)
        {
            await component.Find("input[role='combobox']").ClickAsync();
            await this.portalHost.WaitForAssertionAsync(() =>
                Assert.That(this.portalHost.FindAll(".mb-project-browser-search-assistant__surface"), Has.Count.EqualTo(1)));
        }

        private async Task OpenSearchAssistantAsync(IRenderedComponent<ProjectBrowserComponent> component, string query)
        {
            var searchInput = component.FindComponent<SearchInputComponent>();

            await component.InvokeAsync(() => searchInput.Instance.ValueChanged.InvokeAsync(query));

            await this.portalHost.WaitForAssertionAsync(() =>
                Assert.That(this.portalHost.FindAll(".mb-project-browser-search-assistant__surface"), Has.Count.EqualTo(1)));
        }

        private static IRenderedComponent<BbPopover> GetSearchAssistantPopover(IRenderedComponent<ProjectBrowserComponent> component)
        {
            return component.FindComponent<ProjectBrowserSearchAssistantComponent>()
                .FindComponent<BbPopover>();
        }

        private IRenderedComponent<ProjectBrowserComponent> RenderProjectBrowser(Action<ComponentParameterCollectionBuilder<ProjectBrowserComponent>> configure = null)
        {
            return this.Render<ProjectBrowserComponent>(parameters =>
            {
                parameters.Add(component => component.ViewModel, this.registeredViewModel);
                configure?.Invoke(parameters);
            });
        }
    }
}
