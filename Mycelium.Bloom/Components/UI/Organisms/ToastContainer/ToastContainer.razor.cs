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

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Represents a fixed-position stack of toast notifications.
    /// </summary>
    public partial class ToastContainer : ComponentBase
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
        /// Gets or sets additional CSS classes applied to the container.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional unmatched attributes applied to the container element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets a value indicating whether notifications should be rendered.
        /// </summary>
        /// <returns>A value indicating whether notifications are available.</returns>
        private bool HasNotifications()
        {
            var hasNotifications = this.Notifications.Count > 0;

            return hasNotifications;
        }

        /// <summary>
        /// Gets the final CSS class list applied to the container.
        /// </summary>
        /// <returns>The toast container CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-toast-container",
                this.Class);

            return cssClass;
        }
    }
}
