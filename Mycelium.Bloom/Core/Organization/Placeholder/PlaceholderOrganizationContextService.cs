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
    using Microsoft.Extensions.Options;

    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Answers with the sample account and organizations until the Fabric client provides the real ones. The sample
    /// account may edit the settings only when its configured placeholder role is the administrator.
    /// </summary>
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
        public Task<OrganizationResult<Account>> GetAccountAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(OrganizationResult<Account>.Success(OrganizationPlaceholderData.Account));
        }

        /// <summary>
        /// Gets the sample account, the sample organization designated by a slug and the capabilities of the account.
        /// </summary>
        /// <param name="slug">The canonical slug of the organization, which is lower case.</param>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>The context, or a not found refusal when no sample organization has the slug.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the request is cancelled.</exception>
        public Task<OrganizationResult<OrganizationContext>> GetOrganizationContextAsync(
            string slug,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var organization = OrganizationPlaceholderData.Organizations.FirstOrDefault(candidate =>
                string.Equals(candidate.Slug, slug, StringComparison.Ordinal));

            var result = organization is null
                ? OrganizationResult<OrganizationContext>.Failure(
                    new OrganizationError(OrganizationErrorCode.NotFound, "The organization does not exist."))
                : OrganizationResult<OrganizationContext>.Success(new OrganizationContext(
                    OrganizationPlaceholderData.Account,
                    organization,
                    new OrganizationCapabilities(this.options.Value.PlaceholderRole == OrganizationRole.OrganizationAdministrator)));

            return Task.FromResult(result);
        }
    }
}
