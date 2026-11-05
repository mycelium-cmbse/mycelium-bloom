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
