// ------------------------------------------------------------------------------------------------
// <copyright file="NavigationRailTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.NavigationRail
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Threading.Tasks;

    using BlazorBlueprint.Components;
    using BlazorBlueprint.Icons.Lucide.Components;

    using Bunit;

    using Microsoft.AspNetCore.Components.Web;

    using Moq;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.NavigationRail;

    using NavigationRailComponent = Mycelium.Bloom.Components.UI.Organisms.NavigationRail.NavigationRail;

    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class NavigationRailTestFixture : BunitContext
    {
        private static readonly NavigationRailItem[] Items =
        [
            new NavigationRailItem { Id = "overview", Label = "Overview", IconName = "layout-dashboard" },
            new NavigationRailItem
            {
                Id = "traceability",
                Label = "Traceability",
                IconName = "waypoints",
                StartsNewSection = true
            },
            new NavigationRailItem { Id = "compare", Label = "Compare", IconName = "git-compare-arrows" },
            new NavigationRailItem
            {
                Id = "review",
                Label = "Review",
                IconName = "messages-square",
                StartsNewSection = true
            }
        ];

        private static readonly NavigationRailItem[] ReplacementItems =
        [
            new NavigationRailItem { Id = "activity", Label = "Activity", IconName = "history" },
            new NavigationRailItem
            {
                Id = "settings",
                Label = "Settings",
                IconName = "settings",
                StartsNewSection = true
            }
        ];

        private static readonly NavigationRailItem[] ReorderedItems =
        [
            new NavigationRailItem { Id = "review", Label = "Review queue", IconName = "messages-square" },
            new NavigationRailItem { Id = "activity", Label = "Activity", IconName = "history" },
            new NavigationRailItem
            {
                Id = "overview",
                Label = "Overview",
                IconName = "layout-dashboard",
                StartsNewSection = true
            }
        ];

        private static readonly NavigationRailItem[] OverviewOnly = [Items[0]];

        private static readonly NavigationRailItem[] ReviewOnly = [Items[3]];

        private static readonly NavigationRailItem[] ActivityOnly = [ReplacementItems[0]];

        private static readonly NavigationRailItem[] SettingsOnly = [ReplacementItems[1]];

        private static readonly string[] ExpectedLabels = ["Overview", "Traceability", "Compare", "Review"];

        private static readonly string[] ExpectedReplacementLabels = ["Activity", "Settings"];

        private static readonly string[] ExpectedReorderedLabels = ["Review queue", "Activity", "Overview"];

        private static readonly string[] ExpectedReviewOnlyLabels = ["Review"];

        private static readonly string[] ExpectedSettingsOnlyLabels = ["Settings"];

        public NavigationRailTestFixture()
        {
            BlueprintTestSetup.Configure(this);
        }

        [TearDown]
        public Task TearDown()
        {
            return this.DisposeAsync().AsTask();
        }

        [Test]
        public void VerifyRenderExposesSemanticDestinationsAndCustomAttributes()
        {
            using var viewModel = CreateViewModel(NavigationRailPresentationMode.Expanded);
            using var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel)
                .Add(rail => rail.AriaLabel, "Engineering navigation")
                .Add(rail => rail.Class, "custom-rail")
                .AddUnmatched("data-testid", "application-navigation")
                .AddUnmatched("data-collapsed", "caller-value"));
            var root = component.Find("nav");
            var links = component.FindAll(".mb-navigation-rail__link");
            var icons = component.FindAll(".mb-navigation-rail__icon");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root.GetAttribute("aria-label"), Is.EqualTo("Engineering navigation"));
                Assert.That(root.GetAttribute("data-testid"), Is.EqualTo("application-navigation"));
                Assert.That(root.GetAttribute("data-collapsed"), Is.EqualTo("false"));
                Assert.That(root.ClassList, Does.Contain("mb-navigation-rail"));
                Assert.That(root.ClassList, Does.Contain("custom-rail"));
                Assert.That(component.FindAll("li.mb-navigation-rail__item"), Has.Count.EqualTo(Items.Length));
                Assert.That(links.Select(link => link.GetAttribute("aria-label")), Is.EqualTo(ExpectedLabels));
                Assert.That(links.All(link => link.GetAttribute("type") == "button"), Is.True);
                Assert.That(links.All(link => link.GetAttribute("title") is null), Is.True);
                Assert.That(links.Single(link => link.GetAttribute("aria-label") == "Overview")
                    .GetAttribute("aria-current"), Is.EqualTo("page"));
                Assert.That(links.Single(link => link.GetAttribute("aria-label") == "Compare")
                    .GetAttribute("aria-current"), Is.Null);
                Assert.That(icons, Has.Count.EqualTo(Items.Length));
                Assert.That(icons.All(icon => icon.GetAttribute("aria-hidden") == "true"), Is.True);
                Assert.That(component.FindAll(".mb-navigation-rail__label"), Has.Count.EqualTo(Items.Length));
                Assert.That(component.FindAll(".mb-navigation-rail__collapse-toggle"), Has.Count.EqualTo(1));
                Assert.That(component.Markup, Does.Not.Contain(">Workspace<").IgnoreCase);
            }
        }

        [Test]
        public void VerifyGroupedNavigationRendersDecorativeSectionDividers()
        {
            using var viewModel = CreateViewModel();
            using var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel));
            var dividers = component.FindAll(".mb-navigation-rail__divider");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dividers, Has.Count.EqualTo(2));
                Assert.That(dividers.All(divider => divider.GetAttribute("aria-hidden") == "true"), Is.True);
                Assert.That(dividers.All(divider => divider.GetAttribute("role") is null), Is.True);
                Assert.That(dividers.All(divider => divider.GetAttribute("aria-orientation") is null), Is.True);
            }
        }

        [Test]
        public async Task VerifySelectionUpdatesReactiveStateAndRendering()
        {
            using var viewModel = CreateViewModel(NavigationRailPresentationMode.Expanded);
            using var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel));

            await component.Find("button[aria-label='Review']").ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(viewModel.SelectedItemId, Is.EqualTo("review"));
                    Assert.That(component.Find("button[aria-label='Overview']").GetAttribute("aria-current"), Is.Null);
                    Assert.That(component.Find("button[aria-label='Review']").GetAttribute("aria-current"),
                        Is.EqualTo("page"));
                }
            });
        }

        [Test]
        public async Task VerifyInteractionsDelegateToViewModelContract()
        {
            var viewModel = CreateViewModelMock();
            viewModel.Setup(x => x.SelectItem("review"));
            viewModel.Setup(x => x.TogglePresentation());
            viewModel.Setup(x => x.HandlePointerEntered());
            viewModel.Setup(x => x.HandlePointerExited());
            viewModel.Setup(x => x.SetPresentationMode(NavigationRailPresentationMode.Expanded));
            using var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel.Object));

            await component.Find("button[aria-label='Review']").ClickAsync();
            await component.Find(".mb-navigation-rail__collapse-toggle").ClickAsync();
            await component.Find("nav").TriggerEventAsync("onmouseenter", new MouseEventArgs());
            await component.Find("nav").TriggerEventAsync("onmouseleave", new MouseEventArgs());
            await OpenSidebarControlMenuAsync(component);
            var options = await component.WaitForElementsAsync("[role='menuitem']", 3);
            await options.Single(option => option.TextContent.Trim() == "Expanded").ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                viewModel.Verify(x => x.SelectItem("review"), Times.Once);
                viewModel.Verify(x => x.TogglePresentation(), Times.Once);
                viewModel.Verify(x => x.HandlePointerEntered(), Times.Once);
                viewModel.Verify(x => x.HandlePointerExited(), Times.Once);
                viewModel.Verify(
                    x => x.SetPresentationMode(NavigationRailPresentationMode.Expanded),
                    Times.Once);
            }
        }

        [Test]
        public void VerifyContextChangesRerenderAndReconcileSelection()
        {
            using var contexts = new BehaviorSubject<NavigationRailContext>(CreateContext(
                ProjectLifecycleState.Preparation));
            using var viewModel = new NavigationRailViewModel(
                contexts,
                context => SelectItemsByLifecycleState(context, Items, ReplacementItems));
            viewModel.SelectItem("review");
            viewModel.SetPresentationMode(NavigationRailPresentationMode.Expanded);
            using var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel));

            contexts.OnNext(CreateContext(ProjectLifecycleState.Open));

            component.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.FindAll(".mb-navigation-rail__link")
                        .Select(link => link.GetAttribute("aria-label")),
                        Is.EqualTo(ExpectedReplacementLabels));
                    Assert.That(component.FindAll(".mb-navigation-rail__divider"), Has.Count.EqualTo(1));
                    Assert.That(component.Find("button[aria-label='Activity']").GetAttribute("aria-current"),
                        Is.EqualTo("page"));
                    Assert.That(viewModel.SelectedItemId, Is.EqualTo("activity"));
                }
            });
        }

        [Test]
        public async Task VerifySidebarControlMenuOwnsPresentationChoices()
        {
            using var viewModel = CreateViewModel(NavigationRailPresentationMode.ExpandOnHover);
            using var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel));
            var trigger = component.Find(".mb-navigation-rail__collapse-toggle");

            await OpenSidebarControlMenuAsync(component);

            var menu = await component.WaitForElementAsync("[role='menu']");
            var options = await component.WaitForElementsAsync("[role='menuitem']", 3);
            var selectedOption = options.Single(option => option.TextContent.Contains("Expand on hover"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(trigger.GetAttribute("aria-label"),
                    Is.EqualTo("Expand workspace navigation; right-click for sidebar controls"));
                Assert.That(trigger.GetAttribute("aria-keyshortcuts"), Is.EqualTo("Shift+F10"));
                Assert.That(menu.TextContent, Does.Contain("Sidebar control"));
                Assert.That(options.Any(option => option.TextContent.Contains("Expanded")), Is.True);
                Assert.That(options.Any(option => option.TextContent.Contains("Collapsed")), Is.True);
                Assert.That(options.Any(option => option.TextContent.Contains("Expand on hover")), Is.True);
                Assert.That(component.FindAll("[role='separator']"), Has.Count.EqualTo(2));
                Assert.That(options.All(option => option.GetAttribute("aria-disabled") != "true"), Is.True);
                Assert.That(selectedOption.ClassList,
                    Does.Contain("mb-navigation-rail__control-option--selected"));
                Assert.That(selectedOption.TextContent, Does.Contain("Current selection"));
                Assert.That(component.FindComponents<BbContextMenu>(), Has.Count.EqualTo(1));
                Assert.That(component.FindComponents<BbContextMenuContent>(), Has.Count.EqualTo(1));
            }

            await options.Single(option => option.TextContent.Trim() == "Expanded").ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(viewModel.PresentationMode, Is.EqualTo(NavigationRailPresentationMode.Expanded));
                    Assert.That(component.Find("nav").GetAttribute("data-collapsed"), Is.EqualTo("false"));
                }
            });
        }

        [Test]
        public async Task VerifySidebarControlPrimaryClickTogglesFixedModes()
        {
            using var viewModel = CreateViewModel(NavigationRailPresentationMode.Expanded);
            using var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel));
            var sidebarControlIcon = component.FindComponent<BbContextMenuTrigger>()
                .FindComponent<LucideIcon>();

            Assert.That(sidebarControlIcon.Instance.Name, Is.EqualTo("panel-left-close"));

            await component.Find(".mb-navigation-rail__collapse-toggle").ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(viewModel.PresentationMode, Is.EqualTo(NavigationRailPresentationMode.Collapsed));
                    Assert.That(component.Find("nav").GetAttribute("data-collapsed"), Is.EqualTo("true"));
                    Assert.That(component.FindAll("[role='menu']"), Is.Empty);
                }
            });

            sidebarControlIcon = component.FindComponent<BbContextMenuTrigger>()
                .FindComponent<LucideIcon>();

            Assert.That(sidebarControlIcon.Instance.Name, Is.EqualTo("panel-left-open"));

            await component.Find(".mb-navigation-rail__collapse-toggle").ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(viewModel.PresentationMode, Is.EqualTo(NavigationRailPresentationMode.Expanded));
                    Assert.That(component.Find("nav").GetAttribute("data-collapsed"), Is.EqualTo("false"));
                }
            });
        }

        [Test]
        public async Task VerifyHoverModeReactsWithoutCallerRoundTrips()
        {
            using var viewModel = CreateViewModel(NavigationRailPresentationMode.ExpandOnHover);
            using var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel));

            Assert.That(component.Find("nav").GetAttribute("data-collapsed"), Is.EqualTo("true"));

            await component.Find("nav").TriggerEventAsync("onmouseenter", new MouseEventArgs());

            await component.WaitForAssertionAsync(() =>
                Assert.That(component.Find("nav").GetAttribute("data-collapsed"), Is.EqualTo("false")));

            await component.Find("nav").TriggerEventAsync("onmouseleave", new MouseEventArgs());

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.Find("nav").GetAttribute("data-collapsed"), Is.EqualTo("true"));
                    Assert.That(viewModel.PresentationMode, Is.EqualTo(NavigationRailPresentationMode.ExpandOnHover));
                }
            });
        }

        [Test]
        public void VerifyCollapsedPresentationRetainsIconFirstAccessibility()
        {
            using var viewModel = CreateViewModel();
            using var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel));
            var root = component.Find("nav");
            var links = component.FindAll(".mb-navigation-rail__link");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root.GetAttribute("data-collapsed"), Is.EqualTo("true"));
                Assert.That(root.ClassList, Does.Contain("mb-navigation-rail--collapsed"));
                Assert.That(links.Select(link => link.GetAttribute("aria-label")), Is.EqualTo(ExpectedLabels));
                Assert.That(links.Select(link => link.GetAttribute("title")), Is.EqualTo(ExpectedLabels));
                Assert.That(component.FindAll(".mb-navigation-rail__label"), Has.Count.EqualTo(Items.Length));
                Assert.That(component.FindAll("[role='tooltip']"), Is.Empty);
            }
        }

        [Test]
        public void VerifyDynamicReorderingPreservesStableDestinationIdentity()
        {
            using var contexts = new BehaviorSubject<NavigationRailContext>(CreateContext(
                ProjectLifecycleState.Preparation));
            using var viewModel = new NavigationRailViewModel(
                contexts,
                context => SelectItemsByLifecycleState(context, Items, ReorderedItems));
            viewModel.SetPresentationMode(NavigationRailPresentationMode.Expanded);
            using var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel));
            var originalOverviewButton = component.FindComponents<BbButton>()
                .Single(button => button.Instance.AriaLabel == "Overview")
                .Instance;

            contexts.OnNext(CreateContext(ProjectLifecycleState.Review));

            component.WaitForAssertion(() =>
            {
                var updatedOverviewButton = component.FindComponents<BbButton>()
                    .Single(button => button.Instance.AriaLabel == "Overview")
                    .Instance;

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.FindAll(".mb-navigation-rail__link")
                        .Select(link => link.GetAttribute("aria-label")), Is.EqualTo(ExpectedReorderedLabels));
                    Assert.That(updatedOverviewButton, Is.SameAs(originalOverviewButton));
                    Assert.That(viewModel.SelectedItemId, Is.EqualTo("overview"));
                }
            });
        }

        [Test]
        public void VerifyReplacingViewModelDetachesOldRenderingSubscription()
        {
            using var firstContexts = new BehaviorSubject<NavigationRailContext>(CreateContext(
                ProjectLifecycleState.Preparation));
            using var secondContexts = new BehaviorSubject<NavigationRailContext>(CreateContext(
                ProjectLifecycleState.Preparation));
            using var firstViewModel = new NavigationRailViewModel(
                firstContexts,
                context => SelectItemsByLifecycleState(context, OverviewOnly, ActivityOnly));
            using var secondViewModel = new NavigationRailViewModel(
                secondContexts,
                context => SelectItemsByLifecycleState(context, ReviewOnly, SettingsOnly));
            using var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, firstViewModel));

            component.Render(parameters => parameters.Add(rail => rail.ViewModel, secondViewModel));
            var renderCountAfterReplacement = component.RenderCount;

            firstContexts.OnNext(CreateContext(ProjectLifecycleState.Open));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.RenderCount, Is.EqualTo(renderCountAfterReplacement));
                Assert.That(component.FindAll(".mb-navigation-rail__link")
                    .Select(link => link.GetAttribute("aria-label")), Is.EqualTo(ExpectedReviewOnlyLabels));
            }

            secondContexts.OnNext(CreateContext(ProjectLifecycleState.Open));

            component.WaitForAssertion(() =>
                Assert.That(component.FindAll(".mb-navigation-rail__link")
                    .Select(link => link.GetAttribute("aria-label")), Is.EqualTo(ExpectedSettingsOnlyLabels)));
        }

        [Test]
        public async Task VerifyIndependentInstancesDoNotShareState()
        {
            using var firstViewModel = CreateViewModel(NavigationRailPresentationMode.ExpandOnHover);
            using var secondViewModel = CreateViewModel(NavigationRailPresentationMode.Collapsed);
            using var first = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, firstViewModel));
            using var second = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, secondViewModel));

            await first.Find("nav").TriggerEventAsync("onmouseenter", new MouseEventArgs());
            await first.Find("button[aria-label='Compare']").ClickAsync();

            await first.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(firstViewModel.NavigationItems, Is.Not.SameAs(secondViewModel.NavigationItems));
                    Assert.That(firstViewModel.SelectedItemId, Is.EqualTo("compare"));
                    Assert.That(secondViewModel.SelectedItemId, Is.EqualTo("overview"));
                    Assert.That(first.Find("nav").GetAttribute("data-collapsed"), Is.EqualTo("false"));
                    Assert.That(second.Find("nav").GetAttribute("data-collapsed"), Is.EqualTo("true"));
                }
            });
        }

        [Test]
        public void VerifyComponentDisposalDoesNotDisposeSuppliedViewModel()
        {
            using var contexts = new BehaviorSubject<NavigationRailContext>(CreateContext(
                ProjectLifecycleState.Preparation));
            using var viewModel = new NavigationRailViewModel(
                contexts,
                context => SelectItemsByLifecycleState(context, Items, ReplacementItems));
            var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel));

            component.Dispose();

            Assert.DoesNotThrow(() => contexts.OnNext(CreateContext(ProjectLifecycleState.Open)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.NavigationItems, Is.EqualTo(ReplacementItems));
                Assert.That(viewModel.SelectedItemId, Is.EqualTo("activity"));
            }
        }

        [Test]
        public void VerifyComponentDisposalDoesNotDisposeViewModelContract()
        {
            var viewModel = CreateViewModelMock();
            var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel.Object));

            component.Dispose();

            viewModel.Verify(x => x.Dispose(), Times.Never);
        }

        [Test]
        public void VerifyNavigationRailStyleContracts()
        {
            var repositoryRoot = TestRepository.GetRootPath();
            var componentDirectory = Path.Combine(
                repositoryRoot,
                "Mycelium.Bloom",
                "Components",
                "UI",
                "Organisms",
                "NavigationRail");
            var style = File.ReadAllText(Path.Combine(componentDirectory, "NavigationRail.razor.css"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(style, Does.Contain("--mb-navigation-rail-target-size: var(--mb-control-height-md);"));
                Assert.That(style, Does.Contain("--mb-navigation-rail-active-size: 32px;"));
                Assert.That(style, Does.Contain("--mb-navigation-rail-icon-size: 16px;"));
                Assert.That(style, Does.Contain("--mb-navigation-rail-divider-height: 22px;"));
                Assert.That(style, Does.Contain("--mb-navigation-rail-divider-width: var(--mb-spacing-6);"));
                Assert.That(style, Does.Contain("padding: 16px 6px;"));
                Assert.That(style, Does.Contain("border-right: 1px solid var(--mb-color-border-subtle);"));
                Assert.That(style, Does.Contain("background: var(--mb-color-action-primary-soft);"));
                Assert.That(style, Does.Contain("overflow-y: auto;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail\s*\{[^}]*width:\s*fit-content;[^}]*max-width:\s*100%;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__items\s*\{[^}]*align-self:\s*flex-start;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__link\s*\{[^}]*gap:\s*var\(--mb-spacing-3\);[^}]*padding-inline-start:\s*calc\(\s*\(var\(--mb-navigation-rail-target-size\)\s*-\s*var\(--mb-navigation-rail-icon-size\)\)\s*/\s*2\s*\);"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__link:hover\s*\{[^}]*background:\s*var\(--mb-color-surface-hover\);"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__link:focus-visible\s*\{[^}]*background:\s*var\(--mb-color-surface-hover\);[^}]*box-shadow:\s*none;"));
                Assert.That(style, Does.Not.Contain(".mb-navigation-rail__link:hover::before"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__link::before\s*\{[^}]*inset-inline-start:\s*calc\(\s*\(var\(--mb-navigation-rail-target-size\)\s*-\s*var\(--mb-navigation-rail-active-size\)\)\s*/\s*2\s*\);"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail:not\(\.mb-navigation-rail--collapsed\).*?\.mb-navigation-rail__link--active:focus-visible\s*\{[^}]*background:\s*var\(--mb-color-action-primary-soft\);"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__sidebar-control\s*\{[^}]*justify-content:\s*center;[^}]*width:\s*var\(--mb-navigation-rail-target-size\);"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__collapse-toggle\s*\{[^}]*display:\s*inline-flex;[^}]*align-items:\s*center;[^}]*justify-content:\s*center;[^}]*margin:\s*0;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__collapse-toggle:hover\s*\{[^}]*background:\s*transparent;[^}]*color:\s*var\(--mb-color-action-primary-hover\);"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__collapse-toggle:focus-visible\s*\{[^}]*background:\s*transparent;[^}]*outline:\s*2px\s+solid\s+var\(--mb-color-focus-ring\);[^}]*box-shadow:\s*none;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__control-menu-content\s*\{[^}]*min-width:\s*13rem;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__control-menu-content\s+::deep\s+\.mb-navigation-rail__control-option:not\(\[aria-disabled=""true""\]\)\s*\{[^}]*cursor:\s*pointer;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__control-option:not\(\[aria-disabled=""true""\]\):hover\s*\{[^}]*background:\s*var\(--mb-color-surface-hover\);[^}]*color:\s*var\(--mb-color-text-primary\);"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__control-option:focus\s*\{[^}]*background:\s*transparent;[^}]*outline:\s*2px\s+solid\s+var\(--mb-color-focus-ring\);"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__control-option--selected:focus\s*\{[^}]*background:\s*var\(--mb-color-action-primary-soft\);[^}]*color:\s*var\(--mb-color-action-primary\);[^}]*font-weight:\s*var\(--mb-font-weight-label-xs\);"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__divider::before\s*\{[^}]*inset-inline-start:\s*var\(--mb-spacing-2\);[^}]*width:\s*max\(\s*var\(--mb-navigation-rail-divider-width\),\s*calc\(100%\s*-\s*\(2\s*\*\s*var\(--mb-spacing-2\)\)\)\s*\);"));
                Assert.That(style, Does.Contain("@media (prefers-reduced-motion: reduce)"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail--collapsed\s+\.mb-navigation-rail__label\s*\{[^}]*display:\s*none;"));
                Assert.That(style, Does.Contain("@supports (interpolate-size: allow-keywords)"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail\s*\{[^}]*interpolate-size:\s*allow-keywords;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__link\s*\{[^}]*gap\s+var\(--mb-transition-fast\);"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__label\s*\{[^}]*display:\s*block;[^}]*width:\s*max-content;[^}]*opacity:\s*1;[^}]*width\s+var\(--mb-transition-fast\),[^}]*opacity\s+var\(--mb-transition-fast\);"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail--collapsed\s+::deep\s+\.mb-navigation-rail__link\s*\{[^}]*gap:\s*0;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail--collapsed\s+\.mb-navigation-rail__label\s*\{[^}]*display:\s*block;[^}]*width:\s*0;[^}]*opacity:\s*0;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)@media\s*\(prefers-reduced-motion:\s*reduce\).*?\.mb-navigation-rail__label\s*\{[^}]*transition:\s*none;"));
                Assert.That(style, Does.Not.Contain("width: 52px;"));
            }
        }

        private static Mock<INavigationRailViewModel> CreateViewModelMock()
        {
            var navigationItems = new ReadOnlyObservableCollection<NavigationRailItem>(
                new ObservableCollection<NavigationRailItem>(Items));
            var viewModel = new Mock<INavigationRailViewModel>(MockBehavior.Strict);
            viewModel.SetupGet(x => x.NavigationItems).Returns(navigationItems);
            viewModel.SetupGet(x => x.SelectedItemId).Returns("overview");
            viewModel.SetupGet(x => x.PresentationMode).Returns(NavigationRailPresentationMode.Collapsed);
            viewModel.SetupGet(x => x.IsCollapsed).Returns(true);
            viewModel.Setup(x => x.Dispose());

            return viewModel;
        }

        private static NavigationRailViewModel CreateViewModel(
            NavigationRailPresentationMode mode = NavigationRailPresentationMode.Collapsed,
            string selectedItemId = "overview")
        {
            var viewModel = new NavigationRailViewModel(
                Observable.Return(CreateContext(ProjectLifecycleState.Preparation)),
                _ => Items);
            viewModel.SelectItem(selectedItemId);
            viewModel.SetPresentationMode(mode);

            return viewModel;
        }

        private static NavigationRailContext CreateContext(ProjectLifecycleState lifecycleState)
        {
            return new NavigationRailContext { LifecycleState = lifecycleState };
        }

        private static IReadOnlyList<NavigationRailItem> SelectItemsByLifecycleState(
            NavigationRailContext context,
            IReadOnlyList<NavigationRailItem> preparationItems,
            IReadOnlyList<NavigationRailItem> nonPreparationItems)
        {
            return context.LifecycleState switch
            {
                ProjectLifecycleState.Preparation => preparationItems,
                ProjectLifecycleState.Open => nonPreparationItems,
                ProjectLifecycleState.Review => nonPreparationItems,
                ProjectLifecycleState.Archived => nonPreparationItems,
                _ => throw new ArgumentOutOfRangeException(nameof(context), context.LifecycleState, null)
            };
        }

        private static Task OpenSidebarControlMenuAsync(
            IRenderedComponent<NavigationRailComponent> component)
        {
            return component.Find(".mb-navigation-rail__context-trigger")
                .TriggerEventAsync("oncontextmenu", new MouseEventArgs { Button = 2 });
        }
    }
}
