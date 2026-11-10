// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramCompositionTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.Pages.Workspace
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using Microsoft.JSInterop;
    using Blazor.Diagrams.Core.Geometry;
    using Blazor.Diagrams.Core.Models;
    using Blazor.Diagrams;
    using Bunit;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using Microsoft.AspNetCore.Components;
    using Moq;
    using Mycelium.Bloom.Components.Pages.Workspace;
    using Mycelium.Bloom.Components.UI.Molecules.ActionMenu;
    using Mycelium.Bloom.Components.UI.Organisms.Interconnection;
    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Extensions;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Diagram;
    using Mycelium.Bloom.ViewModel.WorkspaceEditor;
    using SysML2.NET.Core.Core.Types;

    [TestFixture]
    public sealed class DiagramCompositionTestFixture
    {
        private BunitContext context;
        private static readonly string[] InitialTabs = ["project-browser", "placeholder", "interconnection"];
        private static readonly string[] AddActions = ["empty-editor", "open-project-browser", "open-interconnection"];

        [SetUp]
        public void SetUp()
        {
            this.context = new BunitContext();
            BlueprintTestSetup.Configure(this.context);
            var environment = new Mock<IHostEnvironment>(MockBehavior.Strict);
            environment.SetupGet(value => value.EnvironmentName).Returns(Environments.Production);
            environment.SetupGet(value => value.ContentRootPath)
                .Returns(TestRepository.GetDirectoryPath("Mycelium.Bloom"));
            this.context.Services.AddSingleton(environment.Object);
            this.context.Services.AddLogging();
            this.context.Services.AddApplicationServices();
            this.context.JSInterop.Mode = JSRuntimeMode.Loose;
            this.context.JSInterop.Setup<Rectangle>("ZBlazorDiagrams.getBoundingClientRect", _ => true)
                .SetResult(new Rectangle(0, 0, 1000, 700));
        }

        [TearDown]
        public async Task TearDown() => await this.context.DisposeAsync();

        [Test]
        public async Task VerifyFreshDefaultDiagramIsNotRecreatedAfterCloseOrRerender()
        {
            using var editor =
                new WorkspaceEditorViewModel(Options.Create(new WorkspaceEditorOptions { MaximumGroupCount = 4 }));
            this.context.Services.AddScoped<Func<IWorkspaceEditorViewModel>>(_ => () => editor);
            var session = this.context.Services.GetRequiredService<Mycelium.Bloom.Core.Context.WorkspaceModelSession>();
            await session.EnsureLoadedAsync();
            using var component = this.context.Render<Modelling>();
            var group = editor.Groups[2];
            var initial = group.ActiveTab;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(editor.Groups.Select(value => value.ActiveTab.ViewTypeKey),
                            Is.EqualTo(InitialTabs));
                Assert.That(initial.Title, Is.EqualTo("Interconnection"));
            }
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.Markup, Does.Contain("No structural parts to display")));
            component.Render();
            Assert.That(group.Tabs.Single(), Is.SameAs(initial));
            await component.Find($"[data-testid='editor-workspace-tab-close'][data-tab-id='{initial.Id}']")
                .ClickAsync();
            component.Render();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(editor.Groups.SelectMany(value => value.Tabs).Any(tab => tab.ViewTypeKey == "interconnection"),
                            Is.False);
                Assert.That(
                    editor.Groups.SelectMany(value => value.Tabs).Count(tab => tab.ViewTypeKey == "project-browser"),
                    Is.EqualTo(1));
            }
        }

        [Test]
        public async Task VerifyIndependentDiagramBrowserPairsReuseOnlyCanonicalModel()
        {
            using var editor =
                new WorkspaceEditorViewModel(Options.Create(new WorkspaceEditorOptions { MaximumGroupCount = 4 }));
            this.context.Services.AddScoped<Func<IWorkspaceEditorViewModel>>(_ => () => editor);
            var session = this.context.Services.GetRequiredService<Mycelium.Bloom.Core.Context.WorkspaceModelSession>();
            await session.EnsureLoadedAsync();
            using var stream = System.IO.File.OpenRead(System.IO.Path.Combine(
                TestRepository.GetDirectoryPath("Mycelium.Bloom"),
                "Resources", "Prototypes", "Interconnection.json"));
            using var model = await session.OpenContextAsync(stream, System.Threading.CancellationToken.None);
            using var component = this.context.Render<Modelling>();
            var diagramGroup = editor.Groups[2];
            var browserGroup = editor.Groups[0];
            var firstTab = diagramGroup.ActiveTab;
            var open = component.FindComponent<Mycelium.Bloom.Components.UI.Organisms.ModelOpening.ModelOpening>();
            await component.InvokeAsync(() => open.Instance.Opened.InvokeAsync(model));
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.FindComponents<Interconnection>(), Has.Count.EqualTo(1)));
            var first = component.FindComponent<Interconnection>().Instance.ViewModel;
            var firstBrowserTab = browserGroup.ActiveTab;
            var firstBrowser = component
                .FindComponent<Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser.ProjectBrowser>().Instance
                .ViewModel;
            var firstRequests = new List<string>();
            using var firstSubscription = first.RevealRequested.Subscribe(firstRequests.Add);
            var menu = component.FindComponents<ActionMenu>()[^1].Instance;
            await component.InvokeAsync(() =>
                menu.ItemSelected.InvokeAsync(menu.Items.Single(item => item.Id == "open-interconnection")));
            var secondTab = diagramGroup.ActiveTab;
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.FindComponent<Interconnection>().Instance.ViewModel, Is.Not.SameAs(first)));
            var second = component.FindComponent<Interconnection>().Instance.ViewModel;
            open = component.FindComponent<Mycelium.Bloom.Components.UI.Organisms.ModelOpening.ModelOpening>();
            await component.InvokeAsync(() => open.Instance.Opened.InvokeAsync(model));
            var secondBrowserTab = browserGroup.ActiveTab;
            var secondBrowser = component
                .FindComponent<Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser.ProjectBrowser>().Instance
                .ViewModel;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(secondTab.Id, Is.Not.EqualTo(firstTab.Id));
                Assert.That(second.State.DiagramId, Is.Not.EqualTo(first.State.DiagramId));
                Assert.That(second.ModelContextId, Is.EqualTo(first.ModelContextId));
                Assert.That(secondBrowserTab, Is.Not.SameAs(firstBrowserTab));
                Assert.That(secondBrowser, Is.Not.SameAs(firstBrowser));
            }
            Assert.That(secondBrowser.RootNodes.Single().SourceElement,
                Is.SameAs(firstBrowser.RootNodes.Single().SourceElement));
            var unchanged = second.State;
            await component.InvokeAsync(() =>
            {
                firstBrowser.FilterText = "aocs";
                firstBrowser.RootNodes[0].IsExpanded = false;
                first.ApplyPresentation(first.State with { Viewport = new DiagramViewport(-100, -150, 0.7) });
                first.AddManualBend();
            });
            using (Assert.EnterMultipleScope())
            {
                Assert.That(secondBrowser.FilterText, Is.Empty);
                Assert.That(secondBrowser.RootNodes[0].IsExpanded, Is.True);
                Assert.That(second.State, Is.SameAs(unchanged));
            }
            var count = browserGroup.Tabs.Count;
            open = component.FindComponent<Mycelium.Bloom.Components.UI.Organisms.ModelOpening.ModelOpening>();
            await component.InvokeAsync(() => open.Instance.Opened.InvokeAsync(model));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(browserGroup.Tabs, Has.Count.EqualTo(count));
                Assert.That(second.State, Is.SameAs(unchanged));
            }

            var secondRequests = new List<string>();
            using var secondSubscription = second.RevealRequested.Subscribe(secondRequests.Add);
            await component.InvokeAsync(() => editor.ActivateTab(browserGroup.Id, firstBrowserTab.Id));
            var browser = component
                .FindComponent<Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser.ProjectBrowser>().Instance;
            var canonical =
                model.Snapshot.Root.ownedElement.First(value => first.Projection.Parts.ContainsKey(value.ElementId));
            var part = first.Projection.Parts[canonical.ElementId];
            var node = firstBrowser.RootNodes[0].FindPathTo(canonical)[^1];
            await component.InvokeAsync(() => browser.SelectedNodeChanged.InvokeAsync(node));
            await component.InvokeAsync(() => browser.SelectedNodeChanged.InvokeAsync(node));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstRequests, Is.EqualTo(new[] { part.ElementId, part.ElementId }));
                Assert.That(secondRequests, Is.Empty);
                Assert.That(second.State, Is.SameAs(unchanged));
                Assert.That(diagramGroup.ActiveTab, Is.SameAs(secondTab));
            }
            await component.InvokeAsync(() => editor.MoveTab(diagramGroup.Id, firstTab.Id, editor.Groups[1].Id));
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.FindComponents<Interconnection>(), Has.Count.EqualTo(2)));
            var restored = first.State;
            var firstSelection = first.SelectedOccurrences.ToArray();
            var selected = second.State.Occurrences.First();
            await component.InvokeAsync(() => second.SelectAsync([selected.Id], selected.Id));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.State, Is.SameAs(restored));
                Assert.That(first.SelectedOccurrences, Is.EqualTo(firstSelection));
            }
            await component.Find($"[data-testid='editor-workspace-tab-close'][data-tab-id='{firstTab.Id}']")
                .ClickAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(second.IsScopeAvailable, Is.True);
                Assert.That(secondBrowser.RootNodes.Single().SourceElement, Is.SameAs(model.Snapshot.Root));
            }
        }

        [Test]
        public void VerifyModelOpeningIsAvailableWithoutAuthentication()
        {
            using var component = this.context.Render<Mycelium.Bloom.Components.UI.Organisms.ModelOpening.ModelOpening>();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("button").HasAttribute("disabled"), Is.False);
                Assert.That(component.Markup, Does.Not.Contain("requires authorization"));
                Assert.That(component.FindAll("input[type='file']"), Is.Empty);
            }
        }

        [Test]
        public async Task VerifyAlreadyOpenChoiceBindsItsValueAndPreservesWorkspace()
        {
            var session = this.context.Services.GetRequiredService<Mycelium.Bloom.Core.Context.WorkspaceModelSession>();
            await session.EnsureLoadedAsync();
            var original = session.Current;
            using var file = new Mycelium.Bloom.Tests.ViewModel.Diagram.WorkspaceModelFile("Existing model", "part");
            await using var stream = System.IO.File.OpenRead(file.Uri.LocalPath);
            using var lease = await session.OpenContextAsync(stream, System.Threading.CancellationToken.None);
            Mycelium.Bloom.Model.WorkspaceModelSnapshot chosen = null;
            using var portal = this.context.Render<BlazorBlueprint.Primitives.Services.BbPortalHost>();
            using var component =
                this.context.Render<Mycelium.Bloom.Components.UI.Organisms.ModelOpening.ModelOpening>(parameters => parameters
                    .Add(value => value.ExistingModels, new[] { lease.Snapshot })
                    .Add(value => value.Opened, value => chosen = value.Snapshot));
            await component.Find("button").ClickAsync();
            var select = portal.FindComponent<Mycelium.Bloom.Components.UI.Atoms.SelectInput.SelectInput>();
            Assert.That(select.Instance.Value, Is.Null.Or.Empty);
            await component.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(lease.Snapshot.Id.ToString()));
            Assert.That(select.Instance.Value, Is.EqualTo(lease.Snapshot.Id.ToString()));
            await portal.FindAll("button").Single(button => button.TextContent.Trim() == "Open for this session")
                .ClickAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(chosen, Is.SameAs(lease.Snapshot));
                Assert.That(session.Current, Is.SameAs(original));
            }
        }

        [Test]
        public async Task VerifyExampleRequiresExplicitChoiceAndUsesCanonicalWorkspace()
        {
            var session = this.context.Services.GetRequiredService<Mycelium.Bloom.Core.Context.WorkspaceModelSession>();
            await session.EnsureLoadedAsync();
            var previous = session.Current;
            using var portal = this.context.Render<BlazorBlueprint.Primitives.Services.BbPortalHost>();
            Mycelium.Bloom.Model.WorkspaceModelLease lease = null;
            using var component =
                this.context.Render<Mycelium.Bloom.Components.UI.Organisms.ModelOpening.ModelOpening>(parameters => parameters
                    .Add(value => value.Opened, opened => lease = session.Acquire(opened.Snapshot)));
            Assert.That(component.Find("button").HasAttribute("disabled"), Is.False);
            await component.Find("button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            await portal.WaitForAssertionAsync(() =>
                Assert.That(portal.FindAll("input[type='file']"), Has.Count.EqualTo(1)));
            await portal.FindAll("button")
                .Single(button => button.TextContent.Trim() == "Example interconnection model")
                .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            Assert.That(session.Current, Is.SameAs(previous));
            await portal.FindAll("button").Single(button => button.TextContent.Trim() == "Open for this session")
                .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            await portal.WaitForAssertionAsync(() => Assert.That(portal.FindAll("input[type='file']"), Is.Empty));
            using var retained = lease;
            Assert.That(session.Current, Is.SameAs(previous));
            using var diagram = this.context.Services.GetRequiredService<Func<DiagramViewModel>>()();
            diagram.UpdateWorkspace(lease.Snapshot);
            await diagram.Preparation;
            var projection = diagram.Projection;
            Assert.That(projection.Parts, Has.Count.EqualTo(7));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(projection.Parts.Values.Sum(part => part.Ports.Length), Is.EqualTo(10));
                Assert.That(projection.Connections, Has.Count.EqualTo(5));
                Assert.That(projection.Parts.Values.SelectMany(part => part.Ports).All(port => port.Direction.HasValue),
                    Is.True);
            }
            foreach (var part in projection.Parts.Values)
            {
                Assert.That(await this.context.Services
                    .GetRequiredService<Mycelium.Bloom.Core.ModelLoading.IElementIdResolver>()
                    .ResolveAsync(part.ElementId, System.Threading.CancellationToken.None), Is.Not.Null);
            }
        }

        [Test]
        public async Task VerifyTabSwitchMoveCloseAndPageDisposalRespectExactOwnership()
        {
            using var editor =
                new WorkspaceEditorViewModel(Options.Create(new WorkspaceEditorOptions { MaximumGroupCount = 4 }));
            this.context.Services.AddScoped<Func<IWorkspaceEditorViewModel>>(_ => () => editor);
            var owned = new List<DiagramViewModel>();
            this.context.Services.AddScoped<Func<DiagramViewModel>>(provider => () =>
            {
                var viewModel = ActivatorUtilities.CreateInstance<DiagramViewModel>(provider);
                owned.Add(viewModel);
                return viewModel;
            });
            using var component = this.context.Render<Modelling>();
            var source = editor.Groups[2];
            var destination = editor.Groups[1];
            var placeholder = source.ActiveTab;
            await this.OpenDiagramAsync(component);
            var first = component.FindComponent<Interconnection>().Instance.ViewModel;
            var tab = source.ActiveTab;
            await component.InvokeAsync(() =>
                first.ApplyPresentation(first.State with { Viewport = new DiagramViewport(-20, 30, 0.8) }));
            var saved = DiagramPresentationSerializer.Export(first.State);

            await component.InvokeAsync(() => editor.ActivateTab(source.Id, placeholder.Id));
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.FindComponents<Interconnection>(), Is.Empty));
            await component.InvokeAsync(() => editor.ActivateTab(source.Id, tab.Id));
            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.FindComponent<Interconnection>().Instance.ViewModel, Is.SameAs(first));
                    Assert.That(DiagramPresentationSerializer.Export(first.State), Is.EqualTo(saved));
                }
            });

            await component.InvokeAsync(() => editor.MoveTab(source.Id, tab.Id, destination.Id));
            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.FindComponent<Interconnection>().Instance.ViewModel, Is.SameAs(first));
                    Assert.That(DiagramPresentationSerializer.Export(first.State), Is.EqualTo(saved));
                }
            });

            await component.Find($"[data-testid='editor-workspace-tab-close'][data-tab-id='{tab.Id}']").ClickAsync();
            Assert.That(first.AddExisting(first.State.Occurrences[0].ElementId), Is.False);
            await this.OpenDiagramAsync(component);
            var second = component.FindComponent<Interconnection>().Instance.ViewModel;
            Assert.That(second, Is.Not.SameAs(first));
            component.Instance.Dispose();
            Assert.That(second.AddExisting(second.State.Occurrences[0].ElementId), Is.False);
        }

        [Test]
        public async Task VerifyComponentDisposalLeavesTheCallerOwnedTabStateAlive()
        {
            var module =
                this.context.JSInterop.SetupModule("./Components/UI/Organisms/Interconnection/Interconnection.razor.js");
            module.SetupVoid("mount", _ => true).SetVoidResult();
            module.SetupVoid("unmount", _ => true).SetVoidResult();
            using var viewModel = this.context.Services.GetRequiredService<Func<DiagramViewModel>>()();
            await this.InitializeDiagramAsync(viewModel);
            var occurrence = viewModel.State.Occurrences.First(node => node.ParentId is null && !viewModel.State.Occurrences.Any(child => child.ParentId == node.Id));
            Assert.That(viewModel.ApplyPresentation(viewModel.State with
            {
                Occurrences = viewModel.State.Occurrences.Replace(occurrence,
                    occurrence with { Geometry = occurrence.Geometry with { Width = 200.5 } })
            }), Is.True);
            var component =
                this.context.Render<Interconnection>(parameters => parameters.Add(value => value.ViewModel, viewModel));
            await component.WaitForAssertionAsync(() =>
            {
                var numericValues = component.FindAll(".connection-svg__multiplicity")
                    .SelectMany(element => new[] { element.GetAttribute("x"), element.GetAttribute("y") }).ToArray();
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(numericValues, Is.Not.Empty);
                    Assert.That(numericValues.All(value => !value.Contains(',', StringComparison.Ordinal)), Is.True);
                    Assert.That(
                        component.FindAll(".part-svg__body").Any(element => element.GetAttribute("width") == "200.5"),
                        Is.True);
                }
            });
            await component.InvokeAsync(async () => await component.Instance.DisposeAsync());
            Assert.That(viewModel.AddExisting(viewModel.State.Occurrences[0].ElementId), Is.True);
        }

        [Test]
        public async Task VerifyLateImportIsReleasedAndBackgroundPublicationsCrossTheRenderer()
        {
            var completion =
                new TaskCompletionSource<IJSObjectReference>(TaskCreationOptions.RunContinuationsAsynchronously);
            var runtime = new Mock<IJSRuntime>(MockBehavior.Loose);
            runtime.Setup(value => value.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()))
                .Returns(new ValueTask<IJSObjectReference>(completion.Task));
            runtime.Setup(value =>
                    value.InvokeAsync<Rectangle>("ZBlazorDiagrams.getBoundingClientRect", It.IsAny<object[]>()))
                .Returns(new ValueTask<Rectangle>(new Rectangle(0, 0, 1000, 700)));
            this.context.Services.AddSingleton(runtime.Object);
            var module = new Mock<IJSObjectReference>(MockBehavior.Loose);
            using var viewModel = this.context.Services.GetRequiredService<Func<DiagramViewModel>>()();
            await this.InitializeDiagramAsync(viewModel);
            var component = this.context.Render<Interconnection>(parameters =>
                parameters.Add(value => value.ViewModel, viewModel));
            try
            {
                await component.WaitForAssertionAsync(() => runtime.Verify(
                    value => value.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()), Times.Once));
                await Task.Run(() => viewModel.IsReadOnly = true);
                await component.WaitForAssertionAsync(() => Assert.That(component.FindComponent<DiagramCanvasHost>()
                    .Instance.BlazorDiagram.Nodes.All(node => node.Locked), Is.True));
                await Task.Run(() => viewModel.IsReadOnly = false);
                var disposal = component.InvokeAsync(async () => await component.Instance.DisposeAsync());
                completion.SetResult(module.Object);
                await disposal;
            }
            finally
            {
                completion.TrySetResult(module.Object);
            }

            module.Verify(value => value.DisposeAsync(), Times.Once);
            Assert.That(viewModel.AddExisting(viewModel.State.Occurrences[0].ElementId), Is.True);
        }

        [TestCase("mount")]
        [TestCase("unmount")]
        public async Task VerifyInteropFailureStillDisposesOwnedModule(string failingOperation)
        {
            var mounted =
                new TaskCompletionSource<Microsoft.JSInterop.Infrastructure.IJSVoidResult>(TaskCreationOptions
                    .RunContinuationsAsynchronously);
            var runtime = new Mock<IJSRuntime>();
            var module = new Mock<IJSObjectReference>();
            runtime.Setup(value => value.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()))
                .ReturnsAsync(module.Object);
            runtime.Setup(value =>
                    value.InvokeAsync<Rectangle>("ZBlazorDiagrams.getBoundingClientRect", It.IsAny<object[]>()))
                .ReturnsAsync(new Rectangle(0, 0, 1000, 700));
            module.Setup(value =>
                    value.InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>("mount", It.IsAny<object[]>()))
                .Returns(new ValueTask<Microsoft.JSInterop.Infrastructure.IJSVoidResult>(mounted.Task));
            if (failingOperation == "unmount")
            {
                module.Setup(value =>
                        value.InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>("unmount",
                            It.IsAny<object[]>()))
                    .ThrowsAsync(new JSException("unmount failure"));
            }

            this.context.Services.AddSingleton(runtime.Object);
            using var owner = this.context.Services.GetRequiredService<Func<DiagramViewModel>>()();
            await this.InitializeDiagramAsync(owner);
            var component = this.context.Render<Interconnection>(parameters => parameters.Add(value => value.ViewModel, owner));
            await component.WaitForAssertionAsync(() => module.Verify(value =>
                value.InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>(
                    "mount", It.IsAny<object[]>()), Times.Once));
            var disposal = component.Instance.DisposeAsync().AsTask();
            if (failingOperation == "mount")
            {
                mounted.SetException(new JSException("mount failure"));
            }
            else
            {
                mounted.SetResult(default);
            }

            await Assert.ThatAsync(async () => await disposal,
                Throws.TypeOf<JSException>().With.Message.EqualTo($"{failingOperation} failure"));
            await component.Instance.DisposeAsync();
            module.Verify(
                value => value.InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>("unmount",
                    It.IsAny<object[]>()), Times.Once);
            module.Verify(value => value.DisposeAsync(), Times.Once);
            Assert.That(owner.AddExisting(owner.State.Occurrences[0].ElementId), Is.True);
        }

        [TestCase(FeatureDirectionKind.In)]
        [TestCase(FeatureDirectionKind.Out)]
        [TestCase(FeatureDirectionKind.Inout)]
        [TestCase(null)]
        public async Task VerifyPortGlyphPreservesDirectionAndBoundaryCenter(FeatureDirectionKind? direction)
        {
            await this.LoadExampleAsync();
            foreach (var canonical in this.context.Services.GetRequiredService<SysML2.NET.Dal.IAssembler>().Cache.Values.Select(value => value.Value)
                         .OfType<SysML2.NET.Core.POCO.Systems.Ports.IPortUsage>())
            {
                canonical.Direction = direction;
            }

            using var owner = this.context.Services.GetRequiredService<Func<DiagramViewModel>>()();
            await this.InitializeDiagramAsync(owner);
            using var component =
                this.context.Render<Interconnection>(parameters => parameters.Add(value => value.ViewModel, owner));
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.FindAll(".part-svg__port"), Has.Count.EqualTo(10)));

            if (direction.HasValue)
            {
                var image = component.Find(".part-svg__port-direction");
                var portId = image.ParentElement.GetAttribute("data-port-id");
                var node = component.FindComponents<PartSvg>().Select(part => part.Instance.Node)
                    .Single(item => item.Ports.Any(port => port.Id == portId));
                var port = node.Ports.Single(item => item.Id == portId);
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(image.GetAttribute("data-direction"), Is.EqualTo(direction.ToString()));
                    Assert.That(image.GetAttribute("href"), Does.StartWith("data:image/svg+xml;base64,"));
                    Assert.That(image.GetAttribute("width"), Is.EqualTo("12"));
                    Assert.That(
                        double.Parse(image.GetAttribute("x"), System.Globalization.CultureInfo.InvariantCulture) + 6,
                        Is.EqualTo(port.MiddlePosition.X - node.Position.X));
                    Assert.That(
                        double.Parse(image.GetAttribute("y"), System.Globalization.CultureInfo.InvariantCulture) + 6,
                        Is.EqualTo(port.MiddlePosition.Y - node.Position.Y));
                }

                var bytes = Convert.FromBase64String(image.GetAttribute("href").Split(',')[1]);
                var asset = System.Xml.Linq.XElement.Parse(System.Text.Encoding.UTF8.GetString(bytes));
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(asset.Attribute("viewBox")?.Value, Is.EqualTo("0 0 20 20"));
                    Assert.That(asset.Descendants().Count(element => element.Name.LocalName == "path"), Is.EqualTo(2));
                }
            }
            else
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.FindAll(".part-svg__port-direction"), Is.Empty);
                    Assert.That(component.FindAll(".part-svg__port rect"), Has.Count.EqualTo(10));
                }
            }
        }

        [Test]
        public async Task VerifyPlainConnectionsAndMultipleSelectionDoNotInventFlowOrPresence()
        {
            using var owner = this.context.Services.GetRequiredService<Func<DiagramViewModel>>()();
            await this.InitializeDiagramAsync(owner);
            using var component =
                this.context.Render<Interconnection>(parameters => parameters.Add(value => value.ViewModel, owner));
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.FindAll(".connection-svg__icon"), Has.Count.EqualTo(5)));
            var diagram = component.FindComponent<DiagramCanvasHost>().Instance.BlazorDiagram;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(diagram.Links.Select(link => link.TargetMarker), Is.All.Null);
                Assert.That(component.FindAll(".connection-svg__marker"), Is.Empty);
                Assert.That(component.FindAll(".connection-svg__multiplicity").Select(element => element.TextContent),
                    Is.All.EqualTo("[1]"));
            }

            await component.InvokeAsync(() =>
            {
                diagram.SelectModel(diagram.Nodes[0], true);
                diagram.SelectModel(diagram.Nodes[1], false);
            });
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.FindAll(".part-svg--selected"), Has.Count.EqualTo(2)));
            Assert.That(owner.SelectedOccurrences, Has.Count.EqualTo(2));
        }

        [Test]
        public void VerifyNormalCompositionOmitsAlternativeEngineAndStressControls()
        {
            using var component = this.context.Render<Modelling>();
            foreach (var menu in component.FindComponents<ActionMenu>())
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(menu.Instance.Items.Select(item => item.Id), Does.Not.Contain("open-interconnection-x6"));
                    Assert.That(menu.Instance.Items.Single(item => item.Id == "open-interconnection").Label,
                        Is.EqualTo("Interconnection"));
                }

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(menu.Instance.Items.Select(item => item.Id),
                                    Is.EqualTo(AddActions));
                    Assert.That(menu.Instance.Items.All(item => !item.Id.Contains("stress", StringComparison.Ordinal)),
                        Is.True);
                }
            }

            Assert.That(component.FindAll("[data-testid='diagram-benchmark-probe']"), Is.Empty);
        }

        [Test]
        public async Task VerifyClosingInvokingTabCancelsUnpublishedBrowserAndReleasesCandidate()
        {
            using var editor =
                new WorkspaceEditorViewModel(Options.Create(new WorkspaceEditorOptions { MaximumGroupCount = 4 }));
            this.context.Services.AddScoped<Func<IWorkspaceEditorViewModel>>(_ => () => editor);
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var pending = new Mock<Mycelium.Bloom.ViewModel.ProjectBrowser.IProjectBrowserViewModel>();
            pending.Setup(value => value.InitializeAsync(It.IsAny<SysML2.NET.Core.POCO.Root.Namespaces.INamespace>(),
                    It.IsAny<System.Threading.CancellationToken>()))
                .Returns(async (SysML2.NET.Core.POCO.Root.Namespaces.INamespace _,
                    System.Threading.CancellationToken token) =>
                {
                    entered.TrySetResult();
                    await release.Task.WaitAsync(token);
                    return true;
                });
            var delay = false;
            this.context.Services.AddScoped<Func<Mycelium.Bloom.ViewModel.ProjectBrowser.IProjectBrowserViewModel>>(provider =>
                () => delay
                    ? pending.Object
                    : ActivatorUtilities
                        .CreateInstance<Mycelium.Bloom.ViewModel.ProjectBrowser.ProjectBrowserViewModel>(provider));
            var session = this.context.Services.GetRequiredService<Mycelium.Bloom.Core.Context.WorkspaceModelSession>();
            await session.EnsureLoadedAsync();
            using var component = this.context.Render<Modelling>();
            var menu = component.FindComponents<ActionMenu>()[^1].Instance;
            await component.InvokeAsync(() =>
                menu.ItemSelected.InvokeAsync(menu.Items.Single(item => item.Id == "open-interconnection")));
            var tab = editor.Groups[2].ActiveTab;
            using var file = new Mycelium.Bloom.Tests.ViewModel.Diagram.WorkspaceModelFile("Late model", "part");
            await using var stream = System.IO.File.OpenRead(file.Uri.LocalPath);
            var lease = await session.OpenContextAsync(stream, System.Threading.CancellationToken.None);
            delay = true;
            var opener = component.FindComponent<Mycelium.Bloom.Components.UI.Organisms.ModelOpening.ModelOpening>()
                .Instance;
            var operation = component.InvokeAsync(() => opener.Opened.InvokeAsync(lease));
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await component.Find($"[data-testid='editor-workspace-tab-close'][data-tab-id='{tab.Id}']").ClickAsync();
            await operation;
            lease.Dispose();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(editor.Groups.SelectMany(group => group.Tabs).Any(item => item.Title == "Late model"),
                            Is.False);
                Assert.That(session.GetOpenModels().Any(model => model.Root.DeclaredName == "Late model"), Is.False);
            }
            pending.Verify(value => value.Dispose(), Times.Once);
        }

        [Test]
        public async Task VerifyDiagramLocalOpenPreservesQuantitiesAndReusesTabs()
        {
            using var editor =
                new WorkspaceEditorViewModel(Options.Create(new WorkspaceEditorOptions { MaximumGroupCount = 4 }));
            this.context.Services.AddScoped<Func<IWorkspaceEditorViewModel>>(_ => () => editor);
            var session = this.context.Services.GetRequiredService<Mycelium.Bloom.Core.Context.WorkspaceModelSession>();
            await session.EnsureLoadedAsync();
            var quantities = session.Current;
            using var component = this.context.Render<Modelling>();
            var originalBrowser = component
                .FindComponent<Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser.ProjectBrowser>().Instance
                .ViewModel;
            await component.WaitForAssertionAsync(() => Assert.That(originalBrowser.RootNodes, Is.Not.Empty));
            var menu = component.FindComponents<ActionMenu>()[^1].Instance;
            await component.InvokeAsync(() =>
                menu.ItemSelected.InvokeAsync(menu.Items.Single(item => item.Id == "open-interconnection")));
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.Markup, Does.Contain("No structural parts to display")));
            var invoking = editor.Groups[2].ActiveTab;
            using var file = new Mycelium.Bloom.Tests.ViewModel.Diagram.WorkspaceModelFile("Pump model", "pump");
            await using var stream = System.IO.File.OpenRead(file.Uri.LocalPath);
            using var model = await session.OpenContextAsync(stream, System.Threading.CancellationToken.None);
            var opener = component.FindComponent<Mycelium.Bloom.Components.UI.Organisms.ModelOpening.ModelOpening>();
            await component.InvokeAsync(() => opener.Instance.Opened.InvokeAsync(model));
            await component.WaitForAssertionAsync(() =>
                Assert.That(
                    component.FindComponent<Interconnection>().Instance.ViewModel.Projection.Parts.Values.Single().Name,
                    Is.EqualTo("pump")));
            Assert.That(editor.Groups[2].ActiveTab, Is.SameAs(invoking));
            originalBrowser.SelectNode(originalBrowser.RootNodes.Single());
            using (Assert.EnterMultipleScope())
            {
                Assert.That(originalBrowser.RootNodes.Single().SourceElement, Is.SameAs(quantities.Root));
                Assert.That(session.Current, Is.SameAs(quantities));
            }
            var tabCount = editor.Groups.Sum(group => group.Tabs.Count);
            opener = component.FindComponent<Mycelium.Bloom.Components.UI.Organisms.ModelOpening.ModelOpening>();
            Assert.That(opener.Instance.ExistingModels.Select(value => value.Id), Does.Contain(model.Snapshot.Id));
            await component.InvokeAsync(() => opener.Instance.Opened.InvokeAsync(model));
            Assert.That(editor.Groups.Sum(group => group.Tabs.Count), Is.EqualTo(tabCount));
            var pump = component.FindComponent<Interconnection>().Instance.ViewModel;
            var saved = pump.State;
            using var valveFile = new Mycelium.Bloom.Tests.ViewModel.Diagram.WorkspaceModelFile("Valve model", "valve");
            await using var valveStream = System.IO.File.OpenRead(valveFile.Uri.LocalPath);
            using var valveModel = await session.OpenContextAsync(valveStream, System.Threading.CancellationToken.None);
            opener = component.FindComponent<Mycelium.Bloom.Components.UI.Organisms.ModelOpening.ModelOpening>();
            await component.InvokeAsync(() => opener.Instance.Opened.InvokeAsync(valveModel));
            await component.WaitForAssertionAsync(() =>
                Assert.That(
                    component.FindComponent<Interconnection>().Instance.ViewModel.Projection.Parts.Values.Single().Name,
                    Is.EqualTo("valve")));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(pump.IsScopeAvailable, Is.True);
                Assert.That(pump.State, Is.SameAs(saved));
            }
            await component.InvokeAsync(() => editor.ActivateTab(editor.Groups[2].Id, invoking.Id));
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.FindComponent<Interconnection>().Instance.ViewModel, Is.SameAs(pump)));
            var quantityTab = editor.Groups[0].Tabs[0];
            await component.InvokeAsync(() => editor.ActivateTab(editor.Groups[0].Id, quantityTab.Id));
            await component.WaitForAssertionAsync(() =>
                Assert.That(
                    component.FindComponent<Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser.ProjectBrowser>()
                        .Instance.ViewModel, Is.SameAs(originalBrowser)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task VerifyNormalEmptyOrUnsupportedScopeNeverSubstitutesFixture(bool noModel)
        {
            var session = this.context.Services.GetRequiredService<Mycelium.Bloom.Core.Context.WorkspaceModelSession>();
            if (noModel)
            {
                session.Clear();
            }
            else
            {
                await session.EnsureLoadedAsync();
            }

            var count = this.context.Services.GetRequiredService<SysML2.NET.Dal.IAssembler>().Cache.Count;
            using var component = this.context.Render<Modelling>();
            var menu = component.FindComponents<ActionMenu>()[^1].Instance;
            await component.InvokeAsync(() =>
                menu.ItemSelected.InvokeAsync(menu.Items.Single(item => item.Id == "open-interconnection")));
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.Find("[data-testid='interconnection-scope-status']").TextContent,
                    Does.Contain(noModel ? "No model loaded" : "No structural parts to display")));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindComponents<Interconnection>(), Is.Empty);
                Assert.That(this.context.Services.GetRequiredService<SysML2.NET.Dal.IAssembler>().Cache, Has.Count.EqualTo(count));
            }
        }

        [Test]
        public async Task VerifyBrowserReselectionTargetsItsPairedDiagramWithoutActivation()
        {
            using var editor =
                new WorkspaceEditorViewModel(Options.Create(new WorkspaceEditorOptions { MaximumGroupCount = 4 }));
            this.context.Services.AddScoped<Func<IWorkspaceEditorViewModel>>(_ => () => editor);
            using var component = this.context.Render<Modelling>();
            var group = editor.Groups[2];
            var placeholder = group.ActiveTab;
            await this.OpenDiagramAsync(component);
            var owner = component.FindComponent<Interconnection>().Instance.ViewModel;
            var requests = new List<string>();
            using var subscription = owner.RevealRequested.Subscribe(requests.Add);
            var browser = component
                .FindComponent<Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser.ProjectBrowser>().Instance;
            await component.WaitForAssertionAsync(() => Assert.That(browser.ViewModel.RootNodes, Is.Not.Empty));
            var node = browser.ViewModel.RootNodes[0];
            await component.InvokeAsync(() => browser.SelectedNodeChanged.InvokeAsync(node));
            await component.InvokeAsync(() => browser.SelectedNodeChanged.InvokeAsync(node));
            Assert.That(requests, Is.EqualTo(new[] { node.ElementId, node.ElementId }));
            await component.InvokeAsync(() => editor.ActivateTab(group.Id, placeholder.Id));
            await component.InvokeAsync(() => browser.SelectedNodeChanged.InvokeAsync(node));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(requests, Is.EqualTo(new[] { node.ElementId, node.ElementId, node.ElementId }));
                Assert.That(group.ActiveTab, Is.SameAs(placeholder));
            }
        }

        [Test]
        public async Task VerifyNormalLargeScopeCreatesOnlyRegionalModelsAndRetainsThemAfterMovement()
        {
            using var file = new Mycelium.Bloom.Tests.ViewModel.Diagram.LargeWorkspaceFile(600);
            var session = this.context.Services.GetRequiredService<Mycelium.Bloom.Core.Context.WorkspaceModelSession>();
            await session.OpenAsync(file.Uri);
            using var owner = this.context.Services.GetRequiredService<Func<DiagramViewModel>>()();
            owner.BindWorkspace(session);
            await owner.Preparation;
            this.context.JSInterop.Setup<Rectangle>("ZBlazorDiagrams.getBoundingClientRect", _ => true)
                .SetResult(new Rectangle(0, 0, 900, 700));
            using var component =
                this.context.Render<Interconnection>(parameters => parameters.Add(value => value.ViewModel, owner));
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.FindAll(".part-svg"), Has.Count.InRange(1, 40)));
            var graph = component.FindComponent<DiagramCanvasHost>().Instance.BlazorDiagram;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(graph.Nodes.Count + graph.Groups.Count, Is.LessThan(40));
                Assert.That(owner.State.Occurrences, Has.Length.EqualTo(600));
                Assert.That(component.FindComponents<GroupSvg>(), Has.Count.GreaterThan(1));
            }
            var root = owner.State.Occurrences.First(node =>
                owner.Projection.Parts[node.ElementId].Name == "part00000");
            var mounted = component.FindComponents<PartSvg>().Single(part =>
                part.Find(".part-svg").GetAttribute("data-element-id") == root.ElementId).Instance.Node;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(graph.Groups, Does.Contain(mounted));
                Assert.That(mounted.Position.X, Is.EqualTo(root.Geometry.X));
            }
            await component.InvokeAsync(() => mounted.SetPosition(root.Geometry.X + 10, root.Geometry.Y));
            await component.Find(".diagram-canvas")
                .PointerUpAsync(new Microsoft.AspNetCore.Components.Web.PointerEventArgs { Button = 0 });
            await component.WaitForAssertionAsync(() =>
                Assert.That(owner.State.Occurrences.Single(node => node.Id == root.Id).Geometry.X,
                    Is.EqualTo(root.Geometry.X + 10)));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindComponent<DiagramCanvasHost>().Instance.BlazorDiagram, Is.SameAs(graph));
                Assert.That(DiagramPresentationSerializer.Validate(owner.State, owner.Projection, out _, owner.Limits),
                    Is.True);
            }
            var distant = owner.State.Occurrences[^1];
            await component.InvokeAsync(() => owner.RevealRequested.Execute(distant.ElementId).Subscribe());
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.FindAll($".part-svg[data-element-id='{distant.ElementId}']"),
                    Has.Count.EqualTo(1)));
            Assert.That(component.FindComponent<DiagramCanvasHost>().Instance.BlazorDiagram.Nodes,
                Has.Count.LessThan(40));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task VerifyRevealWaitsForRegionalCanvasBounds(bool beforeMount)
        {
            using var file = new Mycelium.Bloom.Tests.ViewModel.Diagram.LargeWorkspaceFile(600);
            var session = this.context.Services.GetRequiredService<Mycelium.Bloom.Core.Context.WorkspaceModelSession>();
            await session.OpenAsync(file.Uri);
            using var owner = this.context.Services.GetRequiredService<Func<DiagramViewModel>>()();
            owner.BindWorkspace(session);
            await owner.Preparation;
            var bounds = this.context.JSInterop.Setup<Rectangle>("ZBlazorDiagrams.getBoundingClientRect", _ => true);
            var target = owner.State.Occurrences[^1];
            if (beforeMount)
            {
                owner.RevealRequested.Execute(target.ElementId).Subscribe();
            }

            using var component =
                this.context.Render<Interconnection>(parameters => parameters.Add(value => value.ViewModel, owner));
            if (!beforeMount)
            {
                await component.InvokeAsync(() => owner.RevealRequested.Execute(target.ElementId).Subscribe());
            }

            Assert.That(owner.State.Viewport, Is.EqualTo(new DiagramViewport(0, 0, 1)));
            bounds.SetResult(new Rectangle(0, 0, 900, 700));
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.FindAll($".part-svg[data-element-id='{target.ElementId}']"),
                    Has.Count.EqualTo(1)));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.State.Viewport.X, Is.LessThan(-1000));
                Assert.That(owner.PendingRevealElementId, Is.Null);
            }
        }

        [Test]
        public async Task VerifyReconciliationPreservesExactGeometryWithoutRedundantMoves()
        {
            using var owner = this.context.Services.GetRequiredService<Func<DiagramViewModel>>()();
            await this.InitializeDiagramAsync(owner);
            using var component = this.context.Render<Interconnection>(parameters => parameters.Add(value => value.ViewModel, owner));
            await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".part-svg__port"), Has.Count.EqualTo(10)));
            var diagram = component.FindComponent<DiagramCanvasHost>().Instance.BlazorDiagram;
            var occurrence = owner.State.Occurrences.Single(item => owner.Projection.Parts[item.ElementId].Name == "aocs : AOCS");
            var node = diagram.Nodes.Single(item => item.Id == occurrence.Id.ToString());
            var moves = 0;
            node.Moving += _ => moves++;
            await component.InvokeAsync(() => owner.IsReadOnly = true);
            await component.WaitForAssertionAsync(() => Assert.That(node.Locked, Is.True));
            await component.InvokeAsync(() => owner.IsReadOnly = false);
            await component.WaitForAssertionAsync(() => Assert.That(node.Locked, Is.False));
            Assert.That(moves, Is.Zero);

            const double delta = 1.0 / 1024;
            var geometry = occurrence.Geometry with
            {
                X = occurrence.Geometry.X + delta,
                Y = occurrence.Geometry.Y - delta,
                Width = occurrence.Geometry.Width + delta,
                Height = occurrence.Geometry.Height + delta
            };
            await component.InvokeAsync(() => Assert.That(owner.ApplyPresentation(owner.State with
            {
                Occurrences = owner.State.Occurrences.Replace(occurrence, occurrence with { Geometry = geometry })
            }), Is.True));
            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(node.Position, Is.EqualTo(new Point(geometry.X, geometry.Y)));
                    Assert.That(node.Size, Is.EqualTo(new Size(geometry.Width, geometry.Height)));
                }
            });
            using (Assert.EnterMultipleScope())
            {
                Assert.That(moves, Is.EqualTo(1));
                Assert.That(node.Ports.All(port => double.IsFinite(port.Position.X) && double.IsFinite(port.Position.Y)), Is.True);
            }
            await component.InvokeAsync(() => owner.IsReadOnly = true);
            await component.WaitForAssertionAsync(() => Assert.That(node.Locked, Is.True));
            await component.InvokeAsync(() => owner.IsReadOnly = false);
            await component.WaitForAssertionAsync(() => Assert.That(node.Locked, Is.False));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(moves, Is.EqualTo(1));
                Assert.That(diagram.Nodes.Single(item => item.Id == node.Id), Is.SameAs(node));
            }
        }

        [TestCase(0.5)]
        [TestCase(4)]
        public async Task VerifyViewportBoundariesAndConnectedMovementSurviveRemountWithoutDrift(double zoom)
        {
            using var owner = this.context.Services.GetRequiredService<Func<DiagramViewModel>>()();
            await this.InitializeDiagramAsync(owner);
            var expectedViewport = new DiagramViewport(-20.125, 30.375, zoom);
            Assert.That(owner.ApplyPresentation(owner.State with { Viewport = expectedViewport }), Is.True);
            Mycelium.Bloom.Model.Diagram.DiagramOccurrence occurrence = null;
            Point expectedPosition = null;
            string saved = null;
            var component = this.context.Render<Interconnection>(parameters => parameters.Add(value => value.ViewModel, owner));
            try
            {
                await component.WaitForAssertionAsync(() => Assert.That(component.FindComponents<DiagramCanvasHost>(), Has.Count.EqualTo(1)));
                var diagram = component.FindComponent<DiagramCanvasHost>().Instance.BlazorDiagram;
                await component.WaitForAssertionAsync(() =>
                {
                    using (Assert.EnterMultipleScope())
                    {
                        Assert.That(diagram.Zoom, Is.EqualTo(zoom));
                        Assert.That(diagram.Pan, Is.EqualTo(new Point(expectedViewport.X, expectedViewport.Y)));
                    }
                });
                var panEvents = 0;
                var zoomEvents = 0;
                diagram.PanChanged += () => panEvents++;
                diagram.ZoomChanged += () => zoomEvents++;
                await component.InvokeAsync(() => owner.IsReadOnly = true);
                await component.WaitForAssertionAsync(() => Assert.That(diagram.Nodes.All(node => node.Locked), Is.True));
                await component.InvokeAsync(() => owner.IsReadOnly = false);
                await component.WaitForAssertionAsync(() => Assert.That(diagram.Nodes.Any(node => !node.Locked), Is.True));
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(panEvents, Is.Zero);
                    Assert.That(zoomEvents, Is.Zero);
                }

                var maximumPan = diagram.Container.Width - owner.State.Occurrences.Min(node => node.Geometry.X) * zoom + 400;
                await component.InvokeAsync(() =>
                {
                    diagram.TriggerPointerDown(null, new Blazor.Diagrams.Core.Events.PointerEventArgs(0, 0, 0, 1, false, false, false, 1, 1, 1, 0, 0, 0, "mouse", true));
                    diagram.SetPan(Math.BitIncrement(maximumPan), expectedViewport.Y);
                    diagram.TriggerPointerUp(null, new Blazor.Diagrams.Core.Events.PointerEventArgs(0, 0, 0, 1, false, false, false, 1, 1, 1, 0, 0, 0, "mouse", true));
                });
                await component.WaitForAssertionAsync(() =>
                {
                    using (Assert.EnterMultipleScope())
                    {
                        Assert.That(component.FindComponent<DiagramCanvasHost>().Instance.BlazorDiagram.Pan.X, Is.EqualTo(maximumPan));
                        Assert.That(owner.State.Viewport.X, Is.EqualTo(maximumPan));
                    }
                });
                Assert.That(panEvents, Is.EqualTo(2));
                await component.InvokeAsync(() => Assert.That(owner.ApplyPresentation(owner.State with { Viewport = expectedViewport }), Is.True));
                await component.WaitForAssertionAsync(() => Assert.That(component.FindComponent<DiagramCanvasHost>().Instance.BlazorDiagram.Pan.X, Is.EqualTo(expectedViewport.X)));

                await component.InvokeAsync(() => Assert.That(owner.ApplyPresentation(owner.State with
                {
                    Viewport = owner.State.Viewport with { Zoom = Math.BitDecrement(0.5) }
                }), Is.True));
                await component.WaitForAssertionAsync(() => Assert.That(owner.State.Viewport.Zoom, Is.EqualTo(0.5)));
                Assert.That(component.Find("[aria-label='Current zoom']").TextContent.Trim(), Is.EqualTo("50%"));
                diagram = component.FindComponent<DiagramCanvasHost>().Instance.BlazorDiagram;
                occurrence = owner.State.Occurrences.Single(item => owner.Projection.Parts[item.ElementId].Name == "aocs : AOCS");
                var node = diagram.Nodes.Single(item => item.Id == occurrence.Id.ToString());
                const double delta = 1.0 / 1024;
                expectedPosition = new Point(node.Position.X + delta, node.Position.Y - delta);
                await component.InvokeAsync(() =>
                {
                    diagram.SelectModel(node, true);
                    diagram.TriggerPointerDown(node, new Blazor.Diagrams.Core.Events.PointerEventArgs(0, 0, 0, 1, false, false, false, 1, 1, 1, 0, 0, 0, "mouse", true));
                    node.SetPosition(expectedPosition.X, expectedPosition.Y);
                    diagram.TriggerPointerUp(node, new Blazor.Diagrams.Core.Events.PointerEventArgs(0, 0, 0, 1, false, false, false, 1, 1, 1, 0, 0, 0, "mouse", true));
                });
                await component.WaitForAssertionAsync(() =>
                {
                    var accepted = owner.State.Occurrences.Single(item => item.Id == occurrence.Id);
                    using (Assert.EnterMultipleScope())
                    {
                        Assert.That(accepted.Geometry.X, Is.EqualTo(expectedPosition.X));
                        Assert.That(accepted.Geometry.Y, Is.EqualTo(expectedPosition.Y));
                    }
                });
                Assert.That(diagram.Links, Is.Not.Empty);
                saved = DiagramPresentationSerializer.Export(owner.State);
            }
            finally
            {
                await component.InvokeAsync(async () => await component.Instance.DisposeAsync());
                component.Dispose();
            }

            var restoredComponent = this.context.Render<Interconnection>(parameters => parameters.Add(value => value.ViewModel, owner));
            try
            {
                await restoredComponent.WaitForAssertionAsync(() =>
                {
                    var remounted = restoredComponent.FindComponent<DiagramCanvasHost>().Instance.BlazorDiagram;
                    using (Assert.EnterMultipleScope())
                    {
                        Assert.That(remounted.Nodes.Single(item => item.Id == occurrence.Id.ToString()).Position, Is.EqualTo(expectedPosition));
                        Assert.That(DiagramPresentationSerializer.Export(owner.State), Is.EqualTo(saved));
                    }
                });
            }
            finally
            {
                await restoredComponent.InvokeAsync(async () => await restoredComponent.Instance.DisposeAsync());
                restoredComponent.Dispose();
            }
        }

        [TestCase(TaskStatus.RanToCompletion, false)]
        [TestCase(TaskStatus.Faulted, false)]
        [TestCase(TaskStatus.Canceled, false)]
        [TestCase(TaskStatus.RanToCompletion, true)]
        [TestCase(TaskStatus.Faulted, true)]
        [TestCase(TaskStatus.Canceled, true)]
        public async Task VerifyBrowserScrollCompletionAfterDisposalDoesNotPublish(TaskStatus completionStatus,
            bool disposeSynchronously)
        {
            var read = new TaskCompletionSource<Mycelium.Bloom.Model.BrowserScrollPosition>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var started = new TaskCompletionSource<System.Threading.CancellationToken>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var module = new Mock<IJSObjectReference>();
            module.Setup(value => value.InvokeAsync<Mycelium.Bloom.Model.BrowserScrollPosition>(
                    "readScroll", It.IsAny<System.Threading.CancellationToken>(), It.IsAny<object[]>()))
                .Returns((string _, System.Threading.CancellationToken token, object[] _) =>
                {
                    started.TrySetResult(token);
                    return new ValueTask<Mycelium.Bloom.Model.BrowserScrollPosition>(read.Task);
                });
            var runtime = new Mock<IJSRuntime>();
            runtime.Setup(value => value.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()))
                .ReturnsAsync(module.Object);
            this.context.Services.AddSingleton(runtime.Object);
            using var owner = this.context.Services.GetRequiredService<Func<Mycelium.Bloom.ViewModel.ProjectBrowser.IProjectBrowserViewModel>>()();
            await owner.InitializeAsync(System.Threading.CancellationToken.None);
            var publications = 0;
            var browser = this.context.Render<Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser.ProjectBrowser>(parameters => parameters
                .Add(value => value.ViewModel, owner)
                .Add(value => value.ScrollPositionChanged, _ => publications++));
            var pending = browser.Find(".mb-project-browser__tree-viewport").TriggerEventAsync("onscroll", EventArgs.Empty);
            var cancellationToken = await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.That(cancellationToken.IsCancellationRequested, Is.False);
            if (disposeSynchronously)
            {
                await browser.InvokeAsync(browser.Instance.Dispose);
                Assert.That(cancellationToken.IsCancellationRequested, Is.True);
            }

            await browser.InvokeAsync(async () => await browser.Instance.DisposeAsync());
            Assert.That(cancellationToken.IsCancellationRequested, Is.True);
            switch (completionStatus)
            {
                case TaskStatus.RanToCompletion:
                    read.SetResult(new Mycelium.Bloom.Model.BrowserScrollPosition(10, 200));
                    break;
                case TaskStatus.Faulted:
                    read.SetException(new JSException("The disposed scroll target is unavailable."));
                    break;
                case TaskStatus.Canceled:
                    read.SetCanceled(cancellationToken);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(completionStatus));
            }

            await pending;
            Assert.That(publications, Is.Zero);
            module.Verify(value => value.DisposeAsync(), Times.Once);
        }

        private async Task<SysML2.NET.Core.POCO.Root.Namespaces.INamespace> LoadExampleAsync()
        {
            var session = this.context.Services.GetRequiredService<Mycelium.Bloom.Core.Context.WorkspaceModelSession>();
            if (session.Current.Root?.DeclaredName != "Interconnection demonstration")
            {
                await session.OpenAsync(new Uri(System.IO.Path.Combine(TestRepository.GetDirectoryPath("Mycelium.Bloom"),
                    "Resources", "Prototypes", "Interconnection.json")));
            }

            return session.Current.Root;
        }

        private async Task InitializeDiagramAsync(DiagramViewModel viewModel)
        {
            await this.LoadExampleAsync();
            viewModel.UpdateWorkspace(this.context.Services.GetRequiredService<Mycelium.Bloom.Core.Context.WorkspaceModelSession>().Current);
            await viewModel.Preparation;
            Assert.That(viewModel.ApplyPresentation(viewModel.State with { Viewport = new DiagramViewport(0, 0, 0.5) }), Is.True);
        }

        private async Task OpenDiagramAsync(IRenderedComponent<Modelling> component)
        {
            var session = this.context.Services.GetRequiredService<Mycelium.Bloom.Core.Context.WorkspaceModelSession>();
            using var stream = System.IO.File.OpenRead(System.IO.Path.Combine(TestRepository.GetDirectoryPath("Mycelium.Bloom"), "Resources", "Prototypes", "Interconnection.json"));
            using var model = await session.OpenContextAsync(stream, System.Threading.CancellationToken.None);
            var menu = component.FindComponents<ActionMenu>()[^1].Instance;
            await component.InvokeAsync(() => menu.ItemSelected.InvokeAsync(menu.Items.Single(item => item.Id == "open-interconnection")));
            var opener = component.FindComponent<Mycelium.Bloom.Components.UI.Organisms.ModelOpening.ModelOpening>();
            await component.InvokeAsync(() => opener.Instance.Opened.InvokeAsync(model));
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.FindComponents<Interconnection>(), Has.Count.EqualTo(1)));
        }
    }
}
