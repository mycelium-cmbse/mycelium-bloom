// ------------------------------------------------------------------------------------------------
// <copyright file="AppHeaderTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.AppHeader
{
    using Bunit;

    using AppHeaderComponent = Mycelium.Bloom.Components.UI.Organisms.AppHeader.AppHeader;

    /// <summary>
    /// Tests the <see cref="AppHeaderComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class AppHeaderTestFixture : BunitContext
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
            var component = this.Render<AppHeaderComponent>(parameters => parameters
                .Add(component => component.BrandContent, "<span>Bloom</span>")
                .Add(component => component.NavigationContent, "<span>Navigate</span>")
                .Add(component => component.ContextContent, "<span>Architecture</span>")
                .Add(component => component.ProjectContent, "<span>Project</span>")
                .Add(component => component.CenterContent, "<span>Search</span>")
                .Add(component => component.ActionsContent, "<span>Actions</span>")
                .Add(component => component.UserContent, "<span>User</span>")
                .Add(component => component.Compact, true)
                .Add(component => component.AriaLabel, "Model workspace header")
                .Add(component => component.NavigationAriaLabel, "Primary workspace navigation")
                .Add(component => component.Class, "custom-header")
                .AddUnmatched("data-testid", "app-header"));

            var header = component.Find("header");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-app-header__brand").TextContent.Trim(), Is.EqualTo("Bloom"));
                Assert.That(component.Find(".mb-app-header__navigation").TextContent.Trim(), Is.EqualTo("Navigate"));
                Assert.That(component.Find(".mb-app-header__context").TextContent.Trim(), Is.EqualTo("Architecture"));
                Assert.That(component.Find(".mb-app-header__project").TextContent.Trim(), Is.EqualTo("Project"));
                Assert.That(component.Find(".mb-app-header__center").TextContent.Trim(), Is.EqualTo("Search"));
                Assert.That(component.Find(".mb-app-header__actions").TextContent.Trim(), Is.EqualTo("Actions"));
                Assert.That(component.Find(".mb-app-header__user").TextContent.Trim(), Is.EqualTo("User"));
                Assert.That(header.GetAttribute("aria-label"), Is.EqualTo("Model workspace header"));
                Assert.That(component.Find("nav").GetAttribute("aria-label"), Is.EqualTo("Primary workspace navigation"));
                Assert.That(header.ClassList, Does.Contain("mb-app-header--compact"));
                Assert.That(header.ClassList, Does.Contain("custom-header"));
                Assert.That(header.GetAttribute("data-testid"), Is.EqualTo("app-header"));
            }
        }

        /// <summary>
        /// Verifies optional regions are omitted when no content is supplied.
        /// </summary>
        [Test]
        public void VerifyAbsentRegionsAreOmitted()
        {
            var component = this.Render<AppHeaderComponent>(parameters => parameters
                .Add(component => component.BrandContent, "<span>Bloom</span>"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(".mb-app-header__brand"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll(".mb-app-header__navigation"), Is.Empty);
                Assert.That(component.FindAll(".mb-app-header__context"), Is.Empty);
                Assert.That(component.FindAll(".mb-app-header__project"), Is.Empty);
                Assert.That(component.FindAll(".mb-app-header__center"), Is.Empty);
                Assert.That(component.FindAll(".mb-app-header__actions"), Is.Empty);
                Assert.That(component.FindAll(".mb-app-header__user"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies separate header instances retain their own presentation parameters.
        /// </summary>
        [Test]
        public void VerifyInstancesRemainIndependent()
        {
            var compactHeader = this.Render<AppHeaderComponent>(parameters => parameters
                .Add(component => component.Compact, true)
                .Add(component => component.AriaLabel, "Compact header"));
            var standardHeader = this.Render<AppHeaderComponent>(parameters => parameters
                .Add(component => component.AriaLabel, "Standard header"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(compactHeader.Find("header").ClassList, Does.Contain("mb-app-header--compact"));
                Assert.That(standardHeader.Find("header").ClassList, Does.Not.Contain("mb-app-header--compact"));
                Assert.That(compactHeader.Find("header").GetAttribute("aria-label"), Is.EqualTo("Compact header"));
                Assert.That(standardHeader.Find("header").GetAttribute("aria-label"), Is.EqualTo("Standard header"));
            }
        }
    }
}
