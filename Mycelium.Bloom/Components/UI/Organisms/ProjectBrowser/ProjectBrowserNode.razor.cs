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
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    /// <summary>
    /// Renders one recursive node in the project browser tree.
    /// </summary>
    public partial class ProjectBrowserNode : ComponentBase
    {
        /// <summary>
        /// Gets or sets the project browser node view model.
        /// </summary>
        [Parameter]
        public ProjectBrowserNodeViewModel ViewModel { get; set; }

        /// <summary>
        /// Gets or sets the tree depth of this node.
        /// </summary>
        [Parameter]
        public int Depth { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked after the node is selected.
        /// </summary>
        [Parameter]
        public EventCallback<ProjectBrowserNodeViewModel> OnNodeSelected { get; set; }

        /// <summary>
        /// Invokes the node selection callback for the current node.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SelectNodeAsync()
        {
            if (this.ViewModel == null)
            {
                return;
            }

            await this.OnNodeSelected.InvokeAsync(this.ViewModel);
        }

        /// <summary>
        /// Gets the CSS classes for the node row.
        /// </summary>
        /// <returns>The CSS classes for the node row.</returns>
        private string GetNodeCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-project-browser-node__row",
                CssClassBuilder.When("mb-project-browser-node__row--selected", this.ViewModel.IsSelected));

            return cssClass;
        }

        /// <summary>
        /// Gets the inline style variables for the node row.
        /// </summary>
        /// <returns>The inline style variables for the node row.</returns>
        private string GetNodeStyle()
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"--mb-project-browser-node-element-color: {this.GetElementColor()};");
        }

        /// <summary>
        /// Gets the indentation style for the current node depth.
        /// </summary>
        /// <returns>The indentation style for the current node depth.</returns>
        private string GetIndentStyle()
        {
            var safeDepth = Math.Max(0, this.Depth);
            var width = safeDepth * 16;

            return string.Create(CultureInfo.InvariantCulture, $"width: {width}px;");
        }

        /// <summary>
        /// Gets the stereotype label displayed for the node.
        /// </summary>
        /// <returns>The stereotype label displayed for the node.</returns>
        private string GetStereotype()
        {
            return this.GetTypeLabel();
        }

        /// <summary>
        /// Gets the design token color for the node element kind.
        /// </summary>
        /// <returns>The design token color for the node element kind.</returns>
        private string GetElementColor()
        {
            return this.ViewModel.ElementKind.ToColorToken();
        }

        /// <summary>
        /// Gets the tooltip text for the node.
        /// </summary>
        /// <returns>The tooltip text for the node.</returns>
        private string GetTooltip()
        {
            var suffix = this.GetTypeLabel();

            if (!string.IsNullOrWhiteSpace(this.ViewModel.QualifiedName))
            {
                return string.Create(CultureInfo.InvariantCulture, $"{this.ViewModel.QualifiedName} - {suffix}");
            }

            if (!string.IsNullOrWhiteSpace(this.ViewModel.ElementId))
            {
                return string.Create(CultureInfo.InvariantCulture, $"{this.ViewModel.ElementId} - {suffix}");
            }

            return suffix;
        }

        /// <summary>
        /// Gets the most specific type label available for the node.
        /// </summary>
        /// <returns>The most specific type label available for the node.</returns>
        private string GetTypeLabel()
        {
            if (!string.IsNullOrWhiteSpace(this.ViewModel.RuntimeTypeName))
            {
                return this.ViewModel.RuntimeTypeName;
            }

            if (this.ViewModel.ElementKind != SysmlModelElementKind.Unknown)
            {
                return this.ViewModel.ElementKind.ToString();
            }

            return SysmlModelElementKind.Unknown.ToString();
        }

        /// <summary>
        /// Gets the ARIA expanded value for the node.
        /// </summary>
        /// <returns>The ARIA expanded value for the node.</returns>
        private string GetAriaExpanded()
        {
            if (!this.ViewModel.HasChildren)
            {
                return null;
            }

            return this.ViewModel.IsExpanded.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// Gets the ARIA selected value for the node.
        /// </summary>
        /// <returns>The ARIA selected value for the node.</returns>
        private string GetAriaSelected()
        {
            return this.ViewModel.IsSelected.ToString().ToLowerInvariant();
        }
    }
}
