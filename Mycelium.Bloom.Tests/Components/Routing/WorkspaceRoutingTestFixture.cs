// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceRoutingTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Bunit;

    using Microsoft.AspNetCore.Components;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;

    using Moq;

    using Mycelium.Bloom.Components.Layout;
    using Mycelium.Bloom.Components.Pages.Workspace;
    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.NavigationRail;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;
    using Mycelium.Bloom.ViewModel.WorkspaceEditor;

    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;

    using AppHeaderComponent = Mycelium.Bloom.Components.UI.Organisms.AppHeader.AppHeader;
    using DetailsPanelComponent = Mycelium.Bloom.Components.UI.Organisms.DetailsPanel.DetailsPanel;
    using EditorWorkspaceComponent = Mycelium.Bloom.Components.UI.Organisms.EditorWorkspace.EditorWorkspace;
    using NavigationRailComponent = Mycelium.Bloom.Components.UI.Organisms.NavigationRail.NavigationRail;
    using ProjectBrowserComponent = Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser.ProjectBrowser;
    using StatusBarComponent = Mycelium.Bloom.Components.UI.Organisms.StatusBar.StatusBar;
    using WorkspaceShellComponent = Mycelium.Bloom.Components.UI.Organisms.WorkspaceShell.WorkspaceShell;

    /// <summary>
    /// Tests the real router boundary shared by workspace pages.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class WorkspaceRoutingTestFixture : BunitContext
    {
        private static readonly ReadOnlyObservableCollection<Type> EmptyElementTypes =
            new(new ObservableCollection<Type>());

        private readonly List<IWorkspaceEditorViewModel> editorViewModels = [];
        private readonly List<Mock<IProjectBrowserViewModel>> projectBrowserViewModels = [];
        private readonly List<INavigationRailViewModel> navigationViewModels = [];
        private readonly ContextAwareService context;
        private readonly Mock<IElementIdResolver> elementIdResolver;
        private readonly ProjectBrowserFilterPresentation inactiveFilterPresentation;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkspaceRoutingTestFixture" /> class.
        /// </summary>
        public WorkspaceRoutingTestFixture()
        {
            BlueprintTestSetup.Configure(this);

            this.context = new ContextAwareService();
            this.elementIdResolver = new Mock<IElementIdResolver>(MockBehavior.Strict);
            var editorOptions = Options.Create(new WorkspaceEditorOptions { MaximumGroupCount = 3 });
            using var filterPresentationOwner = new ProjectBrowserViewModel(
                new Mock<IModelLoaderService>(MockBehavior.Strict).Object,
                this.context);
            this.inactiveFilterPresentation = filterPresentationOwner.FilterPresentation;

            this.Services.AddSingleton<IContextAwareService>(this.context);
            this.Services.AddSingleton<IElementSelectionService>(this.context);
            this.Services.AddSingleton(this.elementIdResolver.Object);
            this.Services.AddScoped<Func<IWorkspaceUrlContextService>>(serviceProvider =>
                () => new WorkspaceUrlContextService(
                    serviceProvider.GetRequiredService<NavigationManager>(),
                    this.elementIdResolver.Object,
                    this.context,
                    NullLogger<WorkspaceUrlContextService>.Instance));
            this.Services.AddSingleton<INavigationRailItemProvider, NavigationRailItemProvider>();
            this.Services.AddScoped<Func<INavigationRailViewModel>>(serviceProvider =>
                () =>
                {
                    var viewModel = ActivatorUtilities.CreateInstance<NavigationRailViewModel>(serviceProvider);
                    this.navigationViewModels.Add(viewModel);

                    return viewModel;
                });
            this.Services.AddSingleton<IOptions<WorkspaceEditorOptions>>(editorOptions);
            this.Services.AddScoped<Func<IWorkspaceEditorViewModel>>(_ =>
                () =>
                {
                    var viewModel = new WorkspaceEditorViewModel(editorOptions);
                    this.editorViewModels.Add(viewModel);

                    return viewModel;
                });
            this.Services.AddScoped<Func<IProjectBrowserViewModel>>(_ => this.CreateProjectBrowserViewModel);

            var themeModule = this.JSInterop.SetupModule(
                "./_content/BlazorBlueprint.Components/js/theme.js");
            themeModule.SetupVoid("applyTheme", invocation => true).SetVoidResult();
            themeModule.SetupVoid("applyDarkMode", invocation => true).SetVoidResult();
            themeModule.SetupVoid("saveTheme", invocation => true).SetVoidResult();
        }

        /// <summary>
        /// Disposes the bUnit context after each test.
        /// </summary>
        [TearDown]
        public Task TearDown()
        {
            return this.DisposeAsync().AsTask();
        }

        /// <summary>
        /// Verifies both Modelling entry routes and their query or fragment variants select the canonical destination.
        /// </summary>
        /// <param name="route">The Modelling route to load directly.</param>
        [TestCase("/")]
        [TestCase("/workspace/modeling")]
        [TestCase("/?foo=bar")]
        [TestCase("/workspace/modeling?foo=bar#test")]
        public void VerifyDirectModellingRoutesUseCanonicalWorkspaceDestination(string route)
        {
            this.Services.GetRequiredService<NavigationManager>().NavigateTo(route);

            using var routes = this.Render<Mycelium.Bloom.Components.Routes>();
            var navigation = routes.FindComponent<NavigationRailComponent>();
            var modellingLink = routes.Find("a[aria-label='Modelling']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(routes.FindComponents<WorkspaceLayout>(), Has.Count.EqualTo(1));
                Assert.That(routes.FindComponents<WorkspaceShellComponent>(), Has.Count.EqualTo(1));
                Assert.That(routes.FindComponents<AppHeaderComponent>(), Has.Count.EqualTo(1));
                Assert.That(routes.FindComponents<NavigationRailComponent>(), Has.Count.EqualTo(1));
                Assert.That(routes.FindComponents<StatusBarComponent>(), Has.Count.EqualTo(1));
                Assert.That(routes.FindComponents<Modelling>(), Has.Count.EqualTo(1));
                Assert.That(routes.FindComponents<Dashboard>(), Is.Empty);
                Assert.That(routes.FindComponents<EditorWorkspaceComponent>(), Has.Count.EqualTo(1));
                Assert.That(routes.Find(".mb-workspace-shell__main h1").TextContent.Trim(),
                    Is.EqualTo("Modelling"));
                Assert.That(navigation.Instance.ViewModel.SelectedItem.Id, Is.EqualTo("modelling"));
                Assert.That(modellingLink.GetAttribute("href"), Is.EqualTo("/workspace/modeling"));
                Assert.That(modellingLink.GetAttribute("aria-current"), Is.EqualTo("page"));
                Assert.That(routes.FindAll(".mb-navigation-rail__link[aria-current='page']"),
                    Has.Count.EqualTo(1));
            }
        }

        /// <summary>
        /// Verifies a direct proof-route load selects Dashboard without constructing editor state.
        /// </summary>
        [Test]
        public void VerifyDirectDashboardRouteUsesSharedWorkspaceLayout()
        {
            this.Services.GetRequiredService<NavigationManager>()
                .NavigateTo("/workspace/dashboard?panel=summary#workspace-dashboard-heading");

            using var routes = this.Render<Mycelium.Bloom.Components.Routes>();
            var navigation = routes.FindComponent<NavigationRailComponent>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(routes.FindComponents<WorkspaceLayout>(), Has.Count.EqualTo(1));
                Assert.That(routes.FindComponents<WorkspaceShellComponent>(), Has.Count.EqualTo(1));
                Assert.That(routes.FindComponents<AppHeaderComponent>(), Has.Count.EqualTo(1));
                Assert.That(routes.FindComponents<NavigationRailComponent>(), Has.Count.EqualTo(1));
                Assert.That(routes.FindComponents<DetailsPanelComponent>(), Has.Count.EqualTo(1));
                Assert.That(routes.FindComponents<StatusBarComponent>(), Has.Count.EqualTo(1));
                Assert.That(routes.FindComponents<Dashboard>(), Has.Count.EqualTo(1));
                Assert.That(routes.FindComponents<Modelling>(), Is.Empty);
                Assert.That(routes.Find(".mb-workspace-shell__main h1").TextContent.Trim(),
                    Is.EqualTo("Dashboard"));
                Assert.That(routes.FindAll("header.mb-app-header h1"), Is.Empty);
                Assert.That(navigation.Instance.ViewModel.SelectedItem.Id, Is.EqualTo("dashboard"));
                Assert.That(routes.Find("a[aria-label='Dashboard']").GetAttribute("aria-current"),
                    Is.EqualTo("page"));
                Assert.That(routes.FindAll(".mb-navigation-rail__link[aria-current='page']"),
                    Has.Count.EqualTo(1));
                Assert.That(this.editorViewModels, Is.Empty);
                Assert.That(this.projectBrowserViewModels, Is.Empty);
            }
        }

        /// <summary>
        /// Verifies direct workspace URLs restore shared model identity and the matching route destination.
        /// </summary>
        /// <param name="route">The direct workspace route.</param>
        /// <param name="expectedDestinationId">The canonical NavigationRail destination.</param>
        /// <param name="expectsProjectBrowser">Whether the route composes modeling content.</param>
        [TestCase("/?selectedElement=part%2Falpha%20value", "modelling", true)]
        [TestCase("/workspace/modeling?selectedElement=part%2Falpha%20value", "modelling", true)]
        [TestCase("/workspace/dashboard?selectedElement=part%2Falpha%20value", "dashboard", false)]
        public void VerifyDirectWorkspaceUrlRestoresSelectedElement(
            string route,
            string expectedDestinationId,
            bool expectsProjectBrowser)
        {
            var element = new Namespace { ElementId = "part/alpha value" };
            this.elementIdResolver
                .Setup(resolver => resolver.ResolveAsync(
                    "part/alpha value",
                    It.IsAny<CancellationToken>()))
                .Returns(() => ValueTask.FromResult<IElement>(element));
            this.Services.GetRequiredService<NavigationManager>().NavigateTo(route);

            using var routes = this.Render<Mycelium.Bloom.Components.Routes>();

            routes.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(this.context.SelectedElement, Is.SameAs(element));
                    Assert.That(
                        routes.FindComponent<NavigationRailComponent>().Instance.ViewModel.SelectedItem.Id,
                        Is.EqualTo(expectedDestinationId));
                    Assert.That(
                        routes.FindComponents<ProjectBrowserComponent>(),
                        Has.Count.EqualTo(expectsProjectBrowser ? 1 : 0));
                }
            });

            if (expectsProjectBrowser)
            {
                this.projectBrowserViewModels[0].Verify(
                    viewModel => viewModel.FocusElement(element),
                    Times.Once);
            }
        }

        /// <summary>
        /// Verifies a selected element survives destination changes without carrying unrelated route context.
        /// </summary>
        [Test]
        public async Task VerifyNavigationRailDestinationPreservesOnlySelectedElementContext()
        {
            var element = new Namespace { ElementId = "part/alpha value" };
            this.elementIdResolver
                .Setup(resolver => resolver.ResolveAsync(
                    "part/alpha value",
                    It.IsAny<CancellationToken>()))
                .Returns(() => ValueTask.FromResult<IElement>(element));
            var navigation = this.Services.GetRequiredService<NavigationManager>();
            navigation.NavigateTo("/workspace/modeling?panel=editor#old");
            using var routes = this.Render<Mycelium.Bloom.Components.Routes>();

            await routes.InvokeAsync(() => this.context.SelectedElement = element);
            await routes.WaitForAssertionAsync(() =>
                Assert.That(navigation.Uri, Does.Contain("selectedElement=part%2Falpha%20value")));
            var dashboardHref = routes.Find("a[aria-label='Dashboard']").GetAttribute("href");

            Assert.That(
                dashboardHref,
                Is.EqualTo("/workspace/dashboard?selectedElement=part%2Falpha%20value"));

            navigation.NavigateTo(dashboardHref);

            routes.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(routes.FindComponents<Dashboard>(), Has.Count.EqualTo(1));
                    Assert.That(this.context.SelectedElement, Is.SameAs(element));
                    Assert.That(
                        routes.FindComponent<NavigationRailComponent>().Instance.ViewModel.SelectedItem.Id,
                        Is.EqualTo("dashboard"));
                }
            });
        }

        /// <summary>
        /// Verifies a selected-element-only location update preserves the existing editor and browser instances.
        /// </summary>
        [Test]
        public void VerifyQueryOnlySelectionUpdateDoesNotRemountModellingState()
        {
            var element = new Namespace { ElementId = "restored" };
            this.elementIdResolver
                .Setup(resolver => resolver.ResolveAsync("restored", It.IsAny<CancellationToken>()))
                .Returns(() => ValueTask.FromResult<IElement>(element));
            using var routes = this.Render<Mycelium.Bloom.Components.Routes>();
            var layout = routes.FindComponent<WorkspaceLayout>().Instance;
            var editor = routes.FindComponent<EditorWorkspaceComponent>().Instance.ViewModel;
            var browser = this.projectBrowserViewModels[0];

            this.Services.GetRequiredService<NavigationManager>()
                .NavigateTo("/workspace/modeling?selectedElement=restored");

            routes.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(routes.FindComponent<WorkspaceLayout>().Instance, Is.SameAs(layout));
                    Assert.That(routes.FindComponent<EditorWorkspaceComponent>().Instance.ViewModel, Is.SameAs(editor));
                    Assert.That(this.context.SelectedElement, Is.SameAs(element));
                }
            });

            browser.Verify(viewModel => viewModel.Dispose(), Times.Never);
            browser.Verify(viewModel => viewModel.FocusElement(element), Times.Once);
        }

        /// <summary>
        /// Verifies workspace route changes reuse chrome and preserve layout state while editor state stays page-owned.
        /// </summary>
        [Test]
        public async Task VerifyWorkspaceRouteRoundTripPreservesChromeAndRecreatesEditorSession()
        {
            using var routes = this.Render<Mycelium.Bloom.Components.Routes>();
            var initialLayout = routes.FindComponent<WorkspaceLayout>().Instance;
            var initialNavigation = routes.FindComponent<NavigationRailComponent>().Instance.ViewModel;
            var initialEditor = routes.FindComponent<EditorWorkspaceComponent>().Instance.ViewModel;
            var initialEditorGroupId = initialEditor.Groups[0].Id;
            initialNavigation.PresentationMode = NavigationRailPresentationMode.Expanded;

            await routes.Find("button[aria-label='Switch to dark mode']").ClickAsync();
            await routes.Find(".mb-details-panel button[aria-label='Close details panel']").ClickAsync();
            this.Services.GetRequiredService<NavigationManager>().NavigateTo("/workspace/dashboard");

            routes.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(routes.FindComponents<WorkspaceLayout>().Single().Instance, Is.SameAs(initialLayout));
                    Assert.That(routes.FindComponents<WorkspaceShellComponent>(), Has.Count.EqualTo(1));
                    Assert.That(routes.FindComponents<AppHeaderComponent>(), Has.Count.EqualTo(1));
                    Assert.That(routes.FindComponents<NavigationRailComponent>(), Has.Count.EqualTo(1));
                    Assert.That(routes.FindComponents<StatusBarComponent>(), Has.Count.EqualTo(1));
                    Assert.That(routes.FindComponents<DetailsPanelComponent>(), Is.Empty);
                    Assert.That(routes.FindAll("aside.mb-workspace-shell__right-panel"), Is.Empty);
                    Assert.That(routes.Find("button[aria-label='Open details panel']")
                        .GetAttribute("aria-pressed"), Is.EqualTo("false"));
                    Assert.That(routes.FindComponents<EditorWorkspaceComponent>(), Is.Empty);
                    Assert.That(routes.Find(".mb-workspace-shell__main h1").TextContent.Trim(),
                        Is.EqualTo("Dashboard"));
                    Assert.That(routes.FindComponent<NavigationRailComponent>().Instance.ViewModel,
                        Is.SameAs(initialNavigation));
                    Assert.That(initialNavigation.SelectedItem.Id, Is.EqualTo("dashboard"));
                    Assert.That(initialNavigation.PresentationMode,
                        Is.EqualTo(NavigationRailPresentationMode.Expanded));
                    Assert.That(routes.Find("button[aria-label='Switch to light mode']"), Is.Not.Null);
                    this.projectBrowserViewModels[0].Verify(viewModel => viewModel.Dispose(), Times.Once);
                }
            });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    initialEditor.TryOpenTab(
                        initialEditorGroupId,
                        "Disposed editor",
                        "placeholder",
                        out var disposedEditorTab),
                    Is.False);
                Assert.That(disposedEditorTab, Is.Null);
                Assert.That(this.navigationViewModels, Has.Count.EqualTo(1));
            }

            var modellingHref = routes.Find("a[aria-label='Modelling']").GetAttribute("href");
            Assert.That(modellingHref, Is.EqualTo("/workspace/modeling"));
            this.Services.GetRequiredService<NavigationManager>().NavigateTo(modellingHref);

            routes.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(routes.FindComponents<WorkspaceLayout>().Single().Instance, Is.SameAs(initialLayout));
                    Assert.That(routes.FindComponents<WorkspaceShellComponent>(), Has.Count.EqualTo(1));
                    Assert.That(routes.FindComponents<Modelling>(), Has.Count.EqualTo(1));
                    Assert.That(routes.FindComponents<EditorWorkspaceComponent>(), Has.Count.EqualTo(1));
                    Assert.That(routes.FindComponents<DetailsPanelComponent>(), Is.Empty);
                    Assert.That(routes.FindAll("aside.mb-workspace-shell__right-panel"), Is.Empty);
                    Assert.That(routes.Find("button[aria-label='Open details panel']")
                        .GetAttribute("aria-pressed"), Is.EqualTo("false"));
                    Assert.That(routes.FindComponent<EditorWorkspaceComponent>().Instance.ViewModel,
                        Is.Not.SameAs(initialEditor));
                    Assert.That(this.editorViewModels, Has.Count.EqualTo(2));
                    Assert.That(this.projectBrowserViewModels, Has.Count.EqualTo(2));
                    Assert.That(initialNavigation.SelectedItem.Id, Is.EqualTo("modelling"));
                    Assert.That(initialNavigation.PresentationMode,
                        Is.EqualTo(NavigationRailPresentationMode.Expanded));
                    Assert.That(this.navigationViewModels, Has.Count.EqualTo(1));
                    Assert.That(routes.Find("button[aria-label='Switch to light mode']"), Is.Not.Null);
                    Assert.That(routes.Find(".mb-workspace-shell__main h1").TextContent.Trim(),
                        Is.EqualTo("Modelling"));
                    Assert.That(routes.FindAll(".mb-navigation-rail__link[aria-current='page']"),
                        Has.Count.EqualTo(1));
                }
            });

            await routes.Find("button[aria-label='Open details panel']").ClickAsync();

            await routes.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(routes.FindComponents<DetailsPanelComponent>(), Has.Count.EqualTo(1));
                    Assert.That(routes.FindAll("aside.mb-workspace-shell__right-panel"), Has.Count.EqualTo(1));
                    Assert.That(routes.FindAll(".mb-details-panel"), Has.Count.EqualTo(1));
                }
            });
        }

        /// <summary>
        /// Verifies leaving the workspace layout releases its navigation subscription.
        /// </summary>
        [Test]
        public void VerifyLeavingWorkspaceLayoutDisposesNavigationState()
        {
            using var routes = this.Render<Mycelium.Bloom.Components.Routes>();
            var navigation = routes.FindComponent<NavigationRailComponent>().Instance.ViewModel;
            var notificationCount = 0;
            PropertyChangedEventHandler handler = (_, args) =>
            {
                if (string.Equals(args.PropertyName, nameof(navigation.NavigationItems), StringComparison.Ordinal))
                {
                    notificationCount++;
                }
            };

            navigation.PropertyChanged += handler;
            this.Services.GetRequiredService<NavigationManager>().NavigateTo("/route-that-does-not-exist");

            routes.WaitForAssertion(() =>
            {
                Assert.That(routes.FindComponents<WorkspaceLayout>(), Is.Empty);
            });

            notificationCount = 0;
            this.context.LifecycleState = ProjectLifecycleState.Open;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(notificationCount, Is.Zero);
                Assert.That(this.navigationViewModels, Has.Count.EqualTo(1));
            }

            navigation.PropertyChanged -= handler;
        }

        private IProjectBrowserViewModel CreateProjectBrowserViewModel()
        {
            var rootNodes = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(
                new ObservableCollection<ProjectBrowserNodeViewModel>());
            var viewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            viewModel.SetupGet(candidate => candidate.RootNodes).Returns(rootNodes);
            viewModel.SetupGet(candidate => candidate.AvailableElementTypes).Returns(EmptyElementTypes);
            viewModel.SetupProperty(candidate => candidate.FilterText, string.Empty);
            viewModel.SetupGet(candidate => candidate.SelectedElementTypes).Returns(EmptyElementTypes);
            viewModel.SetupGet(candidate => candidate.SelectedNode).Returns((ProjectBrowserNodeViewModel)null);
            viewModel.SetupGet(candidate => candidate.FilterPresentation).Returns(this.inactiveFilterPresentation);
            viewModel.SetupGet(candidate => candidate.IsLoaded).Returns(true);
            viewModel.SetupGet(candidate => candidate.IsLoading).Returns(false);
            viewModel.SetupGet(candidate => candidate.ErrorMessage).Returns(string.Empty);
            viewModel.Setup(candidate => candidate.ClearFilter());
            viewModel.Setup(candidate => candidate.ToggleElementTypeFilter(It.IsAny<Type>()));
            viewModel.Setup(candidate => candidate.FocusElement(It.IsAny<IElement>()));
            viewModel.Setup(candidate => candidate.Dispose());
            this.projectBrowserViewModels.Add(viewModel);

            return viewModel.Object;
        }
    }
}
