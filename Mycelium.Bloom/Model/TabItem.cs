// ------------------------------------------------------------------------------------------------
// <copyright file="TabItem.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    /// <summary>
    /// Represents an item displayed inside a tab list.
    /// </summary>
    public sealed class TabItem
    {
        /// <summary>
        /// Gets or sets the unique tab value.
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the visible tab label.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the tab item is disabled.
        /// </summary>
        public bool Disabled { get; set; }
    }
}
