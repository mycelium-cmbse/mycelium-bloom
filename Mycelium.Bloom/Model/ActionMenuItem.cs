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
    /// Represents an action displayed inside a reusable menu.
    /// </summary>
    public sealed class ActionMenuItem
    {
        /// <summary>
        /// Gets or sets the stable action identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the visible action label.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional supporting description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional short icon text.
        /// </summary>
        public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets an optional repository-owned SVG symbol.
        /// </summary>
        public SymbolIconName? Symbol { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the action is disabled.
        /// </summary>
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the action is destructive.
        /// </summary>
        public bool Destructive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a separator is rendered before the action.
        /// </summary>
        public bool SeparatorBefore { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the action represents the current selection.
        /// </summary>
        public bool IsSelected { get; set; }
    }
}
