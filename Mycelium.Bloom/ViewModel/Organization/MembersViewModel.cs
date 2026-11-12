// ------------------------------------------------------------------------------------------------
// <copyright file="MembersViewModel.cs" company="Starion Group S.A.">
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
    /// Loads the people of the organization of the page once and holds them, with the failure of the load. The people live
    /// in a source list that others can subscribe to or add to, and the rows mirror it. An account that may invite members
    /// also gets the pending invitations of the organization.
    /// </summary>
    public sealed class MembersViewModel : ListLoadingViewModel<MemberRowViewModel>, IMembersViewModel
    {
        /// <summary>
        /// Provides the people.
        /// </summary>
        private readonly IOrganizationMemberService memberService;

        /// <summary>
        /// Provides the current time, from which the last activity of each person is told.
        /// </summary>
        private readonly TimeProvider timeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="MembersViewModel" /> class and starts loading the people.
        /// </summary>
        /// <param name="context">The context of the organization whose people are listed, which is ready.</param>
        /// <param name="memberService">The service that provides the people.</param>
        /// <param name="invitationsFactory">
        /// The factory of the pending invitations block, called for an account that may invite.
        /// </param>
        /// <param name="timeProvider">The provider of the current time.</param>
        /// <param name="logger">The logger that receives a failure to load the people.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="memberService" />, <paramref name="invitationsFactory" />,
        /// <paramref name="timeProvider" /> or <paramref name="logger" /> is <see langword="null" />.
        /// </exception>
        public MembersViewModel(IOrganizationContextViewModel context, IOrganizationMemberService memberService, Func<IPendingInvitationsViewModel> invitationsFactory, TimeProvider timeProvider, ILogger<MembersViewModel> logger)
            : base(context.Organization.Id, "members", logger)
        {
            ArgumentNullException.ThrowIfNull(memberService);
            ArgumentNullException.ThrowIfNull(invitationsFactory);
            ArgumentNullException.ThrowIfNull(timeProvider);

            this.memberService = memberService;
            this.timeProvider = timeProvider;
            this.CanInviteMembers = context.Capabilities is { CanInviteMembers: true };
            this.CanViewOutsideCollaborators = context.Capabilities is { CanViewOutsideCollaborators: true };
            this.Invitations = this.CanInviteMembers ? invitationsFactory() : null;

            if (this.Invitations is not null)
            {
                this.Disposables.Add(this.Invitations);
            }

            this.BeginLoad();
        }

        /// <summary>
        /// Gets every person that the account may see, which a later ViewModel can subscribe to or add to.
        /// </summary>
        public ISourceList<MemberRowViewModel> Members => this.Source;

        /// <summary>
        /// Gets the pending invitations of the organization, or <see langword="null" /> for an account that may not invite
        /// members and so neither loads nor sees them. They load on their own, so that a failure there never hides the members.
        /// </summary>
        public IPendingInvitationsViewModel Invitations { get; }

        /// <summary>
        /// Gets a value indicating whether the current account may invite people.
        /// </summary>
        public bool CanInviteMembers { get; }

        /// <summary>
        /// Gets a value indicating whether the current account may see the Outside Collaborators of the organization.
        /// </summary>
        public bool CanViewOutsideCollaborators { get; }

        /// <summary>
        /// Gets the people of the organization as rows, in the order the service returns them, told from one current time.
        /// </summary>
        /// <param name="organizationId">The identifier of the organization.</param>
        /// <param name="cancellationToken">The token that cancels the load.</param>
        /// <returns>The rows, or the errors of the service.</returns>
        protected override async Task<ErrorOr<IReadOnlyList<MemberRowViewModel>>> LoadRowsAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            var result = await this.memberService.GetMembersAsync(organizationId, cancellationToken);
            var now = this.timeProvider.GetUtcNow();

            return result.Then(members => (IReadOnlyList<MemberRowViewModel>)[.. members.Select(member => new MemberRowViewModel(member, organizationId, now))]);
        }
    }
}
