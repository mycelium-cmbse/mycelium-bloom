// ------------------------------------------------------------------------------------------------
// <copyright file="DataTableColumn.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    /// <summary>
    /// Represents a column displayed inside a data table.
    /// </summary>
    public sealed class DataTableColumn
    {
        /// <summary>
        /// Gets or sets the key used to read values from table items.
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the visible column header.
        /// </summary>
        public string Header { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the column cells should use monospace typography.
        /// </summary>
        public bool IsMonospace { get; set; }

        /// <summary>
        /// Gets or sets whether the column cells should be right aligned.
        /// </summary>
        public bool IsRightAligned { get; set; }

        /// <summary>
        /// Gets or sets the optional CSS width for this column.
        /// </summary>
        public string Width { get; set; } = string.Empty;
    }
}
