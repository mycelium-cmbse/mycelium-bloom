// ------------------------------------------------------------------------------------------------
// <copyright file="NavigationRailPresentationMode.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Enum
{
    /// <summary>
    /// Defines how a navigation rail presents and reserves its width.
    /// </summary>
    public enum NavigationRailPresentationMode
    {
        /// <summary>
        /// The rail remains expanded.
        /// </summary>
        Expanded,

        /// <summary>
        /// The rail remains collapsed.
        /// </summary>
        Collapsed,

        /// <summary>
        /// The rail reserves its collapsed width and temporarily expands while hovered.
        /// </summary>
        ExpandOnHover
    }
}
