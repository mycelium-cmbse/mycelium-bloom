// ------------------------------------------------------------------------------------------------
// <copyright file="ActionMenu.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Molecules.ActionMenu
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;
    using Microsoft.JSInterop;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Atoms.IconButton;
    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Represents a reusable action menu with instance-specific state and native keyboard interaction.
    /// </summary>
    public partial class ActionMenu : BloomComponentBase, IAsyncDisposable
    {
        /// <summary>
        /// The standard downward-arrow key value.
        /// </summary>
        private const string ArrowDownKey = "ArrowDown";

        /// <summary>
        /// The standard upward-arrow key value.
        /// </summary>
        private const string ArrowUpKey = "ArrowUp";

        /// <summary>
        /// The generated stable identifier shared by the trigger and menu.
        /// </summary>
        private readonly string generatedId = CreateGeneratedId("mb-action-menu");

        /// <summary>
        /// References to rendered menu-item buttons.
        /// </summary>
        private ElementReference[] itemElements = [];

        /// <summary>
        /// The default icon trigger component used when no labelled trigger content is supplied.
        /// </summary>
        private IconButton triggerIconButton;

        /// <summary>
        /// The native trigger element used for labelled and chevron-only triggers.
        /// </summary>
        private ElementReference triggerElement;

        /// <summary>
        /// The root containing the trigger and menu for outside-click detection.
        /// </summary>
        private ElementReference popupRootElement;

        /// <summary>
        /// The instance-specific outside-click registration.
        /// </summary>
        private OutsideClickRegistration<ActionMenu> outsideClickRegistration;

        /// <summary>
        /// The element-scoped keyboard-default registration.
        /// </summary>
        private KeyboardDefaultPreventionRegistration keyboardDefaultPreventionRegistration;

        /// <summary>
        /// The menu-item index that should receive focus after rendering.
        /// </summary>
        private int? pendingFocusIndex;

        /// <summary>
        /// Indicates whether focus should return to the trigger after the closed menu is rendered.
        /// </summary>
        private bool pendingTriggerFocus;

        /// <summary>
        /// Indicates whether an item-selection callback is currently running.
        /// </summary>
        private bool isSelecting;

        /// <summary>
        /// Indicates whether the component has been disposed.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// Gets or sets the available action items.
        /// </summary>
        [Parameter]
        public IReadOnlyList<ActionMenuItem> Items { get; set; } = [];

        /// <summary>
        /// Gets or sets optional custom content rendered inside the trigger button.
        /// </summary>
        [Parameter]
        public RenderFragment TriggerContent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the shared dropdown chevron is rendered without custom trigger content.
        /// </summary>
        [Parameter]
        public bool ShowChevron { get; set; }

        /// <summary>
        /// Gets or sets the accessible trigger label.
        /// </summary>
        [Parameter]
        public string TriggerAriaLabel { get; set; } = "Open actions";

        /// <summary>
        /// Gets or sets the trigger title.
        /// </summary>
        [Parameter]
        public string TriggerTitle { get; set; } = "Open actions";

        /// <summary>
        /// Gets or sets additional classes applied to the trigger button.
        /// </summary>
        [Parameter]
        public string TriggerClass { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the horizontal menu alignment.
        /// </summary>
        [Parameter]
        public ActionMenuAlignment Alignment { get; set; } = ActionMenuAlignment.End;

        /// <summary>
        /// Gets or sets a value indicating whether the trigger is disabled.
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether items represent mutually exclusive selections.
        /// </summary>
        [Parameter]
        public bool IsSelectionMenu { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the menu is open.
        /// </summary>
        [Parameter]
        public bool IsOpen { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the open state changes.
        /// </summary>
        [Parameter]
        public EventCallback<bool> IsOpenChanged { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when an enabled item is selected.
        /// </summary>
        [Parameter]
        public EventCallback<ActionMenuItem> ItemSelected { get; set; }

        /// <summary>
        /// Gets or sets the index participating in roving keyboard focus.
        /// </summary>
        private int FocusedItemIndex { get; set; } = -1;

        /// <summary>
        /// Gets the trigger element identifier.
        /// </summary>
        private string TriggerId => $"{this.generatedId}-trigger";

        /// <summary>
        /// Gets the menu element identifier.
        /// </summary>
        private string MenuId => $"{this.generatedId}-menu";

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            if (this.isDisposed)
            {
                return;
            }

            if (this.itemElements.Length != this.Items.Count)
            {
                this.itemElements = new ElementReference[this.Items.Count];
            }

            if (!this.IsOpen || this.Disabled || this.Items.Count == 0)
            {
                if (this.Disabled || this.Items.Count == 0)
                {
                    this.IsOpen = false;
                    this.pendingTriggerFocus = false;
                }

                this.FocusedItemIndex = -1;
                this.pendingFocusIndex = null;
            }
            else if (!this.IsEnabledIndex(this.FocusedItemIndex))
            {
                this.pendingTriggerFocus = false;
                this.FocusedItemIndex = this.FindEnabledIndex(0, 1);
            }
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender && !this.isDisposed)
            {
                this.outsideClickRegistration = new OutsideClickRegistration<ActionMenu>(this.JsRuntime);
                await this.outsideClickRegistration.RegisterAsync(this.popupRootElement, this);

                this.keyboardDefaultPreventionRegistration = new KeyboardDefaultPreventionRegistration(this.JsRuntime);

                await this.keyboardDefaultPreventionRegistration.RegisterAsync(
                    this.popupRootElement,
                    [
                        new KeyboardDefaultPreventionRule(
                            ".mb-action-menu__trigger",
                            ArrowDownKey,
                            "Down",
                            ArrowUpKey,
                            "Up"),
                        new KeyboardDefaultPreventionRule(
                            "[role='menuitem'], [role='menuitemradio']",
                            "Enter",
                            " ",
                            "Space",
                            "Spacebar",
                            ArrowDownKey,
                            "Down",
                            ArrowUpKey,
                            "Up",
                            "Home",
                            "End")
                    ]);
            }

            if (this.isDisposed)
            {
                return;
            }

            if (this.pendingTriggerFocus && !this.IsOpen)
            {
                this.pendingTriggerFocus = false;
                await this.FocusTriggerAsync();
                return;
            }

            if (!this.IsOpen
                || this.pendingFocusIndex is not { } itemIndex
                || !this.IsEnabledIndex(itemIndex))
            {
                return;
            }

            this.pendingFocusIndex = null;

            await this.itemElements[itemIndex].FocusAsync();
        }

        /// <summary>
        /// Prevents delayed focus or callbacks from targeting a disposed component instance.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            this.isDisposed = true;
            this.IsOpen = false;
            this.FocusedItemIndex = -1;
            this.pendingFocusIndex = null;
            this.pendingTriggerFocus = false;

            if (this.outsideClickRegistration is not null)
            {
                await this.outsideClickRegistration.DisposeAsync();
                this.outsideClickRegistration = null;
            }

            if (this.keyboardDefaultPreventionRegistration is not null)
            {
                await this.keyboardDefaultPreventionRegistration.DisposeAsync();
                this.keyboardDefaultPreventionRegistration = null;
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Closes an open menu after a pointer interaction outside this component instance.
        /// </summary>
        /// <returns>A task representing the asynchronous state and render update.</returns>
        [JSInvokable]
        public async Task DismissFromOutsideClickAsync()
        {
            if (this.isDisposed || !this.IsOpen)
            {
                return;
            }

            await this.CloseMenuAsync();

            if (!this.isDisposed)
            {
                await this.InvokeAsync(this.StateHasChanged);
            }
        }

        /// <summary>
        /// Gets the final CSS class list applied to the action-menu root.
        /// </summary>
        /// <returns>The root CSS class list.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass(
                "mb-action-menu",
                CssClassBuilder.When("mb-action-menu--open", this.IsOpen));
        }

        /// <summary>
        /// Gets the CSS class list applied to the trigger.
        /// </summary>
        /// <returns>The trigger CSS class list.</returns>
        private string GetTriggerCssClass()
        {
            return CssClassBuilder.Build(
                "mb-action-menu__trigger",
                CssClassBuilder.When("mb-action-menu__trigger--open", this.IsOpen),
                this.TriggerClass);
        }

        /// <summary>
        /// Gets the CSS class list applied to the popup menu.
        /// </summary>
        /// <returns>The popup CSS class list.</returns>
        private string GetMenuCssClass()
        {
            return CssClassBuilder.Build(
                "mb-action-menu__menu",
                this.Alignment == ActionMenuAlignment.Start
                    ? "mb-action-menu__menu--start"
                    : "mb-action-menu__menu--end");
        }

        /// <summary>
        /// Gets the CSS class list applied to an action item.
        /// </summary>
        /// <param name="item">The action item.</param>
        /// <returns>The item CSS class list.</returns>
        private static string GetItemCssClass(ActionMenuItem item)
        {
            return CssClassBuilder.Build(
                "mb-action-menu__item",
                CssClassBuilder.When("mb-action-menu__item--destructive", item.Destructive),
                CssClassBuilder.When("mb-action-menu__item--selected", item.IsSelected),
                CssClassBuilder.When("mb-action-menu__item--disabled", item.Disabled));
        }

        /// <summary>
        /// Gets a value indicating whether the trigger is disabled.
        /// </summary>
        /// <returns>True when the trigger cannot open a menu; otherwise, false.</returns>
        private bool IsTriggerDisabled()
        {
            return this.Disabled || this.Items.Count == 0 || this.isSelecting || this.isDisposed;
        }

        /// <summary>
        /// Gets the ARIA role used by each menu item.
        /// </summary>
        /// <returns>The menu-item role.</returns>
        private string GetItemRole()
        {
            return this.IsSelectionMenu ? "menuitemradio" : "menuitem";
        }

        /// <summary>
        /// Gets the selected-state announcement for a selection-menu item.
        /// </summary>
        /// <param name="item">The menu item.</param>
        /// <returns>The selected state, or null for a standard action menu.</returns>
        private string GetAriaChecked(ActionMenuItem item)
        {
            if (!this.IsSelectionMenu)
            {
                return null;
            }

            return item.IsSelected ? "true" : "false";
        }

        /// <summary>
        /// Toggles the menu from its trigger.
        /// </summary>
        /// <param name="args">The mouse event arguments.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ToggleMenuAsync(MouseEventArgs args)
        {
            if (this.IsTriggerDisabled())
            {
                return;
            }

            if (this.IsOpen)
            {
                await this.CloseMenuAsync();
            }
            else
            {
                await this.OpenMenuAsync(this.FindEnabledIndex(0, 1), false);
            }
        }

        /// <summary>
        /// Selects an enabled menu item and closes the menu.
        /// </summary>
        /// <param name="item">The selected item.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SelectItemAsync(ActionMenuItem item)
        {
            if (!this.IsOpen || item.Disabled || this.isSelecting || this.isDisposed)
            {
                return;
            }

            await this.FocusTriggerAsync();

            if (this.isDisposed)
            {
                return;
            }

            this.isSelecting = true;

            try
            {
                await this.CloseMenuAsync();

                if (!this.isDisposed)
                {
                    await this.ItemSelected.InvokeAsync(item);
                }
            }
            finally
            {
                this.isSelecting = false;
            }
        }

        /// <summary>
        /// Handles trigger keyboard commands not already provided by the semantic button.
        /// </summary>
        /// <param name="args">The keyboard event arguments.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleTriggerKeyDownAsync(KeyboardEventArgs args)
        {
            switch (args.Key)
            {
                case ArrowDownKey:
                case "Down":
                    await this.OpenMenuAsync(this.FindEnabledIndex(0, 1), true);
                    break;
                case ArrowUpKey:
                case "Up":
                    await this.OpenMenuAsync(this.FindEnabledIndex(this.Items.Count - 1, -1), true);
                    break;
                case "Escape":
                    await this.CloseMenuAsync(true);
                    break;
                case "Tab":
                    await this.CloseMenuAsync();
                    break;
            }
        }

        /// <summary>
        /// Handles keyboard navigation within the popup menu.
        /// </summary>
        /// <param name="args">The keyboard event arguments.</param>
        /// <param name="itemIndex">The source item index.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleItemKeyDownAsync(KeyboardEventArgs args, int itemIndex)
        {
            switch (args.Key)
            {
                case "Enter":
                case " ":
                case "Space":
                case "Spacebar":
                    if (this.IsEnabledIndex(itemIndex))
                    {
                        await this.SelectItemAsync(this.Items[itemIndex]);
                    }

                    break;
                case ArrowDownKey:
                case "Down":
                    this.FocusItem(this.FindNextEnabledIndex(itemIndex, 1));
                    break;
                case ArrowUpKey:
                case "Up":
                    this.FocusItem(this.FindNextEnabledIndex(itemIndex, -1));
                    break;
                case "Home":
                    this.FocusItem(this.FindEnabledIndex(0, 1));
                    break;
                case "End":
                    this.FocusItem(this.FindEnabledIndex(this.Items.Count - 1, -1));
                    break;
                case "Escape":
                    await this.CloseMenuAsync(true);
                    break;
                case "Tab":
                    await this.CloseMenuAsync();
                    break;
            }
        }

        /// <summary>
        /// Opens the menu and optionally moves focus to a menu item.
        /// </summary>
        /// <param name="itemIndex">The enabled item index used for roving focus.</param>
        /// <param name="moveFocus">A value indicating whether focus moves after rendering.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task OpenMenuAsync(int itemIndex, bool moveFocus)
        {
            if (this.IsTriggerDisabled())
            {
                return;
            }

            var stateChanged = !this.IsOpen;

            this.IsOpen = true;
            this.FocusedItemIndex = itemIndex;
            this.pendingFocusIndex = moveFocus && itemIndex >= 0 ? itemIndex : null;
            this.pendingTriggerFocus = false;

            if (stateChanged)
            {
                await this.IsOpenChanged.InvokeAsync(true);
            }
        }

        /// <summary>
        /// Closes the menu and optionally schedules focus restoration to its trigger.
        /// </summary>
        /// <param name="restoreTriggerFocus">Whether focus should return to the trigger after rendering.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task CloseMenuAsync(bool restoreTriggerFocus = false)
        {
            if (!this.IsOpen || this.isDisposed)
            {
                return;
            }

            this.IsOpen = false;
            this.FocusedItemIndex = -1;
            this.pendingFocusIndex = null;
            this.pendingTriggerFocus = restoreTriggerFocus;

            await this.IsOpenChanged.InvokeAsync(false);
        }

        /// <summary>
        /// Moves focus to the trigger owned by this menu instance.
        /// </summary>
        /// <returns>A task representing the asynchronous focus request.</returns>
        private async Task FocusTriggerAsync()
        {
            if (this.isDisposed)
            {
                return;
            }

            if (this.TriggerContent is null && !this.ShowChevron)
            {
                if (this.triggerIconButton is not null)
                {
                    await this.triggerIconButton.FocusAsync();
                }

                return;
            }

            await this.triggerElement.FocusAsync(true);
        }

        /// <summary>
        /// Schedules focus for an enabled menu item.
        /// </summary>
        /// <param name="itemIndex">The item index.</param>
        private void FocusItem(int itemIndex)
        {
            if (this.isDisposed || !this.IsEnabledIndex(itemIndex))
            {
                return;
            }

            this.FocusedItemIndex = itemIndex;
            this.pendingFocusIndex = itemIndex;
        }

        /// <summary>
        /// Finds the next enabled item, wrapping around the collection.
        /// </summary>
        /// <param name="sourceIndex">The source item index.</param>
        /// <param name="direction">The navigation direction.</param>
        /// <returns>The next enabled index, or negative one when none exists.</returns>
        private int FindNextEnabledIndex(int sourceIndex, int direction)
        {
            if (this.Items.Count == 0)
            {
                return -1;
            }

            for (var offset = 1; offset <= this.Items.Count; offset++)
            {
                var candidate = (sourceIndex + (offset * direction) + this.Items.Count) % this.Items.Count;

                if (this.IsEnabledIndex(candidate))
                {
                    return candidate;
                }
            }

            return -1;
        }

        /// <summary>
        /// Finds an enabled item from a starting index without wrapping.
        /// </summary>
        /// <param name="startIndex">The first index to inspect.</param>
        /// <param name="direction">The search direction.</param>
        /// <returns>The enabled item index, or negative one when none exists.</returns>
        private int FindEnabledIndex(int startIndex, int direction)
        {
            for (var index = startIndex; index >= 0 && index < this.Items.Count; index += direction)
            {
                if (this.IsEnabledIndex(index))
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// Checks whether an index identifies an enabled menu item.
        /// </summary>
        /// <param name="itemIndex">The item index.</param>
        /// <returns>True when the item exists and is enabled; otherwise, false.</returns>
        private bool IsEnabledIndex(int itemIndex)
        {
            return itemIndex >= 0 && itemIndex < this.Items.Count && !this.Items[itemIndex].Disabled;
        }
    }
}
