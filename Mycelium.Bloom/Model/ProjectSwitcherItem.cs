// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectSwitcherItem.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    /// <summary>
    /// Represents a project option rendered by the project switcher.
    /// </summary>
    public class ProjectSwitcherItem
    {
        /// <summary>
        /// Gets or sets the project identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the project display name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the project description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the project lifecycle state.
        /// </summary>
        public string Lifecycle { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the project option is disabled.
        /// </summary>
        public bool Disabled { get; set; }
    }
}
