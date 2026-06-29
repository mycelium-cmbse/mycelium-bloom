// ------------------------------------------------------------------------------------------------
// <copyright file="ModelTreeItem.cs" company="Starion Group S.A.">
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
    /// Represents one item in the Bloom model tree.
    /// </summary>
    public class ModelTreeItem
    {
        /// <summary>
        /// Gets or sets the unique item identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the visible item title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the SysML stereotype text.
        /// </summary>
        public string Stereotype { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ownership state.
        /// </summary>
        public TreeNodeOwnership Ownership { get; set; } = TreeNodeOwnership.None;

        /// <summary>
        /// Gets or sets the ownership color.
        /// </summary>
        public string OwnershipColor { get; set; } = "var(--mb-color-ownership-aocs)";

        /// <summary>
        /// Gets or sets the default element color.
        /// </summary>
        public string ElementColor { get; set; } = "var(--mb-color-neutral-600)";

        /// <summary>
        /// Gets or sets whether the item has comments.
        /// </summary>
        public bool HasComment { get; set; }

        /// <summary>
        /// Gets or sets whether the item has modifications.
        /// </summary>
        public bool IsModified { get; set; }

        /// <summary>
        /// Gets or sets child items.
        /// </summary>
        public IReadOnlyList<ModelTreeItem> Children { get; set; } = [];
    }
}
