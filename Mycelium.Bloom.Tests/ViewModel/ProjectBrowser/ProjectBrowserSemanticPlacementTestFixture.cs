// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserSemanticPlacementTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.ProjectBrowser
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;
    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Kernel.Connectors;
    using SysML2.NET.Core.POCO.Kernel.FeatureValues;
    using SysML2.NET.Core.POCO.Kernel.Functions;
    using SysML2.NET.Core.POCO.Root.Annotations;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Actions;
    using SysML2.NET.Core.POCO.Systems.Calculations;
    using SysML2.NET.Core.POCO.Systems.Allocations;
    using SysML2.NET.Core.POCO.Systems.Connections;
    using SysML2.NET.Core.POCO.Systems.Constraints;
    using SysML2.NET.Core.POCO.Systems.DefinitionAndUsage;
    using SysML2.NET.Core.POCO.Systems.Enumerations;
    using SysML2.NET.Core.POCO.Systems.Flows;
    using SysML2.NET.Core.POCO.Systems.Interfaces;
    using SysML2.NET.Core.POCO.Systems.Metadata;
    using SysML2.NET.Core.POCO.Systems.Parts;
    using SysML2.NET.Core.POCO.Systems.Requirements;
    using SysML2.NET.Core.POCO.Systems.States;
    using static Mycelium.Bloom.Tests.Common.ProjectBrowserNodeTestFactory;

    /// <summary>Verifies the review's placement policy independently of the old tree assertions.</summary>
    [TestFixture]
    public sealed class ProjectBrowserSemanticPlacementTestFixture
    {
        /// <summary>Verifies known sequencing relationships cannot reach the broad connector/usage fallback.</summary>
        [TestCase(TypeArgs = [typeof(Succession)])]
        [TestCase(TypeArgs = [typeof(SuccessionAsUsage)])]
        [TestCase(TypeArgs = [typeof(SysML2.NET.Core.POCO.Kernel.Interactions.SuccessionFlow)])]
        public void VerifyGenericSuccessionsAreOwnerInformation<TElement>() where TElement : class, IElement, new()
        {
            var element = new TElement();
            Assert.That(element, Is.InstanceOf<ISuccession>());
            Assert.That(ProjectBrowserElementProjection.IsExcluded(element), Is.True);
            Assert.That(ProjectBrowserElementProjection.IsNavigable(element), Is.False);
            Assert.That(ProjectBrowserElementProjection.IsNavigable(new SuccessionFlowUsage()), Is.True);
        }

        /// <summary>Preserves the canonical initial-state semantics and hidden succession navigation.</summary>
        [Test]
        public async Task VerifyHiddenStateSuccessionRevealsOwnerAndPreservesInitialMarker()
        {
            var model = Mycelium.Bloom.Tests.Common.BrowserAcceptanceModelTestFactory.CreateStates();
            var loader = new Mock<IModelLoaderService>();
            loader.Setup(service => service.LoadQuantitiesModel()).Returns(model.Owner);
            var selection = new ContextAwareService();
            using var browser = new ProjectBrowserViewModel(loader.Object, selection);
            Assert.That(await browser.InitializeAsync(CancellationToken.None), Is.True);
            var owner = browser.RootNodes.Single();
            Assert.That(owner.Children.Select(node => node.SourceElement), Is.EqualTo(new[] { model.First, model.Initial }));
            Assert.That(owner.Children[1].Row.Indicators, Does.Contain("Initial"));
            Assert.That(owner.Children[1].SourceElement, Is.SameAs(model.Initial));
            Assert.That(model.Succession.sourceFeature, Is.SameAs(model.Owner.entryAction));
            Assert.That(model.Succession.targetFeature.Single(), Is.SameAs(model.Initial));
            selection.SelectedElement = model.Succession;
            browser.FocusElement(model.Succession);
            Assert.That(browser.SelectedNode, Is.SameAs(owner));
            Assert.That(selection.SelectedElement, Is.SameAs(model.Succession));
            browser.FocusElement(model.Succession.ownedElement.First());
            Assert.That(browser.SelectedNode, Is.SameAs(owner));
        }

        [TestCase(TypeArgs = [typeof(PartDefinition)])]
        [TestCase(TypeArgs = [typeof(PartUsage)])]
        [TestCase(TypeArgs = [typeof(ActionUsage)])]
        [TestCase(TypeArgs = [typeof(StateUsage)])]
        [TestCase(TypeArgs = [typeof(ConstraintUsage)])]
        [TestCase(TypeArgs = [typeof(AssertConstraintUsage)])]
        [TestCase(TypeArgs = [typeof(CalculationUsage)])]
        [TestCase(TypeArgs = [typeof(MetadataDefinition)])]
        [TestCase(TypeArgs = [typeof(EnumerationDefinition)])]
        [TestCase(TypeArgs = [typeof(EnumerationUsage)])]
        public void VerifyOrdinaryDomainElementsKeepTheirStructuralRows<TElement>()
            where TElement : class, IElement, new()
        {
            Assert.That(ProjectBrowserElementProjection.IsNavigable(new TElement()), Is.True);
        }

        [TestCase("Connections", TypeArgs = [typeof(ConnectionDefinition)])]
        [TestCase("Connections", TypeArgs = [typeof(ConnectionUsage)])]
        [TestCase("Connections", TypeArgs = [typeof(BindingConnector)])]
        [TestCase("Interfaces", TypeArgs = [typeof(InterfaceDefinition)])]
        [TestCase("Interfaces", TypeArgs = [typeof(InterfaceUsage)])]
        [TestCase("Flows", TypeArgs = [typeof(FlowDefinition)])]
        [TestCase("Flows", TypeArgs = [typeof(FlowUsage)])]
        [TestCase("Flows", TypeArgs = [typeof(SuccessionFlowUsage)])]
        [TestCase("Allocations", TypeArgs = [typeof(AllocationDefinition)])]
        [TestCase("Allocations", TypeArgs = [typeof(AllocationUsage)])]
        [TestCase("Satisfies", TypeArgs = [typeof(SatisfyRequirementUsage)])]
        public async Task VerifyGroupsPreserveSelectionFilterRevealAndIndependentState<TElement>(string label)
            where TElement : class, IElement, new()
        {
            var element = CreateElement<TElement>("grouped", "Needle");
            var root = CreateElement<Namespace>("root", "Model", element);
            var loader = new Mock<IModelLoaderService>();
            loader.Setup(service => service.LoadQuantitiesModel()).Returns(root);
            var selection = new ContextAwareService();
            using var first = new ProjectBrowserViewModel(loader.Object, selection);
            using var second = new ProjectBrowserViewModel(loader.Object, selection);
            Assert.That(await first.InitializeAsync(CancellationToken.None), Is.True);
            Assert.That(await second.InitializeAsync(CancellationToken.None), Is.True);
            var group = first.RootNodes[0].Children.Single();
            var node = group.Children.Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(group.IsGroup, Is.True);
                Assert.That(group.DisplayName, Is.EqualTo(label));
                Assert.That(group.SourceElement, Is.Null);
                Assert.That(group.ElementId, Is.Null);
                Assert.That(group.ElementType, Is.Null);
                Assert.That(group.IsExpanded, Is.False);
                Assert.That(node.SourceElement, Is.SameAs(element));
                Assert.That(first.AvailableElementTypes, Is.EquivalentTo(new[] { typeof(Namespace), typeof(TElement) }));
            }

            first.SelectNode(group);
            Assert.That(selection.SelectedElement, Is.Null);
            Assert.That(group.IsExpanded, Is.True);
            Assert.That(first.SelectedNode, Is.SameAs(first.RootNodes[0]));
            first.ToggleNode(group);
            first.ToggleElementTypeFilter(typeof(TElement));
            first.FilterText = "Needle";
            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.FilterPresentation.IsVisible(group), Is.True);
                Assert.That(first.FilterPresentation.IsVisible(node), Is.True);
                Assert.That(group.IsExpanded, Is.False);
            }
            first.FocusElement(element);
            first.SelectNode(node);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.SelectedNode, Is.SameAs(node));
                Assert.That(selection.SelectedElement, Is.SameAs(element));
                Assert.That(group.IsExpanded, Is.True);
                Assert.That(second.RootNodes[0].Children.Single().IsExpanded, Is.False);
                Assert.That(second.FilterText, Is.Empty);
            }
            first.FilterText = "absent";
            Assert.That(first.FilterPresentation.IsVisible(group), Is.False);
            first.ClearFilter();
            first.SelectNode(group);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(selection.SelectedElement, Is.SameAs(element));
                Assert.That(first.SelectedNode, Is.SameAs(node));
            }
        }

        [TestCase(TypeArgs = [typeof(EndFeatureMembership), typeof(PartUsage)])]
        [TestCase(TypeArgs = [typeof(StateSubactionMembership), typeof(ActionUsage)])]
        [TestCase(TypeArgs = [typeof(TransitionFeatureMembership), typeof(ActionUsage)])]
        [TestCase(TypeArgs = [typeof(RequirementConstraintMembership), typeof(ConstraintUsage)])]
        [TestCase(TypeArgs = [typeof(FeatureValue), typeof(ConstraintUsage)])]
        [TestCase(TypeArgs = [typeof(ResultExpressionMembership), typeof(ConstraintUsage)])]
        public async Task VerifyDetailMembershipSuppressesItsWholeSubtree<TMembership, TElement>()
            where TMembership : class, IRelationship, new()
            where TElement : class, IElement, new()
        {
            var descendant = new PartUsage { ElementId = "descendant", DeclaredName = "Detail child" };
            var detail = CreateElement<TElement>("detail", "Detail", descendant);
            var owner = new StateUsage { ElementId = "owner", DeclaredName = "Owner" };
            var role = Attach<TMembership>(owner, detail);
            var root = CreateElement<Namespace>("root", "Model", owner);
            var loader = new Mock<IModelLoaderService>();
            loader.Setup(service => service.LoadQuantitiesModel()).Returns(root);
            var selection = new ContextAwareService();
            using var browser = new ProjectBrowserViewModel(loader.Object, selection);
            Assert.That(await browser.InitializeAsync(CancellationToken.None), Is.True);
            var ownerNode = browser.RootNodes[0].Children.Single();

            Assert.That(ownerNode.Children, Is.Empty);
            foreach (var target in new IElement[] { role, detail, descendant })
            {
                selection.SelectedElement = target;
                browser.FocusElement(target);
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(browser.SelectedNode, Is.SameAs(ownerNode));
                    Assert.That(selection.SelectedElement, Is.SameAs(target));
                }
            }
            Assert.That(owner.ownedElement, Does.Contain(detail));
        }

        [TestCase(TypeArgs = [typeof(TransitionUsage)])]
        [TestCase(TypeArgs = [typeof(MetadataUsage)])]
        [TestCase(TypeArgs = [typeof(NamespaceImport)])]
        [TestCase(TypeArgs = [typeof(FeatureTyping)])]
        [TestCase(TypeArgs = [typeof(Documentation)])]
        public void VerifyDetailOnlyKindsAreExcludedBeforeBroadInterfaces<TElement>()
            where TElement : class, IElement, new()
        {
            Assert.That(ProjectBrowserElementProjection.IsNavigable(new TElement()), Is.False);
        }

        [Test]
        public async Task VerifyEnumerationValuesBelongToDefinitionButTypedUsagesKeepRows()
        {
            var literal = new EnumerationUsage { ElementId = "literal", DeclaredName = "On" };
            var definition = new EnumerationDefinition { ElementId = "enum", DeclaredName = "Mode" };
            Attach<VariantMembership>(definition, literal);
            var usage = new EnumerationUsage { ElementId = "usage", DeclaredName = "mode" };
            var part = CreateElement<PartUsage>("part", "Controller", usage);
            var root = CreateElement<Namespace>("root", "Model", definition, part);
            var loader = new Mock<IModelLoaderService>();
            loader.Setup(service => service.LoadQuantitiesModel()).Returns(root);
            using var browser = new ProjectBrowserViewModel(loader.Object, new ContextAwareService());
            Assert.That(await browser.InitializeAsync(CancellationToken.None), Is.True);
            var definitionNode = browser.RootNodes[0].Children[0];
            using (Assert.EnterMultipleScope())
            {
                Assert.That(definitionNode.Children, Is.Empty);
                Assert.That(definitionNode.Row.Indicators, Does.Not.Contain("Variation"));
                Assert.That(browser.RootNodes[0].Children[1].Children.Single().SourceElement, Is.SameAs(usage));
            }
            browser.FocusElement(literal);
            Assert.That(browser.SelectedNode, Is.SameAs(definitionNode));
        }

        [Test]
        public async Task VerifyVariantRoleOverridesOrdinaryPartAndConnectionPlacement()
        {
            var part = new PartUsage { ElementId = "part", DeclaredName = "Variant" };
            var connection = new ConnectionUsage { ElementId = "connection", DeclaredName = "Variant connection" };
            var variation = new PartDefinition { ElementId = "variation", DeclaredName = "Variation", IsVariation = true };
            Attach<VariantMembership>(variation, part);
            Attach<VariantMembership>(variation, connection);
            var root = CreateElement<Namespace>("root", "Model", variation);
            var loader = new Mock<IModelLoaderService>();
            loader.Setup(service => service.LoadQuantitiesModel()).Returns(root);
            using var browser = new ProjectBrowserViewModel(loader.Object, new ContextAwareService());
            Assert.That(await browser.InitializeAsync(CancellationToken.None), Is.True);
            var node = browser.RootNodes[0].Children.Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(node.IsGroup, Is.False);
                Assert.That(node.Row.Indicators, Does.Contain("Variation"));
                Assert.That(node.Children.Single().DisplayName, Is.EqualTo("Variants"));
                Assert.That(node.Children.Single().Children.Select(child => child.SourceElement),
                    Is.EqualTo(new IElement[] { part, connection }));
                Assert.That(node.Children.Single().IsExpanded, Is.False);
            }
        }

        [Test]
        public async Task VerifyAliasAndImportedOccurrencesKeepCanonicalIdentityAndMembershipData()
        {
            var shared = new PartUsage { ElementId = "shared", DeclaredName = "Original" };
            var alias = new Membership
            {
                ElementId = "alias",
                MemberElement = shared,
                MemberName = "Alias",
                Visibility = SysML2.NET.Core.Root.Namespaces.VisibilityKind.Private
            };
            var imported = new Membership { ElementId = "import", MemberElement = shared, MemberName = "Imported alias" };
            var source = CreateElement<SysML2.NET.Core.POCO.Kernel.Packages.Package>("source", "Library", shared);
            Mycelium.Bloom.Tests.Common.BrowserAcceptanceModelTestFactory.AddRelationship(source, imported);
            var root = new Namespace { ElementId = "root", DeclaredName = "Model" };
            Mycelium.Bloom.Tests.Common.BrowserAcceptanceModelTestFactory.AddRelationship(root, alias);
            Mycelium.Bloom.Tests.Common.BrowserAcceptanceModelTestFactory.AddRelationship(root,
                new MembershipImport
                {
                    ImportedMembership = imported,
                    Visibility = SysML2.NET.Core.Root.Namespaces.VisibilityKind.Protected
                });
            var loader = new Mock<IModelLoaderService>();
            loader.Setup(service => service.LoadQuantitiesModel()).Returns(root);
            var selection = new ContextAwareService();
            using var browser = new ProjectBrowserViewModel(loader.Object, selection);
            Assert.That(await browser.InitializeAsync(CancellationToken.None), Is.True);
            var nodes = browser.RootNodes[0].Children;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(nodes.Select(node => node.DisplayName), Is.EqualTo(new[] { "Alias", "Imported alias" }));
                Assert.That(nodes.All(node => ReferenceEquals(node.SourceElement, shared)), Is.True);
                Assert.That(nodes.Select(node => node.Id).Distinct().Count(), Is.EqualTo(2));
                Assert.That(nodes[1].Row.Indicators, Does.Contain("Imported"));
                Assert.That(nodes[0].Row.Indicators, Does.Not.Contain("Imported"));
                Assert.That(nodes[0].OccurrenceMembership, Is.SameAs(alias));
                Assert.That(nodes[1].OccurrenceMembership, Is.SameAs(imported));
                Assert.That(nodes.Select(node => node.Row.Visibility), Is.EqualTo(new[] { "private", "protected" }));
                Assert.That(imported.Visibility, Is.EqualTo(SysML2.NET.Core.Root.Namespaces.VisibilityKind.Public));
            }
            browser.SelectNode(nodes[1]);
            Assert.That(selection.SelectedElement, Is.SameAs(shared));
            Assert.That(browser.SelectedNode, Is.SameAs(nodes[1]));
            Assert.That(shared.DeclaredName, Is.EqualTo("Original"));
            browser.FocusElement(shared);
            Assert.That(browser.SelectedNode, Is.SameAs(nodes[0]));
            Assert.That(selection.SelectedElement, Is.SameAs(shared));
        }

        [Test]
        public async Task VerifyAliasToAncestorRemainsASeparateFiniteOccurrence()
        {
            var root = new Mock<INamespace>();
            var alias = new Membership { MemberName = "Self", MemberElement = root.Object };
            root.SetupGet(element => element.ElementId).Returns("root");
            root.SetupGet(element => element.DeclaredName).Returns("Model");
            root.SetupGet(element => element.ownedElement).Returns([]);
            root.SetupGet(element => element.OwnedRelationship).Returns([alias]);
            var loader = new Mock<IModelLoaderService>();
            loader.Setup(service => service.LoadQuantitiesModel()).Returns(root.Object);
            using var browser = new ProjectBrowserViewModel(loader.Object, new ContextAwareService());
            Assert.That(await browser.InitializeAsync(CancellationToken.None), Is.True);
            var occurrence = browser.RootNodes[0].Children.Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(occurrence.SourceElement, Is.SameAs(root.Object));
                Assert.That(occurrence.DisplayName, Is.EqualTo("Self"));
                Assert.That(occurrence.Id, Is.Not.EqualTo(browser.RootNodes[0].Id));
                Assert.That(occurrence.Children, Is.Empty);
            }
        }

        /// <summary>Connects an owner to a child through the SDK containment collections of a new relationship.</summary>
        /// <typeparam name="TMembership">The relationship type to instantiate.</typeparam>
        /// <param name="owner">The element whose owned relationships receive the new relationship.</param>
        /// <param name="child">The element added to the relationship's owned related elements.</param>
        /// <returns>The attached relationship, allowing the test to configure its semantic role.</returns>
        private static TMembership Attach<TMembership>(IElement owner, IElement child)
            where TMembership : IRelationship, new()
        {
            var membership = new TMembership();
            var assembly = typeof(IElement).Assembly;
            var related = assembly.GetType("SysML2.NET.Core.POCO.Root.Elements.IContainedRelationship")
                .GetProperty("OwnedRelatedElement");
            var relationships = assembly.GetType("SysML2.NET.Core.POCO.Root.Elements.IContainedElement")
                .GetProperty("OwnedRelationship");
            ((ICollection<IElement>)related.GetValue(membership)).Add(child);
            ((ICollection<IRelationship>)relationships.GetValue(owner)).Add(membership);
            return membership;
        }
    }
}
