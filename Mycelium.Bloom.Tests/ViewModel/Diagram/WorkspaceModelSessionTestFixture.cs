// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceModelSessionTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.Diagram
{
    using System;
    using System.IO;
    using System.Diagnostics;
    using System.Text.Json;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Moq;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Extensions;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.SDK;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Diagram;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;
    using SysML2.NET.Dal;

    [TestFixture]
    public sealed class WorkspaceModelSessionTestFixture
    {
        [Test]
        public async Task VerifyFileModelsShareCanonicalObjectsAndRetainIndependentScopes()
        {
            using var services = CreateServices();
            var session = services.GetRequiredService<WorkspaceModelSession>();
            using var firstFile = new WorkspaceModelFile("First model", "pump");
            using var secondFile = new WorkspaceModelFile("Second model", "valve");
            var firstRoot = await session.OpenAsync(firstFile.Uri);
            using var first = services.GetRequiredService<Func<DiagramViewModel>>()();
            using var independent = services.GetRequiredService<Func<DiagramViewModel>>()();
            first.BindWorkspace(session);
            independent.UpdateWorkspace(session.Current);
            using var browser = services.GetRequiredService<Func<IProjectBrowserViewModel>>()();
            Assert.That(await browser.InitializeAsync(CancellationToken.None), Is.True);
            var node = first.State.Occurrences.Single();
            await first.SelectAsync([node.Id], node.Id);
            var context = services.GetRequiredService<IContextAwareService>();
            var canonical = firstRoot.ownedElement.Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(browser.RootNodes.Single().SourceElement, Is.SameAs(firstRoot));
                Assert.That(context.SelectedElement, Is.SameAs(canonical));
                Assert.That(first.State.ModelScope, Is.EqualTo(firstRoot.ElementId));
                Assert.That(first.Projection.Parts.Single().Value.Name, Is.EqualTo("pump"));
                Assert.That(first.State.DiagramId, Is.Not.EqualTo(independent.State.DiagramId));
                Assert.That(services.GetRequiredService<IAssembler>().Cache, Has.Count.EqualTo(3));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.AddExisting(canonical.ElementId), Is.True);
                Assert.That(independent.State.Occurrences, Has.Length.EqualTo(1));
            }
            var saved = first.State;
            var secondRoot = await session.OpenAsync(secondFile.Uri);
            using var second = services.GetRequiredService<Func<DiagramViewModel>>()();
            second.UpdateWorkspace(session.Current);
            await first.SelectAsync([node.Id], node.Id);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.IsScopeAvailable, Is.False);
                Assert.That(first.State, Is.SameAs(saved));
                Assert.That(first.ApplyPresentation(saved), Is.False);
                Assert.That(context.SelectedElement, Is.Null);
                Assert.That(second.State.ModelScope, Is.EqualTo(secondRoot.ElementId));
                Assert.That(second.Projection.Parts.Single().Value.Name, Is.EqualTo("valve"));
                Assert.That(second.AddExisting(canonical.ElementId), Is.False);
            }
        }

        [Test]
        public async Task VerifyBoundOccurrencesAndConnectionsResolveOnlyCanonicalScopeObjects()
        {
            using var services = CreateServices();
            var session = services.GetRequiredService<WorkspaceModelSession>();
            var root = await session.OpenAsync(new Uri(Path.Combine(TestRepository.GetDirectoryPath("Mycelium.Bloom"),
                "Resources", "Prototypes", "Interconnection.json")));
            using var diagram = services.GetRequiredService<Func<DiagramViewModel>>()();
            diagram.BindWorkspace(session);
            var canonical = services.GetRequiredService<IAssembler>().Cache.Values.Select(value => value.Value).ToDictionary(element => element.ElementId);
            var context = services.GetRequiredService<IContextAwareService>();
            foreach (var item in diagram.State.Occurrences.Select(node => (node.Id, node.ElementId))
                         .Concat(diagram.State.Routes.Select(route => (route.Id, route.ElementId))))
            {
                await diagram.SelectAsync([item.Id], item.Id);
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(context.SelectedElement, Is.SameAs(canonical[item.ElementId]));
                    Assert.That(diagram.State.ModelScope, Is.EqualTo(root.ElementId));
                }
            }

            context.LifecycleState = ProjectLifecycleKind.Archived;
            Assert.That(diagram.AddExisting(diagram.State.Occurrences[0].ElementId), Is.False);
            session.Clear(true);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(diagram.IsScopeAvailable, Is.False);
                Assert.That(context.SelectedElement, Is.Null);
            }
        }

        [Test]
        public async Task VerifyDefaultFileDoesNotLoadDemonstrationAndClearDoesNotReload()
        {
            using var services = CreateServices();
            var session = services.GetRequiredService<WorkspaceModelSession>();
            var root = await session.EnsureLoadedAsync();
            var count = services.GetRequiredService<IAssembler>().Cache.Count;
            using var diagram = services.GetRequiredService<Func<DiagramViewModel>>()();
            diagram.UpdateWorkspace(session.Current);
            await diagram.Preparation;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(diagram.Projection.Parts, Is.Empty);
                Assert.That(diagram.State.ModelScope, Is.EqualTo(root.ElementId));
                Assert.That(await session.EnsureLoadedAsync(), Is.SameAs(root));
                Assert.That(services.GetRequiredService<IAssembler>().Cache, Has.Count.EqualTo(count));
            }

            session.Clear();
            Assert.That(await session.EnsureLoadedAsync(), Is.Null);
            diagram.UpdateWorkspace(session.Current);
            Assert.That(diagram.IsScopeAvailable, Is.False);
            using var empty = services.GetRequiredService<Func<DiagramViewModel>>()();
            empty.UpdateWorkspace(session.Current);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(empty.State, Is.Null);
                Assert.That(empty.WorkspaceStatus, Is.EqualTo(WorkspaceModelStatus.Empty));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task VerifySupersededOrDisposedLoadCannotRestoreOldContext(bool dispose)
        {
            using var services = CreateServices();
            using var file = new WorkspaceModelFile("Old model", "part");
            var root = services.GetRequiredService<IModelLoaderService>().LoadModel(file.Uri);
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var release = new ManualResetEventSlim();
            var loader = new Mock<IModelLoaderService>(MockBehavior.Strict);
            loader.Setup(value => value.LoadQuantitiesModel()).Returns(() =>
            {
                started.SetResult();
                if (!release.Wait(TimeSpan.FromSeconds(10)))
                {
                    throw new TimeoutException();
                }

                return root;
            });
            var session =
                new WorkspaceModelSession(loader.Object, services.GetRequiredService<IContextAwareService>());
            var disposed = false;
            try
            {
                var loading = session.EnsureLoadedAsync();
                try
                {
                    await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
                    if (dispose)
                    {
                        disposed = true;
                        session.Dispose();
                    }
                    else
                    {
                        session.Clear(true);
                    }
                }
                finally
                {
                    release.Set();
                }

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(await loading, Is.Null);
                    Assert.That(session.Current.Root, Is.Null);
                    Assert.That(session.Current.Status,
                        Is.EqualTo(dispose ? WorkspaceModelStatus.Empty : WorkspaceModelStatus.Unavailable));
                }
            }
            finally
            {
                if (!disposed)
                {
                    session.Dispose();
                }
            }
        }

        [Test]
        public async Task VerifyDisposeInvalidatesBoundTabsBeforePublishingClosure()
        {
            using var services = CreateServices();
            using var file = new WorkspaceModelFile("Review model", "part");
            var session = services.GetRequiredService<WorkspaceModelSession>();
            await session.OpenAsync(file.Uri);
            using var diagram = services.GetRequiredService<Func<DiagramViewModel>>()();
            diagram.BindWorkspace(session);
            var state = diagram.State;
            var disposedAtPublication = false;
            session.PropertyChanged += (_, change) =>
            {
                if (change.PropertyName == nameof(session.Current))
                {
                    disposedAtPublication = session.IsDisposed;
                }
            };
            session.Dispose();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(disposedAtPublication, Is.True);
                Assert.That(diagram.IsScopeAvailable, Is.False);
                Assert.That(diagram.ApplyPresentation(state), Is.False);
                Assert.That(session.Current.Root, Is.Null);
            }
        }

        [Test]
        public async Task VerifyFileFailureDoesNotSubstituteDemonstration()
        {
            using var services = CreateServices();
            var session = services.GetRequiredService<WorkspaceModelSession>();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(await session.OpenAsync(new Uri(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json"))),
                            Is.Null);
                Assert.That(session.Current.Status, Is.EqualTo(WorkspaceModelStatus.Failed));
                Assert.That(services.GetRequiredService<IAssembler>().Cache, Is.Empty);
            }
        }

        private static ServiceProvider CreateServices()
        {
            var environment = new Mock<IHostEnvironment>();
            environment.SetupGet(value => value.ContentRootPath)
                .Returns(TestRepository.GetDirectoryPath("Mycelium.Bloom"));
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(environment.Object);
            services.AddApplicationServices();
            return services.BuildServiceProvider();
        }

        [Test]
        public async Task VerifyUploadedFilesSwitchCanonicalScopeWithoutChangingPreviousModels()
        {
            using var services = CreateServices();
            var session = services.GetRequiredService<WorkspaceModelSession>();
            var quantities = await session.EnsureLoadedAsync();
            using var opening = services.GetRequiredService<Func<Mycelium.Bloom.ViewModel.ModelOpeningViewModel>>()();
            using var pump = new WorkspaceModelFile("Pump model", "pump");
            using var valve = new WorkspaceModelFile("Valve model", "valve");
            var pumpBytes = await File.ReadAllBytesAsync(pump.Uri.LocalPath);
            var valveBytes = await File.ReadAllBytesAsync(valve.Uri.LocalPath);
            Assert.That(await opening.OpenAsync("pump.json", pumpBytes.Length, _ => new MemoryStream(pumpBytes)),
                Is.True);
            var pumpRoot = session.Current.Root;
            using var diagram = services.GetRequiredService<Func<DiagramViewModel>>()();
            diagram.BindWorkspace(session);
            await diagram.Preparation;
            var state = diagram.State;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(await opening.OpenAsync("valve.json", valveBytes.Length, _ => new MemoryStream(valveBytes)),
                            Is.True);
                Assert.That(session.Current.Root.ownedElement.Single().DeclaredName, Is.EqualTo("valve"));
                Assert.That(diagram.IsScopeAvailable, Is.False);
                Assert.That(diagram.State, Is.SameAs(state));
                Assert.That(pumpRoot.ownedElement.Single().DeclaredName, Is.EqualTo("pump"));
                Assert.That(await opening.OpenAsync("pump.json", pumpBytes.Length, _ => new MemoryStream(pumpBytes)),
                    Is.True);
                Assert.That(session.Current.Root, Is.SameAs(pumpRoot));
            }
            var changed =
                System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(pumpBytes)
                    .Replace("pump\"", "changed\""));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(await opening.OpenAsync("changed.json", changed.Length, _ => new MemoryStream(changed)),
                            Is.False);
                Assert.That(pumpRoot.ownedElement.Single().DeclaredName, Is.EqualTo("pump"));
                Assert.That(opening.State.Error, Does.Contain("conflicts"));
                Assert.That(await opening.OpenAsync(null, 0, null), Is.True);
                Assert.That(session.Current.Root, Is.SameAs(quantities));
            }
        }

        [TestCase("model.xmi", 20, "[]")]
        [TestCase("model.json", 67_108_865, "[]")]
        [TestCase("model.json", 0, "")]
        [TestCase("model.json", 2, "[]")]
        [TestCase("model.json", 1, "{")]
        [TestCase("model.json", 9, "[]")]
        public async Task VerifyInvalidUploadsRetainCurrentWorkspace(string name, long size, string content)
        {
            using var services = CreateServices();
            var session = services.GetRequiredService<WorkspaceModelSession>();
            await session.EnsureLoadedAsync();
            var previous = session.Current;
            var count = services.GetRequiredService<IAssembler>().Cache.Count;
            using var opening = services.GetRequiredService<Func<Mycelium.Bloom.ViewModel.ModelOpeningViewModel>>()();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                            await opening.OpenAsync(name, size, _ => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content))),
                            Is.False);
                Assert.That(opening.State.Error, Is.Not.Empty);
                Assert.That(opening.State.Busy, Is.False);
                Assert.That(session.Current, Is.SameAs(previous));
                Assert.That(services.GetRequiredService<IAssembler>().Cache, Has.Count.EqualTo(count));
            }
            using var recovery = new WorkspaceModelFile("Recovery", "part");
            var valid = await File.ReadAllBytesAsync(recovery.Uri.LocalPath);
            Assert.That(await opening.OpenAsync("recovery.json", valid.Length, _ => new MemoryStream(valid)), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task VerifyUploadCancellationAndContextSwitchRejectLateCompletion(bool switchModel)
        {
            using var services = CreateServices();
            var session = services.GetRequiredService<WorkspaceModelSession>();
            await session.EnsureLoadedAsync();
            var previous = session.Current;
            using var opening = services.GetRequiredService<Func<Mycelium.Bloom.ViewModel.ModelOpeningViewModel>>()();
            using var file = new WorkspaceModelFile("Pump", "pump");
            var bytes = await File.ReadAllBytesAsync(file.Uri.LocalPath);
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var source = new Mock<Stream>();
            var sent = false;
            source.Setup(stream => stream.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                .Returns(async (Memory<byte> buffer, CancellationToken token) =>
                {
                    if (sent)
                    {
                        return 0;
                    }

                    started.TrySetResult();
                    await release.Task.WaitAsync(token);
                    bytes.CopyTo(buffer);
                    sent = true;
                    return bytes.Length;
                });
            var operation = opening.OpenAsync("pump.json", bytes.Length, _ => source.Object);
            await started.Task;
            if (switchModel)
            {
                session.Clear();
            }
            else
            {
                opening.Cancel();
            }

            release.TrySetResult();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(await operation, Is.False);
                Assert.That(session.Current.Root, Is.SameAs(switchModel ? null : previous.Root));
                Assert.That(opening.State.Busy, Is.False);
            }
            source.Verify(stream => stream.DisposeAsync(), Times.Once);
            Assert.That(await opening.OpenAsync("pump.json", bytes.Length, _ => new MemoryStream(bytes)), Is.True);
        }

        [TestCase("duplicate")]
        [TestCase("missing")]
        [TestCase("cycle")]
        [TestCase("remote")]
        [TestCase("file")]
        public async Task VerifyInvalidUploadGraphCannotChangeCanonicalState(string invalidity)
        {
            using var services = CreateServices();
            var session = services.GetRequiredService<WorkspaceModelSession>();
            await session.EnsureLoadedAsync();
            var before = session.Current;
            var count = services.GetRequiredService<IAssembler>().Cache.Count;
            using var file = new WorkspaceModelFile("Pump", "pump");
            var json = System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(file.Uri.LocalPath)).AsArray();
            if (invalidity == "duplicate")
            {
                json.Add(json[2].DeepClone());
            }

            if (invalidity == "missing")
            {
                json[1]["memberElement"]["@id"] = Guid.NewGuid().ToString();
            }

            if (invalidity == "remote")
            {
                json[1]["memberElement"]["@id"] = "https://example.invalid/model.json";
            }

            if (invalidity == "file")
            {
                json[1]["memberElement"]["@id"] = "file:///model.json";
            }

            if (invalidity == "cycle")
            {
                json[0]["owningRelationship"] = new System.Text.Json.Nodes.JsonObject
                { ["@id"] = json[1]["@id"].GetValue<string>() };
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(json.ToJsonString());
            using var opening = services.GetRequiredService<Func<Mycelium.Bloom.ViewModel.ModelOpeningViewModel>>()();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(await opening.OpenAsync("invalid.json", bytes.Length, _ => new MemoryStream(bytes)), Is.False);
                Assert.That(session.Current, Is.SameAs(before));
                Assert.That(services.GetRequiredService<IAssembler>().Cache, Has.Count.EqualTo(count));
            }
            var valid = await File.ReadAllBytesAsync(file.Uri.LocalPath);
            Assert.That(await opening.OpenAsync("valid.json", valid.Length, _ => new MemoryStream(valid)), Is.True);
        }

        [Test]
        public async Task VerifyLargeFileUsesNormalWorkspaceAndProjectsOnlyRequestedDetails()
        {
            using var file = new LargeWorkspaceFile(10_000);
            using var services = CreateServices();
            var session = services.GetRequiredService<WorkspaceModelSession>();
            var root = await session.OpenAsync(file.Uri);
            Assert.That(root, Is.Not.Null);
            var diagram = services.GetRequiredService<Func<DiagramViewModel>>()();
            using (diagram)
            {
                diagram.BindWorkspace(session);
                await diagram.Preparation;
                Assert.That(diagram.PreparationStatus, Is.Empty);
                var original = diagram.State;
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(original.Occurrences, Has.Length.EqualTo(10_000));
                    Assert.That(original.Routes, Has.Length.EqualTo(5000));
                    Assert.That(diagram.IndexedElementCount, Is.EqualTo(file.ElementCount));
                    Assert.That(diagram.Projection.Parts.Values.Sum(part => part.Ports.Length), Is.EqualTo(10_000));
                    Assert.That(
                        diagram.Projection.Parts.Values.SelectMany(part => part.Ports).Count(port =>
                            port.Direction == SysML2.NET.Core.Core.Types.FeatureDirectionKind.Out), Is.EqualTo(5000));
                    Assert.That(
                        diagram.Projection.Parts.Values.SelectMany(part => part.Ports).Count(port =>
                            port.Direction == SysML2.NET.Core.Core.Types.FeatureDirectionKind.In), Is.EqualTo(5000));
                    Assert.That(diagram.Region.Occurrences, Is.Empty);
                    Assert.That(diagram.WorkspaceDetailCache.CurrentEntryCount, Is.Zero);
                    Assert.That(diagram.Projection.Parts.Values.All(part => part.Attributes.IsEmpty), Is.True);
                }

                diagram.RequestRegion(900, 700, 200);
                Assert.That(diagram.Region.Occurrences, Has.Length.InRange(1, 100));
                var firstRegion = diagram.Region;
                Assert.That(diagram.Projection.Parts.Values.Count(part => !part.Attributes.IsEmpty),
                    Is.EqualTo(firstRegion.Occurrences.Length));
                var distant = original.Occurrences[^1];
                Assert.That(diagram.RevealOccurrence(distant.ElementId, 900, 700, 0.5), Is.EqualTo(distant.Id));
                diagram.RequestRegion(900, 700, 200);
                Assert.That(diagram.Region.Occurrences.Any(node => node.Id == distant.Id), Is.True);
                await diagram.SelectAsync([distant.Id], distant.Id);
                var canonical = services.GetRequiredService<IAssembler>().Cache.Values
                    .Single(value => value.Value.ElementId == distant.ElementId).Value;
                Assert.That(services.GetRequiredService<IContextAwareService>().SelectedElement, Is.SameAs(canonical));
                var misses = diagram.WorkspaceDetailCache.TotalMisses;
                Assert.That(diagram.ApplyPresentation(original), Is.True);
                diagram.RequestRegion(900, 700, 200);
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(diagram.WorkspaceDetailCache.TotalMisses, Is.EqualTo(misses));
                    Assert.That(diagram.WorkspaceDetailCache.TotalHits, Is.GreaterThan(0));
                    Assert.That(diagram.WorkspaceDetailCache.CurrentEstimatedSize, Is.LessThanOrEqualTo(1_048_576));
                }

                var parent =
                    original.Occurrences.First(node => diagram.Projection.Parts[node.ElementId].Name == "part00000");
                var child = original.Occurrences.First(node => node.ParentId == parent.Id);
                var moved = original with
                {
                    Occurrences = original.Occurrences.Replace(parent,
                        parent with { Geometry = parent.Geometry with { X = parent.Geometry.X + 30 } })
                };
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(
                                diagram.ApplyRenderedPresentation(original, moved,
                                    new System.Collections.Generic.HashSet<Guid> { parent.Id }), Is.True);
                    Assert.That(diagram.State.Occurrences.Single(node => node.Id == child.Id).Geometry.X,
                        Is.EqualTo(child.Geometry.X + 30));
                    Assert.That(
                        DiagramPresentationSerializer.Validate(diagram.State, diagram.Projection, out _, diagram.Limits),
                        Is.True);
                }
                var json = DiagramPresentationSerializer.Export(diagram.State);
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(
                                DiagramPresentationSerializer.TryImport(json, diagram.Projection, out var restored, out _,
                                    diagram.Limits), Is.True);
                    Assert.That(restored.Occurrences, Has.Length.EqualTo(10_000));
                }
                diagram.IsReadOnly = true;
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(diagram.ApplyPresentation(original), Is.False);
                    Assert.That(diagram.RevealOccurrence(distant.ElementId, 900, 700, 0.5), Is.EqualTo(distant.Id));
                }
            }
            using (Assert.EnterMultipleScope())
            {
                Assert.That(diagram.IndexedElementCount, Is.Zero);
                Assert.That(diagram.WorkspaceDetailCache, Is.Null);
                Assert.That(services.GetRequiredService<IAssembler>().Cache, Has.Count.EqualTo(file.ElementCount));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task VerifyScopePreparationCannotPublishAfterSwitchOrDispose(bool dispose)
        {
            using var services = CreateServices();
            using var file = new LargeWorkspaceFile(600);
            var session = services.GetRequiredService<WorkspaceModelSession>();
            await session.OpenAsync(file.Uri);
            var diagram = services.GetRequiredService<Func<DiagramViewModel>>()();
            var disposed = false;
            try
            {
                diagram.BindWorkspace(session);
                Assert.That(diagram.State, Is.Null);
                if (dispose)
                {
                    disposed = true;
                    diagram.Dispose();
                }
                else
                {
                    session.Clear(true);
                }

                await diagram.Preparation;
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(diagram.State, Is.Null);
                    Assert.That(diagram.IndexedElementCount, Is.Zero);
                    Assert.That(diagram.WorkspaceDetailCache, Is.Null);
                }
            }
            finally
            {
                if (!disposed)
                {
                    diagram.Dispose();
                }
            }
        }

        [Test]
        public async Task VerifyPathologicalNestingIsRejectedWithoutPartialPublication()
        {
            using var services = CreateServices();
            using var file = new LargeWorkspaceFile(140, 130);
            var session = services.GetRequiredService<WorkspaceModelSession>();
            await session.OpenAsync(file.Uri);
            using var diagram = services.GetRequiredService<Func<DiagramViewModel>>()();
            diagram.BindWorkspace(session);
            await diagram.Preparation;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(diagram.State, Is.Null);
                Assert.That(diagram.PreparationStatus, Does.Contain("128 levels"));
                Assert.That(diagram.IndexedElementCount, Is.Zero);
            }
        }

        [Test]
        public async Task VerifyWorkspaceDetailExpirationAndCapacityPreserveCanonicalIdentity()
        {
            using var services = CreateServices();
            using var file = new LargeWorkspaceFile(600);
            var session = services.GetRequiredService<WorkspaceModelSession>();
            await session.OpenAsync(file.Uri);
            var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
            var clock = new Mock<Microsoft.Extensions.Internal.ISystemClock>();
            clock.SetupGet(value => value.UtcNow).Returns(() => now);
            var options =
                Microsoft.Extensions.Options.Options.Create(new Mycelium.Bloom.Core.Configuration.DiagramDataOptions
                { CapacityBytes = 1024 });
            using var diagram = new DiagramViewModel(services.GetRequiredService<IContextAwareService>(),
                services.GetRequiredService<IElementIdResolver>(), options, clock.Object);
            diagram.BindWorkspace(session);
            await diagram.Preparation;
            foreach (var occurrence in diagram.State.Occurrences.Skip(100).Where((_, index) => index % 50 == 0))
            {
                diagram.RevealOccurrence(occurrence.ElementId, 400, 300, 0.5);
                diagram.RequestRegion(400, 300, 20);
                Assert.That(diagram.WorkspaceDetailCache.CurrentEstimatedSize, Is.LessThanOrEqualTo(1024));
            }

            var target = diagram.State.Occurrences[100];
            diagram.RevealOccurrence(target.ElementId, 400, 300, 0.5);
            diagram.RequestRegion(400, 300, 20);
            var previous = diagram.Projection.Parts[target.ElementId];
            await diagram.SelectAsync([target.Id], target.Id);
            var canonical = services.GetRequiredService<IContextAwareService>().SelectedElement;
            now = now.AddSeconds(121);
            diagram.RevealOccurrence(diagram.State.Occurrences[^1].ElementId, 400, 300, 0.5);
            diagram.RequestRegion(400, 300, 20);
            var misses = diagram.WorkspaceDetailCache.TotalMisses;
            diagram.RevealOccurrence(target.ElementId, 400, 300, 0.5);
            diagram.RequestRegion(400, 300, 20);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(diagram.WorkspaceDetailCache.TotalMisses, Is.GreaterThan(misses));
                Assert.That(diagram.Projection.Parts[target.ElementId], Is.Not.SameAs(previous));
            }
            Assert.That(diagram.Projection.Parts[target.ElementId].Attributes, Is.EqualTo(previous.Attributes));
            await diagram.SelectAsync([target.Id], target.Id);
            Assert.That(services.GetRequiredService<IContextAwareService>().SelectedElement, Is.SameAs(canonical));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        [TestCase(-1)]
        public async Task VerifyRegionRejectsInvalidDimensionsWithoutReplacingAcceptedContent(double invalid)
        {
            using var services = CreateServices();
            using var file = new LargeWorkspaceFile(40);
            var session = services.GetRequiredService<WorkspaceModelSession>();
            await session.OpenAsync(file.Uri);
            using var diagram = services.GetRequiredService<Func<DiagramViewModel>>()();
            diagram.UpdateWorkspace(session.Current);
            await diagram.Preparation;
            diagram.RequestRegion(900, 700, 200);
            var region = diagram.Region;
            var projection = diagram.Projection;
            var accepted = diagram.State;
            var cache = diagram.WorkspaceDetailCache;

            diagram.RequestRegion(invalid, 700, 200);
            diagram.RequestRegion(900, invalid, 200);
            diagram.RequestRegion(900, 700, invalid);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(diagram.Region, Is.SameAs(region));
                Assert.That(diagram.Projection, Is.SameAs(projection));
                Assert.That(diagram.State, Is.SameAs(accepted));
                Assert.That(diagram.WorkspaceDetailCache.TotalHits, Is.EqualTo(cache.TotalHits));
                Assert.That(diagram.WorkspaceDetailCache.TotalMisses, Is.EqualTo(cache.TotalMisses));
            }

            var outside = -accepted.Occurrences.Max(node => node.Geometry.X + node.Geometry.Width) - 1000;
            Assert.That(diagram.ApplyPresentation(accepted with { Viewport = new DiagramViewport(outside, 0, 1) }), Is.True);
            diagram.RequestRegion(900, 700, 200);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(diagram.Region.Occurrences, Is.Empty);
                Assert.That(diagram.ApplyPresentation(accepted), Is.True);
            }
            diagram.RequestRegion(900, 700, 200);
            Assert.That(diagram.Region.Occurrences, Is.EqualTo(region.Occurrences));
        }

        [TestCase("width")]
        [TestCase("height")]
        [TestCase("zoom")]
        public async Task VerifyRegionReusesIdenticalRequestsAndRetainsSmallBoundaryChanges(string dimension)
        {
            using var services = CreateServices();
            using var file = new WorkspaceModelFile("Scope", "part");
            var session = services.GetRequiredService<WorkspaceModelSession>();
            await session.OpenAsync(file.Uri);
            using var diagram = services.GetRequiredService<Func<DiagramViewModel>>()();
            diagram.UpdateWorkspace(session.Current);
            await diagram.Preparation;
            var node = diagram.State.Occurrences.Single();
            Assert.That(diagram.ApplyPresentation(diagram.State with
            {
                Occurrences = [node with { Geometry = node.Geometry with { X = 200, Y = 200 } }],
                Viewport = new DiagramViewport(0, 0, 1)
            }), Is.True);
            diagram.RequestRegion(100, 100, 100);
            Assert.That(diagram.Region.Occurrences, Has.Length.EqualTo(1));
            var region = diagram.Region;
            var projection = diagram.Projection;
            var cache = diagram.WorkspaceDetailCache;
            for (var index = 0; index < 3; index++)
            {
                diagram.RequestRegion(100, 100, 100);
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(diagram.Region, Is.SameAs(region));
                Assert.That(diagram.Projection, Is.SameAs(projection));
                Assert.That(diagram.WorkspaceDetailCache.TotalHits, Is.EqualTo(cache.TotalHits));
                Assert.That(diagram.WorkspaceDetailCache.TotalMisses, Is.EqualTo(cache.TotalMisses));
            }

            Assert.That(diagram.ApplyPresentation(diagram.State with { Viewport = new DiagramViewport(-0.0, -0.0, 1) }), Is.True);
            diagram.RequestRegion(100, 100, 100);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(diagram.Region, Is.SameAs(region));
                Assert.That(diagram.WorkspaceDetailCache.TotalHits, Is.EqualTo(cache.TotalHits));
                Assert.That(diagram.WorkspaceDetailCache.TotalMisses, Is.EqualTo(cache.TotalMisses));
            }

            const double delta = 1.0 / 1024;
            if (dimension == "zoom")
            {
                Assert.That(diagram.ApplyPresentation(diagram.State with { Viewport = new DiagramViewport(0, 0, 1 + delta) }), Is.True);
            }

            diagram.RequestRegion(dimension == "width" ? 100 - delta : 100,
                dimension == "height" ? 100 - delta : 100, 100);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(diagram.Region.Occurrences, Is.Empty);
                Assert.That(diagram.ApplyPresentation(diagram.State with { Viewport = new DiagramViewport(0, 0, 1) }), Is.True);
            }
            diagram.RequestRegion(100, 100, 100);
            Assert.That(diagram.Region.Occurrences, Is.EqualTo(region.Occurrences));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public async Task VerifyPresentationRejectsNonFiniteValuesAndPreservesExactAcceptedCoordinates(double invalid)
        {
            using var services = CreateServices();
            using var file = new WorkspaceModelFile("Scope", "part");
            var session = services.GetRequiredService<WorkspaceModelSession>();
            await session.OpenAsync(file.Uri);
            using var diagram = services.GetRequiredService<Func<DiagramViewModel>>()();
            diagram.UpdateWorkspace(session.Current);
            await diagram.Preparation;
            var original = diagram.State;
            var node = original.Occurrences.Single();
            var invalidGeometries = new[]
            {
                node.Geometry with { X = invalid }, node.Geometry with { Y = invalid },
                node.Geometry with { Width = invalid }, node.Geometry with { Height = invalid }
            };
            foreach (var geometry in invalidGeometries)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(diagram.ApplyPresentation(original with { Occurrences = [node with { Geometry = geometry }] }), Is.False);
                    Assert.That(diagram.State, Is.SameAs(original));
                }
            }

            foreach (var viewport in new[] { new DiagramViewport(invalid, 0, 1), new DiagramViewport(0, invalid, 1), new DiagramViewport(0, 0, invalid) })
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(diagram.ApplyPresentation(original with { Viewport = viewport }), Is.False);
                    Assert.That(diagram.State, Is.SameAs(original));
                }
            }

            var exact = original with
            {
                Occurrences = [node with { Geometry = node.Geometry with { X = Math.BitIncrement(node.Geometry.X) } }],
                Viewport = new DiagramViewport(Math.BitIncrement(1), -0.0, Math.BitDecrement(4))
            };
            using (Assert.EnterMultipleScope())
            {
                Assert.That(diagram.ApplyPresentation(exact), Is.True);
                Assert.That(diagram.State, Is.SameAs(exact));
            }
            var encoded = DiagramPresentationSerializer.Export(exact);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(DiagramPresentationSerializer.TryImport(encoded, diagram.Projection, out var restored, out _, diagram.Limits), Is.True);
                Assert.That(restored.Occurrences, Is.EqualTo(exact.Occurrences));
                Assert.That(restored.Viewport, Is.EqualTo(exact.Viewport));
                Assert.That(diagram.ApplyPresentation(exact with { Viewport = exact.Viewport with { Zoom = Math.BitIncrement(4) } }), Is.False);
                Assert.That(diagram.State, Is.SameAs(exact));
            }
        }
    }

    internal sealed class WorkspaceModelFile : IDisposable
    {
        private readonly string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");

        public WorkspaceModelFile(string modelName, string partName)
        {
            var root = Guid.NewGuid();
            var membership = Guid.NewGuid();
            var part = Guid.NewGuid();
            File.WriteAllText(this.path, $$$"""
                                            [
                                              {"@type":"Package","@id":"{{{root}}}","elementId":"{{{root}}}","declaredName":"{{{modelName}}}","ownedRelationship":[{"@id":"{{{membership}}}"}]},
                                              {"@type":"OwningMembership","@id":"{{{membership}}}","elementId":"{{{membership}}}","ownedRelationship":[],"owningRelatedElement":{"@id":"{{{root}}}"},"ownedRelatedElement":[{"@id":"{{{part}}}"}],"memberElement":{"@id":"{{{part}}}"},"source":[{"@id":"{{{root}}}"}],"target":[{"@id":"{{{part}}}"}]},
                                              {"@type":"PartUsage","@id":"{{{part}}}","elementId":"{{{part}}}","declaredName":"{{{partName}}}","ownedRelationship":[],"isComposite":true,"owningRelationship":{"@id":"{{{membership}}}"}}
                                            ]
                                            """);
        }

        public Uri Uri => new(this.path);

        public void Dispose() => File.Delete(this.path);
    }
}
