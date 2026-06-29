// ------------------------------------------------------------------------------------------------
// <copyright file="TreeNode.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Molecules.TreeNode
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Reusable Bloom tree node used inside model browser and workspace trees.
    /// </summary>
    public partial class TreeNode : ComponentBase
    {
        /// <summary>
        /// Gets or sets the visible node title.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the SysML stereotype text.
        /// </summary>
        [Parameter]
        public string Stereotype { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the node is currently active.
        /// </summary>
        [Parameter]
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets whether the node has child nodes.
        /// </summary>
        [Parameter]
        public bool HasChildren { get; set; }

        /// <summary>
        /// Gets or sets whether the node children are expanded.
        /// </summary>
        [Parameter]
        public bool IsExpanded { get; set; }

        /// <summary>
        /// Gets or sets whether the node has comments.
        /// </summary>
        [Parameter]
        public bool HasComment { get; set; }

        /// <summary>
        /// Gets or sets whether the node has unsaved or remote modifications.
        /// </summary>
        [Parameter]
        public bool IsModified { get; set; }

        /// <summary>
        /// Gets or sets the node indentation level.
        /// </summary>
        [Parameter]
        public int IndentLevel { get; set; }

        /// <summary>
        /// Gets or sets the ownership state of the node.
        /// </summary>
        [Parameter]
        public TreeNodeOwnership Ownership { get; set; } = TreeNodeOwnership.None;

        /// <summary>
        /// Gets or sets the ownership color used for the stripe and owned element icon.
        /// </summary>
        [Parameter]
        public string OwnershipColor { get; set; } = "var(--mb-color-ownership-aocs)";

        /// <summary>
        /// Gets or sets the default element icon color.
        /// </summary>
        [Parameter]
        public string ElementColor { get; set; } = "var(--mb-color-neutral-600)";

        /// <summary>
        /// Gets or sets the click callback.
        /// </summary>
        [Parameter]
        public EventCallback<MouseEventArgs> OnClick { get; set; }

        /// <summary>
        /// Gets or sets additional CSS classes.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets unmatched attributes passed to the tree node element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-tree-node",
                CssClassBuilder.When("mb-tree-node--active", this.IsActive),
                CssClassBuilder.When("mb-tree-node--owned", this.Ownership == TreeNodeOwnership.Mine),
                CssClassBuilder.When("mb-tree-node--other-owned", this.Ownership == TreeNodeOwnership.Others),
                this.Class);

            return cssClass;
        }

        private string GetStyle()
        {
            var elementColor = this.Ownership == TreeNodeOwnership.Mine
                ? this.OwnershipColor
                : this.ElementColor;

            var style = $"--mb-tree-node-ownership-color: {this.OwnershipColor}; --mb-tree-node-element-color: {elementColor};";

            return style;
        }

        private string GetIndentStyle()
        {
            var safeIndentLevel = Math.Max(0, this.IndentLevel);
            var indent = safeIndentLevel * 16;
            var style = $"width: {indent}px;";

            return style;
        }
    }
}
