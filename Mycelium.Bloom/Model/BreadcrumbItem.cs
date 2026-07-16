// ------------------------------------------------------------------------------------------------
// <copyright file="BreadcrumbItem.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    /// <summary>
    /// Represents an item displayed inside a breadcrumb trail.
    /// </summary>
    public sealed class BreadcrumbItem
    {
        /// <summary>
        /// Gets or sets the stable breadcrumb identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the visible breadcrumb label.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets an optional parent-owned navigation target.
        /// </summary>
        public string Target { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the breadcrumb is disabled.
        /// </summary>
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the breadcrumb represents the current page.
        /// </summary>
        public bool IsCurrent { get; set; }
    }
}
