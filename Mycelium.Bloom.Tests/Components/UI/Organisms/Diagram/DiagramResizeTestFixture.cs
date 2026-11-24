// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramResizeTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.Diagram
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Blazor.Diagrams.Core.Anchors;
    using Blazor.Diagrams.Core.Geometry;

    using Bunit;

    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.Components.UI.Organisms.Diagram;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Extensions;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Diagram;
    using Mycelium.Bloom.ViewModel.Diagram.Tools;
    using Mycelium.SDK;

    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Systems.Parts;

    [TestFixture]
    public sealed class DiagramResizeTestFixture
    {
        private readonly DiagramTestServices diagrams = new();

        [TearDown]
        public void DisposeDiagramServices() => this.diagrams.Dispose();

        [Test]
        public async Task VerifyResizeHandleKeyboardUpdatesRenderedRectangle()
        {
            using var selection = new ContextAwareService();
            var projection = DiagramTestFactory.Create([new PartUsage { DeclaredName = "Box" }]);
            using var owner = this.diagrams.CreateViewModel(projection, selection);
            owner.Select(projection.Nodes[0].Occurrence.Element);
            await using var context = new BunitContext();
            context.Services.AddDiagramServices();
            context.SetRendererInfo(new RendererInfo("Server", true));
            context.JSInterop.Mode = JSRuntimeMode.Loose;
            context.JSInterop.Setup<Rectangle>("ZBlazorDiagrams.getBoundingClientRect", _ => true).SetResult(new Rectangle(0, 0, 1000, 700));
            var component = context.Render<DiagramCanvas>(parameters => parameters.Add(value => value.ViewModel, owner));
            var handle = component.Find("button[aria-label^='Resize ']");
            await handle.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });
            await handle.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown", ShiftKey = true });
            await component.WaitForAssertionAsync(() => Assert.That(component.Find("[data-occurrence-id]").GetAttribute("style"), Does.Contain("width:250px;height:161px")));
            var descriptionId = component.Find("[data-diagram-resize]").GetAttribute("aria-describedby");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.GetNodeViewModel(projection.Nodes[0].Occurrence.Id).Size, Is.EqualTo(new DiagramSize(250, 161)));
                Assert.That(component.Find($"#{descriptionId}").GetAttribute("aria-hidden"), Is.EqualTo("true"));
            }
        }

        [Test]
        public async Task VerifyTemporaryPanRetainsHandleWithoutAllowingKeyboardResize()
        {
            using var selection = new ContextAwareService();
            var projection = DiagramTestFactory.Create([new PartUsage()]);
            using var owner = this.diagrams.CreateViewModel(projection, selection);
            owner.Select(projection.Nodes[0].Occurrence.Element);
            await using var context = new BunitContext();
            context.Services.AddDiagramServices();
            context.SetRendererInfo(new RendererInfo("Server", true));
            context.JSInterop.Mode = JSRuntimeMode.Loose;
            context.JSInterop.Setup<Rectangle>("ZBlazorDiagrams.getBoundingClientRect", _ => true).SetResult(new Rectangle(0, 0, 1000, 700));
            var component = context.Render<DiagramCanvas>(parameters => parameters.Add(value => value.ViewModel, owner));
            await component.Find(".general-diagram-canvas").KeyDownAsync(new KeyboardEventArgs { Key = " ", Code = "Space" });
            await component.Find("[data-diagram-resize]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });
            Assert.That(owner.Tools.Temporary, Is.Not.Null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".general-diagram").GetAttribute("data-can-resize"), Is.EqualTo("true"));
                Assert.That(owner.Tools.Temporary.Tool.Id, Is.EqualTo(DiagramToolDefaults.Pan));
                Assert.That(owner.GetNodeViewModel(projection.Nodes[0].Occurrence.Id).Size, Is.EqualTo(new DiagramSize(240, 160)));
            }

            await component.Find(".general-diagram-canvas").KeyUpAsync(new KeyboardEventArgs { Key = " ", Code = "Space" });
            await component.Find("[data-diagram-resize]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });
            Assert.That(owner.GetNodeViewModel(projection.Nodes[0].Occurrence.Id).Size, Is.EqualTo(new DiagramSize(250, 160)));
        }

        [Test]
        public async Task VerifyBrowserFinalResizeAppliesLastCoordinateAndEndsGesture()
        {
            using var selection = new ContextAwareService();
            var projection = DiagramTestFactory.Create([new PartUsage()]);
            using var owner = this.diagrams.CreateViewModel(projection, selection);
            owner.SetViewport(20, 30, 2);
            await using var context = new BunitContext();
            context.Services.AddDiagramServices();
            context.SetRendererInfo(new RendererInfo("Server", true));
            context.JSInterop.Mode = JSRuntimeMode.Loose;
            context.JSInterop.Setup<Rectangle>("ZBlazorDiagrams.getBoundingClientRect", _ => true).SetResult(new Rectangle(0, 0, 1000, 700));
            var component = context.Render<DiagramCanvas>(parameters => parameters.Add(value => value.ViewModel, owner));
            owner.Select(projection.Nodes[0].Occurrence.Element);
            var occurrence = projection.Nodes[0].Occurrence;
            var handle = component.Find("[data-diagram-resize]");
            await handle.PointerDownAsync(new PointerEventArgs { ClientX = 100, ClientY = 100, PointerId = 1, Button = 0, IsPrimary = true });
            await component.Find("[data-node-id]").PointerUpAsync(new PointerEventArgs { ClientX = 180, ClientY = 160, PointerId = 1, Button = 0, IsPrimary = true });
            await component.Find(".diagram-canvas").PointerMoveAsync(new PointerEventArgs { ClientX = 300, ClientY = 300, PointerId = 1, Button = 0, IsPrimary = true });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.GetNodeViewModel(occurrence.Id).Size, Is.EqualTo(new DiagramSize(280, 190)));
                Assert.That(owner.Viewport, Is.EqualTo(new DiagramViewport(20, 30, 2, true)));
            }
        }
        [Test]
        public void VerifyResizeAtZoomRetainsCanonicalProjectionAndRefreshesEdges()
        {
            using var selection = new ContextAwareService();
            var source = new PartUsage();
            var target = new PartUsage();
            var edge = new Redefinition { RedefiningFeature = source, RedefinedFeature = target };
            var projection = DiagramTestFactory.Create([source, target, edge]);
            using var owner = this.diagrams.CreateViewModel(projection, selection);
            owner.Select(source);
            owner.SetViewport(20, 30, 2);
            var occurrence = projection.Nodes[0].Occurrence;
            var expected = new DiagramSize(occurrence.Width + 30, occurrence.Height + 20);

            using (var renderer = this.CreateRenderer(owner))
            {
                var refreshes = 0;
                renderer.Diagram.Links[0].Changed += _ => refreshes++;
                renderer.Pointer.BeginResize(occurrence.Id, Pointer(100, 100));
                renderer.Diagram.TriggerPointerMove(null, Pointer(160, 140));
                renderer.Diagram.TriggerPointerUp(null, Pointer(160, 140));
                var node = renderer.Diagram.Nodes[0];

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(owner.GetNodeViewModel(occurrence.Id).Size, Is.EqualTo(expected));
                    Assert.That(node.Size, Is.EqualTo(new Size(expected.Width, expected.Height)));
                    Assert.That(node.Position, Is.EqualTo(new Point(occurrence.X, occurrence.Y)));
                    Assert.That(owner.Viewport, Is.EqualTo(new DiagramViewport(20, 30, 2, true)));
                    Assert.That(owner.Projection, Is.SameAs(projection));
                    Assert.That(selection.SelectedElement, Is.SameAs(source));
                    Assert.That(edge.RedefiningFeature, Is.SameAs(source));
                    Assert.That(edge.RedefinedFeature, Is.SameAs(target));
                    Assert.That(refreshes, Is.GreaterThan(0));
                }
            }

            using var remount = this.CreateRenderer(owner);
            Assert.That(remount.Diagram.Nodes[0].Size, Is.EqualTo(new Size(expected.Width, expected.Height)));
        }

        [TestCase(ProjectLifecycleKind.Preparation, false, DiagramToolDefaults.Move, true)]
        [TestCase(ProjectLifecycleKind.Open, false, DiagramToolDefaults.Move, true)]
        [TestCase(ProjectLifecycleKind.Review, false, DiagramToolDefaults.Move, false)]
        [TestCase(ProjectLifecycleKind.Archived, false, DiagramToolDefaults.Move, false)]
        [TestCase(ProjectLifecycleKind.Open, true, DiagramToolDefaults.Move, false)]
        [TestCase(ProjectLifecycleKind.Open, false, DiagramToolDefaults.Pan, false)]
        public void VerifyResizePolicy(ProjectLifecycleKind lifecycle, bool readOnly, string tool, bool accepted)
        {
            using var selection = new ContextAwareService { LifecycleState = lifecycle };
            var projection = DiagramTestFactory.Create([new PartUsage()]);
            using var owner = this.diagrams.CreateViewModel(projection, selection);
            owner.IsReadOnly = readOnly;
            owner.Tools.TryActivate(tool);
            var occurrence = projection.Nodes[0].Occurrence;
            var result = owner.ResizeNode(occurrence.Id, 400, 300);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.EqualTo(accepted));
                Assert.That(owner.GetNodeViewModel(occurrence.Id).Size,
                    Is.EqualTo(accepted ? new DiagramSize(400, 300) : new DiagramSize(occurrence.Width, occurrence.Height)));
            }
        }

        [Test]
        public void VerifyOccurrenceDimensionsRemainIndependentAndReconcileCanonicalReplacement()
        {
            using var selection = new ContextAwareService();
            var original = DiagramTestFactory.Node(new PartUsage());
            var repeated = original with { Occurrence = original.Occurrence with { Id = Guid.NewGuid() } };
            var projection = new DiagramProjection([original, repeated], []);
            using var first = this.diagrams.CreateViewModel(projection, selection);
            using var second = this.diagrams.CreateViewModel(projection, selection);
            first.ResizeNode(original.Occurrence.Id, 400, 300);
            first.UpdateProjection(projection with { ViewName = "Updated view" });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.GetNodeViewModel(original.Occurrence.Id).Size, Is.EqualTo(new DiagramSize(400, 300)));
                Assert.That(first.GetNodeViewModel(repeated.Occurrence.Id).Size, Is.EqualTo(new DiagramSize(240, 160)));
                Assert.That(second.GetNodeViewModel(original.Occurrence.Id).Size, Is.EqualTo(new DiagramSize(240, 160)));
            }

            var replacement = original with { Occurrence = original.Occurrence with { Element = new PartUsage() } };
            first.UpdateProjection(projection with { Nodes = [replacement, repeated] });
            Assert.That(first.GetNodeViewModel(original.Occurrence.Id).Size, Is.EqualTo(new DiagramSize(240, 160)));
        }

        [Test]
        public void VerifyResizingParentPreservesReadOnlyNestedViewState()
        {
            using var selection = new ContextAwareService();
            var projection = GeneralViewExample.Create(DiagramTestFactory.CreateProjector());
            var parent = projection.Nodes.OfType<DiagramShellPresentation>().Single(node => node.NestedView is not null);
            using var owner = this.diagrams.CreateViewModel(projection, selection);
            var nested = owner.GetNestedView(parent.Occurrence.Id, parent.NestedView);
            var child = nested.Projection.Nodes[0].Occurrence;
            nested.SetViewport(20, 30, 0.5);
            owner.ResizeNode(parent.Occurrence.Id, 500, 400);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(nested.ResizeNode(child.Id, 300, 200), Is.False);
                Assert.That(owner.GetNestedView(parent.Occurrence.Id, parent.NestedView), Is.SameAs(nested));
                Assert.That(nested.Viewport, Is.EqualTo(new DiagramViewport(20, 30, 0.5, true)));
                Assert.That(nested.GetNodeViewModel(child.Id).Size, Is.EqualTo(new DiagramSize(child.Width, child.Height)));
                Assert.That(owner.GetNodeViewModel(parent.Occurrence.Id).Size, Is.EqualTo(new DiagramSize(500, 400)));
            }
        }

        [Test]
        public void VerifyBoundaryPositionsAndFitUseResizedRectangle()
        {
            using var selection = new ContextAwareService();
            var source = DiagramTestFactory.Node(new PartUsage()) with
            {
                Boundary = [new(new PartUsage(), "left", BoundarySide.Left, 0.25), new(new PartUsage(), "right", BoundarySide.Right, 0.75),
                    new(new PartUsage(), "top", BoundarySide.Top, 0.25), new(new PartUsage(), "bottom", BoundarySide.Bottom, 0.75)]
            };
            using var owner = this.diagrams.CreateViewModel(new DiagramProjection([source], []), selection);
            using var renderer = this.CreateRenderer(owner);
            owner.ResizeNode(source.Occurrence.Id, 800, 600);
            var node = renderer.Diagram.Nodes[0];
            var ports = node.Ports.Select(port => new Point(port.Position.X - node.Position.X + 6, port.Position.Y - node.Position.Y + 6)).ToArray();
            owner.FitToView(1000, 700);
            var expected = new Point[] { new(0, 150), new(800, 450), new(200, 0), new(600, 600) };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ports, Is.EqualTo(expected));
                Assert.That(owner.Viewport.Zoom, Is.LessThan(1));
                Assert.That(owner.GetNodeViewModel(source.Occurrence.Id).Size, Is.EqualTo(new DiagramSize(800, 600)));
            }
        }

        [Test]
        public void VerifyBoundaryLinkRetainsItsPortAndCanonicalTargetDuringResize()
        {
            using var selection = new ContextAwareService();
            var source = new PartUsage();
            var target = new PartUsage();
            var boundary = new PartUsage();
            var projection = DiagramTestFactory.Create([source, target, new Redefinition { RedefiningFeature = source, RedefinedFeature = target }]);
            var sourcePresentation = (DiagramShellPresentation)projection.Nodes.Single(node => node.Occurrence.Id == projection.Edges[0].Source);
            projection = projection with
            {
                Nodes = projection.Nodes.SetItem(projection.Nodes.IndexOf(sourcePresentation), sourcePresentation with
                {
                    Boundary = [new(boundary, "port", BoundarySide.Right, 0.75)]
                }),
                Edges = projection.Edges.SetItem(0, projection.Edges[0] with { SourceFeature = boundary })
            };
            using var owner = this.diagrams.CreateViewModel(projection, selection);
            using var renderer = this.CreateRenderer(owner);
            var node = renderer.Diagram.Nodes.Single(value => value.Id == sourcePresentation.Occurrence.Id.ToString());
            var port = node.Ports[0];
            var link = renderer.Diagram.Links[0];
            var refreshes = 0;
            link.Changed += _ => refreshes++;
            owner.ResizeNode(sourcePresentation.Occurrence.Id, 400, 300);
            owner.Select(((IDiagramBoundaryPortModel)port).ViewModel.Element);

            Assert.That(link.Source, Is.TypeOf<SinglePortAnchor>());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(((SinglePortAnchor)link.Source).Port, Is.SameAs(port));
                Assert.That(port.Position, Is.EqualTo(new Point(node.Position.X + 394, node.Position.Y + 219)));
                Assert.That(selection.SelectedElement, Is.SameAs(boundary));
                Assert.That(owner.Projection, Is.SameAs(projection));
                Assert.That(refreshes, Is.GreaterThan(0));
            }
        }

        [Test]
        public void VerifyProjectionReplacementChangesDimensionsAndBoundaryTopologyTogether()
        {
            using var selection = new ContextAwareService();
            var node = DiagramTestFactory.Node(new PartUsage()) with { Boundary = [new(new PartUsage(), "port", BoundarySide.Right, 0.5)] };
            var projection = new DiagramProjection([node], []);
            using var owner = this.diagrams.CreateViewModel(projection, selection);
            using var renderer = this.CreateRenderer(owner);
            var replacement = node with { Occurrence = node.Occurrence with { Width = 400 }, Boundary = [] };
            owner.UpdateProjection(projection with { Nodes = [replacement] });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(renderer.Diagram.Nodes[0].Ports, Is.Empty);
                Assert.That(renderer.Diagram.Nodes[0].Size.Width, Is.EqualTo(400));
            }
        }

        [Test]
        public void VerifyMinimumInvalidPinnedAndDisposedResizeRequests()
        {
            using var selection = new ContextAwareService();
            var source = DiagramTestFactory.Node(new PartUsage());
            using var owner = this.diagrams.CreateViewModel(new DiagramProjection([source], []), selection);
            owner.ResizeNode(source.Occurrence.Id, -10, -20);
            var minimum = owner.GetNodeViewModel(source.Occurrence.Id).Size;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(minimum, Is.EqualTo(new DiagramSize(80, 60)));
                Assert.That(owner.ResizeNode(source.Occurrence.Id, double.NaN, 400), Is.False);
                Assert.That(owner.ResizeNode(source.Occurrence.Id, 400, double.PositiveInfinity), Is.False);
                Assert.That(owner.ResizeNode(Guid.NewGuid(), 400, 300), Is.False);
            }

            owner.UpdateProjection(new DiagramProjection([source with { Occurrence = source.Occurrence with { IsResizable = false } }], []));
            Assert.That(owner.ResizeNode(source.Occurrence.Id, 400, 300), Is.False);
            owner.Dispose();
            Assert.That(owner.ResizeNode(source.Occurrence.Id, 400, 300), Is.False);
        }

        [Test]
        public void VerifyCancellationPolicyChangeAndDisposalStopResize()
        {
            using var selection = new ContextAwareService();
            var projection = DiagramTestFactory.Create([new PartUsage()]);
            using var owner = this.diagrams.CreateViewModel(projection, selection);
            using var renderer = this.CreateRenderer(owner);
            var occurrence = projection.Nodes[0].Occurrence;
            renderer.Pointer.BeginResize(occurrence.Id, Pointer(0, 0));
            renderer.Pointer.Cancel();
            renderer.Diagram.TriggerPointerMove(null, Pointer(100, 100));
            renderer.Pointer.BeginResize(occurrence.Id, Pointer(0, 0));
            selection.LifecycleState = ProjectLifecycleKind.Review;
            renderer.Diagram.TriggerPointerMove(null, Pointer(100, 100));
            selection.LifecycleState = ProjectLifecycleKind.Open;
            renderer.Pointer.BeginResize(occurrence.Id, Pointer(0, 0));
            renderer.Dispose();
            renderer.Diagram.TriggerPointerMove(null, Pointer(100, 100));

            Assert.That(owner.GetNodeViewModel(occurrence.Id).Size, Is.EqualTo(new DiagramSize(occurrence.Width, occurrence.Height)));
        }
        [TestCase(0.25)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        public void VerifyNativeResizeCoordinatesAtZoomAndPan(double zoom)
        {
            using var selection = new ContextAwareService();
            var projection = DiagramTestFactory.Create([new PartUsage()]);
            using var owner = this.diagrams.CreateViewModel(projection, selection);
            owner.SetViewport(-150, 75, zoom);
            using var renderer = this.CreateRenderer(owner);
            renderer.Diagram.SetContainer(new Rectangle(30, 40, 1030, 740));
            var occurrence = projection.Nodes[0].Occurrence;
            renderer.Pointer.BeginResize(occurrence.Id, Pointer(100, 100));
            renderer.Diagram.TriggerPointerMove(null, Pointer(100 + 30 * zoom, 100 + 20 * zoom));
            renderer.Diagram.TriggerPointerUp(null, Pointer(100 + 40 * zoom, 100 + 25 * zoom));
            renderer.Diagram.TriggerPointerMove(null, Pointer(500, 500));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.GetNodeViewModel(occurrence.Id).Size, Is.EqualTo(new DiagramSize(280, 185)));
                Assert.That(owner.GetNodeViewModel(occurrence.Id).Position, Is.EqualTo(new DiagramPoint(occurrence.X, occurrence.Y)));
                Assert.That(owner.Viewport, Is.EqualTo(new DiagramViewport(-150, 75, zoom, true)));
                Assert.That(renderer.Pointer.IsInteracting, Is.False);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void VerifyHandlePreservesMiddleButtonAndSpaceNavigation(bool space)
        {
            using var selection = new ContextAwareService();
            var projection = DiagramTestFactory.Create([new PartUsage()]);
            using var owner = this.diagrams.CreateViewModel(projection, selection);
            using var renderer = this.CreateRenderer(owner);
            var occurrence = projection.Nodes[0].Occurrence;

            if (space)
            {
                renderer.Pointer.KeyChanged(" ", "Space", true);
            }

            renderer.Pointer.BeginResize(occurrence.Id, Pointer(100, 100, button: space ? 0 : 1));
            renderer.Diagram.TriggerPointerMove(null, Pointer(160, 140));
            renderer.Diagram.TriggerPointerUp(null, Pointer(160, 140));
            renderer.Pointer.KeyChanged(" ", "Space", false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.GetNodeViewModel(occurrence.Id).Size, Is.EqualTo(new DiagramSize(240, 160)));
                Assert.That(owner.Viewport, Is.EqualTo(new DiagramViewport(60, 40, 1, true)));
                Assert.That(owner.Tools.Effective.Tool.Id, Is.EqualTo(DiagramToolDefaults.Move));
                Assert.That(renderer.Pointer.IsInteracting, Is.False);
            }
        }

        [Test]
        public void VerifyCancelledPointerAndToolChangeStopResizeWithoutRollingBackAcceptedDimensions()
        {
            using var selection = new ContextAwareService();
            var projection = DiagramTestFactory.Create([new PartUsage()]);
            using var owner = this.diagrams.CreateViewModel(projection, selection);
            using var renderer = this.CreateRenderer(owner);
            var occurrence = projection.Nodes[0].Occurrence;
            renderer.Pointer.BeginResize(occurrence.Id, Pointer(100, 100));
            renderer.Diagram.TriggerPointerMove(null, Pointer(120, 130));
            var unrelatedCancelled = renderer.Pointer.CancelPointer(2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(unrelatedCancelled, Is.False);
                Assert.That(renderer.Pointer.IsInteracting, Is.True);
            }

            Assert.That(renderer.Pointer.CancelPointer(1), Is.True);
            renderer.Diagram.TriggerPointerMove(null, Pointer(200, 200));
            renderer.Diagram.TriggerPointerUp(null, Pointer(200, 200));
            renderer.Pointer.BeginResize(occurrence.Id, Pointer(100, 100));
            owner.Tools.TryActivate(DiagramToolDefaults.Pan);
            renderer.Diagram.TriggerPointerMove(null, Pointer(300, 300));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.GetNodeViewModel(occurrence.Id).Size, Is.EqualTo(new DiagramSize(260, 190)));
                Assert.That(owner.Viewport, Is.EqualTo(new DiagramViewport(0, 0, 1, true)));
                Assert.That(renderer.Pointer.IsInteracting, Is.False);
            }
        }

        private IDiagramRenderer CreateRenderer(IDiagramViewModel owner)
        {
            if (!owner.Viewport.IsInitialized)
            {
                owner.SetViewport(0, 0, 1);
            }

            var renderer = this.diagrams.CreateRenderer(owner);
            renderer.Diagram.SetContainer(new Rectangle(0, 0, 1000, 700));
            return renderer;
        }

        private static Blazor.Diagrams.Core.Events.PointerEventArgs Pointer(double x, double y, long button = 0)
            => new(x, y, button, 1, false, false, false, 1, 1, 1, 0, 0, 0, "mouse", true);

    }
}
