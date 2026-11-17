// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramToolboxTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.Diagram
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Blazor.Diagrams.Core.Geometry;

    using Bunit;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.UI.Organisms.Diagram;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Extensions;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.Diagram;

    [TestFixture]
    public sealed class DiagramToolboxTestFixture
    {
        private BunitContext context;

        private List<DiagramViewModel> owners;

        [SetUp]
        public void SetUp()
        {
            this.context = new BunitContext();
            this.owners = [];
            this.context.Services.AddDiagramServices();
            this.context.SetRendererInfo(new RendererInfo("Server", true));
            this.context.JSInterop.SetupModule("./Components/UI/Organisms/Diagram/DiagramCanvas.razor.js").SetupVoid("ensureDiagramAssets").SetVoidResult();
            this.context.JSInterop.Setup<Rectangle>("ZBlazorDiagrams.getBoundingClientRect", _ => true).SetResult(new Rectangle(0, 0, 1000, 700));
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
                foreach (var owner in this.owners)
                {
                    owner.Dispose();
                }
            }
        }

        [Test]
        public void VerifyMoveIsDefaultAndBothToolsAreEnabled()
        {
            var owner = this.CreateOwner();
            var component = this.context.Render<DiagramToolbox>(parameters => parameters.Add(value => value.ViewModel, owner));
            var move = component.Find("button[aria-label=Move]");
            var pan = component.Find("button[aria-label=Pan]");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("[role=toolbar]").GetAttribute("aria-label"), Is.EqualTo("Diagram tools"));
                Assert.That(component.FindAll("button:enabled"), Has.Count.EqualTo(2));
                Assert.That(move.GetAttribute("type"), Is.EqualTo("button"));
                Assert.That(move.GetAttribute("aria-pressed"), Is.EqualTo("true"));
                Assert.That(move.HasAttribute("disabled"), Is.False);
                Assert.That(move.HasAttribute("tabindex"), Is.False);
                Assert.That(pan.HasAttribute("disabled"), Is.False);
                Assert.That(pan.GetAttribute("aria-pressed"), Is.EqualTo("false"));
                Assert.That(component.FindAll("svg[aria-hidden=true]"), Has.Count.EqualTo(2));
            }
        }

        [Test]
        public void VerifyToolButtonsChangeOnlyTheirDiagramMode()
        {
            var owner = this.CreateOwner();
            var other = this.CreateOwner();
            var component = this.context.Render<DiagramToolbox>(parameters => parameters.Add(value => value.ViewModel, owner));
            component.Find("button[aria-label=Pan]").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.ActiveTool, Is.EqualTo(DiagramTool.Pan));
                Assert.That(other.ActiveTool, Is.EqualTo(DiagramTool.Move));
                Assert.That(component.Find("button[aria-label=Pan]").GetAttribute("aria-pressed"), Is.EqualTo("true"));
                Assert.That(component.Find("button[aria-label=Move]").GetAttribute("aria-pressed"), Is.EqualTo("false"));
            }

            component.Find("button[aria-label=Move]").Click();
            Assert.That(owner.ActiveTool, Is.EqualTo(DiagramTool.Move));
        }

        [TestCase(false, 1)]
        [TestCase(true, 0)]
        public void VerifyOnlyTopLevelDiagramsShowToolbox(bool nested, int count)
        {
            var owner = this.CreateOwner();
            var component = this.context.Render<DiagramCanvas>(parameters => parameters.Add(value => value.ViewModel, owner).Add(value => value.IsNested, nested));

            Assert.That(component.FindComponents<DiagramToolbox>(), Has.Count.EqualTo(count));
        }

        [Test]
        public void VerifyMultipleDiagramsKeepTheirViewportAcrossToolboxRemount()
        {
            var firstOwner = this.CreateOwner();
            var secondOwner = this.CreateOwner();
            firstOwner.SetViewport(20, 30, 1.5);
            firstOwner.SetTool(DiagramTool.Pan);
            secondOwner.SetViewport(-40, 50, 0.75);
            var firstViewport = firstOwner.Viewport;
            var secondViewport = secondOwner.Viewport;
            var first = this.context.Render<DiagramCanvas>(parameters => parameters.Add(value => value.ViewModel, firstOwner));
            var second = this.context.Render<DiagramCanvas>(parameters => parameters.Add(value => value.ViewModel, secondOwner));

            first.Dispose();
            var remounted = this.context.Render<DiagramCanvas>(parameters => parameters.Add(value => value.ViewModel, firstOwner));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstOwner.Viewport, Is.EqualTo(firstViewport));
                Assert.That(secondOwner.Viewport, Is.EqualTo(secondViewport));
                Assert.That(remounted.Find("button[aria-label=Pan]").GetAttribute("aria-pressed"), Is.EqualTo("true"));
                Assert.That(second.Find("button[aria-label=Move]").GetAttribute("aria-pressed"), Is.EqualTo("true"));
            }
        }

        [Test]
        public async Task VerifyToolboxDoesNotOwnReplacedOrUnmountedTabState()
        {
            var previous = this.CreateOwner();
            var replacement = this.CreateOwner();
            var component = this.context.Render<DiagramCanvas>(parameters => parameters.Add(value => value.ViewModel, previous));
            await component.InvokeAsync(() => component.Render(parameters => parameters.Add(value => value.ViewModel, replacement)));
            component.Dispose();

            using (Assert.EnterMultipleScope())
            {
                Assert.DoesNotThrow(() => previous.SetViewport(10, 20, 1));
                Assert.DoesNotThrow(() => replacement.SetViewport(30, 40, 2));
            }
        }

        private DiagramViewModel CreateOwner()
        {
            var owner = new DiagramViewModel(new DiagramProjection([], []), new ContextAwareService());
            this.owners.Add(owner);
            return owner;
        }
    }
}
