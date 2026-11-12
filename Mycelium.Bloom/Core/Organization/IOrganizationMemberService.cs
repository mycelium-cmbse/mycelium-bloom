// ------------------------------------------------------------------------------------------------
// <copyright file="IOrganizationMemberService.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Organization
{
    using ErrorOr;

    using Mycelium.SDK.POCO;

    /// <summary>
    /// Provides the people of an organization as the users of the SDK, linked to the organization and to its projects by the
    /// relationships of the SDK (an <c>OrganizationMember</c> for a member, a <c>ProjectMember</c> for an Outside Collaborator). Every answer is an <see cref="ErrorOr{TValue}" />: an
    /// unknown organization or an account that may not see its people is a typed error, never an exception. It brings the
    /// people only: counting and narrowing them is done by whoever shows them.
    /// </summary>
    /// <remarks>
    /// Shaped for the Fabric API: the methods are asynchronous and cancellable, and the answers carry no view concern. The
    /// list is not paged, because paging belongs to the view. The real service applies the rights of the caller itself: an
    /// Outside Collaborator is never returned to an account that may not see them.
    /// </remarks>
    public interface IOrganizationMemberService
    {
        /// <summary>
        /// Gets the people of an organization. An Outside Collaborator is returned only to an account that may see them.
        /// </summary>
        /// <param name="organizationId">The identifier of the organization.</param>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>The people, or the error that tells why they cannot be returned.</returns>
        Task<ErrorOr<IReadOnlyList<User>>> GetMembersAsync(Guid organizationId, CancellationToken cancellationToken);
    }
}
