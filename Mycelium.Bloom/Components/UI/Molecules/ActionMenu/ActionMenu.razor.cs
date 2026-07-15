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

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Represents a reusable action menu with instance-specific state and native keyboard interaction.
    /// </summary>
    public partial class ActionMenu : BloomComponentBase
    {
        /// <summary>
        /// The generated stable identifier shared by the trigger and menu.
        /// </summary>
        private readonly string generatedId = CreateGeneratedId("mb-action-menu");

        /// <summary>
        /// References to rendered menu-item buttons.
        /// </summary>
        private ElementReference[] itemElements = [];

        /// <summary>
        /// The menu-item index that should receive focus after rendering.
        /// </summary>
        private int? pendingFocusIndex;

        /// <summary>
        /// Indicates whether an item-selection callback is currently running.
        /// </summary>
        private bool isSelecting;

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
            if (this.itemElements.Length != this.Items.Count)
            {
                this.itemElements = new ElementReference[this.Items.Count];
            }

            if (!this.IsOpen || this.Disabled || this.Items.Count == 0)
            {
                if (this.Disabled || this.Items.Count == 0)
                {
                    this.IsOpen = false;
                }

                this.FocusedItemIndex = -1;
                this.pendingFocusIndex = null;
            }
            else if (this.IsOpen && !this.IsEnabledIndex(this.FocusedItemIndex))
            {
                this.FocusedItemIndex = this.FindEnabledIndex(0, 1);
            }
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (!this.IsOpen || this.pendingFocusIndex is not { } itemIndex || !this.IsEnabledIndex(itemIndex))
            {
                return;
            }

            this.pendingFocusIndex = null;

            await this.itemElements[itemIndex].FocusAsync();
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
        private string GetItemCssClass(ActionMenuItem item)
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
            return this.Disabled || this.Items.Count == 0 || this.isSelecting;
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
            return this.IsSelectionMenu ? (item.IsSelected ? "true" : "false") : null;
        }

        /// <summary>
        /// Gets the tab index for a menu item participating in roving focus.
        /// </summary>
        /// <param name="itemIndex">The menu-item index.</param>
        /// <returns>Zero for the current enabled item; otherwise, negative one.</returns>
        private int GetItemTabIndex(int itemIndex)
        {
            return itemIndex == this.FocusedItemIndex && this.IsEnabledIndex(itemIndex) ? 0 : -1;
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
            if (!this.IsOpen || item.Disabled || this.isSelecting)
            {
                return;
            }

            this.isSelecting = true;

            try
            {
                await this.CloseMenuAsync();
                await this.ItemSelected.InvokeAsync(item);
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
                case "ArrowDown":
                case "Down":
                    await this.OpenMenuAsync(this.FindEnabledIndex(0, 1), true);
                    break;
                case "ArrowUp":
                case "Up":
                    await this.OpenMenuAsync(this.FindEnabledIndex(this.Items.Count - 1, -1), true);
                    break;
                case "Escape":
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
                case "ArrowDown":
                case "Down":
                    this.FocusItem(this.FindNextEnabledIndex(itemIndex, 1));
                    break;
                case "ArrowUp":
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

            if (stateChanged)
            {
                await this.IsOpenChanged.InvokeAsync(true);
            }
        }

        /// <summary>
        /// Closes the menu.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task CloseMenuAsync()
        {
            if (!this.IsOpen)
            {
                return;
            }

            this.IsOpen = false;
            this.FocusedItemIndex = -1;
            this.pendingFocusIndex = null;

            await this.IsOpenChanged.InvokeAsync(false);
        }

        /// <summary>
        /// Schedules focus for an enabled menu item.
        /// </summary>
        /// <param name="itemIndex">The item index.</param>
        private void FocusItem(int itemIndex)
        {
            if (!this.IsEnabledIndex(itemIndex))
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
