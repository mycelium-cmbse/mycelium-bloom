// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserExpressionProjectionTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.ProjectBrowser
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging.Abstractions;
    using Moq;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.DetailsPanel;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;
    using SysML2.NET.Core.Core.Types;
    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Core.Types;
    using SysML2.NET.Core.POCO.Kernel.Behaviors;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.FeatureValues;
    using SysML2.NET.Core.POCO.Kernel.Functions;
    using SysML2.NET.Core.POCO.Root.Annotations;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Attributes;
    using SysML2.NET.Core.POCO.Systems.Constraints;
    using SysML2.NET.Core.POCO.Systems.Parts;
    using SysML2.NET.Core.POCO.Systems.Ports;
    using SysML2.NET.Dal;
    using SysML2.NET.Serializer.Json;
    using static Mycelium.Bloom.Tests.Common.ProjectBrowserNodeTestFactory;

    [TestFixture]
    public sealed class ProjectBrowserExpressionProjectionTestFixture
    {
        private static readonly string[] ExpectedConstraintNames = ["orderSum", "boundMatch"];
        private static readonly string[] ExpectedCompoundExpressionReferences =
        [
            "contravariantOrder",
            "covariantOrder",
            "order"
        ];

        [Test]
        public void VerifyQuantitiesHidesOnlyEmptyExpressionSupport()
        {
            var environment = new Mock<IHostEnvironment>();
            environment.SetupGet(value => value.ContentRootPath)
                .Returns(TestRepository.GetDirectoryPath("Mycelium.Bloom"));
            var assembler = new Assembler(NullLoggerFactory.Instance);
            var loader = new ModelLoaderService(environment.Object, new DeSerializer(NullLoggerFactory.Instance),
                assembler, NullLogger<ModelLoaderService>.Instance);
            var root = loader.LoadQuantitiesModel();
            var elements = Descendants(root).ToArray();
            var hiddenFeatures = elements.OfType<IFeature>()
                .Where(feature => !ProjectBrowserElementProjection.IsNavigable(feature) && feature is not IMultiplicity)
                .ToArray();
            var expressions = elements.OfType<IExpression>().Where(expression => expression.owner is not IMultiplicity)
                .ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(hiddenFeatures, Has.Length.GreaterThanOrEqualTo(22));
                Assert.That(hiddenFeatures.OfType<IExpression>(), Is.Empty);
                Assert.That(hiddenFeatures.Any(feature => feature.owningMembership is IReturnParameterMembership),
                    Is.True);
                Assert.That(expressions, Has.Length.EqualTo(21));
                Assert.That(expressions.All(ProjectBrowserElementProjection.IsNavigable), Is.True);
                Assert.That(elements.OfType<IAssertConstraintUsage>().Select(element => element.name),
                    Is.EquivalentTo(ExpectedConstraintNames));
                Assert.That(elements.OfType<IComment>().Where(element => element is not IDocumentation)
                    .All(ProjectBrowserElementProjection.IsNavigable), Is.True);
            }
        }

        [Test]
        public async Task VerifyQuantitiesProjectsReferenceLeavesAndExpandableCompoundExpressions()
        {
            var environment = new Mock<IHostEnvironment>();
            environment.SetupGet(value => value.ContentRootPath)
                .Returns(TestRepository.GetDirectoryPath("Mycelium.Bloom"));
            var assembler = new Assembler(NullLoggerFactory.Instance);
            var loader = new ModelLoaderService(environment.Object, new DeSerializer(NullLoggerFactory.Instance),
                assembler, NullLogger<ModelLoaderService>.Instance);
            var root = loader.LoadQuantitiesModel();
            var modelLoader = new Mock<IModelLoaderService>();
            modelLoader.Setup(service => service.LoadQuantitiesModel()).Returns(root);
            using var browser = new ProjectBrowserViewModel(modelLoader.Object, new ContextAwareService());
            await browser.InitializeAsync(CancellationToken.None);

            var nodes = Flatten(browser.RootNodes).ToArray();
            var dimensions = Descendants(root).OfType<IAttributeUsage>()
                .Single(element => element.name == "dimensions");
            var dimensionsNode = nodes.Single(node => ReferenceEquals(node.SourceElement, dimensions));
            var notation = new DetailsPanelPresentation(dimensions).ExpressionNotations
                .Select(expression => expression.TextualNotation.Text)
                .ToArray();
            var orderSum = nodes.Single(node => node.SourceElement is IAssertConstraintUsage
                                                && node.SourceElement.name == "orderSum");
            var boundMatch = nodes.Single(node => node.SourceElement is IAssertConstraintUsage
                                                  && node.SourceElement.name == "boundMatch");
            var genericParameterNodes = Flatten([orderSum, boundMatch])
                .Where(node => node.SourceElement is IFeature feature
                               && feature.owningMembership is IParameterMembership
                               && string.IsNullOrWhiteSpace(feature.DeclaredName)
                               && string.IsNullOrWhiteSpace(feature.DeclaredShortName))
                .ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dimensionsNode.SourceElement, Is.SameAs(dimensions));
                Assert.That(dimensionsNode.HasChildren, Is.False);
                Assert.That(notation, Does.Contain("mRef.dimensions"));
                Assert.That(orderSum.HasChildren, Is.True);
                Assert.That(Flatten([orderSum]).Select(node => node.DisplayName),
                    Does.Contain("Operator +").And.Contain("Operator =="));
                Assert.That(Flatten([orderSum]).Where(node => node.SourceElement is IFeatureReferenceExpression)
                        .Select(node => GetReferenceName(node.DisplayName))
                        .Where(label => label is "contravariantOrder" or "covariantOrder" or "order"),
                    Is.EqualTo(ExpectedCompoundExpressionReferences));
                Assert.That(genericParameterNodes, Is.Empty);
                Assert.That(boundMatch.HasChildren, Is.True);
                Assert.That(Flatten([boundMatch]).Select(node => node.DisplayName),
                    Does.Contain("Operator or").And.Contain("Operator ==")
                        .And.Contain("Operator and").And.Contain("Operator not"));
                Assert.That(Flatten([orderSum, boundMatch])
                    .Where(node => ProjectBrowserElementProjection.IsLeafReferenceExpression(node.SourceElement))
                    .All(node => !node.HasChildren), Is.True);
            }
        }

        [TestCase("name")]
        [TestCase("shortName")]
        [TestCase("documentation")]
        [TestCase("typing")]
        [TestCase("multiplicity")]
        [TestCase("ordered")]
        [TestCase("defaultValue")]
        [TestCase("private")]
        public void VerifyExpressionSupportWithAdditionalContentRemainsVisible(string content)
        {
            var feature = new Feature { ElementId = "input", Direction = FeatureDirectionKind.In };
            var expression = new FeatureChainExpression { ElementId = "chain", Operator = "." };
            Attach<FeatureValue>(new AttributeUsage(), expression);
            Attach<ParameterMembership>(expression, feature);
            Assert.That(ProjectBrowserElementProjection.IsNavigable(feature), Is.False);

            switch (content)
            {
                case "name": feature.DeclaredName = "argument"; break;
                case "shortName": feature.DeclaredShortName = "arg"; break;
                case "documentation":
                    Attach<OwningMembership>(feature, new Documentation { Body = "Parameter meaning" }); break;
                case "typing": AttachRelationship(feature, new FeatureTyping()); break;
                case "multiplicity":
                    Attach<OwningMembership>(feature,
                        new SysML2.NET.Core.POCO.Kernel.Multiplicities.MultiplicityRange()); break;
                case "ordered": feature.IsOrdered = true; break;
                case "defaultValue":
                    Attach<FeatureValue>(feature, new LiteralInteger { Value = 5 });
                    feature.OwnedRelationship.OfType<FeatureValue>().Single().IsDefault = true;
                    break;
                case "private":
                    feature.owningMembership.Visibility = SysML2.NET.Core.Root.Namespaces.VisibilityKind.Private;
                    Attach<FeatureMembership>(feature, new Feature { DeclaredName = "restricted member" });
                    feature.OwnedRelationship.OfType<FeatureMembership>().Single().Visibility =
                        SysML2.NET.Core.Root.Namespaces.VisibilityKind.Private;
                    break;
            }

            Assert.That(ProjectBrowserElementProjection.IsNavigable(feature), Is.True);
        }

        [TestCase(TypeArgs = [typeof(AssertConstraintUsage)])]
        [TestCase(TypeArgs = [typeof(PartUsage)])]
        [TestCase(TypeArgs = [typeof(PortUsage)])]
        [TestCase(TypeArgs = [typeof(PartDefinition)])]
        public void VerifyDomainElementsWithinExpressionsRemainVisible<TElement>()
            where TElement : class, IElement, new()
        {
            var element = new TElement { DeclaredName = "Modeled element" };
            var expression = new OperatorExpression();
            Attach<FeatureValue>(new AttributeUsage(), expression);
            Attach<ParameterMembership>(expression, element);

            Assert.That(ProjectBrowserElementProjection.IsNavigable(element), Is.True);
            if (element is AssertConstraintUsage)
            {
                Assert.That(element, Is.InstanceOf<IExpression>());
            }
        }

        [Test]
        public void VerifyStandaloneExpressionsAndOrdinaryFeaturesRemainVisible()
        {
            var standalone = new Expression();
            var input = new Feature { Direction = FeatureDirectionKind.In };
            Attach<ParameterMembership>(standalone, input);
            var ordinary = new Feature();
            Attach<FeatureMembership>(new Feature(), ordinary);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ProjectBrowserElementProjection.IsNavigable(standalone), Is.True);
                Assert.That(ProjectBrowserElementProjection.IsNavigable(input), Is.True);
                Assert.That(ProjectBrowserElementProjection.IsNavigable(ordinary), Is.True);
                Assert.That(ProjectBrowserElementProjection.IsNavigable(new Comment()), Is.True);
                Assert.That(ProjectBrowserElementProjection.IsNavigable(Mock.Of<IElement>()), Is.True);
            }
        }

        [Test]
        public async Task VerifyProjectedExpressionParametersPreserveNavigationAndIdentity()
        {
            var reference = new FeatureReferenceExpression { ElementId = "reference" };
            var input = new Feature { ElementId = "input", Direction = FeatureDirectionKind.In };
            var output = new Feature { ElementId = "output", Direction = FeatureDirectionKind.Out };
            var target = new Feature { ElementId = "target" };
            var port = new PortUsage { ElementId = "port", DeclaredName = "Nested port" };
            var chain = new FeatureChainExpression { ElementId = "chain", Operator = "." };
            var attribute = new AttributeUsage { ElementId = "attribute", DeclaredName = "dimensions" };
            Attach<FeatureValue>(attribute, chain);
            Attach<ParameterMembership>(chain, input);
            Attach<ReturnParameterMembership>(chain, output);
            Attach<FeatureValue>(input, reference);
            Attach<FeatureMembership>(input, target);
            Attach<FeatureMembership>(target, port);
            var root = CreateElement<Namespace>("root", "Model", attribute);
            var loader = new Mock<IModelLoaderService>();
            loader.Setup(service => service.LoadQuantitiesModel()).Returns(root);
            var selection = new ContextAwareService();
            using var first = new ProjectBrowserViewModel(loader.Object, selection);
            using var second = new ProjectBrowserViewModel(loader.Object, selection);
            await Assert.MultipleAsync(async () =>
            {
                Assert.That(await first.InitializeAsync(CancellationToken.None), Is.True);
                Assert.That(await second.InitializeAsync(CancellationToken.None), Is.True);
            });
            var chainNode = first.RootNodes[0].Children.Single().Children.Single();
            var portNode = chainNode.Children.Single(node => node.SourceElement == port);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(chainNode.SourceElement, Is.SameAs(chain));
                Assert.That(chainNode.Children.Select(node => node.SourceElement),
                    Is.EqualTo(new IElement[] { reference, port }));
                Assert.That(first.AvailableElementTypes, Does.Not.Contain(typeof(Feature)));
                Assert.That(input.owner, Is.SameAs(chain));
                Assert.That(port.owner, Is.SameAs(target));
            }

            first.FilterText = "Nested port";
            first.FocusElement(port);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.SelectedNode, Is.SameAs(portNode));
                Assert.That(first.FilterPresentation.IsVisible(chainNode), Is.True);
                Assert.That(chainNode.IsExpanded, Is.True);
            }

            first.SelectNode(portNode);
            Assert.That(selection.SelectedElement, Is.SameAs(port));
            first.ClearFilter();
            foreach (var hidden in new[] { input, output, target })
            {
                selection.SelectedElement = hidden;
                first.FocusElement(hidden);
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(first.SelectedNode, Is.SameAs(chainNode));
                    Assert.That(selection.SelectedElement, Is.SameAs(hidden));
                }
            }

            first.ToggleNode(chainNode);
            first.FocusElement(new PortUsage { ElementId = "port" });
            using (Assert.EnterMultipleScope())
            {
                Assert.That(chainNode.IsExpanded, Is.True);
                Assert.That(first.SelectedNode, Is.SameAs(portNode));
                Assert.That(selection.SelectedElement, Is.SameAs(target));
                Assert.That(second.RootNodes[0].Children.Single().IsExpanded, Is.False);
                Assert.That(second.SelectedNode, Is.SameAs(second.RootNodes[0]));
                Assert.That(second.FilterText, Is.Empty);
            }
        }

        private static IEnumerable<IElement> Descendants(IElement element)
        {
            yield return element;
            foreach (var child in element.ownedElement.SelectMany(Descendants))
            {
                yield return child;
            }
        }

        private static IEnumerable<ProjectBrowserNodeViewModel> Flatten(IEnumerable<ProjectBrowserNodeViewModel> nodes)
        {
            foreach (var node in nodes)
            {
                yield return node;
                foreach (var descendant in Flatten(node.Children))
                {
                    yield return descendant;
                }
            }
        }

        private static string GetReferenceName(string label)
        {
            var separator = label.LastIndexOf("::", StringComparison.Ordinal);
            return separator < 0 ? label : label[(separator + 2)..];
        }

        private static void Attach<TMembership>(IElement owner, IElement child) where TMembership : IRelationship, new()
        {
            var membership = new TMembership();
            var property = typeof(IElement).Assembly
                .GetType("SysML2.NET.Core.POCO.Root.Elements.IContainedRelationship")
                .GetProperty("OwnedRelatedElement");
            ((ICollection<IElement>)property.GetValue(membership)).Add(child);
            AttachRelationship(owner, membership);
        }

        private static void AttachRelationship(IElement owner, IRelationship relationship)
        {
            var property = typeof(IElement).Assembly.GetType("SysML2.NET.Core.POCO.Root.Elements.IContainedElement")
                .GetProperty("OwnedRelationship");
            ((ICollection<IRelationship>)property.GetValue(owner)).Add(relationship);
        }
    }
}