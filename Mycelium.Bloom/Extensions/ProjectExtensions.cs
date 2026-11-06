// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectExtensions.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Extensions
{
    using Mycelium.SDK;

    /// <summary>
    /// Gives the icon by which the projects list shows the visibility of a project.
    /// </summary>
    public static class ProjectExtensions
    {
        /// <summary>
        /// Defines what the projects list shows about the visibility of a project.
        /// </summary>
        /// <param name="visibility">The visibility whose icon is read.</param>
        extension(ProjectVisibility visibility)
        {
            /// <summary>
            /// Gets the name of the icon of the visibility.
            /// </summary>
            public string Icon => visibility switch
            {
                ProjectVisibility.Private => "lock",
                ProjectVisibility.Organization => "building-2",
                _ => "globe"
            };
        }
    }
}
