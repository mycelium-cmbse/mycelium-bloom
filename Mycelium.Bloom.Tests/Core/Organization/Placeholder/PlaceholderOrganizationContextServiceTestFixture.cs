// ------------------------------------------------------------------------------------------------
// <copyright file="PlaceholderOrganizationContextServiceTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Core.Organization.Placeholder
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using ErrorOr;

    using Microsoft.Extensions.Options;

    using Mycelium.Bloom.Core;
    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Core.MockedData;
    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.Core.Organization.Placeholder;
    using Mycelium.SDK;

    [TestFixture]
    public sealed class PlaceholderOrganizationContextServiceTestFixture
    {
        private MockedDataService mockedData;

        private OrganizationOptions options;

        private PlaceholderOrganizationContextService service;

        [SetUp]
        public void SetUp()
        {
            this.mockedData = new MockedDataService(TimeProvider.System);
            this.options = new OrganizationOptions();
            this.service = new PlaceholderOrganizationContextService(Options.Create(this.options), this.mockedData);
        }

        [Test]
        public async Task VerifyAccountIsTheMockedCurrentUser()
        {
            var result = await this.service.GetAccountAsync(CancellationToken.None);

            Assert.That(result.Value, Is.EqualTo(this.mockedData.CurrentUser));
        }

        [TestCase(OrganizationMembershipRole.Administrator, true)]
        [TestCase(OrganizationMembershipRole.Member, false)]
        public async Task VerifyOrganizationContextFollowsThePlaceholderRole(OrganizationMembershipRole role, bool canEditSettings)
        {
            var expected = this.mockedData.Organizations["orbital-systems"];
            this.options.PlaceholderRole = role;

            var result = await this.service.GetOrganizationContextAsync("orbital-systems", CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Value.Account, Is.EqualTo(this.mockedData.CurrentUser));
                Assert.That(result.Value.Organization, Is.EqualTo(expected));
                Assert.That(result.Value.Capabilities.CanEditSettings, Is.EqualTo(canEditSettings));
                Assert.That(result.Value.Capabilities.CanCreateProject, Is.EqualTo(canEditSettings));
                Assert.That(result.Value.Capabilities.CanViewMembers, Is.True);
                Assert.That(result.Value.Capabilities.CanInviteMembers, Is.EqualTo(canEditSettings));
                Assert.That(result.Value.Capabilities.CanViewOutsideCollaborators, Is.EqualTo(canEditSettings));
            }
        }

        [TestCase("does-not-exist")]
        [TestCase("ORBITAL-Systems")]
        [TestCase("")]
        public async Task VerifyUnknownSlugIsANotFoundError(string slug)
        {
            var result = await this.service.GetOrganizationContextAsync(slug, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.FirstError.Type, Is.EqualTo(ErrorType.NotFound));
                Assert.That(result.FirstError.Code, Is.EqualTo(ErrorCodes.NotFound));
            }
        }
    }
}
