// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectSummary.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Describes a project of an organization as the projects list shows it.
    /// </summary>
    /// <remarks>
    /// Placeholder model of the projects page, to be replaced by the transfer object of the Fabric API when the Fabric
    /// client is bound.
    /// </remarks>
    /// <param name="Name">The name of the project.</param>
    /// <param name="Description">The short description of the project.</param>
    /// <param name="Visibility">Who can see the project.</param>
    /// <param name="Mode">How the members work on the project.</param>
    /// <param name="MemberCount">The number of members of the project.</param>
    /// <param name="LifecycleState">The current lifecycle state of the project.</param>
    /// <param name="UpdatedAt">The moment of the last update of the project.</param>
    /// <param name="IsMember">A value indicating whether the current account is a member of the project.</param>
    public sealed record ProjectSummary(string Name, string Description, ProjectVisibility Visibility, ProjectMode Mode,
        int MemberCount, ProjectLifecycleState LifecycleState, DateTimeOffset UpdatedAt, bool IsMember);
}
