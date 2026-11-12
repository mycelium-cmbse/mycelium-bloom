// ------------------------------------------------------------------------------------------------
// <copyright file="PendingInvitationsViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Organization
{
    using DynamicData;

    using ErrorOr;

    using Mycelium.Bloom.Core.Organization;

    /// <summary>
    /// Loads the invitations of the organization of the page that are waiting for an answer, once, and holds them with the
    /// failure of the load. The invitations live in a source list that the dialog that invites a member can add to, so that
    /// an invitation sent joins the list without a new request.
    /// </summary>
    public sealed class PendingInvitationsViewModel : ListLoadingViewModel<InvitationRowViewModel>, IPendingInvitationsViewModel
    {
        /// <summary>
        /// Provides the invitations.
        /// </summary>
        private readonly IOrganizationInvitationService invitationService;

        /// <summary>
        /// Provides the current time, from which the dates of each invitation are told.
        /// </summary>
        private readonly TimeProvider timeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="PendingInvitationsViewModel" /> class and starts loading the invitations.
        /// </summary>
        /// <param name="context">The context of the organization whose invitations are listed.</param>
        /// <param name="invitationService">The service that provides the invitations.</param>
        /// <param name="timeProvider">The provider of the current time.</param>
        /// <param name="logger">The logger that receives a failure to load the invitations.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="invitationService" />, <paramref name="timeProvider" /> or <paramref name="logger" /> is
        /// <see langword="null" />.
        /// </exception>
        public PendingInvitationsViewModel(IOrganizationContextViewModel context, IOrganizationInvitationService invitationService, TimeProvider timeProvider, ILogger<PendingInvitationsViewModel> logger)
            : base(context.Organization.Id, "pending invitations", logger)
        {
            ArgumentNullException.ThrowIfNull(invitationService);
            ArgumentNullException.ThrowIfNull(timeProvider);

            this.invitationService = invitationService;
            this.timeProvider = timeProvider;
            this.BeginLoad();
        }

        /// <summary>
        /// Gets the pending invitations, which the dialog that invites a member can add to, so that an invitation sent joins the
        /// list without a new request.
        /// </summary>
        public ISourceList<InvitationRowViewModel> Invitations => this.Source;

        /// <summary>
        /// Gets the pending invitations of the organization as rows, the most recently sent first, told from one current time.
        /// </summary>
        /// <param name="organizationId">The identifier of the organization.</param>
        /// <param name="cancellationToken">The token that cancels the load.</param>
        /// <returns>The rows, or the errors of the service.</returns>
        protected override async Task<ErrorOr<IReadOnlyList<InvitationRowViewModel>>> LoadRowsAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            var result = await this.invitationService.GetPendingInvitationsAsync(organizationId, cancellationToken);
            var now = this.timeProvider.GetUtcNow();

            return result.Then(invitations => (IReadOnlyList<InvitationRowViewModel>)[.. invitations.Select(invitation => new InvitationRowViewModel(invitation, now))]);
        }
    }
}
