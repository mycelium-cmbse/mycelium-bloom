// ------------------------------------------------------------------------------------------------
// <copyright file="ApplicationErrorKind.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Enum
{
    /// <summary>
    /// Identifies the application error presentation selected by a page.
    /// </summary>
    public enum ApplicationErrorKind
    {
        /// <summary>
        /// An unavailable application route.
        /// </summary>
        NotFound,

        /// <summary>
        /// An unexpected request failure.
        /// </summary>
        ServerError
    }
}
