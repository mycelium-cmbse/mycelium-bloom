// ------------------------------------------------------------------------------------------------
// <copyright file="UserMenuTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Molecules.UserMenu
{
    using Bunit;

    using Mycelium.Bloom.Model;

    using AvatarComponent = Mycelium.Bloom.Components.UI.Atoms.Avatar.Avatar;
    using UserMenuComponent = Mycelium.Bloom.Components.UI.Molecules.UserMenu.UserMenu;

    /// <summary>
    /// Tests the <see cref="UserMenuComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class UserMenuTestFixture : BunitContext
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
        /// Verifies that Avatar and configured user information render in the trigger.
        /// </summary>
        [Test]
        public void VerifyUserIdentityRenders()
        {
            var component = this.Render<UserMenuComponent>(parameters => parameters
                .Add(component => component.DisplayName, "Alex Morgan")
                .Add(component => component.Subtitle, "alex@example.test")
                .Add(component => component.AvatarText, "AM")
                .Add(component => component.Items, this.CreateItems()));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindComponent<AvatarComponent>().Instance.Text, Is.EqualTo("AM"));
                Assert.That(component.Find(".mb-user-menu__name").TextContent, Is.EqualTo("Alex Morgan"));
                Assert.That(component.Find(".mb-user-menu__subtitle").TextContent, Is.EqualTo("alex@example.test"));
                Assert.That(component.Find("button").GetAttribute("aria-label"),
                    Is.EqualTo("Open user menu for Alex Morgan"));
            }
        }

        /// <summary>
        /// Verifies that opening the menu renders supplied actions and forwards selection.
        /// </summary>
        [Test]
        public void VerifyActionSelectionIsForwarded()
        {
            ActionMenuItem selectedItem = null;
            var items = this.CreateItems();

            var component = this.Render<UserMenuComponent>(parameters => parameters
                .Add(component => component.DisplayName, "Alex Morgan")
                .Add(component => component.Items, items)
                .Add(component => component.ItemSelected, item => selectedItem = item));

            component.Find("button").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("[role='menuitem']"), Has.Count.EqualTo(2));
                Assert.That(component.FindAll("[role='menuitem']")[0].TextContent, Does.Contain("Profile"));
            }

            component.FindAll("[role='menuitem']")[1].Click();

            Assert.That(selectedItem, Is.SameAs(items[1]));
        }

        /// <summary>
        /// Verifies that compact mode omits user text while preserving the Avatar trigger.
        /// </summary>
        [Test]
        public void VerifyCompactModeRendersAvatarOnly()
        {
            var component = this.Render<UserMenuComponent>(parameters => parameters
                .Add(component => component.DisplayName, "Alex Morgan")
                .Add(component => component.Compact, true)
                .Add(component => component.Items, this.CreateItems()));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-user-menu").GetAttribute("class"),
                    Does.Contain("mb-user-menu--compact"));
                Assert.That(component.FindAll(".mb-user-menu__identity"), Is.Empty);
                Assert.That(component.FindComponent<AvatarComponent>().Instance.Text, Is.EqualTo("AM"));
            }
        }

        /// <summary>
        /// Verifies that separate user-menu instances do not share menu state.
        /// </summary>
        [Test]
        public void VerifyInstancesMaintainIndependentMenuState()
        {
            var first = this.Render<UserMenuComponent>(parameters => parameters
                .Add(component => component.DisplayName, "Alex Morgan")
                .Add(component => component.Items, this.CreateItems()));
            var second = this.Render<UserMenuComponent>(parameters => parameters
                .Add(component => component.DisplayName, "Jordan Lee")
                .Add(component => component.Items, this.CreateItems()));

            first.Find("button").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.FindAll("[role='menu']"), Has.Count.EqualTo(1));
                Assert.That(second.FindAll("[role='menu']"), Is.Empty);
            }
        }

        /// <summary>
        /// Creates standard user-menu actions.
        /// </summary>
        /// <returns>The user-menu actions.</returns>
        private ActionMenuItem[] CreateItems()
        {
            return
            [
                new ActionMenuItem { Id = "profile", Label = "Profile" },
                new ActionMenuItem { Id = "sign-out", Label = "Sign out" }
            ];
        }
    }
}
