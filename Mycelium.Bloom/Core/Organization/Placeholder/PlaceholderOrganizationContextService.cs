// ------------------------------------------------------------------------------------------------
// <copyright file="PlaceholderOrganizationContextService.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Organization.Placeholder
{
    using ErrorOr;

    using Microsoft.Extensions.Options;

    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Core.MockedData;
    using Mycelium.Bloom.Model;
    using Mycelium.SDK;
    using Mycelium.SDK.POCO;

    /// <summary>
    /// Answers with the mocked account and organizations until the Fabric client provides the real ones. The mocked
    /// account may edit the settings and create projects only when its configured placeholder role is the administrator.
    /// </summary>
    /// <remarks>
    /// Placeholder of the organization screens, to be replaced by the Fabric client when it is bound; only the service
    /// registration changes then.
    /// </remarks>
    public sealed class PlaceholderOrganizationContextService : IOrganizationContextService
    {
        /// <summary>
        /// Provides the placeholder role given to the mocked account.
        /// </summary>
        private readonly IOptions<OrganizationOptions> options;

        /// <summary>
        /// Provides the mocked account and organizations.
        /// </summary>
        private readonly IMockedDataService mockedDataService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaceholderOrganizationContextService" /> class.
        /// </summary>
        /// <param name="options">The organization options holding the placeholder role.</param>
        /// <param name="mockedDataService">The provider of the mocked account and organizations.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="options" /> or <paramref name="mockedDataService" /> is <see langword="null" />.
        /// </exception>
        public PlaceholderOrganizationContextService(IOptions<OrganizationOptions> options, IMockedDataService mockedDataService)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(mockedDataService);

            this.options = options;
            this.mockedDataService = mockedDataService;
        }

        /// <summary>
        /// Gets the mocked account.
        /// </summary>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>The mocked account.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the request is cancelled.</exception>
        public Task<ErrorOr<User>> GetAccountAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult<ErrorOr<User>>(this.mockedDataService.CurrentUser);
        }

        /// <summary>
        /// Gets the mocked account, the mocked organization designated by a slug and the capabilities of the account.
        /// </summary>
        /// <param name="slug">The canonical slug of the organization, which is lower case.</param>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>The context, or an <see cref="ErrorCodes.NotFound" /> error when no mocked organization has the slug.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the request is cancelled.</exception>
        public Task<ErrorOr<OrganizationContext>> GetOrganizationContextAsync(string slug, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!this.mockedDataService.Organizations.TryGetValue(slug, out var organization))
            {
                return Task.FromResult<ErrorOr<OrganizationContext>>(Error.NotFound(ErrorCodes.NotFound, "The organization does not exist."));
            }

            var isAdministrator = this.options.Value.PlaceholderRole == OrganizationMembershipRole.Administrator;
            var capabilities = new OrganizationCapabilities(isAdministrator, isAdministrator);

            return Task.FromResult<ErrorOr<OrganizationContext>>(new OrganizationContext(this.mockedDataService.CurrentUser, organization, capabilities));
        }
    }
}
