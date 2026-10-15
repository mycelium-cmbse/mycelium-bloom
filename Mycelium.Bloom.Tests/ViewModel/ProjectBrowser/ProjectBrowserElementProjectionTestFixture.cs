// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserElementProjectionTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.ProjectBrowser
{
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Moq;

    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using SysML2.NET.Core.POCO.Core.Classifiers;
    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Kernel.Associations;
    using SysML2.NET.Core.POCO.Kernel.Connectors;
    using SysML2.NET.Core.POCO.Kernel.Interactions;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.Multiplicities;
    using SysML2.NET.Core.POCO.Kernel.Packages;
    using SysML2.NET.Core.POCO.Root.Annotations;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Attributes;
    using SysML2.NET.Core.POCO.Systems.Connections;
    using SysML2.NET.Core.POCO.Systems.DefinitionAndUsage;
    using SysML2.NET.Core.POCO.Systems.Flows;
    using SysML2.NET.Core.POCO.Systems.Parts;
    using SysML2.NET.Core.POCO.Systems.Ports;
    using SysML2.NET.Core.POCO.Systems.States;
    using SysML2.NET.Core.POCO.Systems.Views;

    using static Mycelium.Bloom.Tests.Common.ProjectBrowserNodeTestFactory;

    [TestFixture]
    public sealed class ProjectBrowserElementProjectionTestFixture
    {
        [Test]
        public async Task VerifyIncompleteNamesAndMultiplicityRetainCanonicalNodes()
        {
            var feature = new Mock<IFeature>();
            feature.SetupGet(element => element.ElementId).Returns("incomplete-feature");
            feature.SetupGet(element => element.name).Throws(new SysML2.NET.Exceptions.IncompleteModelException("Missing name"));
            feature.SetupGet(element => element.qualifiedName).Throws(new SysML2.NET.Exceptions.IncompleteModelException("Missing namespace"));
            feature.SetupGet(element => element.multiplicity).Throws(new SysML2.NET.Exceptions.IncompleteModelException("Missing bound"));
            var root = new Mock<INamespace>();
            root.SetupGet(element => element.ElementId).Returns("root");
            root.SetupGet(element => element.DeclaredName).Returns("Root");
            root.SetupGet(element => element.ownedElement).Returns([feature.Object]);
            var loader = new Mock<IModelLoaderService>();
            loader.Setup(service => service.LoadQuantitiesModel()).Returns(root.Object);
            var selection = new ContextAwareService();
            using var browser = new ProjectBrowserViewModel(loader.Object, selection);
            Assert.That(await browser.InitializeAsync(CancellationToken.None), Is.True);
            var node = browser.RootNodes.Single().Children.Single();
            Assert.That(node.DisplayName, Does.Contain("name unresolved"));
            Assert.That(node.Multiplicity, Is.Null);
            browser.SelectNode(node);
            Assert.That(selection.SelectedElement, Is.SameAs(feature.Object));
            Assert.That(new Mycelium.Bloom.ViewModel.DetailsPanel.DetailsPanelPresentation(feature.Object).ModelLinks
                .Single(link => link.Role == "Multiplicity").Label, Does.Contain("Unresolved"));
        }

        [TestCase(true, TypeArgs = [typeof(IPackage)])]
        [TestCase(true, TypeArgs = [typeof(IClassifier)])]
        [TestCase(true, TypeArgs = [typeof(IPartUsage)])]
        [TestCase(true, TypeArgs = [typeof(IPortUsage)])]
        [TestCase(true, TypeArgs = [typeof(IAttributeUsage)])]
        [TestCase(true, TypeArgs = [typeof(IStateUsage)])]
        [TestCase(true, TypeArgs = [typeof(IViewUsage)])]
        [TestCase(true, TypeArgs = [typeof(INamespace)])]
        [TestCase(false, TypeArgs = [typeof(IFeatureTyping)])]
        [TestCase(false, TypeArgs = [typeof(IMembership)])]
        [TestCase(false, TypeArgs = [typeof(IDocumentation)])]
        [TestCase(false, TypeArgs = [typeof(IMultiplicityRange)])]
        public void VerifyIsNavigable<TElement>(bool expected) where TElement : class, IElement
        {
            Assert.That(ProjectBrowserElementProjection.IsNavigable(Mock.Of<TElement>()), Is.EqualTo(expected));
        }

        [Test]
        public void VerifyUsageBelongsToFeatureHierarchy()
        {
            var part = new PartUsage();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(part, Is.InstanceOf<IFeature>());
                Assert.That(part, Is.InstanceOf<IUsage>());
                Assert.That(part, Is.Not.InstanceOf<IClassifier>());
            }
        }

        [TestCase(TypeArgs = [typeof(Association)])]
        [TestCase(TypeArgs = [typeof(Connector)])]
        [TestCase(TypeArgs = [typeof(BindingConnector)])]
        [TestCase(TypeArgs = [typeof(Succession)])]
        [TestCase(TypeArgs = [typeof(Flow)])]
        [TestCase(TypeArgs = [typeof(SuccessionFlow)])]
        [TestCase(TypeArgs = [typeof(ConnectionDefinition)])]
        [TestCase(TypeArgs = [typeof(FlowDefinition)])]
        [TestCase(TypeArgs = [typeof(ConnectionUsage)])]
        [TestCase(TypeArgs = [typeof(FlowUsage)])]
        [TestCase(TypeArgs = [typeof(SuccessionFlowUsage)])]
        public async Task VerifyOverlappingRelationshipsRemainNavigableAndFilterable<TElement>()
            where TElement : class, IElement, new()
        {
            var leaf = new PartUsage { ElementId = "leaf", DeclaredName = "Nested part" };
            var intermediate = CreateElement<Membership>("intermediate", null, leaf);
            var element = CreateElement<TElement>("connection", "Connection", intermediate);
            var wrapper = CreateElement<Membership>("wrapper", null, element);
            var root = CreateElement<Namespace>("root", "Root", wrapper);
            var loader = new Mock<IModelLoaderService>();
            loader.Setup(service => service.LoadQuantitiesModel()).Returns(root);
            var selection = new ContextAwareService();
            using var first = new ProjectBrowserViewModel(loader.Object, selection);
            using var second = new ProjectBrowserViewModel(loader.Object, selection);

            Assert.That(await first.InitializeAsync(CancellationToken.None), Is.True);
            Assert.That(await second.InitializeAsync(CancellationToken.None), Is.True);
            var node = first.RootNodes[0].Children.Single();
            var leafNode = node.Children.Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(element, Is.InstanceOf<IRelationship>());
                Assert.That(element, Is.InstanceOf<IClassifier>().Or.InstanceOf<IUsage>().Or.InstanceOf<IConnector>());
                Assert.That(ProjectBrowserElementProjection.IsNavigable(element), Is.True);
                Assert.That(node.SourceElement, Is.SameAs(element));
                Assert.That(leafNode.SourceElement, Is.SameAs(leaf));
                Assert.That(first.AvailableElementTypes, Does.Contain(typeof(TElement)));
            }

            first.ToggleElementTypeFilter(typeof(TElement));
            first.FilterText = "Connection";
            first.SelectNode(node);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.FilterPresentation.IsVisible(node), Is.True);
                Assert.That(first.FilterPresentation.IsVisible(first.RootNodes[0]), Is.True);
                Assert.That(first.FilterPresentation.IsVisible(leafNode), Is.False);
                Assert.That(selection.SelectedElement, Is.SameAs(element));
                Assert.That(second.SelectedNode, Is.SameAs(second.RootNodes[0]));
                Assert.That(second.SelectedElementTypes, Is.Empty);
                Assert.That(second.FilterText, Is.Empty);
            }

            selection.SelectedElement = intermediate;
            first.FocusElement(intermediate);
            Assert.That(first.SelectedNode, Is.SameAs(node));
            Assert.That(selection.SelectedElement, Is.SameAs(intermediate));

            first.ClearFilter();
            first.FocusElement(leaf);
            Assert.That(first.SelectedNode, Is.SameAs(leafNode));
            Assert.That(node.IsExpanded, Is.True);
            first.ToggleNode(node);
            Assert.That(node.IsExpanded, Is.False);
            first.FocusElement(new PartUsage { ElementId = "leaf" });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(node.IsExpanded, Is.True);
                Assert.That(first.SelectedNode, Is.SameAs(leafNode));
                Assert.That(second.RootNodes[0].Children.Single().IsExpanded, Is.False);
                Assert.That(selection.SelectedElement, Is.SameAs(intermediate));
                Assert.That(element.ownedElement.Single(), Is.SameAs(intermediate));
                Assert.That(wrapper.ownedElement.Single(), Is.SameAs(element));
            }
        }

        [Test]
        public async Task VerifyHiddenRelationshipsPreserveNestedDescendantsAndFiltering()
        {
            var port = new PortUsage { ElementId = "port", DeclaredName = "Power inlet" };
            var inner = CreateElement<Membership>("inner", null, port);
            var outer = CreateElement<Membership>("outer", null, inner);
            var part = CreateElement<PartUsage>("part", "Part", outer);
            var root = CreateElement<Namespace>("root", "Root", part);
            var loader = new Mock<IModelLoaderService>();
            loader.Setup(service => service.LoadQuantitiesModel()).Returns(root);
            var selection = new ContextAwareService();
            using var browser = new ProjectBrowserViewModel(loader.Object, selection);

            Assert.That(await browser.InitializeAsync(CancellationToken.None), Is.True);
            var partNode = browser.RootNodes[0].Children.Single();
            var portNode = partNode.Children.Single();
            browser.FilterText = "Power inlet";
            browser.FocusElement(port);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(portNode.SourceElement, Is.SameAs(port));
                Assert.That(browser.SelectedNode, Is.SameAs(portNode));
                Assert.That(partNode.IsExpanded, Is.True);
                Assert.That(browser.FilterPresentation.IsVisible(partNode), Is.True);
                Assert.That(browser.FilterPresentation.IsVisible(portNode), Is.True);
                Assert.That(part.ownedElement, Does.Contain(outer));
                Assert.That(outer.ownedElement, Does.Contain(inner));
                Assert.That(selection.SelectedElement, Is.Null);
            }

            browser.SelectNode(portNode);
            Assert.That(selection.SelectedElement, Is.SameAs(port));

            selection.SelectedElement = inner;
            browser.FocusElement(inner);
            Assert.That(browser.SelectedNode, Is.SameAs(partNode));
            Assert.That(selection.SelectedElement, Is.SameAs(inner));

            var containment = part.OwnedRelationship.Single();
            selection.SelectedElement = containment;
            browser.FocusElement(containment);
            Assert.That(browser.SelectedNode, Is.SameAs(partNode));
            Assert.That(selection.SelectedElement, Is.SameAs(containment));
        }

        [Test]
        public async Task VerifyHiddenFocusPreservesSharedSelectionAndIndependentBrowserState()
        {
            var documentation = new Documentation { ElementId = "documentation", Body = "Description" };
            var upper = new LiteralInteger { ElementId = "upper", Value = 4 };
            var multiplicity = CreateElement<MultiplicityRange>("multiplicity", null, upper);
            var part = CreateElement<PartUsage>("part", "Part", documentation, multiplicity);
            var root = CreateElement<Namespace>("root", "Root", part);
            var loader = new Mock<IModelLoaderService>();
            loader.Setup(service => service.LoadQuantitiesModel()).Returns(root);
            var selection = new ContextAwareService { SelectedElement = documentation };
            using var first = new ProjectBrowserViewModel(loader.Object, selection);
            using var second = new ProjectBrowserViewModel(loader.Object, selection);
            first.FocusElement(documentation);

            Assert.That(await first.InitializeAsync(CancellationToken.None), Is.True);
            Assert.That(await second.InitializeAsync(CancellationToken.None), Is.True);
            var firstPart = first.RootNodes[0].Children.Single();
            var secondPart = second.RootNodes[0].Children.Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.SelectedNode, Is.SameAs(firstPart));
                Assert.That(firstPart.Children, Is.Empty);
                Assert.That(firstPart.Multiplicity, Is.EqualTo("[4]"));
                Assert.That(selection.SelectedElement, Is.SameAs(documentation));
                Assert.That(second.SelectedNode, Is.SameAs(second.RootNodes[0]));
                Assert.That(firstPart, Is.Not.SameAs(secondPart));
                Assert.That(part.ownedElement, Has.Count.EqualTo(2));
            }

            first.FocusElement(new LiteralInteger { ElementId = "upper" });
            first.FilterText = "Part";
            first.ToggleNode(first.RootNodes[0]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.SelectedNode, Is.SameAs(firstPart));
                Assert.That(selection.SelectedElement, Is.SameAs(documentation));
                Assert.That(second.FilterText, Is.Empty);
                Assert.That(second.RootNodes[0].IsExpanded, Is.True);
            }
        }
    }
}
