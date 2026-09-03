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
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    using Moq;

    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Parts;

    /// <summary>
    /// Creates Project Browser nodes for component tests.
    /// </summary>
    internal static class ProjectBrowserNodeTestFactory
    {
        /// <summary>
        /// The SDK containment property that stores an element's owned relationships.
        /// </summary>
        private static readonly PropertyInfo OwnedRelationshipProperty = GetRequiredProperty(
            "SysML2.NET.Core.POCO.Root.Elements.IContainedElement",
            "OwnedRelationship");

        /// <summary>
        /// The SDK containment property that stores a relationship's owned elements.
        /// </summary>
        private static readonly PropertyInfo OwnedRelatedElementProperty = GetRequiredProperty(
            "SysML2.NET.Core.POCO.Root.Elements.IContainedRelationship",
            "OwnedRelatedElement");

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
                    new Namespace
                    {
                        ElementId = elementId,
                        DeclaredName = displayName
                    }),
                children);
        }

        /// <summary>
        /// Creates and loads the canonical tree used by recursive filter presentation scenarios.
        /// </summary>
        /// <returns>The loaded real Project Browser ViewModel.</returns>
        internal static async Task<ProjectBrowserViewModel> CreateFilterTreeViewModelAsync()
        {
            var hiddenDescendant = CreateElement<PartDefinition>("hidden", "Hidden descendant");
            var matchingElement = CreateElement<PartUsage>("needle", "Needle", hiddenDescendant);
            var matchingBranch = CreateElement<Namespace>("branch", "Branch", matchingElement);
            var sibling = CreateElement<Namespace>("sibling", "Sibling");
            var root = CreateElement<Namespace>("root", "Root", matchingBranch, sibling);
            var modelLoaderService = new Mock<IModelLoaderService>(MockBehavior.Strict);
            modelLoaderService.Setup(x => x.LoadQuantitiesModel()).Returns(root);
            var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object, new ContextAwareService());

            await viewModel.InitializeAsync(CancellationToken.None);

            return viewModel;
        }

        /// <summary>
        /// Creates one concrete SDK element for a real Project Browser tree-building scenario.
        /// </summary>
        /// <typeparam name="TElement">The concrete SDK element type.</typeparam>
        /// <param name="elementId">The source element identifier.</param>
        /// <param name="displayName">The source element display name.</param>
        /// <param name="children">The canonical owned elements.</param>
        /// <returns>The configured SDK element.</returns>
        internal static TElement CreateElement<TElement>(
            string elementId,
            string displayName,
            params IElement[] children)
            where TElement : class, IElement, new()
        {
            var element = new TElement
            {
                ElementId = elementId,
                DeclaredName = displayName
            };

            foreach (var child in children)
            {
                AttachOwnedElement(element, child);
            }

            return element;
        }

        /// <summary>
        /// Adds an element through the SDK containment relationship that backs <see cref="IElement.ownedElement"/>.
        /// </summary>
        /// <param name="owner">The owning element.</param>
        /// <param name="child">The owned element.</param>
        private static void AttachOwnedElement(IElement owner, IElement child)
        {
            var membership = new OwningMembership();
            var ownedElements = (ICollection<IElement>)OwnedRelatedElementProperty.GetValue(membership)!;
            var ownedRelationships = (ICollection<IRelationship>)OwnedRelationshipProperty.GetValue(owner)!;
            ownedElements.Add(child);
            ownedRelationships.Add(membership);
        }

        /// <summary>
        /// Resolves a required internal SDK containment property used to assemble test models.
        /// </summary>
        /// <param name="typeName">The declaring type's full name.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The required property.</returns>
        private static PropertyInfo GetRequiredProperty(string typeName, string propertyName)
        {
            var declaringType = typeof(IElement).Assembly.GetType(typeName, throwOnError: true)!;

            return declaringType.GetProperty(propertyName)
                   ?? throw new InvalidOperationException($"Property '{typeName}.{propertyName}' was not found.");
        }
    }
}
