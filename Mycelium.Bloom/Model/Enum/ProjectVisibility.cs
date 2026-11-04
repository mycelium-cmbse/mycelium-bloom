// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectVisibility.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Enum
{
    /// <summary>
    /// Defines who can see a project.
    /// </summary>
    /// <remarks>
    /// Placeholder visibility of the projects page, to be replaced by the visibility of the Fabric API when the Fabric client is
    /// bound.
    /// </remarks>
    public enum ProjectVisibility
    {
        /// <summary>
        /// Only the members of the project can see it.
        /// </summary>
        Private,

        /// <summary>
        /// Every member of the organization can see the project.
        /// </summary>
        Organization,

        /// <summary>
        /// Anyone can see the project.
        /// </summary>
        Public
    }
}
