// ------------------------------------------------------------------------------------------------
// <copyright file="NotificationToast.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Atoms.NotificationToast
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Represents a compact toast notification.
    /// </summary>
    public partial class NotificationToast : ComponentBase
    {
        /// <summary>
        /// Gets or sets the notification to render.
        /// </summary>
        [Parameter]
        public ToastNotification Notification { get; set; } = new();

        /// <summary>
        /// Gets or sets the callback invoked when the notification is dismissed.
        /// </summary>
        [Parameter]
        public EventCallback<string> Dismissed { get; set; }

        /// <summary>
        /// Gets or sets additional CSS classes applied to the toast.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional unmatched attributes applied to the toast element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the final CSS class list applied to the toast.
        /// </summary>
        /// <returns>The toast CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-notification-toast",
                this.GetVariantClass(),
                this.Class);

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class matching the selected notification variant.
        /// </summary>
        /// <returns>The CSS class for the selected notification variant.</returns>
        private string GetVariantClass()
        {
            var cssClass = this.Notification.Variant switch
            {
                ToastNotificationVariant.Success => "mb-notification-toast--success",
                ToastNotificationVariant.Warning => "mb-notification-toast--warning",
                ToastNotificationVariant.Danger => "mb-notification-toast--danger",
                _ => "mb-notification-toast--info"
            };

            return cssClass;
        }

        /// <summary>
        /// Gets the accessible dismissal label.
        /// </summary>
        /// <returns>The dismissal label.</returns>
        private string GetDismissLabel()
        {
            var label = string.IsNullOrWhiteSpace(this.Notification.Title)
                ? "Dismiss notification"
                : $"Dismiss {this.Notification.Title}";

            return label;
        }

        /// <summary>
        /// Handles dismissing the notification.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private Task HandleDismissAsync()
        {
            var task = this.Dismissed.InvokeAsync(this.Notification.Id);

            return task;
        }
    }
}
