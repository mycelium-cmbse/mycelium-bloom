// ------------------------------------------------------------------------------------------------
// <copyright file="ToastContainer.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.ToastContainer
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Represents a top-right stack of toast notifications.
    /// </summary>
    public partial class ToastContainer : BloomComponentBase
    {
        /// <summary>
        /// Gets or sets the notifications to render.
        /// </summary>
        [Parameter]
        public IReadOnlyList<ToastNotification> Notifications { get; set; } = [];

        /// <summary>
        /// Gets or sets the callback invoked when a notification is dismissed.
        /// </summary>
        [Parameter]
        public EventCallback<string> Dismissed { get; set; }

        /// <summary>
        /// Validates that notifications provide identifiers suitable for stable keyed rendering.
        /// </summary>
        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (this.Notifications is null)
            {
                throw new InvalidOperationException("The toast notification collection cannot be null.");
            }

            var notificationIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var notification in this.Notifications)
            {
                if (notification is null)
                {
                    throw new InvalidOperationException("Toast notifications cannot contain null items.");
                }

                if (string.IsNullOrWhiteSpace(notification.Id))
                {
                    throw new InvalidOperationException("Toast notifications require a non-empty identifier.");
                }

                if (!notificationIds.Add(notification.Id))
                {
                    throw new InvalidOperationException("Toast notification identifiers must be unique.");
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether notifications are available.
        /// </summary>
        /// <returns>A value indicating whether notification items should be rendered.</returns>
        private bool HasNotifications()
        {
            var hasNotifications = this.Notifications.Count > 0;

            return hasNotifications;
        }

        /// <summary>
        /// Gets the final CSS class list applied to the toast container.
        /// </summary>
        /// <returns>The toast container CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = this.BuildRootCssClass("mb-toast-container");

            return cssClass;
        }
    }
}
