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
    using Humanizer;

    using Mycelium.SDK;

    /// <summary>
    /// Gives the words and the icon by which the projects list shows the lifecycle state, the visibility and the mode of a
    /// project. Humanizer writes the label from the name of the member; the members whose label differs are named here.
    /// </summary>
    public static class ProjectExtensions
    {
        /// <summary>
        /// Gets the label of a lifecycle state.
        /// </summary>
        /// <param name="lifecycle">The lifecycle state.</param>
        /// <returns>The label to show.</returns>
        public static string GetLabel(this ProjectLifecycleKind lifecycle)
        {
            return lifecycle == ProjectLifecycleKind.Review ? "In review" : lifecycle.Humanize();
        }

        /// <summary>
        /// Gets the label of a visibility.
        /// </summary>
        /// <param name="visibility">The visibility.</param>
        /// <returns>The label to show.</returns>
        public static string GetLabel(this ProjectVisibility visibility)
        {
            return visibility.Humanize();
        }

        /// <summary>
        /// Gets the label of a mode.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <returns>The label to show.</returns>
        public static string GetLabel(this ProjectMode mode)
        {
            return mode == ProjectMode.Concurrent ? "Concurrent Design" : mode.Humanize();
        }

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
