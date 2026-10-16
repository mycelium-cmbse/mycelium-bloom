// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserNodeMetadata.cs" company="Starion Group S.A.">
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
    /// Provides SysML metadata associated with a project browser node.
    /// </summary>
    public sealed class ProjectBrowserNodeMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectBrowserNodeMetadata" /> class.
        /// </summary>
        /// <param name="elementId">The SysML element identifier, when available.</param>
        /// <param name="qualifiedName">The qualified SysML name, when available.</param>
        /// <param name="sourceElement">The source SysML element represented by the node.</param>
        /// <param name="hiddenElements">Elements whose nearest navigable ancestor is this node.</param>
        /// <param name="isModelRoot">Whether this element is the root returned by the model loader.</param>
        /// <param name="uiDisplayLabel">The UI fallback label for an unnamed model root.</param>
        public ProjectBrowserNodeMetadata(
            string elementId,
            string qualifiedName,
            IElement sourceElement,
            IReadOnlyList<IElement> hiddenElements = null,
            bool isModelRoot = false,
            string uiDisplayLabel = null)
        {
            ArgumentNullException.ThrowIfNull(sourceElement);

            this.ElementId = elementId;
            this.QualifiedName = qualifiedName;
            this.SourceElement = sourceElement;
            this.HiddenElements = hiddenElements?.ToArray() ?? [];
            this.IsModelRoot = isModelRoot;
            this.UiDisplayLabel = uiDisplayLabel;
        }

        /// <summary>
        /// Gets the SysML element identifier, when available.
        /// </summary>
        public string ElementId { get; }

        /// <summary>
        /// Gets the qualified SysML name, when available.
        /// </summary>
        public string QualifiedName { get; }

        /// <summary>
        /// Gets the source SysML element represented by the node.
        /// </summary>
        public IElement SourceElement { get; }

        /// <summary>
        /// Gets the hidden elements associated with this navigable ancestor.
        /// </summary>
        public IReadOnlyList<IElement> HiddenElements { get; }

        /// <summary>
        /// Gets whether this element is the root returned by the model loader.
        /// </summary>
        public bool IsModelRoot { get; }

        /// <summary>
        /// Gets a UI fallback label that is distinct from SDK naming properties.
        /// </summary>
        public string UiDisplayLabel { get; }
    }
}
