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

    using Mycelium.Bloom.Core.MockedData;
    using Mycelium.SDK.POCO;

    /// <summary>
    /// Answers with the mocked projects until the Fabric client provides the real ones. It applies no visibility
    /// rule: every mocked project of the organization is returned.
    /// </summary>
    /// <remarks>
    /// Placeholder of the projects page, to be replaced by the Fabric client when it is bound; only the service
    /// registration changes then. The visibility rule that <see cref="IOrganizationProjectService" /> promises (only the
    /// projects the current account may see) is a later logic issue.
    /// </remarks>
    public sealed class PlaceholderOrganizationProjectService : IOrganizationProjectService
    {
        /// <summary>
        /// The error returned for an identifier that no mocked organization has.
        /// </summary>
        private static readonly Error OrganizationNotFound = Error.NotFound(ErrorCodes.NotFound, "The organization does not exist.");

        /// <summary>
        /// Provides the mocked organizations and their projects.
        /// </summary>
        private readonly IMockedDataService mockedDataService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaceholderOrganizationProjectService" /> class.
        /// </summary>
        /// <param name="mockedDataService">The provider of the mocked organizations and their projects.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="mockedDataService" /> is <see langword="null" />.</exception>
        public PlaceholderOrganizationProjectService(IMockedDataService mockedDataService)
        {
            ArgumentNullException.ThrowIfNull(mockedDataService);

            this.mockedDataService = mockedDataService;
        }

        /// <summary>
        /// Gets the mocked projects of an organization.
        /// </summary>
        /// <param name="organizationId">The identifier of the organization.</param>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>
        /// The projects, or an <see cref="ErrorCodes.NotFound" /> error when no mocked organization has the identifier.
        /// </returns>
        /// <exception cref="OperationCanceledException">Thrown when the request is cancelled.</exception>
        public Task<ErrorOr<IReadOnlyList<FunctionalProject>>> GetProjectsAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var organization = this.mockedDataService.Organizations.Values.FirstOrDefault(candidate => candidate.Id == organizationId);

            if (organization == null)
            {
                return Task.FromResult<ErrorOr<IReadOnlyList<FunctionalProject>>>(OrganizationNotFound);
            }

            IReadOnlyList<FunctionalProject> projects = [.. organization.Projects.OfType<FunctionalProject>()];

            return Task.FromResult<ErrorOr<IReadOnlyList<FunctionalProject>>>(ErrorOrFactory.From(projects));
        }
    }
}
