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
    using ReactiveUI;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Exceptions;

    /// <summary>
    /// Represents a canonical model occurrence or a presentation-only kind header in the Browser tree.
    /// </summary>
    public sealed class ProjectBrowserNodeViewModel : ReactiveObject
    {
        /// <summary>
        /// A value indicating whether the node is expanded.
        /// </summary>
        private bool isExpanded;

        /// <summary>
        /// The hidden elements whose local navigation target is this node.
        /// </summary>
        private readonly IReadOnlyList<IElement> hiddenElements;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectBrowserNodeViewModel" /> class.
        /// </summary>
        /// <param name="id">The unique node identifier used by the project browser.</param>
        /// <param name="displayName">The display name shown for the node.</param>
        /// <param name="metadata">The SysML metadata associated with the node.</param>
        /// <param name="children">The child nodes built from the SysML owned element hierarchy.</param>
        /// <param name="membership">The membership for this occurrence, including an alias or variant.</param>
        /// <param name="isImported">Whether the SDK identifies this occurrence as imported.</param>
        /// <param name="importingNamespace">The namespace defining effective import visibility.</param>
        public ProjectBrowserNodeViewModel(
            string id,
            string displayName,
            ProjectBrowserNodeMetadata metadata,
            IReadOnlyList<ProjectBrowserNodeViewModel> children,
            IMembership membership = null,
            bool isImported = false,
            INamespace importingNamespace = null)
        {
            ArgumentNullException.ThrowIfNull(metadata);

            this.Id = id;
            this.DisplayName = displayName;
            this.ElementId = metadata.ElementId;
            this.QualifiedName = metadata.QualifiedName;
            this.ElementType = metadata.SourceElement.GetType();
            this.Presentation = ProjectBrowserElementPresentation.For(this.ElementType, metadata.IsModelRoot);
            this.Children = children;
            this.SourceElement = metadata.SourceElement;
            this.OccurrenceMembership = membership ?? this.SourceElement.owningMembership;
            this.Group = ProjectBrowserElementProjection.GetGroup(this.SourceElement, membership);
            this.hiddenElements = metadata.HiddenElements;
            this.UiDisplayLabel = metadata.UiDisplayLabel;
            this.Row = ProjectBrowserRowPresentation.For(this.SourceElement, membership, isImported, importingNamespace);
            try
            {
                this.Multiplicity = ElementPresentation.GetMultiplicity(this.SourceElement);
            }
            catch (IncompleteModelException)
            {
                this.Multiplicity = null;
            }
        }

        /// <summary>Initializes a presentation-only kind header with no SDK element or model identity.</summary>
        /// <param name="id">The occurrence identifier within this browser.</param>
        /// <param name="label">The reviewed kind-group label.</param>
        /// <param name="children">The canonical model rows in the group.</param>
        public ProjectBrowserNodeViewModel(string id, string label, IReadOnlyList<ProjectBrowserNodeViewModel> children)
        {
            this.Id = id;
            this.DisplayName = label;
            this.Children = children;
            this.hiddenElements = [];
            this.IsGroup = true;
        }

        /// <summary>Gets whether this is a presentation-only kind header, never a selectable SDK element.</summary>
        public bool IsGroup { get; }

        /// <summary>Gets the reviewed group for this occurrence, or null for a structural row.</summary>
        public string Group { get; }

        /// <summary>Gets the available model facets for this row; headers have no model facets.</summary>
        public ProjectBrowserRowPresentation Row { get; }

        /// <summary>Gets the canonical membership for this occurrence, including an alias or imported member.</summary>
        public IMembership OccurrenceMembership { get; }

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
        /// Gets the concrete SysML POCO type represented by the node, or null on a kind header.
        /// </summary>
        public Type ElementType { get; }

        /// <summary>
        /// Gets the semantic kind, supported stereotype and icon for the row.
        /// </summary>
        public ProjectBrowserElementPresentation Presentation { get; }

        /// <summary>
        /// Gets a UI-provided fallback label, when this node represents an unnamed model root.
        /// </summary>
        public string UiDisplayLabel { get; }

        /// <summary>
        /// Gets the child nodes built from the SysML owned element hierarchy.
        /// </summary>
        public IReadOnlyList<ProjectBrowserNodeViewModel> Children { get; }

        /// <summary>
        /// Gets the source SysML element represented by the node, or null on a kind header.
        /// </summary>
        public IElement SourceElement { get; }

        /// <summary>
        /// Gets the resolved multiplicity annotation, when available.
        /// </summary>
        public string Multiplicity { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the node is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get => this.isExpanded;
            set => this.RaiseAndSetIfChanged(ref this.isExpanded, value);
        }

        /// <summary>
        /// Gets a value indicating whether the node has child nodes.
        /// </summary>
        public bool HasChildren => this.Children.Count > 0;

        /// <summary>
        /// Finds the first depth-first path from this node to a canonical model identity in its subtree.
        /// </summary>
        /// <param name="element">The canonical model element to locate.</param>
        /// <returns>The path from this node to the target, or an empty path when the identity is absent.</returns>
        public IReadOnlyList<ProjectBrowserNodeViewModel> FindPathTo(IElement element)
        {
            ArgumentNullException.ThrowIfNull(element);

            if (MatchesIdentity(this.SourceElement, element)
                || this.hiddenElements.Any(hidden => MatchesIdentity(hidden, element)))
            {
                return [this];
            }

            var childPath = this.Children
                .Select(childNode => childNode.FindPathTo(element))
                .FirstOrDefault(path => path.Count > 0);

            return childPath is null
                ? Array.Empty<ProjectBrowserNodeViewModel>()
                : [this, .. childPath];
        }

        /// <summary>
        /// Compares canonical references or available model identifiers.
        /// </summary>
        private static bool MatchesIdentity(IElement candidate, IElement target)
        {
            return candidate != null && (ReferenceEquals(candidate, target)
                   || (!string.IsNullOrWhiteSpace(target.ElementId)
                       && string.Equals(candidate.ElementId, target.ElementId, StringComparison.Ordinal)));
        }
    }
}
