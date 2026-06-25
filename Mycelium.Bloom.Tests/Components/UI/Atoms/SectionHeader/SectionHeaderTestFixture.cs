// ------------------------------------------------------------------------------------------------
// <copyright file="SectionHeaderTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Atoms.SectionHeader
{
    using Bunit;

    using SectionHeaderComponent = Mycelium.Bloom.Components.UI.Atoms.SectionHeader.SectionHeader;

    /// <summary>
    /// Tests the <see cref="SectionHeaderComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class SectionHeaderTestFixture : BunitContext
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
        /// Verifies that the section header displays action content when provided.
        /// </summary>
        [Test]
        public void Render_DisplaysActionsWhenProvided()
        {
            var component = this.Render<SectionHeaderComponent>(parameters => parameters
                .Add(component => component.Label, "Properties")
                .Add(component => component.Actions, "<button type=\"button\">Add</button>"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-section-header__actions button").TextContent, Is.EqualTo("Add"));
                Assert.That(component.FindAll(".mb-section-header__actions"), Has.Count.EqualTo(1));
            }
        }

        /// <summary>
        /// Verifies that the section header displays the configured label, classes, and attributes.
        /// </summary>
        [Test]
        public void Render_DisplaysConfiguredSectionHeader()
        {
            var component = this.Render<SectionHeaderComponent>(parameters => parameters
                .Add(component => component.Label, "Properties")
                .Add(component => component.Class, "custom-section-header")
                .AddUnmatched("data-testid", "section-header"));

            var header = component.Find(".mb-section-header");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(header.GetAttribute("data-testid"), Is.EqualTo("section-header"));
                Assert.That(header.GetAttribute("class"), Does.Contain("custom-section-header"));
                Assert.That(component.Find(".mb-section-header__label").TextContent.Trim(), Is.EqualTo("Properties"));
                Assert.That(component.FindAll(".mb-section-header__actions"), Is.Empty);
            }
        }
    }
}
