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
    using System.Linq;
    using System.Threading.Tasks;

    using Blazor.Diagrams;
    using Blazor.Diagrams.Core.Geometry;

    using Bunit;

    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;
    using Microsoft.Extensions.DependencyInjection;

    using Mycelium.Bloom.Components.UI.Organisms.Diagram;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Extensions;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.DetailsPanel;
    using Mycelium.Bloom.ViewModel.Diagram;
    using Mycelium.Bloom.ViewModel.Diagram.Tools;

    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Systems.Parts;

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
                Assert.That(owner.Tools.Active.Id, Is.EqualTo(DiagramToolDefaults.Pan));
                Assert.That(other.Tools.Active.Id, Is.EqualTo(DiagramToolDefaults.Move));
                Assert.That(component.Find("button[aria-label=Pan]").GetAttribute("aria-pressed"), Is.EqualTo("true"));
                Assert.That(component.Find("button[aria-label=Move]").GetAttribute("aria-pressed"), Is.EqualTo("false"));
            }

            component.Find("button[aria-label=Move]").Click();
            Assert.That(owner.Tools.Active.Id, Is.EqualTo(DiagramToolDefaults.Move));
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
            firstOwner.Tools.TryActivate(DiagramToolDefaults.Pan);
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

        [Test]
        public void VerifyToolboxRendersRegisteredLayoutAndDisablesEditingInReadOnlyViews()
        {
            var custom = new DiagramToolDefinition("connect", "Connect", "link", (_, _) => null)
            {
                DiagramKinds = ["sample"],
                RequiredCapabilities = ["relationships"],
                RequiresEditableDiagram = true,
                DefaultShortcut = "c"
            };
            var layout = new DiagramToolLayout([new(custom.Id, "editing", "r"), new(DiagramToolDefaults.Move, "selection")],
                DiagramToolDefaults.Move, DiagramToolDefaults.Pan);
            var registrations = new ServiceCollection();
            registrations.AddDiagramServices();
            registrations.AddSingleton(custom);
            using var provider = registrations.BuildServiceProvider();
            var catalogue = provider.GetRequiredService<DiagramToolCatalogue>();
            var unsupported = new DiagramProjection([], []) { Kind = "sample" };
            var supported = unsupported with { Capabilities = unsupported.Capabilities.Add("relationships") };
            using (Assert.EnterMultipleScope())
            {
                Assert.That(catalogue.For(unsupported), Has.None.EqualTo(custom));
                Assert.That(catalogue.For(supported with { Kind = "other" }), Has.None.EqualTo(custom));
                Assert.That(catalogue.For(supported).Select(tool => tool.Id), Is.EqualTo((string[])["move", "pan", "connect"]));
            }

            using var selection = new ContextAwareService();
            using var owner = new DiagramViewModel(supported, selection, catalogue: catalogue, layout: layout);
            var component = this.context.Render<DiagramToolbox>(parameters => parameters.Add(value => value.ViewModel, owner));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("button").Select(button => button.GetAttribute("aria-label")), Is.EqualTo((string[])["Connect", "Move"]));
                Assert.That(component.Find("button[aria-label=Connect]").GetAttribute("aria-keyshortcuts"), Is.EqualTo("r"));
                Assert.That(component.FindAll(".diagram-toolbox-divider"), Has.Count.EqualTo(1));
            }

            component.Find("button[aria-label=Connect]").Click();
            Assert.That(owner.Tools.Active, Is.SameAs(custom));
            owner.IsReadOnly = true;

            component.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.Find("button[aria-label=Connect]").HasAttribute("disabled"), Is.True);
                    Assert.That(component.Find("button[aria-label=Move]").GetAttribute("aria-pressed"), Is.EqualTo("true"));
                    Assert.That(owner.Tools.TryActivate(custom.Id), Is.False);
                }
            });
        }

        [TestCase("onpointercancel")]
        [TestCase("onlostpointercapture")]
        [TestCase("onfocusout")]
        [TestCase("Escape")]
        public async Task VerifyCanvasOwnsTemporaryNavigationCancellation(string cancellation)
        {
            using var selection = new ContextAwareService();
            var (owner, selected, _) = this.CreateSelectionDiagram(selection);
            var otherOwner = this.CreateOwner();
            var component = this.context.Render<DiagramCanvas>(parameters => parameters.Add(value => value.ViewModel, owner));
            var other = this.context.Render<DiagramCanvas>(parameters => parameters.Add(value => value.ViewModel, otherOwner).Add(value => value.IsNested, true));
            var canvas = component.Find(".general-diagram-canvas");
            await component.Find(".diagram-edge").KeyDownAsync(new KeyboardEventArgs { Code = "Space", Key = " " });
            await canvas.KeyDownAsync(new KeyboardEventArgs { Code = "Space", Key = " " });
            var diagram = component.FindComponent<CascadingValue<BlazorDiagram>>().Instance.Value;
            await component.InvokeAsync(() => diagram.TriggerPointerDown(null,
                new Blazor.Diagrams.Core.Events.PointerEventArgs(30, 40, 0, 1, false, false, false, 3, 1, 1, 0, 0, 0, "mouse", true)));
            await canvas.KeyUpAsync(new KeyboardEventArgs { Code = "Space", Key = " " });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("section[data-tool=move]").GetAttribute("data-temporary-pan"), Is.EqualTo("true"));
                Assert.That(other.Find("section").HasAttribute("data-temporary-pan"), Is.False);
            }

            if (cancellation == "Escape")
            {
                await canvas.KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
            }
            else if (cancellation == "onfocusout")
            {
                await canvas.TriggerEventAsync(cancellation, new FocusEventArgs());
            }
            else
            {
                await canvas.TriggerEventAsync(cancellation, new PointerEventArgs { PointerId = 3 });
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.Tools.Active.Id, Is.EqualTo(DiagramToolDefaults.Move));
                Assert.That(owner.Tools.Temporary, Is.Null);
                Assert.That(component.Find("section[data-tool=move]").HasAttribute("data-temporary-pan"), Is.False);
                Assert.That(selection.SelectedElement, Is.SameAs(selected));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task VerifyFocusedSpaceEitherActivatesOnReleaseOrPansWithoutChangingProperties(bool drag)
        {
            using var selection = new ContextAwareService();
            using var properties = new DetailsPanelViewModel(selection);
            var (owner, selected, relationship) = this.CreateSelectionDiagram(selection);
            var component = this.context.Render<DiagramCanvas>(parameters => parameters.Add(value => value.ViewModel, owner));
            var canvas = component.Find(".general-diagram-canvas");
            var edge = component.Find(".diagram-edge");
            var originalProperties = properties.Presentation;
            var viewport = owner.Viewport;
            var space = new KeyboardEventArgs { Code = "Space", Key = " " };
            var repeat = new KeyboardEventArgs { Code = "Space", Key = " ", Repeat = true };

            await edge.KeyDownAsync(space);
            await canvas.KeyDownAsync(space);
            await edge.KeyDownAsync(repeat);
            await canvas.KeyDownAsync(repeat);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selection.SelectedElement, Is.SameAs(selected));
                Assert.That(properties.Presentation, Is.SameAs(originalProperties));
                Assert.That(owner.Tools.Temporary?.Id, Is.EqualTo(DiagramToolDefaults.Pan));
            }

            if (drag)
            {
                var native = component.FindComponent<CascadingValue<BlazorDiagram>>().Instance.Value;
                await component.InvokeAsync(() =>
                {
                    native.TriggerPointerDown(native.Links[0], NativePointer(30, 40));
                    native.TriggerPointerMove(null, NativePointer(50, 55));
                    native.TriggerPointerUp(null, NativePointer(50, 55));
                });
            }

            await canvas.KeyUpAsync(space);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(selection.SelectedElement, Is.SameAs(drag ? selected : relationship));
                Assert.That(properties.Presentation.Element, Is.SameAs(selection.SelectedElement));
                Assert.That(owner.Viewport.PanX, Is.EqualTo(viewport.PanX + (drag ? 20 : 0)));
                Assert.That(owner.Tools.Temporary, Is.Null);
            }
        }

        [TestCase("Escape")]
        [TestCase("Blur")]
        [TestCase("FocusOut")]
        [TestCase("Pan")]
        public async Task VerifyInterruptedSpaceTapNeverActivatesTheRelationship(string interruption)
        {
            using var selection = new ContextAwareService();
            var (owner, selected, relationship) = this.CreateSelectionDiagram(selection);
            var component = this.context.Render<DiagramCanvas>(parameters => parameters.Add(value => value.ViewModel, owner));
            var canvas = component.Find(".general-diagram-canvas");
            var edge = component.Find(".diagram-edge");
            var space = new KeyboardEventArgs { Code = "Space", Key = " " };
            await edge.KeyDownAsync(space);
            await canvas.KeyDownAsync(space);

            switch (interruption)
            {
                case "Escape":
                    await canvas.KeyDownAsync(new KeyboardEventArgs { Code = "Escape", Key = "Escape" });
                    break;
                case "Blur":
                    await edge.BlurAsync(new FocusEventArgs());
                    break;
                case "FocusOut":
                    await canvas.TriggerEventAsync("onfocusout", new FocusEventArgs());
                    break;
                case "Pan":
                    await component.Find("button[aria-label=Pan]").ClickAsync();
                    await edge.KeyDownAsync(new KeyboardEventArgs { Code = "Enter", Key = "Enter" });
                    break;
            }

            await canvas.KeyUpAsync(space);
            Assert.That(selection.SelectedElement, Is.SameAs(selected));

            await component.Find("button[aria-label=Move]").ClickAsync();
            await edge.KeyDownAsync(new KeyboardEventArgs { Code = "Enter", Key = "Enter" });
            Assert.That(selection.SelectedElement, Is.SameAs(relationship));
        }

        private (DiagramViewModel Owner, PartUsage Selected, Redefinition Relationship) CreateSelectionDiagram(ContextAwareService selection)
        {
            var source = new PartUsage { DeclaredName = "source" };
            var target = new PartUsage { DeclaredName = "target" };
            var relationship = new Redefinition { RedefiningFeature = source, RedefinedFeature = target };
            var owner = new DiagramViewModel(DiagramTestFactory.Create([source, target, relationship]), selection);
            this.owners.Add(owner);
            owner.Select(source);
            return (owner, source, relationship);
        }

        private static Blazor.Diagrams.Core.Events.PointerEventArgs NativePointer(double x, double y) => new(x, y, 0, 1, false, false, false, 3, 1, 1, 0, 0, 0, "mouse", true);

        private DiagramViewModel CreateOwner()
        {
            var owner = new DiagramViewModel(new DiagramProjection([], []), new ContextAwareService());
            this.owners.Add(owner);
            return owner;
        }
    }
}
