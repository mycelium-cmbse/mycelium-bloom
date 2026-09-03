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
    using System.Threading.Tasks;

    using BlazorBlueprint.Components;
    using BlazorBlueprint.Icons.Lucide.Components;
    using BlazorBlueprint.Primitives;
    using BlazorBlueprint.Primitives.Services;

    using Bunit;

    using Microsoft.AspNetCore.Components.Web;

    using Moq;

    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.NavigationRail;

    using SysML2.NET.Core.POCO.Root.Elements;

    using NavigationRailComponent = Mycelium.Bloom.Components.UI.Organisms.NavigationRail.NavigationRail;

    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class NavigationRailTestFixture : BunitContext
    {
        private static readonly NavigationRailItem[] Items =
        [
            new NavigationRailItem
            {
                Id = "overview",
                Label = "Overview",
                IconName = "layout-dashboard",
                Href = "/",
                GroupKey = "model"
            },
            new NavigationRailItem
            {
                Id = "traceability",
                Label = "Traceability",
                IconName = "waypoints",
                Href = "/traceability",
                GroupKey = "analysis",
                GroupLabel = "ANALYSIS"
            },
            new NavigationRailItem
            {
                Id = "compare",
                Label = "Compare",
                IconName = "git-compare-arrows",
                GroupKey = "analysis"
            },
            new NavigationRailItem
            {
                Id = "review",
                Label = "Review",
                IconName = "messages-square",
                Href = "/review",
                GroupKey = "collaboration",
                GroupLabel = "COLLABORATION"
            }
        ];

        private static readonly NavigationRailItem[] ReplacementItems =
        [
            new NavigationRailItem
            {
                Id = "activity",
                Label = "Activity",
                IconName = "history",
                Href = "/activity",
                GroupKey = "activity"
            },
            new NavigationRailItem
            {
                Id = "settings",
                Label = "Settings",
                IconName = "settings",
                Href = "/settings",
                GroupKey = "settings"
            }
        ];

        private static readonly NavigationRailItem[] ReorderedItems =
        [
            new NavigationRailItem
            {
                Id = "review",
                Label = "Review queue",
                IconName = "messages-square",
                Href = "/review",
                GroupKey = "collaboration"
            },
            new NavigationRailItem
            {
                Id = "activity",
                Label = "Activity",
                IconName = "history",
                Href = "/activity",
                GroupKey = "activity"
            },
            new NavigationRailItem
            {
                Id = "overview",
                Label = "Overview",
                IconName = "layout-dashboard",
                Href = "/",
                GroupKey = "model"
            }
        ];

        private static readonly NavigationRailItem[] OverviewOnly = [Items[0]];

        private static readonly NavigationRailItem[] ReviewOnly = [Items[3]];

        private static readonly NavigationRailItem[] ActivityOnly = [ReplacementItems[0]];

        private static readonly NavigationRailItem[] SettingsOnly = [ReplacementItems[1]];

        private static readonly string[] ExpectedLabels = ["Overview", "Traceability", "Compare", "Review"];

        private static readonly string[] ExpectedGroupHeadingLabels = ["ANALYSIS", "COLLABORATION"];

        private static readonly string[] ExpectedReplacementLabels = ["Activity", "Settings"];

        private static readonly string[] ExpectedReorderedLabels = ["Review queue", "Activity", "Overview"];

        private static readonly string[] ExpectedReviewOnlyLabels = ["Review"];

        private static readonly string[] ExpectedSettingsOnlyLabels = ["Settings"];

        private static readonly string[] ExpectedPresentationModeLabels =
            ["Expanded", "Collapsed", "Expand on hover"];

        private static readonly bool[] InitialCollapsedLayoutStates = [true];

        private static readonly bool[] ExpandedLayoutStates = [true, false];

        private static readonly bool[] CollapsedAgainLayoutStates = [true, false, true];

        private readonly IRenderedComponent<BbPortalHost> portalHost;

        public NavigationRailTestFixture()
        {
            this.portalHost = BlueprintTestSetup.ConfigureWithPortalHost(this);
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
                Assert.That(links.Single(link => link.GetAttribute("aria-label") == "Overview").LocalName,
                    Is.EqualTo("a"));
                Assert.That(links.Single(link => link.GetAttribute("aria-label") == "Overview")
                    .GetAttribute("href"), Is.EqualTo("/"));
                Assert.That(links.Single(link => link.GetAttribute("aria-label") == "Compare").LocalName,
                    Is.EqualTo("button"));
                Assert.That(links.Single(link => link.GetAttribute("aria-label") == "Compare")
                    .GetAttribute("disabled"), Is.Not.Null);
                Assert.That(links.Single(link => link.GetAttribute("aria-label") == "Compare")
                    .GetAttribute("aria-disabled"), Is.EqualTo("true"));
                Assert.That(links.All(link => link.GetAttribute("title") is null), Is.True);
                Assert.That(links.Single(link => link.GetAttribute("aria-label") == "Overview")
                    .GetAttribute("aria-current"), Is.EqualTo("page"));
                Assert.That(links.Single(link => link.GetAttribute("aria-label") == "Compare")
                    .GetAttribute("aria-current"), Is.Null);
                Assert.That(icons, Has.Count.EqualTo(Items.Length));
                Assert.That(icons.All(icon => icon.GetAttribute("aria-hidden") == "true"), Is.True);
                Assert.That(component.FindAll(".mb-navigation-rail__label"), Has.Count.EqualTo(Items.Length));
                Assert.That(component.FindAll(
                    ".mb-navigation-rail__label > .mb-navigation-rail__label-text"),
                    Has.Count.EqualTo(Items.Length));
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
            var headings = component.FindAll(".mb-navigation-rail__group-heading");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dividers, Has.Count.EqualTo(2));
                Assert.That(component.Find(".mb-navigation-rail__items > li:first-child").ClassList,
                    Does.Contain("mb-navigation-rail__item"));
                Assert.That(dividers.All(divider => divider.GetAttribute("aria-hidden") == "true"), Is.True);
                Assert.That(dividers.All(divider => divider.GetAttribute("role") is null), Is.True);
                Assert.That(dividers.All(divider => divider.GetAttribute("aria-orientation") is null), Is.True);
                Assert.That(headings, Has.Count.EqualTo(ExpectedGroupHeadingLabels.Length));
                Assert.That(headings.All(heading => heading.GetAttribute("aria-hidden") == "true"), Is.True);
            }
        }

        [Test]
        public void VerifyExpandedNavigationRendersOptionalNonInteractiveGroupHeadings()
        {
            using var viewModel = CreateViewModel(NavigationRailPresentationMode.Expanded);
            using var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel));
            var headings = component.FindAll(".mb-navigation-rail__group-heading-label");
            var headingWrappers = component.FindAll(".mb-navigation-rail__group-heading");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(headings.Select(heading => heading.TextContent.Trim()), Is.EqualTo(ExpectedGroupHeadingLabels));
                Assert.That(headings.All(heading => heading.GetAttribute("role") == "heading"), Is.True);
                Assert.That(headings.All(heading => heading.GetAttribute("aria-level") == "2"), Is.True);
                Assert.That(headingWrappers.All(heading => heading.GetAttribute("aria-hidden") == "false"), Is.True);
                Assert.That(component.FindAll(".mb-navigation-rail__group-heading button"), Is.Empty);
                Assert.That(component.FindAll(".mb-navigation-rail__section-marker--named"),
                    Has.Count.EqualTo(ExpectedGroupHeadingLabels.Length));
                Assert.That(component.FindAll(".mb-navigation-rail__divider"),
                    Has.Count.EqualTo(ExpectedGroupHeadingLabels.Length));
            }
        }

        [Test]
        public async Task VerifyRouteSelectionUpdatesReactiveRendering()
        {
            using var viewModel = CreateViewModel(NavigationRailPresentationMode.Expanded);
            using var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel));

            await component.InvokeAsync(() => viewModel.ReconcileSelection("/review"));

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(viewModel.SelectedItem, Is.SameAs(Items[3]));
                    Assert.That(component.Find("a[aria-label='Overview']").GetAttribute("aria-current"), Is.Null);
                    Assert.That(component.Find("a[aria-label='Review']").GetAttribute("aria-current"),
                        Is.EqualTo("page"));
                }
            });
        }

        [Test]
        public async Task VerifyPresentationInteractionsDelegateToViewModelContract()
        {
            var viewModel = CreateViewModelMock();
            using var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel.Object));

            await OpenSidebarControlMenuAsync(component);
            var options = await this.portalHost.WaitForElementsAsync(
                "[role='menuitem']",
                ExpectedPresentationModeLabels.Length);
            await options.Single(option => option.TextContent.Trim() == "Expanded").ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                viewModel.VerifySet(x => x.SelectedItem = It.IsAny<NavigationRailItem>(), Times.Never);
                viewModel.VerifySet(x => x.PresentationMode = NavigationRailPresentationMode.Expanded, Times.Once);
            }
        }

        [Test]
        public void VerifyContextChangesRerenderAndReconcileSelection()
        {
            var contextService = new ContextAwareService();
            using var viewModel = new NavigationRailViewModel(
                contextService,
                CreateNavigationRailItemProvider(
                    (lifecycleState, _) => SelectItemsByLifecycleState(
                        lifecycleState,
                        Items,
                        ReplacementItems)));
            viewModel.SelectedItem = Items[3];
            viewModel.PresentationMode = NavigationRailPresentationMode.Expanded;
            using var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel));

            contextService.LifecycleState = ProjectLifecycleState.Open;

            component.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.FindAll(".mb-navigation-rail__link")
                        .Select(link => link.GetAttribute("aria-label")),
                        Is.EqualTo(ExpectedReplacementLabels));
                    Assert.That(component.FindAll(".mb-navigation-rail__divider"), Has.Count.EqualTo(1));
                    Assert.That(component.Find("a[aria-label='Activity']").GetAttribute("aria-current"),
                        Is.EqualTo("page"));
                    Assert.That(viewModel.SelectedItem, Is.SameAs(ReplacementItems[0]));
                }
            });
        }

        [Test]
        public async Task VerifySidebarControlMenuShowsThreeMutuallyExclusiveModes()
        {
            using var viewModel = CreateViewModel();
            using var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel));
            var trigger = component.Find(".mb-navigation-rail__collapse-toggle");

            await OpenSidebarControlMenuAsync(component);

            var menu = await this.portalHost.WaitForElementAsync("[role='menu']");
            var options = await this.portalHost.WaitForElementsAsync(
                "[role='menuitem']",
                ExpectedPresentationModeLabels.Length);
            var selectedOption = options.Single(option => option.TextContent.Contains("Expand on hover"));
            var dropdownTrigger = component.FindComponent<BbDropdownMenuTrigger>();
            var dropdownContent = component.FindComponent<BbDropdownMenuContent>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(trigger.GetAttribute("aria-label"), Is.EqualTo("Navigation rail options"));
                Assert.That(trigger.GetAttribute("aria-keyshortcuts"), Is.Null);
                Assert.That(trigger.GetAttribute("aria-haspopup"), Is.EqualTo("menu"));
                Assert.That(trigger.GetAttribute("aria-expanded"), Is.EqualTo("true"));
                Assert.That(component.Find("nav").GetAttribute("data-overlay-expanded"), Is.EqualTo("false"));
                Assert.That(menu.TextContent, Does.Not.Contain("Navigation rail"));
                Assert.That(menu.TextContent, Does.Contain("Sidebar control"));
                Assert.That(menu.TextContent, Does.Not.Contain("Default state"));
                Assert.That(options.Select(option => option.TextContent
                        .Replace("\u2022", string.Empty, StringComparison.Ordinal)
                        .Replace("Current selection", string.Empty, StringComparison.Ordinal)
                        .Trim()),
                    Is.EqualTo(ExpectedPresentationModeLabels));
                Assert.That(this.portalHost.FindAll("[role='separator']"), Is.Empty);
                Assert.That(this.portalHost.FindAll("[role='menuitemcheckbox']"), Is.Empty);
                Assert.That(options.All(option => option.GetAttribute("aria-disabled") != "true"), Is.True);
                Assert.That(selectedOption.ClassList,
                    Does.Contain("mb-navigation-rail__control-option--selected"));
                Assert.That(selectedOption.TextContent, Does.Contain("Current selection"));
                Assert.That(component.FindComponents<BbDropdownMenu>(), Has.Count.EqualTo(1));
                Assert.That(component.FindComponents<BbDropdownMenuContent>(), Has.Count.EqualTo(1));
                Assert.That(dropdownTrigger.Instance.AsChild, Is.False);
                Assert.That(dropdownTrigger.Instance.CustomClickHandling, Is.False);
                Assert.That(dropdownContent.Instance.Side, Is.EqualTo(PopoverSide.Right));
                Assert.That(dropdownContent.Instance.Align, Is.EqualTo(PopoverAlign.End));
                Assert.That(dropdownContent.Instance.Offset, Is.EqualTo(4));
                Assert.That(dropdownContent.Instance.Strategy, Is.EqualTo(PositioningStrategy.Fixed));
            }

            await options.Single(option => option.TextContent.Trim() == "Expanded").ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(viewModel.PresentationMode, Is.EqualTo(NavigationRailPresentationMode.Expanded));
                    Assert.That(component.Find("nav").GetAttribute("data-collapsed"), Is.EqualTo("false"));
                    Assert.That(this.portalHost.FindAll("[role='menu']"), Is.Empty);
                }
            });
        }

        [Test]
        public async Task VerifySidebarControlMenuSelectsCollapsedMode()
        {
            using var viewModel = CreateViewModel(NavigationRailPresentationMode.Expanded);
            using var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel));

            await OpenSidebarControlMenuAsync(component);
            var options = await this.portalHost.WaitForElementsAsync(
                "[role='menuitem']",
                ExpectedPresentationModeLabels.Length);
            await options.Single(option => option.TextContent.Trim() == "Collapsed").ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(viewModel.PresentationMode, Is.EqualTo(NavigationRailPresentationMode.Collapsed));
                    Assert.That(component.Find("nav").GetAttribute("data-layout-collapsed"), Is.EqualTo("true"));
                    Assert.That(component.Find("nav").GetAttribute("data-overlay-expanded"), Is.EqualTo("false"));
                    Assert.That(component.Find("nav").ClassList,
                        Does.Not.Contain("mb-navigation-rail--hover-overlay"));
                    Assert.That(this.portalHost.FindAll("[role='menu']"), Is.Empty);
                }
            });
        }

        [Test]
        public async Task VerifySidebarControlMenuSelectsExpandOnHoverMode()
        {
            using var viewModel = CreateViewModel(NavigationRailPresentationMode.Expanded);
            using var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel));

            await component.Find("nav").TriggerEventAsync("onmouseenter", new MouseEventArgs());
            await OpenSidebarControlMenuAsync(component);
            var options = await this.portalHost.WaitForElementsAsync(
                "[role='menuitem']",
                ExpectedPresentationModeLabels.Length);
            await options.Single(option => option.TextContent.Trim() == "Expand on hover").ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(viewModel.PresentationMode,
                        Is.EqualTo(NavigationRailPresentationMode.ExpandOnHover));
                    Assert.That(component.Find("nav").GetAttribute("data-layout-collapsed"), Is.EqualTo("true"));
                    Assert.That(component.Find("nav").GetAttribute("data-collapsed"), Is.EqualTo("true"));
                    Assert.That(component.Find("nav").GetAttribute("data-overlay-expanded"), Is.EqualTo("false"));
                    Assert.That(this.portalHost.FindAll("[role='menu']"), Is.Empty);
                }
            });

            await component.Find("nav").TriggerEventAsync("onmouseleave", new MouseEventArgs());
            await component.Find("nav").TriggerEventAsync("onmouseenter", new MouseEventArgs());

            await component.WaitForAssertionAsync(() =>
                Assert.That(component.Find("nav").GetAttribute("data-overlay-expanded"), Is.EqualTo("true")));
        }

        [Test]
        public async Task VerifySidebarControlTriggerDoesNotTogglePersistentPresentation()
        {
            using var viewModel = CreateViewModel(NavigationRailPresentationMode.Expanded);
            using var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel));
            var sidebarControlIcon = component.FindComponent<BbDropdownMenuTrigger>()
                .FindComponent<LucideIcon>();

            Assert.That(sidebarControlIcon.Instance.Name, Is.EqualTo("panel-left-close"));

            await component.Find(".mb-navigation-rail__collapse-toggle").ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(viewModel.PresentationMode, Is.EqualTo(NavigationRailPresentationMode.Expanded));
                    Assert.That(component.Find("nav").GetAttribute("data-collapsed"), Is.EqualTo("false"));
                    Assert.That(this.portalHost.FindAll("[role='menu']"), Has.Count.EqualTo(1));
                }
            });
        }

        [Test]
        public async Task VerifyHoverModeReactsWithoutCallerRoundTrips()
        {
            using var viewModel = CreateViewModel();
            using var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("nav").GetAttribute("data-collapsed"), Is.EqualTo("true"));
                Assert.That(component.Find("nav").ClassList,
                    Does.Contain("mb-navigation-rail--hover-overlay"));
                Assert.That(component.Find("nav").ClassList,
                    Does.Not.Contain("mb-navigation-rail--overlay-expanded"));
            }

            await component.Find("nav").TriggerEventAsync("onmouseenter", new MouseEventArgs());

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.Find("nav").GetAttribute("data-collapsed"), Is.EqualTo("false"));
                    Assert.That(component.Find("nav").ClassList,
                        Does.Contain("mb-navigation-rail--hover-overlay"));
                    Assert.That(component.Find("nav").ClassList,
                        Does.Contain("mb-navigation-rail--overlay-expanded"));
                }
            });

            await component.Find("nav").TriggerEventAsync("onmouseleave", new MouseEventArgs());

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.Find("nav").GetAttribute("data-collapsed"), Is.EqualTo("true"));
                    Assert.That(component.Find("nav").ClassList,
                        Does.Contain("mb-navigation-rail--hover-overlay"));
                    Assert.That(component.Find("nav").ClassList,
                        Does.Not.Contain("mb-navigation-rail--overlay-expanded"));
                    Assert.That(viewModel.PresentationMode,
                        Is.EqualTo(NavigationRailPresentationMode.ExpandOnHover));
                }
            });
        }

        /// <summary>
        /// Verifies pointer entry cannot expand a rail in the persistent collapsed mode.
        /// </summary>
        [Test]
        public async Task VerifyCollapsedModeKeepsRailVisuallyCollapsedOnHover()
        {
            using var viewModel = CreateViewModel(NavigationRailPresentationMode.Collapsed);
            using var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel));

            await component.Find("nav").TriggerEventAsync("onmouseenter", new MouseEventArgs());

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.Find("nav").GetAttribute("data-collapsed"), Is.EqualTo("true"));
                    Assert.That(component.Find("nav").GetAttribute("data-overlay-expanded"), Is.EqualTo("false"));
                }
            });
        }

        /// <summary>
        /// Verifies the persistent expanded mode does not use transient overlay presentation.
        /// </summary>
        [Test]
        public async Task VerifyPersistentExpandedStateDoesNotUseTransientOverlay()
        {
            using var viewModel = CreateViewModel(NavigationRailPresentationMode.Expanded);
            using var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel));

            await component.Find("nav").TriggerEventAsync("onmouseenter", new MouseEventArgs());
            await component.Find("nav").TriggerEventAsync("onmouseleave", new MouseEventArgs());

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.Find("nav").GetAttribute("data-collapsed"), Is.EqualTo("false"));
                    Assert.That(component.Find("nav").GetAttribute("data-layout-collapsed"), Is.EqualTo("false"));
                    Assert.That(component.Find("nav").GetAttribute("data-overlay-expanded"), Is.EqualTo("false"));
                }
            });
        }

        [Test]
        public async Task VerifyLayoutCollapsedChangesReportOnlyPersistentShellReservation()
        {
            using var viewModel = CreateViewModel();
            var reportedStates = new List<bool>();
            using var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel)
                .Add(rail => rail.LayoutCollapsedChanged, reportedStates.Add));

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(reportedStates, Has.Count.EqualTo(1));
                    Assert.That(reportedStates[^1], Is.True);
                }
            });

            await component.Find("nav").TriggerEventAsync("onmouseenter", new MouseEventArgs());

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.Find("nav").GetAttribute("data-collapsed"), Is.EqualTo("false"));
                    Assert.That(component.Find("nav").GetAttribute("data-layout-collapsed"), Is.EqualTo("true"));
                    Assert.That(reportedStates, Is.EqualTo(InitialCollapsedLayoutStates));
                }
            });

            await component.Find("nav").TriggerEventAsync("onmouseleave", new MouseEventArgs());

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(reportedStates, Is.EqualTo(InitialCollapsedLayoutStates));
                }
            });

            viewModel.PresentationMode = NavigationRailPresentationMode.Expanded;

            await component.WaitForAssertionAsync(() =>
                Assert.That(reportedStates, Is.EqualTo(ExpandedLayoutStates)));

            viewModel.PresentationMode = NavigationRailPresentationMode.Collapsed;

            await component.WaitForAssertionAsync(() =>
                Assert.That(reportedStates, Is.EqualTo(CollapsedAgainLayoutStates)));

            viewModel.PresentationMode = NavigationRailPresentationMode.ExpandOnHover;

            await component.WaitForAssertionAsync(() =>
                Assert.That(reportedStates, Is.EqualTo(CollapsedAgainLayoutStates)));
        }

        [Test]
        public void VerifyCollapsedPresentationRetainsIconFirstAccessibility()
        {
            using var viewModel = CreateViewModel(NavigationRailPresentationMode.Collapsed);
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
                Assert.That(component.FindAll(".mb-navigation-rail__group-heading")
                    .All(heading => heading.GetAttribute("aria-hidden") == "true"), Is.True);
                Assert.That(component.FindAll("[role='tooltip']"), Is.Empty);
            }
        }

        [Test]
        public async Task VerifyIconSlotStructureRemainsInvariantAcrossPresentationModes()
        {
            using var viewModel = CreateViewModel(NavigationRailPresentationMode.Collapsed);
            using var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel));
            var initialSlotClasses = component.FindAll(".mb-navigation-rail__icon-slot")
                .Select(slot => slot.GetAttribute("class"))
                .ToArray();
            var initialSectionMarkerClasses = component.FindAll(".mb-navigation-rail__section-marker")
                .Select(marker => marker.GetAttribute("class"))
                .ToArray();

            foreach (var presentationMode in Enum.GetValues<NavigationRailPresentationMode>())
            {
                viewModel.PresentationMode = presentationMode;

                await component.WaitForAssertionAsync(() =>
                {
                    using (Assert.EnterMultipleScope())
                    {
                        Assert.That(component.FindAll(".mb-navigation-rail__icon-slot")
                            .Select(slot => slot.GetAttribute("class")), Is.EqualTo(initialSlotClasses));
                        Assert.That(component.FindAll(
                            ".mb-navigation-rail__link > .mb-navigation-rail__icon-slot > .mb-navigation-rail__icon"),
                            Has.Count.EqualTo(Items.Length));
                        Assert.That(component.FindAll(
                            ".mb-navigation-rail__link > .mb-navigation-rail__label > .mb-navigation-rail__label-text"),
                            Has.Count.EqualTo(Items.Length));
                        Assert.That(component.FindAll(".mb-navigation-rail__section-marker")
                            .Select(marker => marker.GetAttribute("class")), Is.EqualTo(initialSectionMarkerClasses));
                    }
                });
            }
        }

        [Test]
        public void VerifyDynamicReorderingPreservesStableDestinationIdentity()
        {
            var contextService = new ContextAwareService();
            using var viewModel = new NavigationRailViewModel(
                contextService,
                CreateNavigationRailItemProvider(
                    (lifecycleState, _) => SelectItemsByLifecycleState(
                        lifecycleState,
                        Items,
                        ReorderedItems)));
            viewModel.PresentationMode = NavigationRailPresentationMode.Expanded;
            using var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel));
            var originalOverviewButton = component.FindComponents<BbButton>()
                .Single(button => button.Instance.AriaLabel == "Overview")
                .Instance;

            contextService.LifecycleState = ProjectLifecycleState.Review;

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
                    Assert.That(viewModel.SelectedItem, Is.SameAs(ReorderedItems[2]));
                }
            });
        }

        [Test]
        public void VerifyReplacingViewModelDetachesOldRenderingSubscription()
        {
            var firstContextService = new ContextAwareService();
            var secondContextService = new ContextAwareService();
            using var firstViewModel = new NavigationRailViewModel(
                firstContextService,
                CreateNavigationRailItemProvider(
                    (lifecycleState, _) => SelectItemsByLifecycleState(
                        lifecycleState,
                        OverviewOnly,
                        ActivityOnly)));
            using var secondViewModel = new NavigationRailViewModel(
                secondContextService,
                CreateNavigationRailItemProvider(
                    (lifecycleState, _) => SelectItemsByLifecycleState(
                        lifecycleState,
                        ReviewOnly,
                        SettingsOnly)));
            using var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, firstViewModel));

            component.Render(parameters => parameters.Add(rail => rail.ViewModel, secondViewModel));
            var renderCountAfterReplacement = component.RenderCount;

            firstContextService.LifecycleState = ProjectLifecycleState.Open;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.RenderCount, Is.EqualTo(renderCountAfterReplacement));
                Assert.That(component.FindAll(".mb-navigation-rail__link")
                    .Select(link => link.GetAttribute("aria-label")), Is.EqualTo(ExpectedReviewOnlyLabels));
            }

            secondContextService.LifecycleState = ProjectLifecycleState.Open;

            component.WaitForAssertion(() =>
                Assert.That(component.FindAll(".mb-navigation-rail__link")
                    .Select(link => link.GetAttribute("aria-label")), Is.EqualTo(ExpectedSettingsOnlyLabels)));
        }

        [Test]
        public async Task VerifyComponentsSharingViewModelDoNotShareHoverState()
        {
            using var viewModel = CreateViewModel();
            using var first = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel));
            using var second = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel));

            await first.Find("nav").TriggerEventAsync("onmouseenter", new MouseEventArgs());

            await first.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(first.Find("nav").GetAttribute("data-collapsed"), Is.EqualTo("false"));
                    Assert.That(second.Find("nav").GetAttribute("data-collapsed"), Is.EqualTo("true"));
                }
            });

            await first.Find("nav").TriggerEventAsync("onmouseleave", new MouseEventArgs());

            await first.WaitForAssertionAsync(() =>
                Assert.That(first.Find("nav").GetAttribute("data-collapsed"), Is.EqualTo("true")));
        }

        [Test]
        public void VerifyComponentDisposalDoesNotDisposeSuppliedViewModel()
        {
            var contextService = new ContextAwareService();
            using var viewModel = new NavigationRailViewModel(
                contextService,
                CreateNavigationRailItemProvider(
                    (lifecycleState, _) => SelectItemsByLifecycleState(
                        lifecycleState,
                        Items,
                        ReplacementItems)));
            var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.ViewModel, viewModel));

            component.Dispose();

            Assert.DoesNotThrow(() => contextService.LifecycleState = ProjectLifecycleState.Open);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.NavigationItems, Is.EqualTo(ReplacementItems));
                Assert.That(viewModel.SelectedItem, Is.SameAs(ReplacementItems[0]));
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
                Assert.That(style, Does.Contain(
                    "--mb-navigation-rail-icon-column-width: var(--mb-navigation-rail-target-size);"));
                Assert.That(style, Does.Contain("--mb-navigation-rail-divider-height: 22px;"));
                Assert.That(style, Does.Contain("--mb-navigation-rail-divider-width: var(--mb-spacing-6);"));
                Assert.That(style, Does.Contain("padding: 16px 6px;"));
                Assert.That(style, Does.Contain("border-right: 1px solid var(--mb-color-border-subtle);"));
                Assert.That(style, Does.Contain("background: var(--mb-color-action-primary-soft);"));
                Assert.That(style, Does.Contain("overflow-y: auto;"));
                Assert.That(style, Does.Contain("scrollbar-width: thin;"));
                Assert.That(style, Does.Contain("scrollbar-color: var(--mb-navigation-rail-scrollbar-thumb) transparent;"));
                Assert.That(style, Does.Contain("background-attachment: local, local, scroll, scroll;"));
                Assert.That(style, Does.Contain("@supports (scrollbar-width: none)"));
                Assert.That(style, Does.Contain("scrollbar-width: none;"));
                Assert.That(style, Does.Contain("@media (forced-colors: active)"));
                Assert.That(style, Does.Contain("scrollbar-color: ButtonText Canvas;"));
                Assert.That(style, Does.Contain(".mb-navigation-rail__items::-webkit-scrollbar-button"));
                Assert.That(style, Does.Contain("--mb-navigation-rail-collapsed-width: 52px;"));
                Assert.That(style, Does.Not.Contain("--mb-navigation-rail-expanded-max-width:"));
                Assert.That(style, Does.Not.Contain("--mb-navigation-rail-expanded-width:"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail\s*\{[^}]*width:\s*fit-content;[^}]*max-width:\s*100%;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail--collapsed\s*\{[^}]*width:\s*var\(--mb-navigation-rail-collapsed-width\);"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail--hover-overlay\s*\{[^}]*position:\s*absolute;[^}]*z-index:\s*2;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail--hover-overlay\s*\{[^}]*width:\s*max-content;[^}]*max-width:\s*calc\(100vw\s*-\s*var\(--mb-spacing-4\)\);"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail--overlay-expanded\s*\{[^}]*box-shadow:"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__items\s*\{[^}]*align-self:\s*flex-start;[^}]*align-items:\s*stretch;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__link\s*\{[^}]*display:\s*grid;[^}]*grid-template-columns:\s*var\(--mb-navigation-rail-icon-column-width\)\s+auto;[^}]*justify-content:\s*start;[^}]*gap:\s*0;[^}]*padding:\s*0;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__icon-slot\s*\{[^}]*display:\s*grid;[^}]*place-items:\s*center;[^}]*width:\s*var\(--mb-navigation-rail-icon-column-width\);"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__label-text\s*\{[^}]*width:\s*max-content;[^}]*padding-inline-start:\s*var\(--mb-spacing-3\);[^}]*padding-inline-end:\s*var\(--mb-spacing-2\);"));
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
                    Does.Not.Match(
                        @"(?s)\.mb-navigation-rail__sidebar-control\s+::deep\s+\.mb-navigation-rail__control-menu\s*\{[^}]*translate:"));
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
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__section-marker\s*\{[^}]*position:\s*relative;[^}]*align-self:\s*stretch;[^}]*height:\s*var\(--mb-navigation-rail-divider-height\);[^}]*overflow:\s*hidden;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__divider\s*\{[^}]*position:\s*absolute;[^}]*inset:\s*0;[^}]*opacity:\s*1;[^}]*transition:\s*opacity\s+var\(--mb-transition-fast\);"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__group-heading\s*\{[^}]*width:\s*max-content;[^}]*max-width:\s*100%;[^}]*overflow:\s*hidden;[^}]*opacity:\s*1;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__group-heading-label\s*\{[^}]*width:\s*max-content;[^}]*padding:\s*var\(--mb-spacing-3\)\s+var\(--mb-spacing-2\)\s+var\(--mb-spacing-1\)\s+var\(--mb-spacing-3\);[^}]*letter-spacing:\s*0\.08em;[^}]*pointer-events:\s*none;[^}]*text-align:\s*start;[^}]*text-overflow:\s*ellipsis;"));
                Assert.That(style, Does.Contain("@media (prefers-reduced-motion: reduce)"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail--collapsed\s+\.mb-navigation-rail__label\s*\{[^}]*display:\s*none;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail--collapsed\s+\.mb-navigation-rail__group-heading\s*\{[^}]*display:\s*none;"));
                Assert.That(style, Does.Contain("@supports (interpolate-size: allow-keywords)"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail\s*\{[^}]*interpolate-size:\s*allow-keywords;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail:not\(\.mb-navigation-rail--hover-overlay\)\s*\{[^}]*transition:\s*width\s+var\(--mb-transition-fast\);"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__section-marker\s*\{[^}]*transition:\s*height\s+var\(--mb-transition-fast\);"));
                Assert.That(style, Does.Not.Contain("gap var(--mb-transition-fast)"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__label\s*\{[^}]*display:\s*block;[^}]*opacity:\s*1;[^}]*transition:\s*opacity\s+var\(--mb-transition-fast\);"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail--collapsed\s+\.mb-navigation-rail__label\s*\{[^}]*display:\s*block;[^}]*opacity:\s*0;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail--hover-overlay\s+\.mb-navigation-rail__label\s*\{[^}]*width\s+var\(--mb-transition-fast\),[^}]*opacity\s+var\(--mb-transition-fast\);"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail--hover-overlay\.mb-navigation-rail--collapsed\s+\.mb-navigation-rail__label\s*\{[^}]*width:\s*0;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail__group-heading\s*\{[^}]*display:\s*block;[^}]*transition:\s*opacity\s+var\(--mb-transition-fast\);"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail--collapsed\s+\.mb-navigation-rail__group-heading\s*\{[^}]*display:\s*block;[^}]*opacity:\s*0;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail--hover-overlay\s+\.mb-navigation-rail__group-heading\s*\{[^}]*width\s+var\(--mb-transition-fast\),[^}]*opacity\s+var\(--mb-transition-fast\);"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail--hover-overlay\.mb-navigation-rail--collapsed\s+\.mb-navigation-rail__group-heading\s*\{[^}]*width:\s*0;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)@media\s*\(prefers-reduced-motion:\s*reduce\).*?\.mb-navigation-rail__label,[^{]*\{[^}]*transition:\s*none;"));
            }
        }

        private static Mock<INavigationRailViewModel> CreateViewModelMock()
        {
            var navigationItems = new ReadOnlyObservableCollection<NavigationRailItem>(
                new ObservableCollection<NavigationRailItem>(Items));
            var viewModel = new Mock<INavigationRailViewModel>(MockBehavior.Strict);
            viewModel.SetupGet(x => x.NavigationItems).Returns(navigationItems);
            viewModel.SetupProperty(x => x.SelectedItem, Items[0]);
            viewModel.SetupProperty(x => x.PresentationMode, NavigationRailPresentationMode.ExpandOnHover);
            viewModel.Setup(x => x.Dispose());

            return viewModel;
        }

        private static NavigationRailViewModel CreateViewModel(
            NavigationRailPresentationMode mode = NavigationRailPresentationMode.ExpandOnHover,
            string selectedItemId = "overview")
        {
            var viewModel = new NavigationRailViewModel(
                new ContextAwareService(),
                CreateNavigationRailItemProvider((_, _) => Items));
            viewModel.SelectedItem = Items.Single(item => item.Id == selectedItemId);
            viewModel.PresentationMode = mode;

            return viewModel;
        }

        private static IReadOnlyList<NavigationRailItem> SelectItemsByLifecycleState(
            ProjectLifecycleState lifecycleState,
            IReadOnlyList<NavigationRailItem> preparationItems,
            IReadOnlyList<NavigationRailItem> nonPreparationItems)
        {
            return lifecycleState switch
            {
                ProjectLifecycleState.Preparation => preparationItems,
                ProjectLifecycleState.Open => nonPreparationItems,
                ProjectLifecycleState.Review => nonPreparationItems,
                ProjectLifecycleState.Archived => nonPreparationItems,
                _ => throw new ArgumentOutOfRangeException(nameof(lifecycleState), lifecycleState, null)
            };
        }

        private static Task OpenSidebarControlMenuAsync(
            IRenderedComponent<NavigationRailComponent> component)
        {
            return component.Find(".mb-navigation-rail__collapse-toggle").ClickAsync();
        }

        private static INavigationRailItemProvider CreateNavigationRailItemProvider(
            Func<ProjectLifecycleState, IElement, IReadOnlyList<NavigationRailItem>> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);

            var provider = new Mock<INavigationRailItemProvider>(MockBehavior.Strict);
            provider.Setup(x => x.GetNavigationItems(
                    It.IsAny<ProjectLifecycleState>(),
                    It.IsAny<IElement>()))
                .Returns(selector);

            return provider.Object;
        }
    }
}
