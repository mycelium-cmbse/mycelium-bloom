// ------------------------------------------------------------------------------------------------
// <copyright file="ActivationStatusExtensions.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Extensions
{
    /// <summary>
    /// Gives what the lists show about the status of an account.
    /// </summary>
    public static class ActivationStatusExtensions
    {
        /// <summary>
        /// Defines what the lists show about the status of an account.
        /// </summary>
        /// <param name="status">The status whose label is read.</param>
        extension(ActivationStatus status)
        {
            /// <summary>
            /// Gets the label of the status of an account. An account is active or deactivated for the users
            /// (SSS-IA-USR-B6P), so a suspended or an archived account reads "Deactivated".
            /// </summary>
            /// <returns>The label to show.</returns>
            public string GetLabel()
            {
                return status is ActivationStatus.Suspended or ActivationStatus.Archived ? "Deactivated" : status.Humanize();
            }
        }
    }
}
