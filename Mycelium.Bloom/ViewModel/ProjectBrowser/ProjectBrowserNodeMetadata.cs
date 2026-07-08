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
    using Mycelium.Bloom.Model.Enum;

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
        /// <param name="runtimeTypeName">The runtime SysML POCO type name.</param>
        /// <param name="elementKind">The broad SysML element kind.</param>
        /// <param name="sourceElement">The source SysML element represented by the node.</param>
        public ProjectBrowserNodeMetadata(
            string elementId,
            string qualifiedName,
            string runtimeTypeName,
            SysmlModelElementKind elementKind,
            IElement sourceElement)
        {
            this.ElementId = elementId;
            this.QualifiedName = qualifiedName;
            this.RuntimeTypeName = runtimeTypeName;
            this.ElementKind = elementKind;
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
        /// Gets the runtime SysML POCO type name.
        /// </summary>
        public string RuntimeTypeName { get; }

        /// <summary>
        /// Gets the broad SysML element kind.
        /// </summary>
        public SysmlModelElementKind ElementKind { get; }

        /// <summary>
        /// Gets the source SysML element represented by the node.
        /// </summary>
        public IElement SourceElement { get; }
    }
}
