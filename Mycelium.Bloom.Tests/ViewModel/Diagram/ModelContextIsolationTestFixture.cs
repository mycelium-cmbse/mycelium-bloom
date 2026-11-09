// ------------------------------------------------------------------------------------------------
// <copyright file="ModelContextIsolationTestFixture.cs" company="Starion Group S.A.">
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
    using System.Linq;
    using System.Reactive.Linq;
    using ReactiveUI;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Moq;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Extensions;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel;
    using Mycelium.Bloom.ViewModel.Diagram;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    [TestFixture]
    public sealed class ModelContextIsolationTestFixture
    {
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
        public async Task VerifyInitialRegionRequestDuringPublicationIsRetained()
        {
            using var services = CreateServices();
            var session = services.GetRequiredService<WorkspaceModelSession>();
            using var file = new WorkspaceModelFile("Model", "part");
            await using var stream = File.OpenRead(file.Uri.LocalPath);
            using var model = await session.OpenContextAsync(stream, CancellationToken.None);
            using var diagram = services.GetRequiredService<Func<DiagramViewModel>>()();
            using var observer = diagram.WhenAnyValue(value => value.State).Where(value => value is not null)
                .Subscribe(_ => diagram.RequestRegion(1000, 700, 200));
            diagram.UpdateWorkspace(model.Snapshot);
            await diagram.Preparation;
            Assert.That(diagram.Region.Occurrences, Has.Length.EqualTo(1));
        }

        [Test]
        public async Task VerifyOverlappingIdsRemainCanonicalWithinIndependentContexts()
        {
            using var services = CreateServices();
            var session = services.GetRequiredService<WorkspaceModelSession>();
            await session.EnsureLoadedAsync();
            var quantities = session.Current;
            using var file = new WorkspaceModelFile("Pump model", "pump");
            var original = await File.ReadAllTextAsync(file.Uri.LocalPath);
            using var firstStream = new MemoryStream(Encoding.UTF8.GetBytes(original));
            using var first = await session.OpenContextAsync(firstStream, CancellationToken.None);
            using var secondStream =
                new MemoryStream(Encoding.UTF8.GetBytes(original.Replace("Pump model", "Valve model")
                    .Replace("pump", "valve")));
            using var second = await session.OpenContextAsync(secondStream, CancellationToken.None);
            Assert.That(first.Snapshot.Id, Is.Not.EqualTo(second.Snapshot.Id));
            Assert.That(first.Snapshot.Root.ElementId, Is.EqualTo(second.Snapshot.Root.ElementId));
            Assert.That(first.Snapshot.Root, Is.Not.SameAs(second.Snapshot.Root));
            Assert.That(session.Current, Is.SameAs(quantities));
            using var pump = services.GetRequiredService<Func<DiagramViewModel>>()();
            using var valve = services.GetRequiredService<Func<DiagramViewModel>>()();
            pump.UpdateWorkspace(first.Snapshot);
            valve.UpdateWorkspace(second.Snapshot);
            await Task.WhenAll(pump.Preparation, valve.Preparation);
            var pumpPart = first.Snapshot.Root.ownedElement.Single();
            var valvePart = second.Snapshot.Root.ownedElement.Single();
            var occurrence = pump.State.Occurrences.Single();
            var other = valve.State.Occurrences.Single();
            Assert.That(pump.ContainsCanonicalElement(valvePart), Is.False);
            Assert.That(valve.ContainsCanonicalElement(pumpPart), Is.False);
            await pump.SelectAsync([occurrence.Id], occurrence.Id);
            Assert.That(services.GetRequiredService<IContextAwareService>().SelectedElement, Is.SameAs(pumpPart));
            await valve.SelectAsync([other.Id], other.Id);
            Assert.That(services.GetRequiredService<IContextAwareService>().SelectedElement, Is.SameAs(valvePart));
            Assert.That(pump.SelectedOccurrences, Does.Contain(occurrence.Id));
            using var browser = services.GetRequiredService<Func<IProjectBrowserViewModel>>()();
            await browser.InitializeAsync(first.Snapshot.Root, CancellationToken.None);
            browser.SelectNode(browser.RootNodes.Single());
            Assert.That(services.GetRequiredService<IContextAwareService>().SelectedElement,
                Is.SameAs(first.Snapshot.Root));
            Assert.That(services.GetRequiredService<IModelLoaderService>().FindUploadedElement(pumpPart.ElementId),
                Is.Null);
            Assert.That(
                services.GetRequiredService<IModelLoaderService>()
                    .FindUploadedElement(first.Snapshot.Root, pumpPart.ElementId), Is.SameAs(pumpPart));
        }

        [Test]
        public async Task VerifyIdenticalContentReusesOwnerUntilTheLastConsumerCloses()
        {
            using var services = CreateServices();
            var session = services.GetRequiredService<WorkspaceModelSession>();
            using var file = new WorkspaceModelFile("Example", "part");
            var bytes = await File.ReadAllBytesAsync(file.Uri.LocalPath);
            using var firstStream = new MemoryStream(bytes);
            var first = await session.OpenContextAsync(firstStream, CancellationToken.None);
            using var secondStream = new MemoryStream(bytes);
            var second = await session.OpenContextAsync(secondStream, CancellationToken.None);
            Assert.That(second.Snapshot, Is.SameAs(first.Snapshot));
            var child = first.Snapshot.Root.ownedElement.Single();
            services.GetRequiredService<IContextAwareService>().SelectedElement = child;
            first.Dispose();
            Assert.That(session.OpenModels, Has.Count.EqualTo(1));
            Assert.That(services.GetRequiredService<IModelLoaderService>().FindUploadedElement(child.ElementId),
                Is.SameAs(child));
            second.Dispose();
            Assert.That(session.OpenModels, Is.Empty);
            Assert.That(services.GetRequiredService<IModelLoaderService>().FindUploadedElement(child.ElementId),
                Is.Null);
            Assert.That(services.GetRequiredService<IContextAwareService>().SelectedElement, Is.Null);
            using var retryStream = new MemoryStream(bytes);
            using var retry = await session.OpenContextAsync(retryStream, CancellationToken.None);
            Assert.That(retry.Snapshot.Id, Is.Not.EqualTo(first.Snapshot.Id));
            Assert.That(retry.Snapshot.Root.ownedElement.Single().DeclaredName, Is.EqualTo("part"));
        }

        [Test]
        public async Task VerifyCachedOwnerSurvivesLastTabClosingBeforeAcceptance()
        {
            using var services = CreateServices();
            var loader = services.GetRequiredService<IModelLoaderService>();
            using var file = new WorkspaceModelFile("Example", "part");
            using var firstStream = File.OpenRead(file.Uri.LocalPath);
            var original = await loader.OpenIndependentModelAsync(firstStream, CancellationToken.None,
                publish => Task.FromResult(publish()));
            using var secondStream = File.OpenRead(file.Uri.LocalPath);
            var reopened = await loader.OpenIndependentModelAsync(secondStream, CancellationToken.None, publish =>
            {
                loader.ReleaseUploadedModel(original);
                return Task.FromResult(publish());
            });
            Assert.That(reopened, Is.SameAs(original));
            Assert.That(loader.FindUploadedElement(reopened, reopened.ElementId), Is.SameAs(reopened));
            loader.ReleaseUploadedModel(reopened);
            Assert.That(loader.FindUploadedElement(reopened.ElementId), Is.Null);
        }

        [Test]
        public async Task VerifyCancelledOpeningDoesNotRegisterAContextAndRetrySucceeds()
        {
            using var services = CreateServices();
            var session = services.GetRequiredService<WorkspaceModelSession>();
            await session.EnsureLoadedAsync();
            var original = session.Current;
            using var opening = services.GetRequiredService<Func<ModelOpeningViewModel>>()();
            using var file = new WorkspaceModelFile("Example", "part");
            var bytes = await File.ReadAllBytesAsync(file.Uri.LocalPath);
            Assert.That(await opening.OpenAsync("part.json", bytes.Length, _ =>
            {
                opening.Cancel();
                return new MemoryStream(bytes);
            }, true), Is.False);
            Assert.That(session.OpenModels, Is.Empty);
            Assert.That(session.Current, Is.SameAs(original));
            Assert.That(await opening.OpenAsync("part.json", bytes.Length, _ => new MemoryStream(bytes), true),
                Is.True);
            using var lease = opening.TakeOpenedContext();
            Assert.That(lease.Snapshot.Root.DeclaredName, Is.EqualTo("Example"));
            Assert.That(session.Current, Is.SameAs(original));
        }
    }
}