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
    using System.Threading.Tasks;

    using BlazorBlueprint.Components;
    using BlazorBlueprint.Primitives.Services;

    using Bunit;

    using Microsoft.AspNetCore.Components;
    using Microsoft.Extensions.DependencyInjection;

    using Mycelium.Bloom.Components.Layout;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.NavigationRail;

    using AppHeaderComponent = Mycelium.Bloom.Components.UI.Organisms.AppHeader.AppHeader;
    using DetailsPanelComponent = Mycelium.Bloom.Components.UI.Organisms.DetailsPanel.DetailsPanel;
    using NavigationRailComponent = Mycelium.Bloom.Components.UI.Organisms.NavigationRail.NavigationRail;
    using StatusBarComponent = Mycelium.Bloom.Components.UI.Organisms.StatusBar.StatusBar;
    using WorkspaceShellComponent = Mycelium.Bloom.Components.UI.Organisms.WorkspaceShell.WorkspaceShell;

    /// <summary>
    /// Tests the <see cref="WorkspaceLayout" /> routed application frame.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class WorkspaceLayoutTestFixture : BunitContext
    {
        private readonly BunitJSModuleInterop themeModule;

        private readonly ContextAwareService context;

        private int navigationViewModelCreationCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkspaceLayoutTestFixture" /> class.
        /// </summary>
        public WorkspaceLayoutTestFixture()
        {
            BlueprintTestSetup.Configure(this);

            this.context = new ContextAwareService();
            this.Services.AddSingleton<IContextAwareService>(this.context);
            this.Services.AddSingleton<IElementSelectionService>(this.context);
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

        /// <summary>
        /// Disposes the bUnit context after each test.
        /// </summary>
        [TearDown]
        public Task TearDown()
        {
            return this.DisposeAsync().AsTask();
        }

        /// <summary>
        /// Verifies the routed layout owns exactly one shared frame and inserts its body into the main region.
        /// </summary>
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
                Assert.That(component.Find("button[aria-label='Close details panel'][aria-pressed='true']"),
                    Is.Not.Null);
                Assert.That(component.FindAll("button[aria-label='Close details panel']"), Has.Count.EqualTo(2));
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

        /// <summary>
        /// Verifies layout-owned inspector visibility can be closed and reopened without duplicating its region.
        /// </summary>
        [Test]
        public async Task VerifyDetailsPanelCanCloseAndReopen()
        {
            using var component = this.RenderLayout(CreateBody("Editor content", "Modelling"));

            await component.Find(".mb-details-panel button[aria-label='Close details panel']").ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.FindComponents<DetailsPanelComponent>(), Is.Empty);
                    Assert.That(component.FindAll("aside.mb-workspace-shell__right-panel"), Is.Empty);
                    Assert.That(component.FindAll("button[aria-label='Close details panel']"), Is.Empty);
                    Assert.That(component.Find("button[aria-label='Open details panel']")
                        .GetAttribute("aria-pressed"), Is.EqualTo("false"));
                }
            });

            await component.Find("button[aria-label='Open details panel']").ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.FindComponents<DetailsPanelComponent>(), Has.Count.EqualTo(1));
                    Assert.That(component.FindAll("aside.mb-workspace-shell__right-panel"), Has.Count.EqualTo(1));
                    Assert.That(component.Find("button[aria-label='Close details panel'][aria-pressed='true']"),
                        Is.Not.Null);
                    Assert.That(component.FindAll(".mb-details-panel button[aria-label='Close details panel']"),
                        Has.Count.EqualTo(1));
                }
            });
        }

        /// <summary>
        /// Verifies route reconciliation ignores query and fragment values and preserves the layout-owned rail mode.
        /// </summary>
        [Test]
        public async Task VerifyRouteChangesReconcileSelectionWithoutDuplicatingTheFrame()
        {
            using var component = this.RenderLayout(CreateBody("Editor content", "Modelling"));
            var navigationRail = component.FindComponent<NavigationRailComponent>();
            var viewModel = navigationRail.Instance.ViewModel;
            viewModel.PresentationMode = NavigationRailPresentationMode.Expanded;
            var layoutInstance = component.Instance;

            await component.Find(".mb-details-panel button[aria-label='Close details panel']").ClickAsync();

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
                Assert.That(component.FindComponents<DetailsPanelComponent>(), Is.Empty);
                Assert.That(component.FindAll("aside.mb-workspace-shell__right-panel"), Is.Empty);
                Assert.That(component.Find("button[aria-label='Open details panel']")
                    .GetAttribute("aria-pressed"), Is.EqualTo("false"));
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

        /// <summary>
        /// Verifies layout disposal releases its navigation subscription idempotently.
        /// </summary>
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
            this.context.LifecycleState = ProjectLifecycleState.Open;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(notificationCount, Is.Zero);
                Assert.That(this.navigationViewModelCreationCount, Is.EqualTo(1));
                Assert.DoesNotThrow(layout.Dispose);
            }

            viewModel.PropertyChanged -= handler;
        }

        /// <summary>
        /// Verifies the shared Blueprint control applies and retains the scoped application theme state.
        /// </summary>
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

        /// <summary>
        /// Verifies the rail's persistent presentation controls the shell-owned width reservation.
        /// </summary>
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

        /// <summary>
        /// Verifies the route layout provides an unpadded, overflow-bounded application viewport.
        /// </summary>
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
                Assert.That(style, Does.Contain("--mb-workspace-left-panel-width: fit-content;"));
                Assert.That(style, Does.Contain(
                    "width: calc(var(--mb-workspace-left-panel-collapsed-width) - (2 * var(--mb-spacing-2)));"));
                Assert.That(style, Does.Not.Match("#[0-9a-fA-F]{3,8}"));
            }
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
