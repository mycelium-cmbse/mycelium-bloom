// ------------------------------------------------------------------------------------------------
// <copyright file="ErrorCodes.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core
{
    /// <summary>
    /// Defines the few codes shared by every error that a service or a ViewModel returns instead of throwing. A refusal is a
    /// shared code with its own description, and metadata when a screen must tell what it concerns (for example the field); a
    /// code is never created for one screen or one case.
    /// </summary>
    public static class ErrorCodes
    {
        /// <summary>
        /// The requested element, page or account does not exist.
        /// </summary>
        public const string NotFound = "Bloom.NotFound";

        /// <summary>
        /// The current account is not allowed to access the requested element or to run the operation.
        /// </summary>
        public const string Forbidden = "Bloom.Forbidden";

        /// <summary>
        /// The operation conflicts with what already exists, such as a name or an address that is already taken.
        /// </summary>
        public const string Conflict = "Bloom.Conflict";

        /// <summary>
        /// A value is missing or does not have the expected form.
        /// </summary>
        public const string Validation = "Bloom.Validation";

        /// <summary>
        /// The back end could not answer the request, or it failed for a reason that the caller cannot act on.
        /// </summary>
        public const string Unavailable = "Bloom.Unavailable";
    }
}
