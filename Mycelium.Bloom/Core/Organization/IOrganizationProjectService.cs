// ------------------------------------------------------------------------------------------------
// <copyright file="IOrganizationProjectService.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Organization
{
    using ErrorOr;

    using Mycelium.SDK.POCO;

    /// <summary>
    /// Provides the projects of an organization that the current account may see. It only brings the projects: counting,
    /// searching and filtering them is done by whoever shows them.
    /// </summary>
    public interface IOrganizationProjectService
    {
        /// <summary>
        /// Gets the projects of an organization. The list is neither paged nor filtered: the view pages and narrows what it
        /// shows.
        /// </summary>
        /// <param name="organizationId">The identifier of the organization.</param>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>
        /// The projects, or an error: <see cref="ErrorCodes.NotFound" /> when the organization does not
        /// exist and <see cref="ErrorCodes.Unavailable" /> when the projects cannot be retrieved.
        /// </returns>
        Task<ErrorOr<IReadOnlyList<FunctionalProject>>> GetProjectsAsync(Guid organizationId, CancellationToken cancellationToken);
    }
}
