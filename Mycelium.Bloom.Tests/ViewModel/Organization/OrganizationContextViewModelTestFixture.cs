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

    using Bunit;

    using Microsoft.AspNetCore.Components;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging.Abstractions;

    using Moq;

    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.Organization;

    /// <summary>
    /// Tests the resolution of the organization designated by the browser location.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class OrganizationContextViewModelTestFixture : BunitContext
    {
        /// <summary>
        /// The sample account returned by the mocked account service.
        /// </summary>
        private static readonly Account SampleAccount = new(Guid.NewGuid(), "Sample Account", "sample@example.org");

        /// <summary>
        /// The sample capabilities returned by the mocked permission service.
        /// </summary>
        private static readonly OrganizationCapabilities SampleCapabilities = new(false);

        /// <summary>
        /// The mocked organization context service.
        /// </summary>
        private Mock<IOrganizationContextService> contextService;

        /// <summary>
        /// The navigation service of the bUnit context.
        /// </summary>
        private NavigationManager navigation;

        /// <summary>
        /// Creates the mocked service, which answers with the account by default.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            this.navigation = this.Services.GetRequiredService<NavigationManager>();
            this.contextService = new Mock<IOrganizationContextService>();
            this.contextService
                .Setup(service => service.GetAccountAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(OrganizationResult<Account>.Success(SampleAccount));
        }

        /// <summary>
        /// Disposes the bUnit context after each test.
        /// </summary>
        /// <returns>A task that completes when the context is disposed.</returns>
        [TearDown]
        public Task TearDown()
        {
            return this.DisposeAsync().AsTask();
        }

        /// <summary>
        /// Verifies that a known organization page resolves the account, the organization and the capabilities.
        /// </summary>
        [Test]
        public void VerifyKnownOrganizationIsReady()
        {
            var organization = this.SetUpOrganization("orbital-systems");
            this.navigation.NavigateTo("/organizations/orbital-systems/members");

            using var viewModel = this.CreateViewModel();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.State, Is.EqualTo(OrganizationContextState.Ready));
                Assert.That(viewModel.Account, Is.EqualTo(SampleAccount));
                Assert.That(viewModel.Organization, Is.EqualTo(organization));
                Assert.That(viewModel.Capabilities, Is.EqualTo(SampleCapabilities));
                Assert.That(
                    viewModel.Location,
                    Is.EqualTo(OrganizationLocation.ForOrganization("orbital-systems", OrganizationSection.Members)));
            }
        }

        /// <summary>
        /// Verifies that the account dashboard resolves the account only.
        /// </summary>
        [Test]
        public void VerifyAccountPageIsReadyWithoutOrganization()
        {
            this.navigation.NavigateTo("/account");

            using var viewModel = this.CreateViewModel();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.State, Is.EqualTo(OrganizationContextState.Ready));
                Assert.That(viewModel.Account, Is.EqualTo(SampleAccount));
                Assert.That(viewModel.Organization, Is.Null);
                Assert.That(viewModel.Capabilities, Is.Null);
            }

            this.contextService.Verify(
                service => service.GetOrganizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that a location outside the account and organization area is not found and that no service is
        /// asked about it.
        /// </summary>
        [Test]
        public void VerifyLocationOutsideTheAreaIsNotFound()
        {
            this.navigation.NavigateTo("/workspace/modeling");

            using var viewModel = this.CreateViewModel();

            Assert.That(viewModel.State, Is.EqualTo(OrganizationContextState.NotFound));
            this.contextService.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Verifies that a refusal of the context service is mapped to a state.
        /// </summary>
        /// <param name="code">The refusal code.</param>
        /// <param name="expected">The state the refusal maps to.</param>
        [TestCase(OrganizationErrorCode.NotFound, OrganizationContextState.NotFound)]
        [TestCase(OrganizationErrorCode.Forbidden, OrganizationContextState.NotFound)]
        [TestCase(OrganizationErrorCode.Unavailable, OrganizationContextState.Error)]
        public void VerifyRefusalIsMappedToAState(OrganizationErrorCode code, OrganizationContextState expected)
        {
            this.contextService
                .Setup(service => service.GetOrganizationContextAsync("orbital-systems", It.IsAny<CancellationToken>()))
                .ReturnsAsync(OrganizationResult<OrganizationContext>.Failure(new OrganizationError(code, "Refused.")));
            this.navigation.NavigateTo("/organizations/orbital-systems");

            using var viewModel = this.CreateViewModel();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.State, Is.EqualTo(expected));
                Assert.That(viewModel.Organization, Is.Null);
            }
        }

        /// <summary>
        /// Verifies that the settings page is available only to an account that may edit the settings.
        /// </summary>
        /// <param name="canEditSettings">A value indicating whether the account may edit the settings.</param>
        /// <param name="expected">The state of the context.</param>
        [TestCase(true, OrganizationContextState.Ready)]
        [TestCase(false, OrganizationContextState.NotFound)]
        public void VerifySettingsPageIsGatedByTheCapability(bool canEditSettings, OrganizationContextState expected)
        {
            this.SetUpOrganization("orbital-systems", new OrganizationCapabilities(canEditSettings));
            this.navigation.NavigateTo("/organizations/orbital-systems/settings");

            using var viewModel = this.CreateViewModel();

            Assert.That(viewModel.State, Is.EqualTo(expected));
        }

        /// <summary>
        /// Verifies that a refusal of the account service is mapped to a state.
        /// </summary>
        [Test]
        public void VerifyAccountRefusalIsMappedToError()
        {
            this.contextService
                .Setup(service => service.GetAccountAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(OrganizationResult<Account>.Failure(
                    new OrganizationError(OrganizationErrorCode.Unavailable, "Offline.")));
            this.navigation.NavigateTo("/account");

            using var viewModel = this.CreateViewModel();

            Assert.That(viewModel.State, Is.EqualTo(OrganizationContextState.Error));
        }

        /// <summary>
        /// Verifies that an unexpected failure of a service is reported as an error state instead of an exception.
        /// </summary>
        [Test]
        public void VerifyUnexpectedFailureIsMappedToError()
        {
            this.contextService
                .Setup(service => service.GetAccountAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Broken."));
            this.navigation.NavigateTo("/account");

            using var viewModel = this.CreateViewModel();

            Assert.That(viewModel.State, Is.EqualTo(OrganizationContextState.Error));
        }

        /// <summary>
        /// Verifies that moving to another page of the same organization shows the new page without loading the
        /// organization again, and that the settings page is found again once the account leaves it.
        /// </summary>
        [Test]
        public void VerifyChangingThePageOfTheSameOrganizationDoesNotLoadItAgain()
        {
            this.SetUpOrganization("orbital-systems");
            this.navigation.NavigateTo("/organizations/orbital-systems/members");
            using var viewModel = this.CreateViewModel();
            var states = new List<OrganizationContextState> { viewModel.State };
            viewModel.PropertyChanged += (_, _) => states.Add(viewModel.State);

            this.navigation.NavigateTo("/organizations/orbital-systems/projects");
            var projectsLocation = viewModel.Location;
            this.navigation.NavigateTo("/organizations/orbital-systems/settings");
            var settingsState = viewModel.State;
            this.navigation.NavigateTo("/organizations/orbital-systems/members?tab=all");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(projectsLocation.Section, Is.EqualTo(OrganizationSection.Projects));
                Assert.That(settingsState, Is.EqualTo(OrganizationContextState.NotFound));
                Assert.That(viewModel.State, Is.EqualTo(OrganizationContextState.Ready));
                Assert.That(viewModel.Location.Section, Is.EqualTo(OrganizationSection.Members));
                Assert.That(states, Does.Not.Contain(OrganizationContextState.Loading));
            }

            this.contextService.Verify(
                service => service.GetOrganizationContextAsync("orbital-systems", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// Verifies that designating another organization clears the previous one while the new one loads.
        /// </summary>
        [Test]
        public void VerifyLoadingAnotherOrganizationClearsThePreviousOne()
        {
            this.SetUpOrganization("fast");
            this.contextService
                .Setup(service => service.GetOrganizationContextAsync("slow", It.IsAny<CancellationToken>()))
                .Returns(new TaskCompletionSource<OrganizationResult<OrganizationContext>>().Task);
            this.navigation.NavigateTo("/organizations/fast");
            using var viewModel = this.CreateViewModel();

            this.navigation.NavigateTo("/organizations/slow");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.State, Is.EqualTo(OrganizationContextState.Loading));
                Assert.That(viewModel.Account, Is.Null);
                Assert.That(viewModel.Organization, Is.Null);
                Assert.That(viewModel.Capabilities, Is.Null);
                Assert.That(viewModel.Location.Slug, Is.EqualTo("slow"));
            }
        }

        /// <summary>
        /// Verifies that designating another organization cancels the lookup in progress and that the late outcome of
        /// that lookup, a result or a cancellation, never replaces the newer organization.
        /// </summary>
        /// <param name="cancelled">A value indicating whether the late outcome is a cancellation.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void VerifySlugChangeCancelsThePreviousLookup(bool cancelled)
        {
            var slowLookup = new TaskCompletionSource<OrganizationResult<OrganizationContext>>();
            var slowToken = CancellationToken.None;
            this.contextService
                .Setup(service => service.GetOrganizationContextAsync("slow", It.IsAny<CancellationToken>()))
                .Returns((string _, CancellationToken token) =>
                {
                    slowToken = token;

                    return slowLookup.Task;
                });
            var fast = this.SetUpOrganization("fast");
            this.navigation.NavigateTo("/organizations/slow");

            using var viewModel = this.CreateViewModel();
            var stateWhileLoading = viewModel.State;

            this.navigation.NavigateTo("/organizations/fast");

            if (cancelled)
            {
                slowLookup.SetCanceled();
            }
            else
            {
                slowLookup.SetResult(OrganizationResult<OrganizationContext>.Success(
                    new OrganizationContext(SampleAccount, new Organization(Guid.NewGuid(), "slow", "Slow"), SampleCapabilities)));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stateWhileLoading, Is.EqualTo(OrganizationContextState.Loading));
                Assert.That(slowToken.IsCancellationRequested, Is.True);
                Assert.That(viewModel.State, Is.EqualTo(OrganizationContextState.Ready));
                Assert.That(viewModel.Organization, Is.EqualTo(fast));
            }
        }

        /// <summary>
        /// Verifies that the ViewModel stops observing the location once disposed.
        /// </summary>
        [Test]
        public void VerifyDisposalStopsTheObservation()
        {
            this.SetUpOrganization("first");
            this.SetUpOrganization("second");
            this.navigation.NavigateTo("/organizations/first");
            var viewModel = this.CreateViewModel();

            this.navigation.NavigateTo("/organizations/second");
            viewModel.Dispose();
            this.navigation.NavigateTo("/organizations/third");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.Organization.Slug, Is.EqualTo("second"));
                this.contextService.Verify(
                    service => service.GetOrganizationContextAsync("third", It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        /// <summary>
        /// Sets up the context service to know an organization.
        /// </summary>
        /// <param name="slug">The slug of the organization.</param>
        /// <param name="capabilities">The capabilities of the account, or <see langword="null" /> for the default ones.</param>
        /// <returns>The organization the service returns.</returns>
        private Organization SetUpOrganization(string slug, OrganizationCapabilities capabilities = null)
        {
            var organization = new Organization(Guid.NewGuid(), slug, slug);
            this.contextService
                .Setup(service => service.GetOrganizationContextAsync(slug, It.IsAny<CancellationToken>()))
                .ReturnsAsync(OrganizationResult<OrganizationContext>.Success(
                    new OrganizationContext(SampleAccount, organization, capabilities ?? SampleCapabilities)));

            return organization;
        }

        /// <summary>
        /// Creates the ViewModel under test over the mocked service.
        /// </summary>
        /// <returns>The ViewModel.</returns>
        private OrganizationContextViewModel CreateViewModel()
        {
            return new OrganizationContextViewModel(
                this.navigation,
                this.contextService.Object,
                NullLogger<OrganizationContextViewModel>.Instance);
        }
    }
}
