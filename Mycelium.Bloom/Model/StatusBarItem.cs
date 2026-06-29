// ------------------------------------------------------------------------------------------------
// <copyright file="StatusBarItem.cs" company="Starion Group S.A.">
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
    /// Represents an item displayed inside a workspace status bar.
    /// </summary>
    public sealed class StatusBarItem
    {
        /// <summary>
        /// Gets or sets the visible item label.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the visible item value.
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the status indicator variant.
        /// </summary>
        public StatusIndicatorVariant Variant { get; set; } = StatusIndicatorVariant.Neutral;

        /// <summary>
        /// Gets or sets whether the status indicator dot should be displayed.
        /// </summary>
        public bool ShowIndicator { get; set; }
    }
}
