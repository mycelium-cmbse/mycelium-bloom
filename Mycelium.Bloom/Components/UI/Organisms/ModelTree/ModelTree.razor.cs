// ------------------------------------------------------------------------------------------------
// <copyright file="ModelTree.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.ModelTree
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Molecules.TreeNode;
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Reusable Bloom model tree used to browse hierarchical SysML model elements.
    /// </summary>
    public partial class ModelTree : ComponentBase
    {
        private readonly HashSet<string> expandedItemIds = [];

        /// <summary>
        /// Gets or sets the model tree items.
        /// </summary>
        [Parameter]
        public IReadOnlyList<ModelTreeItem> Items { get; set; } = [];

        /// <summary>
        /// Gets or sets the active item identifier.
        /// </summary>
        [Parameter]
        public string ActiveItemId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the active item change callback.
        /// </summary>
        [Parameter]
        public EventCallback<string> ActiveItemIdChanged { get; set; }

        /// <summary>
        /// Gets or sets whether all parent nodes should be expanded by default.
        /// </summary>
        [Parameter]
        public bool ExpandAllByDefault { get; set; }

        /// <summary>
        /// Gets or sets additional CSS classes.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets unmatched attributes passed to the tree container.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Initializes the expanded state when requested.
        /// </summary>
        protected override void OnParametersSet()
        {
            if (this.ExpandAllByDefault)
            {
                this.ExpandItems(this.Items);
            }
        }

        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-model-tree",
                this.Class);

            return cssClass;
        }

        /// <summary>
        /// Renders the tree items recursively.
        /// </summary>
        /// <param name="items">The items to render.</param>
        /// <param name="indentLevel">The current indent level.</param>
        /// <returns>The rendered item fragment.</returns>
        private RenderFragment RenderItems(IReadOnlyList<ModelTreeItem> items, int indentLevel)
        {
            return builder =>
            {
                var sequence = 0;

                foreach (var item in items)
                {
                    var hasChildren = item.Children.Count > 0;
                    var isExpanded = this.IsExpanded(item);

                    builder.OpenComponent<TreeNode>(sequence++);
                    builder.AddAttribute(sequence++, "Title", item.Title);
                    builder.AddAttribute(sequence++, "Stereotype", item.Stereotype);
                    builder.AddAttribute(sequence++, "IsActive", this.IsActive(item));
                    builder.AddAttribute(sequence++, "HasChildren", hasChildren);
                    builder.AddAttribute(sequence++, "IsExpanded", isExpanded);
                    builder.AddAttribute(sequence++, "HasComment", item.HasComment);
                    builder.AddAttribute(sequence++, "IsModified", item.IsModified);
                    builder.AddAttribute(sequence++, "IndentLevel", indentLevel);
                    builder.AddAttribute(sequence++, "Ownership", item.Ownership);
                    builder.AddAttribute(sequence++, "OwnershipColor", item.OwnershipColor);
                    builder.AddAttribute(sequence++, "ElementColor", item.ElementColor);

                    builder.AddAttribute(sequence++, "OnClick",
                        EventCallback.Factory.Create<MouseEventArgs>(this, () => this.SelectItemAsync(item)));

                    builder.CloseComponent();

                    if (hasChildren && isExpanded)
                    {
                        builder.AddContent(sequence++, this.RenderItems(item.Children, indentLevel + 1));
                    }
                }
            };
        }

        /// <summary>
        /// Selects a tree item and toggles it when it has children.
        /// </summary>
        /// <param name="item">The selected item.</param>
        private async Task SelectItemAsync(ModelTreeItem item)
        {
            if (item.Children.Count > 0)
            {
                this.ToggleItem(item);
            }

            this.ActiveItemId = item.Id;

            await this.ActiveItemIdChanged.InvokeAsync(item.Id);
        }

        /// <summary>
        /// Checks whether the provided item is active.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>A value indicating whether the item is active.</returns>
        private bool IsActive(ModelTreeItem item)
        {
            var isActive = string.Equals(item.Id, this.ActiveItemId, StringComparison.Ordinal);

            return isActive;
        }

        /// <summary>
        /// Checks whether the provided item is expanded.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>A value indicating whether the item is expanded.</returns>
        private bool IsExpanded(ModelTreeItem item)
        {
            var isExpanded = this.expandedItemIds.Contains(item.Id);

            return isExpanded;
        }

        /// <summary>
        /// Toggles the expanded state of the provided item.
        /// </summary>
        /// <param name="item">The item to toggle.</param>
        private void ToggleItem(ModelTreeItem item)
        {
            if (this.expandedItemIds.Contains(item.Id))
            {
                this.expandedItemIds.Remove(item.Id);
            }
            else
            {
                this.expandedItemIds.Add(item.Id);
            }
        }

        /// <summary>
        /// Expands all items that contain children.
        /// </summary>
        /// <param name="items">The items to expand.</param>
        private void ExpandItems(IReadOnlyList<ModelTreeItem> items)
        {
            foreach (var item in items)
            {
                if (item.Children.Count > 0)
                {
                    this.expandedItemIds.Add(item.Id);
                    this.ExpandItems(item.Children);
                }
            }
        }
    }
}
