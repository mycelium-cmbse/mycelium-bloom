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
    using Mycelium.SDK;
    using Mycelium.SDK.DTO;

    /// <summary>
    /// Answers with the sample projects until the Fabric client provides the real ones. It applies no visibility
    /// rule: every sample project of the organization is returned.
    /// </summary>
    /// <remarks>
    /// Placeholder of the projects page, to be replaced by the Fabric client when it is bound; only the service
    /// registration changes then. The visibility rule that <see cref="IOrganizationProjectService" /> promises (only the
    /// projects the current account may see) is a later logic issue.
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
        /// Gets the sample projects of an organization that match a filter, the most recently updated first.
        /// </summary>
        /// <param name="organizationId">The identifier of the organization.</param>
        /// <param name="filter">The criteria that the sample projects must match.</param>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>
        /// The projects, or an <see cref="OrganizationErrorCodes.NotFound" /> error when no sample organization has the
        /// identifier.
        /// </returns>
        /// <exception cref="OperationCanceledException">Thrown when the request is cancelled.</exception>
        public Task<ErrorOr<IReadOnlyList<FunctionalProject>>> GetProjectsAsync(Guid organizationId, ProjectFilter filter, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!OrganizationPlaceholderData.Projects.TryGetValue(organizationId, out var samples))
            {
                return Task.FromResult<ErrorOr<IReadOnlyList<FunctionalProject>>>(OrganizationNotFound);
            }

            var now = this.timeProvider.GetUtcNow().UtcDateTime;

            IReadOnlyList<FunctionalProject> projects =
            [
                .. samples
                    .Where(sample => Matches(sample, filter))
                    .Select(sample => ToProject(sample, now))
                    .OrderByDescending(project => project.UpdatedOn)
            ];

            return Task.FromResult<ErrorOr<IReadOnlyList<FunctionalProject>>>(ErrorOrFactory.From(projects));
        }

        /// <summary>
        /// Gets the number of sample projects of an organization, in total and by lifecycle state.
        /// </summary>
        /// <param name="organizationId">The identifier of the organization.</param>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>The counts, or an <see cref="OrganizationErrorCodes.NotFound" /> error when no sample organization has the identifier.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the request is cancelled.</exception>
        public Task<ErrorOr<ProjectCounts>> GetProjectCountsAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!OrganizationPlaceholderData.Projects.TryGetValue(organizationId, out var samples))
            {
                return Task.FromResult<ErrorOr<ProjectCounts>>(OrganizationNotFound);
            }

            var counts = new ProjectCounts(samples.Count, CountIn(samples, ProjectLifecycleKind.Preparation),
                CountIn(samples, ProjectLifecycleKind.Open), CountIn(samples, ProjectLifecycleKind.Review),
                CountIn(samples, ProjectLifecycleKind.Archived));

            return Task.FromResult<ErrorOr<ProjectCounts>>(counts);
        }

        /// <summary>
        /// Counts the sample projects in a lifecycle state.
        /// </summary>
        /// <param name="samples">The sample projects of the organization.</param>
        /// <param name="lifecycle">The lifecycle state to count.</param>
        /// <returns>The number of sample projects in the state.</returns>
        private static int CountIn(IReadOnlyList<ProjectSample> samples, ProjectLifecycleKind lifecycle)
        {
            return samples.Count(sample => sample.Lifecycle == lifecycle);
        }

        /// <summary>
        /// Tells whether a sample project satisfies every criterion of a filter.
        /// </summary>
        /// <param name="sample">The sample project.</param>
        /// <param name="filter">The criteria.</param>
        /// <returns><see langword="true" /> when the sample project matches.</returns>
        private static bool Matches(ProjectSample sample, ProjectFilter filter)
        {
            return (filter.Lifecycle is null || sample.Lifecycle == filter.Lifecycle)
                   && (filter.Mode is null || sample.Mode == filter.Mode)
                   && (sample.Name.Contains(filter.SearchText, StringComparison.OrdinalIgnoreCase)
                       || sample.Description.Contains(filter.SearchText, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Builds the project that the Fabric API would return for a sample. The members are anonymous identifiers: the
        /// projects page only counts them.
        /// </summary>
        /// <param name="sample">The sample project.</param>
        /// <param name="now">The current time, from which the age of the sample gives its last update.</param>
        /// <returns>The project.</returns>
        private static FunctionalProject ToProject(ProjectSample sample, DateTime now)
        {
            return new FunctionalProject
            {
                Id = Guid.NewGuid(),
                Name = sample.Name,
                Description = sample.Description,
                Visibility = sample.Visibility,
                CurrentMode = sample.Mode,
                Lifecycle = sample.Lifecycle,
                Involves = [.. Enumerable.Range(0, sample.MemberCount).Select(_ => Guid.NewGuid())],
                UpdatedOn = now - sample.Age
            };
        }
    }
}
