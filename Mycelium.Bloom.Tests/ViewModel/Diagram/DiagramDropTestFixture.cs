// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramDropTestFixture.cs" company="Starion Group S.A.">
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

    using Blazor.Diagrams.Core.Anchors;
    using Blazor.Diagrams.Core.Geometry;

    using Mycelium.Bloom.Components.UI.Organisms.Diagram;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.Presentation;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Diagram;
    using Mycelium.Bloom.ViewModel.Diagram.Tools;
    using Mycelium.SDK;

    using ReactiveUI;

    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Kernel.Packages;
    using SysML2.NET.Core.POCO.Root.Dependencies;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Connections;
    using SysML2.NET.Core.POCO.Systems.Parts;
    using SysML2.NET.Core.POCO.Systems.Ports;

    [TestFixture]
    public sealed class DiagramDropTestFixture
    {
        private readonly DiagramTestServices diagrams = new();

        [TearDown]
        public void DisposeDiagramServices() => this.diagrams.Dispose();

        [Test]
        public void VerifyPlacementAvailabilityFollowsLiveToolAndEditingPolicy()
        {
            using var selection = new ContextAwareService();
            using var owner = this.diagrams.CreateViewModel(new DiagramProjection([], []), selection);
            var observed = new List<bool>();
            using var subscription = owner.WhenAnyValue(view => view.CanAddElements).Subscribe(observed.Add);
            owner.IsReadOnly = true;
            Assert.That(owner.AddElement(new PartUsage(), 10, 20), Is.False);
            owner.IsReadOnly = false;
            owner.Tools.TryActivate(DiagramToolDefaults.Pan);
            owner.Tools.TryActivate(DiagramToolDefaults.Move);
            selection.LifecycleState = ProjectLifecycleKind.Archived;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(observed, Is.EqualTo((bool[])[true, false, true, false, true, false]));
                Assert.That(owner.Projection.Nodes, Is.Empty);
                Assert.That(owner.CanAddElements, Is.False);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void VerifyPlacementUsesCustomToolCapability(bool allowsElementDrop)
        {
            var tool = new DiagramToolDefinition("place-existing", "Place existing element", "mouse-pointer-2") { AllowsElementDrop = allowsElementDrop };
            var catalogue = DiagramTestServices.CreateCatalogue(tool);
            var layout = new DiagramToolLayout([new(tool.Id), new(DiagramToolDefaults.Pan)], tool.Id, DiagramToolDefaults.Pan);
            using var selection = new ContextAwareService { LifecycleState = ProjectLifecycleKind.Open };
            var projection = new DiagramProjection([], []);
            using var owner = this.diagrams.CreateViewModel(projection, selection, catalogue, layout);
            var element = new PartUsage();
            Assert.That(owner.Tools.Active.Tool, Is.SameAs(tool));

            var added = owner.AddElement(element, 30, 40);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.CanAddElements, Is.EqualTo(allowsElementDrop));
                Assert.That(added, Is.EqualTo(allowsElementDrop));
                Assert.That(owner.Projection.Nodes, Has.Length.EqualTo(allowsElementDrop ? 1 : 0));
            }

            if (allowsElementDrop)
            {
                Assert.That(owner.Projection.Nodes[0].Occurrence.Element, Is.SameAs(element));
            }
            else
            {
                Assert.That(owner.Projection, Is.SameAs(projection));
            }
        }

        [Test]
        public void VerifyTemporaryPanDeniesPlacementAndRestoresCustomToolPermission()
        {
            var tool = new DiagramToolDefinition("place-existing", "Place existing element", "mouse-pointer-2") { AllowsElementDrop = true };
            var catalogue = DiagramTestServices.CreateCatalogue(tool);
            var layout = new DiagramToolLayout([new(tool.Id), new(DiagramToolDefaults.Pan)], tool.Id, DiagramToolDefaults.Pan);
            using var selection = new ContextAwareService { LifecycleState = ProjectLifecycleKind.Open };
            var projection = new DiagramProjection([], []);
            using var owner = this.diagrams.CreateViewModel(projection, selection, catalogue, layout);
            var active = owner.Tools.Active;
            var navigation = owner.Tools.Navigation;
            var element = new PartUsage();
            var observed = new List<bool>();
            using var subscription = owner.WhenAnyValue(view => view.CanAddElements).Subscribe(observed.Add);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(active.Tool, Is.SameAs(tool));
                Assert.That(navigation, Is.Not.Null);
            }

            owner.Tools.Temporary = navigation;

            var denied = owner.AddElement(element, 30, 40);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.Tools.Active, Is.SameAs(active));
                Assert.That(owner.Tools.Effective, Is.SameAs(navigation));
                Assert.That(owner.CanAddElements, Is.False);
                Assert.That(denied, Is.False);
                Assert.That(owner.Projection, Is.SameAs(projection));
            }

            owner.Tools.Temporary = null;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.Tools.Active, Is.SameAs(active));
                Assert.That(owner.Tools.Effective, Is.SameAs(active));
                Assert.That(owner.CanAddElements, Is.True);
                Assert.That(observed, Is.EqualTo((bool[])[true, false, true]));
            }

            var added = owner.AddElement(element, 30, 40);
            Assert.That(owner.Projection.Nodes, Has.Length.EqualTo(1));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(added, Is.True);
                Assert.That(owner.Projection.Nodes[0].Occurrence.Element, Is.SameAs(element));
            }
        }

        [TestCase(ProjectLifecycleKind.Open, true)]
        [TestCase(ProjectLifecycleKind.Review, false)]
        [TestCase(ProjectLifecycleKind.Archived, false)]
        public void VerifyPlacementMutationRejectsEditingProhibitionWithoutChangingActiveTool(ProjectLifecycleKind lifecycle, bool readOnly)
        {
            var tool = new DiagramToolDefinition("place-existing", "Place existing element", "mouse-pointer-2")
            {
                AllowsElementDrop = true,
                RequiresEditableDiagram = false
            };
            var catalogue = DiagramTestServices.CreateCatalogue(tool);
            var layout = new DiagramToolLayout([new(tool.Id), new(DiagramToolDefaults.Pan)], tool.Id, DiagramToolDefaults.Pan);
            using var selection = new ContextAwareService { LifecycleState = ProjectLifecycleKind.Open };
            var projection = new DiagramProjection([], []);
            using var owner = this.diagrams.CreateViewModel(projection, selection, catalogue, layout);
            var active = owner.Tools.Active;
            var viewport = owner.Viewport;
            var element = new PartUsage();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(active.Tool, Is.SameAs(tool));
                Assert.That(owner.CanAddElements, Is.True);
            }

            owner.IsReadOnly = readOnly;
            selection.LifecycleState = lifecycle;

            var added = owner.AddElement(element, 30, 40);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.Tools.Active, Is.SameAs(active));
                Assert.That(owner.Tools.Effective, Is.SameAs(active));
                Assert.That(owner.CanAddElements, Is.False);
                Assert.That(added, Is.False);
                Assert.That(owner.Projection, Is.SameAs(projection));
                Assert.That(owner.Viewport, Is.SameAs(viewport));
            }

            owner.Tools.Temporary = owner.Tools.Navigation;
            owner.Tools.Temporary = null;

            var stillDenied = owner.AddElement(element, 30, 40);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.Tools.Active, Is.SameAs(active));
                Assert.That(owner.Tools.Effective, Is.SameAs(active));
                Assert.That(owner.CanAddElements, Is.False);
                Assert.That(stillDenied, Is.False);
                Assert.That(owner.Projection, Is.SameAs(projection));
                Assert.That(owner.Viewport, Is.SameAs(viewport));
            }
        }

        [Test]
        public void VerifyDropRetainsResizedGeometryAndCanonicalBoundaryConnections()
        {
            using var selection = new ContextAwareService();
            var package = new Package();
            var source = new PartUsage();
            var target = new PartUsage();
            var port = new PortUsage();
            var connector = new ConnectionUsage();
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(package, source);
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(package, target);
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(source, port);
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(package, connector);

            foreach (var endpoint in new IFeature[] { port, target })
            {
                var end = new Feature { IsEnd = true };
                BrowserAcceptanceModelTestFactory.Own<EndFeatureMembership>(connector, end);
                BrowserAcceptanceModelTestFactory.AddRelationship(end, new ReferenceSubsetting { ReferencedFeature = endpoint });
            }

            var owned = package.OwnedRelationship.ToArray();
            var projection = DiagramTestFactory.Create([source]);
            using var owner = this.diagrams.CreateViewModel(projection, selection);
            owner.SetViewport(20, 30, 2);
            var state = owner.GetNodeViewModel(projection.Nodes[0].Occurrence.Id);
            using var renderer = this.diagrams.CreateRenderer(owner);
            renderer.Diagram.SetContainer(new Rectangle(0, 0, 1000, 700));
            Assert.That(owner.ResizeNode(projection.Nodes[0].Occurrence.Id, 400, 300), Is.True);

            var added = renderer.Pointer.DropElement(target, 340, 190);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(added, Is.True);
                Assert.That(owner.Projection.Nodes, Has.Length.EqualTo(2));
                Assert.That(renderer.Diagram.Links, Has.Count.EqualTo(1));
            }

            var native = (DiagramNodeModel)renderer.Diagram.Nodes[0];
            var placed = owner.GetNodeViewModel(owner.Projection.Nodes[1].Occurrence.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(native.ViewModel, Is.SameAs(state));
                Assert.That(native.Size, Is.EqualTo(new Size(400, 300)));
                Assert.That(native.Ports, Has.Count.EqualTo(1));
                Assert.That(renderer.Diagram.Links[0].Source, Is.TypeOf<SinglePortAnchor>());
                Assert.That(placed.Position, Is.EqualTo(new DiagramPoint(160, 80)));
                Assert.That(placed.CanResize, Is.True);
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(((SinglePortAnchor)renderer.Diagram.Links[0].Source).Port, Is.SameAs(native.Ports[0]));
                Assert.That(native.Ports[0].Position, Is.EqualTo(new Point(native.Position.X + 394, native.Position.Y + 144)));
                Assert.That(((IDiagramBoundaryPortModel)native.Ports[0]).ViewModel.Element, Is.SameAs(port));
                Assert.That(owner.Projection.Edges[0].Element, Is.SameAs(connector));
            }

            Assert.That(placed.ResizeBy(20, 10), Is.True);
            var repeated = renderer.Pointer.DropElement(target, 500, 200);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(repeated, Is.True);
                Assert.That(owner.Projection.Nodes, Has.Length.EqualTo(3));
                Assert.That(owner.Projection.Edges, Has.Length.EqualTo(2));
                Assert.That(placed.Size, Is.EqualTo(new DiagramSize(260, 170)));
                Assert.That(owner.GetNodeViewModel(projection.Nodes[0].Occurrence.Id), Is.SameAs(state));
                Assert.That(state.Size, Is.EqualTo(new DiagramSize(400, 300)));
                Assert.That(owner.Projection.Nodes.Select(node => node.Occurrence.Element), Is.EqualTo(new IElement[] { source, target, target }));
                Assert.That(owner.Projection.Edges, Has.All.Property("Element").SameAs(connector));
                Assert.That(package.OwnedRelationship, Is.EqualTo(owned));
            }
        }

        [Test]
        public void VerifyResizeGestureAndTemporaryPanGuardPlacementWithoutChangingRetainedTool()
        {
            using var selection = new ContextAwareService();
            var element = new PartUsage();
            var projection = DiagramTestFactory.Create([element]);
            using var owner = this.diagrams.CreateViewModel(projection, selection);
            owner.SetViewport(0, 0, 1);
            using var renderer = this.diagrams.CreateRenderer(owner);
            renderer.Diagram.SetContainer(new Rectangle(0, 0, 1000, 700));
            var active = owner.Tools.Active;
            var node = (DiagramNodeModel)renderer.Diagram.Nodes[0];
            renderer.Pointer.BeginResize(node, new Blazor.Diagrams.Core.Events.PointerEventArgs(100, 100, 0, 1, false, false, false, 1, 1, 1, 0, 0, 0, "mouse", true));

            var duringResize = renderer.Pointer.DropElement(element, 300, 200);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(renderer.Pointer.IsInteracting, Is.True);
                Assert.That(duringResize, Is.False);
                Assert.That(owner.Projection, Is.SameAs(projection));
            }

            renderer.Pointer.Cancel();
            renderer.Pointer.KeyChanged(" ", "Space", true);

            var duringPan = renderer.Pointer.DropElement(element, 300, 200);
            var resized = owner.ResizeNode(projection.Nodes[0].Occurrence.Id, 400, 300);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.Tools.Active, Is.SameAs(active));
                Assert.That(node.ViewModel.CanResize, Is.True);
                Assert.That(owner.CanAddElements, Is.False);
                Assert.That(duringPan, Is.False);
                Assert.That(resized, Is.False);
                Assert.That(owner.Projection, Is.SameAs(projection));
            }

            renderer.Pointer.KeyChanged(" ", "Space", false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.Tools.Active, Is.SameAs(active));
                Assert.That(owner.Tools.Effective, Is.SameAs(active));
                Assert.That(owner.CanAddElements, Is.True);
            }

            Assert.That(renderer.Pointer.DropElement(element, 300, 200), Is.True);
        }

        [Test]
        public void VerifyDropConnectsEachExistingOccurrenceWithoutDuplicatingConnectors()
        {
            using var selection = new ContextAwareService();
            var source = new PartUsage();
            var target = new PartUsage();
            var relationship = new Redefinition { RedefiningFeature = source, RedefinedFeature = target };
            using var owner = this.diagrams.CreateViewModel(DiagramTestFactory.Create([source, target, relationship]), selection);
            Assert.That(owner.AddElement(target, 300, 400), Is.True);
            var added = owner.AddElement(source, 600, 400);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(added, Is.True);
                Assert.That(owner.Projection.Edges, Has.Length.EqualTo(4));
                Assert.That(owner.Projection.Edges.Select(edge => (edge.Element, edge.Source, edge.Target)).Distinct().Count(), Is.EqualTo(4));
                Assert.That(owner.Projection.Edges, Has.All.Property("Element").SameAs(relationship));
                Assert.That(owner.Projection.Nodes.Select(node => node.Occurrence.Element), Is.EqualTo(new[] { source, target, target, source }));
            }
        }
        [Test]
        public void VerifyDropAtNativeZoomAndPanRetainsCanonicalObjectAcrossRemounts()
        {
            using var selection = new ContextAwareService();
            var element = new PartUsage { Id = Guid.NewGuid(), DeclaredName = "motor" };
            var selected = new PartUsage();
            selection.SelectedElement = selected;
            using var owner = this.diagrams.CreateViewModel(new DiagramProjection([], []), selection);
            using (var renderer = this.diagrams.CreateRenderer(owner))
            {
                renderer.Diagram.SetContainer(new Rectangle(0, 0, 1000, 700));
                owner.SetViewport(100, -50, 2, false);
                Assert.That(renderer.Pointer.DropElement(element, 340, 190), Is.True);
                var occurrence = owner.Projection.Nodes.Single().Occurrence;

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(occurrence.Element, Is.SameAs(element));
                    Assert.That(owner.GetNodeViewModel(occurrence.Id).Position, Is.EqualTo(new DiagramPoint(120, 120)));
                    Assert.That(owner.Viewport, Is.EqualTo(new DiagramViewport(100, -50, 2, true)));
                    Assert.That(selection.SelectedElement, Is.SameAs(selected));
                    Assert.That(element.OwnedRelationship, Is.Empty);
                    Assert.That(renderer.Diagram.Nodes[0].Position, Is.EqualTo(new Point(120, 120)));
                }
            }

            using var remounted = this.diagrams.CreateRenderer(owner);
            Assert.That(remounted.Diagram.Nodes[0].Position, Is.EqualTo(new Point(120, 120)));
        }

        [Test]
        public void VerifyDropRetainsExistingOccurrenceNestedStateAndCreatesIndependentOccurrence()
        {
            using var selection = new ContextAwareService();
            var element = new PartUsage();
            var child = DiagramTestFactory.Create([new PartUsage()]);
            var presentation = DiagramTestFactory.Node(element) with
            {
                Compartments = [new DiagramCompartmentPresentation("nested", [], GraphicalView: child)]
            };
            using var owner = this.diagrams.CreateViewModel(new DiagramProjection([presentation], []), selection);
            var state = owner.GetNodeViewModel(presentation.Occurrence.Id);
            owner.MoveNode(presentation.Occurrence.Id, 321, 654);
            var nested = owner.GetNestedView(presentation.Occurrence.Id, child);
            nested.SetViewport(12, 34, 0.5);
            Assert.That(owner.AddElement(element, -80, 90), Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.Projection.Nodes, Has.Length.EqualTo(2));
                Assert.That(owner.Projection.Nodes[1].Occurrence.Id, Is.Not.EqualTo(presentation.Occurrence.Id));
                Assert.That(owner.Projection.Nodes[1].Occurrence.Element, Is.SameAs(element));
                Assert.That(owner.GetNodeViewModel(presentation.Occurrence.Id), Is.SameAs(state));
                Assert.That(state.Position, Is.EqualTo(new DiagramPoint(321, 654)));
                Assert.That(owner.GetNestedView(presentation.Occurrence.Id, child), Is.SameAs(nested));
                Assert.That(nested.Viewport, Is.EqualTo(new DiagramViewport(12, 34, 0.5, true)));
            }
        }

        [TestCase(typeof(ConnectionUsage))]
        [TestCase(typeof(BindingConnectorAsUsage))]
        public void VerifyDropProjectsExistingOwnedBinaryConnectionWithoutSemanticChanges(Type connectorType)
        {
            using var selection = new ContextAwareService();
            var package = new Package();
            var source = new PartUsage();
            var target = new PartUsage();
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(package, source);
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(package, target);
            var connector = (IElement)Activator.CreateInstance(connectorType);
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(package, connector);
            foreach (var endpoint in new[] { source, target })
            {
                var end = new Feature { IsEnd = true };
                BrowserAcceptanceModelTestFactory.Own<EndFeatureMembership>(connector, end);
                BrowserAcceptanceModelTestFactory.AddRelationship(end, new ReferenceSubsetting { ReferencedFeature = endpoint });
            }
            var owned = package.OwnedRelationship.ToArray();
            using var owner = this.diagrams.CreateViewModel(DiagramTestFactory.Create([source]), selection);
            Assert.That(owner.AddElement(target, 300, 400), Is.True);
            var edge = owner.Projection.Edges.Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(edge.Element, Is.SameAs(connector));
                Assert.That(edge.Source, Is.EqualTo(owner.Projection.Nodes[0].Occurrence.Id));
                Assert.That(edge.Target, Is.EqualTo(owner.Projection.Nodes[1].Occurrence.Id));
                Assert.That(package.OwnedRelationship, Is.EqualTo(owned));
                Assert.That(owner.Projection.HasOmittedContent, Is.False);
            }

            var added = owner.AddElement(target, 600, 400);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(added, Is.True);
                Assert.That(owner.Projection.Edges, Has.Length.EqualTo(2));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.Projection.Edges[0], Is.SameAs(edge));
                Assert.That(owner.Projection.Edges[1].Element, Is.SameAs(connector));
                Assert.That(owner.Projection.Edges[1].Target, Is.EqualTo(owner.Projection.Nodes[2].Occurrence.Id));
            }
        }

        [Test]
        public void VerifyRepeatedDropReusesAlreadyRepresentedRelationshipWithoutOwnership()
        {
            using var selection = new ContextAwareService();
            var source = new PartUsage();
            var target = new PartUsage();
            var relationship = new Redefinition { RedefiningFeature = source, RedefinedFeature = target };
            var projection = DiagramTestFactory.Create([source, target, relationship]);
            using var owner = this.diagrams.CreateViewModel(projection, selection);

            var added = owner.AddElement(target, 600, 400);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(added, Is.True);
                Assert.That(owner.Projection.Edges, Has.Length.EqualTo(2));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.Projection.Edges[0], Is.SameAs(projection.Edges[0]));
                Assert.That(owner.Projection.Edges[1].Element, Is.SameAs(relationship));
                Assert.That(owner.Projection.Edges[1].Target, Is.EqualTo(owner.Projection.Nodes[2].Occurrence.Id));
            }
        }

        [TestCase(typeof(ConnectionUsage))]
        [TestCase(typeof(BindingConnectorAsUsage))]
        [TestCase(typeof(Dependency))]
        public void VerifyDropFindsExistingRelationshipOwnedInSiblingNamespace(Type relationshipType)
        {
            using var selection = new ContextAwareService();
            var root = new Package();
            var sibling = new Package();
            var source = new PartUsage();
            var target = new PartUsage();
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(root, source);
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(root, target);
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(root, sibling);
            var relationship = (IElement)Activator.CreateInstance(relationshipType);

            if (relationship is Dependency dependency)
            {
                dependency.Client.Add(source);
                dependency.Supplier.Add(target);
                BrowserAcceptanceModelTestFactory.AddRelationship(sibling, dependency);
            }
            else
            {
                BrowserAcceptanceModelTestFactory.Own<OwningMembership>(sibling, relationship);

                foreach (var endpoint in new[] { source, target })
                {
                    var end = new Feature { IsEnd = true };
                    BrowserAcceptanceModelTestFactory.Own<EndFeatureMembership>(relationship, end);
                    BrowserAcceptanceModelTestFactory.AddRelationship(end, new ReferenceSubsetting { ReferencedFeature = endpoint });
                }
            }

            var rootOwned = root.OwnedRelationship.ToArray();
            var siblingOwned = sibling.OwnedRelationship.ToArray();
            using var owner = this.diagrams.CreateViewModel(DiagramTestFactory.Create([source]), selection);

            var added = owner.AddElement(target, 300, 400);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(added, Is.True);
                Assert.That(owner.Projection.Edges, Has.Length.EqualTo(1));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.Projection.Edges[0].Element, Is.SameAs(relationship));
                Assert.That(root.OwnedRelationship, Is.EqualTo(rootOwned));
                Assert.That(sibling.OwnedRelationship, Is.EqualTo(siblingOwned));
                Assert.That(owner.Projection.Nodes, Has.Length.EqualTo(2));
            }
        }

        [TestCase(ProjectLifecycleKind.Open, false, DiagramToolDefaults.Move, true)]
        [TestCase(ProjectLifecycleKind.Preparation, false, DiagramToolDefaults.Move, true)]
        [TestCase(ProjectLifecycleKind.Open, true, DiagramToolDefaults.Move, false)]
        [TestCase(ProjectLifecycleKind.Open, false, DiagramToolDefaults.Pan, false)]
        [TestCase(ProjectLifecycleKind.Archived, false, DiagramToolDefaults.Move, false)]
        public void VerifyDropUsesExistingLifecycleAndToolPolicy(ProjectLifecycleKind lifecycle, bool readOnly, string tool, bool accepted)
        {
            using var selection = new ContextAwareService { LifecycleState = lifecycle };
            using var owner = this.diagrams.CreateViewModel(new DiagramProjection([], []), selection);
            owner.IsReadOnly = readOnly;
            owner.Tools.TryActivate(tool);
            Assert.That(owner.AddElement(new PartUsage(), 30, 40), Is.EqualTo(accepted));
        }

        [Test]
        public void VerifyUnavailableInvalidUnsupportedAndFullDropsPreserveProjection()
        {
            using var selection = new ContextAwareService();
            var element = new PartUsage();
            var projection = DiagramTestFactory.Create(Enumerable.Range(0, 200).Select(_ => new PartUsage()));
            using var owner = this.diagrams.CreateViewModel(projection, selection);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.AddElement(element, 1, 2), Is.False);
                Assert.That(owner.AddElement(element, double.NaN, 2), Is.False);
                Assert.That(owner.AddElement(null, 1, 2), Is.False);
                Assert.That(owner.Projection, Is.SameAs(projection));
            }
            using var empty = this.diagrams.CreateViewModel(new DiagramProjection([], []), selection);
            Assert.That(empty.AddElement(new ReferenceSubsetting(), 1, 2), Is.False);
            empty.Dispose();
            Assert.That(empty.AddElement(element, 1, 2), Is.False);
        }

        [Test]
        public void VerifyMountedDragTokensPreserveIndependentCanonicalModelsAndRejectReleasedSources()
        {
            var registry = new ElementDragRegistry();
            var first = new PartUsage { Id = Guid.NewGuid() };
            var otherModel = new PartUsage { Id = first.Id };
            var firstToken = Guid.NewGuid();
            var secondToken = Guid.NewGuid();
            registry.Register(firstToken, first);
            registry.Register(secondToken, otherModel);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(registry.Resolve(firstToken.ToString()), Is.SameAs(first));
                Assert.That(registry.Resolve(secondToken.ToString()), Is.SameAs(otherModel));
                Assert.That(new ElementDragRegistry().Resolve(firstToken.ToString()), Is.Null);
            }
            registry.Remove(firstToken);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(registry.Resolve(firstToken.ToString()), Is.Null);
                Assert.That(registry.Resolve("external payload"), Is.Null);
                Assert.That(registry.Resolve(secondToken.ToString()), Is.SameAs(otherModel));
            }
        }
    }
}
