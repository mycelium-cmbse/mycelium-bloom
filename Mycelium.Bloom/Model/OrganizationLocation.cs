// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationLocation.cs" company="Starion Group S.A.">
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
    /// Describes what a browser location designates in the account and organization area.
    /// </summary>
    /// <param name="Kind">The kind of page designated by the location.</param>
    /// <param name="Slug">The slug of the designated organization, or an empty string when there is none.</param>
    /// <param name="Section">The designated page of the organization.</param>
    public sealed record OrganizationLocation(OrganizationLocationKind Kind, string Slug, OrganizationSection Section)
    {
        /// <summary>
        /// Gets the location that belongs to neither the account page nor an organization page.
        /// </summary>
        public static OrganizationLocation None { get; } =
            new(OrganizationLocationKind.None, string.Empty, OrganizationSection.Overview);

        /// <summary>
        /// Gets the location of the account dashboard.
        /// </summary>
        public static OrganizationLocation Account { get; } =
            new(OrganizationLocationKind.Account, string.Empty, OrganizationSection.Overview);

        /// <summary>
        /// Creates the location of a page of an organization.
        /// </summary>
        /// <param name="slug">The slug of the organization.</param>
        /// <param name="section">The page of the organization.</param>
        /// <returns>The location of the page of the organization.</returns>
        public static OrganizationLocation ForOrganization(string slug, OrganizationSection section)
        {
            return new OrganizationLocation(OrganizationLocationKind.Organization, slug, section);
        }
    }
}
