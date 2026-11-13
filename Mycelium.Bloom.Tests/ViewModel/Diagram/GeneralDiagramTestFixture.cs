// ------------------------------------------------------------------------------------------------
// <copyright file="GeneralDiagramTestFixture.cs" company="Starion Group S.A.">
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

    using Blazor.Diagrams.Core.Geometry;

    using Moq;

    using Mycelium.Bloom.Components.UI.Organisms.Diagram;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Diagram;

    using SysML2.NET.Core.POCO.Core.Classifiers;
    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Core.Types;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.FeatureValues;
    using SysML2.NET.Core.POCO.Kernel.Packages;
    using SysML2.NET.Core.POCO.Root.Annotations;
    using SysML2.NET.Core.POCO.Root.Dependencies;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Actions;
    using SysML2.NET.Core.POCO.Systems.Attributes;
    using SysML2.NET.Core.POCO.Systems.Connections;
    using SysML2.NET.Core.POCO.Systems.Constraints;
    using SysML2.NET.Core.POCO.Systems.Flows;
    using SysML2.NET.Core.POCO.Systems.Parts;
    using SysML2.NET.Core.POCO.Systems.Ports;
    using SysML2.NET.Core.POCO.Systems.Requirements;
    using SysML2.NET.Core.POCO.Systems.States;

    [TestFixture]
    public sealed class GeneralDiagramTestFixture
    {
        [Test]
        public void VerifyReviewOwnedDefinitionDoesNotSilentlyLoseNamespaceMembership()
        {
            var package = new Package { Id = Guid.NewGuid() };
            var definition = new PartDefinition { Id = Guid.NewGuid() };
            var membership = BrowserAcceptanceModelTestFactory.Own<OwningMembership>(package, definition);
            var projection = GeneralViewProjector.Create([package, definition]);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(projection.Nodes, Has.Length.EqualTo(2));
                Assert.That(GeneralViewProjector.Kind(membership), Is.Null);
                Assert.That(projection.Edges, Is.Empty);
                Assert.That(projection.HasOmittedContent, Is.True);
            }
        }

        [TestCase(typeof(ConnectionUsage))]
        [TestCase(typeof(BindingConnectorAsUsage))]
        public void VerifyReviewOwnedBinaryConnectorIsRepresentedAsAnEdge(System.Type type)
        {
            var package = new Package { Id = Guid.NewGuid() };
            var first = new PartUsage { Id = Guid.NewGuid() };
            var second = new PartUsage { Id = Guid.NewGuid() };
            var connector = (IElement)Activator.CreateInstance(type);
            connector.Id = Guid.NewGuid();
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(package, connector);
            foreach (var target in new[] { first, second })
            {
                var end = new Feature { Id = Guid.NewGuid(), IsEnd = true };
                BrowserAcceptanceModelTestFactory.Own<EndFeatureMembership>(connector, end);
                BrowserAcceptanceModelTestFactory.AddRelationship(end, new ReferenceSubsetting { ReferencedFeature = target });
            }
            var projection = GeneralViewProjector.Create([package, first, second, connector]);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(projection.Nodes.Any(node => ReferenceEquals(node.Occurrence.Element, connector)), Is.False);
                Assert.That(projection.Edges.Single().Element, Is.SameAs(connector));
                Assert.That(projection.HasOmittedContent, Is.False);
            }
        }

        [Test]
        public void VerifyReviewKeywordDoesNotDependOnTheRuntimeClassName()
        {
            var variant = new Mock<ISatisfyRequirementUsage>().Object;
            Assert.That(DiagramNotation.Keyword(variant), Is.EqualTo("satisfy requirement"));
        }

        [TestCase(typeof(SysML2.NET.Core.POCO.Systems.UseCases.IncludeUseCaseUsage), "include use case")]
        [TestCase(typeof(SysML2.NET.Core.POCO.Systems.Occurrences.EventOccurrenceUsage), "event occurrence")]
        [TestCase(typeof(SysML2.NET.Core.POCO.Systems.Views.ViewpointDefinition), "viewpoint def")]
        [TestCase(typeof(SysML2.NET.Core.POCO.Systems.Views.ViewpointUsage), "viewpoint")]
        [TestCase(typeof(SysML2.NET.Core.POCO.Systems.AnalysisCases.AnalysisCaseUsage), "analysis")]
        [TestCase(typeof(SysML2.NET.Core.POCO.Systems.Calculations.CalculationUsage), "calc")]
        [TestCase(typeof(SysML2.NET.Core.POCO.Systems.Metadata.MetadataUsage), "metadata")]
        [TestCase(typeof(SysML2.NET.Core.POCO.Systems.Enumerations.EnumerationUsage), "enum")]
        [TestCase(typeof(ConstraintDefinition), "constraint def")]
        [TestCase(typeof(ConstraintUsage), "constraint")]
        [TestCase(typeof(FlowUsage), "flow")]
        [TestCase(typeof(ConnectionDefinition), "connection def")]
        [TestCase(typeof(RequirementUsage), "requirement")]
        [TestCase(typeof(StateUsage), "state")]
        [TestCase(typeof(SatisfyRequirementUsage), "satisfy requirement")]
        [TestCase(typeof(PerformActionUsage), "perform action")]
        [TestCase(typeof(AssertConstraintUsage), "assert constraint")]
        [TestCase(typeof(ExhibitStateUsage), "exhibit-state")]
        [TestCase(typeof(SuccessionFlowUsage), "succession flow")]
        [TestCase(typeof(PartDefinition), "part def")]
        [TestCase(typeof(PartUsage), "part")]
        [TestCase(typeof(RequirementDefinition), "requirement def")]
        public void VerifyReviewGraphicalKeywordUsesSpecificSdkInterfaces(System.Type type, string expected)
        {
            var element = (IElement)Activator.CreateInstance(type);
            element.Id = Guid.NewGuid();
            element.DeclaredName = "Example";
            Assert.That(DiagramNotation.Name(element).Keyword, Is.EqualTo(expected));
        }

        [Test]
        public void VerifyReviewFitIncludesAllTwoHundredNodesAtNativeZoom()
        {
            var projection = GeneralViewProjector.Create(Enumerable.Range(0, 200).Select(index => new PartUsage { Id = Guid.NewGuid() }));
            using var owner = new GeneralDiagramViewModel(projection, new ContextAwareService());
            using var renderer = new GeneralDiagramRenderer(owner);
            renderer.Diagram.SetContainer(new Rectangle(0, 0, 1000, 700));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.Viewport.Zoom, Is.LessThan(0.05).And.GreaterThan(0));
                Assert.That(renderer.Diagram.Zoom, Is.EqualTo(owner.Viewport.Zoom));
                Assert.That(renderer.Diagram.Options.Zoom.Minimum, Is.LessThanOrEqualTo(owner.Viewport.Zoom));
                Assert.That(projection.Nodes.All(node => node.Occurrence.Y * owner.Viewport.Zoom + owner.Viewport.PanY >= 0
                    && (node.Occurrence.Y + node.Occurrence.Height) * owner.Viewport.Zoom + owner.Viewport.PanY <= 700), Is.True);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void VerifyReviewForeignBoundaryAssociationIsRejected(bool source)
        {
            var projection = GeneralViewExample.Create();
            var edge = projection.Edges.Single(value => value.Kind == DiagramRelationshipKind.Connection);
            var foreign = new PortUsage { Id = edge.SourceFeature.Id, ElementId = edge.SourceFeature.ElementId };
            var invalid = source ? edge with { SourceFeature = foreign } : edge with { TargetFeature = foreign };
            Assert.Throws<ArgumentException>(() =>
            {
                using var owner = new GeneralDiagramViewModel(projection with { Edges = [invalid] }, new ContextAwareService());
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void VerifyAnnotationReportsOnlyUnavailableTargets(bool includeTarget)
        {
            var comment = new Comment { Id = Guid.NewGuid(), Body = "Annotation" };
            var target = new PartUsage { Id = Guid.NewGuid() };
            BrowserAcceptanceModelTestFactory.AddRelationship(comment, new Annotation { AnnotatedElement = target });
            IElement[] elements = includeTarget ? [comment, target] : [comment];
            var projection = GeneralViewProjector.Create(elements);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(projection.HasOmittedContent, Is.EqualTo(!includeTarget));
                Assert.That(projection.Edges, Has.Length.EqualTo(includeTarget ? 1 : 0));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void VerifyNestedOwnedMembershipOmissionIsAccurate(bool includeNestedMember)
        {
            var root = new Package { Id = Guid.NewGuid() };
            var child = new Package { Id = Guid.NewGuid() };
            var nested = new PartUsage { Id = Guid.NewGuid() };
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(root, child);
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(child, nested);
            var projection = includeNestedMember ? GeneralViewProjector.Create([root, child, nested]) : GeneralViewProjector.CreateForRoot(root);
            using (Assert.EnterMultipleScope())
            {
                // Namespace ownership itself is not drawn, even when all member nodes are present.
                Assert.That(projection.HasOmittedContent, Is.True);
                Assert.That(projection.Edges, Is.Empty);
                Assert.That(projection.Nodes, Has.Length.EqualTo(includeNestedMember ? 3 : 2));
            }
        }

        [Test]
        public void VerifyInlineAttributesAndBoundaryFeaturesDoNotInventMissingMemberships()
        {
            var part = new PartUsage { Id = Guid.NewGuid() };
            var attribute = new AttributeUsage { Id = Guid.NewGuid(), DeclaredName = "mass", IsComposite = true };
            var port = new PortUsage { Id = Guid.NewGuid(), DeclaredName = "power" };
            BrowserAcceptanceModelTestFactory.Own<FeatureMembership>(part, attribute);
            BrowserAcceptanceModelTestFactory.Own<FeatureMembership>(part, port);
            var projection = GeneralViewProjector.Create([part]);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(projection.Nodes, Has.Length.EqualTo(1));
                Assert.That(projection.Nodes[0].Compartments.Single().Rows, Has.Length.EqualTo(1));
                Assert.That(projection.Nodes[0].Boundary.Single().Element, Is.SameAs(port));
                Assert.That(projection.Edges, Is.Empty);
                Assert.That(projection.HasOmittedContent, Is.False);
            }
        }

        [Test]
        public void VerifyExplicitZoomWinsOverPendingInitialFit()
        {
            using var owner = new GeneralDiagramViewModel(GeneralViewExample.Create(), new ContextAwareService());
            using var renderer = new GeneralDiagramRenderer(owner);
            owner.ChangeZoom(200);
            renderer.Diagram.SetContainer(new Rectangle(0, 0, 1000, 700));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.Viewport.IsInitialized, Is.True);
                Assert.That(renderer.Diagram.Zoom, Is.EqualTo(2));
            }
        }

        [Test]
        public void VerifyFullyRepresentedConnectionDoesNotReportOmittedContent()
        {
            var first = new PartUsage { Id = Guid.NewGuid() };
            var second = new PartUsage { Id = Guid.NewGuid() };
            var connection = new ConnectionUsage { Id = Guid.NewGuid(), DeclaredName = "bus" };

            foreach (var target in new[] { first, second })
            {
                var end = new Feature { Id = Guid.NewGuid(), IsEnd = true };
                BrowserAcceptanceModelTestFactory.Own<EndFeatureMembership>(connection, end);
                BrowserAcceptanceModelTestFactory.AddRelationship(end, new ReferenceSubsetting { ReferencedFeature = target });
            }

            var represented = GeneralViewProjector.Create([first, second, connection]);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(represented.Edges, Has.Length.EqualTo(1));
                Assert.That(represented.HasOmittedContent, Is.False);
            }
        }

        [Test]
        public void VerifyUnsupportedReferenceSubsettingDoesNotInventSpecializationNotation()
        {
            var first = new PartUsage { Id = Guid.NewGuid() };
            var second = new PartUsage { Id = Guid.NewGuid() };
            var relationship = new ReferenceSubsetting { ReferencedFeature = second };
            BrowserAcceptanceModelTestFactory.AddRelationship(first, relationship);
            var projection = GeneralViewProjector.Create([first, second, relationship]);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(projection.Edges, Is.Empty);
                Assert.That(projection.HasOmittedContent, Is.True);
            }
        }

        [Test]
        public void VerifyViewportPreservesAdjacentValuesWithoutRedundantNativeNotifications()
        {
            using var owner = new GeneralDiagramViewModel(GeneralViewExample.Create(), new ContextAwareService());
            owner.SetViewport(12, 34, 1.5);
            using var renderer = new GeneralDiagramRenderer(owner);
            var panNotifications = 0;
            var zoomNotifications = 0;
            renderer.Diagram.PanChanged += () => panNotifications++;
            renderer.Diagram.ZoomChanged += () => zoomNotifications++;
            var panX = Math.BitIncrement(12);
            var panY = Math.BitDecrement(34);
            var zoom = Math.BitIncrement(1.5);

            owner.SetViewport(panX, panY, zoom);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(renderer.Diagram.Pan.X, Is.EqualTo(panX));
                Assert.That(renderer.Diagram.Pan.Y, Is.EqualTo(panY));
                Assert.That(renderer.Diagram.Zoom, Is.EqualTo(zoom));
                Assert.That(owner.Viewport, Is.EqualTo(new DiagramViewport(panX, panY, zoom, true)));
                Assert.That(panNotifications, Is.EqualTo(1));
                Assert.That(zoomNotifications, Is.EqualTo(1));
            }

            owner.SetViewport(panX, panY, Math.BitIncrement(zoom));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(renderer.Diagram.Zoom, Is.EqualTo(Math.BitIncrement(zoom)));
                Assert.That(panNotifications, Is.EqualTo(1));
                Assert.That(zoomNotifications, Is.EqualTo(2));
            }

            owner.SetViewport(Math.BitIncrement(panX), Math.BitDecrement(panY), Math.BitIncrement(zoom));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(renderer.Diagram.Pan.X, Is.EqualTo(Math.BitIncrement(panX)));
                Assert.That(renderer.Diagram.Pan.Y, Is.EqualTo(Math.BitDecrement(panY)));
                Assert.That(panNotifications, Is.EqualTo(2));
                Assert.That(zoomNotifications, Is.EqualTo(2));
            }
        }

        [Test]
        public void VerifyNativeGesturesAndControlsShareLiveViewportAndDetachOnUnmount()
        {
            using var owner = new GeneralDiagramViewModel(GeneralViewExample.Create(), new ContextAwareService());
            var renderer = new GeneralDiagramRenderer(owner);
            renderer.Diagram.SetPan(12, 34);
            renderer.Diagram.SetZoom(1.5);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.Viewport, Is.EqualTo(new DiagramViewport(12, 34, 1.5, true)));
                owner.ChangeZoom(200);
                Assert.That(renderer.Diagram.Zoom, Is.EqualTo(2));
                renderer.Dispose();
                var retained = owner.Viewport;
                renderer.Diagram.SetPan(100, 200);
                renderer.Diagram.SetZoom(3);
                Assert.That(owner.Viewport, Is.EqualTo(retained));
                owner.ChangeZoom(250);
                Assert.That(renderer.Diagram.Zoom, Is.EqualTo(3));
            }
        }

        [Test]
        public void VerifyExplicitBoundaryNodeDoesNotDuplicateCanonicalLookup()
        {
            var part = new PartUsage
            {
                Id = Guid.NewGuid()
            };
            var port = new PortUsage
            {
                Id = Guid.NewGuid(),
                Direction = SysML2.NET.Core.Core.Types.FeatureDirectionKind.In
            };
            BrowserAcceptanceModelTestFactory.Own<FeatureMembership>(part, port);
            var projection = GeneralViewProjector.CreateForRoot(part);
            using var owner = new GeneralDiagramViewModel(projection, new ContextAwareService());
            using var renderer = new GeneralDiagramRenderer(owner);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(projection.Nodes, Has.Length.EqualTo(2));
                Assert.That(projection.Nodes.Single(node => ReferenceEquals(node.Occurrence.Element, part)).Boundary.Single().Element, Is.SameAs(port));
            }
        }

        [Test]
        public void VerifyBoundaryAnchorsUseCanonicalReferencesAndGraphicalPortIdentities()
        {
            var projection = GeneralViewExample.Create();
            var part = projection.Nodes.Single(node => node.Occurrence.Element.ElementId == "example-part");
            var first = part.Boundary[0].Element;
            var second = part.Boundary[1].Element;
            // Deliberately collide SDK IDs; anchors must still follow exact canonical references.
            second.Id = first.Id;
            var repeated = part with
            {
                Occurrence = part.Occurrence with
                {
                    Id = Guid.NewGuid()
                }
            };
            using var owner = new GeneralDiagramViewModel(projection with { Nodes = projection.Nodes.Add(repeated) }, new ContextAwareService());
            using var renderer = new GeneralDiagramRenderer(owner);
            var ports = renderer.Diagram.Nodes.SelectMany(node => node.Ports).ToArray();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ports.Select(port => port.Id).Distinct().Count(), Is.EqualTo(ports.Length));
                var connection = renderer.Diagram.Links.OfType<GeneralDiagramLinkModel>().Single(link => link.Presentation.Kind == DiagramRelationshipKind.Connection);
                var anchor = (Blazor.Diagrams.Core.Anchors.SinglePortAnchor)connection.Source;
                Assert.That(anchor.Port.Id, Is.EqualTo(part.Occurrence.Id + "-boundary-1"));
            }
        }

        [Test]
        public void VerifyDanglingEndpointsAndNonfiniteGeometryAreRejected()
        {
            var projection = GeneralViewExample.Create();
            var edge = projection.Edges[0];
            var node = projection.Nodes.First(value => !value.Boundary.IsEmpty);
            var invalidBoundary = node with
            {
                Boundary = node.Boundary.SetItem(0, node.Boundary[0] with { Offset = double.NaN })
            };
            using (Assert.EnterMultipleScope())
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    using var invalidOwner = new GeneralDiagramViewModel(projection with { Edges = [edge with { Target = Guid.NewGuid() }] },
                    new ContextAwareService());
                });
                Assert.Throws<ArgumentException>(() =>
                {
                    using var invalidOwner = new GeneralDiagramViewModel(projection with { Edges = [edge with { Bends = [new DiagramPoint(double.NaN, 0)] }] },
                        new ContextAwareService());
                });
                Assert.Throws<ArgumentException>(() =>
                {
                    using var invalidOwner = new GeneralDiagramViewModel(projection with { Nodes = [invalidBoundary], Edges = [] },
                    new ContextAwareService());
                });
            }
        }

        [Test]
        public void VerifyFitIncludesManualBendsOutsideNodeBounds()
        {
            var projection = GeneralViewExample.Create();
            var bend = new DiagramPoint(-400, 1200);
            projection = projection with
            {
                Edges = projection.Edges.SetItem(0, projection.Edges[0] with { Bends = [bend] })
            };
            using var owner = new GeneralDiagramViewModel(projection, new ContextAwareService());
            using var renderer = new GeneralDiagramRenderer(owner);
            renderer.Diagram.SetContainer(new Rectangle(0, 0, 1000, 700));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(bend.X * renderer.Diagram.Zoom + renderer.Diagram.Pan.X, Is.InRange(0, 1000));
                Assert.That(bend.Y * renderer.Diagram.Zoom + renderer.Diagram.Pan.Y, Is.InRange(0, 700));
            }
        }

        [Test]
        public void VerifyUnsupportedValuesAndBoundaryMembershipDoNotInventNotation()
        {
            var attribute = new AttributeUsage
            {
                Id = Guid.NewGuid(),
                DeclaredName = "computed"
            };
            BrowserAcceptanceModelTestFactory.Own<FeatureValue>(attribute, new OperatorExpression { Id = Guid.NewGuid(), Operator = "unknown-operator" });
            var relationships = attribute.OwnedRelationship.ToArray();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(DiagramNotation.Declaration(attribute, true), Does.Contain("value notation unavailable"));
                Assert.That(attribute.OwnedRelationship, Is.EqualTo(relationships));
                var projection = GeneralViewExample.Create();
                Assert.That(projection.Edges.Where(edge => edge.Kind is DiagramRelationshipKind.CompositeMembership or DiagramRelationshipKind.NoncompositeMembership)
                    .Any(edge => edge.Source == edge.Target), Is.False);
                Assert.That(projection.Nodes.Any(node => node.Occurrence.Element is ConnectionDefinition), Is.True);
                using var first = new GeneralDiagramViewModel(projection, new ContextAwareService());
                using var second = new GeneralDiagramViewModel(projection, new ContextAwareService());
                using var firstRenderer = new GeneralDiagramRenderer(first);
                using var secondRenderer = new GeneralDiagramRenderer(second);
                Assert.That(firstRenderer.Diagram.Links.OfType<GeneralDiagramLinkModel>().Select(link => link.MarkerPrefix)
                    .Intersect(secondRenderer.Diagram.Links.OfType<GeneralDiagramLinkModel>().Select(link => link.MarkerPrefix)), Is.Empty);
            }
        }

        [Test]
        public void VerifyNaryAndUnavailableRelationshipsAreOmittedWithoutFabricatingEdges()
        {
            var first = new PartUsage
            {
                Id = Guid.NewGuid()
            };
            var second = new PartUsage
            {
                Id = Guid.NewGuid()
            };
            var third = new PartUsage
            {
                Id = Guid.NewGuid()
            };
            var dependency = new Dependency();
            dependency.Client.Add(first);
            dependency.Client.Add(second);
            dependency.Supplier.Add(third);
            var projection = GeneralViewProjector.Create([first, second, third, dependency, null]);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(projection.Edges, Is.Empty);
                Assert.That(projection.HasOmittedContent, Is.True);
            }
        }

        [Test]
        public void VerifyLargeModelProjectionIsBoundedAndReportsOmissions()
        {
            var elements = Enumerable.Range(0, 10000).Select(index => new PartUsage { Id = Guid.NewGuid(), ElementId = index.ToString() });
            var projection = GeneralViewProjector.Create(elements);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(projection.Nodes, Has.Length.EqualTo(200));
                Assert.That(projection.HasOmittedContent, Is.True);
            }
        }

        [Test]
        public void VerifyRepeatedOccurrencesKeepGraphicalIdentityAndCanonicalObjectsUnchanged()
        {
            var projection = GeneralViewExample.Create();
            var original = projection.Nodes.First(node => node.Occurrence.Element.ElementId == "example-part");
            var element = original.Occurrence.Element;
            var relationships = element.OwnedRelationship.ToArray();
            var declaration = DiagramNotation.Declaration(element);
            var repeated = original with
            {
                Occurrence = original.Occurrence with
                {
                    Id = Guid.NewGuid(),
                    X = 1800
                }
            };
            using var owner = new GeneralDiagramViewModel(projection with { Nodes = projection.Nodes.Add(repeated) }, new ContextAwareService());
            using var renderer = new GeneralDiagramRenderer(owner);
            owner.Select(element);
            var occurrences = renderer.Diagram.Nodes.OfType<GeneralDiagramNodeModel>().Where(node => ReferenceEquals(node.Presentation.Occurrence.Element, element)).ToArray();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(occurrences, Has.Length.EqualTo(2));
                Assert.That(occurrences.All(node => node.IsSelected), Is.True);
                renderer.Diagram.SetPan(321, 123);
                renderer.Diagram.SetZoom(2);
                Assert.That(element.OwnedRelationship, Is.EqualTo(relationships));
                Assert.That(DiagramNotation.Declaration(element), Is.EqualTo(declaration));
                Assert.That(renderer.Diagram.GetBehavior<Blazor.Diagrams.Core.Behaviors.DragMovablesBehavior>(), Is.Null);
                Assert.That(renderer.Diagram.GetBehavior<Blazor.Diagrams.Core.Behaviors.KeyboardShortcutsBehavior>(), Is.Null);
            }
        }

        [Test]
        public void VerifyNestedStateIsOwnedByTabAndInvalidGeometryIsRejected()
        {
            var projection = GeneralViewExample.Create();
            var nested = projection.Nodes.Single(node => node.NestedView is not null).NestedView;
            var owner = new GeneralDiagramViewModel(projection, new ContextAwareService());
            var child = owner.GetNestedView(Guid.Empty, nested);
            child.ChangeZoom(200);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.Viewport.Zoom, Is.EqualTo(1));
                Assert.That(owner.GetNestedView(Guid.Empty, nested), Is.SameAs(child));
                var repeatedNested = owner.GetNestedView(Guid.NewGuid(), nested);
                Assert.That(repeatedNested, Is.Not.SameAs(child));
                Assert.That(repeatedNested.Viewport.Zoom, Is.EqualTo(1));
                Assert.Throws<ArgumentOutOfRangeException>(() => owner.ChangeZoom(double.NaN));
                owner.Dispose();
                Assert.Throws<ObjectDisposedException>(() => child.Select(nested.Nodes[0].Occurrence.Element));
                var invalid = projection.Nodes[0] with
                {
                    Occurrence = projection.Nodes[0].Occurrence with
                    {
                        Width = double.PositiveInfinity
                    }
                };
                Assert.Throws<ArgumentException>(() =>
                {
                    using var invalidOwner = new GeneralDiagramViewModel(projection with { Nodes = [invalid] }, new ContextAwareService());
                });
            }
        }

        [Test]
        public void VerifyBinaryConnectionsAndBindingsUseCanonicalEndsAndSdkLabels()
        {
            var first = new PartUsage
            {
                Id = Guid.NewGuid(),
                ElementId = "first",
                DeclaredName = "first"
            };
            var second = new PartUsage
            {
                Id = Guid.NewGuid(),
                ElementId = "second",
                DeclaredName = "second"
            };
            var connection = new ConnectionUsage
            {
                Id = Guid.NewGuid(),
                ElementId = "connection",
                DeclaredName = "bus"
            };
            var flow = new SysML2.NET.Core.POCO.Systems.Flows.FlowUsage
            {
                Id = Guid.NewGuid(),
                ElementId = "flow"
            };
            var binding = new BindingConnectorAsUsage
            {
                Id = Guid.NewGuid(),
                ElementId = "binding"
            };
            foreach (var connector in new IElement[]
            {
                connection,
                binding,
                flow
            }

            )
            {
                foreach (var target in new[]
                {
                    first,
                    second
                }

                )
                {
                    var end = new Feature
                    {
                        Id = Guid.NewGuid(),
                        IsEnd = true
                    };
                    BrowserAcceptanceModelTestFactory.Own<EndFeatureMembership>(connector, end);
                    BrowserAcceptanceModelTestFactory.AddRelationship(end, new ReferenceSubsetting { ReferencedFeature = target });
                }
            }

            var projection = GeneralViewProjector.Create([first, second, connection, binding, flow]);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(projection.HasOmittedContent, Is.True);
                Assert.That(projection.Edges.Any(edge => ReferenceEquals(edge.Element, flow)), Is.False);
                Assert.That(projection.Edges.Select(edge => edge.Label), Does.Contain("="));
                Assert.That(projection.Edges.Select(edge => edge.Label), Does.Contain("bus"));
                Assert.That(projection.Nodes, Has.Length.EqualTo(2));
            }
        }

        [Test]
        public void VerifyTypeRelationshipsResolveCanonicalDirectionAndNotation()
        {
            var first = new PartDefinition
            {
                Id = Guid.NewGuid(),
                DeclaredName = "SpecialEngine"
            };
            var second = new PartDefinition
            {
                Id = Guid.NewGuid(),
                DeclaredName = "Engine"
            };
            var specialization = new Subclassification
            {
                Id = Guid.NewGuid(),
                Subclassifier = first,
                Superclassifier = second
            };
            var firstUsage = new PartUsage
            {
                Id = Guid.NewGuid(),
                DeclaredName = "special",
                IsComposite = true
            };
            var secondUsage = new PartUsage
            {
                Id = Guid.NewGuid(),
                DeclaredName = "base",
                IsComposite = true
            };
            var subset = new Subsetting
            {
                Id = Guid.NewGuid(),
                SubsettingFeature = firstUsage,
                SubsettedFeature = secondUsage
            };
            var redefine = new Redefinition
            {
                Id = Guid.NewGuid(),
                RedefiningFeature = firstUsage,
                RedefinedFeature = secondUsage
            };
            var projection = GeneralViewProjector.Create([first, second, firstUsage, secondUsage, specialization, subset, redefine]);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(projection.Edges, Has.Length.EqualTo(3));
                Assert.That(projection.Edges.Single(edge => ReferenceEquals(edge.Element, specialization)).Target,
                    Is.EqualTo(projection.Nodes.Single(node => ReferenceEquals(node.Occurrence.Element, second)).Occurrence.Id));
                Assert.That(projection.Edges.Single(edge => ReferenceEquals(edge.Element, subset)).TargetMarker, Is.EqualTo(DiagramMarker.Triangle));
                Assert.That(projection.Edges.Single(edge => ReferenceEquals(edge.Element, redefine)).TargetMarker, Is.EqualTo(DiagramMarker.BarredTriangle));
            }
        }

        [Test]
        public void VerifyExampleUsesHeterogeneousCanonicalObjectsAndMembershipDiamonds()
        {
            var projection = GeneralViewExample.Create();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(projection.Nodes.Select(node => node.Kind), Does.Contain(DiagramNodeKind.Package));
                Assert.That(projection.Nodes.Select(node => node.Kind), Does.Contain(DiagramNodeKind.Comment));
                Assert.That(projection.Nodes.Select(node => node.Kind), Does.Contain(DiagramNodeKind.Definition));
                Assert.That(projection.Edges.Any(edge => edge.SourceMarker == DiagramMarker.FilledDiamond), Is.True);
                Assert.That(projection.Edges.Any(edge => edge.SourceMarker == DiagramMarker.HollowDiamond), Is.True);
                Assert.That(projection.Edges.Any(edge => edge.Kind == DiagramRelationshipKind.Annotation), Is.True);
                Assert.That(projection.Edges.Any(edge => edge.Kind == DiagramRelationshipKind.Definition), Is.True);
            }
        }

        [Test]
        public void VerifySdkWritesTypedDeclarationsValuesAndVisiblePortLabels()
        {
            var projection = GeneralViewExample.Create();
            var part = projection.Nodes.Single(node => node.Occurrence.Element.ElementId == "example-part");
            var connection = projection.Edges.Single(edge => edge.Kind == DiagramRelationshipKind.Connection);
            Assert.That(connection.SourceLabel.ToString(), Is.EqualTo("source [1] {ordered}"));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(part.Name.Declaration, Does.Contain("engine").And.Contain("Engine"));
                Assert.That(part.Compartments.Single().Title, Is.EqualTo("attributes"));
                Assert.That(part.Compartments.Single().Rows.Single(), Does.Contain("heaters").And.Contain("= 8"));
                Assert.That(part.Boundary.Single(boundary => boundary.Element.ElementId == "example-port").Label, Does.Contain("powerIn").And.Contain("PowerPort"));
                Assert.That(part.Name.Keyword, Is.EqualTo("part"));
            }
        }

        [TestCase(typeof(PartDefinition), "part def", DiagramNodeKind.Definition)]
        [TestCase(typeof(AttributeDefinition), "attribute def", DiagramNodeKind.Definition)]
        [TestCase(typeof(PortDefinition), "port def", DiagramNodeKind.Definition)]
        public void VerifyDefinitionNotation(System.Type type, string keyword, DiagramNodeKind kind)
        {
            var element = (IElement)Activator.CreateInstance(type);
            element.DeclaredName = "Example";
            var node = GeneralViewProjector.Node(element);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(node.Name.Keyword, Is.EqualTo(keyword));
                Assert.That(node.Kind, Is.EqualTo(kind));
            }
        }

        [Test]
        public void VerifyOccurrencesAndTabsDoNotConflateOverlappingSemanticIds()
        {
            var first = new PartUsage
            {
                ElementId = "same",
                DeclaredName = "first"
            };
            var second = new PartUsage
            {
                ElementId = "same",
                DeclaredName = "second"
            };
            var selection = new ContextAwareService();
            using var left = new GeneralDiagramViewModel(GeneralViewProjector.Create([first]), selection);
            using var right = new GeneralDiagramViewModel(GeneralViewProjector.Create([second]), selection);
            left.Select(first);
            right.Select(first);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(selection.SelectedElement, Is.SameAs(first));
                right.Select(second);
                Assert.That(selection.SelectedElement, Is.SameAs(second));
                Assert.That(left.Projection.Nodes[0].Occurrence.Id, Is.Not.EqualTo(right.Projection.Nodes[0].Occurrence.Id));
                left.ChangeZoom(200);
                Assert.That(right.Viewport.Zoom, Is.EqualTo(1));
            }
        }

        [Test]
        public void VerifyRendererRemountPreservesViewportAndReleasesGraph()
        {
            var projection = GeneralViewExample.Create();
            using var owner = new GeneralDiagramViewModel(projection, new ContextAwareService());
            var renderer = new GeneralDiagramRenderer(owner);
            renderer.Diagram.SetPan(12, 34);
            renderer.Diagram.SetZoom(1.5);
            renderer.Dispose();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(renderer.Diagram.Nodes, Is.Empty);
                Assert.That(renderer.Diagram.Links, Is.Empty);
                using var remount = new GeneralDiagramRenderer(owner);
                Assert.That(remount.Diagram.Pan, Is.EqualTo(new Point(12, 34)));
                Assert.That(remount.Diagram.Zoom, Is.EqualTo(1.5));
                Assert.That(remount.Diagram.Nodes, Has.Count.EqualTo(projection.Nodes.Length));
                owner.Dispose();
                Assert.Throws<ObjectDisposedException>(() => owner.Select(projection.Nodes[0].Occurrence.Element));
            }
        }
    }
}
