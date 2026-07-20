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
    using Bunit;

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
                Assert.That(component.Find("main.mb-workspace-shell__main").GetAttribute("aria-label"),
                    Is.EqualTo("Diagram canvas"));
                Assert.That(component.Find("aside.mb-workspace-shell__right-panel").GetAttribute("aria-label"),
                    Is.EqualTo("Element inspector"));
                Assert.That(component.Find(".mb-workspace-shell__status").GetAttribute("aria-label"),
                    Is.EqualTo("Diagram status"));
                Assert.That(component.Find(".mb-workspace-shell__overlay").TextContent.Trim(), Is.EqualTo("Overlay"));
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
                Assert.That(component.Find("main").TextContent, Does.Contain("Main remains"));
            }

            component.Render(parameters => parameters
                .Add(component => component.LeftPanelVisible, true)
                .Add(component => component.RightPanelVisible, true));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(".mb-workspace-shell__left-panel"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll(".mb-workspace-shell__right-panel"), Has.Count.EqualTo(1));
                Assert.That(component.Find("main").TextContent, Does.Contain("Main remains"));
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
                Assert.That(component.Find("main").TextContent.Trim(), Is.EqualTo("Main only"));
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
                Assert.That(hiddenPanels.Find("main").TextContent, Does.Contain("First"));
                Assert.That(visiblePanels.Find("main").TextContent, Does.Contain("Second"));
            }
        }
    }
}
