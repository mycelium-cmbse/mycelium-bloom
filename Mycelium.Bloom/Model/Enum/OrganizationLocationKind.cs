// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationLocationKind.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Enum
{
    /// <summary>
    /// Defines what a browser location designates in the account and organization area.
    /// </summary>
    public enum OrganizationLocationKind
    {
        /// <summary>
        /// The location belongs to neither the account page nor an organization page.
        /// </summary>
        None,

        /// <summary>
        /// The location is the account dashboard.
        /// </summary>
        Account,

        /// <summary>
        /// The location is a page of an organization.
        /// </summary>
        Organization
    }
}
