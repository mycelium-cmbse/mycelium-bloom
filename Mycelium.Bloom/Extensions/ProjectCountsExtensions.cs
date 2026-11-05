// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectCountsExtensions.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Extensions
{
    using Humanizer;

    using Mycelium.Bloom.Model;

    /// <summary>
    /// Writes the counts of the projects of an organization in words.
    /// </summary>
    public static class ProjectCountsExtensions
    {
        /// <summary>
        /// Writes the number of projects and the number in each lifecycle state.
        /// </summary>
        /// <param name="counts">The counts of the projects of the organization.</param>
        /// <returns>The subtitle of the projects page.</returns>
        public static string GetSubtitle(this ProjectCounts counts)
        {
            return $"{"project".ToQuantity(counts.Total)} · {counts.Open} open · {counts.Preparation} in preparation · "
                   + $"{counts.Review} in review · {counts.Archived} archived";
        }

        /// <summary>
        /// Writes how many projects a list shows out of the projects of the organization.
        /// </summary>
        /// <param name="counts">The counts of the projects of the organization.</param>
        /// <param name="shown">The number of projects that the list shows.</param>
        /// <returns>The text under the list.</returns>
        public static string GetShowingText(this ProjectCounts counts, int shown)
        {
            return $"Showing {shown} of {"project".ToQuantity(counts.Total)}";
        }
    }
}
