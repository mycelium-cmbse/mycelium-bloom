// ------------------------------------------------------------------------------------------------
// <copyright file="MembersViewModelTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.Organization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using ErrorOr;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Time.Testing;

    using Moq;

    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.Extensions;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Organization;
    using Mycelium.SDK;
    using Mycelium.SDK.POCO;

    using Organization = Mycelium.SDK.POCO.Organization;

    [TestFixture]
    public sealed class MembersViewModelTestFixture
    {
        private DateTimeOffset now;

        private Organization organization;

        private User administrator;

        private User member;

        private User collaborator;

        private Organization partner;

        private Mock<IOrganizationContextViewModel> context;

        private Mock<IOrganizationMemberService> memberService;

        private FakeTimeProvider timeProvider;

        private Mock<ILogger<MembersViewModel>> logger;

        private SynchronizationContext testContext;

        [SetUp]
        public void SetUp()
        {
            this.testContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            this.now = new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);
            this.organization = new Organization { Id = Guid.NewGuid(), Name = "Orbital Systems" };
            this.partner = new Organization { Id = Guid.NewGuid(), Name = "Payload Lab" };
            this.administrator = MemberTestData.CreateMember(this.organization, "Jean Dupont", OrganizationMembershipRole.Administrator, ActivationStatus.Active, new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc), this.now.UtcDateTime.AddHours(-2));
            this.member = MemberTestData.CreateMember(this.organization, "Hanna Weber", OrganizationMembershipRole.Member, ActivationStatus.Suspended, new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc), this.now.UtcDateTime.AddDays(-30));
            this.collaborator = MemberTestData.CreateOutsideCollaborator(this.partner, "Priya Nair", new DateTime(2024, 11, 5, 0, 0, 0, DateTimeKind.Utc), null);
            this.context = new Mock<IOrganizationContextViewModel>();
            this.context.SetupGet(model => model.Organization).Returns(this.organization);
            this.context.SetupGet(model => model.Capabilities).Returns(new OrganizationCapabilities(true, true, true, true, true));
            this.memberService = new Mock<IOrganizationMemberService>();
            this.timeProvider = new FakeTimeProvider(this.now);
            this.logger = new Mock<ILogger<MembersViewModel>>();
        }

        [TearDown]
        public void TearDown()
        {
            SynchronizationContext.SetSynchronizationContext(this.testContext);
        }

        [Test]
        public void VerifyPeopleAreLoadedInTheOrderTheServiceReturnsThem()
        {
            this.SetUpMembers([this.administrator, this.member, this.collaborator]);

            using var viewModel = this.CreateViewModel();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.IsBusy, Is.False);
                Assert.That(viewModel.Errors, Is.Empty);
                Assert.That(viewModel.Members.Count, Is.EqualTo(3));
                Assert.That(viewModel.Rows, Has.Count.EqualTo(3));
                Assert.That(viewModel.Rows[0].User, Is.SameAs(this.administrator));
                Assert.That(viewModel.Rows[0].Role, Is.EqualTo(OrganizationMembershipRole.Administrator));
                Assert.That(viewModel.Rows[0].JoinedText, Is.EqualTo("Jan 2024"));
                Assert.That(viewModel.Rows[0].LastActiveText, Is.EqualTo("2 hours ago"));
                Assert.That(viewModel.Rows[2].Role, Is.Null);
                Assert.That(viewModel.Rows[2].LastActiveText, Is.EqualTo("Never"));
            }
        }

        [TestCase(true, true)]
        [TestCase(false, false)]
        [TestCase(null, null)]
        public void VerifyTheCapabilitiesOfTheContextGateTheInviteAndTheCollaborators(bool? canInvite, bool? canViewCollaborators)
        {
            var capabilities = canInvite is null ? null : new OrganizationCapabilities(true, true, true, canInvite.Value, canViewCollaborators.Value);
            this.context.SetupGet(model => model.Capabilities).Returns(capabilities);
            this.SetUpMembers([this.administrator]);

            using var viewModel = this.CreateViewModel();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.CanInviteMembers, Is.EqualTo(canInvite ?? false));
                Assert.That(viewModel.CanViewOutsideCollaborators, Is.EqualTo(canViewCollaborators ?? false));
            }
        }

        private void SetUpMembers(User[] members)
        {
            this.memberService.Setup(service => service.GetMembersAsync(this.organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ErrorOrFactory.From<IReadOnlyList<User>>(members));
        }

        private MembersViewModel CreateViewModel()
        {
            return new MembersViewModel(this.context.Object, this.memberService.Object, this.timeProvider, this.logger.Object);
        }
    }
}
