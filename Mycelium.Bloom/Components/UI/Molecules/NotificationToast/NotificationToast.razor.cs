// ------------------------------------------------------------------------------------------------
// <copyright file="NotificationToast.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Molecules.NotificationToast
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Represents a compact notification toast.
    /// </summary>
    public partial class NotificationToast : BloomComponentBase
    {
        /// <summary>
        /// Gets or sets the notification to render.
        /// </summary>
        [Parameter]
        [EditorRequired]
        public ToastNotification Notification { get; set; } = new();

        /// <summary>
        /// Gets or sets the callback invoked with the notification identifier when dismissed.
        /// </summary>
        [Parameter]
        public EventCallback<string> Dismissed { get; set; }

        /// <summary>
        /// Gets the final CSS class list applied to the notification toast.
        /// </summary>
        /// <returns>The notification toast CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = this.BuildRootCssClass(
                "mb-notification-toast",
                this.GetVariantClass());

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class matching the selected notification variant.
        /// </summary>
        /// <returns>The notification variant CSS class.</returns>
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
        /// Gets the live-region role matching the selected notification variant.
        /// </summary>
        /// <returns>The notification accessibility role.</returns>
        private string GetRole()
        {
            var role = this.RequiresAssertiveAnnouncement()
                ? "alert"
                : "status";

            return role;
        }

        /// <summary>
        /// Gets the live-region priority matching the selected notification variant.
        /// </summary>
        /// <returns>The notification live-region priority.</returns>
        private string GetAriaLive()
        {
            var ariaLive = this.RequiresAssertiveAnnouncement()
                ? "assertive"
                : "polite";

            return ariaLive;
        }

        /// <summary>
        /// Gets a value indicating whether the selected variant requires an assertive announcement.
        /// </summary>
        /// <returns>A value indicating whether assertive accessibility semantics should be used.</returns>
        private bool RequiresAssertiveAnnouncement()
        {
            var requiresAssertiveAnnouncement = this.Notification.Variant is
                ToastNotificationVariant.Warning or ToastNotificationVariant.Danger;

            return requiresAssertiveAnnouncement;
        }

        /// <summary>
        /// Gets the accessible label for the dismiss button.
        /// </summary>
        /// <returns>The dismiss button label.</returns>
        private string GetDismissLabel()
        {
            var dismissLabel = string.IsNullOrWhiteSpace(this.Notification.Title)
                ? "Dismiss notification"
                : $"Dismiss {this.Notification.Title}";

            return dismissLabel;
        }

        /// <summary>
        /// Invokes the dismissal callback for the current notification.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private Task HandleDismissAsync()
        {
            var task = this.Dismissed.InvokeAsync(this.Notification.Id);

            return task;
        }
    }
}
