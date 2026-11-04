// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectSample.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Organization.Placeholder
{
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Describes a sample project. Its last update is an age, so that the sample always looks recent.
    /// </summary>
    /// <remarks>
    /// Placeholder sample data, to be removed with the placeholder services when the Fabric client is bound.
    /// </remarks>
    /// <param name="Name">The name of the project.</param>
    /// <param name="Description">The short description of the project.</param>
    /// <param name="Visibility">Who can see the project.</param>
    /// <param name="Mode">How the members work on the project.</param>
    /// <param name="MemberCount">The number of members of the project.</param>
    /// <param name="LifecycleState">The current lifecycle state of the project.</param>
    /// <param name="Age">The time since the last update of the project.</param>
    /// <param name="IsMember">A value indicating whether the sample account is a member of the project.</param>
    public sealed record ProjectSample(string Name, string Description, ProjectVisibility Visibility, ProjectMode Mode,
        int MemberCount, ProjectLifecycleState LifecycleState, TimeSpan Age, bool IsMember);
}
