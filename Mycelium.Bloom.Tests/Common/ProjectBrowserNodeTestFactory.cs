// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserNodeTestFactory.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Common
{
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using SysML2.NET.Core.POCO.Root.Namespaces;

    /// <summary>
    /// Creates Project Browser nodes for component tests.
    /// </summary>
    internal static class ProjectBrowserNodeTestFactory
    {
        /// <summary>
        /// Creates a namespace node with default metadata values derived from the node values.
        /// </summary>
        /// <param name="nodeId">The node identifier.</param>
        /// <param name="displayName">The node display name.</param>
        /// <param name="children">The child nodes.</param>
        /// <returns>The created namespace node.</returns>
        internal static ProjectBrowserNodeViewModel CreateNamespaceNode(
            string nodeId,
            string displayName,
            params ProjectBrowserNodeViewModel[] children)
        {
            return CreateNamespaceNode(
                nodeId,
                displayName,
                nodeId,
                displayName,
                children);
        }

        /// <summary>
        /// Creates a namespace node with explicit metadata values.
        /// </summary>
        /// <param name="nodeId">The node identifier.</param>
        /// <param name="displayName">The node display name.</param>
        /// <param name="elementId">The SysML element identifier.</param>
        /// <param name="qualifiedName">The SysML qualified name.</param>
        /// <param name="children">The child nodes.</param>
        /// <returns>The created namespace node.</returns>
        internal static ProjectBrowserNodeViewModel CreateNamespaceNode(
            string nodeId,
            string displayName,
            string elementId,
            string qualifiedName,
            params ProjectBrowserNodeViewModel[] children)
        {
            return new ProjectBrowserNodeViewModel(
                nodeId,
                displayName,
                new ProjectBrowserNodeMetadata(
                    elementId,
                    qualifiedName,
                    "Namespace",
                    SysmlModelElementKind.Namespace,
                    new Namespace()),
                children);
        }
    }
}
