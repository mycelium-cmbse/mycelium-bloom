// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationContextResolution.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Organization
{
    using ErrorOr;

    using Mycelium.Bloom.Core;
    using Mycelium.Bloom.Model;
    using Mycelium.SDK.POCO;

    using Organization = Mycelium.SDK.POCO.Organization;

    /// <summary>
    /// Describes the outcome of resolving a location, published as one unit to the properties of the
    /// <see cref="OrganizationContextViewModel" />.
    /// </summary>
    /// <param name="Location">The resolved location.</param>
    /// <param name="IsLoading">A value indicating whether the location is being resolved.</param>
    /// <param name="Account">The current account, when it is known.</param>
    /// <param name="Organization">The designated organization, when it is known.</param>
    /// <param name="Capabilities">The capabilities of the account, when they are known.</param>
    /// <param name="Errors">The errors that prevented the resolution, or an empty list.</param>
    internal sealed record OrganizationContextResolution(OrganizationLocation Location, bool IsLoading, User Account, Organization Organization,
        OrganizationCapabilities Capabilities, IReadOnlyList<Error> Errors)
    {
        /// <summary>
        /// Creates the resolution of a location that is being resolved.
        /// </summary>
        /// <param name="location">The location being resolved, or <see langword="null" /> after a reset.</param>
        /// <returns>The loading resolution.</returns>
        public static OrganizationContextResolution CreateLoading(OrganizationLocation location)
        {
            return new OrganizationContextResolution(location, true, null, null, null, []);
        }

        /// <summary>
        /// Creates the resolution of a location that resolved completely.
        /// </summary>
        /// <param name="location">The resolved location.</param>
        /// <param name="account">The current account.</param>
        /// <param name="organization">The designated organization, or <see langword="null" /> on the account page.</param>
        /// <param name="capabilities">The capabilities of the account, or <see langword="null" /> on the account page.</param>
        /// <returns>The ready resolution.</returns>
        public static OrganizationContextResolution CreateReady(OrganizationLocation location, User account, Organization organization,
            OrganizationCapabilities capabilities)
        {
            return new OrganizationContextResolution(location, false, account, organization, capabilities, []);
        }

        /// <summary>
        /// Creates the resolution of a location that errors prevented.
        /// </summary>
        /// <param name="location">The resolved location.</param>
        /// <param name="errors">The errors that prevented the resolution.</param>
        /// <returns>The refused resolution.</returns>
        public static OrganizationContextResolution CreateRefused(OrganizationLocation location, IReadOnlyList<Error> errors)
        {
            return new OrganizationContextResolution(location, false, null, null, null, errors);
        }

        /// <summary>
        /// Creates the resolution of a location that designates nothing the account can reach.
        /// </summary>
        /// <param name="location">The resolved location.</param>
        /// <returns>The resolution refused with an <see cref="ErrorCodes.NotFound" /> error.</returns>
        public static OrganizationContextResolution CreateNotFound(OrganizationLocation location)
        {
            return CreateRefused(location, [Error.NotFound(ErrorCodes.NotFound, "The page does not exist.")]);
        }
    }
}
