// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserElementPresentationTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.ProjectBrowser
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.ViewModel.DetailsPanel;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;
    using static Mycelium.Bloom.Tests.Common.ProjectBrowserNodeTestFactory;
    using SysML2.NET.Core.POCO.Kernel.Associations;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.Packages;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Attributes;
    using SysML2.NET.Core.POCO.Systems.Connections;
    using SysML2.NET.Core.POCO.Systems.Flows;
    using SysML2.NET.Core.POCO.Systems.Parts;
    using SysML2.NET.Core.POCO.Systems.Ports;
    using SysML2.NET.Core.POCO.Systems.Views;

    /// <summary>
    /// Verifies semantic row presentation and SDK interface precedence.
    /// </summary>
    [TestFixture]
    public sealed class ProjectBrowserElementPresentationTestFixture
    {
        /// <summary>
        /// Verifies supported kinds have their intended labels and established icons.
        /// </summary>
        /// <param name="type">The SDK type or interface.</param>
        /// <param name="kind">The human-readable kind.</param>
        /// <param name="stereotype">The supported inline notation.</param>
        /// <param name="icon">The established tree icon.</param>
        [TestCase(typeof(LibraryPackage), "Library package", "«library package»", "folder")]
        [TestCase(typeof(IPackage), "Package", "«package»", "folder")]
        [TestCase(typeof(PartDefinition), "Part definition", "«part def»", "box")]
        [TestCase(typeof(PartUsage), "Part", "«part»", "component")]
        [TestCase(typeof(IPartUsage), "Part", "«part»", "component")]
        [TestCase(typeof(PortUsage), "Port", "«port»", "port")]
        [TestCase(typeof(AttributeUsage), "Attribute", "«attribute»", null)]
        [TestCase(typeof(AttributeDefinition), "Attribute definition", "«attribute def»", null)]
        [TestCase(typeof(ViewUsage), "View", "«view»", "network")]
        [TestCase(typeof(ViewDefinition), "View definition", "«view def»", null)]
        [TestCase(typeof(Association), "Association", "«association»", null)]
        [TestCase(typeof(ConnectionDefinition), "Connection definition", "«connection def»", null)]
        [TestCase(typeof(ConnectionUsage), "Connection", "«connection»", null)]
        [TestCase(typeof(FlowDefinition), "Flow definition", "«flow def»", null)]
        [TestCase(typeof(FlowUsage), "Flow", "«flow»", null)]
        [TestCase(typeof(SuccessionFlowUsage), "Succession flow", "«succession flow»", null)]
        [TestCase(typeof(FeatureChainExpression), "Feature chain", "«feature chain»", null)]
        [TestCase(typeof(FeatureReferenceExpression), "Feature reference", "«feature reference»", null)]
        [TestCase(typeof(OperatorExpression), "Operator expression", "«operator expression»", null)]
        public void VerifyFor(Type type, string kind, string stereotype, string icon)
        {
            var presentation = ProjectBrowserElementPresentation.For(type);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(presentation.Kind, Is.EqualTo(kind));
                Assert.That(presentation.Stereotype, Is.EqualTo(stereotype));
                Assert.That(presentation.IconName, Is.EqualTo(icon));
                Assert.That(presentation.HasLeadingStereotype, Is.EqualTo(icon is "folder" or "box"));
            }
        }

        /// <summary>
        /// Verifies unrecognized implementations have a neutral semantic fallback.
        /// </summary>
        [Test]
        public void VerifyForUnknownImplementation()
        {
            var element = Mock.Of<IElement>();
            var presentation = ProjectBrowserElementPresentation.For(element.GetType());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(presentation.Kind, Is.EqualTo("Element"));
                Assert.That(presentation.UnnamedLabel, Is.EqualTo("Element"));
                Assert.That(presentation.Stereotype, Is.EqualTo("«element»"));
                Assert.That(presentation.IconName, Is.Null);
            }
        }

        /// <summary>
        /// Verifies an unnamed root is presented as a model while retaining its identity.
        /// </summary>
        /// <returns>The asynchronous test operation.</returns>
        [Test]
        public async Task VerifyUnnamedRootRetainsCanonicalIdentity()
        {
            var nested = CreateElement<Namespace>("nested", null);
            var root = CreateElement<Namespace>("root", null, nested);
            var loader = new Mock<IModelLoaderService>();
            loader.Setup(service => service.LoadQuantitiesModel()).Returns(root);
            var selection = new ContextAwareService();
            using var browser = new ProjectBrowserViewModel(loader.Object, selection);
            using var details = new DetailsPanelViewModel(selection);

            Assert.That(await browser.InitializeAsync(CancellationToken.None), Is.True);
            var node = browser.RootNodes[0];
            browser.SelectNode(node);
            var identity = details.Presentation.Sections.Single(section => section.Title == "Identity")
                .Properties.ToDictionary(property => property.Key, property => property.Value);
            var nestedNode = node.Children.Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(node.DisplayName, Is.EqualTo("Model"));
                Assert.That(node.SourceElement, Is.SameAs(root));
                Assert.That(selection.SelectedElement, Is.SameAs(root));
                Assert.That(browser.SelectedNode, Is.SameAs(node));
                Assert.That(root.DeclaredName, Is.Null);
                Assert.That(node.Presentation.IsModelRoot, Is.True);
                Assert.That(node.Presentation.Kind, Is.EqualTo("Namespace"));
                Assert.That(node.Presentation.IconName, Is.EqualTo("network"));
                Assert.That(nestedNode.DisplayName, Is.EqualTo("Namespace"));
                Assert.That(nestedNode.SourceElement, Is.SameAs(nested));
                Assert.That(nestedNode.Presentation.IsModelRoot, Is.False);
                Assert.That(identity["Declared name"], Is.EqualTo("Not declared"));
                Assert.That(details.Presentation.DisplayName, Is.EqualTo("Namespace"));
                Assert.That(details.Presentation.AdvancedProperties
                    .Single(property => property.Key == "Model kind").Value, Is.EqualTo("Namespace"));
                Assert.That(details.Presentation.AdvancedProperties
                    .Single(property => property.Key == "Element ID").Value, Is.EqualTo(root.ElementId));
            }

            details.Inspect(nested);
            details.GoBack();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(selection.SelectedElement, Is.SameAs(root));
                Assert.That(details.Presentation.DisplayName, Is.EqualTo("Namespace"));
            }

            browser.SelectNode(nestedNode);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(details.Presentation.DisplayName, Is.EqualTo("Namespace"));
            }
        }

        /// <summary>
        /// Verifies that a named model root keeps its SDK name rather than receiving the UI fallback.
        /// </summary>
        /// <returns>The asynchronous test operation.</returns>
        [Test]
        public async Task VerifyNamedRootPreservesSdkName()
        {
            var root = new Namespace { ElementId = "named-root", DeclaredName = "Model Context" };
            var loader = new Mock<IModelLoaderService>();
            loader.Setup(service => service.LoadQuantitiesModel()).Returns(root);
            using var browser = new ProjectBrowserViewModel(loader.Object, new ContextAwareService());

            Assert.That(await browser.InitializeAsync(CancellationToken.None), Is.True);
            var node = browser.RootNodes[0];
            browser.SelectNode(node);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(node.DisplayName, Is.EqualTo("Model Context"));
                Assert.That(node.Presentation.IsModelRoot, Is.True);
                Assert.That(node.Presentation.IconName, Is.EqualTo("network"));
                Assert.That(node.UiDisplayLabel, Is.Null);
            }
        }

        /// <summary>
        /// Verifies an effective SDK name on the loaded root is not replaced with the UI fallback.
        /// </summary>
        /// <returns>The asynchronous test operation.</returns>
        [Test]
        public async Task VerifyEffectiveRootNamePreservesSdkName()
        {
            var root = new Mock<INamespace>();
            root.SetupGet(element => element.ElementId).Returns("effective-root");
            root.SetupGet(element => element.DeclaredName).Returns((string)null);
            root.SetupGet(element => element.DeclaredShortName).Returns((string)null);
            root.SetupGet(element => element.name).Returns("SDK model name");
            root.SetupGet(element => element.shortName).Returns((string)null);
            root.SetupGet(element => element.qualifiedName).Returns((string)null);
            root.SetupGet(element => element.ownedElement).Returns([]);
            var loader = new Mock<IModelLoaderService>();
            loader.Setup(service => service.LoadQuantitiesModel()).Returns(root.Object);
            using var browser = new ProjectBrowserViewModel(loader.Object, new ContextAwareService());

            Assert.That(await browser.InitializeAsync(CancellationToken.None), Is.True);
            var node = browser.RootNodes[0];
            browser.SelectNode(node);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(node.DisplayName, Is.EqualTo("SDK model name"));
                Assert.That(node.SourceElement, Is.SameAs(root.Object));
                Assert.That(node.UiDisplayLabel, Is.Null);
            }
        }
    }
}