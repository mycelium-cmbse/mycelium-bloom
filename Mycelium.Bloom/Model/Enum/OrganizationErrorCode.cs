// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationErrorCode.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Enum
{
    /// <summary>
    /// Defines the expected refusals that an organization service reports as data instead of exceptions.
    /// </summary>
    public enum OrganizationErrorCode
    {
        /// <summary>
        /// The requested organization or account does not exist.
        /// </summary>
        NotFound,

        /// <summary>
        /// The current account is not allowed to access the requested organization or operation.
        /// </summary>
        Forbidden,

        /// <summary>
        /// The organization back end could not answer the request.
        /// </summary>
        Unavailable
    }
}
