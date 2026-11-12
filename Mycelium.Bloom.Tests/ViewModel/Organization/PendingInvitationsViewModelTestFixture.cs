// ------------------------------------------------------------------------------------------------
// <copyright file="PendingInvitationsViewModelTestFixture.cs" company="Starion Group S.A.">
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
    using System.Threading;

    using ErrorOr;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Time.Testing;

    using Moq;

    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.ViewModel.Organization;
    using Mycelium.SDK;
    using Mycelium.SDK.POCO;

    using Organization = Mycelium.SDK.POCO.Organization;

    [TestFixture]
    public sealed class PendingInvitationsViewModelTestFixture
    {
        private DateTimeOffset now;

        private Organization organization;

        private InvitationSummary invitation;

        private Mock<IOrganizationContextViewModel> context;

        private Mock<IOrganizationInvitationService> invitationService;

        private FakeTimeProvider timeProvider;

        private Mock<ILogger<PendingInvitationsViewModel>> logger;

        private SynchronizationContext testContext;

        [SetUp]
        public void SetUp()
        {
            this.testContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            this.now = new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);
            this.organization = new Organization { Id = Guid.NewGuid(), Name = "Orbital Systems" };
            this.invitation = new InvitationSummary("a.nowak@example.org", OrganizationMembershipRole.Member, new User { Id = Guid.NewGuid(), Name = "Jean Dupont", Email = "j.dupont@example.org" }, this.now.UtcDateTime.AddDays(-2), this.now.UtcDateTime.AddDays(5));
            this.context = new Mock<IOrganizationContextViewModel>();
            this.context.SetupGet(model => model.Organization).Returns(this.organization);
            this.invitationService = new Mock<IOrganizationInvitationService>();
            this.timeProvider = new FakeTimeProvider(this.now);
            this.logger = new Mock<ILogger<PendingInvitationsViewModel>>();
        }

        [TearDown]
        public void TearDown()
        {
            SynchronizationContext.SetSynchronizationContext(this.testContext);
        }

        [Test]
        public void VerifyInvitationsAreLoadedAndToldInWords()
        {
            this.invitationService.Setup(service => service.GetPendingInvitationsAsync(this.organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ErrorOrFactory.From<IReadOnlyList<InvitationSummary>>([this.invitation, this.invitation with { ExpiresOn = this.now.UtcDateTime.AddDays(-1) }]));

            using var viewModel = new PendingInvitationsViewModel(this.context.Object, this.invitationService.Object, this.timeProvider, this.logger.Object);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.IsBusy, Is.False);
                Assert.That(viewModel.Errors, Is.Empty);
                Assert.That(viewModel.Rows, Has.Count.EqualTo(2));
                Assert.That(viewModel.Invitations.Count, Is.EqualTo(2));
                Assert.That(viewModel.Rows[0].InvitedIdentity, Is.EqualTo("a.nowak@example.org"));
                Assert.That(viewModel.Rows[0].Details, Is.EqualTo("Invited as Member · Sent 2 days ago by j.dupont@example.org · Expires 5 days from now"));
                Assert.That(viewModel.Rows[1].Details, Does.EndWith("Expires yesterday"));
            }
        }
    }
}
