// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceShellTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.WorkspaceShell
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Bunit;

    using Mycelium.Bloom.Tests.Common;

    using WorkspaceShellComponent = Mycelium.Bloom.Components.UI.Organisms.WorkspaceShell.WorkspaceShell;

    /// <summary>
    /// Tests the <see cref="WorkspaceShellComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class WorkspaceShellTestFixture : BunitContext
    {
        /// <summary>
        /// Disposes the bUnit test context after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            this.Dispose();
        }

        /// <summary>
        /// Verifies all workspace regions render with semantic structure and accessible labels.
        /// </summary>
        [Test]
        public void VerifyAllRegionsRenderSemanticStructure()
        {
            var component = this.Render<WorkspaceShellComponent>(parameters => parameters
                .Add(component => component.Header, "<span>Header</span>")
                .Add(component => component.LeftPanel, "<span>Left</span>")
                .Add(component => component.MainContent, "<span>Main</span>")
                .Add(component => component.RightPanel, "<span>Right</span>")
                .Add(component => component.StatusBar, "<span>Status</span>")
                .Add(component => component.OverlayContent, "<button>Overlay</button>")
                .Add(component => component.AriaLabel, "Architecture workspace")
                .Add(component => component.HeaderAriaLabel, "Architecture header")
                .Add(component => component.LeftPanelAriaLabel, "Model navigation")
                .Add(component => component.MainContentAriaLabel, "Diagram canvas")
                .Add(component => component.RightPanelAriaLabel, "Element inspector")
                .Add(component => component.StatusBarAriaLabel, "Diagram status"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("section.mb-workspace-shell").GetAttribute("aria-label"),
                    Is.EqualTo("Architecture workspace"));
                Assert.That(component.Find(".mb-workspace-shell__header").GetAttribute("aria-label"),
                    Is.EqualTo("Architecture header"));
                Assert.That(component.Find("aside.mb-workspace-shell__left-panel").GetAttribute("aria-label"),
                    Is.EqualTo("Model navigation"));
                Assert.That(component.Find("div.mb-workspace-shell__main").GetAttribute("role"),
                    Is.EqualTo("region"));
                Assert.That(component.Find("div.mb-workspace-shell__main").GetAttribute("aria-label"),
                    Is.EqualTo("Diagram canvas"));
                Assert.That(component.FindAll("main"), Is.Empty);
                Assert.That(component.Find("aside.mb-workspace-shell__right-panel").GetAttribute("aria-label"),
                    Is.EqualTo("Element inspector"));
                Assert.That(component.Find(".mb-workspace-shell__status").GetAttribute("aria-label"),
                    Is.EqualTo("Diagram status"));
                Assert.That(component.Find(".mb-workspace-shell__overlay").TextContent.Trim(), Is.EqualTo("Overlay"));
                Assert.That(component.FindAll(".mb-workspace-shell__pane-button"), Has.Count.EqualTo(3));
            }
        }

        /// <summary>
        /// Verifies compact pane controls expose one active region without removing the other region content.
        /// </summary>
        [Test]
        public async Task VerifyNarrowPaneControlsRemainIndependent()
        {
            var component = this.Render<WorkspaceShellComponent>(parameters => parameters
                .Add(shell => shell.LeftPanel, "<span>Navigation</span>")
                .Add(shell => shell.MainContent, "<span>Canvas</span>")
                .Add(shell => shell.RightPanel, "<span>Details</span>")
                .Add(shell => shell.CompactLeftPanelLabel, "Navigation")
                .Add(shell => shell.CompactMainContentLabel, "Editors")
                .Add(shell => shell.CompactRightPanelLabel, "Auxiliary"));
            var paneButtons = component.FindAll(".mb-workspace-shell__pane-button");

            foreach (var paneButton in paneButtons)
            {
                var controlledRegionId = paneButton.GetAttribute("aria-controls");

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(controlledRegionId, Is.Not.Empty);
                    Assert.That(component.FindAll($"#{controlledRegionId}"), Has.Count.EqualTo(1));
                }
            }

            await paneButtons.Single(button => button.TextContent.Trim() == "Navigation").ClickAsync();
            paneButtons = component.FindAll(".mb-workspace-shell__pane-button");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(paneButtons.Single(button => button.TextContent.Trim() == "Navigation")
                    .GetAttribute("aria-pressed"), Is.EqualTo("true"));
                Assert.That(component.Find(".mb-workspace-shell__left-panel")
                    .GetAttribute("data-narrow-active"), Is.EqualTo("true"));
            }

            await paneButtons.Single(button => button.TextContent.Trim() == "Editors").ClickAsync();
            paneButtons = component.FindAll(".mb-workspace-shell__pane-button");

            Assert.That(paneButtons.Single(button => button.TextContent.Trim() == "Editors")
                .GetAttribute("aria-pressed"), Is.EqualTo("true"));

            await paneButtons.Single(button => button.TextContent.Trim() == "Auxiliary").ClickAsync();
            paneButtons = component.FindAll(".mb-workspace-shell__pane-button");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(paneButtons.Single(button => button.TextContent.Trim() == "Auxiliary")
                    .GetAttribute("aria-pressed"), Is.EqualTo("true"));
                Assert.That(component.Find(".mb-workspace-shell__right-panel")
                    .GetAttribute("data-narrow-active"), Is.EqualTo("true"));
                Assert.That(component.Find(".mb-workspace-shell__main")
                    .GetAttribute("data-narrow-active"), Is.EqualTo("false"));
                Assert.That(component.FindAll(".mb-workspace-shell__left-panel"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll(".mb-workspace-shell__main"), Has.Count.EqualTo(1));
            }
        }

        /// <summary>
        /// Verifies removing the active optional pane restores the compact view to the main content.
        /// </summary>
        [Test]
        public void VerifyRemovingActiveOptionalPaneSelectsMainContent()
        {
            var component = this.Render<WorkspaceShellComponent>(parameters => parameters
                .Add(shell => shell.LeftPanel, "<span>Navigation</span>")
                .Add(shell => shell.MainContent, "<span>Canvas</span>")
                .Add(shell => shell.RightPanel, "<span>Details</span>"));

            component.FindAll(".mb-workspace-shell__pane-button")
                .Single(button => button.TextContent.Trim() == "Navigation")
                .Click();
            component.Render(parameters => parameters.Add(shell => shell.LeftPanelVisible, false));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(".mb-workspace-shell__left-panel"), Is.Empty);
                Assert.That(component.FindAll(".mb-workspace-shell__pane-button")
                    .Single(button => button.TextContent.Trim() == "Canvas")
                    .GetAttribute("aria-pressed"), Is.EqualTo("true"));
            }

            component.FindAll(".mb-workspace-shell__pane-button")
                .Single(button => button.TextContent.Trim() == "Details")
                .Click();
            component.Render(parameters => parameters.Add(shell => shell.RightPanelVisible, false));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(".mb-workspace-shell__right-panel"), Is.Empty);
                Assert.That(component.FindAll(".mb-workspace-shell__pane-button"), Is.Empty);
                Assert.That(component.Find(".mb-workspace-shell__main")
                    .GetAttribute("data-narrow-active"), Is.EqualTo("true"));
            }
        }

        /// <summary>
        /// Verifies visibility parameters add and remove optional panels while preserving main content.
        /// </summary>
        [Test]
        public void VerifyPanelVisibilityChangesApply()
        {
            var component = this.Render<WorkspaceShellComponent>(parameters => parameters
                .Add(component => component.LeftPanel, "<span>Left</span>")
                .Add(component => component.MainContent, "<span>Main remains</span>")
                .Add(component => component.RightPanel, "<span>Right</span>")
                .Add(component => component.LeftPanelVisible, false)
                .Add(component => component.RightPanelVisible, false));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(".mb-workspace-shell__left-panel"), Is.Empty);
                Assert.That(component.FindAll(".mb-workspace-shell__right-panel"), Is.Empty);
                Assert.That(component.Find(".mb-workspace-shell__main").TextContent, Does.Contain("Main remains"));
            }

            component.Render(parameters => parameters
                .Add(component => component.LeftPanelVisible, true)
                .Add(component => component.RightPanelVisible, true));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(".mb-workspace-shell__left-panel"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll(".mb-workspace-shell__right-panel"), Has.Count.EqualTo(1));
                Assert.That(component.Find(".mb-workspace-shell__main").TextContent, Does.Contain("Main remains"));
            }
        }

        /// <summary>
        /// Verifies collapsed navigation state changes the shell width contract without hiding its content.
        /// </summary>
        [Test]
        public void VerifyLeftPanelCollapseStateAndCustomAttributesApply()
        {
            var component = this.Render<WorkspaceShellComponent>(parameters => parameters
                .Add(shell => shell.LeftPanel, "<span>Navigation remains</span>")
                .Add(shell => shell.MainContent, "<span>Canvas</span>")
                .Add(shell => shell.LeftPanelCollapsed, true)
                .Add(shell => shell.Class, "custom-shell")
                .AddUnmatched("data-testid", "workspace-shell"));
            var root = component.Find("section.mb-workspace-shell");
            var leftPanel = component.Find("aside.mb-workspace-shell__left-panel");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root.ClassList, Does.Contain("mb-workspace-shell--left-panel-collapsed"));
                Assert.That(root.ClassList, Does.Contain("custom-shell"));
                Assert.That(root.GetAttribute("data-testid"), Is.EqualTo("workspace-shell"));
                Assert.That(root.GetAttribute("data-navigation-collapsed"), Is.EqualTo("true"));
                Assert.That(leftPanel.GetAttribute("data-collapsed"), Is.EqualTo("true"));
                Assert.That(leftPanel.TextContent, Does.Contain("Navigation remains"));
            }

            component.Render(parameters => parameters.Add(shell => shell.LeftPanelCollapsed, false));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("section.mb-workspace-shell").ClassList,
                    Does.Not.Contain("mb-workspace-shell--left-panel-collapsed"));
                Assert.That(component.Find("section.mb-workspace-shell")
                    .GetAttribute("data-navigation-collapsed"), Is.EqualTo("false"));
                Assert.That(component.Find("aside.mb-workspace-shell__left-panel")
                    .GetAttribute("data-collapsed"), Is.EqualTo("false"));
            }
        }

        /// <summary>
        /// Verifies controlled collapse state overrides a conflicting unmatched semantic attribute.
        /// </summary>
        [Test]
        public void VerifyControlledCollapseStateOverridesConflictingUnmatchedAttribute()
        {
            var component = this.Render<WorkspaceShellComponent>(parameters => parameters
                .Add(shell => shell.MainContent, "<span>Canvas</span>")
                .Add(shell => shell.LeftPanelCollapsed, true)
                .AddUnmatched("data-navigation-collapsed", "false"));

            Assert.That(component.Find("section.mb-workspace-shell")
                .GetAttribute("data-navigation-collapsed"), Is.EqualTo("true"));
        }

        /// <summary>
        /// Verifies the full-application presentation remains explicit and preserves every optional shell region.
        /// </summary>
        [Test]
        public void VerifyFullApplicationPresentationStateApplies()
        {
            var component = this.Render<WorkspaceShellComponent>(parameters => parameters
                .Add(shell => shell.Header, "<span>Header</span>")
                .Add(shell => shell.LeftPanel, "<span>Navigation</span>")
                .Add(shell => shell.MainContent, "<span>Editor</span>")
                .Add(shell => shell.RightPanel, "<span>Auxiliary</span>")
                .Add(shell => shell.StatusBar, "<span>Status</span>")
                .Add(shell => shell.FullApplication, true)
                .Add(shell => shell.LeftPanelCollapsed, true)
                .Add(shell => shell.Class, "application-shell")
                .AddUnmatched("data-testid", "application-workspace"));
            var root = component.Find("section.mb-workspace-shell");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root.ClassList, Does.Contain("mb-workspace-shell--full-application"));
                Assert.That(root.ClassList, Does.Contain("mb-workspace-shell--left-panel-collapsed"));
                Assert.That(root.ClassList, Does.Contain("application-shell"));
                Assert.That(root.GetAttribute("data-testid"), Is.EqualTo("application-workspace"));
                Assert.That(root.GetAttribute("data-navigation-collapsed"), Is.EqualTo("true"));
                Assert.That(component.FindAll(".mb-workspace-shell__header"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll(".mb-workspace-shell__left-panel"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll(".mb-workspace-shell__main"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll(".mb-workspace-shell__right-panel"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll(".mb-workspace-shell__status"), Has.Count.EqualTo(1));
            }

            component.Render(parameters => parameters
                .Add(shell => shell.FullApplication, false)
                .Add(shell => shell.LeftPanelCollapsed, false));
            root = component.Find("section.mb-workspace-shell");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root.ClassList, Does.Not.Contain("mb-workspace-shell--full-application"));
                Assert.That(root.ClassList, Does.Not.Contain("mb-workspace-shell--left-panel-collapsed"));
                Assert.That(root.GetAttribute("data-navigation-collapsed"), Is.EqualTo("false"));
                Assert.That(component.FindAll(".mb-workspace-shell__left-panel"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll(".mb-workspace-shell__right-panel"), Has.Count.EqualTo(1));
            }
        }

        /// <summary>
        /// Verifies embedded and full-application styles retain their distinct size and overflow contracts.
        /// </summary>
        [Test]
        public void VerifyPresentationStyleContracts()
        {
            var style = File.ReadAllText(Path.Combine(
                TestRepository.GetRootPath(),
                "Mycelium.Bloom",
                "Components",
                "UI",
                "Organisms",
                "WorkspaceShell",
                "WorkspaceShell.razor.css"));
            var embeddedRule = GetStyleRule(style, ".mb-workspace-shell");
            var fullApplicationRule = GetStyleRule(style, ".mb-workspace-shell--full-application");
            var collapsedRule = GetStyleRule(
                style,
                ".mb-workspace-shell--left-panel-collapsed .mb-workspace-shell__body");
            var fullApplicationRegionsRule = GetStyleRule(
                style,
                ".mb-workspace-shell--full-application .mb-workspace-shell__header,");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(embeddedRule, Does.Contain("--mb-workspace-left-panel-collapsed-width: 3.5rem;"));
                Assert.That(embeddedRule, Does.Contain("min-width: 0;"));
                Assert.That(embeddedRule, Does.Contain("min-height: 0;"));
                Assert.That(embeddedRule, Does.Contain("overflow: hidden;"));
                Assert.That(embeddedRule, Does.Contain("border: 1px solid var(--mb-color-border-subtle);"));
                Assert.That(fullApplicationRule, Does.Contain("--mb-workspace-left-panel-collapsed-width: 52px;"));
                Assert.That(fullApplicationRule, Does.Contain("margin: 0;"));
                Assert.That(fullApplicationRule, Does.Contain("padding: 0;"));
                Assert.That(fullApplicationRule, Does.Contain("border: 0;"));
                Assert.That(fullApplicationRule, Does.Contain("border-radius: 0;"));
                Assert.That(collapsedRule,
                    Does.Contain("--mb-workspace-left-panel-width: var(--mb-workspace-left-panel-collapsed-width);"));
                Assert.That(fullApplicationRegionsRule, Does.Contain("overflow: hidden;"));
                Assert.That(style, Does.Contain("@media (prefers-reduced-motion: reduce)"));
            }
        }

        /// <summary>
        /// Verifies absent optional fragments leave no empty regions or grid items.
        /// </summary>
        [Test]
        public void VerifyAbsentOptionalRegionsAreOmitted()
        {
            var component = this.Render<WorkspaceShellComponent>(parameters => parameters
                .Add(component => component.MainContent, "<span>Main only</span>"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(".mb-workspace-shell__header"), Is.Empty);
                Assert.That(component.FindAll("aside"), Is.Empty);
                Assert.That(component.FindAll(".mb-workspace-shell__status"), Is.Empty);
                Assert.That(component.FindAll(".mb-workspace-shell__overlay"), Is.Empty);
                Assert.That(component.FindAll(".mb-workspace-shell__pane-switcher"), Is.Empty);
                Assert.That(component.Find(".mb-workspace-shell__main").TextContent.Trim(), Is.EqualTo("Main only"));
            }
        }

        /// <summary>
        /// Verifies separate shell instances retain independent panel visibility.
        /// </summary>
        [Test]
        public void VerifyInstancesRemainIndependent()
        {
            var hiddenPanels = this.Render<WorkspaceShellComponent>(parameters => parameters
                .Add(component => component.MainContent, "<span>First</span>")
                .Add(component => component.LeftPanel, "<span>First left</span>")
                .Add(component => component.LeftPanelVisible, false));
            var visiblePanels = this.Render<WorkspaceShellComponent>(parameters => parameters
                .Add(component => component.MainContent, "<span>Second</span>")
                .Add(component => component.LeftPanel, "<span>Second left</span>"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(hiddenPanels.FindAll("aside"), Is.Empty);
                Assert.That(visiblePanels.FindAll("aside"), Has.Count.EqualTo(1));
                Assert.That(hiddenPanels.Find(".mb-workspace-shell__main").TextContent, Does.Contain("First"));
                Assert.That(visiblePanels.Find(".mb-workspace-shell__main").TextContent, Does.Contain("Second"));
            }
        }

        /// <summary>
        /// Extracts a CSS declaration block for contract-level style assertions.
        /// </summary>
        /// <param name="style">The complete scoped stylesheet.</param>
        /// <param name="selector">The selector whose first declaration block should be returned.</param>
        /// <returns>The declaration block without its braces.</returns>
        private static string GetStyleRule(string style, string selector)
        {
            var selectorIndex = style.IndexOf(selector, StringComparison.Ordinal);
            Assert.That(selectorIndex, Is.GreaterThanOrEqualTo(0), $"Could not find CSS selector '{selector}'.");

            var openingBraceIndex = style.IndexOf('{', selectorIndex);
            Assert.That(openingBraceIndex, Is.GreaterThan(selectorIndex));

            var closingBraceIndex = style.IndexOf('}', openingBraceIndex);
            Assert.That(closingBraceIndex, Is.GreaterThan(openingBraceIndex));

            return style.Substring(openingBraceIndex + 1, closingBraceIndex - openingBraceIndex - 1);
        }
    }
}
