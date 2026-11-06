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
    using Mycelium.Bloom.Model;
    using Mycelium.SDK;
    using Mycelium.SDK.POCO;

    /// <summary>
    /// Answers with the sample account and organizations until the Fabric client provides the real ones. The sample
    /// account may edit the settings and create projects only when its configured placeholder role is the administrator.
    /// </summary>
    /// <remarks>
    /// Placeholder of the organization screens, to be replaced by the Fabric client when it is bound; only the service
    /// registration changes then.
    /// </remarks>
    public sealed class PlaceholderOrganizationContextService : IOrganizationContextService
    {
        /// <summary>
        /// Provides the placeholder role given to the sample account.
        /// </summary>
        private readonly IOptions<OrganizationOptions> options;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaceholderOrganizationContextService" /> class.
        /// </summary>
        /// <param name="options">The organization options holding the placeholder role.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> is <see langword="null" />.</exception>
        public PlaceholderOrganizationContextService(IOptions<OrganizationOptions> options)
        {
            ArgumentNullException.ThrowIfNull(options);

            this.options = options;
        }

        /// <summary>
        /// Gets the sample account.
        /// </summary>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>The sample account.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the request is cancelled.</exception>
        public Task<ErrorOr<User>> GetAccountAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult<ErrorOr<User>>(OrganizationPlaceholderData.Account);
        }

        /// <summary>
        /// Gets the sample account, the sample organization designated by a slug and the capabilities of the account.
        /// </summary>
        /// <param name="slug">The canonical slug of the organization, which is lower case.</param>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>The context, or an <see cref="ErrorCodes.NotFound" /> error when no sample organization has the slug.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the request is cancelled.</exception>
        public Task<ErrorOr<OrganizationContext>> GetOrganizationContextAsync(string slug, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!OrganizationPlaceholderData.Organizations.TryGetValue(slug, out var organization))
            {
                return Task.FromResult<ErrorOr<OrganizationContext>>(Error.NotFound(ErrorCodes.NotFound, "The organization does not exist."));
            }

            var isAdministrator = this.options.Value.PlaceholderRole == OrganizationMembershipRole.Administrator;
            var capabilities = new OrganizationCapabilities(isAdministrator, isAdministrator);

            return Task.FromResult<ErrorOr<OrganizationContext>>(new OrganizationContext(OrganizationPlaceholderData.Account, organization, capabilities));
        }
    }
}
