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
    using Mycelium.SDK;
    using Mycelium.SDK.DTO;

    using Organization = Mycelium.SDK.DTO.Organization;

    /// <summary>
    /// Holds the fictional sample data that every placeholder organization service answers with.
    /// </summary>
    /// <remarks>
    /// Placeholder sample data, to be removed with the placeholder services when the Fabric client is bound.
    /// </remarks>
    public static class OrganizationPlaceholderData
    {
        /// <summary>
        /// Gets the sample account, which belongs to every sample organization.
        /// </summary>
        public static User Account { get; } = new() { Id = new Guid("8d6c2a54-3f1e-4b97-a6d2-0e5b7c914a30"), Name = "Demo Account", Mail = "demo.account@example.org" };

        /// <summary>
        /// Gets the sample organizations by slug: the SDK organization has no slug, which the pages take from the address.
        /// </summary>
        public static IReadOnlyDictionary<string, Organization> Organizations { get; } = new Dictionary<string, Organization>
        {
            ["orbital-systems"] = new() { Id = new Guid("2f4a9b61-7c3d-4e85-b1a0-d6e8f3027c15"), Name = "Orbital Systems" },
            ["payload-lab"] = new() { Id = new Guid("c07e5d38-91a4-4f2b-8e6c-4a1b9d70f263"), Name = "Payload Lab" }
        };

        /// <summary>
        /// Gets the sample projects of each sample organization, by organization identifier. Orbital Systems has twelve
        /// projects, in every visibility, mode and lifecycle state; Payload Lab has none.
        /// </summary>
        public static IReadOnlyDictionary<Guid, IReadOnlyList<ProjectSample>> Projects { get; } =
            new Dictionary<Guid, IReadOnlyList<ProjectSample>>
            {
                [Organizations["orbital-systems"].Id] =
                [
                    new("Aurora Orbiter", "LEO imaging satellite", ProjectVisibility.Organization, ProjectMode.Regular, 12, ProjectLifecycleKind.Open, TimeSpan.FromHours(2)),
                    new("Lunar Relay Study", "Phase A concept", ProjectVisibility.Private, ProjectMode.Concurrent, 8, ProjectLifecycleKind.Review, TimeSpan.FromDays(1)),
                    new("Deimos Lander Concept", "Descent and landing study", ProjectVisibility.Private, ProjectMode.Concurrent, 26, ProjectLifecycleKind.Preparation, TimeSpan.FromMinutes(30)),
                    new("Propulsion Research", "Electric propulsion R&D", ProjectVisibility.Private, ProjectMode.Regular, 15, ProjectLifecycleKind.Open, TimeSpan.FromHours(3)),
                    new("Mars Sample Relay", "Phase 0 feasibility", ProjectVisibility.Private, ProjectMode.Concurrent, 21, ProjectLifecycleKind.Open, TimeSpan.FromHours(4)),
                    new("Comms Constellation", "Inter-satellite link study", ProjectVisibility.Organization, ProjectMode.Regular, 11, ProjectLifecycleKind.Open, TimeSpan.FromDays(1.5)),
                    new("Ground Station v2", "Ground segment data model", ProjectVisibility.Organization, ProjectMode.Regular, 9, ProjectLifecycleKind.Open, TimeSpan.FromDays(5)),
                    new("Solar Array Trade-off", "Power sizing study", ProjectVisibility.Organization, ProjectMode.Regular, 6, ProjectLifecycleKind.Open, TimeSpan.FromDays(6)),
                    new("Attitude Benchmark", "Attitude control", ProjectVisibility.Organization, ProjectMode.Regular, 5, ProjectLifecycleKind.Archived, TimeSpan.FromDays(7)),
                    new("Thermal Reference Models", "Shared thermal library", ProjectVisibility.Public, ProjectMode.Regular, 4, ProjectLifecycleKind.Open, TimeSpan.FromDays(14)),
                    new("Ocean Survey", "Payload subsystem model", ProjectVisibility.Public, ProjectMode.Regular, 7, ProjectLifecycleKind.Open, TimeSpan.FromDays(21)),
                    new("Legacy Migration", "Archive of imported models", ProjectVisibility.Private, ProjectMode.Regular, 3, ProjectLifecycleKind.Archived, TimeSpan.FromDays(35))
                ],
                [Organizations["payload-lab"].Id] = []
            };
    }
}
