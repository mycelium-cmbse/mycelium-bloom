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
        public IProjectBrowserViewModel ViewModel { get; set; }

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

        private async Task SelectNodeAsync()
        {
            if (this.Node.HasChildren)
            {
                this.ViewModel.ToggleNode(this.Node);
            }

            this.ViewModel.SelectNode(this.Node);

            await this.OnNodeSelected.InvokeAsync(this.Node);
            await this.OnStateChanged.InvokeAsync();
        }

        private string GetStereotype()
        {
            return this.GetTypeLabel();
        }

        private string GetElementColor()
        {
            var color = this.Node.ElementKind switch
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

            if (!string.IsNullOrWhiteSpace(this.Node.QualifiedName))
            {
                return string.Create(CultureInfo.InvariantCulture, $"{this.Node.QualifiedName} - {suffix}");
            }

            if (!string.IsNullOrWhiteSpace(this.Node.ElementId))
            {
                return string.Create(CultureInfo.InvariantCulture, $"{this.Node.ElementId} - {suffix}");
            }

            return suffix;
        }

        private string GetTypeLabel()
        {
            if (!string.IsNullOrWhiteSpace(this.Node.RuntimeTypeName))
            {
                return this.Node.RuntimeTypeName;
            }

            if (this.Node.ElementKind != ProjectBrowserElementKind.Unknown)
            {
                return this.Node.ElementKind.ToString();
            }

            return ProjectBrowserElementKind.Unknown.ToString();
        }

        private string GetAriaExpanded()
        {
            if (!this.Node.HasChildren)
            {
                return null;
            }

            return this.Node.IsExpanded.ToString().ToLowerInvariant();
        }

        private string GetAriaSelected()
        {
            return this.Node.IsSelected.ToString().ToLowerInvariant();
        }
    }
}
