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
    using Mycelium.SDK.POCO;

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
        private static readonly Error OrganizationNotFound = Error.NotFound(ErrorCodes.NotFound, "The organization does not exist.");

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
        /// Gets the sample projects of an organization.
        /// </summary>
        /// <param name="organizationId">The identifier of the organization.</param>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>
        /// The projects, or an <see cref="ErrorCodes.NotFound" /> error when no sample organization has the
        /// identifier.
        /// </returns>
        /// <exception cref="OperationCanceledException">Thrown when the request is cancelled.</exception>
        public Task<ErrorOr<IReadOnlyList<FunctionalProject>>> GetProjectsAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!OrganizationPlaceholderData.Projects.TryGetValue(organizationId, out var samples))
            {
                return Task.FromResult<ErrorOr<IReadOnlyList<FunctionalProject>>>(OrganizationNotFound);
            }

            var now = this.timeProvider.GetUtcNow().UtcDateTime;

            IReadOnlyList<FunctionalProject> projects = [.. samples.Select(sample => ToProject(sample, now))];

            return Task.FromResult<ErrorOr<IReadOnlyList<FunctionalProject>>>(ErrorOrFactory.From(projects));
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
                Involves = [.. Enumerable.Range(0, sample.MemberCount).Select(_ => new ProjectMember { Id = Guid.NewGuid() })],
                UpdatedOn = now - sample.Age
            };
        }
    }
}
