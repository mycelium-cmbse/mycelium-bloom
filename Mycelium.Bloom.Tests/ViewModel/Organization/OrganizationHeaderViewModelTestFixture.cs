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

    /// <summary>
    /// Tests the derivation of the header of the account and organization pages from the organization context.
    /// </summary>
    [TestFixture]
    public sealed class OrganizationHeaderViewModelTestFixture
    {
        /// <summary>
        /// The sample account displayed by the header.
        /// </summary>
        private static readonly Account SampleAccount = new(Guid.NewGuid(), "Sample Account", "sample@example.org");

        /// <summary>
        /// The sample organization displayed by the header.
        /// </summary>
        private static readonly Organization SampleOrganization = new(Guid.NewGuid(), "orbital-systems", "Orbital Systems");

        /// <summary>
        /// The labels of the pages offered to an account that may edit the settings.
        /// </summary>
        private static readonly string[] AllPageLabels = ["Overview", "Projects", "Members", "Settings"];

        /// <summary>
        /// The labels of the pages offered to an account that may not edit the settings.
        /// </summary>
        private static readonly string[] PageLabelsWithoutSettings = ["Overview", "Projects", "Members"];

        /// <summary>
        /// The mocked organization context that the header follows.
        /// </summary>
        private Mock<IOrganizationContextViewModel> context;

        /// <summary>
        /// Creates a ready context on the members page of an organization, with every capability.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            this.context = new Mock<IOrganizationContextViewModel>();
            this.SetContext(
                OrganizationContextState.Ready,
                OrganizationLocation.ForOrganization("orbital-systems", OrganizationSection.Members),
                SampleAccount,
                SampleOrganization,
                new OrganizationCapabilities(true));
        }

        /// <summary>
        /// Verifies that an organization page shows the organization, its pages in order, the selected page and the account.
        /// </summary>
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
                Assert.That(
                    presentation.SectionItems.Select(item => item.Label),
                    Is.EqualTo(AllPageLabels));
                Assert.That(presentation.SectionItems.Single(item => item.IsSelected).Label, Is.EqualTo("Members"));
                Assert.That(presentation.SectionItems.Select(item => item.Id), Has.All.StartWith("/organizations/orbital-systems"));
                Assert.That(presentation.HasAccount, Is.True);
                Assert.That(presentation.AccountName, Is.EqualTo("Sample Account"));
                Assert.That(presentation.AccountItems.Single().Id, Is.EqualTo(OrganizationRoutes.AccountPath));
            }
        }

        /// <summary>
        /// Verifies that the settings page is offered only to an account that may edit the settings.
        /// </summary>
        [Test]
        public void VerifySettingsPageIsOfferedOnlyWhenTheCapabilityIsGranted()
        {
            this.SetContext(
                OrganizationContextState.Ready,
                OrganizationLocation.ForOrganization("orbital-systems", OrganizationSection.Overview),
                SampleAccount,
                SampleOrganization,
                new OrganizationCapabilities(false));

            using var viewModel = new OrganizationHeaderViewModel(this.context.Object);

            Assert.That(
                viewModel.Presentation.SectionItems.Select(item => item.Label),
                Is.EqualTo(PageLabelsWithoutSettings));
        }

        /// <summary>
        /// Verifies that the account page shows the account menu and no organization segment.
        /// </summary>
        [Test]
        public void VerifyAccountPageShowsTheAccountMenuOnly()
        {
            this.SetContext(OrganizationContextState.Ready, OrganizationLocation.Account, SampleAccount, null, null);

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

        /// <summary>
        /// Verifies that nothing stale is displayed while the context is not ready.
        /// </summary>
        /// <param name="state">The state of the context.</param>
        [TestCase(OrganizationContextState.Loading)]
        [TestCase(OrganizationContextState.NotFound)]
        [TestCase(OrganizationContextState.Error)]
        public void VerifyNothingIsDisplayedWhileTheContextIsNotReady(OrganizationContextState state)
        {
            this.SetContext(
                state,
                OrganizationLocation.ForOrganization("orbital-systems", OrganizationSection.Members),
                SampleAccount,
                SampleOrganization,
                new OrganizationCapabilities(true));

            using var viewModel = new OrganizationHeaderViewModel(this.context.Object);

            Assert.That(viewModel.Presentation, Is.EqualTo(OrganizationHeaderPresentation.Empty));
        }

        /// <summary>
        /// Verifies that the header follows the context when the location changes and stops following once disposed.
        /// </summary>
        [Test]
        public void VerifyHeaderFollowsTheContextUntilDisposed()
        {
            var viewModel = new OrganizationHeaderViewModel(this.context.Object);
            var announced = 0;
            viewModel.PropertyChanged += (_, _) => announced++;

            this.SetContext(
                OrganizationContextState.Ready,
                OrganizationLocation.ForOrganization("orbital-systems", OrganizationSection.Projects),
                SampleAccount,
                SampleOrganization,
                new OrganizationCapabilities(true));
            this.context.Raise(model => model.PropertyChanged += null, new PropertyChangedEventArgs(nameof(IOrganizationContextViewModel.Location)));
            var followedLabel = viewModel.Presentation.SectionLabel;

            viewModel.Dispose();
            this.SetContext(OrganizationContextState.Loading, OrganizationLocation.None, null, null, null);
            this.context.Raise(model => model.PropertyChanged += null, new PropertyChangedEventArgs(nameof(IOrganizationContextViewModel.State)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(followedLabel, Is.EqualTo("Projects"));
                Assert.That(announced, Is.GreaterThan(0));
                Assert.That(viewModel.Presentation.SectionLabel, Is.EqualTo("Projects"));
            }
        }

        /// <summary>
        /// Verifies that nothing is displayed while the properties of a new resolution are announced one by one and the
        /// account, the organization or the capabilities are not yet in place.
        /// </summary>
        /// <param name="hasAccount">A value indicating whether the account is already in place.</param>
        /// <param name="hasOrganization">A value indicating whether the organization is already in place.</param>
        /// <param name="hasCapabilities">A value indicating whether the capabilities are already in place.</param>
        [TestCase(false, true, true)]
        [TestCase(true, false, true)]
        [TestCase(true, true, false)]
        public void VerifyNothingIsDisplayedWhileTheResolutionIsIncomplete(
            bool hasAccount,
            bool hasOrganization,
            bool hasCapabilities)
        {
            this.SetContext(
                OrganizationContextState.Ready,
                OrganizationLocation.ForOrganization("orbital-systems", OrganizationSection.Members),
                hasAccount ? SampleAccount : null,
                hasOrganization ? SampleOrganization : null,
                hasCapabilities ? new OrganizationCapabilities(true) : null);

            using var viewModel = new OrganizationHeaderViewModel(this.context.Object);

            Assert.That(viewModel.Presentation, Is.EqualTo(OrganizationHeaderPresentation.Empty));
        }

        /// <summary>
        /// Verifies that selecting a menu entry yields the route to navigate to.
        /// </summary>
        /// <returns>A task that completes when the command has run.</returns>
        [Test]
        public async Task VerifySelectingAnEntryYieldsItsRoute()
        {
            using var viewModel = new OrganizationHeaderViewModel(this.context.Object);
            var item = viewModel.Presentation.SectionItems.First(candidate => candidate.Label == "Projects");

            var target = await viewModel.SelectItemCommand.Execute(item);

            Assert.That(target, Is.EqualTo("/organizations/orbital-systems/projects"));
        }

        /// <summary>
        /// Sets what the mocked context reports.
        /// </summary>
        /// <param name="state">The state of the context.</param>
        /// <param name="location">What the location designates.</param>
        /// <param name="account">The current account.</param>
        /// <param name="organization">The designated organization.</param>
        /// <param name="capabilities">The capabilities of the account.</param>
        private void SetContext(
            OrganizationContextState state,
            OrganizationLocation location,
            Account account,
            Organization organization,
            OrganizationCapabilities capabilities)
        {
            this.context.SetupGet(model => model.State).Returns(state);
            this.context.SetupGet(model => model.Location).Returns(location);
            this.context.SetupGet(model => model.Account).Returns(account);
            this.context.SetupGet(model => model.Organization).Returns(organization);
            this.context.SetupGet(model => model.Capabilities).Returns(capabilities);
        }
    }
}
