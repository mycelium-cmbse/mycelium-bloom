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
    using System.Collections.Generic;
    using System.Linq;

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
    using Mycelium.Bloom.ViewModel.Diagram.Tools;
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
            owner.Tools.TryActivate(temporary || middle ? DiagramToolDefaults.Move : DiagramToolDefaults.Pan);
            using var renderer = new DiagramRenderer(owner);
            if (temporary)
            {
                renderer.Pointer.KeyChanged(" ", "Space", true);
            }

            var node = renderer.Diagram.Nodes[0];
            var original = node.Position;
            renderer.Diagram.TriggerPointerDown(node, Pointer(100, 100, middle ? 1 : 0));
            renderer.Diagram.TriggerPointerMove(null, Pointer(160, 140));
            renderer.Diagram.TriggerPointerUp(null, Pointer(160, 140));
            if (temporary)
            {
                renderer.Pointer.KeyChanged(" ", "Space", false);
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.Viewport.PanX, Is.EqualTo(60));
                Assert.That(owner.Viewport.PanY, Is.EqualTo(40));
                Assert.That(node.Position, Is.EqualTo(original));
                Assert.That(selection.SelectedElement, Is.SameAs(target));
                Assert.That(owner.Tools.Active.Id, Is.EqualTo(temporary || middle ? DiagramToolDefaults.Move : DiagramToolDefaults.Pan));
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
            using var renderer = new DiagramRenderer(owner);
            var native = renderer.Diagram.Nodes[0];
            var original = native.Position;
            renderer.Diagram.TriggerPointerDown(native, Pointer(10, 20));
            renderer.Diagram.TriggerPointerMove(null, Pointer(55, 65));
            renderer.Diagram.TriggerPointerUp(null, Pointer(55, 65));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(native.Position, Is.EqualTo(original));
                Assert.That(selection.SelectedElement, Is.SameAs(pinned.Occurrence.Element));
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

        [TestCase(false)]
        [TestCase(true)]
        public void VerifyDispatcherReleasesOnlyItsPointerAndCompletesNativeDragOnce(bool dispose)
        {
            using var selection = new ContextAwareService();
            using var owner = new DiagramViewModel(DiagramTestFactory.Create([new PartUsage()]), selection);
            var renderer = new DiagramRenderer(owner);
            var node = renderer.Diagram.Nodes[0];
            var original = node.Position;
            var start = Pointer(12, 34);
            var completions = new List<PointerEventArgs>();
            var moved = new Point(original.X + 10, original.Y + 10);

            using (renderer)
            {
                renderer.Diagram.PointerUp += (_, args) => completions.Add(args);
                renderer.Diagram.TriggerPointerDown(node, start);
                Assert.That(renderer.Pointer.CancelPointer(start.PointerId + 1), Is.False);
                renderer.Diagram.TriggerPointerMove(null, Pointer(22, 44));
                Assert.That(node.Position, Is.EqualTo(moved));

                if (!dispose)
                {
                    Assert.That(renderer.Pointer.CancelPointer(start.PointerId), Is.True);
                    renderer.Diagram.TriggerPointerMove(null, Pointer(60, 60));
                }
            }

            renderer.Pointer.Cancel();
            renderer.Diagram.TriggerPointerMove(null, Pointer(60, 60));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(completions, Has.Count.EqualTo(1));
                Assert.That(completions.SingleOrDefault(), Is.SameAs(start));
                Assert.That(node.Position, Is.EqualTo(moved));
                Assert.That(node.Selected, Is.False);
            }
        }

        [Test]
        public void VerifyIndependentTabModesAndLateDisposedCallbacks()
        {
            using var selection = new ContextAwareService();
            var projection = DiagramTestFactory.Create([new PartUsage()]);
            using var first = new DiagramViewModel(projection, selection);
            using var second = new DiagramViewModel(projection, selection);
            first.MoveNode(projection.Nodes[0].Occurrence.Id, 90, 120);
            first.Tools.TryActivate(DiagramToolDefaults.Pan);
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
            remount.Pointer.KeyChanged(" ", "Space", true);
            pointer.Cancel();
            Assert.That(first.Tools.Temporary?.Id, Is.EqualTo(DiagramToolDefaults.Pan));
            remount.Pointer.KeyChanged(" ", "Space", false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.Tools.Active.Id, Is.EqualTo(DiagramToolDefaults.Pan));
                Assert.That(second.Tools.Active.Id, Is.EqualTo(DiagramToolDefaults.Move));
                Assert.That(remount.Diagram.Nodes[0].Position, Is.EqualTo(new Point(90, 120)));
                Assert.That(second.GetNodeViewModel(projection.Nodes[0].Occurrence.Id).Position, Is.EqualTo(new DiagramPoint(projection.Nodes[0].Occurrence.X, projection.Nodes[0].Occurrence.Y)));
                Assert.That(first.Viewport, Is.SameAs(retained));
            }
        }

        [Test]
        public void VerifyRegisteredRuntimeReceivesInputAndIsDisposedPerMount()
        {
            var events = new List<string>();
            var parameters = new ConnectionParameters("reference");
            var definition = new DiagramToolDefinition("connect", "Connect", "link", (toolContext, options) =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(options, Is.SameAs(parameters));
                    Assert.That(toolContext.Owner.Projection.Kind, Is.EqualTo("general-view"));
                }

                return new RecordingTool(events);
            })
            {
                Parameters = parameters,
                DefaultShortcut = "r",
                RequiresEditableDiagram = true
            };
            var layout = DiagramToolDefaults.Layout with { Items = DiagramToolDefaults.Layout.Items.Add(new(definition.Id)) };
            var catalogue = new DiagramToolCatalogue(DiagramToolDefaults.Definitions.Add(definition));
            using var selection = new ContextAwareService();
            using var owner = new DiagramViewModel(DiagramTestFactory.Create([new PartUsage()]) with { Kind = "general-view" },
                selection, catalogue: catalogue, layout: layout);

            using (var renderer = new DiagramRenderer(owner))
            {
                Assert.That(events, Is.Empty);
                renderer.Pointer.KeyChanged("r", "KeyR", true);
                Assert.That(renderer.Pointer.Cursor, Is.EqualTo("crosshair"));
                renderer.Diagram.TriggerPointerDown(renderer.Diagram.Nodes[0], Pointer(10, 20));
                renderer.Diagram.TriggerPointerMove(null, Pointer(30, 40));
                renderer.Diagram.TriggerPointerUp(null, Pointer(30, 40));
                renderer.Pointer.KeyChanged("Enter", "Enter", true);
                renderer.Diagram.TriggerPointerDown(null, Pointer(50, 60));
                renderer.Pointer.KeyChanged("Escape", "Escape", true);
                var switchedToMove = owner.Tools.TryActivate(DiagramToolDefaults.Move);
                var switchedBack = owner.Tools.TryActivate(definition.Id);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(switchedToMove, Is.True);
                    Assert.That(switchedBack, Is.True);
                }
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(events, Is.EqualTo((string[])["activate", "down", "move", "up", "Enter", "down", "cancel",
                    "cancel", "deactivate", "activate", "cancel", "deactivate", "dispose"]));
                Assert.That(owner.Tools.Active, Is.SameAs(definition));
            }

            using var remounted = new DiagramRenderer(owner);
            Assert.That(events[^1], Is.EqualTo("activate"));
        }

        [Test]
        public void VerifyNativeRelationshipHitSelectsOnlyInMove()
        {
            using var selection = new ContextAwareService();
            var source = new PartUsage();
            var target = new PartUsage();
            var relationship = new Redefinition { RedefiningFeature = source, RedefinedFeature = target };
            using var owner = new DiagramViewModel(DiagramTestFactory.Create([source, target, relationship]), selection);
            using var renderer = new DiagramRenderer(owner);
            var link = renderer.Diagram.Links[0];

            renderer.Diagram.TriggerPointerDown(link, Pointer(2, 4));
            renderer.Diagram.TriggerPointerUp(link, Pointer(2, 4));
            Assert.That(selection.SelectedElement, Is.SameAs(relationship));

            owner.Select(source);
            owner.Tools.TryActivate(DiagramToolDefaults.Pan);
            renderer.Diagram.TriggerPointerDown(link, Pointer(2, 4));
            renderer.Diagram.TriggerPointerUp(link, Pointer(2, 4));
            Assert.That(selection.SelectedElement, Is.SameAs(source));
        }

        [Test]
        public void VerifyPointerDragDoesNotReplayASelectionClick()
        {
            using var selection = new ContextAwareService();
            var dragged = new PartUsage();
            var other = new PartUsage();
            var projection = DiagramTestFactory.Create([dragged, other]);
            using var owner = new DiagramViewModel(projection, selection);
            using var renderer = new DiagramRenderer(owner);
            var first = renderer.Diagram.Nodes[0];
            var otherNode = owner.GetNodeViewModel(projection.Nodes[1].Occurrence.Id);

            renderer.Diagram.TriggerPointerDown(first, Pointer(10, 20));
            renderer.Diagram.TriggerPointerMove(null, Pointer(30, 40));
            renderer.Diagram.TriggerPointerUp(null, Pointer(30, 40));
            otherNode.Select(true);
            Assert.That(selection.SelectedElement, Is.SameAs(dragged));

            renderer.Diagram.TriggerPointerDown(renderer.Diagram.Nodes[1], Pointer(10, 20));
            renderer.Diagram.TriggerPointerUp(null, Pointer(10, 20));
            otherNode.Select(true);
            Assert.That(selection.SelectedElement, Is.SameAs(other));
        }

        private sealed record ConnectionParameters(string Kind) : IDiagramToolParameters;

        private sealed class RecordingTool(List<string> events) : IDiagramTool
        {
            public string Cursor(bool dragging) => "crosshair";
            public void Activate() => events.Add("activate");
            public void Deactivate() => events.Add("deactivate");
            public void PointerDown(Blazor.Diagrams.Core.Models.Base.Model model, PointerEventArgs args) => events.Add("down");
            public void PointerMove(Blazor.Diagrams.Core.Models.Base.Model model, PointerEventArgs args) => events.Add("move");
            public void PointerUp(Blazor.Diagrams.Core.Models.Base.Model model, PointerEventArgs args) => events.Add("up");
            public void KeyChanged(string key, bool pressed) => events.Add(key);
            public void Cancel(PointerEventArgs pointerStart) => events.Add("cancel");
            public void Dispose() => events.Add("dispose");
        }

        private static PointerEventArgs Pointer(double x, double y, long button = 0) => new(x, y, button, 1, false, false, false, 1, 1, 1, 0, 0, 0, "mouse", true);
    }
}
