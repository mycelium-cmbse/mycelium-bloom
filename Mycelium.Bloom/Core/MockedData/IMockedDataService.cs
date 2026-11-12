// ------------------------------------------------------------------------------------------------
// <copyright file="IMockedDataService.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.MockedData
{
    using Mycelium.SDK.POCO;

    using Organization = Mycelium.SDK.POCO.Organization;

    /// <summary>
    /// Provides a fictional, fully cross-referenced functional-data graph built from the SDK POCOs, so that screens and tests
    /// can work against the shapes the Fabric client will return without hand-writing samples.
    /// </summary>
    /// <remarks>
    /// Mocked data, to be removed when the Fabric client is bound.
    /// </remarks>
    public interface IMockedDataService
    {
        /// <summary>
        /// Gets the account that is using Bloom, which belongs to every mocked organization.
        /// </summary>
        User CurrentUser { get; }

        /// <summary>
        /// Gets every mocked user, in each <see cref="SDK.ActivationStatus" />, including the current one.
        /// </summary>
        IReadOnlyList<User> Users { get; }

        /// <summary>
        /// Gets the mocked organizations by canonical, lower-case slug: the SDK organization has no slug.
        /// </summary>
        IReadOnlyDictionary<string, Organization> Organizations { get; }

        /// <summary>
        /// Gets every mocked project, across all organizations.
        /// </summary>
        IReadOnlyList<FunctionalProject> Projects { get; }
    }
}
