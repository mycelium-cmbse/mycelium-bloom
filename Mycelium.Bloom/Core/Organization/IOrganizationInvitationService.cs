// ------------------------------------------------------------------------------------------------
// <copyright file="IOrganizationInvitationService.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Organization
{
    using ErrorOr;

    using Mycelium.Bloom.Model;

    /// <summary>
    /// Provides the invitations of an organization. Every answer is an <see cref="ErrorOr{TValue}" />: an unknown organization
    /// is a typed error, never an exception.
    /// </summary>
    /// <remarks>
    /// Shaped for the Fabric API: the method is asynchronous and cancellable, and the answer carries no view concern. The list
    /// is not paged, because paging belongs to the view.
    /// </remarks>
    public interface IOrganizationInvitationService
    {
        /// <summary>
        /// Gets the invitations of an organization that are waiting for an answer and have not expired, the most recently sent
        /// first.
        /// </summary>
        /// <param name="organizationId">The identifier of the organization.</param>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>The pending invitations, or the error that tells why they cannot be returned.</returns>
        Task<ErrorOr<IReadOnlyList<InvitationSummary>>> GetPendingInvitationsAsync(Guid organizationId, CancellationToken cancellationToken);
    }
}
