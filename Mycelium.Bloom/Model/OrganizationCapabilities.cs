// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationCapabilities.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    /// <summary>
    /// Describes what the current account may do in an organization.
    /// </summary>
    /// <param name="CanEditSettings">A value indicating whether the account may edit the organization settings.</param>
    public sealed record OrganizationCapabilities(bool CanEditSettings);
}
