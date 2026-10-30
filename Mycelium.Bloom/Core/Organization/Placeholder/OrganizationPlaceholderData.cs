// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationPlaceholderData.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Organization.Placeholder
{
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Holds the fictional sample data that every placeholder organization service answers with.
    /// </summary>
    public static class OrganizationPlaceholderData
    {
        /// <summary>
        /// Gets the sample account, which belongs to every sample organization.
        /// </summary>
        public static Account Account { get; } = new(
            new Guid("8d6c2a54-3f1e-4b97-a6d2-0e5b7c914a30"),
            "Demo Account",
            "demo.account@example.org");

        /// <summary>
        /// Gets the sample organizations.
        /// </summary>
        public static IReadOnlyList<Organization> Organizations { get; } =
        [
            new(new Guid("2f4a9b61-7c3d-4e85-b1a0-d6e8f3027c15"), "orbital-systems", "Orbital Systems"),
            new(new Guid("c07e5d38-91a4-4f2b-8e6c-4a1b9d70f263"), "payload-lab", "Payload Lab")
        ];
    }
}
