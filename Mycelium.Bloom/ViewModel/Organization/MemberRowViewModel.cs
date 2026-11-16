// ------------------------------------------------------------------------------------------------
// <copyright file="MemberRowViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Organization
{
    using System.Globalization;

    using Mycelium.SDK.POCO;

    /// <summary>
    /// Holds one row of the members list: the user as the SDK describes them, their role in the organization read from the
    /// relationships of the user, and when they joined and were last active in words. The avatar of the user is the view's.
    /// </summary>
    public sealed class MemberRowViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemberRowViewModel" /> class.
        /// </summary>
        /// <param name="user">The person to show, with their relationships.</param>
        /// <param name="organizationId">The identifier of the organization whose people are listed.</param>
        /// <param name="now">The current time, from which the last activity is told.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="user" /> is <see langword="null" />.</exception>
        public MemberRowViewModel(User user, Guid organizationId, DateTimeOffset now)
        {
            ArgumentNullException.ThrowIfNull(user);

            var membership = user.IsPartOfOrganizations.FirstOrDefault(member => member.Organization.Id == organizationId);
            var joinedOn = membership?.CreatedOn ?? user.IsPartOfProjects.Select(member => member.CreatedOn).DefaultIfEmpty(user.CreatedOn).Min();

            this.User = user;
            this.Role = membership?.Role;
            this.JoinedText = joinedOn.ToString("MMM yyyy", CultureInfo.InvariantCulture);
            this.LastActiveText = user.UpdatedOn > user.CreatedOn
                ? new DateTimeOffset(DateTime.SpecifyKind(user.UpdatedOn, DateTimeKind.Utc)).Humanize(now, CultureInfo.InvariantCulture)
                : "Never";
        }

        /// <summary>
        /// Gets the user.
        /// </summary>
        public User User { get; }

        /// <summary>
        /// Gets the role of the user in the organization, or <see langword="null" /> for an Outside Collaborator, who has no
        /// membership in it.
        /// </summary>
        public OrganizationMembershipRole? Role { get; }

        /// <summary>
        /// Gets the month and the year in which the user joined the organization or was given access to one of its projects.
        /// </summary>
        public string JoinedText { get; }

        /// <summary>
        /// Gets how long ago the user last contributed, in words, or "Never" for a user who did not contribute since their
        /// creation. A contribution is a change to the model, such as a commit, never a sign-in; the SDK has no such date yet,
        /// so the last update of the user stands for it until Fabric provides the last commit.
        /// </summary>
        public string LastActiveText { get; }
    }
}
