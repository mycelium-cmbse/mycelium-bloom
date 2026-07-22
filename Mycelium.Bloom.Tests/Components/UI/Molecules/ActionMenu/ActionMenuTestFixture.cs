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
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Bunit;

    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;

    using ActionMenuComponent = Mycelium.Bloom.Components.UI.Molecules.ActionMenu.ActionMenu;

    /// <summary>
    /// Tests the <see cref="ActionMenuComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ActionMenuTestFixture : BunitContext
    {
        /// <summary>
        /// The shared keyboard module used for registration and disposal assertions.
        /// </summary>
        private BunitJSModuleInterop keyboardModule;

        /// <summary>
        /// Configures the shared outside-click helper used by ActionMenu.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            JavaScriptInteropTestSetup.SetUpOutsideClick(this.JSInterop);
            this.keyboardModule = JavaScriptInteropTestSetup.SetUpKeyboardDefaults(this.JSInterop);

            var focusHandler = this.JSInterop.SetupVoid(
                "Blazor._internal.domWrapper.focus",
                invocation => true);
            focusHandler.SetVoidResult();
        }

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
                .Add(component => component.Items, CreateItems())
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
        /// Verifies that an outside pointer callback closes the menu and reports the state change.
        /// </summary>
        [Test]
        public async Task VerifyOutsideClickDismissesOpenMenu()
        {
            var openStateChangeCount = 0;

            var component = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(actionMenu => actionMenu.Items, CreateItems())
                .Add(actionMenu => actionMenu.IsOpenChanged, _ => openStateChangeCount++));

            component.Find("button").Click();

            await component.InvokeAsync(component.Instance.DismissFromOutsideClickAsync);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("button").GetAttribute("aria-expanded"), Is.EqualTo("false"));
                Assert.That(component.FindAll("[role='menu']"), Is.Empty);
                Assert.That(openStateChangeCount, Is.EqualTo(2));
                Assert.That(this.JSInterop.Invocations["Blazor._internal.domWrapper.focus"], Is.Empty);
            }
        }

        /// <summary>
        /// Verifies that selecting an enabled action returns it and closes the popup.
        /// </summary>
        [Test]
        public void VerifyEnabledActionInvokesCallbackAndClosesMenu()
        {
            ActionMenuItem selectedItem = null;
            var items = CreateItems();

            var component = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(component => component.Items, items)
                .Add(component => component.ItemSelected, item => selectedItem = item));

            component.Find("button").Click();
            component.FindAll("[role='menuitem']")[0].Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectedItem, Is.SameAs(items[0]));
                Assert.That(component.FindAll("[role='menu']"), Is.Empty);
                Assert.That(this.JSInterop.Invocations["Blazor._internal.domWrapper.focus"],
                    Has.Count.EqualTo(1));
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
        /// <param name="useLabelledTrigger">Whether the menu uses a native labelled trigger.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void VerifyEscapeClosesMenuAndRestoresTriggerFocus(bool useLabelledTrigger)
        {
            var component = this.Render<ActionMenuComponent>(parameters =>
            {
                parameters.Add(actionMenu => actionMenu.Items, CreateItems());

                if (useLabelledTrigger)
                {
                    parameters.Add(actionMenu => actionMenu.TriggerContent, "Actions");
                }
            });

            component.Find("button").Click();
            component.Find("[role='menuitem']").KeyDown(new KeyboardEventArgs { Key = "Escape" });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("[role='menu']"), Is.Empty);
                Assert.That(this.JSInterop.Invocations["Blazor._internal.domWrapper.focus"],
                    Has.Count.EqualTo(1));
            }
        }

        /// <summary>
        /// Verifies that trigger keyboard commands open from the final enabled item and safely close an open or closed menu.
        /// </summary>
        [Test]
        public void VerifyTriggerKeyboardCommandsOpenFromEndAndCloseSafely()
        {
            this.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(component => component.Items, CreateItems()));

            component.Find("button").KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });

            var items = component.FindAll("[role='menuitem']");

            Assert.That(items.All(item => item.GetAttribute("tabindex") == "-1"), Is.True);

            component.Find("button").KeyDown(new KeyboardEventArgs { Key = "Escape" });
            component.Find("button").KeyDown(new KeyboardEventArgs { Key = "Escape" });

            Assert.That(component.FindAll("[role='menu']"), Is.Empty);
        }

        /// <summary>
        /// Verifies that a disabled trigger ignores keyboard requests to open the menu.
        /// </summary>
        [Test]
        public void VerifyDisabledTriggerIgnoresKeyboardOpen()
        {
            var component = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(component => component.Items, CreateItems())
                .Add(component => component.Disabled, true));

            component.Find("button").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

            Assert.That(component.FindAll("[role='menu']"), Is.Empty);
        }

        /// <summary>
        /// Verifies that a labelled trigger uses stable content and shared chevron structure.
        /// </summary>
        [Test]
        public void VerifyLabelledTriggerRendersAlignedChevronStructure()
        {
            RenderFragment triggerContent = builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "trigger-label");
                builder.AddContent(2, "Actions");
                builder.CloseElement();
            };

            var component = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(actionMenu => actionMenu.Items, CreateItems())
                .Add(actionMenu => actionMenu.TriggerContent, triggerContent));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-action-menu__trigger-content").TextContent.Trim(),
                    Is.EqualTo("Actions"));
                Assert.That(component.FindAll(".mb-action-menu__chevron"), Has.Count.EqualTo(1));
                Assert.That(component.Find(".mb-action-menu__chevron").GetAttribute("aria-hidden"),
                    Is.EqualTo("true"));
                Assert.That(component.Find(".mb-action-menu__chevron svg"), Is.Not.Null);
            }
        }

        /// <summary>
        /// Verifies that arrow, Home, and End keys move focus while menu items stay outside the page tab order.
        /// </summary>
        [Test]
        public void VerifyKeyboardNavigationMovesFocusOutsidePageTabOrder()
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
                Assert.That(items[1].GetAttribute("tabindex"), Is.EqualTo("-1"));
                Assert.That(items[2].GetAttribute("tabindex"), Is.EqualTo("-1"));
                Assert.That(this.JSInterop.Invocations["Blazor._internal.domWrapper.focus"], Has.Count.EqualTo(1));
            }

            items[1].KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
            items = component.FindAll("[role='menuitem']");
            Assert.That(this.JSInterop.Invocations["Blazor._internal.domWrapper.focus"], Has.Count.EqualTo(2));

            items[2].KeyDown(new KeyboardEventArgs { Key = "Home" });
            items = component.FindAll("[role='menuitem']");
            Assert.That(this.JSInterop.Invocations["Blazor._internal.domWrapper.focus"], Has.Count.EqualTo(3));

            items[1].KeyDown(new KeyboardEventArgs { Key = "End" });
            items = component.FindAll("[role='menuitem']");
            Assert.That(this.JSInterop.Invocations["Blazor._internal.domWrapper.focus"], Has.Count.EqualTo(4));

            items[2].KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
            items = component.FindAll("[role='menuitem']");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(items.All(item => item.GetAttribute("tabindex") == "-1"), Is.True);
                Assert.That(this.JSInterop.Invocations["Blazor._internal.domWrapper.focus"], Has.Count.EqualTo(5));
            }
        }

        /// <summary>
        /// Verifies that Tab closes the popup from either the trigger or a focused item without selecting an action.
        /// </summary>
        /// <param name="fromMenuItem">A value indicating whether Tab originates from a menu item.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void VerifyTabClosesMenuWithoutSelection(bool fromMenuItem)
        {
            var selectionCount = 0;
            var component = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(actionMenu => actionMenu.Items, CreateItems())
                .Add(actionMenu => actionMenu.ItemSelected, _ => selectionCount++));

            var trigger = component.Find(".mb-action-menu__trigger");

            if (fromMenuItem)
            {
                this.JSInterop.Mode = JSRuntimeMode.Loose;
                trigger.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
                component.Find("[role='menuitem']").KeyDown(new KeyboardEventArgs { Key = "Tab" });
            }
            else
            {
                trigger.Click();
                trigger.KeyDown(new KeyboardEventArgs { Key = "Tab" });
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("[role='menu']"), Is.Empty);
                Assert.That(selectionCount, Is.Zero);
            }
        }

        /// <summary>
        /// Verifies that browser-default prevention is scoped to handled keys and releases its listener.
        /// </summary>
        [Test]
        public async Task VerifyKeyboardDefaultPreventionIsScopedAndDisposable()
        {
            var component = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(actionMenu => actionMenu.Items, CreateItems()));

            var registration = this.keyboardModule.Invocations["registerKeyPrevention"].Single();
            var rules = ((IEnumerable)registration.Arguments[1]).Cast<object>().ToArray();
            var selectors = rules.Select(rule => GetRuleProperty<string>(rule, "Selector")).ToArray();
            var keys = rules
                .SelectMany(rule => GetRuleProperty<IEnumerable<string>>(rule, "Keys"))
                .ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rules, Has.Length.EqualTo(2));
                Assert.That(selectors, Does.Contain(".mb-action-menu__trigger"));
                Assert.That(selectors, Does.Contain("[role='menuitem'], [role='menuitemradio']"));
                Assert.That(keys, Does.Contain("ArrowDown"));
                Assert.That(keys, Does.Contain(" "));
                Assert.That(keys, Does.Not.Contain("Tab"));
            }

            await component.Instance.DisposeAsync();

            Assert.That(this.keyboardModule.Invocations["disposeKeyPrevention"], Has.Count.EqualTo(1));
        }

        /// <summary>
        /// Verifies that Enter and Space activate the focused action and close the menu.
        /// </summary>
        /// <param name="key">The activation key.</param>
        [TestCase("Enter")]
        [TestCase(" ")]
        public void VerifyKeyboardActivationSelectsFocusedAction(string key)
        {
            ActionMenuItem selectedItem = null;
            var items = CreateItems();
            var component = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(actionMenu => actionMenu.Items, items)
                .Add(actionMenu => actionMenu.ItemSelected, item => selectedItem = item));

            component.Find("button").Click();
            component.FindAll("[role='menuitem']")[0].KeyDown(new KeyboardEventArgs { Key = key });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectedItem, Is.SameAs(items[0]));
                Assert.That(component.FindAll("[role='menu']"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies that externally closing the menu clears roving focus before it is reopened.
        /// </summary>
        [Test]
        public void VerifyExternalCloseResetsRovingFocus()
        {
            this.JSInterop.Mode = JSRuntimeMode.Loose;

            var items = CreateItems();
            var component = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(component => component.Items, items)
                .Add(component => component.IsOpen, true));

            component.FindAll("[role='menuitem']")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

            Assert.That(
                component.FindAll("[role='menuitem']").All(item => item.GetAttribute("tabindex") == "-1"),
                Is.True);

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
                Assert.That(reopenedItems[0].GetAttribute("tabindex"), Is.EqualTo("-1"));
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
                .Add(component => component.Items, CreateItems())
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
                .Add(component => component.Items, CreateItems()));
            var second = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(component => component.Items, CreateItems()));

            first.Find("button").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.FindAll("[role='menu']"), Has.Count.EqualTo(1));
                Assert.That(second.FindAll("[role='menu']"), Is.Empty);
                Assert.That(first.Find("button").GetAttribute("aria-controls"),
                    Is.Not.EqualTo(second.Find("button").GetAttribute("aria-controls")));
            }

            first.Find("[role='menuitem']").KeyDown(new KeyboardEventArgs { Key = "Escape" });

            var firstFocusTarget = (ElementReference)this.JSInterop
                .Invocations["Blazor._internal.domWrapper.focus"][0]
                .Arguments[0];

            second.Find("button").Click();
            second.Find("[role='menuitem']").KeyDown(new KeyboardEventArgs { Key = "Escape" });

            var secondFocusTarget = (ElementReference)this.JSInterop
                .Invocations["Blazor._internal.domWrapper.focus"][1]
                .Arguments[0];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.FindAll("[role='menu']"), Is.Empty);
                Assert.That(second.FindAll("[role='menu']"), Is.Empty);
                Assert.That(firstFocusTarget.Id, Is.Not.EqualTo(secondFocusTarget.Id));
            }
        }

        /// <summary>
        /// Verifies disposal clears a requested trigger-focus restoration before it can run.
        /// </summary>
        [Test]
        public void VerifyDisposalClearsPendingTriggerFocus()
        {
            IRenderedComponent<ActionMenuComponent> component = null;

            component = this.Render<ActionMenuComponent>(parameters => parameters
                .Add(actionMenu => actionMenu.Items, CreateItems())
                .Add(actionMenu => actionMenu.IsOpenChanged, async isOpen =>
                {
                    if (!isOpen)
                    {
                        await component.Instance.DisposeAsync();
                    }
                }));

            component.Find("button").Click();
            component.Find("[role='menuitem']").KeyDown(new KeyboardEventArgs { Key = "Escape" });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("[role='menu']"), Is.Empty);
                Assert.That(this.JSInterop.Invocations["Blazor._internal.domWrapper.focus"], Is.Empty);
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
                Assert.That(item.QuerySelector(".mb-action-menu__item-icon"), Is.Not.Null);
                Assert.That(component.Find(".mb-action-menu__item-label").GetAttribute("title"),
                    Is.EqualTo("Delete"));
                Assert.That(component.Find("[role='menu']").GetAttribute("class"),
                    Does.Contain("mb-action-menu__menu--start"));
            }
        }

        /// <summary>
        /// Creates a standard enabled action list.
        /// </summary>
        /// <returns>The action items.</returns>
        private static ActionMenuItem[] CreateItems()
        {
            return
            [
                new ActionMenuItem { Id = "open", Label = "Open" },
                new ActionMenuItem { Id = "duplicate", Label = "Duplicate" }
            ];
        }

        /// <summary>
        /// Gets a serialized keyboard rule property by name.
        /// </summary>
        /// <typeparam name="TValue">The expected property value type.</typeparam>
        /// <param name="rule">The serialized keyboard rule.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The configured property value.</returns>
        private static TValue GetRuleProperty<TValue>(object rule, string propertyName)
        {
            var property = rule.GetType().GetProperty(propertyName);

            Assert.That(property, Is.Not.Null);

            return (TValue)property.GetValue(rule)!;
        }
    }
}
