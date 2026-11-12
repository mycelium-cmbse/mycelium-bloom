// ------------------------------------------------------------------------------------------------
// <copyright file="InvitationRowViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Organization
{
    using System.Globalization;

    using Mycelium.Bloom.Model;

    /// <summary>
    /// Holds one row of the pending invitations: who is invited, and when the invitation was sent and expires in words.
    /// </summary>
    public sealed class InvitationRowViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvitationRowViewModel" /> class.
        /// </summary>
        /// <param name="invitation">The invitation to show.</param>
        /// <param name="now">The current time, from which the dates are told.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="invitation" /> is <see langword="null" />.</exception>
        public InvitationRowViewModel(InvitationSummary invitation, DateTimeOffset now)
        {
            ArgumentNullException.ThrowIfNull(invitation);

            var sent = new DateTimeOffset(DateTime.SpecifyKind(invitation.SentOn, DateTimeKind.Utc)).Humanize(now, CultureInfo.InvariantCulture);
            var expires = new DateTimeOffset(DateTime.SpecifyKind(invitation.ExpiresOn, DateTimeKind.Utc)).Humanize(now, CultureInfo.InvariantCulture);

            this.InvitedIdentity = invitation.InvitedIdentity;
            this.Details = $"Invited as {invitation.Role.Humanize()} · Sent {sent} by {invitation.SentBy.Email} · Expires {expires}";
        }

        /// <summary>
        /// Gets the email address or the username of the invited person.
        /// </summary>
        public string InvitedIdentity { get; }

        /// <summary>
        /// Gets the role offered, the date of sending with its sender, and the date of expiry, in words.
        /// </summary>
        public string Details { get; }
    }
}
