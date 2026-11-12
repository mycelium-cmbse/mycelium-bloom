// ------------------------------------------------------------------------------------------------
// <copyright file="PlaceholderOrganizationInvitationServiceTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Core.Organization.Placeholder
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Time.Testing;

    using Mycelium.Bloom.Core;
    using Mycelium.Bloom.Core.MockedData;
    using Mycelium.Bloom.Core.Organization.Placeholder;
    using Mycelium.SDK;

    [TestFixture]
    public sealed class PlaceholderOrganizationInvitationServiceTestFixture
    {
        private static readonly string[] ExpectedIdentities = ["l.moreau", "a.nowak@example.org"];

        private FakeTimeProvider timeProvider;

        private MockedDataService mockedData;

        private PlaceholderOrganizationInvitationService service;

        [SetUp]
        public void SetUp()
        {
            this.timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero));
            this.mockedData = new MockedDataService(this.timeProvider);
            this.service = new PlaceholderOrganizationInvitationService(this.mockedData, this.timeProvider);
        }

        [Test]
        public async Task VerifyExpiredInvitationsAreDroppedAndTheMostRecentComesFirst()
        {
            var organization = this.mockedData.Organizations["orbital-systems"];

            var result = await this.service.GetPendingInvitationsAsync(organization.Id, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Value.Select(invitation => invitation.InvitedIdentity), Is.EqualTo(ExpectedIdentities));
                Assert.That(result.Value.All(invitation => invitation.SentBy.IsPartOfOrganizations.Any(member => member.Organization == organization && member.Role == OrganizationMembershipRole.Administrator)), Is.True);
                Assert.That(result.Value.All(invitation => invitation.Role == OrganizationMembershipRole.Member), Is.True);
                Assert.That(result.Value[0].SentOn, Is.EqualTo(this.timeProvider.GetUtcNow().UtcDateTime.AddHours(-5)));
                Assert.That(result.Value[0].ExpiresOn, Is.EqualTo(this.timeProvider.GetUtcNow().UtcDateTime.AddDays(7)));
            }
        }

        [Test]
        public async Task VerifyAnOrganizationWithoutAdministratorHasNoInvitationAndAnUnknownOneIsNotFound()
        {
            var empty = await this.service.GetPendingInvitationsAsync(this.mockedData.Organizations["heritage-avionics"].Id, CancellationToken.None);
            var unknown = await this.service.GetPendingInvitationsAsync(Guid.NewGuid(), CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(empty.Value, Is.Empty);
                Assert.That(unknown.FirstError.Code, Is.EqualTo(ErrorCodes.NotFound));
            }
        }
    }
}
