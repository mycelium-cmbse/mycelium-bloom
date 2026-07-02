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
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Represents a compact confirmation dialog for important or dangerous actions.
    /// </summary>
    public partial class ConfirmDialog : ComponentBase
    {
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
        /// Gets or sets the dialog title.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the dialog message.
        /// </summary>
        [Parameter]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the confirmation button text.
        /// </summary>
        [Parameter]
        public string ConfirmText { get; set; } = "Confirm";

        /// <summary>
        /// Gets or sets the cancel button text.
        /// </summary>
        [Parameter]
        public string CancelText { get; set; } = "Cancel";

        /// <summary>
        /// Gets or sets the dialog variant.
        /// </summary>
        [Parameter]
        public ConfirmDialogVariant Variant { get; set; } = ConfirmDialogVariant.Default;

        /// <summary>
        /// Gets or sets a value indicating whether the confirm action is in progress.
        /// </summary>
        [Parameter]
        public bool IsConfirming { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the dialog should close after confirmation.
        /// </summary>
        [Parameter]
        public bool CloseOnConfirm { get; set; } = true;

        /// <summary>
        /// Gets or sets the callback invoked when the confirm button is selected.
        /// </summary>
        [Parameter]
        public EventCallback Confirmed { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the cancel button is selected.
        /// </summary>
        [Parameter]
        public EventCallback Cancelled { get; set; }

        /// <summary>
        /// Gets or sets additional CSS classes applied to the dialog shell.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional unmatched attributes applied to the dialog shell.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the final CSS class list applied to the dialog shell.
        /// </summary>
        /// <returns>The confirm dialog CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-confirm-dialog",
                this.Class);

            return cssClass;
        }

        /// <summary>
        /// Gets the final CSS class list applied to the variant icon.
        /// </summary>
        /// <returns>The variant icon CSS class list.</returns>
        private string GetIconCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-confirm-dialog__icon",
                this.GetVariantIconClass());

            return cssClass;
        }

        /// <summary>
        /// Gets the icon class matching the selected variant.
        /// </summary>
        /// <returns>The variant icon CSS class.</returns>
        private string GetVariantIconClass()
        {
            var cssClass = this.Variant switch
            {
                ConfirmDialogVariant.Danger => "mb-confirm-dialog__icon--danger",
                ConfirmDialogVariant.Warning => "mb-confirm-dialog__icon--warning",
                _ => "mb-confirm-dialog__icon--default"
            };

            return cssClass;
        }

        /// <summary>
        /// Gets the compact icon text matching the selected variant.
        /// </summary>
        /// <returns>The icon text.</returns>
        private string GetIconText()
        {
            var iconText = this.Variant switch
            {
                ConfirmDialogVariant.Danger => "!",
                ConfirmDialogVariant.Warning => "!",
                _ => "?"
            };

            return iconText;
        }

        /// <summary>
        /// Gets the confirm button visual variant.
        /// </summary>
        /// <returns>The confirm button variant.</returns>
        private ButtonVariant GetConfirmButtonVariant()
        {
            var variant = this.Variant == ConfirmDialogVariant.Danger
                ? ButtonVariant.Danger
                : ButtonVariant.Primary;

            return variant;
        }

        /// <summary>
        /// Handles cancellation.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleCancelAsync()
        {
            await this.Cancelled.InvokeAsync();
            await this.IsOpenChanged.InvokeAsync(false);
        }

        /// <summary>
        /// Handles confirmation.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleConfirmAsync()
        {
            await this.Confirmed.InvokeAsync();

            if (this.CloseOnConfirm)
            {
                await this.IsOpenChanged.InvokeAsync(false);
            }
        }
    }
}
