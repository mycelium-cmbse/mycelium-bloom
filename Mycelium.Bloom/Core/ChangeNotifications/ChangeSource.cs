// ------------------------------------------------------------------------------------------------
// <copyright file="ChangeSource.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ChangeNotifications
{
    /// <summary>
    /// Identifies whether the current user originated the mutation, independently of transport.
    /// </summary>
    public enum ChangeSource
    {
        /// <summary>Indicates a mutation originating from the current user.</summary>
        Local,

        /// <summary>Indicates a mutation originating from another user.</summary>
        Remote
    }
}
