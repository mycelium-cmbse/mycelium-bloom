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
    using BlazorBlueprint.Primitives;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Maps Bloom action metadata onto the styled Blazor Blueprint dropdown menu.
    /// </summary>
    public sealed partial class ActionMenu : BloomComponentBase
    {
        /// <summary>
        /// Indicates whether an item callback is currently running.
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
        /// Gets or sets the supplementary native title text for the trigger.
        /// </summary>
        [Parameter]
        public string TriggerTitle { get; set; } = "Open actions";

        /// <summary>
        /// Gets or sets additional classes applied to the Blueprint trigger.
        /// </summary>
        [Parameter]
        public string TriggerClass { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional classes applied to portaled menu content.
        /// </summary>
        [Parameter]
        public string MenuClass { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the horizontal menu alignment.
        /// </summary>
        [Parameter]
        public ActionMenuAlignment Alignment { get; set; } = ActionMenuAlignment.End;

        /// <summary>
        /// Gets or sets a value indicating whether the menu should match its trigger width.
        /// </summary>
        [Parameter]
        public bool MatchTriggerWidth { get; set; }

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
        /// Gets or sets the callback invoked when an enabled item is selected.
        /// </summary>
        [Parameter]
        public EventCallback<ActionMenuItem> ItemSelected { get; set; }

        /// <summary>
        /// Gets the final CSS class list applied to the action-menu root.
        /// </summary>
        /// <returns>The root CSS class list.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass("mb-action-menu");
        }

        /// <summary>
        /// Gets the CSS class list applied to the Blueprint trigger.
        /// </summary>
        /// <returns>The trigger CSS class list.</returns>
        private string GetTriggerCssClass()
        {
            return CssClassBuilder.Build(
                "mb-action-menu__trigger",
                this.TriggerClass);
        }

        /// <summary>
        /// Gets the Blueprint popup alignment matching Bloom's public API.
        /// </summary>
        /// <returns>The primitive popup alignment.</returns>
        private PopoverAlign GetPopoverAlignment()
        {
            return this.Alignment == ActionMenuAlignment.Start
                ? PopoverAlign.Start
                : PopoverAlign.End;
        }

        /// <summary>
        /// Gets the CSS class list applied to an action item.
        /// </summary>
        /// <param name="item">The action item.</param>
        /// <returns>The item CSS class list.</returns>
        private static string GetItemCssClass(ActionMenuItem item)
        {
            return CssClassBuilder.Build(
                CssClassBuilder.When("text-destructive", item.Destructive),
                CssClassBuilder.When("bg-accent text-accent-foreground font-semibold", item.IsSelected));
        }

        /// <summary>
        /// Gets a value indicating whether the trigger cannot open a menu.
        /// </summary>
        /// <returns>True when opening is unavailable; otherwise, false.</returns>
        private bool IsTriggerDisabled()
        {
            return this.Disabled || this.Items.Count == 0 || this.isSelecting;
        }

        /// <summary>
        /// Forwards an enabled styled menu action exactly once while its callback is running.
        /// </summary>
        /// <param name="item">The selected action.</param>
        /// <returns>A task representing the callback.</returns>
        private async Task HandleItemSelectedAsync(ActionMenuItem item)
        {
            if (item.Disabled || this.isSelecting)
            {
                return;
            }

            this.isSelecting = true;

            try
            {
                await this.ItemSelected.InvokeAsync(item);
            }
            finally
            {
                this.isSelecting = false;
            }
        }
    }
}
