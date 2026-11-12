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
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Describes what the current account may do in an organization.
    /// </summary>
    /// <remarks>
    /// Placeholder model of the organization screens, to be replaced by the transfer object of the Fabric API when the
    /// Fabric client is bound. The capabilities will then come from the permissions of the account.
    /// </remarks>
    /// <param name="CanEditSettings">A value indicating whether the account may edit the organization settings.</param>
    /// <param name="CanCreateProject">A value indicating whether the account may create a project in the organization.</param>
    /// <param name="CanViewMembers">A value indicating whether the account may see the people of the organization (false by default).</param>
    /// <param name="CanInviteMembers">A value indicating whether the account may invite people to the organization.</param>
    /// <param name="CanViewOutsideCollaborators">A value indicating whether the account may see the Outside Collaborators, for audit.</param>
    public sealed record OrganizationCapabilities(bool CanEditSettings, bool CanCreateProject = false, bool CanViewMembers = false, bool CanInviteMembers = false, bool CanViewOutsideCollaborators = false)
    {
        /// <summary>
        /// Tells whether the account may open a page of the organization. A page that the account may not open looks the
        /// same as a missing page, so that it cannot be probed.
        /// </summary>
        /// <param name="section">The page of the organization.</param>
        /// <returns><see langword="true" /> when the account may open the page.</returns>
        public bool CanOpen(OrganizationSection section)
        {
            return section switch
            {
                OrganizationSection.Overview or OrganizationSection.Projects => true,
                OrganizationSection.Members => this.CanViewMembers,
                OrganizationSection.Settings => this.CanEditSettings,
                _ => false
            };
        }
    }
}
