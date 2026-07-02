// ------------------------------------------------------------------------------------------------
// <copyright file="SplitButton.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Molecules.SplitButton
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Reusable Bloom split button for primary actions with related dropdown actions.
    /// </summary>
    public partial class SplitButton : ComponentBase
    {
        /// <summary>
        /// The base CSS class applied to split button dropdown items.
        /// </summary>
        private const string ItemCssClass = "mb-split-button__item";

        /// <summary>
        /// Gets or sets the visible primary action text.
        /// </summary>
        [Parameter]
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the visual variant of the split button.
        /// </summary>
        [Parameter]
        public ButtonVariant Variant { get; set; } = ButtonVariant.Primary;

        /// <summary>
        /// Gets or sets the size of the split button.
        /// </summary>
        [Parameter]
        public ButtonSize Size { get; set; } = ButtonSize.Medium;

        /// <summary>
        /// Gets or sets a value indicating whether the split button is disabled.
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the split button is in a loading state.
        /// </summary>
        [Parameter]
        public bool IsLoading { get; set; }

        /// <summary>
        /// Gets or sets the dropdown action items.
        /// </summary>
        [Parameter]
        public IReadOnlyList<ActionMenuItem> Items { get; set; } = [];

        /// <summary>
        /// Gets or sets the callback invoked when the primary action is selected.
        /// </summary>
        [Parameter]
        public EventCallback PrimaryAction { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when a dropdown action item is selected.
        /// </summary>
        [Parameter]
        public EventCallback<ActionMenuItem> ItemSelected { get; set; }

        /// <summary>
        /// Gets or sets the dropdown trigger title.
        /// </summary>
        [Parameter]
        public string MenuTitle { get; set; } = "More actions";

        /// <summary>
        /// Gets or sets the dropdown trigger aria-label.
        /// </summary>
        [Parameter]
        public string MenuAriaLabel { get; set; } = "More actions";

        /// <summary>
        /// Gets or sets additional CSS classes.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets unmatched attributes passed to the split button wrapper.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets whether the dropdown menu is currently open.
        /// </summary>
        private bool IsOpen { get; set; }

        /// <summary>
        /// Gets a value indicating whether the primary and toggle buttons should be disabled.
        /// </summary>
        /// <returns>A value indicating whether the split button is disabled.</returns>
        private bool IsDisabled()
        {
            var isDisabled = this.Disabled || this.IsLoading;

            return isDisabled;
        }

        /// <summary>
        /// Gets a value indicating whether the dropdown toggle should be disabled.
        /// </summary>
        /// <returns>A value indicating whether the dropdown toggle is disabled.</returns>
        private bool IsToggleDisabled()
        {
            var isDisabled = this.IsDisabled() || this.Items.Count == 0;

            return isDisabled;
        }

        /// <summary>
        /// Gets the final CSS class list applied to the split button wrapper.
        /// </summary>
        /// <returns>The split button CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-split-button",
                this.GetVariantClass(),
                this.GetSizeClass(),
                CssClassBuilder.When("mb-split-button--disabled", this.IsDisabled()),
                this.Class);

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class matching the selected button variant.
        /// </summary>
        /// <returns>The variant CSS class.</returns>
        private string GetVariantClass()
        {
            var cssClass = this.Variant switch
            {
                ButtonVariant.Secondary => "mb-split-button--secondary",
                ButtonVariant.Ghost => "mb-split-button--ghost",
                ButtonVariant.Danger => "mb-split-button--danger",
                _ => "mb-split-button--primary"
            };

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class matching the selected button size.
        /// </summary>
        /// <returns>The size CSS class.</returns>
        private string GetSizeClass()
        {
            var cssClass = this.Size switch
            {
                ButtonSize.Small => "mb-split-button--small",
                ButtonSize.Large => "mb-split-button--large",
                _ => "mb-split-button--medium"
            };

            return cssClass;
        }

        /// <summary>
        /// Invokes the primary action when the split button is enabled.
        /// </summary>
        private async Task InvokePrimaryActionAsync()
        {
            if (!this.IsDisabled())
            {
                await this.PrimaryAction.InvokeAsync();
            }
        }

        /// <summary>
        /// Toggles whether the dropdown menu is open.
        /// </summary>
        private void ToggleMenu()
        {
            if (!this.IsToggleDisabled())
            {
                this.IsOpen = !this.IsOpen;
            }
        }

        /// <summary>
        /// Selects the provided dropdown item when it is enabled.
        /// </summary>
        /// <param name="item">The selected dropdown item.</param>
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
