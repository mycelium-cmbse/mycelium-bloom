// ------------------------------------------------------------------------------------------------
// <copyright file="ToastNotification.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Represents a notification displayed in a toast surface.
    /// </summary>
    public sealed class ToastNotification
    {
        /// <summary>
        /// Gets or sets the stable notification identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the notification title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional notification message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the notification visual variant.
        /// </summary>
        public ToastNotificationVariant Variant { get; set; } = ToastNotificationVariant.Info;

        /// <summary>
        /// Gets or sets a value indicating whether the notification can be dismissed.
        /// </summary>
        public bool IsDismissible { get; set; } = true;
    }
}
