// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationErrorCodes.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Organization
{
    /// <summary>
    /// Defines the codes of the errors that the organization services return instead of throwing.
    /// </summary>
    public static class OrganizationErrorCodes
    {
        /// <summary>
        /// The requested organization, account or page does not exist.
        /// </summary>
        public const string NotFound = "Organization.NotFound";

        /// <summary>
        /// The current account is not allowed to access the requested organization or operation.
        /// </summary>
        public const string Forbidden = "Organization.Forbidden";

        /// <summary>
        /// The organization back end could not answer the request.
        /// </summary>
        public const string Unavailable = "Organization.Unavailable";
    }
}
