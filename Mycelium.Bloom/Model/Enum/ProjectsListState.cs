// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectsListState.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Enum
{
    /// <summary>
    /// Defines what the projects page shows below its title.
    /// </summary>
    public enum ProjectsListState
    {
        /// <summary>
        /// The projects are being retrieved.
        /// </summary>
        Loading,

        /// <summary>
        /// The organization has projects; the list shows those that match the search and the filters, possibly none.
        /// </summary>
        Ready,

        /// <summary>
        /// The organization has no project.
        /// </summary>
        Empty,

        /// <summary>
        /// The projects could not be retrieved.
        /// </summary>
        Error
    }
}
