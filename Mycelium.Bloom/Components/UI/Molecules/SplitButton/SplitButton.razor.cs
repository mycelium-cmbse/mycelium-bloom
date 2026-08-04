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
    using BlueprintButtonSize = BlazorBlueprint.Components.ButtonSize;
    using BlueprintButtonVariant = BlazorBlueprint.Components.ButtonVariant;

    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Represents a primary action connected to a reusable secondary action menu.
    /// </summary>
    public partial class SplitButton : BloomComponentBase
    {
        /// <summary>
        /// Indicates whether the primary-action callback is currently running.
        /// </summary>
        private bool isPrimaryActionInProgress;

        /// <summary>
        /// Gets or sets the visible primary-action text.
        /// </summary>
        [Parameter]
        public string PrimaryText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the button visual variant.
        /// </summary>
        [Parameter]
        public ButtonVariant Variant { get; set; } = ButtonVariant.Primary;

        /// <summary>
        /// Gets or sets the button size.
        /// </summary>
        [Parameter]
        public ButtonSize Size { get; set; } = ButtonSize.Medium;

        /// <summary>
        /// Gets or sets a value indicating whether both actions are disabled.
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the primary action is loading.
        /// </summary>
        [Parameter]
        public bool IsLoading { get; set; }

        /// <summary>
        /// Gets or sets the secondary actions.
        /// </summary>
        [Parameter]
        public IReadOnlyList<ActionMenuItem> Items { get; set; } = [];

        /// <summary>
        /// Gets or sets the callback invoked by the primary action.
        /// </summary>
        [Parameter]
        public EventCallback PrimaryAction { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when a secondary action is selected.
        /// </summary>
        [Parameter]
        public EventCallback<ActionMenuItem> ItemSelected { get; set; }

        /// <summary>
        /// Gets or sets the accessible label of the secondary action trigger.
        /// </summary>
        [Parameter]
        public string MenuAriaLabel { get; set; } = "More actions";

        /// <summary>
        /// Gets the final CSS class list applied to the connected control.
        /// </summary>
        /// <returns>The split-button CSS class list.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass(
                "mb-split-button",
                this.GetVariantCssClass(),
                this.GetSizeCssClass(),
                CssClassBuilder.When("mb-split-button--disabled", this.IsDisabled()));
        }

        /// <summary>
        /// Gets the selected variant CSS class.
        /// </summary>
        /// <returns>The variant CSS class.</returns>
        private string GetVariantCssClass()
        {
            return this.Variant switch
            {
                ButtonVariant.Secondary => "mb-split-button--secondary",
                ButtonVariant.Ghost => "mb-split-button--ghost",
                ButtonVariant.Danger => "mb-split-button--danger",
                _ => "mb-split-button--primary"
            };
        }

        /// <summary>
        /// Gets the selected size CSS class.
        /// </summary>
        /// <returns>The size CSS class.</returns>
        private string GetSizeCssClass()
        {
            return this.Size switch
            {
                ButtonSize.Small => "mb-split-button--small",
                ButtonSize.Large => "mb-split-button--large",
                _ => "mb-split-button--medium"
            };
        }

        /// <summary>
        /// Gets the styled Blueprint variant matching Bloom's public contract.
        /// </summary>
        /// <returns>The Blueprint button variant.</returns>
        private BlueprintButtonVariant GetBlueprintVariant()
        {
            return this.Variant switch
            {
                ButtonVariant.Secondary => BlueprintButtonVariant.Outline,
                ButtonVariant.Ghost => BlueprintButtonVariant.Ghost,
                ButtonVariant.Danger => BlueprintButtonVariant.Destructive,
                _ => BlueprintButtonVariant.Default
            };
        }

        /// <summary>
        /// Gets the closest compact styled Blueprint size matching Bloom's public contract.
        /// </summary>
        /// <returns>The Blueprint button size.</returns>
        private BlueprintButtonSize GetBlueprintSize()
        {
            return this.Size switch
            {
                ButtonSize.Large => BlueprintButtonSize.Default,
                _ => BlueprintButtonSize.Small
            };
        }

        /// <summary>
        /// Gets a value indicating whether the split button is disabled or loading.
        /// </summary>
        /// <returns>True when actions cannot be invoked; otherwise, false.</returns>
        private bool IsDisabled()
        {
            return this.Disabled || this.IsActionInProgress();
        }

        /// <summary>
        /// Gets a value indicating whether the primary action is currently running.
        /// </summary>
        /// <returns>True when external or internal loading state is active; otherwise, false.</returns>
        private bool IsActionInProgress()
        {
            return this.IsLoading || this.isPrimaryActionInProgress;
        }

        /// <summary>
        /// Invokes the primary action when the component is enabled.
        /// </summary>
        /// <param name="args">The mouse event arguments.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandlePrimaryActionAsync(MouseEventArgs args)
        {
            if (this.IsDisabled())
            {
                return;
            }

            this.isPrimaryActionInProgress = true;

            try
            {
                await this.PrimaryAction.InvokeAsync();
            }
            finally
            {
                this.isPrimaryActionInProgress = false;
            }
        }

        /// <summary>
        /// Forwards an enabled secondary action to the parent.
        /// </summary>
        /// <param name="item">The selected secondary action.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleItemSelectedAsync(ActionMenuItem item)
        {
            if (!this.IsDisabled() && !item.Disabled)
            {
                await this.ItemSelected.InvokeAsync(item);
            }
        }
    }
}
