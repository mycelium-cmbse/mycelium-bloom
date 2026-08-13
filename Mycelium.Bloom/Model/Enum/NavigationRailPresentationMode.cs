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
    /// Defines how a navigation rail responds to its primary presentation control.
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
        /// The rail rests collapsed and expands temporarily while hovered.
        /// </summary>
        ExpandOnHover
    }
}
