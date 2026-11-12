// ------------------------------------------------------------------------------------------------
// <copyright file="MockedDataServiceTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Core.MockedData
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.Time.Testing;

    using Mycelium.Bloom.Core.MockedData;
    using Mycelium.SDK;
    using Mycelium.SDK.POCO;

    [TestFixture]
    public sealed class MockedDataServiceTestFixture
    {
        private static readonly DateTimeOffset Now = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);

        private static readonly bool[] BothOutsideCollaboratorStates = [false, true];

        private MockedDataService service;

        [SetUp]
        public void SetUp()
        {
            this.service = new MockedDataService(new FakeTimeProvider(Now));
        }

        [Test]
        public void VerifyEveryEnumerationLiteralOfTheSdkIsUsed()
        {
            var organizations = this.service.Organizations.Values.ToList();
            var projects = this.service.Projects;
            var reviews = projects.SelectMany(project => project.Reviews).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.service.Users.Select(user => user.Status).Distinct(), Is.EquivalentTo(Enum.GetValues<ActivationStatus>()));
                Assert.That(organizations.SelectMany(organization => organization.InvolvedUser).Select(member => member.Role).Distinct(), Is.EquivalentTo(Enum.GetValues<OrganizationMembershipRole>()));
                Assert.That(projects.Select(project => project.Visibility).Distinct(), Is.EquivalentTo(Enum.GetValues<ProjectVisibility>()));
                Assert.That(projects.Select(project => project.CurrentMode).Distinct(), Is.EquivalentTo(Enum.GetValues<ProjectMode>()));
                Assert.That(projects.Select(project => project.Lifecycle).Distinct(), Is.EquivalentTo(Enum.GetValues<ProjectLifecycleKind>()));
                Assert.That(projects.SelectMany(project => project.Involves).Select(member => member.Role).Distinct(), Is.EquivalentTo(Enum.GetValues<ProjectMemberRole>()));
                Assert.That(reviews.Select(review => review.Status).Distinct(), Is.EquivalentTo(Enum.GetValues<ReviewStatus>()));
                Assert.That(reviews.SelectMany(review => review.Comments).Select(comment => comment.Status).Distinct(), Is.EquivalentTo(Enum.GetValues<CommentStatus>()));
            }
        }

        [Test]
        public void VerifyReferencesAreSetFromBothSides()
        {
            var organizationMembers = this.service.Organizations.Values.SelectMany(organization => organization.InvolvedUser).ToList();
            var projectMembers = this.service.Projects.SelectMany(project => project.Involves).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.service.Organizations.Values.SelectMany(organization => organization.InvolvedUser), Has.All.Matches<IOrganizationMember>(member => member.User.IsPartOfOrganizations.Contains(member)));
                Assert.That(this.service.Organizations.Values, Has.All.Matches<IOrganization>(organization => organization.Projects.All(project => project.BelongsTo == organization)));
                Assert.That(this.service.Projects, Has.All.Matches<IFunctionalProject>(project => project.Involves.All(member => member.IsPartOf == project && member.User.IsPartOfProjects.Contains(member))));
                Assert.That(projectMembers, Has.All.Matches<IProjectMember>(member => member.Owns.All(ownership => member.IsPartOf.Defines.Contains(ownership))));
                Assert.That(this.service.Users, Has.All.Matches<IUser>(user => user.IsPartOfOrganizations.All(organizationMembers.Contains)));
                Assert.That(projectMembers.Select(member => member.IsOutsideCollaborator).Distinct(), Is.EquivalentTo(BothOutsideCollaboratorStates));
                Assert.That(this.service.Organizations.Values, Has.All.Matches<IOrganization>(organization => organization.InvolvedUser.Any(member => member.User == this.service.CurrentUser)));
            }
        }

        [Test]
        public void VerifyEveryThingIsIdentifiedAndAudited()
        {
            var things = EnumerateThings(this.service).ToList();
            var otherThings = EnumerateThings(new MockedDataService(new FakeTimeProvider(Now.AddDays(3)))).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(things.Select(thing => thing.Id), Is.Unique);
                Assert.That(things.Select(thing => thing.Id), Is.EqualTo(otherThings.Select(thing => thing.Id)));
                Assert.That(things, Has.All.Matches<IAuditableThing>(thing => thing.CreatedBy != null && thing.UpdatedBy != null));
                Assert.That(things, Has.All.Matches<IAuditableThing>(thing => thing.CreatedOn <= thing.UpdatedOn && thing.UpdatedOn <= Now.UtcDateTime));
            }
        }

        private static IEnumerable<IAuditableThing> EnumerateThings(MockedDataService mockedData)
        {
            foreach (var user in mockedData.Users)
            {
                yield return user;
            }

            foreach (var organization in mockedData.Organizations.Values)
            {
                yield return organization;
                yield return organization.Policy;

                foreach (var member in organization.InvolvedUser)
                {
                    yield return member;
                }
            }

            foreach (var project in mockedData.Projects)
            {
                IEnumerable<IAuditableThing> parts =
                [
                    project,
                    project.Policy,
                    .. project.Defines,
                    .. project.Involves,
                    .. project.BranchRules,
                    .. project.Reviews,
                    .. project.Reviews.SelectMany(review => review.Comments),
                    .. project.Reviews.SelectMany(review => review.Comments).SelectMany(comment => comment.Replies)
                ];

                foreach (var part in parts)
                {
                    yield return part;
                }
            }
        }
    }
}
