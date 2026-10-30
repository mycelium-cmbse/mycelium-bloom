// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationContext.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    /// <summary>
    /// Describes everything the layout of an organization page needs from one request: who is using Bloom, which
    /// organization is designated and what the account may do in it.
    /// </summary>
    /// <param name="Account">The current account.</param>
    /// <param name="Organization">The designated organization.</param>
    /// <param name="Capabilities">What the current account may do in the organization.</param>
    public sealed record OrganizationContext(Account Account, Organization Organization, OrganizationCapabilities Capabilities);
}
