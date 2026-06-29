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
        /// Gets or sets the unique breadcrumb value.
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the visible breadcrumb label.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the breadcrumb item represents the current page.
        /// </summary>
        public bool IsCurrent { get; set; }
    }
}
