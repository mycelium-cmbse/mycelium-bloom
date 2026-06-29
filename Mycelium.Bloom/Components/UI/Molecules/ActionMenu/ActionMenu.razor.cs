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

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Reusable Bloom action menu for compact toolbar and header actions.
    /// </summary>
    public partial class ActionMenu : ComponentBase
    {
        /// <summary>
        /// Gets or sets the available action menu items.
        /// </summary>
        [Parameter]
        public IReadOnlyList<ActionMenuItem> Items { get; set; } = [];

        /// <summary>
        /// Gets or sets the trigger text rendered when the trigger is not icon-only.
        /// </summary>
        [Parameter]
        public string TriggerText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the trigger title.
        /// </summary>
        [Parameter]
        public string TriggerTitle { get; set; } = "Open menu";

        /// <summary>
        /// Gets or sets the trigger aria-label.
        /// </summary>
        [Parameter]
        public string TriggerAriaLabel { get; set; } = "Open menu";

        /// <summary>
        /// Gets or sets whether the trigger should render only the icon.
        /// </summary>
        [Parameter]
        public bool IsIconOnly { get; set; } = true;

        /// <summary>
        /// Gets or sets the trigger icon text.
        /// </summary>
        [Parameter]
        public string Icon { get; set; } = "⋯";

        /// <summary>
        /// Gets or sets the menu placement relative to the trigger.
        /// </summary>
        [Parameter]
        public ActionMenuPlacement Placement { get; set; } = ActionMenuPlacement.BottomEnd;

        /// <summary>
        /// Gets or sets the callback invoked when an enabled action item is selected.
        /// </summary>
        [Parameter]
        public EventCallback<ActionMenuItem> ItemSelected { get; set; }

        /// <summary>
        /// Gets or sets additional CSS classes.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets unmatched attributes passed to the action menu wrapper.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets whether the menu is currently open.
        /// </summary>
        private bool IsOpen { get; set; }

        /// <summary>
        /// Gets the final CSS class list applied to the action menu wrapper.
        /// </summary>
        /// <returns>The action menu CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-action-menu",
                this.Class);

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class list applied to the trigger button.
        /// </summary>
        /// <returns>The trigger CSS class list.</returns>
        private string GetTriggerClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-action-menu__trigger",
                CssClassBuilder.When("mb-action-menu__trigger--icon-only", this.IsIconOnly));

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class list applied to the menu.
        /// </summary>
        /// <returns>The menu CSS class list.</returns>
        private string GetMenuClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-action-menu__menu",
                this.GetPlacementClass());

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class matching the selected menu placement.
        /// </summary>
        /// <returns>The menu placement CSS class.</returns>
        private string GetPlacementClass()
        {
            var cssClass = this.Placement switch
            {
                ActionMenuPlacement.BottomStart => "mb-action-menu__menu--bottom-start",
                _ => "mb-action-menu__menu--bottom-end"
            };

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class list applied to an action item.
        /// </summary>
        /// <param name="item">The action item.</param>
        /// <returns>The action item CSS class list.</returns>
        private string GetItemClass(ActionMenuItem item)
        {
            var cssClass = CssClassBuilder.Build(
                "mb-action-menu__item",
                CssClassBuilder.When("mb-action-menu__item--danger", item.Variant == ActionMenuItemVariant.Danger),
                CssClassBuilder.When("mb-action-menu__item--disabled", item.Disabled),
                CssClassBuilder.When("mb-action-menu__item--separator", item.SeparatorBefore));

            return cssClass;
        }

        /// <summary>
        /// Toggles whether the menu is open.
        /// </summary>
        private void ToggleMenu()
        {
            this.IsOpen = !this.IsOpen;
        }

        /// <summary>
        /// Selects the provided action item when it is enabled.
        /// </summary>
        /// <param name="item">The selected action item.</param>
        private async Task SelectItemAsync(ActionMenuItem item)
        {
            if (!item.Disabled)
            {
                this.IsOpen = false;

                await this.ItemSelected.InvokeAsync(item);
            }
        }
    }
}
