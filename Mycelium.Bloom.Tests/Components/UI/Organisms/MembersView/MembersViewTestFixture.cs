// ------------------------------------------------------------------------------------------------
// <copyright file="MembersViewTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.MembersView
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    using BlazorBlueprint.Components;

    using Bunit;

    using ErrorOr;

    using Moq;

    using Mycelium.Bloom.Components.UI.Organisms.MembersView;
    using Mycelium.Bloom.Components.UI.Organisms.PendingInvitationsList;
    using Mycelium.Bloom.Core;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Organization;
    using Mycelium.SDK;
    using Mycelium.SDK.POCO;

    using Organization = Mycelium.SDK.POCO.Organization;

    [TestFixture]
    public sealed class MembersViewTestFixture
    {
        private BunitContext context;

        private Mock<IMembersViewModel> viewModel;

        private DateTimeOffset now;

        private Organization organization;

        private Organization partner;

        [SetUp]
        public void SetUp()
        {
            this.context = new BunitContext();
            BlueprintTestSetup.Configure(this.context);
            this.now = new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);
            this.organization = new Organization { Id = Guid.NewGuid(), Name = "Orbital Systems" };
            this.partner = new Organization { Id = Guid.NewGuid(), Name = "Payload Lab" };
            this.viewModel = new Mock<IMembersViewModel>();
            this.viewModel.SetupGet(model => model.Errors).Returns([]);
            this.viewModel.SetupGet(model => model.CanViewOutsideCollaborators).Returns(true);
            this.SetUpRows(0);
        }

        [TearDown]
        public async Task TearDown()
        {
            await this.context.DisposeAsync();
        }

        [TestCase(true, 0, false, true, 1, 0, 0, 0, 0, 1)]
        [TestCase(false, 2, false, true, 0, 2, 0, 1, 0, 1)]
        [TestCase(false, 0, false, false, 0, 0, 0, 0, 1, 0)]
        [TestCase(false, 0, true, false, 0, 0, 1, 1, 0, 0)]
        public void VerifyTheViewFlipsOnBusyErrorsAndRows(bool isBusy, int rowCount, bool failed, bool canInvite, int skeletons, int rows, int alerts, int searchBoxes, int emptyStates, int inviteButtons)
        {
            this.SetUpRows(rowCount);
            this.viewModel.SetupGet(model => model.IsBusy).Returns(isBusy);
            this.viewModel.SetupGet(model => model.Errors).Returns(failed ? [Error.Unexpected(ErrorCodes.Unavailable, "Failed.")] : []);
            this.viewModel.SetupGet(model => model.CanInviteMembers).Returns(canInvite);

            var cut = this.context.Render<MembersView>(parameters => parameters.Add(component => component.ViewModel, this.viewModel.Object));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cut.FindAll("[role=status]"), Has.Count.EqualTo(skeletons));
                Assert.That(cut.FindAll("tbody tr"), Has.Count.EqualTo(rows));
                Assert.That(cut.FindAll("[role=alert]"), Has.Count.EqualTo(alerts));
                Assert.That(cut.FindAll("#members-search"), Has.Count.EqualTo(searchBoxes));
                Assert.That(cut.FindComponents<BbEmpty>(), Has.Count.EqualTo(emptyStates));
                Assert.That(cut.FindComponents<BbButton>().Count(button => button.Markup.Contains("Invite member", StringComparison.Ordinal)), Is.EqualTo(inviteButtons));
            }
        }

        [TestCase(true, false, false, 1)]
        [TestCase(false, false, false, 0)]
        [TestCase(true, true, false, 0)]
        [TestCase(true, false, true, 0)]
        public void VerifyThePendingInvitationsBlockIsShownOnlyToAnAccountThatMayInviteOnceThePeopleAreLoaded(bool hasInvitations, bool isBusy, bool failed, int blocks)
        {
            var invitations = new Mock<IPendingInvitationsViewModel>();
            invitations.SetupGet(model => model.Errors).Returns([]);
            invitations.SetupGet(model => model.Rows).Returns(new ReadOnlyObservableCollection<InvitationRowViewModel>([]));
            this.SetUpRows(2);
            this.viewModel.SetupGet(model => model.Invitations).Returns(hasInvitations ? invitations.Object : null);
            this.viewModel.SetupGet(model => model.IsBusy).Returns(isBusy);
            this.viewModel.SetupGet(model => model.Errors).Returns(failed ? [Error.Unexpected(ErrorCodes.Unavailable, "Failed.")] : []);

            var cut = this.context.Render<MembersView>(parameters => parameters.Add(component => component.ViewModel, this.viewModel.Object));

            Assert.That(cut.FindComponents<PendingInvitationsList>(), Has.Count.EqualTo(blocks));
        }

        [TestCase("All", 7)]
        [TestCase("Administrators", 2)]
        [TestCase("Members", 4)]
        [TestCase("Outside collaborators", 1)]
        [TestCase("Deactivated", 2)]
        public void VerifyEachKindChipCountsItsPeopleAndNarrowsTheRowsToThem(string label, int expected)
        {
            this.SetUpRows(this.CreateMember("Owner", OrganizationMembershipRole.Owner, ActivationStatus.Active), this.CreateMember("Administrator", OrganizationMembershipRole.Administrator, ActivationStatus.Active), this.CreateMember("Member", OrganizationMembershipRole.Member, ActivationStatus.Active), this.CreateMember("Never signed in", OrganizationMembershipRole.Member, ActivationStatus.Pending), this.CreateMember("Deactivated", OrganizationMembershipRole.Member, ActivationStatus.Suspended), this.CreateMember("Archived", OrganizationMembershipRole.Member, ActivationStatus.Archived), this.CreateCollaborator("Collaborator", ActivationStatus.Suspended));

            var cut = this.context.Render<MembersView>(parameters => parameters.Add(component => component.ViewModel, this.viewModel.Object));
            var chip = cut.FindComponents<BbToggleGroupItem<string>>().Single(item => item.Instance.Value == label);
            chip.Find("button").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(chip.Markup, Does.Contain($">{expected}<"));
                Assert.That(cut.FindAll("tbody tr"), Has.Count.EqualTo(expected));
            }
        }

        [Test]
        public void VerifyTheCollaboratorsChipIsOfferedOnlyToAnAccountThatMaySeeThem()
        {
            this.viewModel.SetupGet(model => model.CanViewOutsideCollaborators).Returns(false);
            this.SetUpRows(2);

            var cut = this.context.Render<MembersView>(parameters => parameters.Add(component => component.ViewModel, this.viewModel.Object));

            Assert.That(cut.FindComponents<BbToggleGroupItem<string>>().Select(item => item.Instance.Value), Does.Not.Contain("Outside collaborators"));
        }

        [Test]
        public async Task VerifyTheSearchAndTheKindNarrowTheRowsAndTheNoResultsActionShowsThemAgain()
        {
            this.SetUpRows(2);

            var cut = this.context.Render<MembersView>(parameters => parameters.Add(component => component.ViewModel, this.viewModel.Object));
            await cut.InvokeAsync(() => cut.FindComponent<BbInputGroupInput>().Instance.JsOnInput("Person 1"));
            var searched = cut.FindAll("tbody tr").Count;
            await cut.InvokeAsync(() => cut.FindComponent<BbInputGroupInput>().Instance.JsOnInput(string.Empty));
            cut.FindAll("button").Single(button => button.TextContent.Contains("Administrators", StringComparison.Ordinal)).Click();
            var ofKind = cut.FindAll("tbody tr").Count;
            await cut.InvokeAsync(() => cut.FindComponent<BbInputGroupInput>().Instance.JsOnInput("zed"));
            var title = cut.FindComponent<BbEmpty>().Instance.Title;
            cut.FindAll("button").Single(button => button.TextContent.Contains("Clear search and filters", StringComparison.Ordinal)).Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(searched, Is.EqualTo(1));
                Assert.That(ofKind, Is.EqualTo(1));
                Assert.That(title, Is.EqualTo("No member matches \"zed\""));
                Assert.That(cut.FindAll("tbody tr"), Has.Count.EqualTo(2));
            }
        }

        private User CreateMember(string name, OrganizationMembershipRole role, ActivationStatus status)
        {
            return MemberTestData.CreateMember(this.organization, name, role, status, this.now.UtcDateTime, this.now.UtcDateTime);
        }

        private User CreateCollaborator(string name, ActivationStatus status)
        {
            return MemberTestData.CreateOutsideCollaborator(this.partner, name, this.now.UtcDateTime, this.now.UtcDateTime, status);
        }

        private void SetUpRows(int rowCount)
        {
            this.SetUpRows([.. Enumerable.Range(0, rowCount).Select(index => index % 2 == 0 ? this.CreateMember($"Person {index}", OrganizationMembershipRole.Administrator, ActivationStatus.Active) : this.CreateCollaborator($"Person {index}", ActivationStatus.Suspended))]);
        }

        private void SetUpRows(params User[] people)
        {
            this.viewModel.SetupGet(model => model.Rows).Returns(new ReadOnlyObservableCollection<MemberRowViewModel>(new ObservableCollection<MemberRowViewModel>(people.Select(person => new MemberRowViewModel(person, this.organization.Id, this.now)))));
        }
    }
}
