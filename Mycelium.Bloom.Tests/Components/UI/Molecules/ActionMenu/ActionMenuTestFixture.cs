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
    using System.Threading.Tasks;

    using BlazorBlueprint.Components;
    using BlazorBlueprint.Primitives;
    using BlazorBlueprint.Primitives.Services;

    using Bunit;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;

    using ActionMenuComponent = Mycelium.Bloom.Components.UI.Molecules.ActionMenu.ActionMenu;

    /// <summary>
    /// Tests Bloom action metadata and callbacks mapped onto styled Blueprint menus.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ActionMenuTestFixture : BunitContext
    {
        private static readonly IReadOnlyList<ActionMenuItem> Items =
        [
            new() { Id = "open", Label = "Open details", Description = "Inspect the selected element", Symbol = SymbolIconName.Inspect },
            new() { Id = "publish", Label = "Publish", Disabled = true },
            new() { Id = "delete", Label = "Delete", Symbol = SymbolIconName.Delete, Destructive = true, SeparatorBefore = true }
        ];

        private readonly IRenderedComponent<BbPortalHost> portalHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionMenuTestFixture" /> class.
        /// </summary>
        public ActionMenuTestFixture()
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
        /// Verifies trigger content, accessibility metadata, alignment, and public class extensions.
        /// </summary>
        [Test]
        public async Task VerifyRenderDisplaysConfiguredTrigger()
        {
            var component = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(menu => menu.Items, Items)
                .Add(menu => menu.TriggerAriaLabel, "Open element actions")
                .Add(menu => menu.TriggerTitle, "Element actions")
                .Add(menu => menu.TriggerClass, "custom-trigger")
                .Add(menu => menu.MenuClass, "custom-menu")
                .Add(menu => menu.Alignment, ActionMenuAlignment.Start)
                .Add(menu => menu.TriggerContent, "<span>Actions</span>")
                .AddUnmatched("data-testid", "actions"));

            var trigger = component.Find("button");
            await trigger.ClickAsync();
            var menu = await this.portalHost.WaitForElementAsync("[role='menu']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("[data-testid='actions']").ClassList, Does.Contain("mb-action-menu"));
                Assert.That(trigger.GetAttribute("aria-label"), Is.EqualTo("Open element actions"));
                Assert.That(trigger.GetAttribute("title"), Is.EqualTo("Element actions"));
                Assert.That(trigger.GetAttribute("aria-haspopup"), Is.EqualTo("menu"));
                Assert.That(trigger.GetAttribute("aria-expanded"), Is.EqualTo("true"));
                Assert.That(trigger.GetAttribute("style"), Does.Contain("cursor: pointer"));
                Assert.That(trigger.ClassList, Does.Contain("custom-trigger"));
                Assert.That(trigger.TextContent, Does.Contain("Actions"));
                Assert.That(menu.ClassList, Does.Contain("custom-menu"));
                Assert.That(component.FindComponent<BbDropdownMenuContent>().Instance.Align, Is.EqualTo(PopoverAlign.Start));
            }
        }

        /// <summary>
        /// Verifies descriptions, separators, disabled state, and destructive presentation.
        /// </summary>
        [Test]
        public async Task VerifyRenderMapsBloomItemMetadata()
        {
            var component = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(menu => menu.Items, Items));

            await component.Find("button").ClickAsync();
            var menuItems = await this.portalHost.WaitForElementsAsync("[role='menuitem']", Items.Count);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(menuItems[0].TextContent, Does.Contain("Open details"));
                Assert.That(menuItems[0].TextContent, Does.Contain("Inspect the selected element"));
                var enabledSurface = menuItems[0].QuerySelector(".mb-action-menu__item-pointer-surface");
                Assert.That(enabledSurface, Is.Not.Null);
                Assert.That(enabledSurface.GetAttribute("role"), Is.EqualTo("presentation"));
                Assert.That(enabledSurface.GetAttribute("aria-hidden"), Is.EqualTo("true"));
                Assert.That(menuItems[1].GetAttribute("aria-disabled"), Is.EqualTo("true"));
                Assert.That(menuItems[1].QuerySelector(".mb-action-menu__item-pointer-surface"), Is.Null);
                Assert.That(menuItems[2].ClassList, Does.Contain("text-destructive"));
                Assert.That(menuItems[2].TextContent, Does.Contain("Destructive action"));
                Assert.That(this.portalHost.FindAll("[role='separator']"), Has.Count.EqualTo(1));
            }
        }

        /// <summary>
        /// Verifies a selection menu exposes a visible and spoken current-item indication.
        /// </summary>
        [Test]
        public async Task VerifySelectionMenuExposesCurrentItem()
        {
            var selectionItems = new[]
            {
                new ActionMenuItem { Id = "first", Label = "First", IsSelected = true },
                new ActionMenuItem { Id = "second", Label = "Second" }
            };

            var component = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(menu => menu.Items, selectionItems)
                .Add(menu => menu.IsSelectionMenu, true));

            await component.Find("button").ClickAsync();
            var items = await this.portalHost.WaitForElementsAsync("[role='menuitem']", 2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(items[0].ClassList, Does.Contain("bg-accent"));
                Assert.That(items[0].TextContent, Does.Contain("Current selection"));
                Assert.That(items[1].TextContent, Does.Not.Contain("Current selection"));
            }
        }

        /// <summary>
        /// Verifies enabled item selection closes the menu and reports the original model exactly once.
        /// </summary>
        [Test]
        public async Task VerifyEnabledItemSelectionReportsItemExactlyOnce()
        {
            ActionMenuItem selectedItem = null;
            var callbackCount = 0;
            var component = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(menu => menu.Items, Items)
                .Add(menu => menu.ItemSelected, item =>
                {
                    selectedItem = item;
                    callbackCount++;
                }));

            await component.Find("button").ClickAsync();
            var menuItems = await this.portalHost.WaitForElementsAsync("[role='menuitem']", Items.Count);
            await menuItems[0].ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectedItem, Is.SameAs(Items[0]));
                Assert.That(callbackCount, Is.EqualTo(1));
                Assert.That(component.Find("button").GetAttribute("aria-expanded"), Is.EqualTo("false"));
            }
        }

        /// <summary>
        /// Verifies disabled menu items cannot invoke Bloom callbacks.
        /// </summary>
        [Test]
        public async Task VerifyDisabledItemCannotBeActivated()
        {
            var callbackCount = 0;
            var component = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(menu => menu.Items, Items)
                .Add(menu => menu.ItemSelected, _ => callbackCount++));

            await component.Find("button").ClickAsync();
            var menuItems = await this.portalHost.WaitForElementsAsync("[role='menuitem']", Items.Count);
            await menuItems[1].ClickAsync();

            Assert.That(callbackCount, Is.Zero);
        }

        /// <summary>
        /// Verifies disabled and empty menus expose an unavailable trigger.
        /// </summary>
        /// <param name="useItems">Whether the menu has items.</param>
        /// <param name="isDisabled">Whether the menu is explicitly disabled.</param>
        [TestCase(false, false)]
        [TestCase(true, true)]
        public async Task VerifyUnavailableMenuDisablesTrigger(bool useItems, bool isDisabled)
        {
            var component = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(menu => menu.Items, useItems ? Items : [])
                .Add(menu => menu.Disabled, isDisabled));
            var trigger = component.Find("button");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(trigger.GetAttribute("aria-disabled"), Is.EqualTo("true"));
                Assert.That(trigger.GetAttribute("aria-expanded"), Is.EqualTo("false"));
            }

            await trigger.ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(trigger.GetAttribute("aria-expanded"), Is.EqualTo("false"));
                Assert.That(this.portalHost.FindAll("[role='menu']"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies multiple instances use independent trigger relationships.
        /// </summary>
        [Test]
        public async Task VerifyMultipleInstancesUseIndependentRelationships()
        {
            var first = this.Render<ActionMenuComponent>(parameters => parameters.Add(menu => menu.Items, Items));
            var second = this.Render<ActionMenuComponent>(parameters => parameters.Add(menu => menu.Items, Items));
            var firstTrigger = first.Find("button");
            var secondTrigger = second.Find("button");

            await firstTrigger.ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstTrigger.GetAttribute("aria-controls"), Is.Not.Null.And.Not.Empty);
                Assert.That(secondTrigger.GetAttribute("aria-controls"), Is.Not.Null.And.Not.Empty);
                Assert.That(secondTrigger.GetAttribute("aria-controls"), Is.Not.EqualTo(firstTrigger.GetAttribute("aria-controls")));
                Assert.That(firstTrigger.GetAttribute("aria-expanded"), Is.EqualTo("true"));
                Assert.That(secondTrigger.GetAttribute("aria-expanded"), Is.EqualTo("false"));
            }
        }

        /// <summary>
        /// Verifies a pending callback cannot be delivered twice by repeated activation.
        /// </summary>
        [Test]
        public async Task VerifyPendingSelectionIgnoresRepeatedActivation()
        {
            var callbackCount = 0;
            var callbackStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var component = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(menu => menu.Items, Items)
                .Add(menu => menu.ItemSelected, async _ =>
                {
                    callbackCount++;
                    callbackStarted.TrySetResult();
                    await releaseCallback.Task;
                }));

            await component.Find("button").ClickAsync();
            var firstItem = (await this.portalHost.WaitForElementsAsync("[role='menuitem']", Items.Count))[0];
            var firstSelection = firstItem.ClickAsync();
            await callbackStarted.Task;
            await firstItem.ClickAsync();
            releaseCallback.SetResult();
            await firstSelection;

            Assert.That(callbackCount, Is.EqualTo(1));
        }
    }
}
