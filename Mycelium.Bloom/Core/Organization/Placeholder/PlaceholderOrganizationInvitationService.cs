// ------------------------------------------------------------------------------------------------
// <copyright file="PlaceholderOrganizationInvitationService.cs" company="Starion Group S.A.">
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
    using Mycelium.Bloom.Model;
    using Mycelium.SDK.POCO;

    /// <summary>
    /// Answers with sample invitations sent by the administrators of a mocked organization until the Fabric client provides
    /// the real ones. It drops the expired ones and puts the most recently sent first.
    /// </summary>
    /// <remarks>
    /// Placeholder of the invitations block, to be replaced by the Fabric client when it is bound; only the service
    /// registration changes then. The SDK has no invitation type, so the invitations are built here and not by the mocked
    /// data service.
    /// </remarks>
    public sealed class PlaceholderOrganizationInvitationService : IOrganizationInvitationService
    {
        /// <summary>
        /// The error returned for an identifier that no mocked organization has.
        /// </summary>
        private static readonly Error OrganizationNotFound = Error.NotFound(ErrorCodes.NotFound, "The organization does not exist.");

        /// <summary>
        /// Provides the mocked organizations and their administrators.
        /// </summary>
        private readonly IMockedDataService mockedDataService;

        /// <summary>
        /// Provides the current time, from which the age of each sample invitation gives its dates.
        /// </summary>
        private readonly TimeProvider timeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaceholderOrganizationInvitationService" /> class.
        /// </summary>
        /// <param name="mockedDataService">The provider of the mocked organizations.</param>
        /// <param name="timeProvider">The provider of the current time.</param>
        /// <exception cref="ArgumentNullException">Thrown when an argument is <see langword="null" />.</exception>
        public PlaceholderOrganizationInvitationService(IMockedDataService mockedDataService, TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(mockedDataService);
            ArgumentNullException.ThrowIfNull(timeProvider);

            this.mockedDataService = mockedDataService;
            this.timeProvider = timeProvider;
        }

        /// <summary>
        /// Gets the sample invitations of an organization that have not expired.
        /// </summary>
        /// <param name="organizationId">The identifier of the organization.</param>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>The pending invitations, or a not found error when no mocked organization has this identifier.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the request is cancelled.</exception>
        public Task<ErrorOr<IReadOnlyList<InvitationSummary>>> GetPendingInvitationsAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var organization = this.mockedDataService.Organizations.Values.FirstOrDefault(candidate => candidate.Id == organizationId);

            if (organization == null)
            {
                return Task.FromResult<ErrorOr<IReadOnlyList<InvitationSummary>>>(OrganizationNotFound);
            }

            var senders = organization.InvolvedUser.Where(member => member.Role == OrganizationMembershipRole.Administrator).Select(member => member.User).OfType<User>().ToList();
            var now = this.timeProvider.GetUtcNow().UtcDateTime;
            IReadOnlyList<InvitationSummary> pending = [.. CreateInvitations(senders, now).Where(invitation => invitation.ExpiresOn > now).OrderByDescending(invitation => invitation.SentOn)];

            return Task.FromResult<ErrorOr<IReadOnlyList<InvitationSummary>>>(ErrorOrFactory.From(pending));
        }

        /// <summary>
        /// Creates the sample invitations of an organization, always as Organization Member and sent by its administrators. An
        /// expired one is kept so that the service proves that it drops the invitations that no longer wait for an answer.
        /// </summary>
        /// <param name="senders">The administrators of the organization.</param>
        /// <param name="now">The current time, in UTC.</param>
        /// <returns>The invitations, or none for an organization without administrator.</returns>
        private static IReadOnlyList<InvitationSummary> CreateInvitations(List<User> senders, DateTime now)
        {
            if (senders.Count == 0)
            {
                return [];
            }

            return
            [
                new("a.nowak@example.org", OrganizationMembershipRole.Member, senders[0], now - TimeSpan.FromDays(2), now + TimeSpan.FromDays(5)),
                new("l.moreau", OrganizationMembershipRole.Member, senders[^1], now - TimeSpan.FromHours(5), now + TimeSpan.FromDays(7)),
                new("p.gomez@example.org", OrganizationMembershipRole.Member, senders[0], now - TimeSpan.FromDays(9), now - TimeSpan.FromDays(2))
            ];
        }
    }
}
