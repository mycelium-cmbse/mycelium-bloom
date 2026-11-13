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
    using System.Threading.Tasks;

    using Blazor.Diagrams.Core.Geometry;

    using Bunit;


    using Moq;

    using Mycelium.Bloom.Components.UI.Molecules.ZoomControls;
    using Mycelium.Bloom.Components.UI.Organisms.Diagram;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.ViewModel.Diagram;

    using SysML2.NET.Core.POCO.Systems.Parts;
    using SysML2.NET.Core.POCO.Systems.Ports;

    [TestFixture]
    public sealed class GeneralDiagramComponentTestFixture
    {
        private BunitContext context;

        private Mock<IGeneralDiagramViewModel> viewModel;

        private List<IDisposable> ownedViewModels;

        [SetUp]
        public void SetUp()
        {
            this.context = new BunitContext();
            this.context.SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Server", true));
            this.viewModel = new Mock<IGeneralDiagramViewModel>();
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
            var owner = new GeneralDiagramViewModel(GeneralViewProjector.Create([new PartUsage { Id = Guid.NewGuid() }]), selection);
            this.ownedViewModels.Add(owner);
            var component = this.context.Render<GeneralDiagram>(parameters => parameters.Add(value => value.ViewModel, owner));
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
                component.Find("button").Click();
                this.viewModel.Verify(value => value.Select(element), Times.Once);
            }
        }

        [Test]
        public void VerifyDiagramMountSharesSelectionAndDoesNotOwnTabState()
        {
            this.context.JSInterop.Mode = JSRuntimeMode.Loose;
            this.context.JSInterop.Setup<Rectangle>("ZBlazorDiagrams.getBoundingClientRect", _ => true).SetResult(new Rectangle(0, 0, 1000, 700));
            var selection = new ContextAwareService();
            var owner = new GeneralDiagramViewModel(GeneralViewExample.Create(), selection);
            this.ownedViewModels.Add(owner);
            var component = this.context.Render<GeneralDiagram>(parameters => parameters.Add(value => value.ViewModel, owner));
            var element = owner.Projection.Nodes[0].Occurrence.Element;
            var node = component.FindComponents<DiagramNodeWidget>()[0];
            node.Find("button").Click();
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
            owner.SetupGet(value => value.Projection).Returns(new GeneralViewProjection([], []));
            owner.SetupGet(value => value.Viewport).Returns(new DiagramViewport(IsInitialized: true));
            var component = this.context.Render<GeneralDiagram>(parameters => parameters.Add(value => value.ViewModel, owner.Object));
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
