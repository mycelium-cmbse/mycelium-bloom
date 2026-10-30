// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationContextViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Organization
{
    using System.Reactive.Linq;

    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Routing;

    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    using ReactiveUI;

    /// <summary>
    /// Resolves the account, the organization and the capabilities designated by the current browser location,
    /// and keeps them up to date when the location changes. Moving to another page of the same organization does
    /// not resolve it again.
    /// </summary>
    public sealed partial class OrganizationContextViewModel : BloomBaseViewModel, IOrganizationContextViewModel
    {
        /// <summary>
        /// Provides the account and the organization context, in one request per context.
        /// </summary>
        private readonly IOrganizationContextService contextService;

        /// <summary>
        /// Reports the failures that the services did not describe as refusals.
        /// </summary>
        private readonly ILogger<OrganizationContextViewModel> logger;

        /// <summary>
        /// Owns the observation of the browser location and of the resolution it triggers.
        /// </summary>
        private readonly IDisposable resolutionSubscription;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationContextViewModel" /> class.
        /// </summary>
        /// <param name="navigationManager">Blazor's authoritative navigation service.</param>
        /// <param name="contextService">The service that provides the account and the organization context.</param>
        /// <param name="logger">The logger that reports unexpected failures.</param>
        /// <exception cref="ArgumentNullException">Thrown when an argument is <see langword="null" />.</exception>
        public OrganizationContextViewModel(
            NavigationManager navigationManager,
            IOrganizationContextService contextService,
            ILogger<OrganizationContextViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(navigationManager);
            ArgumentNullException.ThrowIfNull(contextService);
            ArgumentNullException.ThrowIfNull(logger);

            this.contextService = contextService;
            this.logger = logger;

            // The resolution is keyed on the account or organization, not on the page: the loaded data is combined
            // with the latest location, so a page change only re-evaluates the page rules.
            this.resolutionSubscription = Observable
                .FromEventPattern<LocationChangedEventArgs>(
                    handler => navigationManager.LocationChanged += handler,
                    handler => navigationManager.LocationChanged -= handler)
                .Select(_ => navigationManager.Uri)
                .StartWith(navigationManager.Uri)
                .Select(uri => OrganizationRoutes.Parse(navigationManager.ToBaseRelativePath(uri)))
                .DistinctUntilChanged()
                .Publish(locations => locations
                    .DistinctUntilChanged(location => (location.Kind, location.Slug))
                    .Select(location => Observable
                        .FromAsync(cancellationToken => this.LoadAsync(location, cancellationToken))
                        .StartWith(Resolution.CreateLoading(location)))
                    .Switch()
                    .CombineLatest(locations, Compose))
                .Subscribe(this.Apply);
        }

        /// <summary>
        /// Gets what the current browser location designates.
        /// </summary>
        public OrganizationLocation Location
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        } = OrganizationLocation.None;

        /// <summary>
        /// Gets the state of the designated organization.
        /// </summary>
        public OrganizationContextState State
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        } = OrganizationContextState.Loading;

        /// <summary>
        /// Gets the current account, <see langword="null" /> unless the state is <see cref="OrganizationContextState.Ready" />.
        /// </summary>
        public Account Account
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>
        /// Gets the designated organization, <see langword="null" /> unless the state is
        /// <see cref="OrganizationContextState.Ready" /> on an organization page.
        /// </summary>
        public Organization Organization
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>
        /// Gets what the current account may do in the designated organization, <see langword="null" /> unless the state
        /// is <see cref="OrganizationContextState.Ready" /> on an organization page.
        /// </summary>
        public OrganizationCapabilities Capabilities
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>
        /// Stops observing the browser location and cancels the resolution in progress.
        /// </summary>
        public void Dispose()
        {
            this.resolutionSubscription.Dispose();
        }

        /// <summary>
        /// Combines what was loaded for an account or an organization with the latest location. Data loaded for
        /// another organization is never shown, and the settings page is not found without the capability to edit them.
        /// </summary>
        /// <param name="loaded">The latest resolution of an account or an organization.</param>
        /// <param name="location">The latest location.</param>
        /// <returns>The resolution to publish.</returns>
        private static Resolution Compose(Resolution loaded, OrganizationLocation location)
        {
            if (loaded.Location.Kind != location.Kind || loaded.Location.Slug != location.Slug)
            {
                return Resolution.CreateLoading(location);
            }

            // A missing capability and a missing organization look the same to the account, so the page cannot be probed.
            return loaded.State == OrganizationContextState.Ready
                   && location.Section == OrganizationSection.Settings
                   && loaded.Capabilities is { CanEditSettings: false }
                ? Resolution.CreateNotFound(location)
                : loaded with { Location = location };
        }

        /// <summary>
        /// Reports a failure that no service described as a refusal.
        /// </summary>
        /// <param name="exception">The unexpected failure.</param>
        [LoggerMessage(Level = LogLevel.Error, Message = "The organization context could not be resolved.")]
        private partial void LogResolutionFailure(Exception exception);

        /// <summary>
        /// Publishes a resolution to the properties of this ViewModel. The state is published last so that the data
        /// it announces is already in place.
        /// </summary>
        /// <param name="resolution">The resolution to publish.</param>
        private void Apply(Resolution resolution)
        {
            this.Location = resolution.Location;
            this.Account = resolution.Account;
            this.Organization = resolution.Organization;
            this.Capabilities = resolution.Capabilities;
            this.State = resolution.State;
        }

        /// <summary>
        /// Loads the account, or the account with the organization and its capabilities in one request, that a location
        /// designates.
        /// </summary>
        /// <param name="location">The location to load.</param>
        /// <param name="cancellationToken">The token that cancels the loading when another organization is designated.</param>
        /// <returns>The resolution of the location.</returns>
        private async Task<Resolution> LoadAsync(OrganizationLocation location, CancellationToken cancellationToken)
        {
            if (location.Kind == OrganizationLocationKind.None)
            {
                return Resolution.CreateNotFound(location);
            }

            try
            {
                if (location.Kind == OrganizationLocationKind.Account)
                {
                    var account = await this.contextService.GetAccountAsync(cancellationToken);

                    return account.IsSuccess
                        ? Resolution.CreateReady(location, account.Value, null, null)
                        : Resolution.CreateRefused(location, account.Error);
                }

                var context = await this.contextService.GetOrganizationContextAsync(location.Slug, cancellationToken);

                return context.IsSuccess
                    ? Resolution.CreateReady(location, context.Value.Account, context.Value.Organization, context.Value.Capabilities)
                    : Resolution.CreateRefused(location, context.Error);
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                this.LogResolutionFailure(exception);

                return Resolution.CreateFailed(location);
            }
        }

        /// <summary>
        /// Describes the outcome of resolving a location, published as one unit to the properties of the ViewModel.
        /// </summary>
        /// <param name="Location">The resolved location.</param>
        /// <param name="State">The state of the organization.</param>
        /// <param name="Account">The current account, when it is known.</param>
        /// <param name="Organization">The designated organization, when it is known.</param>
        /// <param name="Capabilities">The capabilities of the account, when they are known.</param>
        private sealed record Resolution(
            OrganizationLocation Location,
            OrganizationContextState State,
            Account Account,
            Organization Organization,
            OrganizationCapabilities Capabilities)
        {
            /// <summary>
            /// Creates the resolution of a location that is being resolved.
            /// </summary>
            /// <param name="location">The location being resolved.</param>
            /// <returns>The loading resolution.</returns>
            public static Resolution CreateLoading(OrganizationLocation location)
            {
                return new Resolution(location, OrganizationContextState.Loading, null, null, null);
            }

            /// <summary>
            /// Creates the resolution of a location that resolved completely.
            /// </summary>
            /// <param name="location">The resolved location.</param>
            /// <param name="account">The current account.</param>
            /// <param name="organization">The designated organization, or <see langword="null" /> on the account page.</param>
            /// <param name="capabilities">The capabilities of the account, or <see langword="null" /> on the account page.</param>
            /// <returns>The ready resolution.</returns>
            public static Resolution CreateReady(
                OrganizationLocation location,
                Account account,
                Organization organization,
                OrganizationCapabilities capabilities)
            {
                return new Resolution(location, OrganizationContextState.Ready, account, organization, capabilities);
            }

            /// <summary>
            /// Creates the resolution of a location that designates nothing the account can reach.
            /// </summary>
            /// <param name="location">The resolved location.</param>
            /// <returns>The not found resolution.</returns>
            public static Resolution CreateNotFound(OrganizationLocation location)
            {
                return new Resolution(location, OrganizationContextState.NotFound, null, null, null);
            }

            /// <summary>
            /// Creates the resolution of a location whose resolution failed.
            /// </summary>
            /// <param name="location">The resolved location.</param>
            /// <returns>The error resolution.</returns>
            public static Resolution CreateFailed(OrganizationLocation location)
            {
                return new Resolution(location, OrganizationContextState.Error, null, null, null);
            }

            /// <summary>
            /// Creates the resolution that maps a refusal of a service to a state: a missing or forbidden organization
            /// is not found, any other refusal is an error.
            /// </summary>
            /// <param name="location">The resolved location.</param>
            /// <param name="error">The refusal of the service.</param>
            /// <returns>The resolution that matches the refusal.</returns>
            public static Resolution CreateRefused(OrganizationLocation location, OrganizationError error)
            {
                return error.Code is OrganizationErrorCode.NotFound or OrganizationErrorCode.Forbidden
                    ? CreateNotFound(location)
                    : CreateFailed(location);
            }
        }
    }
}
