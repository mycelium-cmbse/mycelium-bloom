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
    using System.Threading;
    using System.Threading.Tasks;

    using Moq;

    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

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
        [TestCase(typeof(LibraryPackage), "Package", "«package»", "folder")]
        [TestCase(typeof(IPackage), "Package", "«package»", "folder")]
        [TestCase(typeof(PartDefinition), "Part definition", "«part def»", "box")]
        [TestCase(typeof(PartUsage), "Part", "«part»", "component")]
        [TestCase(typeof(IPartUsage), "Part", "«part»", "component")]
        [TestCase(typeof(PortUsage), "Port", "«port»", "port")]
        [TestCase(typeof(AttributeUsage), "Attribute", "«attribute»", null)]
        [TestCase(typeof(AttributeDefinition), "Attribute definition", null, null)]
        [TestCase(typeof(ViewUsage), "View", null, "network")]
        [TestCase(typeof(ViewDefinition), "View definition", null, null)]
        [TestCase(typeof(Association), "Association", null, null)]
        [TestCase(typeof(ConnectionDefinition), "Connection definition", null, null)]
        [TestCase(typeof(ConnectionUsage), "Connection", null, null)]
        [TestCase(typeof(FlowDefinition), "Flow definition", null, null)]
        [TestCase(typeof(FlowUsage), "Flow", null, null)]
        [TestCase(typeof(SuccessionFlowUsage), "Succession flow", null, null)]
        [TestCase(typeof(FeatureChainExpression), "Feature chain", null, null)]
        [TestCase(typeof(FeatureReferenceExpression), "Feature reference", null, null)]
        [TestCase(typeof(OperatorExpression), "Operator expression", null, null)]
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

            Assert.That(presentation.Kind, Is.EqualTo("Element"));
            Assert.That(presentation.UnnamedLabel, Is.EqualTo("Element"));
            Assert.That(presentation.Stereotype, Is.Null);
            Assert.That(presentation.IconName, Is.Null);
        }

        /// <summary>
        /// Verifies an unnamed root is presented as a model while retaining its identity.
        /// </summary>
        /// <returns>The asynchronous test operation.</returns>
        [Test]
        public async Task VerifyUnnamedRootRetainsCanonicalIdentity()
        {
            var root = new Namespace { ElementId = "root" };
            var loader = new Mock<IModelLoaderService>();
            loader.Setup(service => service.LoadQuantitiesModel()).Returns(root);
            var selection = new ContextAwareService();
            using var browser = new ProjectBrowserViewModel(loader.Object, selection);

            Assert.That(await browser.InitializeAsync(CancellationToken.None), Is.True);
            var node = browser.RootNodes[0];
            browser.SelectNode(node);
            browser.FocusElement(root);

            Assert.That(node.DisplayName, Is.EqualTo("Model"));
            Assert.That(node.SourceElement, Is.SameAs(root));
            Assert.That(selection.SelectedElement, Is.SameAs(root));
            Assert.That(browser.SelectedNode, Is.SameAs(node));
            Assert.That(root.DeclaredName, Is.Null);
        }
    }
}
