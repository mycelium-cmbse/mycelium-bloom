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
    using System.Threading.Tasks;

    using Bunit;

    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    using ActionMenuComponent = Mycelium.Bloom.Components.UI.Molecules.ActionMenu.ActionMenu;

    /// <summary>
    /// Tests the <see cref="ActionMenuComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ActionMenuTestFixture : BunitContext
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
        /// Verifies that the default trigger exposes menu state and toggles the popup.
        /// </summary>
        [Test]
        public void VerifyTriggerOpensAndClosesMenu()
        {
            var openStateChangeCount = 0;
            var lastOpenState = false;

            var component = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(component => component.Items, this.CreateItems())
                .Add(component => component.TriggerAriaLabel, "Element actions")
                .Add(component => component.IsOpenChanged, isOpen =>
                {
                    openStateChangeCount++;
                    lastOpenState = isOpen;
                }));

            var trigger = component.Find("button");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(trigger.GetAttribute("aria-haspopup"), Is.EqualTo("menu"));
                Assert.That(trigger.GetAttribute("aria-expanded"), Is.EqualTo("false"));
                Assert.That(trigger.GetAttribute("aria-controls"), Is.Not.Empty);
                Assert.That(component.FindAll("[role='menu']"), Is.Empty);
            }

            trigger.Click();

            var menu = component.Find("[role='menu']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("button").GetAttribute("aria-expanded"), Is.EqualTo("true"));
                Assert.That(component.FindAll("[role='menuitem']"), Has.Count.EqualTo(2));
                Assert.That(menu.GetAttribute("id"), Is.EqualTo(component.Find("button").GetAttribute("aria-controls")));
                Assert.That(openStateChangeCount, Is.EqualTo(1));
                Assert.That(lastOpenState, Is.True);
            }

            component.Find("button").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("[role='menu']"), Is.Empty);
                Assert.That(openStateChangeCount, Is.EqualTo(2));
                Assert.That(lastOpenState, Is.False);
            }
        }

        /// <summary>
        /// Verifies that selecting an enabled action returns it and closes the popup.
        /// </summary>
        [Test]
        public void VerifyEnabledActionInvokesCallbackAndClosesMenu()
        {
            ActionMenuItem selectedItem = null;
            var items = this.CreateItems();

            var component = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(component => component.Items, items)
                .Add(component => component.ItemSelected, item => selectedItem = item));

            component.Find("button").Click();
            component.FindAll("[role='menuitem']")[0].Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectedItem, Is.SameAs(items[0]));
                Assert.That(component.FindAll("[role='menu']"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies that a disabled action neither invokes the callback nor closes the popup.
        /// </summary>
        [Test]
        public void VerifyDisabledActionDoesNotInvokeCallbackOrCloseMenu()
        {
            var selectionCount = 0;
            var items = new[]
            {
                new ActionMenuItem { Id = "archive", Label = "Archive", Disabled = true }
            };

            var component = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(component => component.Items, items)
                .Add(component => component.ItemSelected, _ => selectionCount++));

            component.Find("button").Click();
            component.Find("[role='menuitem']").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectionCount, Is.Zero);
                Assert.That(component.FindAll("[role='menu']"), Has.Count.EqualTo(1));
            }
        }

        /// <summary>
        /// Verifies that Escape closes an open menu.
        /// </summary>
        [Test]
        public void VerifyEscapeClosesMenu()
        {
            var component = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(component => component.Items, this.CreateItems()));

            component.Find("button").Click();
            component.Find("[role='menuitem']").KeyDown(new KeyboardEventArgs { Key = "Escape" });

            Assert.That(component.FindAll("[role='menu']"), Is.Empty);
        }

        /// <summary>
        /// Verifies that arrow, Home, and End keys move roving focus while skipping disabled items.
        /// </summary>
        [Test]
        public void VerifyKeyboardNavigationMovesRovingFocus()
        {
            this.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(component => component.Items, new[]
                {
                    new ActionMenuItem { Id = "disabled", Label = "Disabled", Disabled = true },
                    new ActionMenuItem { Id = "open", Label = "Open" },
                    new ActionMenuItem { Id = "duplicate", Label = "Duplicate" }
                }));

            component.Find("button").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

            var items = component.FindAll("[role='menuitem']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(items[0].GetAttribute("tabindex"), Is.EqualTo("-1"));
                Assert.That(items[1].GetAttribute("tabindex"), Is.EqualTo("0"));
                Assert.That(items[2].GetAttribute("tabindex"), Is.EqualTo("-1"));
            }

            items[1].KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
            items = component.FindAll("[role='menuitem']");
            Assert.That(items[2].GetAttribute("tabindex"), Is.EqualTo("0"));

            items[2].KeyDown(new KeyboardEventArgs { Key = "Home" });
            items = component.FindAll("[role='menuitem']");
            Assert.That(items[1].GetAttribute("tabindex"), Is.EqualTo("0"));

            items[1].KeyDown(new KeyboardEventArgs { Key = "End" });
            items = component.FindAll("[role='menuitem']");
            Assert.That(items[2].GetAttribute("tabindex"), Is.EqualTo("0"));
        }

        /// <summary>
        /// Verifies that externally closing the menu clears roving focus before it is reopened.
        /// </summary>
        [Test]
        public void VerifyExternalCloseResetsRovingFocus()
        {
            this.JSInterop.Mode = JSRuntimeMode.Loose;

            var items = this.CreateItems();
            var component = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(component => component.Items, items)
                .Add(component => component.IsOpen, true));

            component.FindAll("[role='menuitem']")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

            Assert.That(
                component.FindAll("[role='menuitem']")[1].GetAttribute("tabindex"),
                Is.EqualTo("0"));

            component.Render(parameters => parameters
                .Add(component => component.Items, items)
                .Add(component => component.IsOpen, false));

            Assert.That(component.FindAll("[role='menu']"), Is.Empty);

            component.Render(parameters => parameters
                .Add(component => component.Items, items)
                .Add(component => component.IsOpen, true));

            var reopenedItems = component.FindAll("[role='menuitem']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reopenedItems[0].GetAttribute("tabindex"), Is.EqualTo("0"));
                Assert.That(reopenedItems[1].GetAttribute("tabindex"), Is.EqualTo("-1"));
            }
        }

        /// <summary>
        /// Verifies that a pending item selection disables the trigger until its callback completes.
        /// </summary>
        [Test]
        public async Task VerifyPendingSelectionDisablesTrigger()
        {
            var selectionCount = 0;
            var callbackStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var component = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(component => component.Items, this.CreateItems())
                .Add(component => component.ItemSelected, async _ =>
                {
                    selectionCount++;
                    callbackStarted.TrySetResult();
                    await releaseCallback.Task;
                }));

            component.Find("button").Click();

            var selection = component.Find("[role='menuitem']").ClickAsync(new MouseEventArgs());

            await callbackStarted.Task;

            var trigger = component.Find("button");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(trigger.HasAttribute("disabled"), Is.True);
                Assert.That(component.FindAll("[role='menu']"), Is.Empty);
            }

            trigger.Click();
            releaseCallback.SetResult();

            await selection;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectionCount, Is.EqualTo(1));
                Assert.That(component.Find("button").HasAttribute("disabled"), Is.False);
            }
        }

        /// <summary>
        /// Verifies that separate action-menu instances do not share open state.
        /// </summary>
        [Test]
        public void VerifyInstancesMaintainIndependentOpenState()
        {
            var first = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(component => component.Items, this.CreateItems()));
            var second = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(component => component.Items, this.CreateItems()));

            first.Find("button").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.FindAll("[role='menu']"), Has.Count.EqualTo(1));
                Assert.That(second.FindAll("[role='menu']"), Is.Empty);
                Assert.That(first.Find("button").GetAttribute("aria-controls"),
                    Is.Not.EqualTo(second.Find("button").GetAttribute("aria-controls")));
            }
        }

        /// <summary>
        /// Verifies destructive, separator, selected, alignment, and description rendering.
        /// </summary>
        [Test]
        public void VerifyConfiguredItemStatesRender()
        {
            var component = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(component => component.Items, new[]
                {
                    new ActionMenuItem
                    {
                        Id = "delete",
                        Label = "Delete",
                        Description = "Cannot be undone",
                        Destructive = true,
                        SeparatorBefore = true,
                        IsSelected = true
                    }
                })
                .Add(component => component.IsSelectionMenu, true)
                .Add(component => component.Alignment, ActionMenuAlignment.Start));

            component.Find("button").Click();

            var item = component.Find("[role='menuitemradio']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(item.GetAttribute("class"), Does.Contain("mb-action-menu__item--destructive"));
                Assert.That(item.GetAttribute("class"), Does.Contain("mb-action-menu__item--selected"));
                Assert.That(item.GetAttribute("aria-checked"), Is.EqualTo("true"));
                Assert.That(component.FindAll("[role='separator']"), Has.Count.EqualTo(1));
                Assert.That(component.Find(".mb-action-menu__item-description").TextContent,
                    Is.EqualTo("Cannot be undone"));
                Assert.That(component.Find("[role='menu']").GetAttribute("class"),
                    Does.Contain("mb-action-menu__menu--start"));
            }
        }

        /// <summary>
        /// Creates a standard enabled action list.
        /// </summary>
        /// <returns>The action items.</returns>
        private ActionMenuItem[] CreateItems()
        {
            return
            [
                new ActionMenuItem { Id = "open", Label = "Open" },
                new ActionMenuItem { Id = "duplicate", Label = "Duplicate" }
            ];
        }
    }
}
