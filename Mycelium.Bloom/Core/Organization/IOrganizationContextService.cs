// ------------------------------------------------------------------------------------------------
// <copyright file="IOrganizationContextService.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Organization
{
    using ErrorOr;

    using Mycelium.Bloom.Model;
    using Mycelium.SDK.DTO;

    /// <summary>
    /// Provides what the layout of the account and organization pages needs, in one request per context. A page
    /// loads only its own content afterwards, so that opening a page costs few queries.
    /// </summary>
    public interface IOrganizationContextService
    {
        /// <summary>
        /// Gets the account that is using Bloom.
        /// </summary>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>The current account, or an <see cref="OrganizationErrorCodes.Unavailable" /> error when it cannot be retrieved.</returns>
        Task<ErrorOr<User>> GetAccountAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the current account, the organization designated by a slug and what the account may do in it.
        /// </summary>
        /// <param name="slug">The canonical slug of the organization, which is lower case.</param>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>
        /// The context, or an error: <see cref="OrganizationErrorCodes.NotFound" /> when the organization does not exist,
        /// <see cref="OrganizationErrorCodes.Forbidden" /> when the account has no access to it, and
        /// <see cref="OrganizationErrorCodes.Unavailable" /> when it cannot be retrieved.
        /// </returns>
        Task<ErrorOr<OrganizationContext>> GetOrganizationContextAsync(string slug, CancellationToken cancellationToken);
    }
}
