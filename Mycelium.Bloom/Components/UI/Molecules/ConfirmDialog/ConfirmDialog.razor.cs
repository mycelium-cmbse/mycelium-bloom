// ------------------------------------------------------------------------------------------------
// <copyright file="ConfirmDialog.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Molecules.ConfirmDialog
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Model.Enum;

    using BlueprintButtonVariant = BlazorBlueprint.Components.ButtonVariant;

    /// <summary>
    /// Represents a compact dialog for confirming important actions.
    /// </summary>
    public partial class ConfirmDialog : BloomComponentBase
    {
        /// <summary>
        /// Indicates whether a dialog action callback is currently running.
        /// </summary>
        private bool isActionInProgress;

        /// <summary>
        /// Gets or sets a value indicating whether the dialog is open.
        /// </summary>
        [Parameter]
        public bool IsOpen { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the open state changes.
        /// </summary>
        [Parameter]
        public EventCallback<bool> IsOpenChanged { get; set; }

        /// <summary>
        /// Gets or sets the stable element that receives focus after the dialog closes.
        /// </summary>
        [Parameter]
        public ElementReference? FocusReturnTarget { get; set; }

        /// <summary>
        /// Gets or sets the confirmation dialog title.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional confirmation dialog description.
        /// </summary>
        [Parameter]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the confirmation button text.
        /// </summary>
        [Parameter]
        public string ConfirmText { get; set; } = "Confirm";

        /// <summary>
        /// Gets or sets the cancellation button text.
        /// </summary>
        [Parameter]
        public string CancelText { get; set; } = "Cancel";

        /// <summary>
        /// Gets or sets the confirmation dialog visual variant.
        /// </summary>
        [Parameter]
        public ConfirmDialogVariant Variant { get; set; } = ConfirmDialogVariant.Default;

        /// <summary>
        /// Gets or sets a value indicating whether confirmation is in progress.
        /// </summary>
        [Parameter]
        public bool IsConfirming { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the dialog closes after confirmation.
        /// </summary>
        [Parameter]
        public bool CloseOnConfirm { get; set; } = true;

        /// <summary>
        /// Gets or sets the callback invoked when the action is confirmed.
        /// </summary>
        [Parameter]
        public EventCallback Confirmed { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the action is cancelled.
        /// </summary>
        [Parameter]
        public EventCallback Cancelled { get; set; }

        /// <summary>
        /// Gets the final CSS class list applied to the visual indicator.
        /// </summary>
        /// <returns>The indicator CSS class list.</returns>
        private string GetIndicatorCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-confirm-dialog__indicator",
                $"mb-confirm-dialog__indicator--{this.GetVariantName()}");

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS name matching the selected confirmation dialog variant.
        /// </summary>
        /// <returns>The confirmation dialog variant name.</returns>
        private string GetVariantName()
        {
            var variantName = this.Variant switch
            {
                ConfirmDialogVariant.Warning => "warning",
                ConfirmDialogVariant.Danger => "danger",
                _ => "default"
            };

            return variantName;
        }

        /// <summary>
        /// Gets the compact indicator text matching the selected variant.
        /// </summary>
        /// <returns>The indicator text.</returns>
        private string GetIndicatorText()
        {
            var indicatorText = this.Variant == ConfirmDialogVariant.Default
                ? "?"
                : "!";

            return indicatorText;
        }

        /// <summary>
        /// Gets the Blueprint variant for the confirmation button.
        /// </summary>
        /// <returns>The confirmation-button variant.</returns>
        private BlueprintButtonVariant GetConfirmButtonVariant()
        {
            var buttonVariant = this.Variant == ConfirmDialogVariant.Danger
                ? BlueprintButtonVariant.Destructive
                : BlueprintButtonVariant.Default;

            return buttonVariant;
        }

        /// <summary>
        /// Handles cancellation and closes the dialog.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleCancelAsync()
        {
            if (this.IsActionInProgress())
            {
                return;
            }

            this.isActionInProgress = true;

            try
            {
                await this.Cancelled.InvokeAsync();
                await this.IsOpenChanged.InvokeAsync(false);
            }
            finally
            {
                this.isActionInProgress = false;
            }
        }

        /// <summary>
        /// Handles confirmation and optionally closes the dialog.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleConfirmAsync()
        {
            if (this.IsActionInProgress())
            {
                return;
            }

            this.isActionInProgress = true;

            try
            {
                await this.Confirmed.InvokeAsync();

                if (this.CloseOnConfirm)
                {
                    await this.IsOpenChanged.InvokeAsync(false);
                }
            }
            finally
            {
                this.isActionInProgress = false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether dialog actions should be disabled.
        /// </summary>
        /// <returns>A value indicating whether an action is already in progress.</returns>
        private bool IsActionInProgress()
        {
            var actionInProgress = this.IsConfirming || this.isActionInProgress;

            return actionInProgress;
        }
    }
}
