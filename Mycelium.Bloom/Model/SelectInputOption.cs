// ------------------------------------------------------------------------------------------------
// <copyright file="SelectInputOption.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    /// <summary>
    /// Represents an option rendered by a select input.
    /// </summary>
    public sealed class SelectInputOption
    {
        /// <summary>
        /// Gets or sets the option value.
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the visible option label.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the option is disabled.
        /// </summary>
        public bool Disabled { get; set; }
    }
}
