// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectLifecycleState.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Enum
{
    /// <summary>
    /// Defines the lifecycle state of a project.
    /// </summary>
    public enum ProjectLifecycleState
    {
        /// <summary>
        /// The project is being set up and its baseline is being configured.
        /// </summary>
        Preparation,

        /// <summary>
        /// The project supports active modeling according to applicable ownership and permissions.
        /// </summary>
        Open,

        /// <summary>
        /// The project and model are under review, and modifications are not permitted.
        /// </summary>
        Review,

        /// <summary>
        /// The completed project is preserved as an immutable historical record.
        /// </summary>
        Archived
    }
}
