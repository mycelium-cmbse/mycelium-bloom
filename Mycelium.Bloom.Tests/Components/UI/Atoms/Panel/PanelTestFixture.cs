// ------------------------------------------------------------------------------------------------
// <copyright file="PanelTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Atoms.Panel
{
    using Bunit;

    using Mycelium.Bloom.Model;

    using PanelComponent = Mycelium.Bloom.Components.UI.Atoms.Panel.Panel;

    /// <summary>
    /// Tests the <see cref="PanelComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class PanelTestFixture : BunitContext
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
        /// Verifies that the panel displays configured content, classes, and attributes.
        /// </summary>
        [Test]
        public void Render_DisplaysConfiguredPanel()
        {
            var component = this.Render<PanelComponent>(parameters => parameters
                .Add(component => component.Padding, PanelPadding.Large)
                .Add(component => component.FullHeight, true)
                .Add(component => component.OverflowHidden, true)
                .Add(component => component.Class, "custom-panel")
                .AddChildContent("<p>Panel content</p>")
                .AddUnmatched("data-testid", "detail-panel"));

            var panel = component.Find("section");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(panel.GetAttribute("data-testid"), Is.EqualTo("detail-panel"));
                Assert.That(panel.GetAttribute("class"), Does.Contain("mb-panel--padding-large"));
                Assert.That(panel.GetAttribute("class"), Does.Contain("mb-panel--full-height"));
                Assert.That(panel.GetAttribute("class"), Does.Contain("mb-panel--overflow-hidden"));
                Assert.That(panel.GetAttribute("class"), Does.Contain("custom-panel"));
                Assert.That(component.Find("p").TextContent, Is.EqualTo("Panel content"));
            }
        }

        /// <summary>
        /// Verifies that the panel uses the expected padding class.
        /// </summary>
        /// <param name="padding">The panel padding.</param>
        /// <param name="expectedCssClass">The expected CSS class.</param>
        [TestCase(PanelPadding.None, "mb-panel--padding-none")]
        [TestCase(PanelPadding.Small, "mb-panel--padding-small")]
        [TestCase(PanelPadding.Medium, "mb-panel--padding-medium")]
        [TestCase(PanelPadding.Large, "mb-panel--padding-large")]
        public void Render_UsesExpectedPaddingClass(PanelPadding padding, string expectedCssClass)
        {
            var component = this.Render<PanelComponent>(parameters => parameters
                .Add(component => component.Padding, padding)
                .AddChildContent("Panel content"));

            Assert.That(component.Find("section").GetAttribute("class"), Does.Contain(expectedCssClass));
        }
    }
}
