// ------------------------------------------------------------------------------------------------
// <copyright file="ActionMenuTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Molecules.ActionMenu
{
    using System.Collections.Generic;

    using Bunit;

    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Tests.Common;

    using ActionMenuComponent = Mycelium.Bloom.Components.UI.Molecules.ActionMenu.ActionMenu;

    /// <summary>
    /// Tests the <see cref="ActionMenuComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ActionMenuTestFixture : KeyboardNavigationBunitContext
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
        /// Verifies that the trigger opens the menu with arrow-key navigation.
        /// </summary>
        [Test]
        public void VerifyTriggerArrowDownOpensMenu()
        {
            var component = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(component => component.Items, GetItems()));

            component.Find(".mb-action-menu__trigger").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-action-menu__trigger").GetAttribute("aria-expanded"), Is.EqualTo("true"));
                Assert.That(component.FindAll("[role='menuitem']"), Has.Count.EqualTo(3));
            }
        }

        /// <summary>
        /// Verifies that pressing Escape on a menu item closes the menu.
        /// </summary>
        [Test]
        public void VerifyItemEscapeClosesMenu()
        {
            var component = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(component => component.Items, GetItems()));

            component.Find(".mb-action-menu__trigger").Click();
            component.Find("[role='menuitem']").KeyDown(new KeyboardEventArgs { Key = "Escape" });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-action-menu__trigger").GetAttribute("aria-expanded"), Is.EqualTo("false"));
                Assert.That(component.FindAll(".mb-action-menu__menu"), Is.Empty);
            }
        }

        /// <summary>
        /// Gets sample action menu items.
        /// </summary>
        /// <returns>The sample action menu items.</returns>
        private static IReadOnlyList<ActionMenuItem> GetItems()
        {
            return
            [
                new() { Value = "profile", Label = "Profile" },
                new() { Value = "disabled", Label = "Disabled", Disabled = true },
                new() { Value = "preferences", Label = "Preferences" }
            ];
        }
    }
}
