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
    /// Gives the icon by which the projects list shows the visibility of a project. The labels come from Humanizer, which
    /// writes them from the names of the members.
    /// </summary>
    public static class ProjectExtensions
    {
        /// <summary>
        /// Gets the name of the icon of a visibility.
        /// </summary>
        /// <param name="visibility">The visibility.</param>
        /// <returns>The name of the icon to show.</returns>
        public static string GetIcon(this ProjectVisibility visibility)
        {
            return visibility switch
            {
                ProjectVisibility.Private => "lock",
                ProjectVisibility.Organization => "building-2",
                _ => "globe"
            };
        }
    }
}
