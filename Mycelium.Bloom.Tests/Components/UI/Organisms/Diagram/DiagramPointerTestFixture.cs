// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramPointerTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.Diagram
{
    using System;

    using Blazor.Diagrams;
    using Blazor.Diagrams.Core.Events;
    using Blazor.Diagrams.Core.Geometry;

    using Moq;

    using Mycelium.Bloom.Components.UI.Organisms.Diagram;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Diagram;
    using Mycelium.SDK;

    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Systems.Parts;

    [TestFixture]
    public sealed class DiagramPointerTestFixture
    {
        [Test]
        public void VerifyRejectedProjectionDoesNotObserveCircuitLifecycle()
        {
            var context = new Mock<IContextAwareService>(MockBehavior.Strict);
            context.SetupGet(value => value.LifecycleState).Returns(ProjectLifecycleKind.Open);

            Assert.That(() => new DiagramViewModel(new DiagramProjection(default, []), context.Object), Throws.TypeOf<ArgumentException>());

            context.VerifyGet(value => value.LifecycleState, Times.Never);
        }

        [Test]
        public void VerifyNativeDragAtZoomRetainsOccurrenceAndRefreshesEdge()
        {
            using var selection = new ContextAwareService();
            var source = new PartUsage();
            var target = new PartUsage();
            var edge = new Redefinition { RedefiningFeature = source, RedefinedFeature = target };
            var projection = DiagramTestFactory.Create([source, target, edge]);
            using var owner = new DiagramViewModel(projection, selection);
            Point moved;
            using (var renderer = new DiagramRenderer(owner))
            {
                renderer.Diagram.SetContainer(new Rectangle(0, 0, 1000, 700));
                owner.ChangeZoom(200);
                var node = renderer.Diagram.Nodes[0];
                var original = node.Position;
                moved = new Point(original.X + 30, original.Y + 20);
                var link = renderer.Diagram.Links[0];
                var refreshes = 0;
                link.Changed += _ => refreshes++;
                renderer.Diagram.TriggerPointerDown(node, Pointer(100, 100));
                renderer.Diagram.TriggerPointerMove(null, Pointer(160, 140));
                renderer.Diagram.TriggerPointerUp(null, Pointer(160, 140));

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(node.Position, Is.EqualTo(moved));
                    Assert.That(owner.GetNodeViewModel(projection.Nodes[0].Occurrence.Id).Position, Is.EqualTo(new DiagramPoint(moved.X, moved.Y)));
                    Assert.That(selection.SelectedElement, Is.SameAs(source));
                    Assert.That(refreshes, Is.GreaterThan(0));
                    Assert.That(owner.Projection, Is.SameAs(projection));
                    Assert.That(edge.RedefinedFeature, Is.SameAs(target));
                }
            }

            using var remounted = new DiagramRenderer(owner);
            Assert.That(remounted.Diagram.Nodes[0].Position, Is.EqualTo(moved));
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        public void VerifyPanOverNodesPreservesSelectionAndGeometry(bool temporary, bool middle)
        {
            using var selection = new ContextAwareService();
            var source = new PartUsage();
            var target = new PartUsage();
            using var owner = new DiagramViewModel(DiagramTestFactory.Create([source, target]), selection);
            owner.Select(target);
            owner.SetViewport(0, 0, 2);
            owner.SetTool(temporary || middle ? DiagramTool.Move : DiagramTool.Pan);
            using var renderer = new DiagramRenderer(owner);
            renderer.Pointer.TemporaryPan = temporary;
            var node = renderer.Diagram.Nodes[0];
            var original = node.Position;
            renderer.Diagram.TriggerPointerDown(node, Pointer(100, 100, middle ? 1 : 0));
            renderer.Diagram.TriggerPointerMove(null, Pointer(160, 140));
            renderer.Diagram.TriggerPointerUp(null, Pointer(160, 140));
            renderer.Pointer.TemporaryPan = false;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.Viewport.PanX, Is.EqualTo(60));
                Assert.That(owner.Viewport.PanY, Is.EqualTo(40));
                Assert.That(node.Position, Is.EqualTo(original));
                Assert.That(selection.SelectedElement, Is.SameAs(target));
                Assert.That(owner.ActiveTool, Is.EqualTo(temporary || middle ? DiagramTool.Move : DiagramTool.Pan));
            }
        }

        [TestCase(ProjectLifecycleKind.Preparation, false, true)]
        [TestCase(ProjectLifecycleKind.Open, false, true)]
        [TestCase(ProjectLifecycleKind.Review, false, false)]
        [TestCase(ProjectLifecycleKind.Archived, false, false)]
        [TestCase(ProjectLifecycleKind.Open, true, false)]
        public void VerifyMovementPolicyPreservesReadOnlySelection(ProjectLifecycleKind lifecycle, bool readOnly, bool allowed)
        {
            using var selection = new ContextAwareService();
            selection.LifecycleState = lifecycle;
            var element = new PartUsage();
            using var owner = new DiagramViewModel(DiagramTestFactory.Create([element]), selection);
            owner.IsReadOnly = readOnly;
            using var renderer = new DiagramRenderer(owner);
            var node = renderer.Diagram.Nodes[0];
            var original = node.Position;
            renderer.Diagram.TriggerPointerDown(node, Pointer(0, 0));
            renderer.Diagram.TriggerPointerMove(null, Pointer(20, 40));
            renderer.Diagram.TriggerPointerUp(null, Pointer(20, 40));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.CanMoveNodes, Is.EqualTo(allowed));
                Assert.That(node.Position, Is.EqualTo(allowed ? new Point(original.X + 20, original.Y + 40) : original));
                Assert.That(selection.SelectedElement, Is.SameAs(element));
            }
        }

        [Test]
        public void VerifyPinnedOccurrenceAndInvalidCoordinatesCannotMove()
        {
            using var selection = new ContextAwareService();
            var projection = DiagramTestFactory.Create([new PartUsage()]);
            var pinned = projection.Nodes[0] with { Occurrence = projection.Nodes[0].Occurrence with { IsMovable = false } };
            using var owner = new DiagramViewModel(projection with { Nodes = [pinned] }, selection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.MoveNode(pinned.Occurrence.Id, 30, 40), Is.False);
                Assert.That(owner.MoveNode(pinned.Occurrence.Id, double.NaN, 40), Is.False);
                Assert.That(owner.MoveNode(Guid.NewGuid(), 30, 40), Is.False);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void VerifyCancellationOrPolicyChangeStopsNativeDrag(bool policyChange)
        {
            using var selection = new ContextAwareService();
            using var owner = new DiagramViewModel(DiagramTestFactory.Create([new PartUsage()]), selection);
            using var renderer = new DiagramRenderer(owner);
            var node = renderer.Diagram.Nodes[0];
            var original = node.Position;
            renderer.Diagram.TriggerPointerDown(node, Pointer(0, 0));

            if (policyChange)
            {
                selection.LifecycleState = ProjectLifecycleKind.Review;
            }
            else
            {
                renderer.Pointer.Cancel();
            }

            renderer.Diagram.TriggerPointerMove(null, Pointer(50, 50));
            renderer.Diagram.TriggerPointerUp(null, Pointer(50, 50));
            Assert.That(node.Position, Is.EqualTo(original));
        }

        [Test]
        public void VerifyIndependentTabModesAndLateDisposedCallbacks()
        {
            using var selection = new ContextAwareService();
            var projection = DiagramTestFactory.Create([new PartUsage()]);
            using var first = new DiagramViewModel(projection, selection);
            using var second = new DiagramViewModel(projection, selection);
            first.MoveNode(projection.Nodes[0].Occurrence.Id, 90, 120);
            first.SetTool(DiagramTool.Pan);
            DiagramViewport retained;
            BlazorDiagram diagram;
            DiagramPointerBehavior pointer;
            using (var renderer = new DiagramRenderer(first))
            {
                retained = first.Viewport;
                diagram = renderer.Diagram;
                pointer = renderer.Pointer;
            }

            pointer.Cancel();
            diagram.TriggerPointerDown(null, Pointer(0, 0));
            diagram.TriggerPointerMove(null, Pointer(40, 50));
            using var remount = new DiagramRenderer(first);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.ActiveTool, Is.EqualTo(DiagramTool.Pan));
                Assert.That(second.ActiveTool, Is.EqualTo(DiagramTool.Move));
                Assert.That(remount.Diagram.Nodes[0].Position, Is.EqualTo(new Point(90, 120)));
                Assert.That(second.GetNodeViewModel(projection.Nodes[0].Occurrence.Id).Position, Is.EqualTo(new DiagramPoint(projection.Nodes[0].Occurrence.X, projection.Nodes[0].Occurrence.Y)));
                Assert.That(first.Viewport, Is.SameAs(retained));
            }
        }

        private static PointerEventArgs Pointer(double x, double y, long button = 0) => new(x, y, button, 1, false, false, false, 1, 1, 1, 0, 0, 0, "mouse", true);
    }
}
