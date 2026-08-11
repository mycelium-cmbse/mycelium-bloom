// ------------------------------------------------------------------------------------------------
// <copyright file="NavigationRailItem.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    /// <summary>
    /// Represents a destination displayed in a navigation rail.
    /// </summary>
    public sealed class NavigationRailItem
    {
        /// <summary>
        /// Gets or sets the stable destination identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the destination label and accessible name.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Lucide icon name used for the destination.
        /// </summary>
        public string IconName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this destination starts a visually separated section.
        /// </summary>
        public bool StartsNewSection { get; set; }
    }
}
