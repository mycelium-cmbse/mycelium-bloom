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

    using Mycelium.Bloom.Model;

    /// <summary>
    /// Provides the projects of an organization as the projects page shows them. The projects, their total and their
    /// counts only cover the projects that the current account may see, so that no count reveals a hidden project.
    /// </summary>
    public interface IOrganizationProjectService
    {
        /// <summary>
        /// Gets one page of the projects of an organization that match a filter, the most recently updated first. The
        /// total of the page counts the matching projects, not all the projects of the organization.
        /// </summary>
        /// <param name="organizationId">The identifier of the organization.</param>
        /// <param name="filter">The criteria that the projects must match.</param>
        /// <param name="request">The page to get; the service decides the largest page it answers.</param>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>
        /// The page of projects, or an error: <see cref="OrganizationErrorCodes.NotFound" /> when the organization does not
        /// exist and <see cref="OrganizationErrorCodes.Unavailable" /> when the projects cannot be retrieved.
        /// </returns>
        Task<ErrorOr<PagedResult<ProjectSummary>>> GetProjectsAsync(Guid organizationId, ProjectFilter filter, PagedRequest request,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets the number of projects of an organization, in total and by lifecycle state, whatever the search or the
        /// filters of the projects list.
        /// </summary>
        /// <param name="organizationId">The identifier of the organization.</param>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>
        /// The counts, or an error: <see cref="OrganizationErrorCodes.NotFound" /> when the organization does not exist and
        /// <see cref="OrganizationErrorCodes.Unavailable" /> when the counts cannot be retrieved.
        /// </returns>
        Task<ErrorOr<ProjectCounts>> GetProjectCountsAsync(Guid organizationId, CancellationToken cancellationToken);
    }
}
