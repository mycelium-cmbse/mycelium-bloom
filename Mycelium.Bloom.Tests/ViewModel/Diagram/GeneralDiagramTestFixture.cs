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
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Blazor.Diagrams.Core.Geometry;

    using Moq;

    using Mycelium.Bloom.Components.UI.Organisms.Diagram;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.DetailsPanel;
    using Mycelium.Bloom.ViewModel.Diagram;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

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
        private readonly DiagramTestServices diagrams = new();

        [TearDown]
        public void DisposeDiagramServices() => this.diagrams.Dispose();

        [Test]
        public void VerifyReviewOwnedDefinitionDoesNotSilentlyLoseNamespaceMembership()
        {
            var package = new Package { Id = Guid.NewGuid() };
            var definition = new PartDefinition { Id = Guid.NewGuid() };
            var membership = BrowserAcceptanceModelTestFactory.Own<OwningMembership>(package, definition);
            var projection = DiagramTestFactory.Create([package, definition]);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(projection.Nodes, Has.Length.EqualTo(2));
                Assert.That(DiagramTestFactory.CreateProjector().GetEdgeNotation(membership), Is.Null);
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
            var projection = DiagramTestFactory.Create([package, first, second, connector]);
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
            var projection = DiagramTestFactory.Create(Enumerable.Range(0, 200).Select(index => new PartUsage { Id = Guid.NewGuid() }));
            using var owner = this.diagrams.CreateViewModel(projection, new ContextAwareService());
            using var renderer = this.diagrams.CreateRenderer(owner);
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
            var projection = GeneralViewExample.Create(DiagramTestFactory.CreateProjector());
            var edge = projection.Edges.Single(value => value.Kind == DiagramRelationshipKind.Connection);
            var foreign = new PortUsage { Id = edge.SourceFeature.Id, ElementId = edge.SourceFeature.ElementId };
            var invalid = source ? edge with { SourceFeature = foreign } : edge with { TargetFeature = foreign };
            Assert.Throws<ArgumentException>(() =>
            {
                using var owner = this.diagrams.CreateViewModel(projection with { Edges = [invalid] }, new ContextAwareService());
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
            var projection = DiagramTestFactory.Create(elements);
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
            var projection = includeNestedMember ? DiagramTestFactory.Create([root, child, nested]) : DiagramTestFactory.CreateForRoot(root);
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
            var projection = DiagramTestFactory.Create([part]);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(projection.Nodes, Has.Length.EqualTo(1));
                Assert.That(((DiagramShellPresentation)projection.Nodes[0]).Compartments.Single().Rows, Has.Length.EqualTo(1));
                Assert.That(((DiagramShellPresentation)projection.Nodes[0]).Boundary.Single().Element, Is.SameAs(port));
                Assert.That(projection.Edges, Is.Empty);
                Assert.That(projection.HasOmittedContent, Is.False);
            }
        }

        [Test]
        public void VerifyExplicitZoomWinsOverPendingInitialFit()
        {
            using var owner = this.diagrams.CreateViewModel(GeneralViewExample.Create(DiagramTestFactory.CreateProjector()), new ContextAwareService());
            using var renderer = this.diagrams.CreateRenderer(owner);
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

            var represented = DiagramTestFactory.Create([first, second, connection]);
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
            var projection = DiagramTestFactory.Create([first, second, relationship]);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(projection.Edges, Is.Empty);
                Assert.That(projection.HasOmittedContent, Is.True);
            }
        }

        [Test]
        public void VerifyViewportPreservesAdjacentValuesWithoutRedundantNativeNotifications()
        {
            using var owner = this.diagrams.CreateViewModel(GeneralViewExample.Create(DiagramTestFactory.CreateProjector()), new ContextAwareService());
            owner.SetViewport(12, 34, 1.5);
            using var renderer = this.diagrams.CreateRenderer(owner);
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
        public void VerifyNativeViewportGesturesAreReportedWithoutReplayingNativeSetters()
        {
            using var owner = this.diagrams.CreateViewModel(GeneralViewExample.Create(DiagramTestFactory.CreateProjector()), new ContextAwareService());
            using var renderer = this.diagrams.CreateRenderer(owner);
            var panNotifications = 0;
            var zoomNotifications = 0;
            renderer.Diagram.PanChanged += () => panNotifications++;
            renderer.Diagram.ZoomChanged += () => zoomNotifications++;

            renderer.Diagram.SetPan(12, 34);
            renderer.Diagram.SetZoom(1.5);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.Viewport, Is.EqualTo(new DiagramViewport(12, 34, 1.5, true)));
                Assert.That(panNotifications, Is.EqualTo(1));
                Assert.That(zoomNotifications, Is.EqualTo(1));
            }
        }

        [Test]
        public void VerifyNativeGesturesAndControlsShareLiveViewportAndDetachOnUnmount()
        {
            using var owner = this.diagrams.CreateViewModel(GeneralViewExample.Create(DiagramTestFactory.CreateProjector()), new ContextAwareService());
            var renderer = this.diagrams.CreateRenderer(owner);
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
            var projection = DiagramTestFactory.CreateForRoot(part);
            using var owner = this.diagrams.CreateViewModel(projection, new ContextAwareService());
            using var renderer = this.diagrams.CreateRenderer(owner);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(projection.Nodes, Has.Length.EqualTo(2));
                Assert.That(projection.Nodes.OfType<DiagramShellPresentation>().Single(node => ReferenceEquals(node.Occurrence.Element, part)).Boundary.Single().Element,
                    Is.SameAs(port));
            }
        }

        [Test]
        public void VerifyBoundaryAnchorsUseCanonicalReferencesAndGraphicalPortIdentities()
        {
            var projection = GeneralViewExample.Create(DiagramTestFactory.CreateProjector());
            var part = projection.Nodes.OfType<DiagramShellPresentation>().Single(node => node.Occurrence.Element.ElementId == "example-part");
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
            using var owner = this.diagrams.CreateViewModel(projection with { Nodes = projection.Nodes.Add(repeated) }, new ContextAwareService());
            using var renderer = this.diagrams.CreateRenderer(owner);
            var ports = renderer.Diagram.Nodes.SelectMany(node => node.Ports).ToArray();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ports.Select(port => port.Id).Distinct().Count(), Is.EqualTo(ports.Length));
                var connection = renderer.Diagram.Links.OfType<DiagramLinkModel>().Single(link => link.Presentation.Kind == DiagramRelationshipKind.Connection);
                var anchor = (Blazor.Diagrams.Core.Anchors.SinglePortAnchor)connection.Source;
                Assert.That(anchor.Port.Id, Is.EqualTo(part.Occurrence.Id + "-boundary-1"));
            }
        }

        [Test]
        public void VerifyDanglingEndpointsAndNonfiniteGeometryAreRejected()
        {
            var projection = GeneralViewExample.Create(DiagramTestFactory.CreateProjector());
            var edge = projection.Edges[0];
            var node = projection.Nodes.OfType<DiagramShellPresentation>().First(value => !value.Boundary.IsEmpty);
            var invalidBoundary = node with
            {
                Boundary = node.Boundary.SetItem(0, node.Boundary[0] with { Offset = double.NaN })
            };
            using (Assert.EnterMultipleScope())
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    using var invalidOwner = this.diagrams.CreateViewModel(projection with { Edges = [edge with { Target = Guid.NewGuid() }] },
                    new ContextAwareService());
                });
                Assert.Throws<ArgumentException>(() =>
                {
                    using var invalidOwner = this.diagrams.CreateViewModel(projection with { Edges = [edge with { Bends = [new DiagramPoint(double.NaN, 0)] }] },
                        new ContextAwareService());
                });
                Assert.Throws<ArgumentException>(() =>
                {
                    using var invalidOwner = this.diagrams.CreateViewModel(projection with { Nodes = [invalidBoundary], Edges = [] },
                    new ContextAwareService());
                });
            }
        }

        [Test]
        public void VerifyFitIncludesManualBendsOutsideNodeBounds()
        {
            var projection = GeneralViewExample.Create(DiagramTestFactory.CreateProjector());
            var bend = new DiagramPoint(-400, 1200);
            projection = projection with
            {
                Edges = projection.Edges.SetItem(0, projection.Edges[0] with { Bends = [bend] })
            };
            using var owner = this.diagrams.CreateViewModel(projection, new ContextAwareService());
            using var renderer = this.diagrams.CreateRenderer(owner);
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
                var projection = GeneralViewExample.Create(DiagramTestFactory.CreateProjector());
                Assert.That(projection.Edges.Where(edge => edge.Kind is DiagramRelationshipKind.CompositeMembership or DiagramRelationshipKind.NoncompositeMembership)
                    .Any(edge => edge.Source == edge.Target), Is.False);
                Assert.That(projection.Nodes.Any(node => node.Occurrence.Element is ConnectionDefinition), Is.True);
                using var first = this.diagrams.CreateViewModel(projection, new ContextAwareService());
                using var second = this.diagrams.CreateViewModel(projection, new ContextAwareService());
                using var firstRenderer = this.diagrams.CreateRenderer(first);
                using var secondRenderer = this.diagrams.CreateRenderer(second);
                Assert.That(firstRenderer.Diagram.Links.OfType<DiagramLinkModel>().Select(link => link.MarkerPrefix)
                    .Intersect(secondRenderer.Diagram.Links.OfType<DiagramLinkModel>().Select(link => link.MarkerPrefix)), Is.Empty);
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
            var projection = DiagramTestFactory.Create([first, second, third, dependency, null]);
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
            var projection = DiagramTestFactory.Create(elements);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(projection.Nodes, Has.Length.EqualTo(200));
                Assert.That(projection.HasOmittedContent, Is.True);
            }
        }

        [Test]
        public void VerifyRepeatedOccurrencesKeepGraphicalIdentityAndCanonicalObjectsUnchanged()
        {
            var projection = GeneralViewExample.Create(DiagramTestFactory.CreateProjector());
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
            using var owner = this.diagrams.CreateViewModel(projection with { Nodes = projection.Nodes.Add(repeated) }, new ContextAwareService());
            using var renderer = this.diagrams.CreateRenderer(owner);
            owner.Select(element);
            var occurrences = renderer.Diagram.Nodes.OfType<DiagramNodeModel>().Where(node => ReferenceEquals(node.Presentation.Occurrence.Element, element)).ToArray();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(occurrences, Has.Length.EqualTo(2));
                Assert.That(occurrences.All(node => node.IsSelected), Is.True);
                renderer.Diagram.SetPan(321, 123);
                renderer.Diagram.SetZoom(2);
                Assert.That(element.OwnedRelationship, Is.EqualTo(relationships));
                Assert.That(DiagramNotation.Declaration(element), Is.EqualTo(declaration));
                Assert.That(renderer.Diagram.GetBehavior<Blazor.Diagrams.Core.Behaviors.DragMovablesBehavior>(), Is.Not.Null);
                Assert.That(renderer.Diagram.GetBehavior<Blazor.Diagrams.Core.Behaviors.KeyboardShortcutsBehavior>(), Is.Null);
            }
        }

        [Test]
        public void VerifyNestedStateIsOwnedByTabAndInvalidGeometryIsRejected()
        {
            var projection = GeneralViewExample.Create(DiagramTestFactory.CreateProjector());
            var nested = projection.Nodes.OfType<DiagramShellPresentation>().Single(node => node.NestedView is not null).NestedView;
            var owner = this.diagrams.CreateViewModel(projection, new ContextAwareService());
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
                    using var invalidOwner = this.diagrams.CreateViewModel(projection with { Nodes = [invalid] }, new ContextAwareService());
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

            var projection = DiagramTestFactory.Create([first, second, connection, binding, flow]);
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
            var projection = DiagramTestFactory.Create([first, second, firstUsage, secondUsage, specialization, subset, redefine]);
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
            var projection = GeneralViewExample.Create(DiagramTestFactory.CreateProjector());
            using (Assert.EnterMultipleScope())
            {
                Assert.That(projection.Nodes.OfType<DiagramPackagePresentation>(), Is.Not.Empty);
                Assert.That(projection.Nodes.OfType<DiagramCommentPresentation>(), Is.Not.Empty);
                Assert.That(projection.Nodes.OfType<DiagramShellPresentation>().Select(node => node.Kind), Does.Contain(DiagramNodeKind.Definition));
                Assert.That(projection.Edges.Any(edge => edge.SourceMarker == DiagramMarker.FilledDiamond), Is.True);
                Assert.That(projection.Edges.Any(edge => edge.SourceMarker == DiagramMarker.HollowDiamond), Is.True);
                Assert.That(projection.Edges.Any(edge => edge.Kind == DiagramRelationshipKind.Annotation), Is.True);
                Assert.That(projection.Edges.Any(edge => edge.Kind == DiagramRelationshipKind.Definition), Is.True);
            }
        }

        [Test]
        public void VerifyExampleLayoutPreservesProjectedIdentitiesAndNestedCanonicalReferences()
        {
            var actualProjector = DiagramTestFactory.CreateProjector();
            var projector = new Mock<IGeneralViewProjector>(MockBehavior.Strict);
            DiagramProjection source = null;
            DiagramProjection nested = null;
            projector.Setup(value => value.Create(It.IsAny<IEnumerable<IElement>>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns((IEnumerable<IElement> elements, string name, bool omitted) =>
                {
                    var projection = actualProjector.Create(elements, name, omitted);

                    if (name == "FuelContext")
                    {
                        nested = projection;
                    }
                    else
                    {
                        source = projection;
                    }

                    return projection;
                });

            var result = GeneralViewExample.Create(projector.Object);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(source, Is.Not.Null);
                Assert.That(nested, Is.Not.Null);
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(nested.Nodes, Has.Length.EqualTo(1));
                Assert.That(result.Nodes, Has.Length.EqualTo(source.Nodes.Length));
                Assert.That(result.Edges, Has.Length.EqualTo(source.Edges.Length));
                Assert.That(result.ViewName, Is.EqualTo(source.ViewName));
                Assert.That(result.HasOmittedContent, Is.EqualTo(source.HasOmittedContent));
            }

            foreach (var (before, after) in source.Nodes.Zip(result.Nodes))
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(after.GetType(), Is.EqualTo(before.GetType()));
                    Assert.That(after.Occurrence.Id, Is.EqualTo(before.Occurrence.Id));
                    Assert.That(after.Occurrence.Element, Is.SameAs(before.Occurrence.Element));
                    Assert.That(after.Name, Is.SameAs(before.Name));
                }
            }

            foreach (var (before, after) in source.Edges.Zip(result.Edges))
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(after, Is.EqualTo(before with { Bends = after.Bends }));
                    Assert.That(after.Element, Is.SameAs(before.Element));
                }
            }

            var view = result.Nodes.OfType<DiagramShellPresentation>().Single(node => node.Occurrence.Element.ElementId == "example-view");
            var item = result.Nodes.Single(node => node.Occurrence.Element.ElementId == "example-item");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(view.NestedView, Is.SameAs(nested));
                Assert.That(nested.Nodes[0].Occurrence.Element, Is.SameAs(item.Occurrence.Element));
                Assert.That(nested.Nodes[0].Occurrence.Id, Is.Not.EqualTo(item.Occurrence.Id));
            }
        }

        [Test]
        public void VerifySdkWritesTypedDeclarationsValuesAndVisiblePortLabels()
        {
            var projection = GeneralViewExample.Create(DiagramTestFactory.CreateProjector());
            var part = projection.Nodes.OfType<DiagramShellPresentation>().Single(node => node.Occurrence.Element.ElementId == "example-part");
            var connection = projection.Edges.Single(edge => edge.Kind == DiagramRelationshipKind.Connection);
            Assert.That(connection.SourceLabel.ToString(), Is.EqualTo("source [1] {ordered}"));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(part.Name.Declaration, Does.Contain("engine").And.Contain("Engine"));
                Assert.That(part.Compartments.Single().Title, Is.EqualTo("attributes"));
                Assert.That(part.Compartments.Single().Rows.Single().Text, Does.Contain("heaters").And.Contain("= 8"));
                Assert.That(part.Boundary.Single(boundary => boundary.Element.ElementId == "example-port").Label, Does.Contain("powerIn").And.Contain("PowerPort"));
                Assert.That(part.Name.Keyword, Is.EqualTo("part"));
            }
        }

        [Test]
        public void VerifyCompartmentSelectionUsesCanonicalAttributeAndExistingDetails()
        {
            var projection = GeneralViewExample.Create(DiagramTestFactory.CreateProjector());
            var shell = projection.Nodes.OfType<DiagramShellPresentation>().Single(node => node.Occurrence.Element.ElementId == "example-part");
            var attribute = shell.Occurrence.Element.OwnedRelationship.OfType<IMembership>().Select(member => member.MemberElement)
                .OfType<IAttributeUsage>().Single(element => element.ElementId == "example-attribute");
            var relationships = attribute.OwnedRelationship.ToArray();
            using var selection = new ContextAwareService();
            using var owner = this.diagrams.CreateViewModel(projection, selection);
            using var details = new DetailsPanelViewModel(selection);
            var node = owner.GetNodeViewModel(shell.Occurrence.Id);
            var row = node.CompartmentRows[0][0];

            row.Select();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(row.Element, Is.SameAs(attribute));
                Assert.That(selection.SelectedElement, Is.SameAs(attribute));
                Assert.That(details.Presentation.Element, Is.SameAs(attribute));
                Assert.That(row.IsSelected, Is.True);
                Assert.That(node.IsSelected, Is.False);
                Assert.That(row.Presentation.Text, Is.EqualTo(DiagramNotation.Declaration(attribute, true)));
                Assert.That(attribute.OwnedRelationship, Is.EqualTo(relationships));
                Assert.That(owner.Projection, Is.SameAs(projection));
            }
        }

        [Test]
        public async Task VerifyCompartmentSelectionCanBeRevealedInBrowser()
        {
            var projection = GeneralViewExample.Create(DiagramTestFactory.CreateProjector());
            var root = (INamespace)projection.Nodes.OfType<DiagramPackagePresentation>().Single().Occurrence.Element;
            var loader = new Mock<IModelLoaderService>(MockBehavior.Strict);
            loader.Setup(service => service.LoadQuantitiesModel()).Returns(root);
            using var selection = new ContextAwareService();
            using var browser = new ProjectBrowserViewModel(loader.Object, selection);
            var initialized = await browser.InitializeAsync(CancellationToken.None);
            Assert.That(initialized, Is.True);
            using var owner = this.diagrams.CreateViewModel(projection, selection);
            var shell = projection.Nodes.OfType<DiagramShellPresentation>().Single(node => node.Occurrence.Element.ElementId == "example-part");
            var row = owner.GetNodeViewModel(shell.Occurrence.Id).CompartmentRows[0][0];

            row.Select();
            browser.FocusElement(selection.SelectedElement);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(browser.SelectedNode.SourceElement, Is.SameAs(row.Element));
                Assert.That(browser.SelectedNode.ElementId, Is.EqualTo("example-attribute"));
                Assert.That(selection.SelectedElement, Is.SameAs(row.Element));
            }
        }

        [Test]
        public void VerifyCompartmentSelectionDecoratesRepeatedCanonicalOccurrences()
        {
            var part = new PartUsage { Id = Guid.NewGuid(), ElementId = "part", DeclaredName = "part" };
            var attribute = new AttributeUsage { Id = Guid.NewGuid(), ElementId = "attribute", DeclaredName = "value" };
            BrowserAcceptanceModelTestFactory.Own<FeatureMembership>(part, attribute);
            var shell = DiagramTestFactory.Node(part);
            var repeated = shell with { Occurrence = shell.Occurrence with { Id = Guid.NewGuid(), X = 400 } };
            using var selection = new ContextAwareService();
            using var owner = this.diagrams.CreateViewModel(new DiagramProjection([shell, repeated], []), selection);
            var first = owner.GetNodeViewModel(shell.Occurrence.Id).CompartmentRows[0][0];
            var second = owner.GetNodeViewModel(repeated.Occurrence.Id).CompartmentRows[0][0];

            selection.SelectedElement = new AttributeUsage { Id = attribute.Id, ElementId = attribute.ElementId };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.IsSelected, Is.False);
                Assert.That(second.IsSelected, Is.False);
            }

            first.Select();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first, Is.Not.SameAs(second));
                Assert.That(first.IsSelected, Is.True);
                Assert.That(second.IsSelected, Is.True);
                Assert.That(selection.SelectedElement, Is.SameAs(attribute));
            }
        }

        [Test]
        public void VerifyCompartmentRowsReconcileAndDetachFromReplacedProjection()
        {
            var part = new PartUsage { Id = Guid.NewGuid(), DeclaredName = "part" };
            var attribute = new AttributeUsage { Id = Guid.NewGuid(), DeclaredName = "value" };
            BrowserAcceptanceModelTestFactory.Own<FeatureMembership>(part, attribute);
            var shell = DiagramTestFactory.Node(part);
            using var selection = new ContextAwareService();
            DiagramCompartmentRowViewModel row;
            using (var owner = this.diagrams.CreateViewModel(new DiagramProjection([shell], []), selection))
            {
                row = owner.GetNodeViewModel(shell.Occurrence.Id).CompartmentRows[0][0];
                row.Select();
                var updated = shell with { Compartments = [new("attributes", [new("value = 2", attribute), new("additional text")])] };

                owner.UpdateProjection(new DiagramProjection([updated], []));
                var current = owner.GetNodeViewModel(shell.Occurrence.Id).CompartmentRows[0];

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(current[0], Is.SameAs(row));
                    Assert.That(current[0].Presentation.Text, Is.EqualTo("value = 2"));
                    Assert.That(current[0].IsSelected, Is.True);
                    Assert.That(current[1], Is.Null);
                }

                owner.UpdateProjection(new DiagramProjection([shell with { Compartments = [] }], []));
                owner.Select(part);
                row.Select();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(selection.SelectedElement, Is.SameAs(part));
                    Assert.That(row.IsSelected, Is.False);
                    Assert.That(owner.GetNodeViewModel(shell.Occurrence.Id).CompartmentRows, Is.Empty);
                }
            }

            selection.SelectedElement = attribute;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(row.IsSelected, Is.False);
                Assert.That(() => row.Select(), Throws.TypeOf<ObjectDisposedException>());
            }
        }

        [TestCase(typeof(PartDefinition), "part def", DiagramNodeKind.Definition)]
        [TestCase(typeof(AttributeDefinition), "attribute def", DiagramNodeKind.Definition)]
        [TestCase(typeof(PortDefinition), "port def", DiagramNodeKind.Definition)]
        public void VerifyDefinitionNotation(System.Type type, string keyword, DiagramNodeKind kind)
        {
            var element = (IElement)Activator.CreateInstance(type);
            element.DeclaredName = "Example";
            var node = DiagramTestFactory.Node(element);
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
            using var left = this.diagrams.CreateViewModel(DiagramTestFactory.Create([first]), selection);
            using var right = this.diagrams.CreateViewModel(DiagramTestFactory.Create([second]), selection);
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
            var projection = GeneralViewExample.Create(DiagramTestFactory.CreateProjector());
            using var owner = this.diagrams.CreateViewModel(projection, new ContextAwareService());
            var renderer = this.diagrams.CreateRenderer(owner);
            renderer.Diagram.SetPan(12, 34);
            renderer.Diagram.SetZoom(1.5);
            renderer.Dispose();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(renderer.Diagram.Nodes, Is.Empty);
                Assert.That(renderer.Diagram.Links, Is.Empty);
                using var remount = this.diagrams.CreateRenderer(owner);
                Assert.That(remount.Diagram.Pan, Is.EqualTo(new Point(12, 34)));
                Assert.That(remount.Diagram.Zoom, Is.EqualTo(1.5));
                Assert.That(remount.Diagram.Nodes, Has.Count.EqualTo(projection.Nodes.Length));
                owner.Dispose();
                Assert.Throws<ObjectDisposedException>(() => owner.Select(projection.Nodes[0].Occurrence.Element));
            }
        }
    }
}
