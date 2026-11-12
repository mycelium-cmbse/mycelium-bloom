// ------------------------------------------------------------------------------------------------
// <copyright file="PlaceholderOrganizationMemberService.cs" company="Starion Group S.A.">
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
    using Mycelium.SDK.POCO;

    /// <summary>
    /// Answers with the mocked people until the Fabric client provides the real ones: the members of the organization and the
    /// Outside Collaborators of its projects. It applies two rules: an Outside Collaborator is listed to an Organization
    /// Administrator only, and a deleted account is never listed, since it no longer exists (SSS-OA-USR-N35).
    /// </summary>
    /// <remarks>
    /// Placeholder of the members page, to be replaced by the Fabric client when it is bound; only the service
    /// registration changes then.
    /// </remarks>
    public sealed class PlaceholderOrganizationMemberService : IOrganizationMemberService
    {
        /// <summary>
        /// The error returned for an identifier that no mocked organization has.
        /// </summary>
        private static readonly Error OrganizationNotFound = Error.NotFound(ErrorCodes.NotFound, "The organization does not exist.");

        /// <summary>
        /// Provides the role that the placeholder gives to the current account.
        /// </summary>
        private readonly IOptions<OrganizationOptions> options;

        /// <summary>
        /// Provides the mocked organizations, their members and their projects.
        /// </summary>
        private readonly IMockedDataService mockedDataService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaceholderOrganizationMemberService" /> class.
        /// </summary>
        /// <param name="options">The organization options, which hold the role of the current account.</param>
        /// <param name="mockedDataService">The provider of the mocked organizations.</param>
        /// <exception cref="ArgumentNullException">Thrown when an argument is <see langword="null" />.</exception>
        public PlaceholderOrganizationMemberService(IOptions<OrganizationOptions> options, IMockedDataService mockedDataService)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(mockedDataService);

            this.options = options;
            this.mockedDataService = mockedDataService;
        }

        /// <summary>
        /// Gets the mocked people of an organization.
        /// </summary>
        /// <param name="organizationId">The identifier of the organization.</param>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>The people, or a not found error when no mocked organization has this identifier.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the request is cancelled.</exception>
        public Task<ErrorOr<IReadOnlyList<User>>> GetMembersAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var organization = this.mockedDataService.Organizations.Values.FirstOrDefault(candidate => candidate.Id == organizationId);

            if (organization == null)
            {
                return Task.FromResult<ErrorOr<IReadOnlyList<User>>>(OrganizationNotFound);
            }

            var members = organization.InvolvedUser.Select(membership => membership.User).OfType<User>();
            var canSeeCollaborators = this.options.Value.PlaceholderRole == OrganizationMembershipRole.Administrator;
            var collaborators = canSeeCollaborators ? organization.Projects.SelectMany(project => project.Involves).Where(member => member.IsOutsideCollaborator).Select(member => member.User).OfType<User>() : [];
            IReadOnlyList<User> people = [.. members.Concat(collaborators).Distinct().Where(user => user.Status != ActivationStatus.Deleted)];

            return Task.FromResult<ErrorOr<IReadOnlyList<User>>>(ErrorOrFactory.From(people));
        }
    }
}
