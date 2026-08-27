// ------------------------------------------------------------------------------------------------
// <copyright file="HomeTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.Pages
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using AngleSharp.Dom;

    using BlazorBlueprint.Primitives.Services;

    using Bunit;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    using Moq;

    using Mycelium.Bloom.Components.Pages;
    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.NavigationRail;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;
    using Mycelium.Bloom.ViewModel.WorkspaceEditor;

    using AppHeaderComponent = Mycelium.Bloom.Components.UI.Organisms.AppHeader.AppHeader;
    using DetailsPanelComponent = Mycelium.Bloom.Components.UI.Organisms.DetailsPanel.DetailsPanel;
    using EditorWorkspaceComponent = Mycelium.Bloom.Components.UI.Organisms.EditorWorkspace.EditorWorkspace;
    using NavigationRailComponent = Mycelium.Bloom.Components.UI.Organisms.NavigationRail.NavigationRail;
    using ProjectBrowserComponent = Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser.ProjectBrowser;
    using StatusBarComponent = Mycelium.Bloom.Components.UI.Organisms.StatusBar.StatusBar;
    using WorkspaceShellComponent = Mycelium.Bloom.Components.UI.Organisms.WorkspaceShell.WorkspaceShell;
    using ActionMenuComponent = Mycelium.Bloom.Components.UI.Molecules.ActionMenu.ActionMenu;

    /// <summary>
    /// Tests the <see cref="Home" /> workspace composition.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class HomeTestFixture : BunitContext
    {
        /// <summary>
        /// The Figma-derived relative weights expected for the default three-group composition.
        /// </summary>
        private static readonly double[] ExpectedDefaultGroupWeights = [300d, 320d, 868d];

        /// <summary>
        /// The semantic element order expected inside the workspace body.
        /// </summary>
        private static readonly string[] ExpectedWorkspaceBodyElementNames = ["aside", "div", "aside"];

        /// <summary>
        /// The Blueprint portal host that owns portalled add-tab menu content.
        /// </summary>
        private IRenderedComponent<BbPortalHost> portalHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeTestFixture" /> class.
        /// </summary>
        public HomeTestFixture()
        {
            BlueprintTestSetup.Configure(this);
        }

        /// <summary>
        /// Disposes the bUnit context and async workspace resources after each test.
        /// </summary>
        [TearDown]
        public Task TearDown()
        {
            return this.DisposeAsync().AsTask();
        }

        /// <summary>
        /// Verifies Home composes the full application shell from the exact injected state instances.
        /// </summary>
        [Test]
        public void VerifyRenderComposesFullBleedWorkspaceFromInjectedState()
        {
            var composition = this.RegisterWorkspaceServices(3);
            using var navigationViewModel = composition.Navigation;
            using var component = this.Render<Home>();
            var shell = component.FindComponent<WorkspaceShellComponent>();
            var navigation = component.FindComponent<NavigationRailComponent>();
            var editorWorkspace = component.FindComponent<EditorWorkspaceComponent>();
            var projectBrowser = component.FindComponent<ProjectBrowserComponent>();
            var detailsPanel = component.FindComponent<DetailsPanelComponent>();
            var shellRoot = component.Find("section.mb-workspace-shell");
            var shellBody = component.Find(".mb-workspace-shell__body");
            var navigationRoot = component.Find("nav.mb-navigation-rail");
            var renderedWeights = composition.Editor.Groups
                .Select(group => editorWorkspace.Instance.InitialGroupWeights[group.Id])
                .ToArray();
            var addTabMenus = component.FindComponents<ActionMenuComponent>();
            var renderedTabIcons = component.FindAll("[data-testid='workspace-editor-tab-icon']")
                .ToDictionary(
                    icon => Guid.Parse(icon.GetAttribute("data-tab-id")),
                    icon => icon.GetAttribute("data-icon-name"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(shell.Instance.FullApplication, Is.True);
                Assert.That(shellRoot.GetAttribute("role"), Is.EqualTo("main"));
                Assert.That(shellRoot.GetAttribute("data-navigation-collapsed"), Is.EqualTo("true"));
                Assert.That(shellRoot.GetAttribute("style"),
                    Does.Contain("--mb-workspace-right-panel-width: 380px;"));
                Assert.That(navigation.Instance.ViewModel, Is.SameAs(composition.Navigation));
                Assert.That(navigationRoot.GetAttribute("style"), Is.Null);
                Assert.That(editorWorkspace.Instance.ViewModel, Is.SameAs(composition.Editor));
                Assert.That(editorWorkspace.Instance.AddTabControl, Is.Not.Null);
                Assert.That(editorWorkspace.Instance.AddTabRequested.HasDelegate, Is.False);
                Assert.That(editorWorkspace.Instance.TabLeadingContent, Is.Not.Null);
                Assert.That(projectBrowser.Instance.ViewModel, Is.SameAs(composition.ProjectBrowser));
                Assert.That(detailsPanel.Instance.ViewModel, Is.SameAs(composition.Context));
                Assert.That(component.FindComponents<AppHeaderComponent>(), Has.Count.EqualTo(1));
                Assert.That(component.FindComponents<NavigationRailComponent>(), Has.Count.EqualTo(1));
                Assert.That(component.FindComponents<EditorWorkspaceComponent>(), Has.Count.EqualTo(1));
                Assert.That(component.FindComponents<ProjectBrowserComponent>(), Has.Count.EqualTo(1));
                Assert.That(component.FindComponents<DetailsPanelComponent>(), Has.Count.EqualTo(1));
                Assert.That(component.FindComponents<StatusBarComponent>(), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("main"), Is.Empty);
                Assert.That(component.Find("h1").TextContent.Trim(), Is.EqualTo("Bloom workspace"));
                Assert.That(component.Find(".mb-main-workspace__brand"), Is.Not.Null);
                Assert.That(component.Find("header.mb-app-header").GetAttribute("style"),
                    Does.Contain("height: 48px"));
                Assert.That(shellBody.Children.Select(element => element.LocalName),
                    Is.EqualTo(ExpectedWorkspaceBodyElementNames));
                Assert.That(component.FindAll(".mb-editor-workspace [data-testid='workspace-project-browser']"),
                    Has.Count.EqualTo(1));
                Assert.That(component.FindAll(".mb-workspace-shell__right-panel [data-testid='workspace-details-panel']"),
                    Has.Count.EqualTo(1));
                Assert.That(composition.Editor.Groups, Has.Count.EqualTo(3));
                Assert.That(composition.Editor.Groups.All(group => group.Tabs.Count == 1), Is.True);
                Assert.That(composition.Editor.Groups[0].ActiveTab.Title, Is.EqualTo("Project Browser"));
                Assert.That(composition.Editor.Groups[0].ActiveTab.ViewTypeKey, Is.EqualTo("project-browser"));
                Assert.That(composition.Editor.Groups.Skip(1)
                    .All(group => group.ActiveTab.ViewTypeKey == "placeholder"), Is.True);
                Assert.That(renderedWeights, Is.EqualTo(ExpectedDefaultGroupWeights));
                Assert.That(component.FindAll("[data-testid='workspace-editor-placeholder']"),
                    Has.Count.EqualTo(2));
                Assert.That(addTabMenus, Has.Count.EqualTo(3));
                Assert.That(
                    addTabMenus.All(menu => menu.Instance.Items.Select(item => item.Symbol)
                        .SequenceEqual(new SymbolIconName?[] { SymbolIconName.Document, SymbolIconName.Tree })),
                    Is.True);
                Assert.That(renderedTabIcons[composition.Editor.Groups[0].ActiveTab.Id], Is.EqualTo("list-tree"));
                Assert.That(
                    composition.Editor.Groups.Skip(1)
                        .Select(group => renderedTabIcons[group.ActiveTab.Id]),
                    Is.EqualTo(new[] { "file-text", "file-text" }));
            }

            Assert.That(
                component.Find($"[data-testid='workspace-project-browser'][data-tab-id='{composition.Editor.Groups[0].ActiveTab.Id}']"),
                Is.Not.Null);

            foreach (var group in composition.Editor.Groups.Skip(1))
            {
                Assert.That(
                    component.Find($"[data-testid='workspace-editor-placeholder'][data-tab-id='{group.ActiveTab.Id}']"),
                    Is.Not.Null);
            }
        }

        /// <summary>
        /// Verifies placeholder initialization respects configured limits without assuming three groups.
        /// </summary>
        /// <param name="maximumGroupCount">The configured workspace limit.</param>
        /// <param name="expectedGroupCount">The expected structural group count.</param>
        /// <param name="expectsFigmaWeights">Whether the three-group Figma seed applies.</param>
        [TestCase(1, 1, false)]
        [TestCase(2, 2, false)]
        [TestCase(5, 3, true)]
        public void VerifyPlaceholderInitializationRespectsMaximumGroupCount(
            int maximumGroupCount,
            int expectedGroupCount,
            bool expectsFigmaWeights)
        {
            var composition = this.RegisterWorkspaceServices(maximumGroupCount);
            using var navigationViewModel = composition.Navigation;
            using var component = this.Render<Home>();
            var editorWorkspace = component.FindComponent<EditorWorkspaceComponent>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(composition.Editor.MaximumGroupCount, Is.EqualTo(maximumGroupCount));
                Assert.That(composition.Editor.Groups, Has.Count.EqualTo(expectedGroupCount));
                Assert.That(composition.Editor.Groups.All(group => group.Tabs.Count == 1), Is.True);
                Assert.That(component.FindAll("[data-testid='editor-workspace-group']"),
                    Has.Count.EqualTo(expectedGroupCount));
                Assert.That(component.FindAll("[data-testid='workspace-project-browser']"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("[data-testid='workspace-editor-placeholder']"),
                    Has.Count.EqualTo(expectedGroupCount - 1));
                Assert.That(composition.Editor.Groups[0].ActiveTab.ViewTypeKey, Is.EqualTo("project-browser"));
                Assert.That(composition.Editor.Groups.Skip(1)
                    .All(group => group.ActiveTab.ViewTypeKey == "placeholder"), Is.True);
                Assert.That(editorWorkspace.Instance.InitialGroupWeights,
                    Has.Count.EqualTo(expectsFigmaWeights ? 3 : 0));
            }
        }

        /// <summary>
        /// Verifies the composition preserves an existing durable workspace instead of reseeding it.
        /// </summary>
        [Test]
        public void VerifyExistingWorkspaceStateIsNotReplacedByPlaceholders()
        {
            var composition = this.RegisterWorkspaceServices(3);
            var initialGroup = composition.Editor.Groups.Single();
            Assert.That(
                composition.Editor.TryOpenTab(initialGroup.Id, "Existing editor", "placeholder", out var existingTab),
                Is.True);
            using var navigationViewModel = composition.Navigation;
            using var component = this.Render<Home>();
            var editorWorkspace = component.FindComponent<EditorWorkspaceComponent>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(composition.Editor.Groups, Has.Count.EqualTo(1));
                Assert.That(initialGroup.Tabs, Has.Count.EqualTo(1));
                Assert.That(initialGroup.ActiveTab, Is.SameAs(existingTab));
                Assert.That(editorWorkspace.Instance.InitialGroupWeights, Is.Empty);
                Assert.That(component.Find("[data-testid='workspace-editor-placeholder']")
                    .GetAttribute("data-tab-id"), Is.EqualTo(existingTab.Id.ToString()));
                Assert.That(component.Find("[data-testid='workspace-editor-placeholder'] strong")
                    .TextContent, Is.EqualTo("Existing editor"));
            }
        }

        /// <summary>
        /// Verifies the add-tab menu opens generic content only in the exact selected group.
        /// </summary>
        [Test]
        public async Task VerifyEmptyEditorActionTargetsExactOwningGroup()
        {
            var composition = this.RegisterWorkspaceServices(3);
            using var navigationViewModel = composition.Navigation;
            using var component = this.Render<Home>();
            var targetGroup = composition.Editor.Groups[1];
            var addMenu = FindAddTabMenu(component, targetGroup.Id);
            var addButton = addMenu.QuerySelector("button");

            await addButton.ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(addButton.GetAttribute("aria-label"), Is.EqualTo("Add tab to Editor group 2"));
                Assert.That(addButton.GetAttribute("title"), Is.EqualTo("Add tab"));
                Assert.That(composition.Editor.Groups[0].Tabs, Has.Count.EqualTo(1));
                Assert.That(targetGroup.Tabs, Has.Count.EqualTo(1));
                Assert.That(composition.Editor.Groups[2].Tabs, Has.Count.EqualTo(1));
            }

            var emptyEditorAction = FindPortalledMenuItem("Empty editor");

            Assert.That(emptyEditorAction.QuerySelector("svg"), Is.Not.Null);

            await emptyEditorAction.ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(composition.Editor.Groups[0].Tabs, Has.Count.EqualTo(1));
                    Assert.That(targetGroup.Tabs, Has.Count.EqualTo(2));
                    Assert.That(composition.Editor.Groups[2].Tabs, Has.Count.EqualTo(1));
                    Assert.That(composition.Editor.FocusedGroup, Is.SameAs(targetGroup));
                    Assert.That(targetGroup.ActiveTab.Title, Is.EqualTo("Editor 4"));
                    Assert.That(targetGroup.ActiveTab.ViewTypeKey, Is.EqualTo("placeholder"));
                    Assert.That(component.Find(
                            $"[data-testid='workspace-editor-tab-icon'][data-tab-id='{targetGroup.ActiveTab.Id}']")
                        .GetAttribute("data-icon-name"), Is.EqualTo("file-text"));
                }
            });
        }

        /// <summary>
        /// Verifies the retained Project Browser cannot be opened more than once in the workspace.
        /// </summary>
        [Test]
        public async Task VerifyProjectBrowserActionIsDisabledWhileBrowserExists()
        {
            var composition = this.RegisterWorkspaceServices(3);
            using var navigationViewModel = composition.Navigation;
            using var component = this.Render<Home>();
            var targetGroup = composition.Editor.Groups[1];

            await FindAddTabMenu(component, targetGroup.Id).QuerySelector("button").ClickAsync();
            var projectBrowserAction = FindPortalledMenuItem("Project Browser");

            Assert.That(projectBrowserAction.GetAttribute("aria-disabled"), Is.EqualTo("true"));

            await projectBrowserAction.ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(composition.Editor.Groups.SelectMany(group => group.Tabs)
                    .Count(tab => tab.ViewTypeKey == "project-browser"), Is.EqualTo(1));
                Assert.That(targetGroup.Tabs, Has.Count.EqualTo(1));
            }
        }

        /// <summary>
        /// Verifies Project Browser creation targets the requesting group and uses the retained ViewModel.
        /// </summary>
        [Test]
        public async Task VerifyProjectBrowserActionTargetsExactGroupAndRetainedViewModel()
        {
            var composition = this.RegisterWorkspaceServices(3);
            var initialGroup = composition.Editor.Groups.Single();
            Assert.That(
                composition.Editor.TryOpenTab(initialGroup.Id, "Existing editor", "placeholder", out _),
                Is.True);
            Assert.That(composition.Editor.TryAddGroup(out var targetGroup), Is.True);
            using var navigationViewModel = composition.Navigation;
            using var component = this.Render<Home>();

            await FindAddTabMenu(component, targetGroup.Id).QuerySelector("button").ClickAsync();
            var projectBrowserAction = FindPortalledMenuItem("Project Browser");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(projectBrowserAction.GetAttribute("aria-disabled"), Is.Not.EqualTo("true"));
                Assert.That(projectBrowserAction.QuerySelector("svg"), Is.Not.Null);
            }

            await projectBrowserAction.ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                var browserTab = targetGroup.Tabs.Single(tab => tab.ViewTypeKey == "project-browser");
                var renderedBrowser = component.FindComponent<ProjectBrowserComponent>();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(initialGroup.Tabs, Has.Count.EqualTo(1));
                    Assert.That(targetGroup.Tabs, Has.Count.EqualTo(1));
                    Assert.That(targetGroup.ActiveTab, Is.SameAs(browserTab));
                    Assert.That(composition.Editor.FocusedGroup, Is.SameAs(targetGroup));
                    Assert.That(renderedBrowser.Instance.ViewModel, Is.SameAs(composition.ProjectBrowser));
                    Assert.That(component.Find("[data-testid='workspace-project-browser']")
                        .GetAttribute("data-tab-id"), Is.EqualTo(browserTab.Id.ToString()));
                    Assert.That(component.Find(
                            $"[data-testid='workspace-editor-tab-icon'][data-tab-id='{browserTab.Id}']")
                        .GetAttribute("data-icon-name"), Is.EqualTo("list-tree"));
                }
            });
        }

        /// <summary>
        /// Verifies closing Project Browser re-enables its action without consuming placeholder numbering.
        /// </summary>
        [Test]
        public async Task VerifyClosingProjectBrowserReenablesCreationWithoutConsumingPlaceholderNumber()
        {
            var composition = this.RegisterWorkspaceServices(3);
            using var navigationViewModel = composition.Navigation;
            using var component = this.Render<Home>();
            var projectBrowserTab = composition.Editor.Groups[0].Tabs.Single();
            var closeButton = component.FindAll("[data-testid='editor-workspace-tab-close']")
                .Single(button => button.GetAttribute("data-tab-id") == projectBrowserTab.Id.ToString());

            await closeButton.ClickAsync();

            await component.WaitForAssertionAsync(() =>
                Assert.That(composition.Editor.Groups.SelectMany(group => group.Tabs)
                    .Any(tab => tab.ViewTypeKey == "project-browser"), Is.False));

            var targetGroup = composition.Editor.Groups[0];
            await FindAddTabMenu(component, targetGroup.Id).QuerySelector("button").ClickAsync();
            var projectBrowserAction = FindPortalledMenuItem("Project Browser");

            Assert.That(projectBrowserAction.GetAttribute("aria-disabled"), Is.Not.EqualTo("true"));

            await projectBrowserAction.ClickAsync();
            await component.WaitForAssertionAsync(() =>
                Assert.That(targetGroup.Tabs.Any(tab => tab.ViewTypeKey == "project-browser"), Is.True));

            await FindAddTabMenu(component, targetGroup.Id).QuerySelector("button").ClickAsync();
            await FindPortalledMenuItem("Empty editor").ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(targetGroup.ActiveTab.Title, Is.EqualTo("Editor 4"));
                    Assert.That(targetGroup.ActiveTab.ViewTypeKey, Is.EqualTo("placeholder"));
                    Assert.That(composition.Editor.Groups.SelectMany(group => group.Tabs)
                        .Count(tab => tab.ViewTypeKey == "project-browser"), Is.EqualTo(1));
                }
            });
        }

        /// <summary>
        /// Verifies moving Project Browser retains its exact tab and ViewModel without enabling duplication.
        /// </summary>
        [Test]
        public async Task VerifyMovingProjectBrowserRetainsCompositionAndUniqueness()
        {
            var composition = this.RegisterWorkspaceServices(3);
            using var navigationViewModel = composition.Navigation;
            using var component = this.Render<Home>();
            var sourceGroup = composition.Editor.Groups[0];
            var destinationGroup = composition.Editor.Groups[1];
            var projectBrowserTab = sourceGroup.Tabs.Single();

            Assert.That(
                composition.Editor.MoveTab(sourceGroup.Id, projectBrowserTab.Id, destinationGroup.Id),
                Is.True);

            await component.WaitForAssertionAsync(() =>
            {
                var renderedBrowser = component.FindComponent<ProjectBrowserComponent>();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(composition.Editor.Groups, Does.Not.Contain(sourceGroup));
                    Assert.That(destinationGroup.Tabs.Any(tab => ReferenceEquals(tab, projectBrowserTab)), Is.True);
                    Assert.That(destinationGroup.ActiveTab, Is.SameAs(projectBrowserTab));
                    Assert.That(renderedBrowser.Instance.ViewModel, Is.SameAs(composition.ProjectBrowser));
                    Assert.That(component.Find("[data-testid='workspace-project-browser']")
                        .GetAttribute("data-tab-id"), Is.EqualTo(projectBrowserTab.Id.ToString()));
                }
            });

            await FindAddTabMenu(component, destinationGroup.Id).QuerySelector("button").ClickAsync();

            Assert.That(FindPortalledMenuItem("Project Browser").GetAttribute("aria-disabled"),
                Is.EqualTo("true"));
        }

        /// <summary>
        /// Verifies Project Browser selection reaches Details Panel through the shared context instance.
        /// </summary>
        [Test]
        public async Task VerifyProjectBrowserSelectionFlowsToDetailsPanelThroughSharedContext()
        {
            var composition = this.RegisterWorkspaceServices(3);
            using var navigationViewModel = composition.Navigation;
            using var component = this.Render<Home>();

            Assert.That(component.FindAll(".mb-details-panel__empty"), Has.Count.EqualTo(1));

            await component.Find(".mb-project-browser-node__row").ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(composition.Context.SelectedElement,
                        Is.SameAs(composition.ProjectBrowserNode.SourceElement));
                    Assert.That(component.FindAll(".mb-details-panel__empty"), Is.Empty);
                    Assert.That(component.FindAll(".mb-details-panel__properties"), Has.Count.EqualTo(1));
                }
            });
        }

        /// <summary>
        /// Verifies the rail's effective presentation controls the shell-owned navigation width state.
        /// </summary>
        [Test]
        public async Task VerifyNavigationCollapsePresentationUpdatesWorkspaceShell()
        {
            var composition = this.RegisterWorkspaceServices(3);
            using var navigationViewModel = composition.Navigation;
            using var component = this.Render<Home>();

            Assert.That(component.Find("section.mb-workspace-shell")
                .GetAttribute("data-navigation-collapsed"), Is.EqualTo("true"));

            await component.Find(".mb-navigation-rail__collapse-toggle").ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(composition.Navigation.PresentationMode,
                        Is.EqualTo(NavigationRailPresentationMode.Expanded));
                    Assert.That(component.Find("section.mb-workspace-shell")
                        .GetAttribute("data-navigation-collapsed"), Is.EqualTo("false"));
                }
            });
        }

        /// <summary>
        /// Verifies component-scoped workspace styling owns viewport containment without literal theme colors.
        /// </summary>
        [Test]
        public void VerifyWorkspaceStyleUsesViewportContainmentAndSemanticTokens()
        {
            var style = File.ReadAllText(Path.Combine(
                TestRepository.GetRootPath(),
                "Mycelium.Bloom",
                "Components",
                "Pages",
                "Home.razor.css"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(style, Does.Contain("height: 100dvh;"));
                Assert.That(style, Does.Contain("min-width: 0;"));
                Assert.That(style, Does.Contain("min-height: 0;"));
                Assert.That(style, Does.Contain("overflow: hidden;"));
                Assert.That(style, Does.Contain(
                    ".mb-workspace-shell:not(.mb-workspace-shell--left-panel-collapsed) .mb-workspace-shell__body"));
                Assert.That(style, Does.Contain("--mb-workspace-left-panel-width: fit-content;"));
                Assert.That(style, Does.Contain(
                    "width: calc(var(--mb-workspace-left-panel-collapsed-width) - (2 * var(--mb-spacing-2)));"));
                Assert.That(style, Does.Contain("justify-content: center;"));
                Assert.That(style, Does.Contain("var(--mb-color-workspace-background)"));
                Assert.That(style, Does.Not.Match("#[0-9a-fA-F]{3,8}"));
                Assert.That(style, Does.Not.Contain("border-radius"));
            }
        }

        /// <summary>
        /// Finds the generic add-tab menu belonging to one exact editor group.
        /// </summary>
        /// <param name="component">The rendered Home composition.</param>
        /// <param name="groupId">The represented editor group.</param>
        /// <returns>The matching menu root.</returns>
        private static IElement FindAddTabMenu(IRenderedComponent<Home> component, Guid groupId)
        {
            return component.FindAll("[data-testid='editor-workspace-add-tab-menu']")
                .Single(menu => menu.GetAttribute("data-group-id") == groupId.ToString());
        }

        /// <summary>
        /// Finds one action in the currently open portalled add-tab menu.
        /// </summary>
        /// <param name="label">The exact visible action label.</param>
        /// <returns>The matching menu item.</returns>
        private IElement FindPortalledMenuItem(string label)
        {
            return this.portalHost.WaitForElements("[role='menuitem']", 2)
                .Single(item => string.Equals(item.TextContent.Trim(), label, StringComparison.Ordinal));
        }

        /// <summary>
        /// Registers one navigation and editor-state instance for the composition root to pass to its children.
        /// </summary>
        /// <param name="maximumGroupCount">The editor-group limit for the state instance.</param>
        /// <returns>The exact registered state instances.</returns>
        private (
            WorkspaceEditorViewModel Editor,
            NavigationRailViewModel Navigation,
            IProjectBrowserViewModel ProjectBrowser,
            ContextAwareService Context,
            ProjectBrowserNodeViewModel ProjectBrowserNode) RegisterWorkspaceServices(
            int maximumGroupCount)
        {
            var context = new ContextAwareService();
            var editorViewModel = new WorkspaceEditorViewModel(
                Options.Create(new WorkspaceEditorOptions { MaximumGroupCount = maximumGroupCount }));
            var navigationViewModel = new NavigationRailViewModel(
                context,
                new NavigationRailItemProvider());
            var projectBrowserNode = ProjectBrowserNodeTestFactory.CreateNamespaceNode(
                "project-root",
                "Project root");
            var mutableRootNodes = new ObservableCollection<ProjectBrowserNodeViewModel> { projectBrowserNode };
            var rootNodes = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRootNodes);
            var projectBrowserViewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            projectBrowserViewModel.SetupGet(viewModel => viewModel.RootNodes).Returns(rootNodes);
            projectBrowserViewModel.SetupGet(viewModel => viewModel.IsLoaded).Returns(true);
            projectBrowserViewModel.SetupGet(viewModel => viewModel.IsLoading).Returns(false);
            projectBrowserViewModel.SetupGet(viewModel => viewModel.ErrorMessage).Returns(string.Empty);
            projectBrowserViewModel
                .Setup(viewModel => viewModel.SelectNode(projectBrowserNode))
                .Callback(() => context.SelectedElement = projectBrowserNode.SourceElement);

            this.Services.AddSingleton<IWorkspaceEditorViewModel>(editorViewModel);
            this.Services.AddSingleton<INavigationRailViewModel>(navigationViewModel);
            this.Services.AddSingleton<IContextAwareService>(context);
            this.Services.AddSingleton<IElementSelectionService>(context);
            this.Services.AddSingleton(projectBrowserViewModel.Object);
            this.portalHost = this.Render<BbPortalHost>();

            return (
                editorViewModel,
                navigationViewModel,
                projectBrowserViewModel.Object,
                context,
                projectBrowserNode);
        }
    }
}
