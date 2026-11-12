// ------------------------------------------------------------------------------------------------
// <copyright file="InvitationSummary.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    using Mycelium.SDK.POCO;

    /// <summary>
    /// Describes an invitation that is waiting for the answer of the invited person.
    /// </summary>
    /// <remarks>
    /// Provisional front-end model: the SDK holds no invitation type, and the invitation is a Fabric requirement
    /// (<c>SSS-FB-AUTH-L0Z</c>). The Fabric transfer object replaces this record when the Fabric client is bound; the invitation
    /// service registration is the only other place that changes.
    /// </remarks>
    /// <param name="InvitedIdentity">The email address or the username of the invited person.</param>
    /// <param name="Role">The role that the invitation offers, which is always the member role.</param>
    /// <param name="SentBy">The user who sent the invitation.</param>
    /// <param name="SentOn">When the invitation was sent, in UTC.</param>
    /// <param name="ExpiresOn">When the invitation expires, in UTC.</param>
    public sealed record InvitationSummary(string InvitedIdentity, OrganizationMembershipRole Role, User SentBy, DateTime SentOn, DateTime ExpiresOn);
}
