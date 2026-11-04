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
    using Mycelium.Bloom.Model.Enum;

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
        public static Account Account { get; } = new(new Guid("8d6c2a54-3f1e-4b97-a6d2-0e5b7c914a30"), "Demo Account", "demo.account@example.org");

        /// <summary>
        /// Gets the sample organizations.
        /// </summary>
        public static IReadOnlyList<Organization> Organizations { get; } =
        [
            new(new Guid("2f4a9b61-7c3d-4e85-b1a0-d6e8f3027c15"), "orbital-systems", "Orbital Systems"),
            new(new Guid("c07e5d38-91a4-4f2b-8e6c-4a1b9d70f263"), "payload-lab", "Payload Lab")
        ];

        /// <summary>
        /// Gets the sample projects of each sample organization, by organization identifier. Orbital Systems has twelve
        /// projects, in every visibility, mode and lifecycle state; Payload Lab has none.
        /// </summary>
        public static IReadOnlyDictionary<Guid, IReadOnlyList<ProjectSample>> Projects { get; } =
            new Dictionary<Guid, IReadOnlyList<ProjectSample>>
            {
                [Organizations[0].Id] =
                [
                    new("Aurora Orbiter", "LEO imaging satellite", ProjectVisibility.Organization, ProjectMode.Regular, 12, ProjectLifecycleState.Open, TimeSpan.FromHours(2), true),
                    new("Lunar Relay Study", "Phase A concept", ProjectVisibility.Private, ProjectMode.ConcurrentDesign, 8, ProjectLifecycleState.Review, TimeSpan.FromDays(1), true),
                    new("Deimos Lander Concept", "Descent and landing study", ProjectVisibility.Private, ProjectMode.ConcurrentDesign, 26, ProjectLifecycleState.Preparation, TimeSpan.FromMinutes(30), false),
                    new("Propulsion Research", "Electric propulsion R&D", ProjectVisibility.Private, ProjectMode.Regular, 15, ProjectLifecycleState.Open, TimeSpan.FromHours(3), true),
                    new("Mars Sample Relay", "Phase 0 feasibility", ProjectVisibility.Private, ProjectMode.ConcurrentDesign, 21, ProjectLifecycleState.Open, TimeSpan.FromHours(4), false),
                    new("Comms Constellation", "Inter-satellite link study", ProjectVisibility.Organization, ProjectMode.Regular, 11, ProjectLifecycleState.Open, TimeSpan.FromDays(1.5), true),
                    new("Ground Station v2", "Ground segment data model", ProjectVisibility.Organization, ProjectMode.Regular, 9, ProjectLifecycleState.Open, TimeSpan.FromDays(5), true),
                    new("Solar Array Trade-off", "Power sizing study", ProjectVisibility.Organization, ProjectMode.Regular, 6, ProjectLifecycleState.Open, TimeSpan.FromDays(6), false),
                    new("Attitude Benchmark", "Attitude control", ProjectVisibility.Organization, ProjectMode.Regular, 5, ProjectLifecycleState.Archived, TimeSpan.FromDays(7), true),
                    new("Thermal Reference Models", "Shared thermal library", ProjectVisibility.Public, ProjectMode.Regular, 4, ProjectLifecycleState.Open, TimeSpan.FromDays(14), true),
                    new("Ocean Survey", "Payload subsystem model", ProjectVisibility.Public, ProjectMode.Regular, 7, ProjectLifecycleState.Open, TimeSpan.FromDays(21), false),
                    new("Legacy Migration", "Archive of imported models", ProjectVisibility.Private, ProjectMode.Regular, 3, ProjectLifecycleState.Archived, TimeSpan.FromDays(35), true)
                ],
                [Organizations[1].Id] = []
            };
    }
}
