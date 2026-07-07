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

        private async Task SelectNodeAsync()
        {
            if (this.ViewModel == null)
            {
                return;
            }

            await this.OnNodeSelected.InvokeAsync(this.ViewModel);
        }

        private string GetNodeCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-project-browser-node__row",
                CssClassBuilder.When("mb-project-browser-node__row--selected", this.ViewModel.IsSelected));

            return cssClass;
        }

        private string GetNodeStyle()
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"--mb-project-browser-node-element-color: {this.GetElementColor()};");
        }

        private string GetIndentStyle()
        {
            var safeDepth = Math.Max(0, this.Depth);
            var width = safeDepth * 16;

            return string.Create(CultureInfo.InvariantCulture, $"width: {width}px;");
        }

        private string GetStereotype()
        {
            return this.GetTypeLabel();
        }

        private string GetElementColor()
        {
            var color = this.ViewModel.ElementKind switch
            {
                ProjectBrowserElementKind.Namespace => "var(--mb-color-sysml-structure-header)",
                ProjectBrowserElementKind.Import => "var(--mb-color-sysml-allocations-header)",
                ProjectBrowserElementKind.Membership => "var(--mb-color-sysml-metadata-header)",
                ProjectBrowserElementKind.Relationship => "var(--mb-color-sysml-connections-header)",
                ProjectBrowserElementKind.Definition => "var(--mb-color-sysml-attributes-header)",
                ProjectBrowserElementKind.Usage => "var(--mb-color-sysml-behavior-header)",
                ProjectBrowserElementKind.Feature => "var(--mb-color-sysml-requirements-header)",
                ProjectBrowserElementKind.Type => "var(--mb-color-sysml-verification-header)",
                ProjectBrowserElementKind.Annotation => "var(--mb-color-info-500)",
                _ => "var(--mb-color-neutral-600)"
            };

            return color;
        }

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

        private string GetTypeLabel()
        {
            if (!string.IsNullOrWhiteSpace(this.ViewModel.RuntimeTypeName))
            {
                return this.ViewModel.RuntimeTypeName;
            }

            if (this.ViewModel.ElementKind != ProjectBrowserElementKind.Unknown)
            {
                return this.ViewModel.ElementKind.ToString();
            }

            return ProjectBrowserElementKind.Unknown.ToString();
        }

        private string GetAriaExpanded()
        {
            if (!this.ViewModel.HasChildren)
            {
                return null;
            }

            return this.ViewModel.IsExpanded.ToString().ToLowerInvariant();
        }

        private string GetAriaSelected()
        {
            return this.ViewModel.IsSelected.ToString().ToLowerInvariant();
        }
    }
}
