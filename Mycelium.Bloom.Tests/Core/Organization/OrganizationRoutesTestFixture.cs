// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationRoutesTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Core.Organization
{
    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Tests the reading and the building of the account and organization routes.
    /// </summary>
    [TestFixture]
    public sealed class OrganizationRoutesTestFixture
    {
        /// <summary>
        /// Verifies that the account dashboard is recognized whatever the form of the location.
        /// </summary>
        /// <param name="location">The location to read.</param>
        [TestCase("/account")]
        [TestCase("account")]
        [TestCase("/account/")]
        [TestCase("/ACCOUNT?tab=profile#top")]
        [TestCase("https://bloom.example.org/account")]
        public void VerifyParseRecognizesTheAccountDashboard(string location)
        {
            Assert.That(OrganizationRoutes.Parse(location), Is.EqualTo(OrganizationLocation.Account));
        }

        /// <summary>
        /// Verifies that every page of an organization is read back from the route that builds it, with a slug that
        /// needs escaping.
        /// </summary>
        /// <param name="section">The page of the organization.</param>
        [TestCase(OrganizationSection.Overview)]
        [TestCase(OrganizationSection.Projects)]
        [TestCase(OrganizationSection.Members)]
        [TestCase(OrganizationSection.Settings)]
        public void VerifyParseReadsBackEveryOrganizationPath(OrganizationSection section)
        {
            var path = OrganizationRoutes.GetOrganizationPath("r&d-lab", section);

            Assert.That(OrganizationRoutes.Parse(path), Is.EqualTo(OrganizationLocation.ForOrganization("r&d-lab", section)));
        }

        /// <summary>
        /// Verifies that the query string, the fragment, the trailing slash and the escaping of the slug are handled.
        /// </summary>
        [Test]
        public void VerifyParseIgnoresQueryAndFragmentAndUnescapesTheSlug()
        {
            var location = OrganizationRoutes.Parse("https://bloom.example.org/organizations/r%26d-lab/members/?page=2#top");

            Assert.That(location, Is.EqualTo(OrganizationLocation.ForOrganization("r&d-lab", OrganizationSection.Members)));
        }

        /// <summary>
        /// Verifies that a location outside the account and organization area, or with a slug that cannot designate an
        /// organization, designates nothing.
        /// </summary>
        /// <param name="location">The location to read.</param>
        [TestCase("")]
        [TestCase("/")]
        [TestCase("/workspace/modeling")]
        [TestCase("/organizations")]
        [TestCase("/organizations/orbital-systems/billing")]
        [TestCase("/organizations/orbital-systems/members/extra")]
        [TestCase("/account/settings")]
        [TestCase("/organizations/a%2Fb")]
        [TestCase("/organizations/a%5Cb")]
        [TestCase("/organizations/a%20b")]
        [TestCase("/organizations/a%0Ab")]
        [TestCase("/organizations/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        public void VerifyParseDesignatesNothingOutsideTheArea(string location)
        {
            Assert.That(OrganizationRoutes.Parse(location), Is.EqualTo(OrganizationLocation.None));
        }
    }
}
