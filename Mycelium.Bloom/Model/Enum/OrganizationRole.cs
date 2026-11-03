// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationRole.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Enum
{
    /// <summary>
    /// Defines the organization-level roles that an account can hold. Outside Collaborators hold no organization role.
    /// </summary>
    /// <remarks>
    /// Placeholder role of the organization screens, used only to configure the placeholder service, to be replaced by
    /// the role of the Fabric API when the Fabric client is bound.
    /// </remarks>
    public enum OrganizationRole
    {
        /// <summary>
        /// Owns the organization and manages its members, settings and projects.
        /// </summary>
        OrganizationAdministrator,

        /// <summary>
        /// Participates in the organization and creates projects when the organization settings permit it.
        /// </summary>
        OrganizationMember
    }
}
