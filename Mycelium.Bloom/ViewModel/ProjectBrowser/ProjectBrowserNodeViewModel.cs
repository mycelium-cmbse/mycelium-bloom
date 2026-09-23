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
    using System.Collections.Immutable;
    using ReactiveUI;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>
    /// Represents one SysML element node in the project browser tree.
    /// </summary>
    public sealed class ProjectBrowserNodeViewModel : ReactiveObject
    {
        /// <summary>
        /// A value indicating whether the node is expanded.
        /// </summary>
        private bool isExpanded;

        /// <summary>Holds the current display name.</summary>
        private string displayName;

        /// <summary>Holds the current qualified name.</summary>
        private string qualifiedName;

        /// <summary>Holds the current canonical SDK identity.</summary>
        private IElement sourceElement;

        /// <summary>Holds the immutable projection of one branch's membership.</summary>
        private IReadOnlyList<ProjectBrowserNodeViewModel> children;

        /// <summary>Indicates whether this branch has materialized its children.</summary>
        private bool areChildrenLoaded = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectBrowserNodeViewModel" /> class.
        /// </summary>
        /// <param name="id">The unique node identifier used by the project browser.</param>
        /// <param name="displayName">The display name shown for the node.</param>
        /// <param name="metadata">The SysML metadata associated with the node.</param>
        /// <param name="children">The child nodes built from the SysML owned element hierarchy.</param>
        public ProjectBrowserNodeViewModel(
            string id,
            string displayName,
            ProjectBrowserNodeMetadata metadata,
            IReadOnlyList<ProjectBrowserNodeViewModel> children)
        {
            ArgumentNullException.ThrowIfNull(metadata);

            this.Id = id;
            this.DisplayName = displayName;
            this.ElementId = metadata.ElementId;
            this.QualifiedName = metadata.QualifiedName;
            this.Children = children.ToImmutableArray();
            this.SourceElement = metadata.SourceElement;
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
        public string DisplayName
        {
            get => this.displayName;
            private set => this.RaiseAndSetIfChanged(ref this.displayName, value);
        }

        /// <summary>
        /// Gets the qualified SysML name, when available.
        /// </summary>
        public string QualifiedName
        {
            get => this.qualifiedName;
            private set => this.RaiseAndSetIfChanged(ref this.qualifiedName, value);
        }

        /// <summary>
        /// Gets the concrete SysML POCO type represented by the node.
        /// </summary>
        public Type ElementType => this.SourceElement.GetType();

        /// <summary>
        /// Gets the child nodes built from the SysML owned element hierarchy.
        /// </summary>
        public IReadOnlyList<ProjectBrowserNodeViewModel> Children
        {
            get => this.children;
            private set => this.RaiseAndSetIfChanged(ref this.children, value);
        }

        /// <summary>
        /// Gets the source SysML element represented by the node.
        /// </summary>
        public IElement SourceElement
        {
            get => this.sourceElement;
            private set => this.RaiseAndSetIfChanged(ref this.sourceElement, value);
        }

        /// <summary>Gets whether this branch has materialized all children rather than only filter match paths.</summary>
        public bool AreChildrenLoaded
        {
            get => this.areChildrenLoaded;
            internal set => this.RaiseAndSetIfChanged(ref this.areChildrenLoaded, value);
        }

        /// <summary>Gets whether the cached child membership requires reconciliation.</summary>
        internal bool ChildrenInvalidated { get; set; }

        /// <summary>Gets the materialized containment parent.</summary>
        internal ProjectBrowserNodeViewModel Parent { get; set; }

        /// <summary>Routes expansion through the owner of a live tree.</summary>
        internal Action<ProjectBrowserNodeViewModel, bool> ExpansionRequested { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the node is expanded.
        /// </summary>
        /// <remarks>The owning browser coordinates expansion to keep child caches, subscriptions and presentation publication consistent.</remarks>
        public bool IsExpanded
        {
            get => this.isExpanded;
            set
            {
                if (this.ExpansionRequested is { } request)
                {
                    request(this, value);
                }
                else
                {
                    this.SetExpanded(value);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the node has child nodes.
        /// </summary>
        public bool HasChildren => this.AreChildrenLoaded && !this.ChildrenInvalidated
            ? this.Children.Count > 0
            : ProjectBrowserViewModel.GetChildElements(this.SourceElement).Any();

        /// <summary>Applies expansion after the owning browser has reconciled child state.</summary>
        /// <param name="value">Whether the branch is expanded.</param>
        internal void SetExpanded(bool value)
        {
            this.RaiseAndSetIfChanged(ref this.isExpanded, value);
        }

        /// <summary>Updates metadata while preserving browser identity and expansion.</summary>
        /// <param name="element">The current canonical SDK element.</param>
        /// <param name="displayName">The current display name.</param>
        internal void UpdateElement(IElement element, string displayName)
        {
            this.SourceElement = element;
            this.DisplayName = displayName;
            this.QualifiedName = element.qualifiedName;
        }

        /// <summary>Publishes a stable child snapshot after owner-side reconciliation.</summary>
        /// <param name="children">The ordered child identities.</param>
        internal void SetChildren(IReadOnlyList<ProjectBrowserNodeViewModel> children)
        {
            this.Children = children;
            this.AreChildrenLoaded = true;
            this.ChildrenInvalidated = false;
        }

        /// <summary>Retains a matching path child without claiming the entire branch has been loaded.</summary>
        /// <param name="child">The matching child identity.</param>
        internal void RetainChild(ProjectBrowserNodeViewModel child)
        {
            if (!this.Children.Contains(child))
            {
                this.Children = this.Children.Append(child).ToImmutableArray();
            }
        }

        /// <summary>Removes stale membership without retrieving the remaining children.</summary>
        /// <param name="child">The deleted or moved child identity.</param>
        internal void ForgetChild(ProjectBrowserNodeViewModel child)
        {
            if (!this.Children.Contains(child))
            {
                return;
            }
            this.Children = this.Children.Where(candidate => !ReferenceEquals(candidate, child)).ToImmutableArray();
            this.ChildrenInvalidated = true;
        }

        /// <summary>
        /// Finds the first depth-first path from this node to a canonical model identity in its subtree.
        /// </summary>
        /// <param name="element">The canonical model element to locate.</param>
        /// <returns>The path from this node to the target, or an empty path when the identity is absent.</returns>
        public IReadOnlyList<ProjectBrowserNodeViewModel> FindPathTo(IElement element)
        {
            ArgumentNullException.ThrowIfNull(element);

            if (ReferenceEquals(this.SourceElement, element)
                || (!string.IsNullOrWhiteSpace(element.ElementId)
                    && string.Equals(this.ElementId, element.ElementId, StringComparison.Ordinal)))
            {
                return [this];
            }

            var childPath = this.Children
                .Where(childNode => this.ExpansionRequested is null
                    || (childNode.ExpansionRequested != null && ReferenceEquals(childNode.Parent, this)))
                .Select(childNode => childNode.FindPathTo(element))
                .FirstOrDefault(path => path.Count > 0);

            return childPath is null
                ? Array.Empty<ProjectBrowserNodeViewModel>()
                : [this, .. childPath];
        }
    }
}
