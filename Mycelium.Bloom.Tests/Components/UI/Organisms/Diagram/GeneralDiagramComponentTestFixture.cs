// ------------------------------------------------------------------------------------------------
// <copyright file="GeneralDiagramComponentTestFixture.cs" company="Starion Group S.A.">
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
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;

    using Blazor.Diagrams.Core.Geometry;

    using Bunit;

    using Microsoft.AspNetCore.Components.Web;
    using Microsoft.Extensions.DependencyInjection;

    using Moq;

    using Mycelium.Bloom.Components.UI.Molecules.ZoomControls;
    using Mycelium.Bloom.Components.UI.Organisms.Diagram;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Extensions;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Diagram;

    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Systems.Parts;
    using SysML2.NET.Core.POCO.Systems.Ports;

    [TestFixture]
    public sealed class GeneralDiagramComponentTestFixture
    {
        private readonly DiagramTestServices diagrams = new();

        [TearDown]
        public void DisposeDiagramServices() => this.diagrams.Dispose();

        private BunitContext context;

        private Mock<IDiagramViewModel> viewModel;

        private List<IDisposable> ownedViewModels;

        [SetUp]
        public void SetUp()
        {
            this.context = new BunitContext();
            this.context.Services.AddDiagramServices();
            this.context.JSInterop.SetupModule("./Components/UI/Organisms/Diagram/DiagramCanvas.razor.js").SetupVoid("ensureDiagramAssets").SetVoidResult();
            this.context.SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Server", true));
            this.viewModel = new Mock<IDiagramViewModel>();
            this.ownedViewModels = [];
            this.context.JSInterop.Mode = JSRuntimeMode.Loose;
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
                foreach (var owned in this.ownedViewModels)
                {
                    owned.Dispose();
                }
            }
        }

        [Test]
        public async Task VerifyCanvasUsesRegisteredRendererAndPointerContracts()
        {
            using var owner = this.diagrams.CreateViewModel(new DiagramProjection([], []), new ContextAwareService());
            var pointer = new Mock<IDiagramPointerBehavior>(MockBehavior.Strict);
            pointer.SetupGet(value => value.Cursor).Returns("crosshair");
            pointer.SetupGet(value => value.Preview).Returns((Microsoft.AspNetCore.Components.RenderFragment)null);
            var renderer = new Mock<IDiagramRenderer>(MockBehavior.Strict);
            renderer.SetupGet(value => value.Diagram).Returns(new Blazor.Diagrams.BlazorDiagram());
            renderer.SetupGet(value => value.Pointer).Returns(pointer.Object);
            renderer.Setup(value => value.Dispose());
            var factory = new Mock<Func<IDiagramViewModel, IDiagramRenderer>>(MockBehavior.Strict);
            factory.Setup(create => create(owner)).Returns(renderer.Object);
            var componentContext = new BunitContext();

            try
            {
                componentContext.Services.AddDiagramServices();
                componentContext.Services.AddSingleton(factory.Object);
                componentContext.SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Server", true));
                componentContext.JSInterop.SetupModule("./Components/UI/Organisms/Diagram/DiagramCanvas.razor.js").SetupVoid("ensureDiagramAssets").SetVoidResult();
                componentContext.JSInterop.Setup<Rectangle>("ZBlazorDiagrams.getBoundingClientRect", _ => true).SetResult(new Rectangle(0, 0, 1000, 700));
                componentContext.JSInterop.Mode = JSRuntimeMode.Loose;

                var component = componentContext.Render<DiagramCanvas>(parameters => parameters.Add(value => value.ViewModel, owner));
                Assert.That(component.Find(".general-diagram").GetAttribute("data-tool-cursor"), Is.EqualTo("crosshair"));
            }
            finally
            {
                await componentContext.DisposeAsync();
            }

            factory.Verify(create => create(owner), Times.Once);
            renderer.Verify(value => value.Dispose(), Times.Once);
            Assert.DoesNotThrow(() => owner.ChangeZoom(175));
        }

        [Test]
        public void VerifyCustomMarkerViewportContainsItsGeometry()
        {
            this.context.JSInterop.Setup<Rectangle>("ZBlazorDiagrams.getBoundingClientRect", _ => true).SetResult(new Rectangle(0, 0, 1000, 700));
            var source = new PartUsage();
            var target = new PartUsage();
            var relationship = new Redefinition { RedefiningFeature = source, RedefinedFeature = target };
            var projection = DiagramTestFactory.CreateProjector().Create([source, target, relationship]);
            var marker = new DiagramMarker("M0 0 L-60 -12 L-60 12 Z", "currentColor") { ViewBox = (-64, -16, 65, 32) };
            var edge = projection.Edges[0];
            projection = projection with { Edges = [edge with { Notation = edge.Notation with { SourceMarker = marker } }] };
            var owner = this.diagrams.CreateViewModel(projection, new ContextAwareService());
            this.ownedViewModels.Add(owner);
            var component = this.context.Render<DiagramCanvas>(parameters => parameters.Add(value => value.ViewModel, owner));
            var viewport = component.FindComponent<DiagramEdge>().Find("marker").GetAttribute("viewBox").Split(' ')
                .Select(value => double.Parse(value, CultureInfo.InvariantCulture)).ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewport[0], Is.LessThanOrEqualTo(-60));
                Assert.That(viewport[1], Is.LessThanOrEqualTo(-12));
                Assert.That(viewport[0] + viewport[2], Is.GreaterThanOrEqualTo(0));
                Assert.That(viewport[1] + viewport[3], Is.GreaterThanOrEqualTo(12));
            }
        }

        [Test]
        public void VerifyTypedPresentationsRenderTheirRegisteredComponents()
        {
            this.context.JSInterop.Setup<Rectangle>("ZBlazorDiagrams.getBoundingClientRect", _ => true).SetResult(new Rectangle(0, 0, 1000, 700));
            var projection = GeneralViewExample.Create(DiagramTestFactory.CreateProjector());
            var owner = this.diagrams.CreateViewModel(projection, new ContextAwareService());
            this.ownedViewModels.Add(owner);
            var component = this.context.Render<DiagramCanvas>(parameters => parameters.Add(value => value.ViewModel, owner));
            var package = component.FindComponent<DiagramPackageNode>();
            var comment = component.FindComponent<DiagramCommentNode>();
            var shells = component.FindComponents<DiagramShellNode>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(package.Instance.Presentation, Is.SameAs(projection.Nodes.OfType<DiagramPackagePresentation>().Single()));
                Assert.That(comment.Instance.Presentation, Is.SameAs(projection.Nodes.OfType<DiagramCommentPresentation>().Single()));
                Assert.That(shells, Is.Not.Empty);
                Assert.That(shells.Select(shell => shell.Instance.Presentation), Has.All.InstanceOf<DiagramShellPresentation>());
            }
        }

        [TestCase("satisfy requirement", "requirement")]
        [TestCase("assert constraint", "constraint")]
        [TestCase("exhibit-state", "state")]
        public void VerifySpecializedNameKeepsBothGraphicalKeywordLines(string prefix, string baseKeyword)
        {
            var presentation = new DiagramNamePresentation(prefix, "Example", [], AdditionalKeywords: [baseKeyword]);
            var component = this.context.Render<DiagramNameCompartment>(parameters => parameters.Add(value => value.Presentation, presentation));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Markup, Does.Contain("«" + prefix + "»"));
                Assert.That(component.Markup, Does.Contain("«" + baseKeyword + "»"));
                Assert.That(component.Markup, Does.Contain("Example"));
            }
        }

        [Test]
        public async Task VerifyDisposalWaitsForPendingCanvasInitialization()
        {
            var initialization = this.context.JSInterop.Setup<Rectangle>("ZBlazorDiagrams.getBoundingClientRect", _ => true);
            var selection = new ContextAwareService();
            var owner = this.diagrams.CreateViewModel(DiagramTestFactory.Create([new PartUsage { Id = Guid.NewGuid() }]), selection);
            this.ownedViewModels.Add(owner);
            var component = this.context.Render<DiagramCanvas>(parameters => parameters.Add(value => value.ViewModel, owner));
            var canvas = component.FindComponent<DiagramCanvasHost>().Instance;
            var disposal = canvas.DisposeAsync().AsTask();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(disposal.IsCompleted, Is.False);
                initialization.SetResult(new Rectangle(0, 0, 1000, 700));
                await disposal;
                Assert.DoesNotThrowAsync(async () => await canvas.DisposeAsync());
            }
        }

        [Test]
        public void VerifyCompartmentAndNameRenderEscapedTextAndTruncation()
        {
            var compartment = this.context.Render<DiagramCompartment>(parameters => parameters.Add(component => component.Presentation,
                new DiagramCompartmentPresentation("attributes", ["value = <script>"], true)));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(compartment.Find("[role=group]").GetAttribute("aria-label"), Is.EqualTo("attributes"));
                Assert.That(compartment.FindAll("script"), Is.Empty);
                Assert.That(compartment.Markup, Does.Contain("…"));
                var name = this.context.Render<DiagramNameCompartment>(parameters => parameters.Add(component => component.Presentation, new DiagramNamePresentation("part",
                    "engine : Engine", ["propulsion"])));
                Assert.That(name.Markup, Does.Contain("«alias» propulsion"));
            }
        }

        [TestCase(BoundarySide.Left)]
        [TestCase(BoundarySide.Right)]
        [TestCase(BoundarySide.Top)]
        [TestCase(BoundarySide.Bottom)]
        public void VerifyBoundaryShowsLabelAndDelegatesSelection(BoundarySide side)
        {
            var element = new PortUsage();
            var presentation = new DiagramBoundaryViewModel(new DiagramBoundaryPresentation(element, "powerIn : PowerPort", side, 0.5, "In"), this.viewModel.Object);
            var component = this.context.Render<DiagramBoundaryFeature>(parameters => parameters.Add(value => value.ViewModel, presentation));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("button").GetAttribute("aria-label"), Is.EqualTo("powerIn : PowerPort"));
                Assert.That(component.Find("button").TextContent, Does.Contain("powerIn : PowerPort"));
                component.Find("button").Click(new MouseEventArgs { Detail = 0 });
                this.viewModel.Verify(value => value.Select(element), Times.Once);
            }
        }

        [Test]
        public void VerifyDiagramMountSharesSelectionAndDoesNotOwnTabState()
        {
            this.context.JSInterop.Mode = JSRuntimeMode.Loose;
            this.context.JSInterop.Setup<Rectangle>("ZBlazorDiagrams.getBoundingClientRect", _ => true).SetResult(new Rectangle(0, 0, 1000, 700));
            var selection = new ContextAwareService();
            var owner = this.diagrams.CreateViewModel(GeneralViewExample.Create(DiagramTestFactory.CreateProjector()), selection);
            this.ownedViewModels.Add(owner);
            var component = this.context.Render<DiagramCanvas>(parameters => parameters.Add(value => value.ViewModel, owner));
            var element = owner.Projection.Nodes[0].Occurrence.Element;
            var node = component.FindComponents<DiagramNodeWidget>()[0];
            node.Find("button").Click(new MouseEventArgs { Detail = 0 });
            using (Assert.EnterMultipleScope())
            {
                Assert.That(selection.SelectedElement, Is.SameAs(element));
                component.Dispose();
                Assert.DoesNotThrow(() => owner.Select(element));
            }
        }

        [Test]
        public async Task VerifyZoomControlsDelegateToBorrowedViewModel()
        {
            this.context.JSInterop.Setup<Rectangle>("ZBlazorDiagrams.getBoundingClientRect", _ => true).SetResult(new Rectangle(0, 0, 1000, 700));
            var owner = this.viewModel;
            using var toolOwner = this.diagrams.CreateViewModel(new DiagramProjection([], []), new ContextAwareService());
            owner.SetupGet(value => value.Projection).Returns(toolOwner.Projection);
            owner.SetupGet(value => value.Tools).Returns(toolOwner.Tools);
            owner.SetupGet(value => value.Viewport).Returns(new DiagramViewport(IsInitialized: true));
            var component = this.context.Render<DiagramCanvas>(parameters => parameters.Add(value => value.ViewModel, owner.Object));
            var controls = component.FindComponent<ZoomControls>();
            await controls.InvokeAsync(() => controls.Instance.ZoomChanged.InvokeAsync(175));
            await controls.InvokeAsync(() => controls.Instance.OnResetZoom.InvokeAsync());
            await controls.InvokeAsync(() => controls.Instance.OnFitToView.InvokeAsync());
            component.Dispose();
            owner.Verify(value => value.ChangeZoom(175), Times.Once);
            owner.Verify(value => value.ResetZoom(), Times.Once);
            owner.Verify(value => value.FitToView(1000, 700), Times.Once);
            owner.Verify(value => value.Dispose(), Times.Never);
        }
    }
}
