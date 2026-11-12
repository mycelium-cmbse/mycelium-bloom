// ------------------------------------------------------------------------------------------------
// <copyright file="PendingInvitationsListTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.PendingInvitationsList
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    using Bunit;

    using ErrorOr;

    using Moq;

    using Mycelium.Bloom.Components.UI.Organisms.PendingInvitationsList;
    using Mycelium.Bloom.Core;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Organization;
    using Mycelium.SDK;
    using Mycelium.SDK.POCO;

    [TestFixture]
    public sealed class PendingInvitationsListTestFixture
    {
        private BunitContext context;

        [SetUp]
        public void SetUp()
        {
            this.context = new BunitContext();
            BlueprintTestSetup.Configure(this.context);
        }

        [TearDown]
        public async Task TearDown()
        {
            await this.context.DisposeAsync();
        }

        [TestCase(true, false, 0, 1, 0, 0, 1)]
        [TestCase(true, true, 0, 0, 0, 1, 1)]
        [TestCase(false, true, 0, 0, 0, 1, 1)]
        [TestCase(false, false, 2, 0, 2, 0, 1)]
        [TestCase(false, false, 0, 0, 0, 0, 0)]
        public void VerifyTheListFlipsOnBusyErrorsAndRows(bool isBusy, bool failed, int rowCount, int skeletons, int rows, int alerts, int headings)
        {
            var now = new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);
            var invitation = new InvitationSummary("a.nowak@example.org", OrganizationMembershipRole.Member, new User { Id = Guid.NewGuid(), Name = "Jean Dupont", Email = "j.dupont@example.org" }, now.UtcDateTime, now.UtcDateTime.AddDays(5));
            var viewModel = new Mock<IPendingInvitationsViewModel>();
            viewModel.SetupGet(model => model.IsBusy).Returns(isBusy);
            viewModel.SetupGet(model => model.Errors).Returns(failed ? [Error.Unexpected(ErrorCodes.Unavailable, "Failed.")] : []);
            viewModel.SetupGet(model => model.Rows).Returns(new ReadOnlyObservableCollection<InvitationRowViewModel>(new ObservableCollection<InvitationRowViewModel>(Enumerable.Range(0, rowCount).Select(_ => new InvitationRowViewModel(invitation, now)))));

            var cut = this.context.Render<PendingInvitationsList>(parameters => parameters.Add(component => component.ViewModel, viewModel.Object));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cut.FindAll("[role=status]"), Has.Count.EqualTo(skeletons));
                Assert.That(cut.FindAll("li"), Has.Count.EqualTo(rows));
                Assert.That(cut.FindAll("[role=alert]"), Has.Count.EqualTo(alerts));
                Assert.That(cut.FindAll("h2"), Has.Count.EqualTo(headings));
            }
        }
    }
}
