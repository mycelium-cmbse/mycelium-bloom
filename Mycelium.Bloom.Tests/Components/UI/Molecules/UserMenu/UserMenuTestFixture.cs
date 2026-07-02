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

    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;

    using UserMenuComponent = Mycelium.Bloom.Components.UI.Molecules.UserMenu.UserMenu;

    /// <summary>
    /// Tests the <see cref="UserMenuComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class UserMenuTestFixture : KeyboardNavigationBunitContext
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
        /// Verifies that the user menu displays configured user identity and item data.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysConfiguredUserMenu()
        {
            var component = this.Render<UserMenuComponent>(parameters => parameters
                .Add(component => component.UserName, "Omrane Haj Mohamed")
                .Add(component => component.UserInitials, "OH")
                .Add(component => component.UserEmail, "omrane@example.com")
                .Add(component => component.UserRole, "Project Admin")
                .Add(component => component.UserColor, "#123456")
                .Add(component => component.Items, new[]
                {
                    new ActionMenuItem { Value = "profile", Label = "Profile", Description = "View account details", Icon = "P" },
                    new ActionMenuItem { Value = "sign-out", Label = "Sign out", SeparatorBefore = true, Variant = ActionMenuItemVariant.Danger }
                })
                .Add(component => component.Class, "custom-user-menu")
                .AddUnmatched("data-testid", "user-menu"));

            var trigger = component.Find(".mb-user-menu__trigger");

            trigger.Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-user-menu").GetAttribute("data-testid"), Is.EqualTo("user-menu"));
                Assert.That(component.Find(".mb-user-menu").GetAttribute("class"), Does.Contain("custom-user-menu"));
                Assert.That(trigger.GetAttribute("aria-haspopup"), Is.EqualTo("menu"));
                Assert.That(trigger.GetAttribute("aria-expanded"), Is.EqualTo("true"));
                Assert.That(component.Find(".mb-user-menu__name").TextContent.Trim(), Is.EqualTo("Omrane Haj Mohamed"));
                Assert.That(component.Find(".mb-user-menu__meta").TextContent.Trim(), Is.EqualTo("Project Admin"));
                Assert.That(component.Find(".mb-user-menu__summary-email").TextContent.Trim(), Is.EqualTo("omrane@example.com"));
                Assert.That(component.Find(".mb-user-menu__summary-role").TextContent.Trim(), Is.EqualTo("Project Admin"));
                Assert.That(component.FindAll("[role='menuitem']"), Has.Count.EqualTo(2));
                Assert.That(component.FindAll("[role='menuitem']")[1].GetAttribute("class"), Does.Contain("mb-user-menu__item--danger"));
                Assert.That(component.FindAll("[role='menuitem']")[1].GetAttribute("class"), Does.Contain("mb-user-menu__item--separator"));
            }
        }

        /// <summary>
        /// Verifies that selecting an enabled item invokes the callback and closes the menu.
        /// </summary>
        [Test]
        public void VerifyClickEnabledItemInvokesItemSelectedAndClosesMenu()
        {
            var selectedValue = string.Empty;

            var component = this.Render<UserMenuComponent>(parameters => parameters
                .Add(component => component.Items, new[]
                {
                    new ActionMenuItem { Value = "profile", Label = "Profile" }
                })
                .Add(component => component.ItemSelected, item => selectedValue = item.Value));

            component.Find(".mb-user-menu__trigger").Click();
            component.Find("[role='menuitem']").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectedValue, Is.EqualTo("profile"));
                Assert.That(component.Find(".mb-user-menu__trigger").GetAttribute("aria-expanded"), Is.EqualTo("false"));
                Assert.That(component.FindAll(".mb-user-menu__menu"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies that disabled items are rendered as disabled and do not invoke item selection.
        /// </summary>
        [Test]
        public void VerifyClickDisabledItemDoesNotInvokeItemSelected()
        {
            var selectedValue = string.Empty;

            var component = this.Render<UserMenuComponent>(parameters => parameters
                .Add(component => component.Items, new[]
                {
                    new ActionMenuItem { Value = "preferences", Label = "Preferences", Disabled = true }
                })
                .Add(component => component.ItemSelected, item => selectedValue = item.Value));

            component.Find(".mb-user-menu__trigger").Click();

            var menuItem = component.Find("[role='menuitem']");

            menuItem.Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectedValue, Is.Empty);
                Assert.That(menuItem.HasAttribute("disabled"), Is.True);
                Assert.That(menuItem.GetAttribute("class"), Does.Contain("mb-user-menu__item--disabled"));
                Assert.That(component.Find(".mb-user-menu__trigger").GetAttribute("aria-expanded"), Is.EqualTo("true"));
            }
        }

        /// <summary>
        /// Verifies that the compact trigger hides the user text when configured.
        /// </summary>
        [Test]
        public void VerifyRenderHidesUserTextWhenConfigured()
        {
            var component = this.Render<UserMenuComponent>(parameters => parameters
                .Add(component => component.UserName, "Omrane Haj Mohamed")
                .Add(component => component.UserInitials, "OH")
                .Add(component => component.ShowUserText, false));

            Assert.That(component.FindAll(".mb-user-menu__identity"), Is.Empty);
        }

        /// <summary>
        /// Verifies that the trigger opens the menu with arrow-key navigation.
        /// </summary>
        [Test]
        public void VerifyTriggerArrowDownOpensMenu()
        {
            var component = this.Render<UserMenuComponent>(parameters => parameters
                .Add(component => component.Items, new[]
                {
                    new ActionMenuItem { Value = "profile", Label = "Profile" },
                    new ActionMenuItem { Value = "preferences", Label = "Preferences" }
                }));

            component.Find(".mb-user-menu__trigger").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-user-menu__trigger").GetAttribute("aria-expanded"), Is.EqualTo("true"));
                Assert.That(component.FindAll("[role='menuitem']"), Has.Count.EqualTo(2));
            }
        }

        /// <summary>
        /// Verifies that pressing Escape on a menu item closes the menu.
        /// </summary>
        [Test]
        public void VerifyItemEscapeClosesMenu()
        {
            var component = this.Render<UserMenuComponent>(parameters => parameters
                .Add(component => component.Items, new[]
                {
                    new ActionMenuItem { Value = "profile", Label = "Profile" }
                }));

            component.Find(".mb-user-menu__trigger").Click();
            component.Find("[role='menuitem']").KeyDown(new KeyboardEventArgs { Key = "Escape" });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-user-menu__trigger").GetAttribute("aria-expanded"), Is.EqualTo("false"));
                Assert.That(component.FindAll(".mb-user-menu__menu"), Is.Empty);
            }
        }
    }
}
