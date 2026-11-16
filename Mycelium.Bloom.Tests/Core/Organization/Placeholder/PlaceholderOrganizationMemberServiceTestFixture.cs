// ------------------------------------------------------------------------------------------------
// <copyright file="PlaceholderOrganizationMemberServiceTestFixture.cs" company="Starion Group S.A.">
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

    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Time.Testing;

    using Mycelium.Bloom.Core;
    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Core.MockedData;
    using Mycelium.Bloom.Core.Organization.Placeholder;
    using Mycelium.SDK;

    using Organization = Mycelium.SDK.POCO.Organization;

    [TestFixture]
    public sealed class PlaceholderOrganizationMemberServiceTestFixture
    {
        private MockedDataService mockedData;

        private Organization orbitalSystems;

        [SetUp]
        public void SetUp()
        {
            this.mockedData = new MockedDataService(new FakeTimeProvider(new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero)));
            this.orbitalSystems = this.mockedData.Organizations["orbital-systems"];
        }

        [TestCase(OrganizationMembershipRole.Administrator, true)]
        [TestCase(OrganizationMembershipRole.Member, false)]
        public async Task VerifyOutsideCollaboratorsAreListedForAdministratorsOnly(OrganizationMembershipRole role, bool seesCollaborators)
        {
            var service = this.CreateService(role);

            var people = await service.GetMembersAsync(this.orbitalSystems.Id, CancellationToken.None);
            var collaborators = people.Value.Where(person => person.IsPartOfOrganizations.All(member => member.Organization != this.orbitalSystems)).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(people.Value.Count - collaborators.Count, Is.EqualTo(this.orbitalSystems.InvolvedUser.Count(member => member.User.Status != ActivationStatus.Deleted)));
                Assert.That(people.Value.Any(person => person.Status == ActivationStatus.Deleted), Is.False);
                Assert.That(collaborators, seesCollaborators ? Is.Not.Empty : Is.Empty);
                Assert.That(collaborators.All(person => person.IsPartOfProjects.Any(member => member.IsPartOf.BelongsTo == this.orbitalSystems && member.IsOutsideCollaborator)), Is.True);
                Assert.That(people.Value, Is.Unique);
            }
        }

        [Test]
        public async Task VerifyPeopleKeepTheirIdentifiersFromOneLoadToTheNext()
        {
            var service = this.CreateService(OrganizationMembershipRole.Administrator);

            var first = await service.GetMembersAsync(this.orbitalSystems.Id, CancellationToken.None);
            var second = await service.GetMembersAsync(this.orbitalSystems.Id, CancellationToken.None);

            Assert.That(second.Value.Select(person => person.Id), Is.EqualTo(first.Value.Select(person => person.Id)));
        }

        [Test]
        public async Task VerifyAnUnknownOrganizationIsNotFound()
        {
            var service = this.CreateService(OrganizationMembershipRole.Administrator);

            var people = await service.GetMembersAsync(Guid.NewGuid(), CancellationToken.None);

            Assert.That(people.FirstError.Code, Is.EqualTo(ErrorCodes.NotFound));
        }

        private PlaceholderOrganizationMemberService CreateService(OrganizationMembershipRole role)
        {
            return new PlaceholderOrganizationMemberService(Options.Create(new OrganizationOptions { PlaceholderRole = role }), this.mockedData);
        }
    }
}
