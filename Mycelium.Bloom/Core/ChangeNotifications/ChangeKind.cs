// ------------------------------------------------------------------------------------------------
// <copyright file="ChangeKind.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ChangeNotifications
{
    /// <summary>
    /// Identifies the committed structural change to an element.
    /// </summary>
    public enum ChangeKind
    {
        /// <summary>Indicates that an element was created.</summary>
        Created,

        /// <summary>Indicates that element properties were updated.</summary>
        Updated,

        /// <summary>Indicates that an element was deleted.</summary>
        Deleted,

        /// <summary>Indicates that element containment changed.</summary>
        Moved
    }
}
