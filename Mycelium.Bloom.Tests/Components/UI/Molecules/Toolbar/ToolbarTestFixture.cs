// ------------------------------------------------------------------------------------------------
// <copyright file="ToolbarTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Molecules.Toolbar
{
    using Bunit;

    using ToolbarComponent = Mycelium.Bloom.Components.UI.Molecules.Toolbar.Toolbar;

    /// <summary>
    /// Tests the <see cref="ToolbarComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ToolbarTestFixture : BunitContext
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
        /// Verifies composed content, compact state, accessibility, and unmatched attributes.
        /// </summary>
        [Test]
        public void VerifyComposedToolbarRendersConfiguredState()
        {
            var component = this.Render<ToolbarComponent>(parameters => parameters
                .Add(component => component.LeadingContent, "<span>Leading</span>")
                .Add(component => component.ChildContent, "<span>Main</span>")
                .Add(component => component.TrailingContent, "<span>Trailing</span>")
                .Add(component => component.Compact, true)
                .Add(component => component.AllowWrap, false)
                .Add(component => component.AriaLabel, "Editor actions")
                .Add(component => component.Class, "custom-toolbar")
                .AddUnmatched("data-testid", "toolbar"));

            var toolbar = component.Find("[role='toolbar']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-toolbar__leading").TextContent.Trim(), Is.EqualTo("Leading"));
                Assert.That(component.Find(".mb-toolbar__main").TextContent.Trim(), Is.EqualTo("Main"));
                Assert.That(component.Find(".mb-toolbar__trailing").TextContent.Trim(), Is.EqualTo("Trailing"));
                Assert.That(toolbar.GetAttribute("aria-label"), Is.EqualTo("Editor actions"));
                Assert.That(toolbar.GetAttribute("class"), Does.Contain("mb-toolbar--compact"));
                Assert.That(toolbar.GetAttribute("class"), Does.Not.Contain("mb-toolbar--wrap"));
                Assert.That(toolbar.GetAttribute("class"), Does.Contain("custom-toolbar"));
                Assert.That(toolbar.GetAttribute("data-testid"), Is.EqualTo("toolbar"));
            }
        }
    }
}
