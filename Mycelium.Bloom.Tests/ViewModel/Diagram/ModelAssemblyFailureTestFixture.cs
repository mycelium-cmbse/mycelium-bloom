// ------------------------------------------------------------------------------------------------
// <copyright file="ModelAssemblyFailureTestFixture.cs" company="Starion Group S.A.">
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
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Extensions;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel;
    using Mycelium.Bloom.ViewModel.Diagram;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;
    using SysML2.NET.Dal;

    [TestFixture, NonParallelizable]
    public sealed class ModelAssemblyFailureTestFixture
    {
        [TestCase(false)]
        [TestCase(true)]
        public async Task VerifyMidAssemblyFailurePreservesModelAndAllowsRetry(bool independent)
        {
            var inject = false;
            var processed = 0;
            var logger = new CallbackLogger<Assembler>(LogLevel.Trace, (_, _, message, _) =>
            {
                if (inject && message.Contains("added to Cache", StringComparison.Ordinal))
                {
                    processed++;
                    throw new InvalidOperationException("Injected failure after SDK cache insertion.");
                }
            });
            var logging = new Mock<ILoggerFactory>();
            logging.Setup(value => value.CreateLogger(It.IsAny<string>())).Returns(logger);
            var assembler = new Assembler(logging.Object);
            var environment = new Mock<IHostEnvironment>();
            environment.SetupGet(value => value.ContentRootPath)
                .Returns(TestRepository.GetDirectoryPath("Mycelium.Bloom"));
            var registrations = new ServiceCollection();
            registrations.AddLogging();
            registrations.AddSingleton(environment.Object);
            registrations.AddApplicationServices();
            registrations.AddSingleton<IAssembler>(assembler);
            WeakReference candidateReference = null;
            registrations.AddSingleton<Func<IAssembler>>(() =>
            {
                var candidate = new Assembler(logging.Object);
                candidateReference = new WeakReference(candidate);
                return candidate;
            });
            using var services = registrations.BuildServiceProvider();
            var session = services.GetRequiredService<WorkspaceModelSession>();
            using var originalFile = new WorkspaceModelFile("Existing", "pump");
            await session.OpenAsync(originalFile.Uri);
            var original = session.Current;
            var objects = assembler.Cache.ToDictionary(pair => pair.Key, pair => pair.Value.Value);
            var child = original.Root.ownedElement.Single();
            var membership = original.Root.OwnedRelationship.Single();
            using var browser = services.GetRequiredService<Func<IProjectBrowserViewModel>>()();
            await browser.InitializeAsync(CancellationToken.None);
            browser.SelectNode(browser.RootNodes.Single());
            var selected = services.GetRequiredService<IElementSelectionService>().SelectedElement;
            var selectedNode = browser.SelectedNode;
            using var diagram = services.GetRequiredService<Func<DiagramViewModel>>()();
            diagram.BindWorkspace(session);
            await diagram.Preparation;
            Assert.That(diagram.AddExisting(child.ElementId), Is.True);
            var presentation = diagram.State;
            var cache = diagram.WorkspaceDetailCache;
            var cacheEntries = cache.CurrentEntryCount;
            var indexed = diagram.IndexedElementCount;
            using var incoming = new WorkspaceModelFile("Incoming", "valve");
            var bytes = await File.ReadAllBytesAsync(incoming.Uri.LocalPath);
            using var opening = services.GetRequiredService<Func<ModelOpeningViewModel>>()();
            var temporaryFiles = Directory.GetFiles(Path.GetTempPath(), "bloom-model-*");
            string temporary = null;
            MemoryStream upload = null;
            inject = true;
            var opened = await opening.OpenAsync("incoming.json", bytes.Length, _ =>
            {
                temporary = Directory.GetFiles(Path.GetTempPath(), "bloom-model-*").Except(temporaryFiles).Single();
                return upload = new MemoryStream(bytes);
            }, independent);
            inject = false;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(opened, Is.False);
                Assert.That(processed, Is.EqualTo(1));
                Assert.That(File.Exists(temporary), Is.False);
                Assert.That(upload.CanRead, Is.False);
                Assert.That(session.Current, Is.SameAs(original));
                Assert.That(original.Root.DeclaredName, Is.EqualTo("Existing"));
                Assert.That(original.Root.ownedElement.Single(), Is.SameAs(child));
                Assert.That(child.DeclaredName, Is.EqualTo("pump"));
                Assert.That(original.Root.OwnedRelationship.Single(), Is.SameAs(membership));
                Assert.That(browser.RootNodes.Single().SourceElement, Is.SameAs(original.Root));
                Assert.That(browser.SelectedNode, Is.SameAs(selectedNode));
                Assert.That(services.GetRequiredService<IElementSelectionService>().SelectedElement,
                    Is.SameAs(selected));
                Assert.That(diagram.State, Is.SameAs(presentation));
                Assert.That(diagram.IsScopeAvailable, Is.True);
                Assert.That(diagram.IndexedElementCount, Is.EqualTo(indexed));
                Assert.That(diagram.WorkspaceDetailCache.CurrentEntryCount, Is.EqualTo(cacheEntries));
                Assert.That(diagram.WorkspaceDetailCache.TotalMisses, Is.EqualTo(cache.TotalMisses));
                Assert.That(diagram.WorkspaceDetailCache.TotalHits, Is.EqualTo(cache.TotalHits));
                foreach (var pair in objects)
                {
                    Assert.That(assembler.Cache[pair.Key].Value, Is.SameAs(pair.Value));
                }

                Assert.That(assembler.Cache.Keys, Is.EquivalentTo(objects.Keys));
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                Assert.That(candidateReference.IsAlive, Is.False);
                Assert.That(
                    await opening.OpenAsync("incoming.json", bytes.Length, _ => new MemoryStream(bytes), independent),
                    Is.True);
                using var acceptedLease = opening.TakeOpenedContext();
                var acceptedContext = independent ? acceptedLease.Snapshot : session.Current;
                var accepted = acceptedContext.Root.ownedElement.Single();
                Assert.That(
                    await services.GetRequiredService<IElementIdResolver>()
                        .ResolveAsync(accepted.ElementId, CancellationToken.None), Is.SameAs(accepted));
                using var acceptedBrowser = services.GetRequiredService<Func<IProjectBrowserViewModel>>()();
                Assert.That(await acceptedBrowser.InitializeAsync(acceptedContext.Root, CancellationToken.None),
                    Is.True);
                Assert.That(acceptedBrowser.RootNodes.Single().SourceElement, Is.SameAs(acceptedContext.Root));
                using var acceptedDiagram = services.GetRequiredService<Func<DiagramViewModel>>()();
                acceptedDiagram.UpdateWorkspace(acceptedContext);
                await acceptedDiagram.Preparation;
                var occurrence = acceptedDiagram.State.Occurrences.Single();
                await acceptedDiagram.SelectAsync([occurrence.Id], occurrence.Id);
                Assert.That(services.GetRequiredService<IElementSelectionService>().SelectedElement,
                    Is.SameAs(accepted));
                Assert.That(accepted.owningNamespace, Is.SameAs(acceptedContext.Root));
                using var recoveryFile = new WorkspaceModelFile("Recovery", "recovered");
                var recovery = await File.ReadAllBytesAsync(recoveryFile.Uri.LocalPath);
                Assert.That(
                    await opening.OpenAsync("recovery.json", recovery.Length, _ => new MemoryStream(recovery),
                        independent), Is.True);
            }
        }
    }
}
