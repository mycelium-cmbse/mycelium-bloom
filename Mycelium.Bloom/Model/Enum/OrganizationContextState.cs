// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationContextState.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Enum
{
    /// <summary>
    /// Defines the states of the organization designated by the current location.
    /// </summary>
    public enum OrganizationContextState
    {
        /// <summary>
        /// The organization is being resolved.
        /// </summary>
        Loading,

        /// <summary>
        /// The account, the organization and the capabilities of the account are available.
        /// </summary>
        Ready,

        /// <summary>
        /// The organization does not exist or the account has no access to it.
        /// </summary>
        NotFound,

        /// <summary>
        /// The organization could not be resolved.
        /// </summary>
        Error
    }
}
