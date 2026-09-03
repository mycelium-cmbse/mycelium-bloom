// ------------------------------------------------------------------------------------------------
// <copyright file="ModellingTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.Pages.Workspace
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using AngleSharp.Dom;

    using BlazorBlueprint.Primitives.Services;

    using Bunit;

    using Microsoft.AspNetCore.Components;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Microsoft.AspNetCore.Components.Web;

    using Moq;

    using Mycelium.Bloom.Components.Pages.Workspace;
    using Mycelium.Bloom.Components.Layout;
    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;
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

    using SysML2.NET.Core.POCO.Root.Namespaces;

    /// <summary>
    /// Tests the <see cref="Modelling" /> workspace composition.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ModellingTestFixture : BunitContext
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
        /// The editor-type icon names expected for the default placeholder tabs.
        /// </summary>
        private static readonly string[] ExpectedPlaceholderTabIconNames = ["file-text", "file-text"];

        /// <summary>
        /// The empty observable Type state used by strict Project Browser test doubles.
        /// </summary>
        private static readonly ReadOnlyObservableCollection<Type> EmptyElementTypes =
            new(new ObservableCollection<Type>());

        /// <summary>
        /// The Blueprint portal host that owns portalled add-tab menu content.
        /// </summary>
        private IRenderedComponent<BbPortalHost> portalHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModellingTestFixture" /> class.
        /// </summary>
        public ModellingTestFixture()
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
        /// Verifies routed Modelling content composes from the exact injected state instances.
        /// </summary>
        [Test]
        public void VerifyRenderComposesEditorBodyFromInjectedState()
        {
            var composition = this.RegisterWorkspaceServices(3);
            using var navigationViewModel = composition.Navigation;
            using var component = this.RenderWorkspaceLayoutWithModelling();
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
                Assert.That(composition.Navigation.PresentationMode,
                    Is.EqualTo(NavigationRailPresentationMode.ExpandOnHover));
                Assert.That(shellRoot.GetAttribute("style"),
                    Does.Contain("--mb-workspace-right-panel-width: 380px;"));
                Assert.That(navigation.Instance.ViewModel, Is.SameAs(composition.Navigation));
                Assert.That(navigationRoot.GetAttribute("style"), Is.Null);
                Assert.That(editorWorkspace.Instance.ViewModel, Is.SameAs(composition.Editor));
                Assert.That(editorWorkspace.Instance.AddTabControl, Is.Not.Null);
                Assert.That(editorWorkspace.Instance.AddTabRequested.HasDelegate, Is.False);
                Assert.That(editorWorkspace.Instance.TabClosed.HasDelegate, Is.True);
                Assert.That(editorWorkspace.Instance.TabLeadingContent, Is.Not.Null);
                Assert.That(projectBrowser.Instance.ViewModel, Is.SameAs(composition.ProjectBrowsers[0].Object));
                Assert.That(detailsPanel.Instance.ViewModel, Is.SameAs(composition.Context));
                Assert.That(component.FindComponents<AppHeaderComponent>(), Has.Count.EqualTo(1));
                Assert.That(component.FindComponents<NavigationRailComponent>(), Has.Count.EqualTo(1));
                Assert.That(component.FindComponents<EditorWorkspaceComponent>(), Has.Count.EqualTo(1));
                Assert.That(component.FindComponents<ProjectBrowserComponent>(), Has.Count.EqualTo(1));
                Assert.That(component.FindComponents<DetailsPanelComponent>(), Has.Count.EqualTo(1));
                Assert.That(component.FindComponents<StatusBarComponent>(), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("main"), Is.Empty);
                Assert.That(component.Find(".mb-workspace-shell__main h1").TextContent.Trim(),
                    Is.EqualTo("Modelling"));
                Assert.That(component.Find(".mb-workspace-layout__brand"), Is.Not.Null);
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
                    Is.EqualTo(ExpectedPlaceholderTabIconNames));
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
            using var component = this.Render<Modelling>();
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
            using var component = this.Render<Modelling>();
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
            using var component = this.Render<Modelling>();
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
        /// Verifies the Project Browser action remains available while another browser exists.
        /// </summary>
        [Test]
        public async Task VerifyProjectBrowserActionRemainsEnabledWhileBrowserExists()
        {
            var composition = this.RegisterWorkspaceServices(3);
            using var navigationViewModel = composition.Navigation;
            using var component = this.Render<Modelling>();
            var targetGroup = composition.Editor.Groups[1];

            await FindAddTabMenu(component, targetGroup.Id).QuerySelector("button").ClickAsync();
            var projectBrowserAction = FindPortalledMenuItem("Project Browser");

            Assert.That(projectBrowserAction.GetAttribute("aria-disabled"), Is.Not.EqualTo("true"));
        }

        /// <summary>
        /// Verifies multiple Project Browser tabs in one group retain distinct identities and exact ViewModels.
        /// </summary>
        [Test]
        public async Task VerifyMultipleProjectBrowsersOpenIndependentlyInSameGroup()
        {
            var composition = this.RegisterWorkspaceServices(3);
            using var navigationViewModel = composition.Navigation;
            using var component = this.Render<Modelling>();
            var group = composition.Editor.Groups[0];
            var firstTab = group.Tabs.Single(tab => tab.ViewTypeKey == "project-browser");
            var firstRenderedBrowser = component.FindComponent<ProjectBrowserComponent>().Instance;
            composition.ProjectBrowsers[0].Object.FilterText = "first browser filter";

            await this.OpenProjectBrowserAsync(component, group.Id);
            composition.ProjectBrowsers[1].Object.FilterText = "second browser filter";

            await component.WaitForAssertionAsync(() =>
            {
                var projectBrowserTabs = group.Tabs
                    .Where(tab => tab.ViewTypeKey == "project-browser")
                    .ToArray();
                var secondTab = projectBrowserTabs[1];

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(projectBrowserTabs, Has.Length.EqualTo(2));
                    Assert.That(secondTab.Id, Is.Not.EqualTo(firstTab.Id));
                    Assert.That(projectBrowserTabs.All(tab => tab.ViewTypeKey == "project-browser"), Is.True);
                    Assert.That(composition.ProjectBrowsers, Has.Count.EqualTo(2));
                    Assert.That(composition.ProjectBrowsers[1].Object,
                        Is.Not.SameAs(composition.ProjectBrowsers[0].Object));
                    Assert.That(
                        GetRenderedProjectBrowserViewModel(component, secondTab.Id),
                        Is.SameAs(composition.ProjectBrowsers[1].Object));
                    Assert.That(component.FindComponent<ProjectBrowserComponent>().Instance,
                        Is.Not.SameAs(firstRenderedBrowser));
                }
            });

            var secondRenderedBrowser = component.FindComponent<ProjectBrowserComponent>().Instance;

            await component.Find(
                    $"[data-testid='editor-workspace-tab'][data-group-id='{group.Id}'][data-tab-id='{firstTab.Id}']")
                .ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(
                        GetRenderedProjectBrowserViewModel(component, firstTab.Id),
                        Is.SameAs(composition.ProjectBrowsers[0].Object));
                    Assert.That(composition.ProjectBrowsers[0].Object.FilterText, Is.EqualTo("first browser filter"));
                    Assert.That(composition.ProjectBrowsers[1].Object.FilterText, Is.EqualTo("second browser filter"));
                    Assert.That(component.FindComponent<ProjectBrowserComponent>().Instance,
                        Is.Not.SameAs(secondRenderedBrowser));
                }
            });
        }

        /// <summary>
        /// Verifies rapid tab replacement leaves every still-owned Project Browser initialization alive.
        /// </summary>
        [Test]
        public async Task VerifyRapidProjectBrowserTabSwitchingLoadsEveryStillOpenInstance()
        {
            const int projectBrowserCount = 5;
            using var releaseLoad = new ManualResetEventSlim();
            using var allLoadsStarted = new CountdownEvent(projectBrowserCount);
            var model = new Mock<INamespace>();
            model.SetupGet(x => x.ElementId).Returns("root");
            model.SetupGet(x => x.DeclaredName).Returns("Root");
            model.SetupGet(x => x.ownedElement).Returns([]);
            var modelLoaderService = new Mock<IModelLoaderService>();
            modelLoaderService
                .Setup(x => x.LoadQuantitiesModel())
                .Returns(() =>
                {
                    allLoadsStarted.Signal();

                    if (!releaseLoad.Wait(TimeSpan.FromSeconds(10)))
                    {
                        throw new TimeoutException("The test did not release model loading.");
                    }

                    return model.Object;
                });
            var projectBrowserViewModels = new List<ProjectBrowserViewModel>();
            var composition = this.RegisterWorkspaceServices(
                3,
                context =>
                {
                    var viewModel = new ProjectBrowserViewModel(
                        modelLoaderService.Object,
                        context);
                    projectBrowserViewModels.Add(viewModel);

                    return viewModel;
                });

            using var navigationViewModel = composition.Navigation;
            using var component = this.Render<Modelling>();
            var group = composition.Editor.Groups[0];

            for (var index = 1; index < projectBrowserCount; index++)
            {
                await this.OpenProjectBrowserAsync(component, group.Id);
            }

            Assert.That(allLoadsStarted.Wait(TimeSpan.FromSeconds(10)), Is.True);
            var projectBrowserTabs = group.Tabs
                .Where(tab => tab.ViewTypeKey == "project-browser")
                .ToArray();

            foreach (var tab in projectBrowserTabs.Concat(projectBrowserTabs.Reverse()))
            {
                await component.Find(
                        $"[data-testid='editor-workspace-tab'][data-group-id='{group.Id}'][data-tab-id='{tab.Id}']")
                    .ClickAsync();
            }

            var initializationCompletions = projectBrowserViewModels
                .Select(viewModel => new
                {
                    ViewModel = viewModel,
                    Completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously)
                })
                .ToArray();
            var loadedHandlers = initializationCompletions
                .Select(completion => new PropertyChangedEventHandler((_, args) =>
                {
                    if (args.PropertyName == nameof(ProjectBrowserViewModel.IsLoaded)
                        && completion.ViewModel.IsLoaded)
                    {
                        completion.Completion.TrySetResult(true);
                    }
                }))
                .ToArray();

            for (var index = 0; index < initializationCompletions.Length; index++)
            {
                initializationCompletions[index].ViewModel.PropertyChanged += loadedHandlers[index];
            }

            try
            {
                releaseLoad.Set();
                await Task.WhenAll(initializationCompletions
                    .Select(completion => completion.Completion.Task.WaitAsync(TimeSpan.FromSeconds(10))));
            }
            finally
            {
                for (var index = 0; index < initializationCompletions.Length; index++)
                {
                    initializationCompletions[index].ViewModel.PropertyChanged -= loadedHandlers[index];
                }
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(projectBrowserTabs, Has.Length.EqualTo(projectBrowserCount));
                Assert.That(projectBrowserViewModels, Has.Count.EqualTo(projectBrowserCount));
                Assert.That(projectBrowserViewModels.All(viewModel => viewModel.IsLoaded), Is.True);
                Assert.That(projectBrowserViewModels.All(viewModel => !viewModel.IsLoading), Is.True);
                Assert.That(projectBrowserViewModels.All(viewModel => viewModel.ErrorMessage.Length == 0), Is.True);
                Assert.That(projectBrowserViewModels.Select(viewModel => viewModel.RootNodes[0]).Distinct().ToArray(),
                    Has.Length.EqualTo(projectBrowserCount));
                modelLoaderService.Verify(
                    loader => loader.LoadQuantitiesModel(),
                    Times.Exactly(projectBrowserCount));
            }
        }

        /// <summary>
        /// Verifies Project Browser tabs in different groups render their own exact ViewModels concurrently.
        /// </summary>
        [Test]
        public async Task VerifyMultipleProjectBrowsersOpenIndependentlyInDifferentGroups()
        {
            var composition = this.RegisterWorkspaceServices(3);
            using var navigationViewModel = composition.Navigation;
            using var component = this.Render<Modelling>();
            var firstGroup = composition.Editor.Groups[0];
            var secondGroup = composition.Editor.Groups[1];
            var firstTab = firstGroup.Tabs.Single(tab => tab.ViewTypeKey == "project-browser");

            await this.OpenProjectBrowserAsync(component, secondGroup.Id);

            await component.WaitForAssertionAsync(() =>
            {
                var secondTab = secondGroup.Tabs.Single(tab => tab.ViewTypeKey == "project-browser");

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(firstTab.Id, Is.Not.EqualTo(secondTab.Id));
                    Assert.That(composition.ProjectBrowsers, Has.Count.EqualTo(2));
                    Assert.That(component.FindComponents<ProjectBrowserComponent>(), Has.Count.EqualTo(2));
                    Assert.That(
                        GetRenderedProjectBrowserViewModel(component, firstTab.Id),
                        Is.SameAs(composition.ProjectBrowsers[0].Object));
                    Assert.That(
                        GetRenderedProjectBrowserViewModel(component, secondTab.Id),
                        Is.SameAs(composition.ProjectBrowsers[1].Object));
                }
            });
        }

        /// <summary>
        /// Verifies durable Project Browser tabs present before Modelling initialization receive explicit ownership.
        /// </summary>
        [Test]
        public void VerifyExistingProjectBrowserTabIsComposedAtInitializationBoundary()
        {
            var composition = this.RegisterWorkspaceServices(3);
            var group = composition.Editor.Groups.Single();
            Assert.That(
                composition.Editor.TryOpenTab(group.Id, "Project Browser", "project-browser", out var tab),
                Is.True);
            using var navigationViewModel = composition.Navigation;
            using var component = this.Render<Modelling>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(composition.ProjectBrowsers, Has.Count.EqualTo(1));
                Assert.That(
                    GetRenderedProjectBrowserViewModel(component, tab.Id),
                    Is.SameAs(composition.ProjectBrowsers[0].Object));
                Assert.That(component.FindAll("[data-testid='workspace-project-browser-unavailable']"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies Project Browser creation targets the requesting group and uses its fresh ViewModel.
        /// </summary>
        [Test]
        public async Task VerifyProjectBrowserActionTargetsExactGroupAndFreshViewModel()
        {
            var composition = this.RegisterWorkspaceServices(3);
            var initialGroup = composition.Editor.Groups.Single();
            var existingEditorOpened = composition.Editor.TryOpenTab(
                initialGroup.Id,
                "Existing editor",
                "placeholder",
                out _);
            var targetGroupAdded = composition.Editor.TryAddGroup(out var targetGroup);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(existingEditorOpened, Is.True);
                Assert.That(targetGroupAdded, Is.True);
            }

            using var navigationViewModel = composition.Navigation;
            using var component = this.Render<Modelling>();

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
                    Assert.That(renderedBrowser.Instance.ViewModel, Is.SameAs(composition.ProjectBrowsers[0].Object));
                    Assert.That(component.Find("[data-testid='workspace-project-browser']")
                        .GetAttribute("data-tab-id"), Is.EqualTo(browserTab.Id.ToString()));
                    Assert.That(component.Find(
                            $"[data-testid='workspace-editor-tab-icon'][data-tab-id='{browserTab.Id}']")
                        .GetAttribute("data-icon-name"), Is.EqualTo("list-tree"));
                }
            });
        }

        /// <summary>
        /// Verifies closing Project Browser releases it and does not consume placeholder numbering.
        /// </summary>
        [Test]
        public async Task VerifyClosingProjectBrowserReleasesInstanceWithoutConsumingPlaceholderNumber()
        {
            var composition = this.RegisterWorkspaceServices(3);
            using var navigationViewModel = composition.Navigation;
            using var component = this.Render<Modelling>();
            var projectBrowserTab = composition.Editor.Groups[0].Tabs.Single();
            var closeButton = component.FindAll("[data-testid='editor-workspace-tab-close']")
                .Single(button => button.GetAttribute("data-tab-id") == projectBrowserTab.Id.ToString());

            await closeButton.ClickAsync();

            await component.WaitForAssertionAsync(() =>
                Assert.That(composition.Editor.Groups.SelectMany(group => group.Tabs)
                    .Any(tab => tab.ViewTypeKey == "project-browser"), Is.False));

            composition.ProjectBrowsers[0].Verify(viewModel => viewModel.Dispose(), Times.Once);

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
                    Assert.That(composition.ProjectBrowsers, Has.Count.EqualTo(2));
                    Assert.That(composition.ProjectBrowsers[1].Object,
                        Is.Not.SameAs(composition.ProjectBrowsers[0].Object));
                }
            });
        }

        [Test]
        public async Task VerifyClosingAllWorkspaceTabsResetsPlaceholderNumbering()
        {
            var composition = this.RegisterWorkspaceServices(3);
            using var navigationViewModel = composition.Navigation;
            using var component = this.Render<Modelling>();
            var tabs = composition.Editor.RenderState.Groups
                .SelectMany(group => group.Tabs.Select(tab => (GroupId: group.Id, TabId: tab.Id)))
                .ToArray();

            foreach (var tab in tabs)
            {
                await component.Find($"[data-testid='editor-workspace-tab-close'][data-group-id='{tab.GroupId}'][data-tab-id='{tab.TabId}']")
                    .ClickAsync();
            }

            await component.WaitForAssertionAsync(() =>
                Assert.That(composition.Editor.RenderState.Groups.SelectMany(group => group.Tabs), Is.Empty));

            var remainingGroup = composition.Editor.Groups.Single();
            await FindAddTabMenu(component, remainingGroup.Id).QuerySelector("button").ClickAsync();
            await FindPortalledMenuItem("Empty editor").ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(remainingGroup.ActiveTab.Title, Is.EqualTo("Editor 1"));
                    Assert.That(remainingGroup.ActiveTab.ViewTypeKey, Is.EqualTo("placeholder"));
                }
            });
        }

        [Test]
        public async Task VerifyClosingPlaceholdersDoesNotResetNumberingWhileProjectBrowserRemains()
        {
            var composition = this.RegisterWorkspaceServices(3);
            using var navigationViewModel = composition.Navigation;
            using var component = this.Render<Modelling>();
            var placeholderTabs = composition.Editor.RenderState.Groups
                .SelectMany(group => group.Tabs
                    .Where(tab => tab.Item.ViewTypeKey == "placeholder")
                    .Select(tab => (GroupId: group.Id, TabId: tab.Id)))
                .ToArray();

            foreach (var tab in placeholderTabs)
            {
                await component.Find($"[data-testid='editor-workspace-tab-close'][data-group-id='{tab.GroupId}'][data-tab-id='{tab.TabId}']")
                    .ClickAsync();
            }

            await component.WaitForAssertionAsync(() =>
            {
                Assert.That(composition.Editor.RenderState.Groups.SelectMany(group => group.Tabs)
                    .Single().Item.ViewTypeKey, Is.EqualTo("project-browser"));
            });

            var browserGroup = composition.Editor.Groups.Single();
            await FindAddTabMenu(component, browserGroup.Id).QuerySelector("button").ClickAsync();
            await FindPortalledMenuItem("Empty editor").ClickAsync();

            await component.WaitForAssertionAsync(() =>
                Assert.That(browserGroup.ActiveTab.Title, Is.EqualTo("Editor 4")));
        }

        /// <summary>
        /// Verifies the composition resets numbering only after the final Project Browser closes too.
        /// </summary>
        [Test]
        public async Task VerifyClosingFinalProjectBrowserResetsPlaceholderNumberingWhenWorkspaceBecomesEmpty()
        {
            var composition = this.RegisterWorkspaceServices(3);
            using var navigationViewModel = composition.Navigation;
            using var component = this.Render<Modelling>();
            var placeholderTabs = composition.Editor.RenderState.Groups
                .SelectMany(group => group.Tabs
                    .Where(tab => tab.Item.ViewTypeKey == "placeholder")
                    .Select(tab => (GroupId: group.Id, TabId: tab.Id)))
                .ToArray();

            foreach (var tab in placeholderTabs)
            {
                await component.Find($"[data-testid='editor-workspace-tab-close'][data-group-id='{tab.GroupId}'][data-tab-id='{tab.TabId}']")
                    .ClickAsync();
            }

            await component.WaitForAssertionAsync(() =>
                Assert.That(composition.Editor.RenderState.Groups.SelectMany(group => group.Tabs)
                    .Single().Item.ViewTypeKey, Is.EqualTo("project-browser")));

            var projectBrowserTab = composition.Editor.RenderState.Groups
                .SelectMany(group => group.Tabs.Select(tab => (GroupId: group.Id, TabId: tab.Id)))
                .Single();
            await component.Find($"[data-testid='editor-workspace-tab-close'][data-group-id='{projectBrowserTab.GroupId}'][data-tab-id='{projectBrowserTab.TabId}']")
                .ClickAsync();

            await component.WaitForAssertionAsync(() =>
                Assert.That(composition.Editor.RenderState.Groups.SelectMany(group => group.Tabs), Is.Empty));

            var remainingGroup = composition.Editor.Groups.Single();
            await FindAddTabMenu(component, remainingGroup.Id).QuerySelector("button").ClickAsync();
            await FindPortalledMenuItem("Empty editor").ClickAsync();

            await component.WaitForAssertionAsync(() =>
                Assert.That(remainingGroup.ActiveTab.Title, Is.EqualTo("Editor 1")));
        }

        /// <summary>
        /// Verifies moving Project Browser retains its exact tab and ViewModel without another DI resolution.
        /// </summary>
        [Test]
        public async Task VerifyMovingProjectBrowserRetainsTabAndViewModelIdentity()
        {
            var composition = this.RegisterWorkspaceServices(3);
            using var navigationViewModel = composition.Navigation;
            using var component = this.Render<Modelling>();
            var sourceGroup = composition.Editor.Groups[0];
            var destinationGroup = composition.Editor.Groups[1];
            var projectBrowserTab = sourceGroup.Tabs.Single();
            composition.ProjectBrowsers[0].Object.FilterText = "retained after move";

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
                    Assert.That(renderedBrowser.Instance.ViewModel, Is.SameAs(composition.ProjectBrowsers[0].Object));
                    Assert.That(renderedBrowser.Instance.ViewModel.FilterText, Is.EqualTo("retained after move"));
                    Assert.That(component.Find("[data-testid='workspace-project-browser']")
                        .GetAttribute("data-tab-id"), Is.EqualTo(projectBrowserTab.Id.ToString()));
                }
            });

            await FindAddTabMenu(component, destinationGroup.Id).QuerySelector("button").ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(FindPortalledMenuItem("Project Browser").GetAttribute("aria-disabled"),
                    Is.Not.EqualTo("true"));
                Assert.That(composition.ProjectBrowsers, Has.Count.EqualTo(1));
            }
        }

        /// <summary>
        /// Verifies splitting and moving a Project Browser preserves both tab and ViewModel identity.
        /// </summary>
        [Test]
        public async Task VerifyMovingProjectBrowserToNewSplitRetainsTabAndViewModelIdentity()
        {
            var composition = this.RegisterWorkspaceServices(4);
            using var navigationViewModel = composition.Navigation;
            using var component = this.Render<Modelling>();
            var sourceGroup = composition.Editor.Groups[0];
            var splitAfterGroup = composition.Editor.Groups[1];
            var projectBrowserTab = sourceGroup.Tabs.Single(tab => tab.ViewTypeKey == "project-browser");
            var projectBrowserViewModel = composition.ProjectBrowsers[0].Object;
            projectBrowserViewModel.FilterText = "retained after split";

            Assert.That(
                composition.Editor.TryMoveTabToNewGroup(
                    sourceGroup.Id,
                    projectBrowserTab.Id,
                    splitAfterGroup.Id,
                    out var newGroup),
                Is.True);

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(newGroup.Tabs.Single(), Is.SameAs(projectBrowserTab));
                    Assert.That(newGroup.Tabs.Single().Id, Is.EqualTo(projectBrowserTab.Id));
                    Assert.That(
                        GetRenderedProjectBrowserViewModel(component, projectBrowserTab.Id),
                        Is.SameAs(projectBrowserViewModel));
                    Assert.That(composition.ProjectBrowsers, Has.Count.EqualTo(1));
                    Assert.That(projectBrowserViewModel.FilterText, Is.EqualTo("retained after split"));
                }
            });
        }

        /// <summary>
        /// Verifies closing one of several Project Browsers disposes only its exact ViewModel.
        /// </summary>
        [Test]
        public async Task VerifyClosingOneOfManyProjectBrowsersDisposesOnlyOwnedInstance()
        {
            var composition = this.RegisterWorkspaceServices(3);
            using var navigationViewModel = composition.Navigation;
            using var component = this.Render<Modelling>();
            var group = composition.Editor.Groups[0];

            await this.OpenProjectBrowserAsync(component, group.Id);
            await this.OpenProjectBrowserAsync(component, group.Id);

            var projectBrowserTabs = group.Tabs
                .Where(tab => tab.ViewTypeKey == "project-browser")
                .ToArray();
            var middleTab = projectBrowserTabs[1];

            await component.Find(
                    $"[data-testid='editor-workspace-tab-close'][data-group-id='{group.Id}'][data-tab-id='{middleTab.Id}']")
                .ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(group.Tabs.Count(tab => tab.ViewTypeKey == "project-browser"), Is.EqualTo(2));
                    composition.ProjectBrowsers[0].Verify(viewModel => viewModel.Dispose(), Times.Never);
                    composition.ProjectBrowsers[1].Verify(viewModel => viewModel.Dispose(), Times.Once);
                    composition.ProjectBrowsers[2].Verify(viewModel => viewModel.Dispose(), Times.Never);
                    Assert.That(
                        GetRenderedProjectBrowserViewModel(component, projectBrowserTabs[2].Id),
                        Is.SameAs(composition.ProjectBrowsers[2].Object));
                }
            });

            await component.Find(
                    $"[data-testid='editor-workspace-tab'][data-group-id='{group.Id}'][data-tab-id='{projectBrowserTabs[0].Id}']")
                .ClickAsync();

            await component.WaitForAssertionAsync(() =>
                Assert.That(
                    GetRenderedProjectBrowserViewModel(component, projectBrowserTabs[0].Id),
                    Is.SameAs(composition.ProjectBrowsers[0].Object)));
        }

        /// <summary>
        /// Verifies closing an initializing Project Browser releases only that owner and remains circuit-safe.
        /// </summary>
        [Test]
        public async Task VerifyClosingInitializingProjectBrowserDoesNotAffectAnotherInstance()
        {
            using var filterPresentationOwner = new ProjectBrowserViewModel(
                new Mock<IModelLoaderService>(MockBehavior.Strict).Object,
                new ContextAwareService());
            var inactiveFilterPresentation = filterPresentationOwner.FilterPresentation;
            var initialization = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            ObservableCollection<ProjectBrowserNodeViewModel> initializingMutableRoots = [];
            var initializingRoots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(
                initializingMutableRoots);
            var initializingViewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            var survivingNode = ProjectBrowserNodeTestFactory.CreateNamespaceNode("surviving", "Surviving");
            ObservableCollection<ProjectBrowserNodeViewModel> survivingMutableRoots = [survivingNode];
            var survivingRoots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(
                survivingMutableRoots);
            var survivingViewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            var initializationToken = CancellationToken.None;

            initializingViewModel.SetupGet(viewModel => viewModel.RootNodes).Returns(initializingRoots);
            initializingViewModel.SetupGet(viewModel => viewModel.AvailableElementTypes).Returns(EmptyElementTypes);
            initializingViewModel.SetupProperty(viewModel => viewModel.FilterText, string.Empty);
            initializingViewModel.SetupGet(viewModel => viewModel.SelectedElementTypes)
                .Returns(EmptyElementTypes);
            initializingViewModel.SetupGet(viewModel => viewModel.SelectedNode)
                .Returns((ProjectBrowserNodeViewModel)null);
            initializingViewModel.SetupGet(viewModel => viewModel.FilterPresentation)
                .Returns(inactiveFilterPresentation);
            initializingViewModel.SetupGet(viewModel => viewModel.IsLoaded).Returns(false);
            initializingViewModel.SetupGet(viewModel => viewModel.IsLoading).Returns(false);
            initializingViewModel.SetupGet(viewModel => viewModel.ErrorMessage).Returns(string.Empty);
            initializingViewModel.Setup(viewModel => viewModel.ClearFilter());
            initializingViewModel.Setup(
                viewModel => viewModel.ToggleElementTypeFilter(It.IsAny<Type>()));
            initializingViewModel
                .Setup(viewModel => viewModel.InitializeAsync(It.IsAny<CancellationToken>()))
                .Returns<CancellationToken>(token =>
                {
                    initializationToken = token;

                    return initialization.Task;
                });
            initializingViewModel
                .Setup(viewModel => viewModel.Dispose())
                .Callback(() => initialization.TrySetResult(false));
            survivingViewModel.SetupGet(viewModel => viewModel.RootNodes).Returns(survivingRoots);
            survivingViewModel.SetupGet(viewModel => viewModel.AvailableElementTypes).Returns(EmptyElementTypes);
            survivingViewModel.SetupProperty(viewModel => viewModel.FilterText, string.Empty);
            survivingViewModel.SetupGet(viewModel => viewModel.SelectedElementTypes)
                .Returns(EmptyElementTypes);
            survivingViewModel.SetupGet(viewModel => viewModel.SelectedNode).Returns(survivingNode);
            survivingViewModel.SetupGet(viewModel => viewModel.FilterPresentation)
                .Returns(inactiveFilterPresentation);
            survivingViewModel.SetupGet(viewModel => viewModel.IsLoaded).Returns(true);
            survivingViewModel.SetupGet(viewModel => viewModel.IsLoading).Returns(false);
            survivingViewModel.SetupGet(viewModel => viewModel.ErrorMessage).Returns(string.Empty);
            survivingViewModel.Setup(viewModel => viewModel.Dispose());
            survivingViewModel.Setup(viewModel => viewModel.ClearFilter());
            survivingViewModel.Setup(
                viewModel => viewModel.ToggleElementTypeFilter(It.IsAny<Type>()));
            var projectBrowserViewModels = new Queue<IProjectBrowserViewModel>(
                [initializingViewModel.Object, survivingViewModel.Object]);
            var composition = this.RegisterWorkspaceServices(
                3,
                _ => projectBrowserViewModels.Dequeue());
            survivingViewModel
                .Setup(viewModel => viewModel.SelectNode(survivingNode))
                .Callback(() => composition.Context.SelectedElement = survivingNode.SourceElement);

            using var navigationViewModel = composition.Navigation;
            using var component = this.Render<Modelling>();
            var initializingGroup = composition.Editor.Groups[0];
            var initializingTab = initializingGroup.Tabs.Single(tab => tab.ViewTypeKey == "project-browser");
            var survivingGroup = composition.Editor.Groups[1];
            await this.OpenProjectBrowserAsync(component, survivingGroup.Id);
            var survivingTab = survivingGroup.Tabs.Single(tab => tab.ViewTypeKey == "project-browser");

            await component.Find(
                    $"[data-testid='editor-workspace-tab-close'][data-tab-id='{initializingTab.Id}']")
                .ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(initializationToken.CanBeCanceled, Is.False);
                    Assert.That(initializationToken.IsCancellationRequested, Is.False);
                    Assert.That(initializingRoots, Is.Empty);
                    initializingViewModel.Verify(viewModel => viewModel.Dispose(), Times.Once);
                    survivingViewModel.Verify(viewModel => viewModel.Dispose(), Times.Never);
                    Assert.That(
                        GetRenderedProjectBrowserViewModel(component, survivingTab.Id),
                        Is.SameAs(survivingViewModel.Object));
                }
            });
        }

        /// <summary>
        /// Verifies closing every Project Browser releases each instance and a later open creates a fresh one.
        /// </summary>
        [Test]
        public async Task VerifyClosingAllProjectBrowsersReleasesInstancesBeforeFreshOpen()
        {
            var composition = this.RegisterWorkspaceServices(3);
            using var navigationViewModel = composition.Navigation;
            using var component = this.Render<Modelling>();
            var initialGroup = composition.Editor.Groups[0];

            await this.OpenProjectBrowserAsync(component, initialGroup.Id);
            await this.OpenProjectBrowserAsync(component, initialGroup.Id);

            var projectBrowserTabs = initialGroup.Tabs
                .Where(tab => tab.ViewTypeKey == "project-browser")
                .ToArray();

            foreach (var tab in projectBrowserTabs)
            {
                await component.Find(
                        $"[data-testid='editor-workspace-tab-close'][data-tab-id='{tab.Id}']")
                    .ClickAsync();
            }

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(composition.Editor.Groups.SelectMany(group => group.Tabs)
                        .Any(tab => tab.ViewTypeKey == "project-browser"), Is.False);

                    foreach (var projectBrowser in composition.ProjectBrowsers)
                    {
                        projectBrowser.Verify(viewModel => viewModel.Dispose(), Times.Once);
                    }
                }
            });

            var remainingGroup = composition.Editor.Groups[0];
            await this.OpenProjectBrowserAsync(component, remainingGroup.Id);

            await component.WaitForAssertionAsync(() =>
            {
                var newTab = remainingGroup.Tabs.Single(tab => tab.ViewTypeKey == "project-browser");

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(composition.ProjectBrowsers, Has.Count.EqualTo(4));
                    Assert.That(composition.ProjectBrowsers.Take(3)
                        .Any(previous => ReferenceEquals(previous.Object, composition.ProjectBrowsers[3].Object)),
                        Is.False);
                    Assert.That(
                        GetRenderedProjectBrowserViewModel(component, newTab.Id),
                        Is.SameAs(composition.ProjectBrowsers[3].Object));
                }
            });
        }

        /// <summary>
        /// Verifies Modelling disposal releases all survivors once without disposing shared composition state.
        /// </summary>
        [Test]
        public async Task VerifyModellingDisposalReleasesAllSurvivingProjectBrowsersExactlyOnce()
        {
            var composition = this.RegisterWorkspaceServices(3);
            using var navigationViewModel = composition.Navigation;
            var component = this.Render<Modelling>();

            await this.OpenProjectBrowserAsync(component, composition.Editor.Groups[1].Id);
            component.Instance.Dispose();
            component.Instance.Dispose();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(composition.ProjectBrowsers, Has.Count.EqualTo(2));
                composition.ProjectBrowsers[0].Verify(viewModel => viewModel.Dispose(), Times.Once);
                composition.ProjectBrowsers[1].Verify(viewModel => viewModel.Dispose(), Times.Once);
                Assert.That(
                    composition.Editor.TryOpenTab(
                        composition.Editor.Groups[0].Id,
                        "Still active",
                        "placeholder",
                        out _),
                    Is.True);
                Assert.That(composition.Context, Is.SameAs(this.Services.GetRequiredService<IElementSelectionService>()));
            }
        }

        /// <summary>
        /// Verifies a failed durable tab open immediately disposes its DI-resolved candidate.
        /// </summary>
        [Test]
        public void VerifyFailedProjectBrowserOpenDisposesCandidateWithoutOwnershipEntry()
        {
            var composition = this.RegisterWorkspaceServices(3);
            composition.Editor.Dispose();
            using var navigationViewModel = composition.Navigation;
            using var component = this.Render<Modelling>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(composition.Editor.Groups.SelectMany(group => group.Tabs), Is.Empty);
                Assert.That(composition.ProjectBrowsers, Has.Count.EqualTo(1));
                composition.ProjectBrowsers[0].Verify(viewModel => viewModel.Dispose(), Times.Once);
                Assert.That(component.FindAll("[data-testid='workspace-project-browser']"), Is.Empty);
                Assert.That(component.FindAll("[data-testid='workspace-project-browser-unavailable']"), Is.Empty);
            }

            component.Instance.Dispose();
            composition.ProjectBrowsers[0].Verify(viewModel => viewModel.Dispose(), Times.Once);
        }

        /// <summary>
        /// Verifies Project Browser selection reaches Details Panel through the shared context instance.
        /// </summary>
        [Test]
        public async Task VerifyProjectBrowserSelectionFlowsToDetailsPanelThroughSharedContext()
        {
            var composition = this.RegisterWorkspaceServices(3);
            using var navigationViewModel = composition.Navigation;
            using var component = this.RenderWorkspaceLayoutWithModelling();

            Assert.That(component.FindAll(".mb-details-panel__empty"), Has.Count.EqualTo(1));

            await component.Find(".mb-project-browser-node__row").ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(composition.Context.SelectedElement,
                        Is.SameAs(composition.ProjectBrowserNodes[0].SourceElement));
                    Assert.That(component.FindAll(".mb-details-panel__empty"), Is.Empty);
                    Assert.That(component.FindAll(".mb-details-panel__properties"), Has.Count.EqualTo(1));
                }
            });
        }

        /// <summary>
        /// Verifies the rail's persistent presentation controls the shell-owned navigation width state.
        /// </summary>
        [Test]
        public async Task VerifyNavigationCollapsePresentationUpdatesWorkspaceShell()
        {
            var composition = this.RegisterWorkspaceServices(3);
            using var navigationViewModel = composition.Navigation;
            using var component = this.RenderWorkspaceLayoutWithModelling();

            Assert.That(component.Find("section.mb-workspace-shell")
                .GetAttribute("data-navigation-collapsed"), Is.EqualTo("true"));

            await component.Find(".mb-navigation-rail__collapse-toggle").ClickAsync();
            var expandedAction = await component.FindComponent<BbPortalHost>()
                .WaitForElementsAsync("[role='menuitem']", 3);
            await expandedAction.Single(item => item.TextContent.Trim().Contains("Expanded", StringComparison.Ordinal))
                .ClickAsync();

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

        [Test]
        public async Task VerifyNavigationHoverDoesNotChangeShellWidthReservation()
        {
            var composition = this.RegisterWorkspaceServices(3);
            using var navigationViewModel = composition.Navigation;
            composition.Navigation.PresentationMode = NavigationRailPresentationMode.ExpandOnHover;
            using var component = this.RenderWorkspaceLayoutWithModelling();
            var shell = component.Find("section.mb-workspace-shell");
            var editorWorkspace = component.FindComponent<EditorWorkspaceComponent>().Instance;

            await component.Find("nav.mb-navigation-rail").TriggerEventAsync("onmouseenter", new MouseEventArgs());

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(shell.GetAttribute("data-navigation-collapsed"), Is.EqualTo("true"));
                    Assert.That(component.Find("nav.mb-navigation-rail").GetAttribute("data-overlay-expanded"),
                        Is.EqualTo("true"));
                    Assert.That(component.FindComponent<EditorWorkspaceComponent>().Instance, Is.SameAs(editorWorkspace));
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
                "Workspace",
                "Modelling.razor.css"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(style, Does.Contain("height: 100%;"));
                Assert.That(style, Does.Contain("min-width: 0;"));
                Assert.That(style, Does.Contain("min-height: 0;"));
                Assert.That(style, Does.Contain("overflow: hidden;"));
                Assert.That(style, Does.Not.Contain("mb-workspace-shell"));
                Assert.That(style, Does.Not.Contain("mb-main-workspace__brand"));
                Assert.That(style, Does.Contain("var(--mb-color-workspace-background)"));
                Assert.That(style, Does.Not.Match("#[0-9a-fA-F]{3,8}"));
                Assert.That(style, Does.Not.Contain("border-radius"));
            }
        }

        /// <summary>
        /// Renders the editor feature as a real body of the shared workspace layout.
        /// </summary>
        /// <returns>The rendered routed workspace composition.</returns>
        private IRenderedComponent<WorkspaceLayout> RenderWorkspaceLayoutWithModelling()
        {
            RenderFragment body = builder =>
            {
                builder.OpenComponent<Modelling>(0);
                builder.CloseComponent();
            };

            return this.Render<WorkspaceLayout>(parameters => parameters.Add(layout => layout.Body, body));
        }

        /// <summary>
        /// Opens a Project Browser through the add-tab menu owned by one exact editor group.
        /// </summary>
        /// <param name="component">The rendered Modelling composition.</param>
        /// <param name="groupId">The target editor group.</param>
        /// <returns>A task representing the interaction.</returns>
        private async Task OpenProjectBrowserAsync(IRenderedComponent<Modelling> component, Guid groupId)
        {
            await FindAddTabMenu(component, groupId).QuerySelector("button").ClickAsync();
            await this.FindPortalledMenuItem("Project Browser").ClickAsync();
        }

        /// <summary>
        /// Gets the exact Project Browser ViewModel rendered for one tab identity.
        /// </summary>
        /// <param name="component">The rendered Modelling composition.</param>
        /// <param name="tabId">The represented durable tab identity.</param>
        /// <returns>The ViewModel supplied to the matching Project Browser component.</returns>
        private static IProjectBrowserViewModel GetRenderedProjectBrowserViewModel(
            IRenderedComponent<Modelling> component,
            Guid tabId)
        {
            return component.FindComponents<ProjectBrowserComponent>()
                .Single(browser => browser.Instance.AdditionalAttributes.TryGetValue("data-tab-id", out var value)
                                   && string.Equals(value?.ToString(), tabId.ToString(), StringComparison.Ordinal))
                .Instance.ViewModel;
        }

        /// <summary>
        /// Finds the generic add-tab menu belonging to one exact editor group.
        /// </summary>
        /// <param name="component">The rendered Modelling composition.</param>
        /// <param name="groupId">The represented editor group.</param>
        /// <returns>The matching menu root.</returns>
        private static IElement FindAddTabMenu(IRenderedComponent<Modelling> component, Guid groupId)
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
        /// <param name="createProjectBrowserViewModel">The optional transient ViewModel creator.</param>
        /// <returns>The exact registered state instances.</returns>
        private (
            WorkspaceEditorViewModel Editor,
            NavigationRailViewModel Navigation,
            List<Mock<IProjectBrowserViewModel>> ProjectBrowsers,
            ContextAwareService Context,
            List<ProjectBrowserNodeViewModel> ProjectBrowserNodes) RegisterWorkspaceServices(
            int maximumGroupCount,
            Func<ContextAwareService, IProjectBrowserViewModel> createProjectBrowserViewModel = null)
        {
            var context = new ContextAwareService();
            var editorViewModel = new WorkspaceEditorViewModel(
                Options.Create(new WorkspaceEditorOptions { MaximumGroupCount = maximumGroupCount }));
            var navigationViewModel = new NavigationRailViewModel(
                context,
                new NavigationRailItemProvider());
            var projectBrowserViewModels = new List<Mock<IProjectBrowserViewModel>>();
            var projectBrowserNodes = new List<ProjectBrowserNodeViewModel>();
            using var filterPresentationOwner = new ProjectBrowserViewModel(
                new Mock<IModelLoaderService>(MockBehavior.Strict).Object,
                context);
            var inactiveFilterPresentation = filterPresentationOwner.FilterPresentation;

            this.Services.AddTransient<IProjectBrowserViewModel>(_ =>
            {
                if (createProjectBrowserViewModel != null)
                {
                    return createProjectBrowserViewModel(context);
                }

                var instanceNumber = projectBrowserViewModels.Count + 1;
                var projectBrowserNode = ProjectBrowserNodeTestFactory.CreateNamespaceNode(
                    $"project-root-{instanceNumber}",
                    $"Project root {instanceNumber}");
                var mutableRootNodes = new ObservableCollection<ProjectBrowserNodeViewModel>
                {
                    projectBrowserNode
                };
                var rootNodes = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRootNodes);
                var projectBrowserViewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
                projectBrowserViewModel.SetupGet(viewModel => viewModel.RootNodes).Returns(rootNodes);
                projectBrowserViewModel.SetupGet(viewModel => viewModel.AvailableElementTypes).Returns(EmptyElementTypes);
                projectBrowserViewModel.SetupProperty(viewModel => viewModel.FilterText, string.Empty);
                projectBrowserViewModel.SetupGet(viewModel => viewModel.SelectedElementTypes)
                    .Returns(EmptyElementTypes);
                projectBrowserViewModel.SetupGet(viewModel => viewModel.SelectedNode)
                    .Returns(projectBrowserNode);
                projectBrowserViewModel.SetupGet(viewModel => viewModel.FilterPresentation)
                    .Returns(inactiveFilterPresentation);
                projectBrowserViewModel.SetupGet(viewModel => viewModel.IsLoaded).Returns(true);
                projectBrowserViewModel.SetupGet(viewModel => viewModel.IsLoading).Returns(false);
                projectBrowserViewModel.SetupGet(viewModel => viewModel.ErrorMessage).Returns(string.Empty);
                projectBrowserViewModel.Setup(viewModel => viewModel.Dispose());
                projectBrowserViewModel.Setup(viewModel => viewModel.ClearFilter());
                projectBrowserViewModel.Setup(
                    viewModel => viewModel.ToggleElementTypeFilter(It.IsAny<Type>()));
                projectBrowserViewModel
                    .Setup(viewModel => viewModel.SelectNode(projectBrowserNode))
                    .Callback(() => context.SelectedElement = projectBrowserNode.SourceElement);
                projectBrowserViewModels.Add(projectBrowserViewModel);
                projectBrowserNodes.Add(projectBrowserNode);

                return projectBrowserViewModel.Object;
            });

            this.Services.AddSingleton<IWorkspaceEditorViewModel>(editorViewModel);
            this.Services.AddSingleton<INavigationRailViewModel>(navigationViewModel);
            this.Services.AddSingleton<IContextAwareService>(context);
            this.Services.AddSingleton<IElementSelectionService>(context);
            this.portalHost = this.Render<BbPortalHost>();

            return (
                editorViewModel,
                navigationViewModel,
                projectBrowserViewModels,
                context,
                projectBrowserNodes);
        }
    }
}
