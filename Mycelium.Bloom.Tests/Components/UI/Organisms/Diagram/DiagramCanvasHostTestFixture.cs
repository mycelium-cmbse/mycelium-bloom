// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramCanvasHostTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.Diagram
{
    using System.Threading.Tasks;

    using Blazor.Diagrams;
    using Blazor.Diagrams.Core.Geometry;

    using Bunit;

    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    using Mycelium.Bloom.Components.UI.Organisms.Diagram;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Extensions;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Diagram;

    using SysML2.NET.Core.POCO.Systems.Parts;

    [TestFixture]
    public sealed class DiagramCanvasHostTestFixture
    {
        [Test]
        public async Task VerifyRendererDisposalReleasesNativeResourcesDuringPendingInitialization()
        {
            await using var context = new BunitContext();
            context.Services.AddDiagramServices();
            context.SetRendererInfo(new RendererInfo("Server", true));
            context.JSInterop.Mode = JSRuntimeMode.Loose;
            context.JSInterop.SetupModule("./Components/UI/Organisms/Diagram/DiagramCanvas.razor.js").SetupVoid("ensureDiagramAssets").SetVoidResult();
            var bounds = context.JSInterop.Setup<Rectangle>("ZBlazorDiagrams.getBoundingClientRect", _ => true);
            var element = new PartUsage();
            using var owner = new DiagramViewModel(DiagramTestFactory.Create([element]), new ContextAwareService());
            var component = context.Render<DiagramCanvas>(parameters => parameters.Add(value => value.ViewModel, owner));
            var native = component.FindComponent<CascadingValue<BlazorDiagram>>().Instance.Value;
            Assert.That(native.Nodes, Has.Count.EqualTo(1));

            var disposal = context.DisposeAsync().AsTask();
            Assert.That(disposal.IsCompleted, Is.False);
            bounds.SetResult(new Rectangle(0, 0, 1000, 700));
            await disposal;
            var viewport = owner.Viewport;
            native.SetZoom(1.5);
            owner.Select(element);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(native.Nodes, Is.Empty);
                Assert.That(native.Links, Is.Empty);
                Assert.That(owner.Viewport, Is.SameAs(viewport));
                Assert.That(owner.Projection.Nodes[0].Occurrence.Element, Is.SameAs(element));
                Assert.That(owner.GetNodeViewModel(owner.Projection.Nodes[0].Occurrence.Id).IsSelected, Is.True);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task VerifyDisposeDuringScriptLoadingSkipsNativeInitialization(bool disconnected)
        {
            await using var context = new BunitContext();
            context.Services.AddDiagramServices();
            context.SetRendererInfo(new RendererInfo("Server", true));
            context.JSInterop.Mode = JSRuntimeMode.Loose;
            var module = context.JSInterop.SetupModule("./Components/UI/Organisms/Diagram/DiagramCanvas.razor.js");
            var load = module.SetupVoid("ensureDiagramAssets");
            context.JSInterop.Setup<Rectangle>("ZBlazorDiagrams.getBoundingClientRect", _ => true).SetResult(new Rectangle(0, 0, 1000, 700));
            using var owner = new DiagramViewModel(new DiagramProjection([], []), new ContextAwareService());
            var component = context.Render<DiagramCanvas>(parameters => parameters.Add(value => value.ViewModel, owner));
            Assert.That(component.FindComponents<DiagramCanvasHost>(), Is.Empty);
            await component.InvokeAsync(() => component.Render(parameters => parameters.Add(value => value.IsNested, true)));
            Assert.That(component.FindComponents<DiagramCanvasHost>(), Is.Empty);
            await context.Renderer.Dispatcher.InvokeAsync(component.Instance.Dispose);
            if (disconnected)
            {
                load.SetException(new JSDisconnectedException("The test circuit has disconnected."));
            }
            else
            {
                load.SetVoidResult();
            }
            await context.Renderer.Dispatcher.InvokeAsync(() => Task.CompletedTask);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(context.JSInterop.Invocations, Has.None.Property(nameof(JSRuntimeInvocation.Identifier)).EqualTo("ZBlazorDiagrams.getBoundingClientRect"));
                Assert.That(owner.Projection.Nodes, Is.Empty);
            }
        }

        [Test]
        public async Task VerifyDisposeWaitsForPendingNativeBoundsInitialization()
        {
            await using var context = new BunitContext();
            context.Services.AddDiagramServices();
            context.SetRendererInfo(new RendererInfo("Server", true));
            context.JSInterop.Mode = JSRuntimeMode.Loose;
            context.JSInterop.SetupModule("./Components/UI/Organisms/Diagram/DiagramCanvas.razor.js").SetupVoid("ensureDiagramAssets").SetVoidResult();
            var bounds = context.JSInterop.Setup<Rectangle>("ZBlazorDiagrams.getBoundingClientRect", _ => true);
            using var owner = new DiagramViewModel(new DiagramProjection([], []), new ContextAwareService());
            var component = context.Render<DiagramCanvas>(parameters => parameters.Add(value => value.ViewModel, owner));
            var canvas = component.FindComponent<DiagramCanvasHost>();

            var disposal = canvas.Instance.DisposeAsync().AsTask();
            Assert.That(disposal.IsCompleted, Is.False);
            bounds.SetResult(new Rectangle(0, 0, 1000, 700));
            await disposal;
            await canvas.Instance.DisposeAsync();

            Assert.That(owner.Projection.Nodes, Is.Empty);
        }
    }
}
