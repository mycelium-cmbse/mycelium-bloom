// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationCapabilitiesTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Model
{
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    [TestFixture]
    public sealed class OrganizationCapabilitiesTestFixture
    {
        [TestCase(OrganizationSection.Overview, false, true)]
        [TestCase(OrganizationSection.Projects, false, true)]
        [TestCase(OrganizationSection.Members, true, true)]
        [TestCase(OrganizationSection.Members, false, false)]
        [TestCase(OrganizationSection.Settings, true, true)]
        [TestCase(OrganizationSection.Settings, false, false)]
        [TestCase((OrganizationSection)99, true, false)]
        public void VerifyPagesFollowTheirCapability(OrganizationSection section, bool granted, bool expected)
        {
            var capabilities = new OrganizationCapabilities(section == OrganizationSection.Settings ? granted : !granted, !granted, section == OrganizationSection.Members ? granted : !granted, !granted);

            Assert.That(capabilities.CanOpen(section), Is.EqualTo(expected));
        }
    }
}
