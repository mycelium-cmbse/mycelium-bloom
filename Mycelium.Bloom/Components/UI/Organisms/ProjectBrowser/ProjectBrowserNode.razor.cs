// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserNode.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser
{
    using System.Globalization;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    /// <summary>
    /// Renders one recursive node in the project browser tree.
    /// </summary>
    public partial class ProjectBrowserNode : ComponentBase
    {
        /// <summary>
        /// Gets or sets the node to render.
        /// </summary>
        [Parameter]
        public ProjectBrowserNodeViewModel Node { get; set; }

        /// <summary>
        /// Gets or sets the project browser view model.
        /// </summary>
        [Parameter]
        public ProjectBrowserViewModel ViewModel { get; set; }

        /// <summary>
        /// Gets or sets the tree depth of this node.
        /// </summary>
        [Parameter]
        public int Depth { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked after expand or collapse state changes.
        /// </summary>
        [Parameter]
        public EventCallback OnStateChanged { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked after the node is selected.
        /// </summary>
        [Parameter]
        public EventCallback<ProjectBrowserNodeViewModel> OnNodeSelected { get; set; }

        private async Task ToggleNodeAsync()
        {
            this.ViewModel.ToggleNode(this.Node);
            await this.OnStateChanged.InvokeAsync();
        }

        private async Task SelectNodeAsync()
        {
            this.ViewModel.SelectNode(this.Node);
            await this.OnNodeSelected.InvokeAsync(this.Node);
        }

        private string GetRowCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-project-browser-node__row",
                CssClassBuilder.When("mb-project-browser-node__row--selected", this.Node.IsSelected));

            return cssClass;
        }

        private string GetRowStyle()
        {
            var depth = Math.Max(0, this.Depth);
            var indent = depth * 16;
            var style = string.Create(CultureInfo.InvariantCulture, $"--mb-project-browser-node-indent: {indent}px;");

            return style;
        }

        private string GetKindLabel()
        {
            var kindName = this.Node.ElementKind.ToString();
            var label = kindName.Length <= 3 ? kindName : kindName[..3];

            return label.ToUpperInvariant();
        }

        private string GetTitle()
        {
            if (!string.IsNullOrWhiteSpace(this.Node.QualifiedName))
            {
                return this.Node.QualifiedName;
            }

            if (!string.IsNullOrWhiteSpace(this.Node.ElementId))
            {
                return this.Node.ElementId;
            }

            return this.Node.RuntimeTypeName;
        }

        private string GetToggleTitle()
        {
            var action = this.Node.IsExpanded ? "Collapse" : "Expand";
            var title = string.Create(CultureInfo.InvariantCulture, $"{action} {this.Node.DisplayName}");

            return title;
        }

        private string GetAriaExpanded()
        {
            if (!this.Node.HasChildren)
            {
                return null;
            }

            return this.Node.IsExpanded.ToString().ToLowerInvariant();
        }
    }
}
