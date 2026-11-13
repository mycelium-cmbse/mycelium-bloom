// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceLayoutTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.Layout
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reactive.Subjects;
    using System.Threading;
    using System.Threading.Tasks;

    using BlazorBlueprint.Components;
    using BlazorBlueprint.Primitives.Services;

    using Bunit;
    using Bunit.TestDoubles;

    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging.Abstractions;

    using Moq;

    using Mycelium.Bloom.Components.Layout;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.DetailsPanel;
    using Mycelium.Bloom.ViewModel.NavigationRail;
    using Mycelium.SDK;

    using AppHeaderComponent = Mycelium.Bloom.Components.UI.Organisms.AppHeader.AppHeader;
    using DetailsPanelComponent = Mycelium.Bloom.Components.UI.Organisms.DetailsPanel.DetailsPanel;
    using NavigationRailComponent = Mycelium.Bloom.Components.UI.Organisms.NavigationRail.NavigationRail;
    using StatusBarComponent = Mycelium.Bloom.Components.UI.Organisms.StatusBar.StatusBar;
    using WorkspaceShellComponent = Mycelium.Bloom.Components.UI.Organisms.WorkspaceShell.WorkspaceShell;

    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class WorkspaceLayoutTestFixture : BunitContext
    {
        private readonly BunitJSModuleInterop themeModule;

        private readonly ContextAwareService context;

        private readonly Mock<IElementIdResolver> elementIdResolver;

        private int navigationViewModelCreationCount;

        public WorkspaceLayoutTestFixture()
        {
            BlueprintTestSetup.Configure(this);

            this.context = new ContextAwareService();
            this.Services.AddSingleton<IContextAwareService>(this.context);
            this.Services.AddSingleton<IElementSelectionService>(this.context);
            this.Services.AddScoped<System.Func<IDetailsPanelViewModel>>(provider =>
                () => ActivatorUtilities.CreateInstance<DetailsPanelViewModel>(provider));
            this.elementIdResolver = new Mock<IElementIdResolver>(MockBehavior.Strict);
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
                    this.navigationViewModelCreationCount++;

                    return ActivatorUtilities.CreateInstance<NavigationRailViewModel>(serviceProvider);
                });

            this.themeModule = this.JSInterop.SetupModule(
                "./_content/BlazorBlueprint.Components/js/theme.js");
            this.themeModule.SetupVoid("applyTheme", invocation => true).SetVoidResult();
            this.themeModule.SetupVoid("applyDarkMode", invocation => true).SetVoidResult();
            this.themeModule.SetupVoid("saveTheme", invocation => true).SetVoidResult();
        }

        [TearDown]
        public Task TearDown()
        {
            return this.DisposeAsync().AsTask();
        }

        [Test]
        public async Task VerifyQueuedContextUpdatesCannotOverwriteNewerHistoryEntry()
        {
            using var restorations = new Subject<WorkspaceUrlContextRestoration>();
            using var requests = new Subject<string>();
            var urls = new Mock<IWorkspaceUrlContextService>();
            urls.SetupGet(service => service.Restorations).Returns(restorations);
            urls.SetupGet(service => service.NavigationRequests).Returns(requests);
            urls.Setup(service => service.GetDestinationUri(It.IsAny<string>())).Returns((string href) => href);
            this.Services.AddScoped<Func<IWorkspaceUrlContextService>>(_ => () => urls.Object);
            using var component = this.RenderLayout(CreateBody("Editor", "Modelling"));
            var navigation = this.Services.GetRequiredService<NavigationManager>();
            var old = new SysML2.NET.Core.POCO.Root.Namespaces.Namespace { ElementId = "old" };
            var current = new SysML2.NET.Core.POCO.Root.Namespaces.Namespace { ElementId = "current" };

            await component.InvokeAsync(() =>
            {
                Task.Run(() =>
                {
                    restorations.OnNext(new WorkspaceUrlContextRestoration(old, null, false));
                    requests.OnNext(navigation.BaseUri + "workspace/modeling?selectedElement=old");
                }).GetAwaiter().GetResult();
                navigation.NavigateTo("/workspace/modeling?selectedElement=current&inspectedElement=reference", new NavigationOptions { HistoryEntryState = "new entry" });
                this.context.SelectedElement = current;
            });

            await component.InvokeAsync(() => { });
            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.context.SelectedElement, Is.SameAs(current));
                Assert.That(navigation.Uri, Does.Contain("selectedElement=current&inspectedElement=reference"));
                Assert.That(navigation.HistoryEntryState, Is.EqualTo("new entry"));
            }
        }

        [Test]
        public async Task VerifyInspectionHeaderMinimumPreservesPreferredWidthAndPanelLifetime()
        {
            using var component = this.RenderLayout(CreateBody("Editor content", "Modelling"));
            var shell = component.FindComponent<WorkspaceShellComponent>().Instance;
            var details = component.FindComponent<DetailsPanelComponent>().Instance;
            Assert.That(shell.RightPanelMinimumWidth, Is.EqualTo(380));
            await component.InvokeAsync(() => details.MinimumWidthChanged.InvokeAsync(485));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(shell.RightPanelMinimumWidth, Is.EqualTo(485));
                Assert.That(shell.RightPanelWidth, Is.EqualTo(380));
            }
            await component.InvokeAsync(() => details.MinimumWidthChanged.InvokeAsync(900));
            Assert.That(shell.RightPanelMinimumWidth, Is.EqualTo(600));
            await component.InvokeAsync(() => details.MinimumWidthChanged.InvokeAsync(380));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(shell.RightPanelMinimumWidth, Is.EqualTo(380));
                Assert.That(component.FindComponent<DetailsPanelComponent>().Instance, Is.SameAs(details));
            }
        }

        [Test]
        public void VerifyRenderOwnsOneSharedWorkspaceFrameAndInfrastructureSet()
        {
            using var component = this.RenderLayout(CreateBody("Editor content", "Modelling"));
            var shell = component.FindComponent<WorkspaceShellComponent>();
            var links = component.FindAll(".mb-navigation-rail__link");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindComponents<WorkspaceShellComponent>(), Has.Count.EqualTo(1));
                Assert.That(component.FindComponents<AppHeaderComponent>(), Has.Count.EqualTo(1));
                Assert.That(component.FindComponents<NavigationRailComponent>(), Has.Count.EqualTo(1));
                Assert.That(component.FindComponents<DetailsPanelComponent>(), Has.Count.EqualTo(1));
                Assert.That(component.FindComponents<StatusBarComponent>(), Has.Count.EqualTo(1));
                Assert.That(component.FindComponents<BbDarkModeToggle>(), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("aside.mb-workspace-shell__right-panel"), Has.Count.EqualTo(1));
                Assert.That(component.Find("button[aria-label='Collapse Properties'][aria-expanded='true']"),
                    Is.Not.Null);
                Assert.That(component.FindAll("button[aria-label='Collapse Properties']"), Has.Count.EqualTo(1));
                Assert.That(shell.Instance.FullApplication, Is.True);
                Assert.That(component.FindAll("section.mb-workspace-shell[role='main']"), Has.Count.EqualTo(1));
                Assert.That(component.Find(".mb-workspace-shell__main [data-testid='layout-body']")
                    .TextContent.Trim(), Is.EqualTo("ModellingEditor content"));
                Assert.That(component.Find(".mb-workspace-shell__main h1").TextContent.Trim(),
                    Is.EqualTo("Modelling"));
                Assert.That(component.FindAll("header.mb-app-header h1"), Is.Empty);
                Assert.That(component.Find("header.mb-app-header").GetAttribute("style"),
                    Does.Contain("height: 48px"));
                Assert.That(component.Find("button[aria-label='Switch to dark mode']"), Is.Not.Null);
                Assert.That(links.Count(link => link.LocalName == "a"), Is.EqualTo(2));
                Assert.That(links.Count(link => link.HasAttribute("disabled")), Is.EqualTo(14));
                Assert.That(links.Single(link => link.GetAttribute("aria-label") == "Modelling")
                    .GetAttribute("aria-current"), Is.EqualTo("page"));
                Assert.That(links.Single(link => link.GetAttribute("aria-label") == "Modelling")
                    .GetAttribute("href"), Is.EqualTo("/workspace/modeling"));
                Assert.That(links.Count(link => link.GetAttribute("aria-current") == "page"), Is.EqualTo(1));
                Assert.That(component.FindComponents<BbPortalHost>(), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("#blazor-error-ui"), Has.Count.EqualTo(1));
                Assert.That(component.Find("#blazor-error-ui button.dismiss").GetAttribute("type"),
                    Is.EqualTo("button"));
                Assert.That(component.Find("#blazor-error-ui button.dismiss").GetAttribute("aria-label"),
                    Is.EqualTo("Dismiss error notification"));
            }
        }

        [Test]
        public async Task VerifyDetailsPanelCanCloseAndReopen()
        {
            using var component = this.RenderLayout(CreateBody("Editor content", "Modelling"));
            var panel = component.FindComponent<DetailsPanelComponent>().Instance;
            var previous = new SysML2.NET.Core.POCO.Systems.Parts.PartUsage { DeclaredName = "Previous" };
            await component.InvokeAsync(() =>
            {
                this.context.SelectedElement = previous;
                panel.ViewModel.Inspect(new SysML2.NET.Core.POCO.Systems.Parts.PartUsage { DeclaredName = "Inspected" });
            });
            var shell = component.FindComponent<WorkspaceShellComponent>();
            await component.InvokeAsync(() => shell.Instance.CommitRightPanelWidth(475));

            await component.Find("button[aria-label='Collapse Properties']").ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.FindComponents<DetailsPanelComponent>(), Has.Count.EqualTo(1));
                    Assert.That(component.FindAll("aside.mb-workspace-shell__right-panel"), Has.Count.EqualTo(1));
                    Assert.That(component.Find("[data-testid='properties-region']").HasAttribute("hidden"), Is.True);
                    Assert.That(component.Find("[data-testid='properties-region']").HasAttribute("inert"), Is.True);
                    Assert.That(component.FindComponent<DetailsPanelComponent>().Instance, Is.SameAs(panel));
                    Assert.That(shell.Instance.RightPanelWidth, Is.EqualTo(475));
                    Assert.That(component.FindAll("button[aria-label='Collapse Properties']"), Is.Empty);
                    Assert.That(component.Find("button[aria-label='Expand Properties']")
                        .GetAttribute("aria-expanded"), Is.EqualTo("false"));
                }
            });

            await component.InvokeAsync(() => this.context.SelectedElement =
                new SysML2.NET.Core.POCO.Systems.Parts.PartUsage { DeclaredName = "Background selection" });
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("[data-testid='properties-region']").HasAttribute("hidden"), Is.True);
                Assert.That(panel.ViewModel.Presentation.Element, Is.SameAs(this.context.SelectedElement));
            }

            await component.Find("button[aria-label='Expand Properties']").ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.FindComponents<DetailsPanelComponent>(), Has.Count.EqualTo(1));
                    Assert.That(component.FindComponent<DetailsPanelComponent>().Instance, Is.SameAs(panel));
                    Assert.That(component.Find("[data-testid='properties-region']").HasAttribute("hidden"), Is.False);
                    Assert.That(component.Find("[data-testid='properties-region']").HasAttribute("inert"), Is.False);
                    Assert.That(shell.Instance.RightPanelWidth, Is.EqualTo(475));
                    Assert.That(component.FindAll("aside.mb-workspace-shell__right-panel"), Has.Count.EqualTo(1));
                    Assert.That(component.Find("button[aria-label='Collapse Properties'][aria-expanded='true']"),
                        Is.Not.Null);
                    Assert.That(component.FindAll("button[aria-label='Collapse Properties']"),
                        Has.Count.EqualTo(1));
                }
            });
        }

        [Test]
        public async Task VerifyRouteChangesReconcileSelectionWithoutDuplicatingTheFrame()
        {
            using var component = this.RenderLayout(CreateBody("Editor content", "Modelling"));
            var navigationRail = component.FindComponent<NavigationRailComponent>();
            var viewModel = navigationRail.Instance.ViewModel;
            viewModel.PresentationMode = NavigationRailPresentationMode.Expanded;
            var layoutInstance = component.Instance;

            await component.Find("button[aria-label='Collapse Properties']").ClickAsync();

            this.Services.GetRequiredService<NavigationManager>()
                .NavigateTo("/workspace/dashboard?panel=summary#workspace-dashboard-heading");
            component.Render(parameters => parameters
                .Add(layout => layout.Body, CreateBody("Dashboard content", "Dashboard")));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Instance, Is.SameAs(layoutInstance));
                Assert.That(component.FindComponents<WorkspaceShellComponent>(), Has.Count.EqualTo(1));
                Assert.That(component.FindComponents<AppHeaderComponent>(), Has.Count.EqualTo(1));
                Assert.That(component.FindComponents<NavigationRailComponent>(), Has.Count.EqualTo(1));
                Assert.That(component.FindComponents<StatusBarComponent>(), Has.Count.EqualTo(1));
                Assert.That(component.FindComponents<DetailsPanelComponent>(), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("aside.mb-workspace-shell__right-panel"), Has.Count.EqualTo(1));
                Assert.That(component.Find("button[aria-label='Expand Properties']")
                    .GetAttribute("aria-expanded"), Is.EqualTo("false"));
                Assert.That(component.Find(".mb-workspace-shell__main h1").TextContent.Trim(),
                    Is.EqualTo("Dashboard"));
                Assert.That(viewModel.SelectedItem.Id, Is.EqualTo("dashboard"));
                Assert.That(viewModel.PresentationMode, Is.EqualTo(NavigationRailPresentationMode.Expanded));
                Assert.That(component.Find("a[aria-label='Dashboard']").GetAttribute("aria-current"),
                    Is.EqualTo("page"));
                Assert.That(component.Find("a[aria-label='Modelling']").GetAttribute("aria-current"), Is.Null);
                Assert.That(component.FindAll(".mb-navigation-rail__link[aria-current='page']"),
                    Has.Count.EqualTo(1));
                Assert.That(this.navigationViewModelCreationCount, Is.EqualTo(1));
            }
        }

        [Test]
        public async Task VerifyDirectUrlRestoresSelectionAndDerivesNavigationDestinations()
        {
            var element = new SysML2.NET.Core.POCO.Root.Namespaces.Namespace
            {
                ElementId = "part/alpha value"
            };
            this.elementIdResolver
                .Setup(resolver => resolver.ResolveAsync(
                    "part/alpha value",
                    It.IsAny<CancellationToken>()))
                .Returns(() => ValueTask.FromResult<SysML2.NET.Core.POCO.Root.Elements.IElement>(element));
            this.Services.GetRequiredService<NavigationManager>().NavigateTo(
                "/workspace/dashboard?selectedElement=part%2Falpha%20value&panel=summary#target");
            using var component = this.RenderLayout(CreateBody("Dashboard content", "Dashboard"));

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(this.context.SelectedElement, Is.SameAs(element));
                    Assert.That(
                        component.Find("a[aria-label='Modelling']").GetAttribute("href"),
                        Is.EqualTo("/workspace/modeling?selectedElement=part%2Falpha%20value"));
                    Assert.That(
                        component.Find("a[aria-label='Dashboard']").GetAttribute("aria-current"),
                        Is.EqualTo("page"));
                }
            });
        }

        [Test]
        public async Task VerifySelectionUpdatesPreserveUriAndReplaceHistory()
        {
            var first = new SysML2.NET.Core.POCO.Root.Namespaces.Namespace { ElementId = "first/value" };
            var equivalentFirst = new SysML2.NET.Core.POCO.Root.Namespaces.Namespace
            {
                ElementId = "first/value"
            };
            var second = new SysML2.NET.Core.POCO.Root.Namespaces.Namespace { ElementId = "second value" };
            this.elementIdResolver
                .Setup(resolver => resolver.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string elementId, CancellationToken _) =>
                    ValueTask.FromResult<SysML2.NET.Core.POCO.Root.Elements.IElement>(
                        elementId switch
                        {
                            "first/value" => first,
                            "second value" => second,
                            _ => null
                        }));
            var navigation = (BunitNavigationManager)this.Services.GetRequiredService<NavigationManager>();
            navigation.NavigateTo("/?panel=summary#target");
            using var component = this.RenderLayout(CreateBody("Editor content", "Modelling"));

            await component.InvokeAsync(() => this.context.SelectedElement = first);
            await component.WaitForAssertionAsync(() =>
                Assert.That(navigation.Uri, Does.Contain("selectedElement=first%2Fvalue")));
            var historyCountAfterFirstSelection = navigation.History.Count;

            await component.InvokeAsync(() => this.context.SelectedElement = equivalentFirst);

            Assert.That(navigation.History, Has.Count.EqualTo(historyCountAfterFirstSelection));

            await component.InvokeAsync(() => this.context.SelectedElement = second);
            await component.WaitForAssertionAsync(() =>
                Assert.That(navigation.Uri, Does.Contain("selectedElement=second%20value")));

            await component.InvokeAsync(() => this.context.SelectedElement = null);
            await component.WaitForAssertionAsync(() =>
            {
                var absoluteUri = navigation.ToAbsoluteUri(navigation.Uri);
                var query = QueryHelpers.ParseQuery(absoluteUri.Query);
                var latestHistory = navigation.History.First();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(absoluteUri.AbsolutePath, Is.EqualTo("/"));
                    Assert.That(absoluteUri.Fragment, Is.EqualTo("#target"));
                    Assert.That(query["panel"][0], Is.EqualTo("summary"));
                    Assert.That(
                        query.ContainsKey(WorkspaceUrlContextService.SelectedElementParameterName),
                        Is.False);
                    Assert.That(latestHistory.Options.ReplaceHistoryEntry, Is.True);
                    Assert.That(latestHistory.Options.ForceLoad, Is.False);
                }
            });
        }

        [Test]
        public async Task VerifyDisposalStopsUrlContextNavigation()
        {
            var navigation = (BunitNavigationManager)this.Services.GetRequiredService<NavigationManager>();
            using var component = this.RenderLayout(CreateBody("Editor content", "Modelling"));
            var historyCount = navigation.History.Count;

            component.Instance.Dispose();
            await component.InvokeAsync(() => this.context.SelectedElement =
                new SysML2.NET.Core.POCO.Root.Namespaces.Namespace { ElementId = "after-disposal" });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(navigation.History, Has.Count.EqualTo(historyCount));
                Assert.That(navigation.Uri, Does.Not.Contain("selectedElement"));
            }
        }

        [Test]
        public void VerifyDisposalReleasesLayoutOwnedNavigationState()
        {
            using var component = this.RenderLayout(CreateBody("Editor content", "Modelling"));
            var layout = component.Instance;
            var viewModel = component.FindComponent<NavigationRailComponent>().Instance.ViewModel;
            var notificationCount = 0;
            PropertyChangedEventHandler handler = (_, args) =>
            {
                if (string.Equals(args.PropertyName, nameof(viewModel.NavigationItems), StringComparison.Ordinal))
                {
                    notificationCount++;
                }
            };
            viewModel.PropertyChanged += handler;

            layout.Dispose();
            notificationCount = 0;
            this.context.LifecycleState = ProjectLifecycleKind.Open;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(notificationCount, Is.Zero);
                Assert.That(this.navigationViewModelCreationCount, Is.EqualTo(1));
                Assert.DoesNotThrow(layout.Dispose);
            }

            viewModel.PropertyChanged -= handler;
        }

        [Test]
        public async Task VerifyThemeToggleUsesApplicationThemeService()
        {
            using var component = this.RenderLayout(CreateBody("Editor content", "Modelling"));
            var themeService = this.Services.GetRequiredService<ThemeService>();

            Assert.That(themeService.IsDarkMode, Is.False);
            await component.Find("button[aria-label='Switch to dark mode']").ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                var applyDarkModeInvocations = this.themeModule.Invocations["applyDarkMode"];
                var saveThemeInvocations = this.themeModule.Invocations["saveTheme"];

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(themeService.IsDarkMode, Is.True);
                    Assert.That(component.Find("button[aria-label='Switch to light mode']"), Is.Not.Null);
                    Assert.That(applyDarkModeInvocations[^1].Arguments[0], Is.True);
                    Assert.That(saveThemeInvocations[^1].Arguments[0], Is.True);
                }
            });

            this.Services.GetRequiredService<NavigationManager>().NavigateTo("/workspace/dashboard");
            component.Render(parameters => parameters
                .Add(layout => layout.Body, CreateBody("Dashboard content", "Dashboard")));

            Assert.That(component.Find("button[aria-label='Switch to light mode']"), Is.Not.Null);
        }

        [Test]
        public async Task VerifyNavigationPresentationUpdatesWorkspaceShell()
        {
            using var component = this.RenderLayout(CreateBody("Editor content", "Modelling"));

            Assert.That(component.Find("section.mb-workspace-shell")
                .GetAttribute("data-navigation-collapsed"), Is.EqualTo("true"));

            await component.Find(".mb-navigation-rail__collapse-toggle").ClickAsync();
            var options = await component.FindComponent<BbPortalHost>()
                .WaitForElementsAsync("[role='menuitem']", 3);
            await options.Single(item => item.TextContent.Trim().Contains("Expanded", StringComparison.Ordinal))
                .ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                Assert.That(component.Find("section.mb-workspace-shell")
                    .GetAttribute("data-navigation-collapsed"), Is.EqualTo("false"));
            });
        }

        [Test]
        public void VerifyStyleOwnsFullViewportAndAcceptedNavigationReservation()
        {
            var style = File.ReadAllText(Path.Combine(
                TestRepository.GetRootPath(),
                "Mycelium.Bloom",
                "Components",
                "Layout",
                "WorkspaceLayout.razor.css"));
            var rootRule = style[..(style.IndexOf('}') + 1)];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rootRule, Does.Contain("height: 100dvh;"));
                Assert.That(rootRule, Does.Contain("overflow: hidden;"));
                Assert.That(rootRule, Does.Not.Contain("border-radius"));
                Assert.That(rootRule, Does.Not.Contain("margin:"));
                Assert.That(rootRule, Does.Not.Contain("padding:"));
                Assert.That(style, Does.Contain(
                    ".mb-workspace-shell:not(.mb-workspace-shell--left-panel-collapsed) .mb-workspace-shell__body"));
                Assert.That(style, Does.Contain("--workspace-shell-left-panel-width: fit-content;"));
                Assert.That(style, Does.Contain(
                    "width: calc(var(--workspace-shell-left-panel-collapsed-width) - (2 * var(--spacing-8)));"));
                Assert.That(style, Does.Not.Match("#[0-9a-fA-F]{3,8}"));
            }
        }

        [Test]
        public async Task VerifyReferenceTrailAndLocationRestorationPreserveSelectionAndPanelLifetime()
        {
            var first = new SysML2.NET.Core.POCO.Root.Namespaces.Namespace { ElementId = "A" };
            var second = new SysML2.NET.Core.POCO.Root.Namespaces.Namespace { ElementId = "B/value" };
            var third = new SysML2.NET.Core.POCO.Root.Namespaces.Namespace { ElementId = "C value" };
            this.elementIdResolver.Setup(resolver => resolver.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string id, CancellationToken _) => ValueTask.FromResult<SysML2.NET.Core.POCO.Root.Elements.IElement>(
                    id == first.ElementId ? first : id == second.ElementId ? second : id == third.ElementId ? third : null));
            var navigation = (BunitNavigationManager)this.Services.GetRequiredService<NavigationManager>();
            navigation.NavigateTo("/workspace/modeling?selectedElement=A&mode=review#target");
            using var component = this.RenderLayout(CreateBody("Editor", "Modelling"));
            var panel = component.FindComponent<DetailsPanelComponent>();
            var viewModel = panel.Instance.ViewModel;
            var urls = panel.Instance.UrlContext;
            var initialCount = navigation.History.Count;
            await component.InvokeAsync(() => urls.Inspect(new SysML2.NET.Core.POCO.Root.Namespaces.Namespace { ElementId = "A" }));
            Assert.That(navigation.History, Has.Count.EqualTo(initialCount));
            await component.InvokeAsync(() => urls.Inspect(second));
            await component.WaitForAssertionAsync(() => Assert.That(viewModel.Presentation.Element, Is.SameAs(second)));
            var secondUri = navigation.Uri;
            var secondState = navigation.History.First().Options.HistoryEntryState;
            Assert.That(navigation.History.First().Options.ReplaceHistoryEntry, Is.False);
            await component.InvokeAsync(() => urls.Inspect(third));
            await component.WaitForAssertionAsync(() => Assert.That(viewModel.Presentation.Element, Is.SameAs(third)));
            var thirdUri = navigation.Uri;
            var thirdState = navigation.History.First().Options.HistoryEntryState;
            var count = navigation.History.Count;
            await component.InvokeAsync(() => urls.Inspect(new SysML2.NET.Core.POCO.Root.Namespaces.Namespace { ElementId = third.ElementId }));
            Assert.That(navigation.History, Has.Count.EqualTo(count));

            // Reproduce the location and entry state delivered by native Back and Forward.
            await component.InvokeAsync(() => navigation.NavigateTo(secondUri, new NavigationOptions { HistoryEntryState = secondState }));
            await component.WaitForAssertionAsync(() => Assert.That(viewModel.Presentation.Element, Is.SameAs(second)));
            await component.InvokeAsync(() => navigation.NavigateTo(thirdUri, new NavigationOptions { HistoryEntryState = thirdState }));
            await component.WaitForAssertionAsync(() => Assert.That(viewModel.Presentation.Element, Is.SameAs(third)));
            await panel.Find("button[aria-label='Go Back']").ClickAsync();
            await component.WaitForAssertionAsync(() => Assert.That(viewModel.Presentation.Element, Is.SameAs(second)));
            await panel.Find("button[aria-label='Go Back']").ClickAsync();
            await component.WaitForAssertionAsync(() => Assert.That(viewModel.Presentation.Element, Is.SameAs(first)));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.context.SelectedElement, Is.SameAs(first));
                Assert.That(this.context.InspectedElement, Is.Null);
                Assert.That(panel.Instance.ViewModel, Is.SameAs(viewModel));
                Assert.That(navigation.Uri, Does.Contain("selectedElement=A&mode=review").And.EndsWith("#target"));
                Assert.That(panel.FindAll("button[aria-label='Go Back']"), Is.Empty);
                Assert.That(navigation.History.First().Options.ReplaceHistoryEntry, Is.True);
                Assert.That(navigation.History.First().Options.ForceLoad, Is.False);
            }
            await component.InvokeAsync(() => urls.Inspect(second));
            await component.InvokeAsync(() => urls.Inspect(new SysML2.NET.Core.POCO.Root.Namespaces.Namespace { ElementId = "A" }));
            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(viewModel.Presentation.Element, Is.SameAs(first));
                    Assert.That(navigation.Uri, Does.Not.Contain("inspectedElement"));
                }
            });
            await component.InvokeAsync(() => urls.Inspect(second));
            await component.InvokeAsync(() => this.context.SelectedElement = third);
            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(viewModel.Presentation.Element, Is.SameAs(third));
                    Assert.That(navigation.Uri, Does.Contain("selectedElement=C%20value").And.Not.Contain("inspectedElement"));
                }
            });
        }

        [TestCase("A", false)]
        [TestCase("A&inspectedElement=A", false)]
        [TestCase("B", true)]
        [TestCase("B&inspectedElement=B", true)]
        [TestCase("B&inspectedElement=other", false)]
        [TestCase("unknown", false)]
        [TestCase("", false)]
        [TestCase("%ZZ", false)]
        [TestCase("inaccessible", false)]
        public async Task VerifyDirectInspectionAndInvalidReferencePreserveSelection(string parameter, bool valid)
        {
            var selected = new SysML2.NET.Core.POCO.Root.Namespaces.Namespace { ElementId = "A" };
            var inspected = new SysML2.NET.Core.POCO.Root.Namespaces.Namespace { ElementId = "B" };
            this.elementIdResolver.Setup(resolver => resolver.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string id, CancellationToken _) => id == "inaccessible"
                    ? ValueTask.FromException<SysML2.NET.Core.POCO.Root.Elements.IElement>(new InvalidOperationException("Unavailable"))
                    : ValueTask.FromResult<SysML2.NET.Core.POCO.Root.Elements.IElement>(id == "A" ? selected : id == "B" ? inspected : null));
            var navigation = this.Services.GetRequiredService<NavigationManager>();
            navigation.NavigateTo("/workspace/modeling?selectedElement=A&mode=review&inspectedElement=" + parameter + "#target");
            using var component = this.RenderLayout(CreateBody("Editor", "Modelling"));
            var panel = component.FindComponent<DetailsPanelComponent>();
            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(this.context.SelectedElement, Is.SameAs(selected));
                    Assert.That(panel.Instance.ViewModel.Presentation.Element, Is.SameAs(valid ? inspected : selected));
                    Assert.That(this.context.InspectedElement, valid ? Is.SameAs(inspected) : Is.Null);
                    Assert.That(navigation.Uri, Does.Contain("selectedElement=A").And.Contain("mode=review").And.EndsWith("#target"));
                    Assert.That(QueryHelpers.ParseQuery(new Uri(navigation.Uri).Query).ContainsKey("inspectedElement"), Is.EqualTo(valid));
                }
            });
            if (!valid)
            {
                var history = ((BunitNavigationManager)navigation).History.First();
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(history.Options.ReplaceHistoryEntry, Is.True);
                    Assert.That(history.Options.ForceLoad, Is.False);
                }
            }

            if (valid)
            {
                await panel.Find("button[aria-label='Go Back']").ClickAsync();
                await component.WaitForAssertionAsync(() => Assert.That(panel.Instance.ViewModel.Presentation.Element, Is.SameAs(selected)));
            }
        }

        [Test]
        public async Task VerifyDirectLinkBackAndUriRestorationResetOnlyChangedElementTabs()
        {
            var selected = new SysML2.NET.Core.POCO.Root.Namespaces.Namespace { ElementId = "A" };
            var inspected = new SysML2.NET.Core.POCO.Root.Namespaces.Namespace { ElementId = "B" };
            this.elementIdResolver.Setup(resolver => resolver.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string id, CancellationToken _) => ValueTask.FromResult<SysML2.NET.Core.POCO.Root.Elements.IElement>(id == "A" ? selected : inspected));
            var navigation = (BunitNavigationManager)this.Services.GetRequiredService<NavigationManager>();
            navigation.NavigateTo("/outside-workspace");
            navigation.NavigateTo("/workspace/modeling?selectedElement=A&inspectedElement=B&mode=review#target");
            using var component = this.RenderLayout(CreateBody("Editor", "Modelling"));
            var panel = component.FindComponent<DetailsPanelComponent>();
            var instance = panel.Instance;
            var viewModel = instance.ViewModel;
            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(viewModel.Presentation.Element, Is.SameAs(inspected));
                    Assert.That(panel.FindAll("button[aria-label='Go Back']"), Has.Count.EqualTo(1));
                    AssertActiveTab("Properties");
                }
            });
            await ChooseTab("Comments");
            var count = navigation.History.Count;
            await panel.Find("button[aria-label='Go Back']").ClickAsync();
            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(viewModel.Presentation.Element, Is.SameAs(selected));
                    Assert.That(this.context.SelectedElement, Is.SameAs(selected));
                    Assert.That(navigation.Uri, Does.EndWith("/workspace/modeling?selectedElement=A&mode=review#target"));
                    Assert.That(navigation.History.First().Options.ReplaceHistoryEntry, Is.True);
                    Assert.That(navigation.History, Has.Count.EqualTo(count));
                    Assert.That(panel.FindAll("button[aria-label='Go Back']"), Is.Empty);
                    AssertActiveTab("Properties");
                }
            });
            Assert.That(this.JSInterop.Invocations.Any(invocation => invocation.Identifier == "history.back"), Is.False);
            await ChooseTab("Attachments");
            navigation.NavigateTo("/workspace/modeling?selectedElement=A&inspectedElement=A&mode=other#fragment");
            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(navigation.Uri, Does.Not.Contain("inspectedElement"));
                    Assert.That(panel.FindAll("button[aria-label='Go Back']"), Is.Empty);
                    AssertActiveTab("Attachments");
                }
            });

            // Restore a distinct inspection into the already-mounted panel.
            navigation.NavigateTo("/workspace/modeling?selectedElement=A&inspectedElement=B&mode=other#fragment");
            await component.WaitForAssertionAsync(() =>
            {
                Assert.That(viewModel.Presentation.Element, Is.SameAs(inspected));
                AssertActiveTab("Properties");
            });
            await ChooseTab("Relations");
            navigation.NavigateTo("/workspace/modeling?selectedElement=A&inspectedElement=B&mode=unrelated#new-fragment");
            await component.WaitForAssertionAsync(() => AssertActiveTab("Relations"));
            await component.InvokeAsync(viewModel.Refresh);
            await component.WaitForAssertionAsync(() => AssertActiveTab("Relations"));

            // Back and Forward deliver the earlier and later URI through LocationChanged.
            navigation.NavigateTo("/workspace/modeling?selectedElement=A&mode=other#fragment");
            await component.WaitForAssertionAsync(() =>
            {
                Assert.That(viewModel.Presentation.Element, Is.SameAs(selected));
                AssertActiveTab("Properties");
            });
            await ChooseTab("Comments");
            navigation.NavigateTo("/workspace/modeling?selectedElement=A&inspectedElement=B&mode=unrelated#new-fragment");
            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(viewModel.Presentation.Element, Is.SameAs(inspected));
                    AssertActiveTab("Properties");
                    Assert.That(this.context.SelectedElement, Is.SameAs(selected));
                    Assert.That(component.FindComponent<DetailsPanelComponent>().Instance, Is.SameAs(instance));
                    Assert.That(instance.ViewModel, Is.SameAs(viewModel));
                }
            });

            Task ChooseTab(string tab) => panel.FindAll("[role='tab']").Single(element => element.TextContent == tab).ClickAsync();
            void AssertActiveTab(string tab) => Assert.That(panel.Find("[role='tab'][data-state='active']").TextContent, Is.EqualTo(tab));
        }

        [Test]
        public async Task VerifyDirectTrailRootNavigationPreservesSelectionAndResetsPropertiesTab()
        {
            var a = new SysML2.NET.Core.POCO.Root.Namespaces.Namespace { ElementId = "A", DeclaredName = "A" };
            var d = new SysML2.NET.Core.POCO.Root.Namespaces.Namespace { ElementId = "D", DeclaredName = "D" };
            this.elementIdResolver.Setup(resolver => resolver.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string id, CancellationToken _) => ValueTask.FromResult<SysML2.NET.Core.POCO.Root.Elements.IElement>(id == "A" ? a : d));
            var navigation = (BunitNavigationManager)this.Services.GetRequiredService<NavigationManager>();
            var direct = "/workspace/modeling?selectedElement=A&inspectedElement=D&mode=review#target";
            navigation.NavigateTo(direct);
            using var component = this.RenderLayout(CreateBody("Editor", "Modelling"));
            var panel = component.FindComponent<DetailsPanelComponent>();
            var viewModel = panel.Instance.ViewModel;
            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(panel.Find("nav[aria-label='Inspection Trail'] [aria-current='step']").TextContent.Trim(), Is.EqualTo("D"));
                    Assert.That(panel.FindAll("nav[aria-label='Inspection Trail'] li"), Has.Count.EqualTo(2));
                    Assert.That(panel.Find("nav[aria-label='Inspection Trail']").ParentElement.TagName, Is.EqualTo("HEADER"));
                    Assert.That(panel.Find("header h2").ClassList, Does.Contain("sr-only"));
                    Assert.That(panel.Find("header").NextElementSibling.ClassList, Does.Contain("mb-properties-tabs"));
                    Assert.That(this.context.SelectedElement, Is.SameAs(a));
                }
            });
            await panel.FindAll("[role='tab']").Single(tab => tab.TextContent == "Comments").ClickAsync();
            await panel.Find("nav[aria-label='Inspection Trail'] button[title='A']").ClickAsync();
            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(panel.FindAll("nav[aria-label='Inspection Trail']"), Is.Empty);
                    Assert.That(panel.Find("[role='tab'][data-state='active']").TextContent, Is.EqualTo("Properties"));
                    Assert.That(navigation.Uri, Does.Not.Contain("inspectedElement").And.Contain("mode=review").And.EndsWith("#target"));
                    Assert.That(navigation.History.First().Options.ReplaceHistoryEntry, Is.False);
                    Assert.That(this.context.SelectedElement, Is.SameAs(a));
                    Assert.That(viewModel.Presentation.Element, Is.SameAs(a));
                    Assert.That(panel.Instance.ViewModel, Is.SameAs(viewModel));
                }
            });
            navigation.NavigateTo(direct);
            await component.WaitForAssertionAsync(() => Assert.That(panel.FindAll("nav[aria-label='Inspection Trail'] li"), Has.Count.EqualTo(2)));
            await component.InvokeAsync(() => this.context.SelectedElement = d);
            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(panel.FindAll("nav[aria-label='Inspection Trail']"), Is.Empty);
                    Assert.That(viewModel.Presentation.Element, Is.SameAs(d));
                    Assert.That(navigation.Uri, Does.Contain("selectedElement=D").And.Not.Contain("inspectedElement"));
                }
            });
        }

        private IRenderedComponent<WorkspaceLayout> RenderLayout(RenderFragment body)
        {
            return this.Render<WorkspaceLayout>(parameters => parameters.Add(layout => layout.Body, body));
        }

        private static RenderFragment CreateBody(string content, string heading)
        {
            return builder =>
            {
                builder.OpenElement(0, "section");
                builder.AddAttribute(1, "data-testid", "layout-body");
                builder.OpenElement(2, "h1");
                builder.AddContent(3, heading);
                builder.CloseElement();
                builder.AddContent(4, content);
                builder.CloseElement();
            };
        }
    }
}
