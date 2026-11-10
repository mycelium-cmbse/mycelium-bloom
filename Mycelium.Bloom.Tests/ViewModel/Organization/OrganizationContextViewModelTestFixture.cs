// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationContextViewModelTestFixture.cs" company="Starion Group S.A.">
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
    using System.Threading.Tasks;

    using ErrorOr;

    using Microsoft.Extensions.Logging;

    using Moq;

    using Mycelium.Bloom.Core;
    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.Organization;
    using Mycelium.SDK.POCO;

    using Organization = Mycelium.SDK.POCO.Organization;

    [TestFixture]
    public sealed class OrganizationContextViewModelTestFixture
    {
        private Mock<IOrganizationContextService> contextService;

        private User account;

        private OrganizationCapabilities capabilities;

        private OrganizationContextViewModel viewModel;

        [SetUp]
        public void SetUp()
        {
            this.account = new User { Id = Guid.NewGuid(), Name = "Sample Account", Mail = "sample@example.org" };
            this.capabilities = new OrganizationCapabilities(false);
            this.contextService = new Mock<IOrganizationContextService>();
            this.contextService.Setup(service => service.GetAccountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(this.account);
            this.viewModel = new OrganizationContextViewModel(this.contextService.Object, new Mock<ILogger<OrganizationContextViewModel>>().Object);
        }

        [TearDown]
        public void TearDown()
        {
            this.viewModel.Dispose();
        }

        [Test]
        public void VerifyKnownOrganizationIsResolved()
        {
            var organization = this.SetUpOrganization("orbital-systems");
            var isLoadingBeforeTheLocation = this.viewModel.IsLoading;

            this.viewModel.Location = OrganizationLocation.ForOrganization("orbital-systems", OrganizationSection.Members);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(isLoadingBeforeTheLocation, Is.True);
                Assert.That(this.viewModel.IsLoading, Is.False);
                Assert.That(this.viewModel.Errors, Is.Empty);
                Assert.That(this.viewModel.Account, Is.EqualTo(this.account));
                Assert.That(this.viewModel.Organization, Is.EqualTo(organization));
                Assert.That(this.viewModel.Capabilities, Is.EqualTo(this.capabilities));
            }
        }

        [Test]
        public void VerifyAccountPageIsResolvedWithoutOrganization()
        {
            this.viewModel.Location = OrganizationLocation.Account;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.viewModel.IsLoading, Is.False);
                Assert.That(this.viewModel.Account, Is.EqualTo(this.account));
                Assert.That(this.viewModel.Organization, Is.Null);
                Assert.That(this.viewModel.Capabilities, Is.Null);
                this.contextService.Verify(service => service.GetOrganizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            }
        }

        [Test]
        public void VerifyLocationOutsideTheAreaIsNotFound()
        {
            this.viewModel.Location = OrganizationLocation.None;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.viewModel.Errors[0].Type, Is.EqualTo(ErrorType.NotFound));
                this.contextService.VerifyNoOtherCalls();
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void VerifyErrorOfTheServiceIsExposed(bool isAccountPage)
        {
            var error = Error.Forbidden(ErrorCodes.Forbidden, "Refused.");
            this.contextService.Setup(service => service.GetAccountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(error);
            this.contextService.Setup(service => service.GetOrganizationContextAsync("orbital-systems", It.IsAny<CancellationToken>())).ReturnsAsync(error);

            var organizationPage = OrganizationLocation.ForOrganization("orbital-systems", OrganizationSection.Overview);

            this.viewModel.Location = isAccountPage ? OrganizationLocation.Account : organizationPage;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.viewModel.Errors, Is.EqualTo(new[] { error }));
                Assert.That(this.viewModel.IsLoading, Is.False);
                Assert.That(this.viewModel.Account, Is.Null);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void VerifySettingsPageIsGatedByTheCapability(bool canEditSettings)
        {
            this.SetUpOrganization("orbital-systems", new OrganizationCapabilities(canEditSettings));

            this.viewModel.Location = OrganizationLocation.ForOrganization("orbital-systems", OrganizationSection.Settings);

            Assert.That(this.viewModel.Errors, Has.Count.EqualTo(canEditSettings ? 0 : 1));
        }

        [Test]
        public void VerifyUnexpectedFailureIsExposedAsAnError()
        {
            this.contextService.Setup(service => service.GetAccountAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Broken."));

            this.viewModel.Location = OrganizationLocation.Account;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.viewModel.Errors[0].Type, Is.EqualTo(ErrorType.Unexpected));
                Assert.That(this.viewModel.Errors[0].Code, Is.EqualTo(ErrorCodes.Unavailable));
            }
        }

        [Test]
        public void VerifyChangingThePageOfTheSameOrganizationDoesNotLoadItAgain()
        {
            this.SetUpOrganization("orbital-systems");
            this.viewModel.Location = OrganizationLocation.ForOrganization("orbital-systems", OrganizationSection.Members);
            var loadingStates = new List<bool>();
            this.viewModel.PropertyChanged += (_, _) => loadingStates.Add(this.viewModel.IsLoading);

            this.viewModel.Location = OrganizationLocation.ForOrganization("orbital-systems", OrganizationSection.Settings);
            var settingsErrors = this.viewModel.Errors;
            this.viewModel.Location = OrganizationLocation.ForOrganization("orbital-systems", OrganizationSection.Projects);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(settingsErrors[0].Type, Is.EqualTo(ErrorType.NotFound));
                Assert.That(this.viewModel.Errors, Is.Empty);
                Assert.That(this.viewModel.Organization, Is.Not.Null);
                Assert.That(loadingStates, Does.Not.Contain(true));
                this.contextService.Verify(service => service.GetOrganizationContextAsync("orbital-systems", It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        [Test]
        public void VerifyLoadingAnotherOrganizationClearsThePreviousOne()
        {
            this.SetUpOrganization("fast");
            var pendingLookup = new TaskCompletionSource<ErrorOr<OrganizationContext>>();
            this.contextService.Setup(service => service.GetOrganizationContextAsync("slow", It.IsAny<CancellationToken>())).Returns(pendingLookup.Task);
            this.viewModel.Location = OrganizationLocation.ForOrganization("fast", OrganizationSection.Overview);

            this.viewModel.Location = OrganizationLocation.ForOrganization("slow", OrganizationSection.Overview);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.viewModel.IsLoading, Is.True);
                Assert.That(this.viewModel.Account, Is.Null);
                Assert.That(this.viewModel.Organization, Is.Null);
                Assert.That(this.viewModel.Capabilities, Is.Null);
                Assert.That(this.viewModel.Errors, Is.Empty);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void VerifySlugChangeCancelsThePreviousLookup(bool cancelled)
        {
            var slowLookup = new TaskCompletionSource<ErrorOr<OrganizationContext>>();
            var slowToken = CancellationToken.None;
            this.contextService.Setup(service => service.GetOrganizationContextAsync("slow", It.IsAny<CancellationToken>()))
                .Returns((string _, CancellationToken token) =>
                {
                    slowToken = token;

                    return slowLookup.Task;
                });
            var fast = this.SetUpOrganization("fast");

            this.viewModel.Location = OrganizationLocation.ForOrganization("slow", OrganizationSection.Overview);
            this.viewModel.Location = OrganizationLocation.ForOrganization("fast", OrganizationSection.Overview);

            if (cancelled)
            {
                slowLookup.SetCanceled();
            }
            else
            {
                slowLookup.SetResult(new OrganizationContext(this.account, new Organization { Id = Guid.NewGuid(), Name = "Slow" }, this.capabilities));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(slowToken.IsCancellationRequested, Is.True);
                Assert.That(this.viewModel.IsLoading, Is.False);
                Assert.That(this.viewModel.Organization, Is.EqualTo(fast));
            }
        }

        [Test]
        public void VerifyResetClearsTheContextAndComingBackLoadsItAgain()
        {
            this.SetUpOrganization("orbital-systems");
            var overview = OrganizationLocation.ForOrganization("orbital-systems", OrganizationSection.Overview);
            this.viewModel.Location = OrganizationLocation.None;
            var errorsBeforeTheReset = this.viewModel.Errors;

            this.viewModel.Location = null;
            var isLoadingAfterTheReset = this.viewModel.IsLoading;
            var errorsAfterTheReset = this.viewModel.Errors;
            this.viewModel.Location = overview;
            this.viewModel.Location = null;
            var organizationAfterTheReset = this.viewModel.Organization;
            this.viewModel.Location = overview;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(errorsBeforeTheReset, Has.Count.EqualTo(1));
                Assert.That(isLoadingAfterTheReset, Is.True);
                Assert.That(errorsAfterTheReset, Is.Empty);
                Assert.That(organizationAfterTheReset, Is.Null);
                Assert.That(this.viewModel.Organization.Name, Is.EqualTo("orbital-systems"));
                this.contextService.Verify(service => service.GetOrganizationContextAsync("orbital-systems", It.IsAny<CancellationToken>()), Times.Exactly(2));
            }
        }

        [Test]
        public void VerifyDisposalStopsTheObservation()
        {
            this.SetUpOrganization("first");
            this.viewModel.Location = OrganizationLocation.ForOrganization("first", OrganizationSection.Overview);

            this.viewModel.Dispose();
            this.viewModel.Location = OrganizationLocation.ForOrganization("second", OrganizationSection.Overview);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.viewModel.Organization.Name, Is.EqualTo("first"));
                this.contextService.Verify(service => service.GetOrganizationContextAsync("second", It.IsAny<CancellationToken>()), Times.Never);
            }
        }

        private Organization SetUpOrganization(string slug, OrganizationCapabilities organizationCapabilities = null)
        {
            var organization = new Organization { Id = Guid.NewGuid(), Name = slug };
            var context = new OrganizationContext(this.account, organization, organizationCapabilities ?? this.capabilities);
            this.contextService.Setup(service => service.GetOrganizationContextAsync(slug, It.IsAny<CancellationToken>())).ReturnsAsync(context);

            return organization;
        }
    }
}
