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

    using ErrorOr;

    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    using ReactiveUI;

    /// <summary>
    /// Resolves the account, the organization and the capabilities designated by the location that the layout sets,
    /// and keeps them up to date when the location changes. Moving to another page of the same organization does
    /// not resolve it again.
    /// </summary>
    public sealed partial class OrganizationContextViewModel : BloomBaseViewModel, IOrganizationContextViewModel, IDisposable
    {
        /// <summary>
        /// Provides the account and the organization context, in one request per context.
        /// </summary>
        private readonly IOrganizationContextService contextService;

        /// <summary>
        /// Reports the failures that the services did not return as errors.
        /// </summary>
        private readonly ILogger<OrganizationContextViewModel> logger;

        /// <summary>
        /// Owns the observation of the location and of the resolution it triggers.
        /// </summary>
        private readonly IDisposable resolutionSubscription;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationContextViewModel" /> class.
        /// </summary>
        /// <param name="contextService">The service that provides the account and the organization context.</param>
        /// <param name="logger">The logger that reports unexpected failures.</param>
        /// <exception cref="ArgumentNullException">Thrown when an argument is <see langword="null" />.</exception>
        public OrganizationContextViewModel(IOrganizationContextService contextService, ILogger<OrganizationContextViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(contextService);
            ArgumentNullException.ThrowIfNull(logger);

            this.contextService = contextService;
            this.logger = logger;
            this.IsLoading = true;

            // The resolution is keyed on the account or organization, not on the page: the loaded data is combined
            // with the latest location, so a page change only re-evaluates the page rules. A null location resets the
            // context, so that coming back to the same organization loads it again.
            this.resolutionSubscription = this.WhenAnyValue(viewModel => viewModel.Location)
                .Publish(locations => locations
                    .DistinctUntilChanged(location => (location?.Kind, location?.Slug))
                    .Select(this.Resolve)
                    .Switch()
                    .CombineLatest(locations, Compose))
                .Subscribe(this.Apply);
        }

        /// <summary>
        /// Gets or sets what the current browser location designates. It is <see langword="null" /> until the layout sets it
        /// and once the layout is disposed, which clears the context.
        /// </summary>
        public OrganizationLocation Location
        {
            get;
            set => this.RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>
        /// Gets the current account, <see langword="null" /> until it is resolved.
        /// </summary>
        public Account Account
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>
        /// Gets the designated organization, <see langword="null" /> until it is resolved or on the account page.
        /// </summary>
        public Organization Organization
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>
        /// Gets what the current account may do in the designated organization, <see langword="null" /> until it is
        /// resolved or on the account page.
        /// </summary>
        public OrganizationCapabilities Capabilities
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>
        /// Gets the errors that prevented the resolution, empty while it loads and once it succeeded.
        /// </summary>
        public IReadOnlyList<Error> Errors
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        } = [];

        /// <summary>
        /// Stops observing the location and cancels the resolution in progress.
        /// </summary>
        public void Dispose()
        {
            this.resolutionSubscription.Dispose();
        }

        /// <summary>
        /// Combines what was loaded for an account or an organization with the latest location. Data loaded for
        /// another organization is never shown, a reset stays loading, and the settings page is not found without the
        /// capability to edit them.
        /// </summary>
        /// <param name="loaded">The latest resolution of an account or an organization.</param>
        /// <param name="location">The latest location.</param>
        /// <returns>The resolution to publish.</returns>
        private static OrganizationContextResolution Compose(OrganizationContextResolution loaded, OrganizationLocation location)
        {
            if (location is null || loaded.Location?.Kind != location.Kind || loaded.Location.Slug != location.Slug)
            {
                return OrganizationContextResolution.CreateLoading(location);
            }

            // A missing capability and a missing organization look the same to the account, so the page cannot be probed.
            return location.Section == OrganizationSection.Settings && loaded.Capabilities is { CanEditSettings: false }
                ? OrganizationContextResolution.CreateNotFound(location)
                : loaded;
        }

        /// <summary>
        /// Reports a failure that no service returned as an error.
        /// </summary>
        /// <param name="exception">The unexpected failure.</param>
        [LoggerMessage(Level = LogLevel.Error, Message = "The organization context could not be resolved.")]
        private partial void LogResolutionFailure(Exception exception);

        /// <summary>
        /// Publishes a resolution to the properties of this ViewModel. The loading flag is published last so that the
        /// data it announces is already in place.
        /// </summary>
        /// <param name="resolution">The resolution to publish.</param>
        private void Apply(OrganizationContextResolution resolution)
        {
            this.Account = resolution.Account;
            this.Organization = resolution.Organization;
            this.Capabilities = resolution.Capabilities;
            this.Errors = resolution.Errors;
            this.IsLoading = resolution.IsLoading;
        }

        /// <summary>
        /// Starts the resolution of a location: a loading resolution, then the loaded one. A null location stays loading
        /// with nothing loaded.
        /// </summary>
        /// <param name="location">The location to resolve, or <see langword="null" /> to reset the context.</param>
        /// <returns>The resolutions of the location.</returns>
        private IObservable<OrganizationContextResolution> Resolve(OrganizationLocation location)
        {
            var loading = OrganizationContextResolution.CreateLoading(location);

            return location is null
                ? Observable.Return(loading)
                : Observable.FromAsync(cancellationToken => this.LoadAsync(location, cancellationToken)).StartWith(loading);
        }

        /// <summary>
        /// Loads the account, or the account with the organization and its capabilities in one request, that a location
        /// designates.
        /// </summary>
        /// <param name="location">The location to load.</param>
        /// <param name="cancellationToken">The token that cancels the loading when another organization is designated.</param>
        /// <returns>The resolution of the location.</returns>
        private async Task<OrganizationContextResolution> LoadAsync(OrganizationLocation location, CancellationToken cancellationToken)
        {
            if (location.Kind == OrganizationLocationKind.None)
            {
                return OrganizationContextResolution.CreateNotFound(location);
            }

            try
            {
                if (location.Kind == OrganizationLocationKind.Account)
                {
                    var account = await this.contextService.GetAccountAsync(cancellationToken);

                    return account.IsError
                        ? OrganizationContextResolution.CreateRefused(location, account.Errors)
                        : OrganizationContextResolution.CreateReady(location, account.Value, null, null);
                }

                var context = await this.contextService.GetOrganizationContextAsync(location.Slug, cancellationToken);

                if (context.IsError)
                {
                    return OrganizationContextResolution.CreateRefused(location, context.Errors);
                }

                var value = context.Value;

                return OrganizationContextResolution.CreateReady(location, value.Account, value.Organization, value.Capabilities);
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                this.LogResolutionFailure(exception);

                var error = Error.Unexpected(OrganizationErrorCodes.Unavailable, "The organization context could not be resolved.");

                return OrganizationContextResolution.CreateRefused(location, [error]);
            }
        }
    }
}
