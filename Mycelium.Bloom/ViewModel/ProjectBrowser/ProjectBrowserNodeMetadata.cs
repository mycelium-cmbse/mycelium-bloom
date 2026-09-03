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
        public ProjectBrowserNodeMetadata(
            string elementId,
            string qualifiedName,
            IElement sourceElement)
        {
            ArgumentNullException.ThrowIfNull(sourceElement);

            this.ElementId = elementId;
            this.QualifiedName = qualifiedName;
            this.SourceElement = sourceElement;
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
    }
}
