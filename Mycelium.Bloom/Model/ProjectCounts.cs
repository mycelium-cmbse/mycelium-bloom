// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectCounts.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    /// <summary>
    /// Counts the projects of an organization, in total and by lifecycle state.
    /// </summary>
    /// <remarks>
    /// Placeholder model of the projects page, to be replaced by the transfer object of the Fabric API when the Fabric
    /// client is bound.
    /// </remarks>
    /// <param name="Total">The number of projects.</param>
    /// <param name="Preparation">The number of projects in preparation.</param>
    /// <param name="Open">The number of open projects.</param>
    /// <param name="Review">The number of projects in review.</param>
    /// <param name="Archived">The number of archived projects.</param>
    public sealed record ProjectCounts(int Total, int Preparation, int Open, int Review, int Archived);
}
