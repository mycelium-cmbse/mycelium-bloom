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
    /// Represents a project option displayed by a project switcher.
    /// </summary>
    public sealed class ProjectSwitcherItem
    {
        /// <summary>
        /// Gets or sets the stable project identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the project display name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional project description or organization label.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional project initial shown by the switcher.
        /// </summary>
        public string Initial { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the project cannot be selected.
        /// </summary>
        public bool Disabled { get; set; }
    }
}
