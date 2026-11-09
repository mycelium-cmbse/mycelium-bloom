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
    using Mycelium.Bloom.Components.UI.Organisms.InterconnectionCanvas;
    using Mycelium.Bloom.Components.UI.Organisms.DiagramBenchmarkProbe;
    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Extensions;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Diagram;
    using Mycelium.Bloom.ViewModel.WorkspaceEditor;
    using SysML2.NET.Core.Core.Types;

    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class DiagramCompositionTestFixture : BunitContext
    {
        public DiagramCompositionTestFixture()
        {
            BlueprintTestSetup.Configure(this);
            var environment = new Mock<IHostEnvironment>(MockBehavior.Strict);
            environment.SetupGet(value => value.EnvironmentName).Returns(Environments.Production);
            environment.SetupGet(value => value.ContentRootPath)
                .Returns(TestRepository.GetDirectoryPath("Mycelium.Bloom"));
            this.Services.AddSingleton(environment.Object);
            this.Services.AddLogging();
            this.Services.AddApplicationServices();
            this.Services.AddSingleton<IOptions<DiagramBenchmarkOptions>>(Options.Create(new DiagramBenchmarkOptions
            { Enabled = true }));
            this.JSInterop.Mode = JSRuntimeMode.Loose;
            this.JSInterop.Setup<Rectangle>("ZBlazorDiagrams.getBoundingClientRect", _ => true)
                .SetResult(new Rectangle(0, 0, 1000, 700));
        }

        [Test]
        public async Task VerifyFreshDefaultDiagramIsNotRecreatedAfterCloseOrRerender()
        {
            using var editor =
                new WorkspaceEditorViewModel(Options.Create(new WorkspaceEditorOptions { MaximumGroupCount = 4 }));
            this.Services.AddScoped<Func<IWorkspaceEditorViewModel>>(_ => () => editor);
            var session = this.Services.GetRequiredService<Mycelium.Bloom.Core.Context.WorkspaceModelSession>();
            await session.EnsureLoadedAsync();
            using var component = this.Render<Modelling>();
            var group = editor.Groups[2];
            var initial = group.ActiveTab;
            Assert.That(editor.Groups.Select(value => value.ActiveTab.ViewTypeKey),
                Is.EqualTo(new[] { "project-browser", "placeholder", "interconnection" }));
            Assert.That(initial.Title, Is.EqualTo("Interconnection"));
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.Markup, Does.Contain("No structural parts to display")));
            component.Render();
            Assert.That(group.Tabs.Single(), Is.SameAs(initial));
            await component.Find($"[data-testid='editor-workspace-tab-close'][data-tab-id='{initial.Id}']")
                .ClickAsync();
            component.Render();
            Assert.That(editor.Groups.SelectMany(value => value.Tabs).Any(tab => tab.ViewTypeKey == "interconnection"),
                Is.False);
            Assert.That(
                editor.Groups.SelectMany(value => value.Tabs).Count(tab => tab.ViewTypeKey == "project-browser"),
                Is.EqualTo(1));
        }

        [Test]
        public async Task VerifyIndependentDiagramBrowserPairsReuseOnlyCanonicalModel()
        {
            using var editor =
                new WorkspaceEditorViewModel(Options.Create(new WorkspaceEditorOptions { MaximumGroupCount = 4 }));
            this.Services.AddScoped<Func<IWorkspaceEditorViewModel>>(_ => () => editor);
            var session = this.Services.GetRequiredService<Mycelium.Bloom.Core.Context.WorkspaceModelSession>();
            await session.EnsureLoadedAsync();
            using var stream = System.IO.File.OpenRead(System.IO.Path.Combine(
                TestRepository.GetDirectoryPath("Mycelium.Bloom"),
                "Resources", "Prototypes", "Interconnection.json"));
            using var model = await session.OpenContextAsync(stream, System.Threading.CancellationToken.None);
            using var component = this.Render<Modelling>();
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
            var menu = component.FindComponents<ActionMenu>().Last().Instance;
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
            Assert.That(secondTab.Id, Is.Not.EqualTo(firstTab.Id));
            Assert.That(second.State.DiagramId, Is.Not.EqualTo(first.State.DiagramId));
            Assert.That(second.ModelContextId, Is.EqualTo(first.ModelContextId));
            Assert.That(secondBrowserTab, Is.Not.SameAs(firstBrowserTab));
            Assert.That(secondBrowser, Is.Not.SameAs(firstBrowser));
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
            Assert.That(secondBrowser.FilterText, Is.Empty);
            Assert.That(secondBrowser.RootNodes[0].IsExpanded, Is.True);
            Assert.That(second.State, Is.SameAs(unchanged));
            var count = browserGroup.Tabs.Count;
            open = component.FindComponent<Mycelium.Bloom.Components.UI.Organisms.ModelOpening.ModelOpening>();
            await component.InvokeAsync(() => open.Instance.Opened.InvokeAsync(model));
            Assert.That(browserGroup.Tabs.Count, Is.EqualTo(count));
            Assert.That(second.State, Is.SameAs(unchanged));

            var secondRequests = new List<string>();
            using var secondSubscription = second.RevealRequested.Subscribe(secondRequests.Add);
            await component.InvokeAsync(() => editor.ActivateTab(browserGroup.Id, firstBrowserTab.Id));
            var browser = component
                .FindComponent<Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser.ProjectBrowser>().Instance;
            var canonical =
                model.Snapshot.Root.ownedElement.First(value => first.Projection.Parts.ContainsKey(value.ElementId));
            var part = first.Projection.Parts[canonical.ElementId];
            var node = firstBrowser.RootNodes[0].FindPathTo(canonical).Last();
            await component.InvokeAsync(() => browser.SelectedNodeChanged.InvokeAsync(node));
            await component.InvokeAsync(() => browser.SelectedNodeChanged.InvokeAsync(node));
            Assert.That(firstRequests, Is.EqualTo(new[] { part.ElementId, part.ElementId }));
            Assert.That(secondRequests, Is.Empty);
            Assert.That(second.State, Is.SameAs(unchanged));
            Assert.That(diagramGroup.ActiveTab, Is.SameAs(secondTab));
            await component.InvokeAsync(() => editor.MoveTab(diagramGroup.Id, firstTab.Id, editor.Groups[1].Id));
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.FindComponents<Interconnection>(), Has.Count.EqualTo(2)));
            var restored = first.State;
            var firstSelection = first.SelectedOccurrences.ToArray();
            var selected = second.State.Occurrences.First();
            await component.InvokeAsync(() => second.SelectAsync([selected.Id], selected.Id));
            Assert.That(first.State, Is.SameAs(restored));
            Assert.That(first.SelectedOccurrences, Is.EqualTo(firstSelection));
            await component.Find($"[data-testid='editor-workspace-tab-close'][data-tab-id='{firstTab.Id}']")
                .ClickAsync();
            Assert.That(second.IsScopeAvailable, Is.True);
            Assert.That(secondBrowser.RootNodes.Single().SourceElement, Is.SameAs(model.Snapshot.Root));
        }

        [Test]
        public void VerifyModelOpeningIsAvailableWithoutAuthentication()
        {
            using var component = this.Render<Mycelium.Bloom.Components.UI.Organisms.ModelOpening.ModelOpening>();
            Assert.That(component.Find("button").HasAttribute("disabled"), Is.False);
            Assert.That(component.Markup, Does.Not.Contain("requires authorization"));
            Assert.That(component.FindAll("input[type='file']"), Is.Empty);
        }

        [Test]
        public async Task VerifyAlreadyOpenChoiceBindsItsValueAndPreservesWorkspace()
        {
            var session = this.Services.GetRequiredService<Mycelium.Bloom.Core.Context.WorkspaceModelSession>();
            await session.EnsureLoadedAsync();
            var original = session.Current;
            using var file = new Mycelium.Bloom.Tests.ViewModel.Diagram.WorkspaceModelFile("Existing model", "part");
            await using var stream = System.IO.File.OpenRead(file.Uri.LocalPath);
            using var lease = await session.OpenContextAsync(stream, System.Threading.CancellationToken.None);
            Mycelium.Bloom.Model.WorkspaceModelSnapshot chosen = null;
            using var portal = this.Render<BlazorBlueprint.Primitives.Services.BbPortalHost>();
            using var component =
                this.Render<Mycelium.Bloom.Components.UI.Organisms.ModelOpening.ModelOpening>(parameters => parameters
                    .Add(value => value.ExistingModels, new[] { lease.Snapshot })
                    .Add(value => value.Opened, value => chosen = value.Snapshot));
            await component.Find("button").ClickAsync();
            var select = portal.FindComponent<Mycelium.Bloom.Components.UI.Atoms.SelectInput.SelectInput>();
            Assert.That(select.Instance.Value, Is.Null.Or.Empty);
            await component.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(lease.Snapshot.Id.ToString()));
            Assert.That(select.Instance.Value, Is.EqualTo(lease.Snapshot.Id.ToString()));
            await portal.FindAll("button").Single(button => button.TextContent.Trim() == "Open for this session")
                .ClickAsync();
            Assert.That(chosen, Is.SameAs(lease.Snapshot));
            Assert.That(session.Current, Is.SameAs(original));
        }

        [Test]
        public async Task VerifyExampleRequiresExplicitChoiceAndUsesCanonicalWorkspace()
        {
            var session = this.Services.GetRequiredService<Mycelium.Bloom.Core.Context.WorkspaceModelSession>();
            await session.EnsureLoadedAsync();
            var previous = session.Current;
            using var portal = this.Render<BlazorBlueprint.Primitives.Services.BbPortalHost>();
            Mycelium.Bloom.Model.WorkspaceModelLease lease = null;
            using var component =
                this.Render<Mycelium.Bloom.Components.UI.Organisms.ModelOpening.ModelOpening>(parameters => parameters
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
            var projection = InterconnectionProjection.Create(lease.Snapshot.Root);
            Assert.That(projection.Parts, Has.Count.EqualTo(7));
            Assert.That(projection.Parts.Values.Sum(part => part.Ports.Length), Is.EqualTo(10));
            Assert.That(projection.Connections, Has.Count.EqualTo(5));
            Assert.That(projection.Parts.Values.SelectMany(part => part.Ports).All(port => port.Direction.HasValue),
                Is.True);
            foreach (var part in projection.Parts.Values)
            {
                Assert.That(await this.Services
                    .GetRequiredService<Mycelium.Bloom.Core.ModelLoading.IElementIdResolver>()
                    .ResolveAsync(part.ElementId, System.Threading.CancellationToken.None), Is.Not.Null);
            }
        }

        [Test]
        public async Task VerifyTabSwitchMoveCloseAndPageDisposalRespectExactOwnership()
        {
            using var editor =
                new WorkspaceEditorViewModel(Options.Create(new WorkspaceEditorOptions { MaximumGroupCount = 4 }));
            this.Services.AddScoped<Func<IWorkspaceEditorViewModel>>(_ => () => editor);
            var owned = new List<DiagramViewModel>();
            this.Services.AddScoped<Func<DiagramViewModel>>(provider => () =>
            {
                var viewModel = ActivatorUtilities.CreateInstance<DiagramViewModel>(provider);
                owned.Add(viewModel);
                return viewModel;
            });
            using var component = this.Render<Modelling>();
            var source = editor.Groups[2];
            var destination = editor.Groups[1];
            var placeholder = source.ActiveTab;
            await this.OpenDiagramAsync(component);
            var first = owned[1];
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
            var second = owned[2];
            Assert.That(second, Is.Not.SameAs(first));
            component.Instance.Dispose();
            Assert.That(second.AddExisting(second.State.Occurrences[0].ElementId), Is.False);
        }

        [Test]
        [SetCulture("fr-BE")]
        public async Task VerifyComponentDisposalLeavesTheCallerOwnedTabStateAlive()
        {
            var module =
                this.JSInterop.SetupModule("./Components/UI/Organisms/Interconnection/Interconnection.razor.js");
            module.SetupVoid("mount", _ => true).SetVoidResult();
            module.SetupVoid("unmount", _ => true).SetVoidResult();
            using var viewModel = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            viewModel.Initialize(this.Services
                .GetRequiredService<Mycelium.Bloom.Core.ModelLoading.InterconnectionPrototype>().Model);
            var occurrence = viewModel.State.Occurrences[0];
            Assert.That(viewModel.ApplyPresentation(viewModel.State with
            {
                Occurrences = viewModel.State.Occurrences.Replace(occurrence,
                    occurrence with { Geometry = occurrence.Geometry with { Width = 200.5 } })
            }), Is.True);
            var component =
                this.Render<Interconnection>(parameters => parameters.Add(value => value.ViewModel, viewModel));
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
            this.Services.AddSingleton(runtime.Object);
            var module = new Mock<IJSObjectReference>(MockBehavior.Loose);
            using var viewModel = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            viewModel.Initialize(this.Services
                .GetRequiredService<Mycelium.Bloom.Core.ModelLoading.InterconnectionPrototype>().Model);
            var component = this.Render<Interconnection>(parameters =>
                parameters.Add(value => value.ViewModel, viewModel).Add(value => value.ShowDiagnostics, true));
            try
            {
                await component.WaitForAssertionAsync(() => runtime.Verify(
                    value => value.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()), Times.Once));
                await Task.Run(() => viewModel.ReportNotice("Background operation completed."));
                await component.WaitForAssertionAsync(() => Assert.That(component.Find("[role='status']").TextContent,
                    Does.Contain("Background operation completed.")));
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

            this.Services.AddSingleton(runtime.Object);
            using var owner = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            owner.Initialize(this.Services
                .GetRequiredService<Mycelium.Bloom.Core.ModelLoading.InterconnectionPrototype>().Model);
            var component = this.Render<Interconnection>(parameters => parameters.Add(value => value.ViewModel, owner));
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

            Assert.That(async () => await disposal,
                Throws.TypeOf<JSException>().With.Message.EqualTo($"{failingOperation} failure"));
            await component.Instance.DisposeAsync();
            module.Verify(
                value => value.InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>("unmount",
                    It.IsAny<object[]>()), Times.Once);
            module.Verify(value => value.DisposeAsync(), Times.Once);
            Assert.That(owner.AddExisting(owner.State.Occurrences[0].ElementId), Is.True);
        }

        [Test]
        public async Task VerifyRepeatedOccurrencesResolvePortsWithinTheirOwnConnections()
        {
            using var viewModel = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            viewModel.Initialize(this.Services
                .GetRequiredService<Mycelium.Bloom.Core.ModelLoading.InterconnectionPrototype>().Model);
            viewModel.DisplayStressScene();
            this.JSInterop.Setup<Rectangle>("ZBlazorDiagrams.getBoundingClientRect", _ => true)
                .SetResult(new Rectangle(0, 0, 20000, 20000));
            var component =
                this.Render<Interconnection>(parameters => parameters.Add(value => value.ViewModel, viewModel));
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.FindAll(".part-svg"), Has.Count.EqualTo(126)));

            foreach (var part in viewModel.Projection.Parts.Values.Where(part => part.Ports.Length > 0))
            {
                var occurrences = component.FindAll($".part-svg[data-element-id='{part.ElementId}']");
                var portSignatures = occurrences.Select(element => string.Join(";",
                    element.QuerySelectorAll(".part-svg__port image, .part-svg__port rect").Select(port =>
                        $"{port.GetAttribute("x")},{port.GetAttribute("y")}"))).Distinct().ToArray();
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(occurrences, Has.Count.EqualTo(18));
                    Assert.That(
                        occurrences.Select(element =>
                            element.QuerySelectorAll(".part-svg__port image, .part-svg__port rect").Length),
                        Is.All.EqualTo(part.Ports.Length));
                    Assert.That(portSignatures, Has.Length.EqualTo(1), part.Name);
                }
            }
        }

        [TestCase(FeatureDirectionKind.In)]
        [TestCase(FeatureDirectionKind.Out)]
        [TestCase(FeatureDirectionKind.Inout)]
        [TestCase(null)]
        public async Task VerifyPortGlyphPreservesDirectionAndBoundaryCenter(FeatureDirectionKind? direction)
        {
            var root = this.Services.GetRequiredService<Mycelium.Bloom.Core.ModelLoading.InterconnectionPrototype>()
                .Model;
            foreach (var canonical in InterconnectionProjection.Traverse(root)
                         .OfType<SysML2.NET.Core.POCO.Systems.Ports.IPortUsage>())
            {
                canonical.Direction = direction;
            }

            using var owner = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            owner.Initialize(root);
            using var component =
                this.Render<Interconnection>(parameters => parameters.Add(value => value.ViewModel, owner));
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
                Assert.That(asset.Attribute("viewBox")?.Value, Is.EqualTo("0 0 20 20"));
                Assert.That(asset.Descendants().Count(element => element.Name.LocalName == "path"), Is.EqualTo(2));
            }
            else
            {
                Assert.That(component.FindAll(".part-svg__port-direction"), Is.Empty);
                Assert.That(component.FindAll(".part-svg__port rect"), Has.Count.EqualTo(10));
            }
        }

        [Test]
        public async Task VerifyPlainConnectionsAndMultipleSelectionDoNotInventFlowOrPresence()
        {
            using var owner = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            owner.Initialize(this.Services
                .GetRequiredService<Mycelium.Bloom.Core.ModelLoading.InterconnectionPrototype>().Model);
            using var component =
                this.Render<Interconnection>(parameters => parameters.Add(value => value.ViewModel, owner));
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

        [TestCase(500, 0, PortAlignment.Right)]
        [TestCase(-500, 0, PortAlignment.Left)]
        [TestCase(0, 500, PortAlignment.Bottom)]
        [TestCase(0, -500, PortAlignment.Top)]
        [TestCase(500, 500, PortAlignment.Right)]
        public async Task VerifyBoundarySidesFollowMovedEndpoints(int dx, int dy, PortAlignment expected)
        {
            using var viewModel = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            viewModel.Initialize(this.Services
                .GetRequiredService<Mycelium.Bloom.Core.ModelLoading.InterconnectionPrototype>().Model);
            var component =
                this.Render<Interconnection>(parameters => parameters.Add(value => value.ViewModel, viewModel));
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.FindAll(".part-svg"), Has.Count.EqualTo(7)));
            var route = viewModel.State.Routes[0];
            var source = viewModel.State.Occurrences.Single(node => node.Id == route.SourceId);
            var target = viewModel.State.Occurrences.Single(node => node.Id == route.TargetId);
            await component.InvokeAsync(() => Assert.That(viewModel.ApplyPresentation(viewModel.State with
            {
                Occurrences = viewModel.State.Occurrences.Replace(target, target with
                {
                    Geometry = target.Geometry with
                    {
                        X = source.Geometry.X + source.Geometry.Width / 2 + dx - target.Geometry.Width / 2,
                        Y = source.Geometry.Y + source.Geometry.Height / 2 + dy - target.Geometry.Height / 2
                    }
                })
            }), Is.True));
            await component.WaitForAssertionAsync(() =>
            {
                var node = component.FindComponents<PartSvg>()
                    .Single(part => part.Instance.Node.Id == source.Id.ToString()).Instance.Node;
                Assert.That(node.Ports.Single(port => port.Id == route.SourcePortId).Alignment, Is.EqualTo(expected));
            });
        }

        [Test]
        public async Task VerifyBoundarySidesTrackConnectionPrecedenceAndOccurrenceReplacement()
        {
            using var viewModel = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            viewModel.Initialize(this.Services
                .GetRequiredService<Mycelium.Bloom.Core.ModelLoading.InterconnectionPrototype>().Model);
            using var component =
                this.Render<Interconnection>(parameters => parameters.Add(value => value.ViewModel, viewModel));
            var original = viewModel.State;
            var route = original.Routes[0];
            var source = original.Occurrences.Single(node => node.Id == route.SourceId);
            var target = original.Occurrences.Single(node => node.Id == route.TargetId);
            var duplicate = target with
            {
                Id = Guid.NewGuid(),
                Geometry = target.Geometry with { X = source.Geometry.X - 600 }
            };
            var alternative = route with { Id = Guid.NewGuid(), TargetId = duplicate.Id };
            await component.InvokeAsync(() => Assert.That(viewModel.ApplyPresentation(original with
            {
                Occurrences = original.Occurrences.Add(duplicate),
                Routes = original.Routes.Add(alternative)
            }), Is.True));
            await component.WaitForAssertionAsync(() =>
                Assert.That(FindPort(component, source.Id, route.SourcePortId).Alignment,
                    Is.EqualTo(PortAlignment.Right)));
            await component.InvokeAsync(() => Assert.That(viewModel.ApplyPresentation(viewModel.State with
            {
                Routes = viewModel.State.Routes.Remove(alternative).Insert(0, alternative)
            }), Is.True));
            await component.WaitForAssertionAsync(() =>
                Assert.That(FindPort(component, source.Id, route.SourcePortId).Alignment,
                    Is.EqualTo(PortAlignment.Left)));
            await component.InvokeAsync(() => Assert.That(viewModel.ApplyPresentation(viewModel.State with
            {
                Routes = original.Routes.Replace(route, route with { TargetId = duplicate.Id })
            }), Is.True));
            await component.WaitForAssertionAsync(() =>
            {
                Assert.That(FindPort(component, source.Id, route.SourcePortId).Alignment,
                    Is.EqualTo(PortAlignment.Left));
                Assert.That(FindPort(component, target.Id, route.TargetPortId).Alignment,
                    Is.EqualTo(PortAlignment.Right));
            });
            var accepted = viewModel.State;
            await component.InvokeAsync(() => Assert.That(viewModel.ApplyPresentation(accepted with
            {
                Routes = accepted.Routes.SetItem(0, accepted.Routes[0] with { SourcePortId = "invalid-port" })
            }), Is.False));
            Assert.That(viewModel.State, Is.SameAs(accepted));
            await component.InvokeAsync(() => Assert.That(viewModel.ApplyPresentation(original), Is.True));
            await component.WaitForAssertionAsync(() =>
            {
                Assert.That(component.FindAll(".part-svg"), Has.Count.EqualTo(7));
                Assert.That(FindPort(component, source.Id, route.SourcePortId).Alignment,
                    Is.EqualTo(PortAlignment.Right));
            });
            await component.InvokeAsync(() => Assert.That(viewModel.ApplyPresentation(original with
            {
                Occurrences = original.Occurrences.Replace(source, source with { ElementId = target.ElementId }),
                Routes = []
            }), Is.True));
            await component.WaitForAssertionAsync(() =>
            {
                var node = component.FindComponents<PartSvg>()
                    .Single(part => part.Instance.Node.Id == source.Id.ToString()).Instance.Node;
                Assert.That(node.Ports.Select(port => port.Id),
                    Is.EquivalentTo(viewModel.Projection.Parts[target.ElementId].Ports.Select(port => port.ElementId)));
                Assert.That(node.Ports.Select(port => port.Alignment), Is.All.EqualTo(PortAlignment.Right));
            });
        }

        [Test]
        public async Task VerifyBoundarySidesFollowSizeAndIgnoreViewportAndBends()
        {
            using var viewModel = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            viewModel.Initialize(this.Services
                .GetRequiredService<Mycelium.Bloom.Core.ModelLoading.InterconnectionPrototype>().Model);
            using var component =
                this.Render<Interconnection>(parameters => parameters.Add(value => value.ViewModel, viewModel));
            var original = viewModel.State;
            var route = original.Routes[0];
            var source = original.Occurrences.Single(node => node.Id == route.SourceId);
            await component.WaitForAssertionAsync(() =>
                Assert.That(FindPort(component, source.Id, route.SourcePortId).Alignment,
                    Is.EqualTo(PortAlignment.Right)));
            await component.InvokeAsync(() => Assert.That(viewModel.ApplyPresentation(original with
            {
                Occurrences = original.Occurrences.Replace(source,
                    source with { Geometry = source.Geometry with { Width = 2000 } })
            }), Is.True));
            await component.WaitForAssertionAsync(() =>
                Assert.That(FindPort(component, source.Id, route.SourcePortId).Alignment,
                    Is.EqualTo(PortAlignment.Left)));
            var port = FindPort(component, source.Id, route.SourcePortId);
            await component.InvokeAsync(() => Assert.That(viewModel.ApplyPresentation(viewModel.State with
            {
                Viewport = new DiagramViewport(-80, 40, 0.5),
                Routes = original.Routes.Replace(route, route with { Bends = [new DiagramPoint(10, 900)] })
            }), Is.True));
            await component.WaitForAssertionAsync(() =>
            {
                Assert.That(FindPort(component, source.Id, route.SourcePortId), Is.SameAs(port));
                Assert.That(port.Alignment, Is.EqualTo(PortAlignment.Left));
            });
        }

        [Test]
        public async Task VerifyBoundarySidesSurviveNestingAndViewModelReplacement()
        {
            var model = this.Services.GetRequiredService<Mycelium.Bloom.Core.ModelLoading.InterconnectionPrototype>()
                .Model;
            using var first = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            using var second = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            first.Initialize(model);
            second.Initialize(model);
            var original = first.State;
            var child = original.Occurrences.Single(node => node.ParentId.HasValue);
            var parent = original.Occurrences.Single(node => node.Id == child.ParentId);
            using var component =
                this.Render<Interconnection>(parameters => parameters.Add(value => value.ViewModel, first));
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.FindComponents<PartSvg>().Count(part => part.Instance.Node is GroupModel),
                    Is.EqualTo(1)));
            var sides = component.FindComponents<PartSvg>().Single(part => part.Instance.Node is GroupModel).Instance
                .Node.Ports.Select(port => port.Alignment).ToArray();
            await component.InvokeAsync(() => Assert.That(first.ApplyPresentation(original with
            {
                Occurrences = original.Occurrences.Replace(child, child with { ParentId = null })
            }), Is.True));
            await component.WaitForAssertionAsync(() =>
            {
                var parts = component.FindComponents<PartSvg>();
                Assert.That(parts.Any(part => part.Instance.Node is GroupModel), Is.False);
                Assert.That(
                    parts.Single(part => part.Instance.Node.Id == parent.Id.ToString()).Instance.Node.Ports
                        .Select(port => port.Alignment), Is.EqualTo(sides));
            });
            await component.InvokeAsync(() => Assert.That(first.ApplyPresentation(original), Is.True));
            await component.WaitForAssertionAsync(() =>
                Assert.That(
                    component.FindComponents<PartSvg>().Single(part => part.Instance.Node is GroupModel).Instance.Node
                        .Ports.Select(port => port.Alignment), Is.EqualTo(sides)));
            component.Render(parameters => parameters.Add(value => value.ViewModel, second));
            var route = second.State.Routes[0];
            await component.WaitForAssertionAsync(() =>
                Assert.That(FindPort(component, route.SourceId, route.SourcePortId).Alignment,
                    Is.EqualTo(PortAlignment.Right)));
            await component.InvokeAsync(() =>
                Assert.That(first.ApplyPresentation(original with { Routes = [] }), Is.True));
            await component.WaitForAssertionAsync(() =>
            {
                Assert.That(component.FindAll(".connection-svg__line"), Has.Count.EqualTo(5));
                Assert.That(FindPort(component, route.SourceId, route.SourcePortId).Alignment,
                    Is.EqualTo(PortAlignment.Right));
            });
        }

        [Test]
        public async Task VerifyViewportRetainsOffscreenStateAndExportsEveryOccurrence()
        {
            using var owner = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            owner.Initialize(this.Services
                .GetRequiredService<Mycelium.Bloom.Core.ModelLoading.InterconnectionPrototype>().Model);
            var distant = owner.State.Occurrences.Single(node =>
                owner.Projection.Parts[node.ElementId].Name.StartsWith("controller"));
            Assert.That(owner.ApplyPresentation(owner.State with
            {
                Occurrences = owner.State.Occurrences.Replace(distant,
                    distant with { Geometry = distant.Geometry with { X = 8000 } })
            }), Is.True);
            using var component = this.Render<Interconnection>(parameters =>
                parameters.Add(value => value.ViewModel, owner).Add(value => value.ShowDiagnostics, true));
            var diagram = component.FindComponent<DiagramCanvasHost>().Instance.BlazorDiagram;
            Assert.That(component.FindAll(".part-svg"), Has.Count.EqualTo(6));
            Assert.That(diagram.Nodes.Count + diagram.Groups.Count, Is.EqualTo(7));
            Assert.That(diagram.Nodes.Single(node => node.Id == distant.Id.ToString()).Visible, Is.False);
            var saved = DiagramPresentationSerializer.Export(owner.State);
            await component.FindAll("button").Single(button => button.TextContent == "Export SVG").ClickAsync();
            var invocation = this.JSInterop.Invocations.Single(call => call.Identifier == "exportCompleteSvg");
            var svg = new AngleSharp.Html.Parser.HtmlParser().ParseDocument((string)invocation.Arguments[1]);
            Assert.That(svg.QuerySelectorAll(".part-svg"), Has.Length.EqualTo(7));
            Assert.That(DiagramPresentationSerializer.Export(owner.State), Is.EqualTo(saved));
            await component.InvokeAsync(() => diagram.SetPan(-7800, 0));
            await component.WaitForStateAsync(() =>
                diagram.Nodes.Single(node => node.Id == distant.Id.ToString()).Visible);
            Assert.That(component.FindAll(".part-svg"), Has.Count.LessThan(7));
            Assert.That(owner.State.Occurrences, Has.Length.EqualTo(7));
        }

        [Test]
        public async Task VerifyCrossingConnectionSurvivesCulledEndpointsAndBoundaryDrag()
        {
            using var owner = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            owner.Initialize(this.Services
                .GetRequiredService<Mycelium.Bloom.Core.ModelLoading.InterconnectionPrototype>().Model);
            var route = owner.State.Routes.Single(route =>
                owner.Projection.Connections[route.ElementId].Name == "CmdBus");
            var source = owner.State.Occurrences.Single(node => node.Id == route.SourceId);
            var target = owner.State.Occurrences.Single(node => node.Id == route.TargetId);
            Assert.That(owner.ApplyPresentation(owner.State with
            {
                Occurrences = owner.State.Occurrences
                    .Replace(source, source with { Geometry = source.Geometry with { X = -2000 } })
                    .Replace(target, target with { Geometry = target.Geometry with { X = 2500 } }),
                Routes = owner.State.Routes.Replace(route, route with { Bends = [] })
            }), Is.True);
            using var component =
                this.Render<Interconnection>(parameters => parameters.Add(value => value.ViewModel, owner));
            var diagram = component.FindComponent<DiagramCanvasHost>().Instance.BlazorDiagram;
            Assert.That(diagram.Nodes.Single(node => node.Id == source.Id.ToString()).Visible, Is.False);
            Assert.That(diagram.Nodes.Single(node => node.Id == target.Id.ToString()).Visible, Is.False);
            Assert.That(diagram.Links.Single(link => link.Id == route.Id.ToString()).Visible, Is.True);
            var node = diagram.Nodes.First(item => item.Visible);
            await component.InvokeAsync(() =>
            {
                diagram.SelectModel(node, true);
                diagram.TriggerPointerDown(node,
                    new Blazor.Diagrams.Core.Events.PointerEventArgs(0, 0, 0, 1, false, false, false, 1, 1, 1, 0, 0, 0,
                        "mouse", true));
                diagram.SetPan(-2200, 0);
            });
            await component.WaitForStateAsync(() => owner.State.Viewport.X == -2200);
            Assert.That(node.Visible, Is.True);
            await component.InvokeAsync(() => diagram.TriggerPointerUp(node,
                new Blazor.Diagrams.Core.Events.PointerEventArgs(0, 0, 0, 1, false, false, false, 1, 1, 1, 0, 0, 0,
                    "mouse", true)));
            Assert.That(node.Visible, Is.False);
            Assert.That(node.Selected, Is.True);
        }

        [Test]
        public async Task VerifyFitNoticePreservesAcceptedMinimumZoom()
        {
            using var owner = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            owner.Initialize(this.Services
                .GetRequiredService<Mycelium.Bloom.Core.ModelLoading.InterconnectionPrototype>().Model);
            var distant = owner.State.Occurrences.Single(node =>
                owner.Projection.Parts[node.ElementId].Name.StartsWith("controller"));
            Assert.That(owner.ApplyPresentation(owner.State with
            {
                Occurrences = owner.State.Occurrences.Replace(distant,
                    distant with { Geometry = distant.Geometry with { X = 8000 } })
            }), Is.True);
            using var component =
                this.Render<Interconnection>(parameters => parameters.Add(value => value.ViewModel, owner));
            var controls = component.FindComponent<Mycelium.Bloom.Components.UI.Molecules.ZoomControls.ZoomControls>();
            await component.InvokeAsync(() => controls.Instance.OnFitToView.InvokeAsync());
            Assert.That(owner.State.Viewport.Zoom, Is.EqualTo(0.5));
            Assert.That(component.FindComponent<DiagramCanvasHost>().Instance.BlazorDiagram.Zoom, Is.EqualTo(0.5));
            Assert.That(component.Find("[aria-label='Current zoom']").TextContent, Is.EqualTo("50%"));
            Assert.That(owner.Notice, Does.Contain("does not fit"));
        }

        [Test]
        public async Task VerifyRestoredViewportClampsZoomAndPanAndResizingRetainsNestedParts()
        {
            using var owner = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            owner.Initialize(this.Services
                .GetRequiredService<Mycelium.Bloom.Core.ModelLoading.InterconnectionPrototype>().Model);
            using var component =
                this.Render<Interconnection>(parameters => parameters.Add(value => value.ViewModel, owner));
            var diagram = component.FindComponent<DiagramCanvasHost>().Instance.BlazorDiagram;
            await component.InvokeAsync(() =>
                owner.ApplyPresentation(owner.State with { Viewport = new DiagramViewport(-99999, 99999, 0.1) }));
            Assert.That(owner.State.Viewport.Zoom, Is.EqualTo(0.5));
            Assert.That(Math.Abs(owner.State.Viewport.X), Is.LessThan(99999));
            Assert.That(Math.Abs(owner.State.Viewport.Y), Is.LessThan(99999));
            await component.InvokeAsync(() =>
            {
                diagram.SetPan(-550, -160);
                diagram.SetZoom(1);
                diagram.SetContainer(new Rectangle(0, 0, 280, 200));
            });
            Assert.That(component.FindComponents<PartSvg>().Any(part => part.Instance.Node.Group is not null), Is.True);
            Assert.That(component.FindComponents<PartSvg>().Any(part => part.Instance.Node is GroupModel), Is.True);
            var snapshot = owner.State;
            await component.InvokeAsync(() => diagram.SetPan(0, 0));
            await component.Instance.DisposeAsync();
            await component.InvokeAsync(() => diagram.SetPan(-400, 0));
            Assert.That(owner.State.Occurrences, Is.EqualTo(snapshot.Occurrences));
            var disposedState = owner.State;
            await component.InvokeAsync(() => diagram.SetZoom(2));
            Assert.That(owner.State, Is.SameAs(disposedState));
        }

        [Test]
        public async Task VerifySharedSelectionRevealsAnOffscreenOccurrenceAfterMount()
        {
            using var owner = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            owner.Initialize(this.Services
                .GetRequiredService<Mycelium.Bloom.Core.ModelLoading.InterconnectionPrototype>().Model);
            var distant = owner.State.Occurrences.Single(node =>
                owner.Projection.Parts[node.ElementId].Name.StartsWith("controller"));
            Assert.That(owner.ApplyPresentation(owner.State with
            {
                Occurrences = owner.State.Occurrences.Replace(distant,
                    distant with { Geometry = distant.Geometry with { X = -8000 } })
            }), Is.True);
            using var component =
                this.Render<Interconnection>(parameters => parameters.Add(value => value.ViewModel, owner));
            var diagram = component.FindComponent<DiagramCanvasHost>().Instance.BlazorDiagram;
            var node = diagram.Nodes.Single(node => node.Id == distant.Id.ToString());
            Assert.That(node.Visible, Is.False);
            var context = this.Services.GetRequiredService<Mycelium.Bloom.Core.Context.IContextAwareService>();
            var resolver = this.Services.GetRequiredService<Mycelium.Bloom.Core.ModelLoading.IElementIdResolver>();
            var canonical = await resolver.ResolveAsync(distant.ElementId, System.Threading.CancellationToken.None);
            await component.InvokeAsync(() => context.SelectedElement = canonical);
            await component.WaitForStateAsync(() => node.Visible);
            Assert.That(owner.State.Viewport.X, Is.GreaterThan(7000));
            Assert.That(context.SelectedElement, Is.SameAs(canonical));
            Assert.That(
                this.JSInterop.Invocations.Any(call =>
                    call.Identifier == "focusOccurrence" && (string)call.Arguments[1] == node.Id), Is.True);
            await component.InvokeAsync(() => diagram.SetPan(0, 0));
            await component.WaitForStateAsync(() => !node.Visible);
            Assert.That(context.SelectedElement, Is.SameAs(canonical));
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task VerifyExplicitRevealRetainsSelectedRepeatedOccurrence(bool diagnostics)
        {
            using var owner = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            owner.Initialize(this.Services
                .GetRequiredService<Mycelium.Bloom.Core.ModelLoading.InterconnectionPrototype>().Model);
            var original = owner.State.Occurrences.Single(node =>
                owner.Projection.Parts[node.ElementId].Name.StartsWith("controller"));
            Assert.That(owner.AddExisting(original.ElementId), Is.True);
            var repeated = owner.State.Occurrences.Last();
            Assert.That(owner.ApplyPresentation(owner.State with
            {
                Occurrences = owner.State.Occurrences.Replace(repeated,
                    repeated with { Geometry = repeated.Geometry with { X = -8000 } })
            }), Is.True);
            using var component = this.Render<Interconnection>(parameters =>
                parameters.Add(value => value.ViewModel, owner).Add(value => value.ShowDiagnostics, diagnostics));
            await component.InvokeAsync(() => owner.SelectAsync([repeated.Id], repeated.Id));
            var diagram = component.FindComponent<DiagramCanvasHost>().Instance.BlazorDiagram;
            var target = diagram.Nodes.Single(node => node.Id == repeated.Id.ToString());
            await component.InvokeAsync(() => diagram.SetPan(0, 0));
            await component.WaitForStateAsync(() => !target.Visible);
            if (diagnostics)
            {
                await component.FindAll("button").Single(button => button.TextContent.Trim() == "Reveal selection")
                    .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            }
            else
            {
                await component.InvokeAsync(async () => await owner.RevealRequested.Execute(original.ElementId));
            }

            await component.WaitForStateAsync(() => target.Visible);
            Assert.That(owner.SelectedOccurrences, Does.Contain(repeated.Id));
            Assert.That(owner.State.Viewport.X, Is.GreaterThan(7000));
            Assert.That(
                this.JSInterop.Invocations.Any(call =>
                    call.Identifier == "focusOccurrence" && (string)call.Arguments[1] == target.Id), Is.True);
        }

        [Test]
        public async Task VerifyDenseViewportWarnsWithoutOmittingOccurrences()
        {
            this.Services.AddSingleton<IOptions<DiagramViewportOptions>>(Options.Create(new DiagramViewportOptions
            { DensityWarningCount = 10 }));
            using var owner = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            owner.Initialize(this.Services
                .GetRequiredService<Mycelium.Bloom.Core.ModelLoading.InterconnectionPrototype>().Model);
            var source = owner.State.Occurrences.Single(node =>
                owner.Projection.Parts[node.ElementId].Name.StartsWith("controller"));
            Assert.That(owner.ApplyPresentation(owner.State with
            {
                Occurrences = owner.State.Occurrences.AddRange(
                    Enumerable.Range(0, 30).Select(_ => source with { Id = Guid.NewGuid() }))
            }), Is.True);
            using var component = this.Render<Interconnection>(parameters =>
                parameters.Add(value => value.ViewModel, owner).Add(value => value.ShowDiagnostics, true));
            Assert.That(component.FindAll(".part-svg"), Has.Count.EqualTo(37));
            Assert.That(component.Markup, Does.Contain("Dense viewport: 37 parts"));
            var mounted = component.FindComponents<PartSvg>().Select(part => part.Instance).ToArray();
            var diagram = component.FindComponent<DiagramCanvasHost>().Instance.BlazorDiagram;
            foreach (var pan in new[] { 1d, -1d, 2d, 0d })
            {
                await component.InvokeAsync(() => diagram.SetPan(pan, 0));
                await component.WaitForStateAsync(() => owner.State.Viewport.X == pan);
                Assert.That(component.FindComponents<PartSvg>().Select(part => part.Instance),
                    Is.EquivalentTo(mounted));
            }
        }

        private static PortModel FindPort(IRenderedComponent<Interconnection> component, Guid occurrenceId,
            string portId) =>
            component.FindComponents<PartSvg>().Single(part => part.Instance.Node.Id == occurrenceId.ToString())
                .Instance.Node.Ports.Single(port => port.Id == portId);

        [Test]
        public async Task VerifyCanvasDisposalWaitsForLateObserverRegistration()
        {
            var bounds = this.JSInterop.Setup<Rectangle>("ZBlazorDiagrams.getBoundingClientRect", _ => true);
            var observe = this.JSInterop.SetupVoid("ZBlazorDiagrams.observe", _ => true);
            observe.SetVoidResult();
            var unobserve = this.JSInterop.SetupVoid("ZBlazorDiagrams.unobserve", _ => true);
            unobserve.SetVoidResult();
            var diagram = new BlazorDiagram(registerDefaultBehaviors: false);
            var host = this.Render<CascadingValue<BlazorDiagram>>(parameters => parameters
                .Add(value => value.Value, diagram)
                .AddChildContent<DiagramCanvasHost>());
            var canvas = host.FindComponent<DiagramCanvasHost>();
            var disposal = canvas.InvokeAsync(async () => await canvas.Instance.DisposeAsync());
            Assert.That(disposal.IsCompleted, Is.False);
            bounds.SetResult(new Rectangle(0, 0, 1000, 700));
            await disposal;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(observe.Invocations, Has.Count.EqualTo(1));
                Assert.That(unobserve.Invocations, Has.Count.EqualTo(1));
            }
        }

        [Test]
        public async Task VerifyDevelopmentAlternativeOpensAlongsideOriginalWithIndependentTabState()
        {
            using var editor =
                new WorkspaceEditorViewModel(Options.Create(new WorkspaceEditorOptions { MaximumGroupCount = 4 }));
            this.Services.AddScoped<Func<IWorkspaceEditorViewModel>>(_ => () => editor);
            this.Services.AddSingleton<IOptions<DiagramBenchmarkOptions>>(Options.Create(new DiagramBenchmarkOptions
            { Enabled = true }));
            using var component = this.Render<Modelling>();
            await this.OpenDiagramAsync(component);
            var original = component.FindComponent<Interconnection>().Instance.ViewModel;
            var menu = component.FindComponents<ActionMenu>()[1].Instance;
            await component.InvokeAsync(() =>
                menu.ItemSelected.InvokeAsync(menu.Items.Single(item => item.Id == "open-interconnection-x6")));
            var alternative = component.FindComponent<InterconnectionCanvas>().Instance.ViewModel;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindComponents<Interconnection>(), Has.Count.EqualTo(1));
                Assert.That(alternative, Is.Not.SameAs(original));
                Assert.That(alternative.State.DiagramId, Is.Not.EqualTo(original.State.DiagramId));
                Assert.That(alternative.Projection.Parts.Keys, Is.EquivalentTo(original.Projection.Parts.Keys));
            }

            var saved = DiagramPresentationSerializer.Export(original.State);
            await component.InvokeAsync(alternative.AddManualBend);
            Assert.That(DiagramPresentationSerializer.Export(original.State), Is.EqualTo(saved));
            var alternativeTab = editor.Groups[1].ActiveTab;
            var alternativeState = alternative.State;
            await component.InvokeAsync(() =>
                editor.MoveTab(editor.Groups[1].Id, alternativeTab.Id, editor.Groups[2].Id));
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.FindComponent<InterconnectionCanvas>().Instance.ViewModel.State,
                    Is.SameAs(alternativeState)));
        }

        [Test]
        public void VerifyNormalCompositionOmitsAlternativeEngineAndStressControls()
        {
            this.Services.AddSingleton<IOptions<DiagramBenchmarkOptions>>(
                Options.Create(new DiagramBenchmarkOptions()));
            using var component = this.Render<Modelling>();
            foreach (var menu in component.FindComponents<ActionMenu>())
            {
                Assert.That(menu.Instance.Items.Select(item => item.Id), Does.Not.Contain("open-interconnection-x6"));
                Assert.That(menu.Instance.Items.Single(item => item.Id == "open-interconnection").Label,
                    Is.EqualTo("Interconnection"));
                Assert.That(menu.Instance.Items.Select(item => item.Id),
                    Is.EqualTo(new[] { "empty-editor", "open-project-browser", "open-interconnection" }));
                Assert.That(menu.Instance.Items.All(item => !item.Id.Contains("stress", StringComparison.Ordinal)),
                    Is.True);
            }

            Assert.That(component.FindAll("[data-testid='diagram-benchmark-probe']"), Is.Empty);
        }

        [Test]
        public async Task VerifyClosingInvokingTabCancelsUnpublishedBrowserAndReleasesCandidate()
        {
            using var editor =
                new WorkspaceEditorViewModel(Options.Create(new WorkspaceEditorOptions { MaximumGroupCount = 4 }));
            this.Services.AddScoped<Func<IWorkspaceEditorViewModel>>(_ => () => editor);
            this.Services.AddSingleton<IOptions<DiagramBenchmarkOptions>>(
                Options.Create(new DiagramBenchmarkOptions()));
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
            this.Services.AddScoped<Func<Mycelium.Bloom.ViewModel.ProjectBrowser.IProjectBrowserViewModel>>(provider =>
                () => delay
                    ? pending.Object
                    : ActivatorUtilities
                        .CreateInstance<Mycelium.Bloom.ViewModel.ProjectBrowser.ProjectBrowserViewModel>(provider));
            var session = this.Services.GetRequiredService<Mycelium.Bloom.Core.Context.WorkspaceModelSession>();
            await session.EnsureLoadedAsync();
            using var component = this.Render<Modelling>();
            var menu = component.FindComponents<ActionMenu>().Last().Instance;
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
            Assert.That(editor.Groups.SelectMany(group => group.Tabs).Any(item => item.Title == "Late model"),
                Is.False);
            Assert.That(session.OpenModels.Any(model => model.Root.DeclaredName == "Late model"), Is.False);
            pending.Verify(value => value.Dispose(), Times.Once);
        }

        [Test]
        public async Task VerifyDiagramLocalOpenPreservesQuantitiesAndReusesTabs()
        {
            using var editor =
                new WorkspaceEditorViewModel(Options.Create(new WorkspaceEditorOptions { MaximumGroupCount = 4 }));
            this.Services.AddScoped<Func<IWorkspaceEditorViewModel>>(_ => () => editor);
            this.Services.AddSingleton<IOptions<DiagramBenchmarkOptions>>(
                Options.Create(new DiagramBenchmarkOptions()));
            var session = this.Services.GetRequiredService<Mycelium.Bloom.Core.Context.WorkspaceModelSession>();
            await session.EnsureLoadedAsync();
            var quantities = session.Current;
            using var component = this.Render<Modelling>();
            var originalBrowser = component
                .FindComponent<Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser.ProjectBrowser>().Instance
                .ViewModel;
            await component.WaitForAssertionAsync(() => Assert.That(originalBrowser.RootNodes, Is.Not.Empty));
            var menu = component.FindComponents<ActionMenu>().Last().Instance;
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
            Assert.That(originalBrowser.RootNodes.Single().SourceElement, Is.SameAs(quantities.Root));
            Assert.That(session.Current, Is.SameAs(quantities));
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
            Assert.That(pump.IsScopeAvailable, Is.True);
            Assert.That(pump.State, Is.SameAs(saved));
            await component.InvokeAsync(() => editor.ActivateTab(editor.Groups[2].Id, invoking.Id));
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.FindComponent<Interconnection>().Instance.ViewModel, Is.SameAs(pump)));
            var quantityTab = editor.Groups[0].Tabs.First();
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
            this.Services.AddSingleton<IOptions<DiagramBenchmarkOptions>>(
                Options.Create(new DiagramBenchmarkOptions()));
            var session = this.Services.GetRequiredService<Mycelium.Bloom.Core.Context.WorkspaceModelSession>();
            if (noModel)
            {
                session.Clear();
            }
            else
            {
                await session.EnsureLoadedAsync();
            }

            var count = this.Services.GetRequiredService<SysML2.NET.Dal.IAssembler>().Cache.Count;
            using var component = this.Render<Modelling>();
            var menu = component.FindComponents<ActionMenu>().Last().Instance;
            await component.InvokeAsync(() =>
                menu.ItemSelected.InvokeAsync(menu.Items.Single(item => item.Id == "open-interconnection")));
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.Find("[data-testid='interconnection-scope-status']").TextContent,
                    Does.Contain(noModel ? "No model loaded" : "No structural parts to display")));
            Assert.That(component.FindComponents<Interconnection>(), Is.Empty);
            Assert.That(this.Services.GetRequiredService<SysML2.NET.Dal.IAssembler>().Cache.Count, Is.EqualTo(count));
        }

        [TestCase("open-interconnection-x6")]
        [TestCase("open-interconnection-demo")]
        [TestCase("open-demonstration-browser")]
        public async Task VerifyNormalCompositionRejectsAlternativeEngineAction(string action)
        {
            this.Services.AddSingleton<IOptions<DiagramBenchmarkOptions>>(
                Options.Create(new DiagramBenchmarkOptions()));
            using var component = this.Render<Modelling>();
            var menu = component.FindComponents<ActionMenu>()
                .Last(value => value.Instance.Items.Any(item => item.Id == "open-interconnection")).Instance;
            await component.InvokeAsync(() => menu.ItemSelected.InvokeAsync(new Mycelium.Bloom.Model.ActionMenuItem
            {
                Id = action,
                Label = "Alternative renderer"
            }));
            Assert.That(component.FindComponents<InterconnectionCanvas>(), Is.Empty);
            Assert.That(component.FindComponents<Interconnection>(), Is.Empty);
            Assert.That(
                component.FindAll("[role='tab']").Any(tab =>
                    tab.TextContent.Contains("Interconnection model", StringComparison.Ordinal)), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void VerifyDiagnosticControlsAreOptInWithoutChangingPresentation(bool useCanvas)
        {
            using var owner = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            owner.Initialize(this.Services
                .GetRequiredService<Mycelium.Bloom.Core.ModelLoading.InterconnectionPrototype>().Model);
            var saved = DiagramPresentationSerializer.Export(owner.State);
            using var component = useCanvas
                ? (IRenderedComponent<IComponent>)this.Render<InterconnectionCanvas>(parameters =>
                    parameters.Add(value => value.ViewModel, owner))
                : this.Render<Interconnection>(parameters => parameters.Add(value => value.ViewModel, owner));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    component.FindAll("input[type=file], .interconnection__diagnostics, .interconnection__status"),
                    Is.Empty);
                Assert.That(component.FindAll("button").Select(button => button.TextContent),
                    Does.Not.Contain("Export SVG"));
                Assert.That(component.FindAll("button").Select(button => button.TextContent),
                    Does.Not.Contain("Export presentation"));
                Assert.That(component.FindAll("[aria-label='Prototype presentation']"), Is.Empty);
                Assert.That(component.FindAll("[aria-label='Current zoom']"), Has.Count.EqualTo(1));
                Assert.That(DiagramPresentationSerializer.Export(owner.State), Is.EqualTo(saved));
            }
        }

        [Test]
        public async Task VerifyBenchmarkScenesAreIndependentAndPreserveNormalPresentation()
        {
            this.Services.AddSingleton<IOptions<DiagramBenchmarkOptions>>(Options.Create(new DiagramBenchmarkOptions
            { Enabled = true }));
            using var editor =
                new WorkspaceEditorViewModel(Options.Create(new WorkspaceEditorOptions { MaximumGroupCount = 4 }));
            this.Services.AddScoped<Func<IWorkspaceEditorViewModel>>(_ => () => editor);
            using var component = this.Render<Modelling>();
            await this.OpenDiagramAsync(component);
            var normal = component.FindComponent<Interconnection>().Instance.ViewModel;
            var saved = DiagramPresentationSerializer.Export(normal.State);
            var menus = component.FindComponents<ActionMenu>();
            await component.InvokeAsync(() =>
                menus[1].Instance.ItemSelected
                    .InvokeAsync(menus[1].Instance.Items.Single(item => item.Id == "open-interconnection-stress")));
            await component.InvokeAsync(() =>
                menus[0].Instance.ItemSelected.InvokeAsync(menus[0].Instance.Items
                    .Single(item => item.Id == "open-interconnection-x6-stress")));
            var stress = component.FindComponents<Interconnection>().Single(item => item.Instance.IsBenchmark).Instance
                .ViewModel;
            var alternative = component.FindComponent<InterconnectionCanvas>().Instance.ViewModel;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(stress.State.Occurrences, Has.Length.EqualTo(126));
                Assert.That(alternative.State.Occurrences, Has.Length.EqualTo(126));
                Assert.That(stress.State.Routes, Has.Length.EqualTo(90));
                Assert.That(alternative.State.Routes, Has.Length.EqualTo(90));
                Assert.That(stress.State.Occurrences.Select(item => (item.ElementId, item.Geometry)),
                    Is.EqualTo(alternative.State.Occurrences.Select(item => (item.ElementId, item.Geometry))));
                Assert.That(stress.State.Viewport, Is.EqualTo(alternative.State.Viewport));
                Assert.That(stress.State.DiagramId, Is.Not.EqualTo(alternative.State.DiagramId));
                Assert.That(DiagramPresentationSerializer.Export(normal.State), Is.EqualTo(saved));
                Assert.That(component.Markup, Does.Not.Contain("Display 120+ occurrences"));
            }
        }

        [Test]
        public async Task VerifyAlternativeDisposalJoinsLateMountAndRejectsCallbacksWithoutClosingTab()
        {
            using var viewModel = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            viewModel.Initialize(this.Services
                .GetRequiredService<Mycelium.Bloom.Core.ModelLoading.InterconnectionPrototype>().Model);
            var module =
                this.JSInterop.SetupModule(
                    "./Components/UI/Organisms/InterconnectionCanvas/InterconnectionCanvas.razor.js");
            var mounting = module.SetupVoid("mount", _ => true);
            var unmounting = module.SetupVoid("unmount", _ => true);
            unmounting.SetVoidResult();
            var component =
                this.Render<InterconnectionCanvas>(parameters => parameters.Add(value => value.ViewModel, viewModel));
            await component.WaitForAssertionAsync(() => Assert.That(mounting.Invocations, Has.Count.EqualTo(1)));
            var disposal = component.InvokeAsync(async () => await component.Instance.DisposeAsync());
            Assert.That(disposal.IsCompleted, Is.False);
            mounting.SetVoidResult();
            await disposal;
            Assert.That(unmounting.Invocations, Has.Count.EqualTo(1));
            Assert.That(await component.Instance.GetSnapshot(), Is.Null);
            Assert.That(await component.Instance.PresentationChanged(null), Is.Null);
            Assert.That(viewModel.AddExisting(viewModel.State.Occurrences[0].ElementId), Is.True);
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public async Task VerifyBenchmarkTraceRequiresIsolationAndEndsOnDisposal(bool enabled, bool large)
        {
            this.Services.AddSingleton<IOptions<DiagramBenchmarkOptions>>(Options.Create(new DiagramBenchmarkOptions
            { Enabled = enabled, LargeSceneEnabled = large }));
            using var viewModel = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            viewModel.Initialize(this.Services
                .GetRequiredService<Mycelium.Bloom.Core.ModelLoading.InterconnectionPrototype>().Model);
            var original = viewModel.State;
            var module =
                this.JSInterop.SetupModule(
                    "./Components/UI/Organisms/DiagramBenchmarkProbe/DiagramBenchmarkProbe.razor.js");
            module.SetupVoid("mount", _ => true).SetVoidResult();
            module.SetupVoid("unmount", _ => true).SetVoidResult();
            using var component =
                this.Render<DiagramBenchmarkProbe>(parameters => parameters.Add(value => value.ViewModel, viewModel));
            await component.Instance.StartBenchmarkTrace();
            using (var activity = Mycelium.Bloom.Core.DiagramDiagnostics.Source.StartActivity("test-operation"))
            {
                Assert.That(activity is not null, Is.EqualTo(enabled && large));
            }

            var observation = await component.Instance.StopBenchmarkTrace();
            Assert.That(observation is not null, Is.EqualTo(enabled && large));
            Assert.That(Mycelium.Bloom.Core.DiagramDiagnostics.Source.HasListeners(), Is.False);
            Assert.That(viewModel.State, Is.SameAs(original));
            await component.Instance.StartBenchmarkTrace();
            await component.InvokeAsync(async () => await component.Instance.DisposeAsync());
            Assert.That(Mycelium.Bloom.Core.DiagramDiagnostics.Source.HasListeners(), Is.False);
            Assert.That(await component.Instance.StopBenchmarkTrace(), Is.Null);
        }

        [Test]
        public async Task VerifyBenchmarkWatchReadsAcceptedStateAndCancelsWhenUnmounted()
        {
            this.Services.AddSingleton<IOptions<DiagramBenchmarkOptions>>(Options.Create(new DiagramBenchmarkOptions
            { Enabled = true }));
            using var viewModel = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            viewModel.Initialize(this.Services
                .GetRequiredService<Mycelium.Bloom.Core.ModelLoading.InterconnectionPrototype>().Model);
            var module =
                this.JSInterop.SetupModule(
                    "./Components/UI/Organisms/DiagramBenchmarkProbe/DiagramBenchmarkProbe.razor.js");
            module.SetupVoid("mount", _ => true).SetVoidResult();
            module.SetupVoid("unmount", _ => true).SetVoidResult();
            using var component =
                this.Render<DiagramBenchmarkProbe>(parameters => parameters.Add(value => value.ViewModel, viewModel));
            var occurrence = viewModel.State.Occurrences[0];
            Task<DiagramBenchmarkSnapshot> watch = null;
            await component.InvokeAsync(() =>
            {
                watch = component.Instance.WatchBenchmarkGeometry(occurrence.Id, occurrence.Geometry.X + 10,
                    occurrence.Geometry.Y);
            });
            Assert.That(watch.IsCompleted, Is.False);
            await component.InvokeAsync(() => viewModel.ApplyPresentation(viewModel.State with
            {
                Occurrences = viewModel.State.Occurrences.Replace(occurrence,
                    occurrence with { Geometry = occurrence.Geometry with { X = occurrence.Geometry.X + 10 } })
            }));
            var accepted = await watch;
            Assert.That(accepted.Geometry.X, Is.EqualTo(occurrence.Geometry.X + 10));
            var pending =
                component.Instance.WatchBenchmarkGeometry(occurrence.Id, occurrence.Geometry.X + 20,
                    occurrence.Geometry.Y);
            await component.InvokeAsync(async () => await component.Instance.DisposeAsync());
            Assert.That(await pending, Is.Null);
            Assert.That(await component.Instance.ReadBenchmarkSnapshot(occurrence.Id), Is.Null);
            Assert.That(component.Instance.ReadBenchmarkResources(), Is.Null);
            Assert.That(viewModel.AddExisting(occurrence.ElementId), Is.True);
        }

        [Test]
        public async Task VerifyBenchmarkProbeDoesNotInitializeWhenDisabled()
        {
            this.Services.AddSingleton<IOptions<DiagramBenchmarkOptions>>(
                Options.Create(new DiagramBenchmarkOptions()));
            using var viewModel = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            viewModel.Initialize(this.Services
                .GetRequiredService<Mycelium.Bloom.Core.ModelLoading.InterconnectionPrototype>().Model);
            using var component =
                this.Render<DiagramBenchmarkProbe>(parameters => parameters.Add(value => value.ViewModel, viewModel));
            Assert.That(component.Instance.BenchmarkPing(), Is.Zero);
            Assert.That(await component.Instance.WatchBenchmarkGeometry(Guid.NewGuid(), 0, 0), Is.Null);
            Assert.That(await component.Instance.ReadBenchmarkSnapshot(Guid.NewGuid()), Is.Null);
        }

        [Test]
        public async Task VerifyOnDemandRenderingRetainsOnlyRegionModelsAndFullPresentation()
        {
            using var owner = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            owner.ConfigureBenchmark(new DiagramBenchmarkOptions
            { Enabled = true, LargeSceneEnabled = true, AccessKey = new string('x', 32) });
            var prototype =
                this.Services.GetRequiredService<Mycelium.Bloom.Core.ModelLoading.InterconnectionPrototype>();
            owner.Initialize(prototype.Model);
            var source = prototype.CreateOnDemandSource(owner.Projection, owner.State, 100);
            using var session =
                ActivatorUtilities.CreateInstance<Mycelium.Bloom.Core.ModelLoading.DiagramModelSession>(this.Services,
                    source, new DiagramDataOptions());
            owner.InitializeOnDemand(session);
            using var component = this.Render<Interconnection>(parameters =>
                parameters.Add(value => value.ViewModel, owner).Add(value => value.ShowDiagnostics, true));
            await component.WaitForAssertionAsync(() =>
                Assert.That(
                    component.FindAll(".part-svg__name")
                        .Any(node => node.TextContent.Contains("AOCS", StringComparison.Ordinal)), Is.True));
            var first = component.FindComponent<DiagramCanvasHost>().Instance.BlazorDiagram;
            Assert.That(first.Nodes.Count + first.Groups.Count, Is.LessThan(40));
            Assert.That(owner.State.Occurrences, Has.Length.EqualTo(700));
            Assert.That(source.Reads, Is.LessThan(10));
            var saved = DiagramPresentationSerializer.Export(owner.State);
            await component.InvokeAsync(() =>
                owner.ApplyPresentation(owner.State with { Viewport = new DiagramViewport(-20000, 0, 1) }));
            await component.WaitForAssertionAsync(() => Assert.That(source.Reads, Is.GreaterThan(4)));
            var distant = component.FindComponent<DiagramCanvasHost>().Instance.BlazorDiagram;
            Assert.That(distant.Nodes.Count + distant.Groups.Count, Is.LessThan(40));
            Assert.That(owner.State.Occurrences, Has.Length.EqualTo(700));
            Assert.That(owner.State.Routes, Has.Length.EqualTo(500));
            await component.InvokeAsync(() => Assert.That(owner.Import(saved, out _), Is.True));
            await component.WaitForAssertionAsync(() => Assert.That(owner.State.Viewport.X, Is.Zero));
            for (var gesture = 0; gesture < 3; gesture++)
            {
                var canvas = component.Find(".diagram-canvas");
                await canvas.PointerDownAsync(new Microsoft.AspNetCore.Components.Web.PointerEventArgs
                { ClientX = 500, ClientY = 10, Button = 0 });
                for (var step = 1; step <= 30; step++)
                {
                    await component.Find(".diagram-canvas").PointerMoveAsync(
                        new Microsoft.AspNetCore.Components.Web.PointerEventArgs
                        { ClientX = 500 - step * 250d / 30, ClientY = 10, Buttons = 1 });
                }

                await component.Find(".diagram-canvas").PointerUpAsync(
                    new Microsoft.AspNetCore.Components.Web.PointerEventArgs
                    { ClientX = 250, ClientY = 10, Button = 0 });
            }

            await component.InvokeAsync(() => Assert.That(owner.Import(saved, out _), Is.True));
            await component.WaitForAssertionAsync(() =>
            {
                Assert.That(owner.State.Viewport.X, Is.Zero);
                Assert.That(component.FindComponent<DiagramCanvasHost>().Instance.BlazorDiagram.Pan.X, Is.Zero);
                Assert.That(component.Find(".diagram-svg-layer").GetAttribute("style"),
                    Does.StartWith("transform: translate(0px, 0px)"));
            });
        }

        [Test]
        public async Task VerifyOnDemandPointerDownDefersSelectionTriggeredTopologyUntilRelease()
        {
            using var owner = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            var prototype =
                this.Services.GetRequiredService<Mycelium.Bloom.Core.ModelLoading.InterconnectionPrototype>();
            owner.Initialize(prototype.Model);
            var source = prototype.CreateOnDemandSource(owner.Projection, owner.State, 3);
            using var session =
                ActivatorUtilities.CreateInstance<Mycelium.Bloom.Core.ModelLoading.DiagramModelSession>(this.Services,
                    source, new DiagramDataOptions());
            owner.InitializeOnDemand(session);
            using var component =
                this.Render<Interconnection>(parameters => parameters.Add(value => value.ViewModel, owner));
            await component.WaitForAssertionAsync(() =>
                Assert.That(
                    component.FindAll(".part-svg__name")
                        .Any(node => node.TextContent.Contains("AOCS", StringComparison.Ordinal)), Is.True));
            var mounted = component.FindComponent<DiagramCanvasHost>().Instance.BlazorDiagram;
            await component.InvokeAsync(() => mounted.SelectModel(mounted.Nodes.First(), true));
            mounted.SelectionChanged += _ => owner.AddExisting(owner.State.Occurrences[0].ElementId);
            await component.Find(".diagram-canvas").PointerDownAsync(
                new Microsoft.AspNetCore.Components.Web.PointerEventArgs { ClientX = 10, ClientY = 10, Button = 0 });
            Assert.That(component.FindComponent<DiagramCanvasHost>().Instance.BlazorDiagram, Is.SameAs(mounted));
            Assert.That(owner.State.Occurrences, Has.Length.EqualTo(22));
            await component.Find(".diagram-canvas").PointerUpAsync(
                new Microsoft.AspNetCore.Components.Web.PointerEventArgs { ClientX = 10, ClientY = 10, Button = 0 });
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.FindComponent<DiagramCanvasHost>().Instance.BlazorDiagram,
                    Is.Not.SameAs(mounted)));
        }

        [Test]
        public async Task VerifyOnDemandExportFailureRecoversWithCompleteDetachedOutput()
        {
            using var owner = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            var prototype =
                this.Services.GetRequiredService<Mycelium.Bloom.Core.ModelLoading.InterconnectionPrototype>();
            owner.Initialize(prototype.Model);
            var fixture = prototype.CreateOnDemandSource(owner.Projection, owner.State, 3);
            var source = new Mock<Mycelium.Bloom.Core.ModelLoading.IDiagramModelSource>(MockBehavior.Strict);
            source.SetupGet(value => value.Scope).Returns(fixture.Scope);
            source.SetupGet(value => value.Metadata).Returns(fixture.Metadata);
            source.SetupGet(value => value.Presentation).Returns(fixture.Presentation);
            source.SetupGet(value => value.Bundles).Returns(fixture.Bundles);
            source.Setup(value => value.AuthorizeAsync(fixture.Scope, It.IsAny<System.Threading.CancellationToken>()))
                .Returns(ValueTask.CompletedTask);
            var unavailable = true;
            source.Setup(value => value.FetchAsync(fixture.Scope, It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<System.Threading.CancellationToken>()))
                .Returns((Mycelium.Bloom.Core.ModelLoading.DiagramReadScope scope, IReadOnlyList<string> ids,
                        System.Threading.CancellationToken token) =>
                    unavailable
                        ? Task.FromException<IReadOnlyDictionary<string, string>>(
                            new System.IO.IOException("Unavailable"))
                        : fixture.FetchAsync(scope, ids, token));
            using var session =
                ActivatorUtilities.CreateInstance<Mycelium.Bloom.Core.ModelLoading.DiagramModelSession>(this.Services,
                    source.Object, new DiagramDataOptions());
            owner.InitializeOnDemand(session);
            using var component = this.Render<Interconnection>(parameters =>
                parameters.Add(value => value.ViewModel, owner).Add(value => value.ShowDiagnostics, true));
            var saved = DiagramPresentationSerializer.Export(owner.State);
            await component.FindAll("button").Single(button => button.TextContent == "Export SVG").ClickAsync();
            Assert.That(this.JSInterop.Invocations.Any(call => call.Identifier == "exportCompleteSvg"), Is.False);
            unavailable = false;
            await component.FindAll("button").Single(button => button.TextContent == "Export SVG").ClickAsync();
            var invocation = this.JSInterop.Invocations.Single(call => call.Identifier == "exportCompleteSvg");
            var svg = new AngleSharp.Html.Parser.HtmlParser().ParseDocument((string)invocation.Arguments[1]);
            Assert.That(svg.QuerySelectorAll(".part-svg"), Has.Length.EqualTo(21));
            Assert.That(svg.QuerySelectorAll(".part-svg__port"), Has.Length.EqualTo(30));
            Assert.That(svg.QuerySelectorAll(".connection-svg__line"), Has.Length.EqualTo(15));
            Assert.That(session.Cache.Statistics.CurrentEntryCount, Is.Zero);
            Assert.That(DiagramPresentationSerializer.Export(owner.State), Is.EqualTo(saved));
        }

        [Test]
        public async Task VerifyBrowserReselectionDispatchesOnlyToActiveDiagramWithoutActivation()
        {
            using var editor =
                new WorkspaceEditorViewModel(Options.Create(new WorkspaceEditorOptions { MaximumGroupCount = 4 }));
            this.Services.AddScoped<Func<IWorkspaceEditorViewModel>>(_ => () => editor);
            using var component = this.Render<Modelling>();
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
            Assert.That(requests, Has.Count.EqualTo(2));
            Assert.That(group.ActiveTab, Is.SameAs(placeholder));
        }

        [Test]
        public async Task VerifyNormalLargeScopeCreatesOnlyRegionalModelsAndRetainsThemAfterMovement()
        {
            using var file = new Mycelium.Bloom.Tests.ViewModel.Diagram.LargeWorkspaceFile(600);
            var session = this.Services.GetRequiredService<Mycelium.Bloom.Core.Context.WorkspaceModelSession>();
            await session.OpenAsync(file.Uri);
            using var owner = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            owner.BindWorkspace(session);
            await owner.Preparation;
            this.JSInterop.Setup<Rectangle>("ZBlazorDiagrams.getBoundingClientRect", _ => true)
                .SetResult(new Rectangle(0, 0, 900, 700));
            using var component =
                this.Render<Interconnection>(parameters => parameters.Add(value => value.ViewModel, owner));
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.FindAll(".part-svg"), Has.Count.InRange(1, 40)));
            var graph = component.FindComponent<DiagramCanvasHost>().Instance.BlazorDiagram;
            Assert.That(graph.Nodes.Count + graph.Groups.Count, Is.LessThan(40));
            Assert.That(owner.State.Occurrences, Has.Length.EqualTo(600));
            Assert.That(component.FindComponents<GroupSvg>().Count, Is.GreaterThan(1));
            var root = owner.State.Occurrences.First(node =>
                owner.Projection.Parts[node.ElementId].Name == "part00000");
            var mounted = graph.Groups.Single(group => component.FindComponents<PartSvg>().Any(part =>
                part.Instance.Node == group && part.Instance.Node.Position.X == root.Geometry.X));
            await component.InvokeAsync(() => mounted.SetPosition(root.Geometry.X + 10, root.Geometry.Y));
            await component.Find(".diagram-canvas")
                .PointerUpAsync(new Microsoft.AspNetCore.Components.Web.PointerEventArgs { Button = 0 });
            await component.WaitForAssertionAsync(() =>
                Assert.That(owner.State.Occurrences.Single(node => node.Id == root.Id).Geometry.X,
                    Is.EqualTo(root.Geometry.X + 10)));
            Assert.That(component.FindComponent<DiagramCanvasHost>().Instance.BlazorDiagram, Is.SameAs(graph));
            Assert.That(DiagramPresentationSerializer.Validate(owner.State, owner.Projection, out _, owner.Limits),
                Is.True);
            var distant = owner.State.Occurrences[^1];
            await component.InvokeAsync(() => owner.RevealRequested.Execute(distant.ElementId).Subscribe());
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.FindAll($".part-svg[data-element-id='{distant.ElementId}']"),
                    Has.Count.EqualTo(1)));
            Assert.That(component.FindComponent<DiagramCanvasHost>().Instance.BlazorDiagram.Nodes.Count,
                Is.LessThan(40));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task VerifyRevealWaitsForRegionalCanvasBounds(bool beforeMount)
        {
            using var file = new Mycelium.Bloom.Tests.ViewModel.Diagram.LargeWorkspaceFile(600);
            var session = this.Services.GetRequiredService<Mycelium.Bloom.Core.Context.WorkspaceModelSession>();
            await session.OpenAsync(file.Uri);
            using var owner = this.Services.GetRequiredService<Func<DiagramViewModel>>()();
            owner.BindWorkspace(session);
            await owner.Preparation;
            var bounds = this.JSInterop.Setup<Rectangle>("ZBlazorDiagrams.getBoundingClientRect", _ => true);
            var target = owner.State.Occurrences[^1];
            if (beforeMount)
            {
                owner.RevealRequested.Execute(target.ElementId).Subscribe();
            }

            using var component =
                this.Render<Interconnection>(parameters => parameters.Add(value => value.ViewModel, owner));
            if (!beforeMount)
            {
                await component.InvokeAsync(() => owner.RevealRequested.Execute(target.ElementId).Subscribe());
            }

            Assert.That(owner.State.Viewport, Is.EqualTo(new DiagramViewport(0, 0, 1)));
            bounds.SetResult(new Rectangle(0, 0, 900, 700));
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.FindAll($".part-svg[data-element-id='{target.ElementId}']"),
                    Has.Count.EqualTo(1)));
            Assert.That(owner.State.Viewport.X, Is.LessThan(-1000));
            Assert.That(owner.PendingRevealElementId, Is.Null);
        }

        private async Task OpenDiagramAsync(IRenderedComponent<Modelling> component)
        {
            var menu = component.FindComponents<ActionMenu>()
                .Last(value => value.Instance.Items.Any(item => item.Id == "open-interconnection-demo")).Instance;
            await component.InvokeAsync(() =>
                menu.ItemSelected.InvokeAsync(menu.Items.Single(item => item.Id == "open-interconnection-demo")));
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.FindComponents<Interconnection>(), Has.Count.EqualTo(1)));
        }
    }
}