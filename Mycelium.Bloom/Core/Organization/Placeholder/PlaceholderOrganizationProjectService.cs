// ------------------------------------------------------------------------------------------------
// <copyright file="PlaceholderOrganizationProjectService.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Organization.Placeholder
{
    using ErrorOr;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Answers with the sample projects until the Fabric client provides the real ones. It applies no visibility
    /// rule: every sample project of the organization is returned.
    /// </summary>
    /// <remarks>
    /// Placeholder of the projects page, to be replaced by the Fabric client when it is bound; only the service
    /// registration changes then.
    /// </remarks>
    public sealed class PlaceholderOrganizationProjectService : IOrganizationProjectService
    {
        /// <summary>
        /// The error returned for an identifier that no sample organization has.
        /// </summary>
        private static readonly Error OrganizationNotFound = Error.NotFound(OrganizationErrorCodes.NotFound, "The organization does not exist.");

        /// <summary>
        /// Provides the current time, from which the age of each sample project gives its last update.
        /// </summary>
        private readonly TimeProvider timeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaceholderOrganizationProjectService" /> class.
        /// </summary>
        /// <param name="timeProvider">The provider of the current time.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="timeProvider" /> is <see langword="null" />.</exception>
        public PlaceholderOrganizationProjectService(TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(timeProvider);
            this.timeProvider = timeProvider;
        }

        /// <summary>
        /// Gets one page of the sample projects of an organization that match a filter, the most recently updated first.
        /// </summary>
        /// <param name="organizationId">The identifier of the organization.</param>
        /// <param name="filter">The criteria that the sample projects must match.</param>
        /// <param name="request">The page to get.</param>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>
        /// The page of projects, or an <see cref="OrganizationErrorCodes.NotFound" /> error when no sample organization has
        /// the identifier.
        /// </returns>
        /// <exception cref="OperationCanceledException">Thrown when the request is cancelled.</exception>
        public Task<ErrorOr<PagedResult<ProjectSummary>>> GetProjectsAsync(Guid organizationId, ProjectFilter filter, PagedRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!OrganizationPlaceholderData.Projects.TryGetValue(organizationId, out var samples))
            {
                return Task.FromResult<ErrorOr<PagedResult<ProjectSummary>>>(OrganizationNotFound);
            }

            return Task.FromResult<ErrorOr<PagedResult<ProjectSummary>>>(this.ToPage(samples, filter, request));
        }

        /// <summary>
        /// Gets the number of sample projects of an organization, in total and by lifecycle state.
        /// </summary>
        /// <param name="organizationId">The identifier of the organization.</param>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>
        /// The counts, or an <see cref="OrganizationErrorCodes.NotFound" /> error when no sample organization has the
        /// identifier.
        /// </returns>
        /// <exception cref="OperationCanceledException">Thrown when the request is cancelled.</exception>
        public Task<ErrorOr<ProjectCounts>> GetProjectCountsAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!OrganizationPlaceholderData.Projects.TryGetValue(organizationId, out var samples))
            {
                return Task.FromResult<ErrorOr<ProjectCounts>>(OrganizationNotFound);
            }

            var counts = new ProjectCounts(samples.Count, CountIn(samples, ProjectLifecycleState.Preparation),
                CountIn(samples, ProjectLifecycleState.Open), CountIn(samples, ProjectLifecycleState.Review),
                CountIn(samples, ProjectLifecycleState.Archived));

            return Task.FromResult<ErrorOr<ProjectCounts>>(counts);
        }

        /// <summary>
        /// Counts the sample projects that are in a lifecycle state.
        /// </summary>
        /// <param name="samples">The sample projects.</param>
        /// <param name="state">The lifecycle state to count.</param>
        /// <returns>The number of sample projects in the state.</returns>
        private static int CountIn(IReadOnlyList<ProjectSample> samples, ProjectLifecycleState state)
        {
            return samples.Count(sample => sample.LifecycleState == state);
        }

        /// <summary>
        /// Tells whether a sample project matches a filter.
        /// </summary>
        /// <param name="sample">The sample project.</param>
        /// <param name="filter">The criteria to match.</param>
        /// <returns><see langword="true" /> when the sample project satisfies every criterion.</returns>
        private static bool Matches(ProjectSample sample, ProjectFilter filter)
        {
            return (filter.LifecycleState is null || sample.LifecycleState == filter.LifecycleState)
                   && (filter.Mode is null || sample.Mode == filter.Mode)
                   && (sample.Name.Contains(filter.SearchText, StringComparison.OrdinalIgnoreCase)
                       || sample.Description.Contains(filter.SearchText, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Takes the page of the sample projects that match a filter, the most recently updated first.
        /// </summary>
        /// <param name="samples">The sample projects.</param>
        /// <param name="filter">The criteria to match.</param>
        /// <param name="request">The page to take.</param>
        /// <returns>The page, whose total counts all the matching sample projects.</returns>
        private PagedResult<ProjectSummary> ToPage(IReadOnlyList<ProjectSample> samples, ProjectFilter filter, PagedRequest request)
        {
            var matches = this.ToSummaries(samples.Where(sample => Matches(sample, filter))).ToList();

            return new PagedResult<ProjectSummary>([.. matches.Skip(request.PageIndex * request.PageSize).Take(request.PageSize)], matches.Count);
        }

        /// <summary>
        /// Turns the sample projects into projects, the most recently updated first.
        /// </summary>
        /// <param name="samples">The sample projects.</param>
        /// <returns>The projects.</returns>
        private IEnumerable<ProjectSummary> ToSummaries(IEnumerable<ProjectSample> samples)
        {
            var now = this.timeProvider.GetUtcNow();

            return samples
                .Select(sample => new ProjectSummary(sample.Name, sample.Description, sample.Visibility, sample.Mode, sample.MemberCount,
                    sample.LifecycleState, now - sample.Age, sample.IsMember))
                .OrderByDescending(project => project.UpdatedAt);
        }
    }
}
