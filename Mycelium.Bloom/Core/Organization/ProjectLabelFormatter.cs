// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectLabelFormatter.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Organization
{
    using System.ComponentModel;

    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Gives the words and the icon by which the projects list shows the lifecycle state, the visibility and the mode of
    /// a project.
    /// </summary>
    /// <remarks>
    /// Kept in one place so that the rows, the filter menus and the filter labels share the same wording, outside the views
    /// and the enums.
    /// </remarks>
    public static class ProjectLabelFormatter
    {
        /// <summary>
        /// Gets the label of a lifecycle state.
        /// </summary>
        /// <param name="state">The lifecycle state.</param>
        /// <returns>The label to show.</returns>
        /// <exception cref="InvalidEnumArgumentException">Thrown when <paramref name="state" /> is not a defined state.</exception>
        public static string GetLifecycleLabel(ProjectLifecycleState state)
        {
            return state switch
            {
                ProjectLifecycleState.Preparation => "Preparation",
                ProjectLifecycleState.Open => "Open",
                ProjectLifecycleState.Review => "In review",
                ProjectLifecycleState.Archived => "Archived",
                _ => throw new InvalidEnumArgumentException(nameof(state), (int)state, typeof(ProjectLifecycleState))
            };
        }

        /// <summary>
        /// Gets the label of a visibility.
        /// </summary>
        /// <param name="visibility">The visibility.</param>
        /// <returns>The label to show.</returns>
        /// <exception cref="InvalidEnumArgumentException">Thrown when <paramref name="visibility" /> is not a defined visibility.</exception>
        public static string GetVisibilityLabel(ProjectVisibility visibility)
        {
            return visibility switch
            {
                ProjectVisibility.Private => "Private",
                ProjectVisibility.Organization => "Organization",
                ProjectVisibility.Public => "Public",
                _ => throw new InvalidEnumArgumentException(nameof(visibility), (int)visibility, typeof(ProjectVisibility))
            };
        }

        /// <summary>
        /// Gets the name of the icon of a visibility.
        /// </summary>
        /// <param name="visibility">The visibility.</param>
        /// <returns>The name of the icon to show.</returns>
        /// <exception cref="InvalidEnumArgumentException">Thrown when <paramref name="visibility" /> is not a defined visibility.</exception>
        public static string GetVisibilityIcon(ProjectVisibility visibility)
        {
            return visibility switch
            {
                ProjectVisibility.Private => "lock",
                ProjectVisibility.Organization => "building-2",
                ProjectVisibility.Public => "globe",
                _ => throw new InvalidEnumArgumentException(nameof(visibility), (int)visibility, typeof(ProjectVisibility))
            };
        }

        /// <summary>
        /// Gets the label of a mode.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <returns>The label to show.</returns>
        /// <exception cref="InvalidEnumArgumentException">Thrown when <paramref name="mode" /> is not a defined mode.</exception>
        public static string GetModeLabel(ProjectMode mode)
        {
            return mode switch
            {
                ProjectMode.Regular => "Regular",
                ProjectMode.ConcurrentDesign => "Concurrent Design",
                _ => throw new InvalidEnumArgumentException(nameof(mode), (int)mode, typeof(ProjectMode))
            };
        }
    }
}
