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
    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.FeatureValues;
    using SysML2.NET.Core.POCO.Kernel.Functions;
    using SysML2.NET.Core.POCO.Root.Annotations;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Attributes;
    using SysML2.NET.Core.POCO.Systems.Constraints;
    using SysML2.NET.Core.POCO.Systems.Ports;
    using SysML2.NET.Dal;
    using SysML2.NET.Serializer.Json;
    using static Mycelium.Bloom.Tests.Common.ProjectBrowserNodeTestFactory;

    /// <summary>Verifies reviewed expression placement, including the constraint usage exception.</summary>
    [TestFixture]
    public sealed class ProjectBrowserExpressionProjectionTestFixture
    {
        private static readonly string[] ExpectedPublicOccurrenceVisibilities = ["public", "public"];

        [TestCase(TypeArgs = [typeof(Expression)])]
        [TestCase(TypeArgs = [typeof(OperatorExpression)])]
        [TestCase(TypeArgs = [typeof(FeatureChainExpression)])]
        [TestCase(TypeArgs = [typeof(FeatureReferenceExpression)])]
        [TestCase(TypeArgs = [typeof(LiteralInteger)])]
        [TestCase(TypeArgs = [typeof(Comment)])]
        [TestCase(TypeArgs = [typeof(Documentation)])]
        public void VerifyDetailOnlyKindsDoNotProduceRows<TElement>() where TElement : class, IElement, new()
        {
            Assert.That(ProjectBrowserElementProjection.IsNavigable(new TElement { DeclaredName = "Named detail" }), Is.False);
        }

        [Test]
        public void VerifyConstraintUsagesAreNotExcludedByTheirExpressionInterface()
        {
            var constraint = new AssertConstraintUsage { DeclaredName = "orderSum" };
            using (Assert.EnterMultipleScope())
            {
                Assert.That(constraint, Is.InstanceOf<IExpression>());
                Assert.That(ProjectBrowserElementProjection.IsNavigable(constraint), Is.True);
            }
        }

        [Test]
        public async Task VerifyQuantitiesKeepsConstraintsAndTheirCanonicalExpressionInformation()
        {
            var environment = new Mock<IHostEnvironment>();
            environment.SetupGet(value => value.ContentRootPath)
                .Returns(TestRepository.GetDirectoryPath("Mycelium.Bloom"));
            var loader = new ModelLoaderService(environment.Object, new DeSerializer(NullLoggerFactory.Instance),
                new Assembler(NullLoggerFactory.Instance), NullLogger<ModelLoaderService>.Instance);
            var root = loader.LoadQuantitiesModel();
            var modelLoader = new Mock<IModelLoaderService>();
            modelLoader.Setup(service => service.LoadQuantitiesModel()).Returns(root);
            using var browser = new ProjectBrowserViewModel(modelLoader.Object, new ContextAwareService());
            Assert.That(await browser.InitializeAsync(CancellationToken.None), Is.True);

            var rootNode = browser.RootNodes.Single();
            var quantities = rootNode.Children.Single();
            var canonicalOwner = quantities.Children.Single(node => node.DisplayName == "TensorQuantityValue");
            var aliasOwner = quantities.Children.Single(node => node.DisplayName == "QuantityValue");
            var paths = Paths(rootNode).ToArray();
            var nodes = paths.Select(path => path[^1]).Where(node => !node.IsGroup).ToArray();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(rootNode.DisplayName, Is.EqualTo("Model"));
                Assert.That(quantities.DisplayName, Is.EqualTo("Quantities"));
                Assert.That(aliasOwner.SourceElement, Is.SameAs(canonicalOwner.SourceElement));
                Assert.That(aliasOwner.OccurrenceMembership, Is.Not.SameAs(canonicalOwner.OccurrenceMembership));
                Assert.That(aliasOwner.OccurrenceMembership.MemberName, Is.EqualTo("QuantityValue"));
                Assert.That(aliasOwner.OccurrenceMembership, Is.Not.InstanceOf<IOwningMembership>());
                Assert.That(aliasOwner.Row.Indicators, Does.Not.Contain("Imported"));
                Assert.That(nodes.Any(node => node.SourceElement is IExpression and not IConstraintUsage), Is.False);
                Assert.That(nodes.Any(node => node.SourceElement is IComment), Is.False);
            }

            foreach (var name in new[] { "dimensions", "orderSum", "boundMatch" })
            {
                var canonical = canonicalOwner.Children.Single(node => node.DisplayName == name);
                var aliasChild = aliasOwner.Children.Single(node => node.DisplayName == name);
                var occurrences = paths.Where(path => ReferenceEquals(path[^1].SourceElement, canonical.SourceElement)).ToArray();
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(occurrences, Has.Length.EqualTo(2));
                    Assert.That(occurrences.Select(path => string.Join("/", path.Select(node => node.DisplayName))),
                        Is.EqualTo(new[]
                        {
                            $"Model/Quantities/TensorQuantityValue/{name}",
                            $"Model/Quantities/QuantityValue/{name}"
                        }));
                    Assert.That(aliasChild.SourceElement, Is.SameAs(canonical.SourceElement));
                    Assert.That(aliasChild.ElementId, Is.EqualTo(canonical.ElementId));
                    Assert.That(aliasChild.Id, Is.Not.EqualTo(canonical.Id));
                    Assert.That(canonical.SourceElement.owner, Is.SameAs(canonicalOwner.SourceElement));
                    Assert.That(aliasChild.OccurrenceMembership, Is.SameAs(canonical.OccurrenceMembership));
                    Assert.That(occurrences.Select(path => path[^1].Row.Visibility),
                        Is.EqualTo(ExpectedPublicOccurrenceVisibilities));
                    Assert.That(occurrences.All(path => !path[^1].Row.Indicators.Contains("Imported")), Is.True);
                }
                browser.SelectNode(aliasChild);
                browser.FocusElement(canonical.SourceElement);
                Assert.That(browser.SelectedNode, Is.SameAs(canonical));
            }

            var dimensionsNode = canonicalOwner.Children.Single(node => node.DisplayName == "dimensions");
            var dimensions = (IAttributeUsage)dimensionsNode.SourceElement;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(dimensions.ElementId, Is.EqualTo("334779bf-6357-5adf-a7c5-ffb32c2ffd17"));
                Assert.That(new DetailsPanelPresentation(dimensions).ExpressionNotations
                    .Select(expression => expression.TextualNotation.Text), Does.Contain("mRef.dimensions"));
            }
            var expression = Descendants(dimensions).OfType<IFeatureChainExpression>().Single();
            browser.FilterText = "dimensions";
            browser.FocusElement(expression);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(browser.SelectedNode, Is.SameAs(dimensionsNode));
                Assert.That(browser.FilterPresentation.IsVisible(aliasOwner), Is.True);
                Assert.That(browser.FilterText, Is.EqualTo("dimensions"));
            }
        }

        [Test]
        public async Task VerifyExcludedExpressionSubtreeDoesNotPromoteItsDomainChildren()
        {
            var port = new PortUsage { ElementId = "port", DeclaredName = "Nested port" };
            var input = CreateElement<Feature>("input", "Named input", port);
            var chain = CreateElement<FeatureChainExpression>("chain", "Named expression", input);
            var attribute = CreateElement<AttributeUsage>("attribute", "dimensions", chain);
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
            var owner = first.RootNodes[0].Children.Single();
            Assert.That(owner.Children, Is.Empty);
            foreach (var hidden in new IElement[] { chain, input, port })
            {
                selection.SelectedElement = hidden;
                first.FilterText = "dimensions";
                first.FocusElement(hidden);
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(first.SelectedNode, Is.SameAs(owner));
                    Assert.That(first.FilterPresentation.IsVisible(owner), Is.True);
                    Assert.That(first.FilterText, Is.EqualTo("dimensions"));
                    Assert.That(selection.SelectedElement, Is.SameAs(hidden));
                    Assert.That(second.SelectedNode, Is.SameAs(second.RootNodes[0]));
                    Assert.That(second.FilterText, Is.Empty);
                }
            }
            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.AvailableElementTypes, Does.Not.Contain(typeof(FeatureChainExpression)));
                Assert.That(attribute.ownedElement, Does.Contain(chain));
                Assert.That(input.ownedElement, Does.Contain(port));
            }
        }

        private static IEnumerable<IElement> Descendants(IElement element)
        {
            yield return element;
            foreach (var child in (element.ownedElement ?? []).SelectMany(Descendants))
            {
                yield return child;
            }
        }

        private static IEnumerable<IReadOnlyList<ProjectBrowserNodeViewModel>> Paths(ProjectBrowserNodeViewModel node)
        {
            yield return new[] { node };
            foreach (var child in node.Children)
            {
                foreach (var path in Paths(child))
                {
                    yield return new[] { node }.Concat(path).ToArray();
                }
            }
        }
    }
}
