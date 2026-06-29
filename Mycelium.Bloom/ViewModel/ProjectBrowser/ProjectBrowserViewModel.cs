// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.ProjectBrowser
{
    using System.Globalization;

    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;

    /// <summary>
    /// Provides tree state and tree-building logic for the project browser.
    /// </summary>
    public sealed class ProjectBrowserViewModel
    {
        private readonly HashSet<string> nodeIds = new(StringComparer.Ordinal);

        /// <summary>
        /// Gets the root nodes displayed by the project browser.
        /// </summary>
        public IReadOnlyList<ProjectBrowserNodeViewModel> RootNodes { get; private set; } = [];

        /// <summary>
        /// Gets the currently selected node.
        /// </summary>
        public ProjectBrowserNodeViewModel SelectedNode { get; private set; }

        /// <summary>
        /// Initializes the project browser tree from the provided SysML namespace.
        /// </summary>
        /// <param name="model">The loaded SysML namespace model.</param>
        public void Initialize(INamespace model)
        {
            ArgumentNullException.ThrowIfNull(model);

            this.nodeIds.Clear();
            this.SelectedNode = null;
            this.RootNodes = [this.BuildNode(model, "root")];
        }

        /// <summary>
        /// Toggles the expanded state of the provided node.
        /// </summary>
        /// <param name="node">The node to expand or collapse.</param>
        public void ToggleNode(ProjectBrowserNodeViewModel node)
        {
            ArgumentNullException.ThrowIfNull(node);

            if (node.HasChildren)
            {
                node.IsExpanded = !node.IsExpanded;
            }
        }

        /// <summary>
        /// Selects the provided node and clears the previous selection.
        /// </summary>
        /// <param name="node">The node to select.</param>
        public void SelectNode(ProjectBrowserNodeViewModel node)
        {
            ArgumentNullException.ThrowIfNull(node);

            if (this.SelectedNode != null)
            {
                this.SelectedNode.IsSelected = false;
            }

            node.IsSelected = true;
            this.SelectedNode = node;
        }

        private ProjectBrowserNodeViewModel BuildNode(IElement element, string fallbackId)
        {
            var runtimeTypeName = element.GetType().Name;
            var elementId = this.ToDisplayString(element.ElementId);
            var nodeId = this.CreateUniqueNodeId(string.IsNullOrWhiteSpace(elementId) ? fallbackId : elementId);
            var children = this.BuildChildren(element, nodeId);
            var displayName = this.GetDisplayName(element, runtimeTypeName);
            var qualifiedName = this.ToDisplayString(element.qualifiedName);
            var elementKind = this.GetElementKind(runtimeTypeName);

            var node = new ProjectBrowserNodeViewModel(
                nodeId,
                elementId,
                displayName,
                qualifiedName,
                runtimeTypeName,
                elementKind,
                children,
                element);

            return node;
        }

        private IReadOnlyList<ProjectBrowserNodeViewModel> BuildChildren(IElement element, string parentNodeId)
        {
            var children = new List<ProjectBrowserNodeViewModel>();

            if (element.ownedElement == null)
            {
                return children;
            }

            var index = 0;

            foreach (var childElement in element.ownedElement)
            {
                if (childElement != null)
                {
                    children.Add(this.BuildNode(childElement, string.Create(
                        CultureInfo.InvariantCulture,
                        $"{parentNodeId}/{index}")));
                }

                index++;
            }

            return children;
        }

        private string CreateUniqueNodeId(string preferredId)
        {
            if (this.nodeIds.Add(preferredId))
            {
                return preferredId;
            }

            var suffix = 2;
            var candidateId = string.Create(CultureInfo.InvariantCulture, $"{preferredId}-{suffix}");

            while (!this.nodeIds.Add(candidateId))
            {
                suffix++;
                candidateId = string.Create(CultureInfo.InvariantCulture, $"{preferredId}-{suffix}");
            }

            return candidateId;
        }

        private string GetDisplayName(IElement element, string runtimeTypeName)
        {
            var declaredName = this.ToDisplayString(element.DeclaredName);

            if (!string.IsNullOrWhiteSpace(declaredName))
            {
                return declaredName;
            }

            var name = this.ToDisplayString(element.name);

            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            var qualifiedName = this.ToDisplayString(element.qualifiedName);

            if (!string.IsNullOrWhiteSpace(qualifiedName))
            {
                return qualifiedName;
            }

            return runtimeTypeName;
        }

        private ProjectBrowserElementKind GetElementKind(string runtimeTypeName)
        {
            if (runtimeTypeName.EndsWith("Namespace", StringComparison.Ordinal))
            {
                return ProjectBrowserElementKind.Namespace;
            }

            if (runtimeTypeName.Contains("Import", StringComparison.Ordinal))
            {
                return ProjectBrowserElementKind.Import;
            }

            if (runtimeTypeName.Contains("Membership", StringComparison.Ordinal))
            {
                return ProjectBrowserElementKind.Membership;
            }

            if (runtimeTypeName.Contains("Relationship", StringComparison.Ordinal))
            {
                return ProjectBrowserElementKind.Relationship;
            }

            if (runtimeTypeName.EndsWith("Definition", StringComparison.Ordinal))
            {
                return ProjectBrowserElementKind.Definition;
            }

            if (runtimeTypeName.EndsWith("Usage", StringComparison.Ordinal))
            {
                return ProjectBrowserElementKind.Usage;
            }

            if (runtimeTypeName.Contains("Feature", StringComparison.Ordinal))
            {
                return ProjectBrowserElementKind.Feature;
            }

            if (runtimeTypeName.Contains("Type", StringComparison.Ordinal))
            {
                return ProjectBrowserElementKind.Type;
            }

            if (runtimeTypeName.Contains("Annotation", StringComparison.Ordinal))
            {
                return ProjectBrowserElementKind.Annotation;
            }

            return ProjectBrowserElementKind.Unknown;
        }

        private string ToDisplayString(object value)
        {
            var displayString = Convert.ToString(value, CultureInfo.InvariantCulture);

            return displayString ?? string.Empty;
        }
    }
}
