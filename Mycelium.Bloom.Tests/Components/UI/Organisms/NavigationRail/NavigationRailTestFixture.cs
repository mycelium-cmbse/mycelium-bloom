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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using BlazorBlueprint.Components;
    using BlazorBlueprint.Icons.Lucide.Components;

    using Bunit;

    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Tests.Common;

    using NavigationRailComponent = Mycelium.Bloom.Components.UI.Organisms.NavigationRail.NavigationRail;

    /// <summary>
    /// Tests the <see cref="NavigationRailComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class NavigationRailTestFixture : BunitContext
    {
        private static readonly IReadOnlyList<NavigationRailItem> Items =
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

        private static readonly string[] ExpectedLabels = ["Overview", "Traceability", "Compare", "Review"];

        private static readonly bool[] ExpectedHoverExpansionRequest = [false];

        private static readonly bool[] ExpectedHoverRoundTripRequests = [false, true];

        private static readonly bool[] ExpectedSidebarControlHoverRequests = [true, false];

        private static readonly bool[] ExpectedSidebarControlCollapseRequests = [true, false, true];

        private static readonly bool[] ExpectedInitialSidebarControlRequests = [true];

        private static readonly bool[] ExpectedSidebarToggleRequests = [true, false];

        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationRailTestFixture" /> class.
        /// </summary>
        public NavigationRailTestFixture()
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
        /// Verifies the rail renders semantic, named destinations and extensibility attributes.
        /// </summary>
        [Test]
        public void VerifyRenderExposesSemanticDestinationsAndCustomAttributes()
        {
            var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.Items, Items)
                .Add(rail => rail.SelectedItemId, "overview")
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
                Assert.That(component.FindAll("li.mb-navigation-rail__item"), Has.Count.EqualTo(Items.Count));
                Assert.That(links.Select(link => link.GetAttribute("aria-label")), Is.EqualTo(ExpectedLabels));
                Assert.That(links.All(link => link.GetAttribute("type") == "button"), Is.True);
                Assert.That(links.Single(link => link.GetAttribute("aria-label") == "Overview")
                    .GetAttribute("aria-current"), Is.EqualTo("page"));
                Assert.That(links.Single(link => link.GetAttribute("aria-label") == "Compare")
                    .GetAttribute("aria-current"), Is.Null);
                Assert.That(icons, Has.Count.EqualTo(Items.Count));
                Assert.That(icons.All(icon => icon.GetAttribute("aria-hidden") == "true"), Is.True);
                Assert.That(component.FindAll(".mb-navigation-rail__collapse-toggle"), Is.Empty);
                Assert.That(component.Markup, Does.Not.Contain(">Workspace<").IgnoreCase);
            }
        }

        /// <summary>
        /// Verifies section boundaries are supplied entirely by navigation data.
        /// </summary>
        [Test]
        public void VerifyGroupedNavigationRendersSectionDividers()
        {
            var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.Items, Items));
            var dividers = component.FindAll(".mb-navigation-rail__divider");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dividers, Has.Count.EqualTo(2));
                Assert.That(dividers.All(divider => divider.GetAttribute("aria-hidden") == "true"), Is.True);
                Assert.That(dividers.All(divider => divider.GetAttribute("role") is null), Is.True);
                Assert.That(dividers.All(divider => divider.GetAttribute("aria-orientation") is null), Is.True);
            }
        }

        /// <summary>
        /// Verifies destination selection is requested through the controlled callback and applied on rerender.
        /// </summary>
        [Test]
        public async Task VerifySelectionCallbackReportsRequestedDestination()
        {
            string requestedItemId = null;
            var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.Items, Items)
                .Add(rail => rail.SelectedItemId, "overview")
                .Add(rail => rail.SelectedItemIdChanged, itemId => requestedItemId = itemId));

            await component.Find("button[aria-label='Review']").ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(requestedItemId, Is.EqualTo("review"));
                Assert.That(component.Find("button[aria-label='Overview']").GetAttribute("aria-current"),
                    Is.EqualTo("page"));
                Assert.That(component.Find("button[aria-label='Review']").GetAttribute("aria-current"), Is.Null);
            }

            component.Render(parameters => parameters
                .Add(rail => rail.Items, Items)
                .Add(rail => rail.SelectedItemId, "review")
                .Add(rail => rail.SelectedItemIdChanged, itemId => requestedItemId = itemId));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("button[aria-label='Overview']").GetAttribute("aria-current"), Is.Null);
                Assert.That(component.Find("button[aria-label='Review']").GetAttribute("aria-current"),
                    Is.EqualTo("page"));
            }
        }

        [Test]
        public async Task VerifySidebarControlMenuExposesAccessiblePresentationChoices()
        {
            var collapseRequests = new List<bool>();
            var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.Items, Items)
                .Add(rail => rail.Collapsed, true)
                .Add(rail => rail.ExpandOnHover, true)
                .Add(rail => rail.CollapsedChanged, collapseRequests.Add)
                .Add(rail => rail.ExpandOnHoverChanged, _ => { }));
            var trigger = component.Find(".mb-navigation-rail__collapse-toggle");

            await OpenSidebarControlMenuAsync(component);

            var menu = component.WaitForElement("[role='menu']");
            var options = component.WaitForElements("[role='menuitem']", 3);
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
                Assert.That(selectedOption.ClassList,
                    Does.Contain("mb-navigation-rail__control-option--selected"));
                Assert.That(options.Count(option => option.ClassList
                    .Contains("mb-navigation-rail__control-option--selected")), Is.EqualTo(1));
                Assert.That(selectedOption.TextContent, Does.Contain("Current selection"));
                Assert.That(selectedOption.QuerySelector("[aria-hidden='true']")?.TextContent, Does.Contain("•"));
                Assert.That(component.FindComponents<BbContextMenu>(), Has.Count.EqualTo(1));
                Assert.That(component.FindComponents<BbContextMenuContent>(), Has.Count.EqualTo(1));
                Assert.That(collapseRequests, Is.Empty);
            }
        }

        private static Task OpenSidebarControlMenuAsync(
            IRenderedComponent<NavigationRailComponent> component)
        {
            return component.Find(".mb-navigation-rail__context-trigger")
                .TriggerEventAsync("oncontextmenu", new MouseEventArgs { Button = 2 });
        }

        [Test]
        public async Task VerifySidebarControlPrimaryClickTogglesWithoutOpeningMenu()
        {
            var collapseRequests = new List<bool>();
            var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.Items, Items)
                .Add(rail => rail.CollapsedChanged, collapseRequests.Add));
            var sidebarControlIcon = component.FindComponent<BbContextMenuTrigger>()
                .FindComponent<LucideIcon>();

            Assert.That(sidebarControlIcon.Instance.Name, Is.EqualTo("panel-left-close"));

            await component.Find(".mb-navigation-rail__collapse-toggle").ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(collapseRequests, Is.EqualTo(ExpectedInitialSidebarControlRequests));
                Assert.That(component.FindAll("[role='menu']"), Is.Empty);
                Assert.That(component.Find("nav").GetAttribute("data-collapsed"), Is.EqualTo("false"));
            }

            component.Render(parameters => parameters
                .Add(rail => rail.Items, Items)
                .Add(rail => rail.Collapsed, true)
                .Add(rail => rail.CollapsedChanged, collapseRequests.Add));

            sidebarControlIcon = component.FindComponent<BbContextMenuTrigger>()
                .FindComponent<LucideIcon>();

            Assert.That(sidebarControlIcon.Instance.Name, Is.EqualTo("panel-left-open"));

            await component.Find(".mb-navigation-rail__collapse-toggle").ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(collapseRequests, Is.EqualTo(ExpectedSidebarToggleRequests));
                Assert.That(component.FindAll("[role='menu']"), Is.Empty);
            }
        }

        [Test]
        public async Task VerifySidebarControlPrimaryClickPinsHoverPresentation()
        {
            var hoverRequests = new List<bool>();
            var collapseRequests = new List<bool>();
            var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.Items, Items)
                .Add(rail => rail.ExpandOnHover, true)
                .Add(rail => rail.CollapsedChanged, collapseRequests.Add)
                .Add(rail => rail.ExpandOnHoverChanged, hoverRequests.Add));

            await component.Find(".mb-navigation-rail__collapse-toggle").ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(hoverRequests, Is.EqualTo(ExpectedHoverExpansionRequest));
                Assert.That(collapseRequests, Is.EqualTo(ExpectedInitialSidebarControlRequests));
                Assert.That(component.FindAll("[role='menu']"), Is.Empty);
            }
        }

        [Test]
        public async Task VerifySidebarControlMenuRequestsControlledModes()
        {
            var hoverRequests = new List<bool>();
            var collapseRequests = new List<bool>();
            var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.Items, Items)
                .Add(rail => rail.CollapsedChanged, collapseRequests.Add)
                .Add(rail => rail.ExpandOnHoverChanged, hoverRequests.Add));

            await OpenSidebarControlMenuAsync(component);
            await component.WaitForElements("[role='menuitem']", 3)
                .Single(option => option.TextContent.Contains("Expand on hover"))
                .ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(hoverRequests, Is.EqualTo(ExpectedInitialSidebarControlRequests));
                Assert.That(collapseRequests, Is.EqualTo(ExpectedInitialSidebarControlRequests));
                Assert.That(component.Find("nav").GetAttribute("data-collapsed"), Is.EqualTo("false"));
            }

            component.Render(parameters => parameters
                .Add(rail => rail.Items, Items)
                .Add(rail => rail.Collapsed, true)
                .Add(rail => rail.ExpandOnHover, true)
                .Add(rail => rail.CollapsedChanged, collapseRequests.Add)
                .Add(rail => rail.ExpandOnHoverChanged, hoverRequests.Add));

            await OpenSidebarControlMenuAsync(component);
            await component.WaitForElements("[role='menuitem']", 3)
                .Single(option => option.TextContent.Contains("Expanded"))
                .ClickAsync();

            component.Render(parameters => parameters
                .Add(rail => rail.Items, Items)
                .Add(rail => rail.Collapsed, false)
                .Add(rail => rail.ExpandOnHover, false)
                .Add(rail => rail.CollapsedChanged, collapseRequests.Add)
                .Add(rail => rail.ExpandOnHoverChanged, hoverRequests.Add));

            await OpenSidebarControlMenuAsync(component);
            await component.WaitForElements("[role='menuitem']", 3)
                .Single(option => option.TextContent.Trim() == "Collapsed")
                .ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(hoverRequests, Is.EqualTo(ExpectedSidebarControlHoverRequests));
                Assert.That(collapseRequests, Is.EqualTo(ExpectedSidebarControlCollapseRequests));
                Assert.That(component.Find("nav").GetAttribute("data-collapsed"), Is.EqualTo("false"));
            }
        }

        [Test]
        public void VerifyCollapsedPresentationRetainsIconFirstAccessibility()
        {
            var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.Items, Items)
                .Add(rail => rail.Collapsed, true)
                .Add(rail => rail.CollapsedChanged, _ => { }));
            var root = component.Find("nav");
            var links = component.FindAll(".mb-navigation-rail__link");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root.GetAttribute("data-collapsed"), Is.EqualTo("true"));
                Assert.That(root.ClassList, Does.Contain("mb-navigation-rail--collapsed"));
                Assert.That(links.Select(link => link.GetAttribute("aria-label")), Is.EqualTo(ExpectedLabels));
                Assert.That(links.Select(link => link.GetAttribute("title")), Is.EqualTo(ExpectedLabels));
                Assert.That(component.FindAll(".mb-navigation-rail__label"), Has.Count.EqualTo(Items.Count));
                Assert.That(component.FindAll("[role='tooltip']"), Is.Empty);
            }
        }

        [Test]
        public async Task VerifyUnavailableHoverPreferenceIsDisabled()
        {
            var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.Items, Items)
                .Add(rail => rail.CollapsedChanged, _ => { }));

            await OpenSidebarControlMenuAsync(component);

            var options = component.WaitForElements("[role='menuitem']", 3);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(options.Single(option => option.TextContent.Contains("Expand on hover"))
                    .GetAttribute("aria-disabled"), Is.EqualTo("true"));
                Assert.That(options.Single(option => option.TextContent.Trim() == "Collapsed")
                    .GetAttribute("aria-disabled"), Is.Not.EqualTo("true"));
            }
        }

        [Test]
        public async Task VerifyHoverExpansionIsOptIn()
        {
            bool? requestedCollapsedState = null;
            var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.Items, Items)
                .Add(rail => rail.Collapsed, true)
                .Add(rail => rail.CollapsedChanged, collapsed => requestedCollapsedState = collapsed));

            await component.Find("nav").TriggerEventAsync("onmouseenter", new MouseEventArgs());

            Assert.That(requestedCollapsedState, Is.Null);
        }

        [Test]
        public async Task VerifyHoverExpansionRequestsControlledStates()
        {
            var requestedCollapsedStates = new List<bool>();
            var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.Items, Items)
                .Add(rail => rail.Collapsed, true)
                .Add(rail => rail.ExpandOnHover, true)
                .Add(rail => rail.CollapsedChanged, requestedCollapsedStates.Add));

            await component.Find("nav").TriggerEventAsync("onmouseenter", new MouseEventArgs());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(requestedCollapsedStates, Is.EqualTo(ExpectedHoverExpansionRequest));
                Assert.That(component.Find("nav").GetAttribute("data-collapsed"), Is.EqualTo("true"));
            }

            component.Render(parameters => parameters
                .Add(rail => rail.Items, Items)
                .Add(rail => rail.Collapsed, false)
                .Add(rail => rail.ExpandOnHover, true)
                .Add(rail => rail.CollapsedChanged, requestedCollapsedStates.Add));

            Assert.That(component.FindAll(".mb-navigation-rail__collapse-toggle"), Has.Count.EqualTo(1));

            await component.Find("nav").TriggerEventAsync("onmouseleave", new MouseEventArgs());

            Assert.That(requestedCollapsedStates, Is.EqualTo(ExpectedHoverRoundTripRequests));
        }

        [Test]
        public void VerifyHoverExpansionKeepsSidebarControlAvailable()
        {
            var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.Items, Items)
                .Add(rail => rail.Collapsed, true)
                .Add(rail => rail.ExpandOnHover, true)
                .Add(rail => rail.CollapsedChanged, _ => { }));

            Assert.That(component.FindAll(".mb-navigation-rail__collapse-toggle"), Has.Count.EqualTo(1));

            component.Render(parameters => parameters
                .Add(rail => rail.Items, Items)
                .Add(rail => rail.ExpandOnHover, true)
                .Add(rail => rail.CollapsedChanged, _ => { }));

            Assert.That(component.FindAll(".mb-navigation-rail__collapse-toggle"), Has.Count.EqualTo(1));
        }

        /// <summary>
        /// Verifies multiple rails keep unique relationships and independent callback state.
        /// </summary>
        [Test]
        public async Task VerifyIndependentInstancesDoNotShareState()
        {
            string firstRequest = null;
            string secondRequest = null;
            bool? firstCollapseRequest = null;
            bool? secondCollapseRequest = null;
            var first = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.Items, Items)
                .Add(rail => rail.Collapsed, true)
                .Add(rail => rail.ExpandOnHover, true)
                .Add(rail => rail.SelectedItemIdChanged, itemId => firstRequest = itemId)
                .Add(rail => rail.CollapsedChanged, collapsed => firstCollapseRequest = collapsed));
            var second = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.Items, Items)
                .Add(rail => rail.Collapsed, true)
                .Add(rail => rail.ExpandOnHover, true)
                .Add(rail => rail.SelectedItemIdChanged, itemId => secondRequest = itemId)
                .Add(rail => rail.CollapsedChanged, collapsed => secondCollapseRequest = collapsed));

            await first.Find("nav").TriggerEventAsync("onmouseenter", new MouseEventArgs());
            await first.Find("button[aria-label='Compare']").ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstRequest, Is.EqualTo("compare"));
                Assert.That(secondRequest, Is.Null);
                Assert.That(firstCollapseRequest, Is.False);
                Assert.That(secondCollapseRequest, Is.Null);
                Assert.That(first.Find(".mb-navigation-rail__items").Id,
                    Is.Not.EqualTo(second.Find(".mb-navigation-rail__items").Id));
                Assert.That(first.FindAll(".mb-navigation-rail__collapse-toggle"), Has.Count.EqualTo(1));
                Assert.That(second.FindAll(".mb-navigation-rail__collapse-toggle"), Has.Count.EqualTo(1));
            }
        }

        /// <summary>
        /// Verifies scoped styles retain the Figma-derived geometry and responsive containment contracts.
        /// </summary>
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
                        @"(?s)\.mb-navigation-rail--collapsed\s+\.mb-navigation-rail__label\s*\{[^}]*visibility:\s*hidden;"));
                Assert.That(style, Does.Not.Contain("width: 52px;"));
            }
        }
    }
}
