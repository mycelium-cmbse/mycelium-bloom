// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationHeaderViewModelTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.Organization
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using Moq;

    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.Organization;
    using Mycelium.SDK.POCO;

    using Organization = Mycelium.SDK.POCO.Organization;

    [TestFixture]
    public sealed class OrganizationHeaderViewModelTestFixture
    {
        private static readonly string[] AllPageLabels = ["Overview", "Projects", "Members", "Settings"];

        private static readonly string[] PageLabelsWithoutSettings = ["Overview", "Projects", "Members"];

        private Mock<IOrganizationContextViewModel> context;

        private User account;

        private Organization organization;

        private OrganizationLocation membersPage;

        [SetUp]
        public void SetUp()
        {
            this.account = new User { Id = Guid.NewGuid(), Name = "Sample Account", Email = "sample@example.org" };
            this.organization = new Organization { Id = Guid.NewGuid(), Name = "Orbital Systems" };
            this.membersPage = OrganizationLocation.ForOrganization("orbital-systems", OrganizationSection.Members);
            this.context = new Mock<IOrganizationContextViewModel>();
            this.SetContext(false, this.membersPage, this.account, this.organization, new OrganizationCapabilities(true));
        }

        [Test]
        public void VerifyOrganizationPageShowsTheOrganizationItsPagesAndTheAccount()
        {
            using var viewModel = new OrganizationHeaderViewModel(this.context.Object);
            var presentation = viewModel.Presentation;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(presentation.Kind, Is.EqualTo(OrganizationLocationKind.Organization));
                Assert.That(presentation.OrganizationName, Is.EqualTo("Orbital Systems"));
                Assert.That(presentation.OrganizationPath, Is.EqualTo("/organizations/orbital-systems"));
                Assert.That(presentation.SectionLabel, Is.EqualTo("Members"));
                Assert.That(presentation.SectionIcon, Is.EqualTo("users"));
                Assert.That(presentation.SectionItems.Select(item => item.Label), Is.EqualTo(AllPageLabels));
                Assert.That(presentation.SectionItems.Single(item => item.IsSelected).Label, Is.EqualTo("Members"));
                Assert.That(presentation.SectionItems.Select(item => item.Id), Has.All.StartWith("/organizations/orbital-systems"));
                Assert.That(presentation.HasAccount, Is.True);
                Assert.That(presentation.AccountName, Is.EqualTo("Sample Account"));
                Assert.That(presentation.AccountItems.Single().Id, Is.EqualTo(OrganizationRoutes.AccountPath));
            }
        }

        [Test]
        public void VerifySettingsPageIsOfferedOnlyWhenTheCapabilityIsGranted()
        {
            this.SetContext(false, this.membersPage, this.account, this.organization, new OrganizationCapabilities(false));

            using var viewModel = new OrganizationHeaderViewModel(this.context.Object);

            Assert.That(viewModel.Presentation.SectionItems.Select(item => item.Label), Is.EqualTo(PageLabelsWithoutSettings));
        }

        [Test]
        public void VerifyAccountPageShowsTheAccountMenuOnly()
        {
            this.SetContext(false, OrganizationLocation.Account, this.account, null, null);

            using var viewModel = new OrganizationHeaderViewModel(this.context.Object);
            var presentation = viewModel.Presentation;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(presentation.Kind, Is.EqualTo(OrganizationLocationKind.Account));
                Assert.That(presentation.OrganizationName, Is.Empty);
                Assert.That(presentation.HasAccount, Is.True);
                Assert.That(presentation.AccountItems, Has.Count.EqualTo(1));
            }
        }

        [TestCase(true, true, true, true)]
        [TestCase(false, false, true, true)]
        [TestCase(false, true, false, true)]
        [TestCase(false, true, true, false)]
        public void VerifyNothingIsDisplayedUntilTheResolutionIsComplete(bool isLoading, bool hasAccount, bool hasOrganization, bool hasCapabilities)
        {
            var capabilities = hasCapabilities ? new OrganizationCapabilities(true) : null;
            this.SetContext(isLoading, this.membersPage, hasAccount ? this.account : null, hasOrganization ? this.organization : null, capabilities);

            using var viewModel = new OrganizationHeaderViewModel(this.context.Object);

            Assert.That(viewModel.Presentation, Is.EqualTo(OrganizationHeaderPresentation.Empty));
        }

        [Test]
        public void VerifyHeaderFollowsTheContextUntilDisposed()
        {
            var viewModel = new OrganizationHeaderViewModel(this.context.Object);
            var announced = 0;
            viewModel.PropertyChanged += (_, _) => announced++;

            var projectsPage = OrganizationLocation.ForOrganization("orbital-systems", OrganizationSection.Projects);
            this.SetContext(false, projectsPage, this.account, this.organization, new OrganizationCapabilities(true));
            this.context.Raise(model => model.PropertyChanged += null, new PropertyChangedEventArgs(nameof(IOrganizationContextViewModel.Location)));
            var followedLabel = viewModel.Presentation.SectionLabel;

            viewModel.Dispose();
            this.SetContext(true, OrganizationLocation.None, null, null, null);
            this.context.Raise(model => model.PropertyChanged += null, new PropertyChangedEventArgs(nameof(IOrganizationContextViewModel.IsLoading)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(followedLabel, Is.EqualTo("Projects"));
                Assert.That(announced, Is.GreaterThan(0));
                Assert.That(viewModel.Presentation.SectionLabel, Is.EqualTo("Projects"));
            }
        }

        [Test]
        public async Task VerifySelectingAnEntryYieldsItsRoute()
        {
            using var viewModel = new OrganizationHeaderViewModel(this.context.Object);
            var item = viewModel.Presentation.SectionItems.First(candidate => candidate.Label == "Projects");

            var target = await viewModel.SelectItemCommand.Execute(item);

            Assert.That(target, Is.EqualTo("/organizations/orbital-systems/projects"));
        }

        private void SetContext(bool isLoading, OrganizationLocation location, User currentAccount, Organization currentOrganization,
            OrganizationCapabilities capabilities)
        {
            this.context.SetupGet(model => model.IsLoading).Returns(isLoading);
            this.context.SetupGet(model => model.Location).Returns(location);
            this.context.SetupGet(model => model.Account).Returns(currentAccount);
            this.context.SetupGet(model => model.Organization).Returns(currentOrganization);
            this.context.SetupGet(model => model.Capabilities).Returns(capabilities);
        }
    }
}
