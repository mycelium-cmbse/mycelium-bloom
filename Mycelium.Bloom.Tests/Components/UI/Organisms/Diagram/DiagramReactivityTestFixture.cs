// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramReactivityTestFixture.cs" company="Starion Group S.A.">
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
    using System.Linq.Expressions;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using Blazor.Diagrams.Components;
    using Blazor.Diagrams.Core.Geometry;

    using Bunit;

    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Components.UI.Organisms.Diagram;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.ViewModel.Diagram;

    using ReactiveUI;

    using Splat;

    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Systems.Parts;

    [TestFixture]
    [NonParallelizable]
    public sealed class DiagramReactivityTestFixture
    {
        private BunitContext context;

        private List<GeneralDiagramViewModel> ownedViews;

        [OneTimeSetUp]
        public void RegisterSubscriptionProbe()
        {
            Locator.CurrentMutable.RegisterConstant<ICreatesObservableForProperty>(new CountingPropertyObservation());
        }

        [OneTimeTearDown]
        public void RemoveSubscriptionProbe()
        {
            Locator.CurrentMutable.UnregisterCurrent<ICreatesObservableForProperty>();
        }

        [SetUp]
        public void SetUp()
        {
            this.context = new BunitContext();
            this.context.SetRendererInfo(new RendererInfo("Server", true));
            this.ownedViews = [];
            this.context.JSInterop.Mode = JSRuntimeMode.Loose;
            this.context.JSInterop.Setup<Rectangle>("ZBlazorDiagrams.getBoundingClientRect", _ => true).SetResult(new Rectangle(0, 0, 1000, 700));
        }

        [TearDown]
        public async Task TearDown()
        {
            try
            {
                await this.context.DisposeAsync();
            }
            finally
            {
                foreach (var owner in this.ownedViews)
                {
                    owner.Dispose();
                }
            }
        }

        [Test]
        public async Task VerifyTwoHundredNodesOnlyRenderChangedSelectionOccurrences()
        {
            var selection = new CountingSelectionService();
            var projection = GeneralViewProjector.Create(Enumerable.Range(0, 200).Select(index => new PartUsage { Id = Guid.NewGuid(), DeclaredName = "Node" + index }));
            var owner = this.CreateOwner(projection, selection);
            var component = this.context.Render<GeneralDiagram>(parameters => parameters.Add(value => value.ViewModel, owner));
            var nodes = component.FindComponents<DiagramNodeWidget>();
            var rootRenders = component.RenderCount;
            var baseline = nodes.Select(node => node.RenderCount).ToArray();

            await component.InvokeAsync(() => owner.Select(projection.Nodes[0].Occurrence.Element));
            component.WaitForAssertion(() => Assert.That(nodes[0].Find("button").GetAttribute("aria-pressed"), Is.EqualTo("true")));
            await component.InvokeAsync(() => owner.Select(projection.Nodes[1].Occurrence.Element));
            component.WaitForAssertion(() => Assert.That(nodes[1].Find("button").GetAttribute("aria-pressed"), Is.EqualTo("true")));

            NUnit.Framework.TestContext.Out.WriteLine($"200 nodes: initial subtree batches={rootRenders}; selection batches={component.RenderCount - rootRenders}; "
                + $"unaffected nodes=198; service subscriptions={selection.ActiveSubscriptions}.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(nodes[0].Find("button").GetAttribute("aria-pressed"), Is.EqualTo("false"));
                Assert.That(nodes[0].RenderCount - baseline[0], Is.EqualTo(2));
                Assert.That(nodes[1].RenderCount - baseline[1], Is.EqualTo(1));
                Assert.That(nodes.Skip(2).Select((node, index) => node.RenderCount - baseline[index + 2]), Is.All.Zero);
                // bUnit counts batches affecting the rendered subtree, including its reactive children.
                Assert.That(component.RenderCount - rootRenders, Is.EqualTo(3));
                Assert.That(selection.ActiveSubscriptions, Is.EqualTo(1));
            }
        }

        [Test]
        public async Task VerifyBoundarySelectionRendersOnlyItsReactiveBoundary()
        {
            var owner = this.CreateOwner(GeneralViewExample.Create(), new CountingSelectionService());
            var component = this.context.Render<GeneralDiagram>(parameters => parameters.Add(value => value.ViewModel, owner));
            var boundary = component.FindComponents<DiagramBoundaryFeature>().First();
            var parent = component.FindComponents<DiagramNodeWidget>().Single(node => node.Instance.ViewModel.BoundaryFeatures.Contains(boundary.Instance.ViewModel));
            var parentRenders = parent.RenderCount;
            await boundary.InvokeAsync(() => boundary.Instance.ViewModel.Select());
            boundary.WaitForAssertion(() => Assert.That(boundary.Find("button").GetAttribute("aria-pressed"), Is.EqualTo("true")));
            Assert.That(parent.RenderCount - parentRenders, Is.EqualTo(1));
        }

        [Test]
        public void VerifySelectionUpdatesRepeatedOccurrencesWithoutCrossingCanonicalModels()
        {
            var selection = new CountingSelectionService();
            var first = GeneralViewProjector.Node(new PartUsage { Id = Guid.NewGuid(), ElementId = "overlap" });
            var repeated = first with { Occurrence = first.Occurrence with { Id = Guid.NewGuid(), X = 500 } };
            var other = GeneralViewProjector.Node(new PartUsage { Id = first.Occurrence.Element.Id, ElementId = "overlap" });
            var owner = this.CreateOwner(new([first, repeated, other], []), selection);
            owner.Select(first.Occurrence.Element);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.GetNodeViewModel(first.Occurrence.Id).IsSelected, Is.True);
                Assert.That(owner.GetNodeViewModel(repeated.Occurrence.Id).IsSelected, Is.True);
                Assert.That(owner.GetNodeViewModel(other.Occurrence.Id).IsSelected, Is.False);
            }
            owner.Select(other.Occurrence.Element);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.GetNodeViewModel(first.Occurrence.Id).IsSelected, Is.False);
                Assert.That(owner.GetNodeViewModel(repeated.Occurrence.Id).IsSelected, Is.False);
                Assert.That(owner.GetNodeViewModel(other.Occurrence.Id).IsSelected, Is.True);
            }
        }

        [Test]
        public async Task VerifyProjectionReplacementUpdatesNotationAndNativeGeometryWithoutMutatingSdk()
        {
            var projection = GeneralViewExample.Create();
            var owner = this.CreateOwner(projection, new CountingSelectionService());
            var original = projection.Nodes.First(node => !node.Boundary.IsEmpty);
            var state = owner.GetNodeViewModel(original.Occurrence.Id);
            var boundaryState = state.BoundaryFeatures[0];
            var canonicalName = state.Element.DeclaredName;
            var component = this.context.Render<GeneralDiagram>(parameters => parameters.Add(value => value.ViewModel, owner));
            var updated = original with
            {
                Occurrence = original.Occurrence with { X = 25, Width = 360 },
                Name = original.Name with { Declaration = "Updated presentation" },
                Boundary = original.Boundary.SetItem(0, original.Boundary[0] with { Label = "Updated port", Offset = 0.25 })
            };
            await component.InvokeAsync(() => owner.UpdateProjection(projection with { Nodes = projection.Nodes.Replace(original, updated), ViewName = "Updated view" }));
            component.WaitForAssertion(() => Assert.That(component.Markup, Does.Contain("Updated presentation").And.Contain("Updated port").And.Contain("Updated view")));
            var widget = component.FindComponents<DiagramNodeWidget>().Single(node => node.Instance.ViewModel == state);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.GetNodeViewModel(original.Occurrence.Id), Is.SameAs(state));
                Assert.That(state.BoundaryFeatures[0], Is.SameAs(boundaryState));
                Assert.That(widget.Instance.Node.Size.Width, Is.EqualTo(360));
                Assert.That(widget.Instance.Node.Position.X, Is.EqualTo(25));
                Assert.That(state.Element.DeclaredName, Is.EqualTo(canonicalName));
                Assert.That(owner.Viewport, Is.EqualTo(new DiagramViewport(0, 0, 1, true)));
            }
        }

        [Test]
        public async Task VerifyRelationshipSelectionAndPresentationUpdateAreReactive()
        {
            var projection = GeneralViewExample.Create();
            var owner = this.CreateOwner(projection, new CountingSelectionService());
            var edge = projection.Edges.First();
            var state = owner.GetEdgeViewModel(edge.Id);
            var component = this.context.Render<GeneralDiagram>(parameters => parameters.Add(value => value.ViewModel, owner));
            await component.InvokeAsync(state.Select);
            Assert.That(state.IsSelected, Is.True);
            var updated = edge with { Label = "Updated edge label" };
            await component.InvokeAsync(() => owner.UpdateProjection(projection with { Edges = projection.Edges.Replace(edge, updated) }));
            component.WaitForAssertion(() => Assert.That(component.Markup, Does.Contain("Updated edge label")));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.GetEdgeViewModel(edge.Id), Is.SameAs(state));
                Assert.That(state.IsSelected, Is.True);
            }
        }

        [Test]
        public void VerifyInvalidSnapshotLeavesReactiveStateAndNativeResourcesUnchanged()
        {
            var projection = GeneralViewExample.Create();
            var owner = this.CreateOwner(projection, new CountingSelectionService());
            using var renderer = new GeneralDiagramRenderer(owner);
            var state = owner.GetNodeViewModel(projection.Nodes[0].Occurrence.Id);
            var native = renderer.Diagram.Nodes[0];
            var invalid = projection with { Edges = projection.Edges.SetItem(0, projection.Edges[0] with { Source = Guid.NewGuid() }) };
            Assert.Throws<ArgumentException>(() => owner.UpdateProjection(invalid));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.Projection, Is.SameAs(projection));
                Assert.That(owner.GetNodeViewModel(projection.Nodes[0].Occurrence.Id), Is.SameAs(state));
                Assert.That(renderer.Diagram.Nodes[0], Is.SameAs(native));
            }
        }

        [Test]
        public async Task VerifyUnmountRemountAndTabCloseOwnSubscriptionsCorrectly()
        {
            var selection = new CountingSelectionService();
            var projection = GeneralViewProjector.Create([new PartUsage { Id = Guid.NewGuid() }]);
            var owner = this.CreateOwner(projection, selection);
            var state = owner.GetNodeViewModel(projection.Nodes[0].Occurrence.Id);
            var first = this.context.Render<GeneralDiagram>(parameters => parameters.Add(value => value.ViewModel, owner));
            first.Dispose();
            var rendersAfterUnmount = first.RenderCount;
            owner.Select(state.Element);
            Assert.That(first.RenderCount, Is.EqualTo(rendersAfterUnmount));
            var second = this.context.Render<GeneralDiagram>(parameters => parameters.Add(value => value.ViewModel, owner));
            second.WaitForAssertion(() => Assert.That(second.FindComponent<DiagramNodeWidget>().Find("button").GetAttribute("aria-pressed"), Is.EqualTo("true")));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(second.FindComponent<DiagramNodeWidget>().Instance.ViewModel, Is.SameAs(state));
                Assert.That(selection.ActiveSubscriptions, Is.EqualTo(1));
            }
            await this.context.Renderer.Dispatcher.InvokeAsync(second.Dispose);
            owner.Dispose();
            selection.SelectedElement = null;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(selection.ActiveSubscriptions, Is.Zero);
                Assert.That(state.IsSelected, Is.True);
                Assert.Throws<ObjectDisposedException>(() => owner.UpdateProjection(projection));
            }
        }

        [Test]
        public void VerifyRemovedNestedViewReleasesItsSubscription()
        {
            var selection = new CountingSelectionService();
            var projection = GeneralViewExample.Create();
            var owner = this.CreateOwner(projection, selection);
            var parent = projection.Nodes.Single(node => node.NestedView is not null);
            var nested = owner.GetNodeViewModel(parent.Occurrence.Id).GetNestedView(parent.NestedView);
            Assert.That(selection.ActiveSubscriptions, Is.EqualTo(2));
            owner.UpdateProjection(projection with { Nodes = projection.Nodes.Replace(parent, parent with { NestedView = null }) });
            using (Assert.EnterMultipleScope())
            {
                Assert.That(selection.ActiveSubscriptions, Is.EqualTo(1));
                Assert.Throws<ObjectDisposedException>(() => nested.Select(parent.NestedView.Nodes[0].Occurrence.Element));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void VerifyNestedViewportIsRetainedOnlyForTheSameCanonicalParent(bool replaceCanonicalParent)
        {
            var selection = new CountingSelectionService();
            var nestedProjection = GeneralViewProjector.Create([new PartUsage { Id = Guid.NewGuid() }]);
            var parentElement = new PartUsage { Id = Guid.NewGuid() };
            var parent = GeneralViewProjector.Node(parentElement) with { NestedView = nestedProjection };
            var owner = this.CreateOwner(new([parent], []), selection);
            var previous = owner.GetNodeViewModel(parent.Occurrence.Id).GetNestedView(nestedProjection);
            previous.SetViewport(47, 91, 1.25);
            var retainedViewport = previous.Viewport;
            var currentElement = replaceCanonicalParent
                ? new PartUsage { Id = parentElement.Id, ElementId = parentElement.ElementId }
                : parentElement;
            var replacement = parent with
            {
                Occurrence = parent.Occurrence with { Element = currentElement },
                Name = parent.Name with { Declaration = "Updated parent presentation" }
            };

            owner.UpdateProjection(new([replacement], []));
            var current = owner.GetNodeViewModel(parent.Occurrence.Id).GetNestedView(nestedProjection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ReferenceEquals(current, previous), Is.EqualTo(!replaceCanonicalParent));
                Assert.That(current.Viewport, Is.EqualTo(replaceCanonicalParent ? new DiagramViewport() : retainedViewport));
                Assert.That(selection.ActiveSubscriptions, Is.EqualTo(2));
            }

            if (replaceCanonicalParent)
            {
                Assert.Throws<ObjectDisposedException>(() => previous.Select(nestedProjection.Nodes[0].Occurrence.Element));
            }
            else
            {
                Assert.DoesNotThrow(() => previous.Select(nestedProjection.Nodes[0].Occurrence.Element));
            }
        }

        [Test]
        public async Task VerifyReplacingBorrowedTabStateDetachesOldOccurrenceComponents()
        {
            var selection = new CountingSelectionService();
            var first = this.CreateOwner(GeneralViewProjector.Create([new PartUsage { Id = Guid.NewGuid(), DeclaredName = "First" }]), selection);
            var second = this.CreateOwner(GeneralViewProjector.Create([new PartUsage { Id = Guid.NewGuid(), DeclaredName = "Second" }]), selection);
            var component = this.context.Render<GeneralDiagram>(parameters => parameters.Add(value => value.ViewModel, first));
            var oldNode = component.FindComponent<DiagramNodeWidget>();
            await component.InvokeAsync(() => component.Render(parameters => parameters.Add(value => value.ViewModel, second)));
            var detachedRenders = oldNode.RenderCount;
            await component.InvokeAsync(() => first.Select(first.Projection.Nodes[0].Occurrence.Element));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Markup, Does.Contain("Second").And.Not.Contain(">First<"));
                Assert.That(oldNode.RenderCount, Is.EqualTo(detachedRenders));
                Assert.That(selection.ActiveSubscriptions, Is.EqualTo(2));
            }
            await component.InvokeAsync(() => second.Select(second.Projection.Nodes[0].Occurrence.Element));
            component.WaitForAssertion(() => Assert.That(component.FindComponent<DiagramNodeWidget>().Find("button").GetAttribute("aria-pressed"), Is.EqualTo("true")));
        }

        [Test]
        public async Task VerifyPresentationCanAddAndRemoveOccurrencesWithoutRetainingTheirSelection()
        {
            var selection = new CountingSelectionService();
            var empty = new GeneralViewProjection([], []);
            var owner = this.CreateOwner(empty, selection);
            var component = this.context.Render<GeneralDiagram>(parameters => parameters.Add(value => value.ViewModel, owner));
            var populated = GeneralViewProjector.Create([new PartUsage { Id = Guid.NewGuid(), DeclaredName = "Added" }]);
            await component.InvokeAsync(() => owner.UpdateProjection(populated));
            component.WaitForAssertion(() => Assert.That(component.FindComponents<DiagramNodeWidget>(), Has.Count.EqualTo(1)));
            var state = owner.GetNodeViewModel(populated.Nodes[0].Occurrence.Id);
            await component.InvokeAsync(state.Select);
            await component.InvokeAsync(() => owner.UpdateProjection(empty));
            component.WaitForAssertion(() => Assert.That(component.Markup, Does.Contain("No elements in this General View.")));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindComponents<DiagramNodeWidget>(), Is.Empty);
                Assert.That(state.IsSelected, Is.False);
                Assert.That(selection.ActiveSubscriptions, Is.EqualTo(1));
                Assert.That(selection.SelectedElement, Is.SameAs(state.Element));
            }
        }

        [Test]
        public void VerifyFirstAvailableProjectionFitsAfterEmptyInitialization()
        {
            var owner = new GeneralDiagramViewModel(new([], []), new CountingSelectionService());
            this.ownedViews.Add(owner);
            using var renderer = new GeneralDiagramRenderer(owner);
            renderer.Diagram.SetContainer(new Rectangle(0, 0, 1000, 700));
            Assert.That(owner.Viewport.IsInitialized, Is.False);
            owner.UpdateProjection(GeneralViewProjector.Create(Enumerable.Range(0, 200).Select(index => new PartUsage { Id = Guid.NewGuid(), DeclaredName = "Node" + index })));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.Viewport.IsInitialized, Is.True);
                Assert.That(owner.Viewport.Zoom, Is.LessThan(0.05).And.GreaterThan(0));
                Assert.That(renderer.Diagram.Zoom, Is.EqualTo(owner.Viewport.Zoom));
            }
        }

        [Test]
        public async Task VerifyStaticRenderingDoesNotInitializeNativeInterop()
        {
            var owner = this.CreateOwner(GeneralViewProjector.Create([new PartUsage { Id = Guid.NewGuid(), DeclaredName = "Static node" }], "Static view"),
                new CountingSelectionService());
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object> { [nameof(GeneralDiagram.ViewModel)] = owner });
            await using (var renderer = new HtmlRenderer(this.context.Services, this.context.Services.GetRequiredService<ILoggerFactory>()))
            {
                var markup = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<GeneralDiagram>(parameters)).ToHtmlString());
                Assert.That(markup, Does.Contain("Static view").And.Not.Contain("Static node"));
            }
            Assert.That(this.context.JSInterop.Invocations, Is.Empty);
        }

        [Test]
        public void VerifyCanvasPreservesThirdPartyInheritanceAndNativeHostLifecycle()
        {
            var owner = this.CreateOwner(GeneralViewProjector.Create([new PartUsage { Id = Guid.NewGuid() }]), new CountingSelectionService());
            var component = this.context.Render<GeneralDiagram>(parameters => parameters.Add(value => value.ViewModel, owner));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindComponent<DiagramCanvasHost>().Instance, Is.InstanceOf<DiagramCanvas>());
                Assert.That(component.FindComponent<DiagramNodeWidget>().Instance, Is.InstanceOf<BloomReactiveComponentBase<DiagramNodeViewModel>>());
            }
        }

        private GeneralDiagramViewModel CreateOwner(GeneralViewProjection projection, IElementSelectionService selection)
        {
            var owner = new GeneralDiagramViewModel(projection, selection);
            owner.SetViewport(0, 0, 1);
            this.ownedViews.Add(owner);
            return owner;
        }

        private sealed class CountingSelectionService : ReactiveObject, IElementSelectionService
        {
            private IElement selectedElement;

            public int ActiveSubscriptions { get; set; }

            public IElement SelectedElement
            {
                get => this.selectedElement;
                set => this.RaiseAndSetIfChanged(ref this.selectedElement, value);
            }

        }

        private sealed class CountingPropertyObservation : ICreatesObservableForProperty
        {
            private readonly IROObservableForProperty inner = new();

            public int GetAffinityForObject(Type type, string propertyName) => this.GetAffinityForObject(type, propertyName, false);

            public int GetAffinityForObject(Type type, string propertyName, bool beforeChanged) => type == typeof(CountingSelectionService) ? 100 : 0;

            public IObservable<IObservedChange<object, object>> GetNotificationForProperty(object sender, Expression expression, string propertyName)
                => this.GetNotificationForProperty(sender, expression, propertyName, false, false);

            public IObservable<IObservedChange<object, object>> GetNotificationForProperty(object sender, Expression expression, string propertyName, bool beforeChanged)
                => this.GetNotificationForProperty(sender, expression, propertyName, beforeChanged, false);

            public IObservable<IObservedChange<object, object>> GetNotificationForProperty(object sender, Expression expression, string propertyName, bool beforeChanged,
                bool suppressWarnings)
            {
                // Wrap ReactiveUI's real property-observation extension point; do not replace its event semantics.
                var source = this.inner.GetNotificationForProperty(sender, expression, propertyName, beforeChanged, suppressWarnings);

                return Observable.Create<IObservedChange<object, object>>(observer =>
                {
                    var service = (CountingSelectionService)sender;
                    service.ActiveSubscriptions++;
                    var subscription = source.Subscribe(observer);

                    return Disposable.Create(() =>
                    {
                        subscription.Dispose();
                        service.ActiveSubscriptions--;
                    });
                });
            }
        }
    }
}
