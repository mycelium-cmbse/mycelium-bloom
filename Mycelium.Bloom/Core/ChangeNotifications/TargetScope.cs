// ------------------------------------------------------------------------------------------------
// <copyright file="TargetScope.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ChangeNotifications
{
    /// <summary>Distinguishes subscription targets without runtime object-shape checks.</summary>
    internal enum TargetScope
    {
        /// <summary>Matches every change.</summary>
        Global,
        /// <summary>Matches an element identifier.</summary>
        Element,
        /// <summary>Matches containment ancestry.</summary>
        Subtree,
        /// <summary>Matches metaclass ancestry.</summary>
        Type
    }
}
