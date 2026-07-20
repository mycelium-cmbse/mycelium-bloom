// ------------------------------------------------------------------------------------------------
// <copyright file="StatusBarTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.StatusBar
{
    using Bunit;

    using StatusBarComponent = Mycelium.Bloom.Components.UI.Organisms.StatusBar.StatusBar;

    /// <summary>
    /// Tests the <see cref="StatusBarComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class StatusBarTestFixture : BunitContext
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
        /// Verifies all supplied regions, compact state, accessibility, and unmatched attributes.
        /// </summary>
        [Test]
        public void VerifySuppliedRegionsRenderConfiguredState()
        {
            var component = this.Render<StatusBarComponent>(parameters => parameters
                .Add(component => component.LeadingContent, "<span>Ready</span>")
                .Add(component => component.ChildContent, "<span>Selection: 2</span>")
                .Add(component => component.TrailingContent, "<button>Details</button>")
                .Add(component => component.Compact, true)
                .Add(component => component.AriaLabel, "Diagram status")
                .Add(component => component.Class, "custom-status")
                .AddUnmatched("data-testid", "status-bar"));

            var footer = component.Find("footer");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-status-bar__leading").TextContent.Trim(), Is.EqualTo("Ready"));
                Assert.That(component.Find(".mb-status-bar__content").TextContent.Trim(), Is.EqualTo("Selection: 2"));
                Assert.That(component.Find(".mb-status-bar__trailing").TextContent.Trim(), Is.EqualTo("Details"));
                Assert.That(footer.GetAttribute("aria-label"), Is.EqualTo("Diagram status"));
                Assert.That(footer.ClassList, Does.Contain("mb-status-bar--compact"));
                Assert.That(footer.ClassList, Does.Contain("custom-status"));
                Assert.That(footer.GetAttribute("data-testid"), Is.EqualTo("status-bar"));
            }
        }

        /// <summary>
        /// Verifies optional status regions are omitted when no content is supplied.
        /// </summary>
        [Test]
        public void VerifyAbsentRegionsAreOmitted()
        {
            var component = this.Render<StatusBarComponent>(parameters => parameters
                .Add(component => component.ChildContent, "<span>Central status</span>"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(".mb-status-bar__leading"), Is.Empty);
                Assert.That(component.FindAll(".mb-status-bar__content"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll(".mb-status-bar__trailing"), Is.Empty);
            }
        }
    }
}
