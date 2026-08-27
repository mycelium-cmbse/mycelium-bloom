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
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Moq;

    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using SysML2.NET.Core.POCO.Root.Elements;
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

        /// <summary>
        /// Creates and loads the canonical tree used by recursive filter presentation scenarios.
        /// </summary>
        /// <returns>The loaded real Project Browser ViewModel.</returns>
        internal static async Task<ProjectBrowserViewModel> CreateFilterTreeViewModelAsync()
        {
            var hiddenDescendant = CreateNamespaceElement("hidden", "Hidden descendant");
            var matchingElement = CreateNamespaceElement("needle", "Needle", hiddenDescendant.Object);
            var matchingBranch = CreateNamespaceElement("branch", "Branch", matchingElement.Object);
            var sibling = CreateNamespaceElement("sibling", "Sibling");
            var root = CreateNamespaceElement("root", "Root", matchingBranch.Object, sibling.Object);
            var modelLoaderService = new Mock<IModelLoaderService>(MockBehavior.Strict);
            modelLoaderService.Setup(x => x.LoadQuantitiesModel()).Returns(root.Object);
            var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object, new ContextAwareService());

            await viewModel.InitializeAsync(CancellationToken.None);

            return viewModel;
        }

        /// <summary>
        /// Creates one SDK namespace element for a real Project Browser tree-building scenario.
        /// </summary>
        /// <param name="elementId">The source element identifier.</param>
        /// <param name="displayName">The source element display name.</param>
        /// <param name="children">The canonical owned elements.</param>
        /// <returns>The configured SDK namespace mock.</returns>
        private static Mock<INamespace> CreateNamespaceElement(
            string elementId,
            string displayName,
            params IElement[] children)
        {
            var element = new Mock<INamespace>();
            element.SetupGet(x => x.ElementId).Returns(elementId);
            element.SetupGet(x => x.DeclaredName).Returns(displayName);
            element.SetupGet(x => x.ownedElement).Returns(children.ToList());

            return element;
        }
    }
}
