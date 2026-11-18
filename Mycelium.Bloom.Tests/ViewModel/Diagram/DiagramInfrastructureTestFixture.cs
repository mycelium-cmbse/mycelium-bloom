// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramInfrastructureTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.Diagram
{
    using System;
    using System.Linq;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    using Moq;

    using Mycelium.Bloom.Components.UI.Organisms.Diagram;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Extensions;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Diagram;

    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Core.Types;
    using SysML2.NET.Core.POCO.Kernel.Connectors;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Systems.Parts;
    using SysML2.NET.Core.POCO.Systems.Ports;

    [TestFixture]
    public sealed class DiagramInfrastructureTestFixture
    {
        [TestCase(0, 16)]
        [TestCase(24, -1)]
        [TestCase(double.NaN, 16)]
        [TestCase(24, double.PositiveInfinity)]
        public void VerifyInvalidMarkerGeometryCannotPartiallyReplaceProjection(double width, double height)
        {
            var source = new PartUsage();
            var target = new PartUsage();
            var relationship = new Redefinition { RedefiningFeature = source, RedefinedFeature = target };
            var projection = DiagramTestFactory.CreateProjector().Create([source, target, relationship]);
            using var owner = new DiagramViewModel(projection, new ContextAwareService());
            var retained = owner.GetNodeViewModel(projection.Nodes[0].Occurrence.Id);
            var marker = DiagramMarker.Triangle with { ViewBox = (-23, -8, width, height) };
            var edge = projection.Edges[0];
            var invalid = projection with { Edges = [edge with { Notation = edge.Notation with { TargetMarker = marker } }] };

            Assert.That(() => owner.UpdateProjection(invalid), Throws.ArgumentException);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.Projection, Is.SameAs(projection));
                Assert.That(owner.GetNodeViewModel(projection.Nodes[0].Occurrence.Id), Is.SameAs(retained));
                Assert.That(retained.Presentation, Is.SameAs(projection.Nodes[0]));
                Assert.That(owner.GetEdgeViewModel(edge.Id).Presentation, Is.SameAs(edge));
            }
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void VerifyDiCompositionPreservesCustomBuiltInNotation(bool registerBeforeDefaults, bool omit)
        {
            var services = new ServiceCollection();
            var notation = omit ? null : DiagramEdgeNotations.Redefinition with { Label = "Custom notation" };
            var source = new PartUsage();
            var target = new PartUsage();
            var relationship = new Redefinition { RedefiningFeature = source, RedefinedFeature = target };
            var factory = new Mock<IDiagramEdgeFactory>(MockBehavior.Strict);
            factory.SetupGet(value => value.ElementType).Returns(typeof(IRedefinition));
            factory.Setup(value => value.Create(relationship)).Returns(notation);

            if (registerBeforeDefaults)
            {
                services.AddSingleton(factory.Object);
            }

            services.AddDiagramServices();

            if (!registerBeforeDefaults)
            {
                services.AddSingleton(factory.Object);
            }

            using var provider = services.BuildServiceProvider();
            var projection = provider.GetRequiredService<IGeneralViewProjector>().Create([source, target, relationship]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(projection.Edges, Has.Length.EqualTo(omit ? 0 : 1));
                Assert.That(projection.HasOmittedContent, Is.EqualTo(omit));
            }

            if (!omit)
            {
                Assert.That(projection.Edges[0].Notation, Is.SameAs(notation));
            }

            factory.Verify(value => value.Create(relationship), Times.Once);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void VerifyTypeDispatchSelectsSpecificInterfaceBeforeBroadFallback(bool reverse)
        {
            System.Type[] registrations = reverse ? [typeof(IElement), typeof(IFeature), typeof(IPortUsage)]
                : [typeof(IPortUsage), typeof(IFeature), typeof(IElement)];
            var selected = TypeDispatch.Find(registrations, typeof(PortUsage), type => type);
            Assert.That(selected, Is.SameAs(typeof(IPortUsage)));
        }

        [Test]
        public void VerifyTypeDispatchRetainsEqualAndUnrelatedRegistrationOrder()
        {
            var first = new DiagramNodeComponentRegistration(typeof(DiagramShellPresentation), typeof(DiagramShellNode));
            var second = new DiagramNodeComponentRegistration(typeof(DiagramShellPresentation), typeof(DiagramCommentNode));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TypeDispatch.Find(new[] { first, second }, typeof(DiagramShellPresentation), entry => entry.PresentationType), Is.SameAs(first));
                Assert.That(TypeDispatch.Find(new[] { typeof(ICloneable), typeof(IComparable) }, typeof(string), type => type), Is.SameAs(typeof(ICloneable)));
                Assert.That(TypeDispatch.Find(new[] { typeof(IPortUsage) }, typeof(PartUsage), type => type), Is.Null);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void VerifyEdgeFactoriesPreserveSpecializationAndExplicitOmission(bool reverse)
        {
            var factories = DiagramEdgeFactories.CreateDefault();
            var projector = new GeneralViewProjector([], reverse ? factories.Reverse() : factories);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(projector.GetEdgeNotation(new Redefinition()), Is.EqualTo(DiagramEdgeNotations.Redefinition));
                Assert.That(projector.GetEdgeNotation(new ReferenceSubsetting()), Is.Null);
                Assert.That(projector.GetEdgeNotation(new BindingConnector()), Is.EqualTo(DiagramEdgeNotations.Binding));
            }
        }

        [Test]
        public void VerifyRegisteredEdgePolicyExtendsProjectionWithoutChangingKindsOrRenderer()
        {
            var marker = new DiagramMarker("M0 0 L-4 -2 L-4 2 Z", "currentColor");
            var notation = new DiagramEdgeNotation("custom-redefinition", marker, DiagramMarker.None, "2 3", "custom", 18);
            var factory = new Mock<IDiagramEdgeFactory>(MockBehavior.Strict);
            factory.SetupGet(value => value.ElementType).Returns(typeof(IRedefinition));
            factory.Setup(value => value.Create(It.IsAny<IElement>())).Returns(notation);
            var source = new PartUsage();
            var target = new PartUsage();
            var relationship = new Redefinition { RedefiningFeature = source, RedefinedFeature = target };
            var projector = new GeneralViewProjector([new ShellDiagramNodeFactory()],
                new[] { factory.Object }.Concat(DiagramEdgeFactories.CreateDefault().Where(value => value.ElementType != typeof(IRedefinition))));

            var projection = projector.Create([source, target, relationship]);
            var edge = projection.Edges.Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(edge.Notation, Is.SameAs(notation));
                Assert.That(edge.Element, Is.SameAs(relationship));
                Assert.That(edge.SourceMarker, Is.SameAs(marker));
                Assert.That(edge.Label, Is.EqualTo("custom"));
                Assert.That(edge.IsDashed, Is.True);
                Assert.That(projection.HasOmittedContent, Is.False);
            }
        }

        [Test]
        public void VerifyLayoutControlsLimitPlacementAndOmission()
        {
            var layout = new GridDiagramLayout(new DiagramLayoutOptions
            {
                MaximumElements = 2,
                Columns = 1,
                Origin = new(12, 34),
                Spacing = new(100, 180)
            });
            var projector = new GeneralViewProjector([new ShellDiagramNodeFactory()], layout: layout);
            var elements = new IElement[] { new PartUsage(), new PartUsage(), new PartUsage() };

            var projection = projector.Create(elements);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(projection.Nodes, Has.Length.EqualTo(2));
                Assert.That(projection.HasOmittedContent, Is.True);
                Assert.That(projection.Nodes[0].Occurrence.Element, Is.SameAs(elements[0]));
                Assert.That(projection.Nodes[1].Occurrence.Element, Is.SameAs(elements[1]));
                Assert.That(projection.Nodes[0].Occurrence.X, Is.EqualTo(12));
                Assert.That(projection.Nodes[0].Occurrence.Y, Is.EqualTo(34));
                Assert.That(projection.Nodes[1].Occurrence.X, Is.EqualTo(12));
                Assert.That(projection.Nodes[1].Occurrence.Y, Is.EqualTo(214));
                Assert.That(projection.Nodes[1].Occurrence.Id, Is.Not.EqualTo(projection.Nodes[0].Occurrence.Id));
            }
        }

        [TestCase(0, 3)]
        [TestCase(200, 0)]
        [TestCase(-1, 3)]
        public void VerifyLayoutRejectsInvalidLimits(int maximumElements, int columns)
        {
            Assert.That(() => new GridDiagramLayout(new DiagramLayoutOptions { MaximumElements = maximumElements, Columns = columns }), Throws.ArgumentException);
        }

        [TestCase("missing-key")]
        [TestCase("missing-marker")]
        [TestCase("non-finite-offset")]
        public void VerifyProjectionValidatorRejectsInvalidRegisteredNotation(string invalid)
        {
            var first = DiagramTestFactory.Node(new PartUsage());
            var second = DiagramTestFactory.Node(new PartUsage());
            var notation = DiagramEdgeNotations.Dependency;

            if (invalid == "missing-key")
            {
                notation = notation with { Key = null };
            }
            else if (invalid == "missing-marker")
            {
                notation = notation with { TargetMarker = null };
            }
            else
            {
                notation = notation with { LabelOffset = double.NaN };
            }

            var edge = new DiagramEdgePresentation(Guid.NewGuid(), new Redefinition(), first.Occurrence.Id, second.Occurrence.Id, notation, null);
            var projection = new DiagramProjection([first, second], [edge]);
            Assert.That(() => DiagramProjectionValidator.Validate(projection), Throws.ArgumentException);
        }

        [Test]
        public void VerifyBoundsIncludeCanonicalBoundaryEnvelopeAndManualBends()
        {
            var element = new PartUsage();
            var port = new PortUsage();
            var node = DiagramTestFactory.Node(element) with
            {
                Boundary = [new(port, "port", Mycelium.Bloom.Model.Enum.BoundarySide.Right, 0.5)]
            };
            var edge = new DiagramEdgePresentation(Guid.NewGuid(), new Redefinition(), node.Occurrence.Id, node.Occurrence.Id,
                DiagramEdgeNotations.Dependency, null, Bends: [new(-500, -600), new(1000, 1100)]);
            var projection = new DiagramProjection([node], [edge]);
            DiagramProjectionValidator.Validate(projection);
            var bounds = DiagramBoundsCalculator.Measure(projection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(bounds, Is.EqualTo((-532d, -632d, 1032d, 1132d)));
                Assert.That(node.Occurrence.Element, Is.SameAs(element));
                Assert.That(node.Boundary[0].Element, Is.SameAs(port));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void VerifyEditorDescriptorsRespectEnvironmentAndDistinctExampleIdentity(bool development)
        {
            var environment = new Mock<IHostEnvironment>(MockBehavior.Strict);
            environment.SetupGet(value => value.EnvironmentName).Returns(development ? Environments.Development : Environments.Production);
            var services = new ServiceCollection().AddDiagramServices();
            services.AddSingleton(environment.Object);
            services.AddSingleton<IElementSelectionService>(new ContextAwareService());
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var registry = scope.ServiceProvider.GetRequiredService<DiagramEditorRegistry>();
            var general = registry.FindAction("open-general-view");
            using var state = general.CreateViewModel(new PartUsage());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(general.ComponentType, Is.SameAs(typeof(DiagramCanvas)));
                Assert.That(state.Projection.Nodes, Has.Length.EqualTo(1));
                Assert.That(registry.FindAction("open-general-example") is not null, Is.EqualTo(development));
                Assert.That(registry.Editors.Select(editor => editor.ViewTypeKey).Distinct().ToArray(), Has.Length.EqualTo(registry.Editors.Length));
            }

            if (development)
            {
                Assert.That(registry.FindAction("open-general-example").ViewTypeKey, Is.EqualTo("general-view-example"));
            }
        }
    }
}
