// ------------------------------------------------------------------------------------------------
// <copyright file="ActionMenuItem.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Represents an item displayed inside an action menu.
    /// </summary>
    public sealed class ActionMenuItem
    {
        /// <summary>
        /// Gets or sets the unique action value.
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the visible action label.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional action description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional action icon text.
        /// </summary>
        public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the action is disabled.
        /// </summary>
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets whether a separator should be rendered before the action.
        /// </summary>
        public bool SeparatorBefore { get; set; }

        /// <summary>
        /// Gets or sets the visual variant of the action item.
        /// </summary>
        public ActionMenuItemVariant Variant { get; set; } = ActionMenuItemVariant.Default;
    }
}
