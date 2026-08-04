// ------------------------------------------------------------------------------------------------
// <copyright file="NavMenuTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.Layout
{
    using System;
    using System.IO;

    using Bunit;

    using Mycelium.Bloom.Tests.Common;

    using NavMenuComponent = Mycelium.Bloom.Components.Layout.NavMenu;

    /// <summary>
    /// Tests the primary navigation source contracts.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class NavMenuTestFixture : BunitContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NavMenuTestFixture" /> class.
        /// </summary>
        public NavMenuTestFixture()
        {
            BlueprintTestSetup.Configure(this);
        }

        /// <summary>
        /// Disposes the bUnit test context after each test.
        /// </summary>
        [TearDown]
        public System.Threading.Tasks.Task TearDown()
        {
            return this.DisposeAsync().AsTask();
        }

        /// <summary>
        /// Verifies that the disclosure controls the named navigation and closes after link activation.
        /// </summary>
        [Test]
        public void VerifyNavigationDisclosureRelationshipsAndCloseBehavior()
        {
            var component = this.Render<NavMenuComponent>();
            var toggle = component.Find("button.mb-nav-menu__toggle");
            var navigation = component.Find("nav[aria-label='Primary navigation']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(toggle.GetAttribute("aria-controls"), Is.EqualTo(navigation.Id));
                Assert.That(toggle.GetAttribute("aria-expanded"), Is.EqualTo("false"));
                Assert.That(toggle.GetAttribute("aria-label"), Is.EqualTo("Open primary navigation"));
            }

            toggle.Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(toggle.GetAttribute("aria-expanded"), Is.EqualTo("true"));
                Assert.That(toggle.GetAttribute("aria-label"), Is.EqualTo("Close primary navigation"));
                Assert.That(navigation.ClassList, Does.Contain("mb-nav-menu__links--expanded"));
            }

            component.Find("a[href='design-system']").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(toggle.GetAttribute("aria-expanded"), Is.EqualTo("false"));
                Assert.That(toggle.GetAttribute("aria-label"), Is.EqualTo("Open primary navigation"));
                Assert.That(navigation.ClassList, Does.Not.Contain("mb-nav-menu__links--expanded"));
            }
        }

        /// <summary>
        /// Verifies that reduced motion crosses the NavLink component boundary and removes the transition.
        /// </summary>
        [Test]
        public void VerifyReducedMotionTargetsRenderedNavLinks()
        {
            var stylePath = Path.Combine(
                TestRepository.GetRootPath(),
                "Mycelium.Bloom",
                "Components",
                "Layout",
                "NavMenu.razor.css");
            var styles = File.ReadAllText(stylePath);
            var reducedMotionStart = styles.IndexOf("@media (prefers-reduced-motion: reduce)", StringComparison.Ordinal);

            Assert.That(reducedMotionStart, Is.GreaterThanOrEqualTo(0));

            var reducedMotionStyles = styles[reducedMotionStart..];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reducedMotionStyles, Does.Contain(".mb-nav-menu__links ::deep .mb-nav-menu__link"));
                Assert.That(reducedMotionStyles, Does.Contain("transition: none;"));
            }
        }
    }
}
