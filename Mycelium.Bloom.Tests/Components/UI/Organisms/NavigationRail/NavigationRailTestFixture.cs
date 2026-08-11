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

    using Bunit;

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

        /// <summary>
        /// Verifies collapse requests and icon-first accessibility remain controlled by the caller.
        /// </summary>
        [Test]
        public async Task VerifyCollapseCallbackReportsRequestedState()
        {
            bool? requestedCollapsedState = null;
            var component = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.Items, Items)
                .Add(rail => rail.CollapsedChanged, collapsed => requestedCollapsedState = collapsed));
            var itemsId = component.Find(".mb-navigation-rail__items").Id;
            var collapseToggle = component.Find("button[aria-label='Collapse workspace navigation']");

            await collapseToggle.ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(requestedCollapsedState, Is.True);
                Assert.That(component.Find("nav").GetAttribute("data-collapsed"), Is.EqualTo("false"));
                Assert.That(collapseToggle.GetAttribute("aria-expanded"), Is.EqualTo("true"));
                Assert.That(collapseToggle.GetAttribute("aria-controls"), Is.EqualTo(itemsId));
            }

            component.Render(parameters => parameters
                .Add(rail => rail.Items, Items)
                .Add(rail => rail.Collapsed, true)
                .Add(rail => rail.CollapsedChanged, collapsed => requestedCollapsedState = collapsed));

            var collapsedRoot = component.Find("nav");
            var expandToggle = component.Find("button[aria-label='Expand workspace navigation']");
            var links = component.FindAll(".mb-navigation-rail__link");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(collapsedRoot.GetAttribute("data-collapsed"), Is.EqualTo("true"));
                Assert.That(collapsedRoot.ClassList, Does.Contain("mb-navigation-rail--collapsed"));
                Assert.That(expandToggle.GetAttribute("aria-expanded"), Is.EqualTo("false"));
                Assert.That(links.Select(link => link.GetAttribute("aria-label")), Is.EqualTo(ExpectedLabels));
                Assert.That(links.Select(link => link.GetAttribute("title")), Is.EqualTo(ExpectedLabels));
                Assert.That(component.FindAll(".mb-navigation-rail__label"), Has.Count.EqualTo(Items.Count));
                Assert.That(component.FindAll("[role='tooltip']"), Is.Empty);
            }

            await expandToggle.ClickAsync();

            Assert.That(requestedCollapsedState, Is.False);
        }

        /// <summary>
        /// Verifies multiple rails keep unique relationships and independent callback state.
        /// </summary>
        [Test]
        public async Task VerifyIndependentInstancesDoNotShareState()
        {
            string firstRequest = null;
            string secondRequest = null;
            var first = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.Items, Items)
                .Add(rail => rail.SelectedItemIdChanged, itemId => firstRequest = itemId)
                .Add(rail => rail.CollapsedChanged, _ => { }));
            var second = this.Render<NavigationRailComponent>(parameters => parameters
                .Add(rail => rail.Items, Items)
                .Add(rail => rail.SelectedItemIdChanged, itemId => secondRequest = itemId)
                .Add(rail => rail.CollapsedChanged, _ => { }));

            await first.Find("button[aria-label='Compare']").ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstRequest, Is.EqualTo("compare"));
                Assert.That(secondRequest, Is.Null);
                Assert.That(first.Find(".mb-navigation-rail__items").Id,
                    Is.Not.EqualTo(second.Find(".mb-navigation-rail__items").Id));
                Assert.That(first.Find(".mb-navigation-rail__collapse-toggle").GetAttribute("aria-controls"),
                    Is.Not.EqualTo(second.Find(".mb-navigation-rail__collapse-toggle").GetAttribute("aria-controls")));
            }
        }

        /// <summary>
        /// Verifies scoped styles retain the Figma-derived geometry and responsive containment contracts.
        /// </summary>
        [Test]
        public void VerifyNavigationRailStyleContracts()
        {
            var style = File.ReadAllText(Path.Combine(
                TestRepository.GetRootPath(),
                "Mycelium.Bloom",
                "Components",
                "UI",
                "Organisms",
                "NavigationRail",
                "NavigationRail.razor.css"));

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
                Assert.That(style, Does.Contain("@media (prefers-reduced-motion: reduce)"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-navigation-rail--collapsed\s+\.mb-navigation-rail__label\s*\{[^}]*display:\s*none;"));
                Assert.That(style, Does.Not.Contain("width: 52px;"));
            }
        }
    }
}
