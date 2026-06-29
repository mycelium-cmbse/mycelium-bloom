// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserNodeViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.ProjectBrowser
{
    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>
    /// Represents one SysML element node in the project browser tree.
    /// </summary>
    public sealed class ProjectBrowserNodeViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectBrowserNodeViewModel" /> class.
        /// </summary>
        /// <param name="id">The unique node identifier used by the project browser.</param>
        /// <param name="elementId">The SysML element identifier, when available.</param>
        /// <param name="displayName">The display name shown for the node.</param>
        /// <param name="qualifiedName">The qualified SysML name, when available.</param>
        /// <param name="runtimeTypeName">The runtime SysML POCO type name.</param>
        /// <param name="elementKind">The broad SysML element kind.</param>
        /// <param name="children">The child nodes built from the SysML owned element hierarchy.</param>
        /// <param name="sourceElement">The source SysML element represented by the node.</param>
        public ProjectBrowserNodeViewModel(
            string id,
            string elementId,
            string displayName,
            string qualifiedName,
            string runtimeTypeName,
            ProjectBrowserElementKind elementKind,
            IReadOnlyList<ProjectBrowserNodeViewModel> children,
            IElement sourceElement)
        {
            this.Id = id;
            this.ElementId = elementId;
            this.DisplayName = displayName;
            this.QualifiedName = qualifiedName;
            this.RuntimeTypeName = runtimeTypeName;
            this.ElementKind = elementKind;
            this.Children = children;
            this.SourceElement = sourceElement;
        }

        /// <summary>
        /// Gets the unique node identifier used by the project browser.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the SysML element identifier, when available.
        /// </summary>
        public string ElementId { get; }

        /// <summary>
        /// Gets the display name shown for the node.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the qualified SysML name, when available.
        /// </summary>
        public string QualifiedName { get; }

        /// <summary>
        /// Gets the runtime SysML POCO type name.
        /// </summary>
        public string RuntimeTypeName { get; }

        /// <summary>
        /// Gets the broad SysML element kind.
        /// </summary>
        public ProjectBrowserElementKind ElementKind { get; }

        /// <summary>
        /// Gets the child nodes built from the SysML owned element hierarchy.
        /// </summary>
        public IReadOnlyList<ProjectBrowserNodeViewModel> Children { get; }

        /// <summary>
        /// Gets the source SysML element represented by the node.
        /// </summary>
        public IElement SourceElement { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the node is expanded.
        /// </summary>
        public bool IsExpanded { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the node is selected.
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// Gets a value indicating whether the node has child nodes.
        /// </summary>
        public bool HasChildren => this.Children.Count > 0;
    }
}
