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
    using System.Threading.Tasks;

    using BlazorBlueprint.Primitives.Services;

    using Bunit;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;

    using UserMenuComponent = Mycelium.Bloom.Components.UI.Molecules.UserMenu.UserMenu;

    /// <summary>
    /// Tests Bloom user identity and account actions composed with a styled Blueprint menu.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class UserMenuTestFixture : BunitContext
    {
        private readonly IRenderedComponent<BbPortalHost> portalHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserMenuTestFixture" /> class.
        /// </summary>
        public UserMenuTestFixture()
        {
            this.portalHost = BlueprintTestSetup.ConfigureWithPortalHost(this);
        }

        /// <summary>
        /// Disposes the bUnit test context after each test.
        /// </summary>
        [TearDown]
        public Task TearDown()
        {
            return this.DisposeAsync().AsTask();
        }

        /// <summary>
        /// Verifies the trigger renders configured user identity and an accessible name.
        /// </summary>
        [Test]
        public void VerifyUserIdentityRenders()
        {
            var component = this.Render<UserMenuComponent>(parameters => parameters
                .Add(menu => menu.DisplayName, "Alex Morgan")
                .Add(menu => menu.Subtitle, "alex@example.test")
                .Add(menu => menu.AvatarText, "AM")
                .Add(menu => menu.AvatarBackgroundColor, "teal")
                .Add(menu => menu.AvatarBorderColor, "navy")
                .Add(menu => menu.Items, CreateItems()));

            var avatar = component.Find(".mb-user-menu__avatar-frame");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-user-menu__avatar-fallback").TextContent.Trim(), Is.EqualTo("AM"));
                Assert.That(avatar.GetAttribute("title"), Is.EqualTo("Alex Morgan"));
                Assert.That(avatar.GetAttribute("style"), Does.Contain("--mb-user-avatar-background: teal"));
                Assert.That(avatar.GetAttribute("style"), Does.Contain("--mb-user-avatar-border: navy"));
                Assert.That(component.Find(".mb-user-menu__name").TextContent, Is.EqualTo("Alex Morgan"));
                Assert.That(component.Find(".mb-user-menu__subtitle").TextContent, Is.EqualTo("alex@example.test"));
                Assert.That(component.Find("button").GetAttribute("aria-label"),
                    Is.EqualTo("Open user menu for Alex Morgan"));
                Assert.That(component.FindAll(".mb-action-menu__chevron svg"), Has.Count.EqualTo(1));
            }
        }

        /// <summary>
        /// Verifies styled actions, grouping, and exactly-once selection forwarding.
        /// </summary>
        [Test]
        public async Task VerifyActionSelectionIsForwarded()
        {
            ActionMenuItem selectedItem = null;
            var callbackCount = 0;
            var items = CreateItems();
            var component = this.Render<UserMenuComponent>(parameters => parameters
                .Add(menu => menu.DisplayName, "Alex Morgan")
                .Add(menu => menu.Items, items)
                .Add(menu => menu.ItemSelected, item =>
                {
                    selectedItem = item;
                    callbackCount++;
                }));

            await component.Find("button").ClickAsync();
            var menuItems = this.portalHost.WaitForElements("[role='menuitem']", items.Length);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.portalHost.FindAll("[role='separator']"), Has.Count.EqualTo(1));
                Assert.That(menuItems[0].TextContent, Does.Contain("Profile"));
                Assert.That(menuItems[1].TextContent, Does.Contain("Preferences"));
                Assert.That(menuItems[2].TextContent, Does.Contain("Sign out"));
                Assert.That(menuItems[2].ClassList, Does.Contain("text-destructive"));
                Assert.That(menuItems[2].TextContent, Does.Contain("Destructive action"));
            }

            await menuItems[1].ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectedItem, Is.SameAs(items[1]));
                Assert.That(callbackCount, Is.EqualTo(1));
                Assert.That(component.Find("button").GetAttribute("aria-expanded"), Is.EqualTo("false"));
            }
        }

        /// <summary>
        /// Verifies compact mode preserves the avatar trigger while omitting visible identity text.
        /// </summary>
        [Test]
        public void VerifyCompactModeRendersAvatarOnly()
        {
            var component = this.Render<UserMenuComponent>(parameters => parameters
                .Add(menu => menu.DisplayName, "Alex Morgan")
                .Add(menu => menu.Compact, true)
                .Add(menu => menu.Items, CreateItems()));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-user-menu").ClassList, Does.Contain("mb-user-menu--compact"));
                Assert.That(component.FindAll(".mb-user-menu__identity"), Is.Empty);
                Assert.That(component.Find(".mb-user-menu__avatar-fallback").TextContent.Trim(), Is.EqualTo("AM"));
                Assert.That(component.FindAll(".mb-action-menu__chevron"), Has.Count.EqualTo(1));
            }
        }

        /// <summary>
        /// Verifies an explicit accessible label overrides the generated label.
        /// </summary>
        [Test]
        public void VerifyExplicitMenuAriaLabelRenders()
        {
            var component = this.Render<UserMenuComponent>(parameters => parameters
                .Add(menu => menu.DisplayName, "Alex Morgan")
                .Add(menu => menu.MenuAriaLabel, "Account actions")
                .Add(menu => menu.Items, CreateItems()));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("button").GetAttribute("aria-label"), Is.EqualTo("Account actions"));
                Assert.That(component.Find("button").GetAttribute("title"), Is.EqualTo("Account actions"));
            }
        }

        /// <summary>
        /// Verifies long identity values remain inspectable while the visible layout can truncate them.
        /// </summary>
        [Test]
        public void VerifyLongIdentityValuesRemainAvailable()
        {
            var displayName = "Alexandra Morgan with an intentionally long display name";
            var subtitle = "alexandra.morgan.with.a.long.address@example.test";
            var component = this.Render<UserMenuComponent>(parameters => parameters
                .Add(menu => menu.DisplayName, displayName)
                .Add(menu => menu.Subtitle, subtitle)
                .Add(menu => menu.Items, CreateItems()));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-user-menu__name").GetAttribute("title"), Is.EqualTo(displayName));
                Assert.That(component.Find(".mb-user-menu__subtitle").GetAttribute("title"), Is.EqualTo(subtitle));
                Assert.That(component.Find("button").GetAttribute("aria-label"), Does.Contain(displayName));
            }
        }

        /// <summary>
        /// Verifies a disabled user menu cannot open or invoke actions.
        /// </summary>
        [Test]
        public async Task VerifyDisabledMenuRemainsClosed()
        {
            var callbackCount = 0;
            var component = this.Render<UserMenuComponent>(parameters => parameters
                .Add(menu => menu.DisplayName, "Alex Morgan")
                .Add(menu => menu.Items, CreateItems())
                .Add(menu => menu.Disabled, true)
                .Add(menu => menu.ItemSelected, _ => callbackCount++));
            var trigger = component.Find("button");

            await trigger.ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(trigger.GetAttribute("aria-disabled"), Is.EqualTo("true"));
                Assert.That(trigger.GetAttribute("aria-expanded"), Is.EqualTo("false"));
                Assert.That(this.portalHost.FindAll("[role='menu']"), Is.Empty);
                Assert.That(callbackCount, Is.Zero);
            }
        }

        private static ActionMenuItem[] CreateItems()
        {
            return
            [
                new ActionMenuItem { Id = "profile", Label = "Profile", Symbol = SymbolIconName.User },
                new ActionMenuItem { Id = "preferences", Label = "Preferences", Symbol = SymbolIconName.Preferences },
                new ActionMenuItem
                {
                    Id = "sign-out",
                    Label = "Sign out",
                    Symbol = SymbolIconName.SignOut,
                    Destructive = true,
                    SeparatorBefore = true
                }
            ];
        }
    }
}
