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
    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    /// <summary>
    /// Renders one recursive node in the project browser tree.
    /// </summary>
    public partial class ProjectBrowserNode : BloomReactiveComponentBase<ProjectBrowserNodeViewModel>
    {
        /// <summary>Describes the effective node state shared by rendering and accessible labels.</summary>
        /// <param name="VisibleChildren">The children visible under the current filter.</param>
        /// <param name="HasVisibleChildren">Whether any children remain visible.</param>
        /// <param name="CanToggle">Whether the user may change expansion.</param>
        /// <param name="IsExpanded">Whether the visible children are rendered.</param>
        private sealed record NodeState(IReadOnlyList<ProjectBrowserNodeViewModel> VisibleChildren, bool HasVisibleChildren, bool CanToggle, bool IsExpanded);

        /// <summary>
        /// Gets the node ViewModel required while rendering an assigned node.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when node rendering is attempted without an assigned ViewModel.
        /// </exception>
        private ProjectBrowserNodeViewModel RequiredViewModel =>
            this.ViewModel
            ?? throw new InvalidOperationException(
                $"{nameof(ProjectBrowserNode)} requires a {nameof(ProjectBrowserNodeViewModel)}.");

        /// <summary>
        /// Gets or sets the tree depth of this node.
        /// </summary>
        [Parameter]
        public int Depth { get; set; }

        /// <summary>
        /// Gets or sets the node selected by the owning project browser.
        /// </summary>
        [Parameter]
        public ProjectBrowserNodeViewModel SelectedNode { get; set; }

        /// <summary>
        /// Gets or sets the immutable presentation that identifies visible canonical nodes.
        /// </summary>
        [Parameter]
        public ProjectBrowserFilterPresentation FilterPresentation { get; set; } =
            ProjectBrowserFilterPresentation.Inactive;

        /// <summary>
        /// Gets or sets the callback invoked after the node is selected.
        /// </summary>
        [Parameter]
        public EventCallback<ProjectBrowserNodeViewModel> OnNodeSelected { get; set; }

        /// <summary>Gets or sets the callback invoked by the separate disclosure control.</summary>
        [Parameter]
        public EventCallback<ProjectBrowserNodeViewModel> OnNodeExpansionToggled { get; set; }

        /// <summary>
        /// Invokes the node selection callback for the current node.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SelectNodeAsync()
        {
            var viewModel = this.ViewModel;

            if (viewModel == null)
            {
                return;
            }

            if (viewModel.IsGroup)
            {
                await this.OnNodeExpansionToggled.InvokeAsync(viewModel);
            }
            else
            {
                await this.OnNodeSelected.InvokeAsync(viewModel);
            }
        }

        /// <summary>Invokes the disclosure callback without selecting this node.</summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ToggleNodeAsync()
        {
            var viewModel = this.ViewModel;

            if (viewModel != null)
            {
                await this.OnNodeExpansionToggled.InvokeAsync(viewModel);
            }
        }

        /// <summary>
        /// Gets the CSS classes for the node row.
        /// </summary>
        /// <returns>The CSS classes for the node row.</returns>
        private string GetNodeCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-project-browser-node__row",
                CssClassBuilder.When(
                    "mb-project-browser-node__row--selected",
                    ReferenceEquals(this.RequiredViewModel, this.SelectedNode)));

            return cssClass;
        }

        /// <summary>Gets a compact, labelled symbol for an indicator derived from actual model data.</summary>
        /// <param name="indicator">The semantic indicator label.</param>
        /// <returns>The established icon name, or null when the indicator should remain text.</returns>
        private static string GetIndicatorIcon(string indicator)
        {
            return indicator switch
            {
                "Imported" => "arrow-down-to-line",
                "Library" => "library",
                "Standard library" => "badge-check",
                "Metadata" => "tag",
                "Variation" => "git-branch",
                "Conjugated" => "rotate-cw",
                "Parallel" => "columns-2",
                _ => null
            };
        }

        /// <summary>
        /// Gets the indentation style for the current node depth.
        /// </summary>
        /// <returns>The indentation style for the current node depth.</returns>
        private string GetIndentStyle()
        {
            var safeDepth = Math.Max(0, this.Depth);
            return string.Create(CultureInfo.InvariantCulture, $"width: calc({safeDepth} * var(--spacing-16));");
        }

        /// <summary>
        /// Gets the tooltip text for the node.
        /// </summary>
        /// <returns>The tooltip text for the node.</returns>
        private string GetTooltip()
        {
            var viewModel = this.RequiredViewModel;
            if (viewModel.IsGroup)
            {
                return viewModel.DisplayName;
            }
            var suffix = viewModel.Presentation.Kind;

            if (!string.IsNullOrWhiteSpace(viewModel.QualifiedName))
            {
                return string.Create(CultureInfo.InvariantCulture, $"{viewModel.QualifiedName} - {suffix}");
            }

            if (!string.IsNullOrWhiteSpace(viewModel.ElementId))
            {
                return string.Create(CultureInfo.InvariantCulture, $"{viewModel.ElementId} - {suffix}");
            }

            return suffix;
        }

        /// <summary>
        /// Resolves the visible children and effective expansion used throughout this node render.
        /// </summary>
        /// <returns>
        /// The visible canonical children, whether any are visible, and whether they should be rendered.
        /// </returns>
        private NodeState GetNodePresentation()
        {
            var viewModel = this.RequiredViewModel;
            var filterPresentation = this.FilterPresentation ?? ProjectBrowserFilterPresentation.Inactive;
            var visibleChildren = filterPresentation.IsActive
                ? viewModel.Children.Where(filterPresentation.IsVisible).ToArray()
                : viewModel.Children;
            var hasVisibleChildren = visibleChildren.Count > 0;
            var isExpanded = hasVisibleChildren
                             && (filterPresentation.IsActive || viewModel.IsExpanded);

            return new(visibleChildren, hasVisibleChildren, hasVisibleChildren && !filterPresentation.IsActive,
                isExpanded);
        }

        /// <summary>Gets a stateful accessible label for the disclosure control.</summary>
        /// <param name="nodePresentation">The effective presentation used by the current render.</param>
        /// <returns>The action available for the node's children.</returns>
        private string GetDisclosureLabel(NodeState nodePresentation)
        {
            var action = nodePresentation.IsExpanded ? "Collapse" : "Expand";
            return $"{action} {this.RequiredViewModel.DisplayName}";
        }

        /// <summary>
        /// Gets the ARIA expanded value for the node.
        /// </summary>
        /// <param name="nodePresentation">The effective presentation used by the current render.</param>
        /// <returns>The ARIA expanded value for the node.</returns>
        private static string GetAriaExpanded(NodeState nodePresentation)
        {
            if (!nodePresentation.HasVisibleChildren)
            {
                return null;
            }

            return nodePresentation.IsExpanded.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// Gets the ARIA selected value for the node.
        /// </summary>
        /// <returns>The ARIA selected value for the node.</returns>
        private string GetAriaSelected()
        {
            return this.RequiredViewModel.IsGroup ? null : ReferenceEquals(this.RequiredViewModel, this.SelectedNode)
                .ToString()
                .ToLowerInvariant();
        }
    }
}
