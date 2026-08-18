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
        /// Gets the stable destination identifier, which must be unique within one rail.
        /// </summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>
        /// Gets the destination label and accessible name.
        /// </summary>
        public string Label { get; init; } = string.Empty;

        /// <summary>
        /// Gets the Lucide icon name used for the destination.
        /// </summary>
        public string IconName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the key of the visual group containing this destination.
        /// </summary>
        public string GroupKey { get; init; } = string.Empty;
    }
}
