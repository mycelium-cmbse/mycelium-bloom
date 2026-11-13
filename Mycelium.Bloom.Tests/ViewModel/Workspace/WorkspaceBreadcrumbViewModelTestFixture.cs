// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceBreadcrumbViewModelTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.Workspace
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using ErrorOr;

    using Moq;

    using Mycelium.Bloom.Core;
    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.Core.Session;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.ViewModel.Workspace;

    using Organization = Mycelium.SDK.POCO.Organization;

    [TestFixture]
    public sealed class WorkspaceBreadcrumbViewModelTestFixture
    {
        private Mock<IServerSessionService> sessionService;

        private Mock<IOrganizationContextService> organizationService;

        private Project project;

        private Organization organization;

        private WorkspaceBreadcrumbViewModel viewModel;

        [SetUp]
        public void SetUp()
        {
            this.project = new Project { Id = Guid.NewGuid(), Name = "Spacecraft Mission" };
            this.organization = new Organization { Id = Guid.NewGuid(), Name = "Orbital Systems" };
            this.sessionService = new Mock<IServerSessionService>();
            this.sessionService.SetupGet(service => service.ServerAddress).Returns(new Uri("https://fabric.example.org"));
            this.sessionService.Setup(service => service.GetProjectAsync(this.project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(this.project);
            this.organizationService = new Mock<IOrganizationContextService>();
            this.organizationService.Setup(service => service.GetOrganizationContextAsync("orbital-systems", It.IsAny<CancellationToken>())).ReturnsAsync(new OrganizationContext(null, this.organization, null));
            this.viewModel = new WorkspaceBreadcrumbViewModel(this.sessionService.Object, this.organizationService.Object);
        }

        [TearDown]
        public void TearDown()
        {
            this.viewModel.Dispose();
        }

        [TestCase("orbital-systems", true)]
        [TestCase("unknown-organization", false)]
        [TestCase(null, false)]
        public async Task VerifyTheProjectIsOpenedWithTheOrganizationItWasOpenedFrom(string organizationSlug, bool hasOrganization)
        {
            this.organizationService.Setup(service => service.GetOrganizationContextAsync("unknown-organization", It.IsAny<CancellationToken>())).ReturnsAsync(Error.NotFound(ErrorCodes.NotFound, "The organization does not exist."));

            await this.viewModel.OpenAsync(this.project.Id, organizationSlug, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.viewModel.IsConnected, Is.True);
                Assert.That(this.viewModel.Errors, Is.Empty);
                Assert.That(this.viewModel.Project, Is.SameAs(this.project));
                Assert.That(this.viewModel.Organization, Is.SameAs(hasOrganization ? this.organization : null));
            }
        }

        [Test]
        public async Task VerifyAProjectTheServerDoesNotKnowKeepsItsErrorAndNoCrumb()
        {
            var notFound = Error.NotFound(ErrorCodes.NotFound, "The project does not exist on this server.");
            this.sessionService.Setup(service => service.GetProjectAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(notFound);

            await this.viewModel.OpenAsync(Guid.NewGuid(), "orbital-systems", CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.viewModel.Errors, Has.Count.EqualTo(1));
                Assert.That(this.viewModel.Errors[0], Is.EqualTo(notFound));
                Assert.That(this.viewModel.Project, Is.Null);
                this.organizationService.Verify(service => service.GetOrganizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            }
        }

        [Test]
        public void VerifyNoConnectionIsReportedWithoutAServerAddress()
        {
            this.sessionService.SetupGet(service => service.ServerAddress).Returns((Uri)null);

            Assert.That(this.viewModel.IsConnected, Is.False);
        }

        [Test]
        public async Task VerifyAnUnexpectedFailureKeepsAnUnavailableError()
        {
            this.sessionService.Setup(service => service.GetProjectAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException());

            await this.viewModel.OpenAsync(this.project.Id, null, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.viewModel.Errors, Has.Count.EqualTo(1));
                Assert.That(this.viewModel.Errors[0].Code, Is.EqualTo(ErrorCodes.Unavailable));
                Assert.That(this.viewModel.Project, Is.Null);
            }
        }

        [Test]
        public void VerifyACancelledOpeningIsNotReportedAsAFailure()
        {
            this.sessionService.Setup(service => service.GetProjectAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ThrowsAsync(new OperationCanceledException());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(async () => await this.viewModel.OpenAsync(this.project.Id, null, CancellationToken.None), Throws.InstanceOf<OperationCanceledException>());
                Assert.That(this.viewModel.Errors, Is.Empty);
            }
        }
    }
}
