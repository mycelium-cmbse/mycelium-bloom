// ------------------------------------------------------------------------------------------------
// <copyright file="ModelUploadBoundaryTestFixture.cs" company="Starion Group S.A.">
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
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Moq;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Extensions;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel;
    using Mycelium.Bloom.ViewModel.Diagram;
    using SysML2.NET.Dal;
    using SysML2.NET.Core.POCO.Root.Namespaces;

    [TestFixture, NonParallelizable]
    public sealed class ModelUploadBoundaryTestFixture
    {
        private static ServiceProvider Services()
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
        public async Task VerifyOverlappingIdsStayIsolatedAcrossCircuits()
        {
            await using var secondServices = Services();
            using var file = new WorkspaceModelFile("Original", "pump");
            var original = await File.ReadAllBytesAsync(file.Uri.LocalPath);
            var changed = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(original).Replace("pump", "valve")
                .Replace("Original", "Changed"));
            var firstServices = Services();
            var firstTab = firstServices.GetRequiredService<Func<DiagramViewModel>>()();
            using var secondTab = secondServices.GetRequiredService<Func<DiagramViewModel>>()();
            var second = secondServices.GetRequiredService<WorkspaceModelSession>();
            var other = secondServices.GetRequiredService<IElementSelectionService>().SelectedElement;
            await using (firstServices)
            using (firstTab)
            {
                var first = firstServices.GetRequiredService<WorkspaceModelSession>();
                using var firstOpener = firstServices.GetRequiredService<Func<ModelOpeningViewModel>>()();
                using var secondOpener = secondServices.GetRequiredService<Func<ModelOpeningViewModel>>()();
                Assert.That(await firstOpener.OpenAsync("first.json", original.Length, _ => new MemoryStream(original)),
                    Is.True);
                firstTab.BindWorkspace(first);
                await firstTab.Preparation;
                var firstState = firstTab.State;
                var occurrence = firstState.Occurrences.Single();
                await firstTab.SelectAsync([occurrence.Id], occurrence.Id);
                var selected = firstServices.GetRequiredService<IElementSelectionService>().SelectedElement;
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(await firstOpener.OpenAsync("changed.json", changed.Length, _ => new MemoryStream(changed)),
                                Is.False);
                    Assert.That(firstTab.State, Is.SameAs(firstState));
                    Assert.That(firstServices.GetRequiredService<IElementSelectionService>().SelectedElement,
                        Is.SameAs(selected));
                    Assert.That(selected.DeclaredName, Is.EqualTo("pump"));
                    Assert.That(await secondOpener.OpenAsync("changed.json", changed.Length, _ => new MemoryStream(changed)),
                        Is.True);
                }
                secondTab.BindWorkspace(second);
                await secondTab.Preparation;
                var secondOccurrence = secondTab.State.Occurrences.Single();
                await secondTab.SelectAsync([secondOccurrence.Id], secondOccurrence.Id);
                other = secondServices.GetRequiredService<IElementSelectionService>().SelectedElement;
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(other.ElementId, Is.EqualTo(selected.ElementId));
                    Assert.That(other, Is.Not.SameAs(selected));
                }
                Assert.That(other.DeclaredName, Is.EqualTo("valve"));
            }
            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstTab.IndexedElementCount, Is.Zero);
                Assert.That(firstTab.WorkspaceDetailCache, Is.Null);
                Assert.That(secondTab.IsScopeAvailable, Is.True);
                Assert.That(other, Is.SameAs(second.Current.Root.ownedElement.Single()));
            }
        }

        [TestCase("cancel")]
        [TestCase("switch")]
        [TestCase("dispose")]
        public async Task VerifyLateAcceptanceDoesNotAssembleObsoleteUpload(string action)
        {
            using var services = Services();
            var real = services.GetRequiredService<IModelLoaderService>();
            var proxy = new Mock<IModelLoaderService>();
            proxy.Setup(loader => loader.LoadQuantitiesModel()).Returns(real.LoadQuantitiesModel);
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var release = new ManualResetEventSlim();
            proxy.Setup(loader => loader.OpenUploadedModelAsync(It.IsAny<Stream>(),
                    It.IsAny<Func<Func<INamespace>, Task<INamespace>>>(), It.IsAny<CancellationToken>()))
                .Returns((Stream stream, Func<Func<INamespace>, Task<INamespace>> accept, CancellationToken token) =>
                    real.OpenUploadedModelAsync(stream, assemble =>
                    {
                        ready.TrySetResult();
                        Assert.That(release.Wait(TimeSpan.FromSeconds(10)), Is.True);
                        return accept(assemble);
                    }, token));
            var session =
                new WorkspaceModelSession(proxy.Object, services.GetRequiredService<IElementSelectionService>());
            var disposed = false;
            try
            {
                await session.EnsureLoadedAsync();
                var original = session.Current;
                var count = services.GetRequiredService<IAssembler>().Cache.Count;
                using var file = new WorkspaceModelFile("Late model", "part");
                await using var input = File.OpenRead(file.Uri.LocalPath);
                using var cancellation = new CancellationTokenSource();
                var opening = session.OpenUploadedAsync(input, original.Id, cancellation.Token);
                await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));
                if (action == "cancel")
                {
                    await cancellation.CancelAsync();
                }
                else if (action == "switch")
                {
                    session.Clear();
                }
                else
                {
                    disposed = true;
                    session.Dispose();
                }

                release.Set();
                await Assert.ThatAsync(async () => await opening, Throws.InstanceOf<OperationCanceledException>());
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(services.GetRequiredService<IAssembler>().Cache, Has.Count.EqualTo(count));
                    Assert.That(session.Current.Root, Is.SameAs(action == "cancel" ? original.Root : null));
                }
                if (action != "dispose")
                {
                    Assert.That(await session.OpenUploadedAsync(input, session.Current.Id, CancellationToken.None),
                        Is.True);
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

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        public async Task VerifyServerByteBoundaryAndTemporaryCleanup(int extraByte)
        {
            using var services = Services();
            using var file = new WorkspaceModelFile("Boundary", "part");
            var payload = await File.ReadAllBytesAsync(file.Uri.LocalPath);
            using var opener = services.GetRequiredService<Func<ModelOpeningViewModel>>()();
            var before = Directory.GetFiles(Path.GetTempPath(), "bloom-model-*");
            string temporary = null;
            long position = 0;
            var actual = ModelOpeningViewModel.MaximumBytes + extraByte;
            var source = new Mock<Stream>();
            source.Setup(stream => stream.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                .Returns((Memory<byte> buffer, CancellationToken token) =>
                {
                    var count = (int)Math.Min(buffer.Length, actual - position);
                    buffer.Span[..count].Fill((byte)' ');
                    if (position < payload.Length)
                    {
                        payload.AsMemory((int)position, Math.Min(count, payload.Length - (int)position)).CopyTo(buffer);
                    }

                    position += count;
                    return ValueTask.FromResult(count);
                });
            var opened = await opener.OpenAsync("model.json", Math.Min(actual, ModelOpeningViewModel.MaximumBytes), _ =>
            {
                temporary = Directory.GetFiles(Path.GetTempPath(), "bloom-model-*").Except(before).Single();
                return source.Object;
            });
            using (Assert.EnterMultipleScope())
            {
                Assert.That(opened, Is.EqualTo(extraByte <= 0));
                Assert.That(File.Exists(temporary), Is.False);
            }
            source.Verify(stream => stream.DisposeAsync(), Times.Once);
            using var recovery = new WorkspaceModelFile("Recovery", "recovered");
            var bytes = await File.ReadAllBytesAsync(recovery.Uri.LocalPath);
            Assert.That(await opener.OpenAsync("recovery.json", bytes.Length, _ => new MemoryStream(bytes)), Is.True);
        }

        [TestCase("json")]
        [TestCase("disconnect")]
        [TestCase("cancel")]
        [TestCase("dispose")]
        public async Task VerifyFailureCleanupAndRecovery(string failure)
        {
            using var services = Services();
            var session = services.GetRequiredService<WorkspaceModelSession>();
            await session.EnsureLoadedAsync();
            var original = session.Current;
            using var opener = services.GetRequiredService<Func<ModelOpeningViewModel>>()();
            var before = Directory.GetFiles(Path.GetTempPath(), "bloom-model-*");
            string temporary = null;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(await opener.OpenAsync("model.json", 1, token =>
                        {
                            temporary = Directory.GetFiles(Path.GetTempPath(), "bloom-model-*").Except(before).Single();
                            if (failure == "disconnect")
                            {
                                throw new IOException("Upload connection ended");
                            }

                            if (failure == "cancel")
                            {
                                opener.Cancel();
                            }

                            if (failure == "dispose")
                            {
                                opener.Dispose();
                            }

                            token.ThrowIfCancellationRequested();
                            return new MemoryStream([(byte)'{']);
                        }), Is.False);
                Assert.That(File.Exists(temporary), Is.False);
                Assert.That(session.Current, Is.SameAs(original));
            }
            using var recovery = services.GetRequiredService<Func<ModelOpeningViewModel>>()();
            using var file = new WorkspaceModelFile("Recovery", "part");
            var bytes = await File.ReadAllBytesAsync(file.Uri.LocalPath);
            Assert.That(await recovery.OpenAsync("recovery.json", bytes.Length, _ => new MemoryStream(bytes)), Is.True);
        }

        [Test]
        public async Task VerifyCircuitDisconnectCancelsUploadWithoutEndingModelAndReconnectAllowsRecovery()
        {
            using var services = Services();
            var connection = services.GetRequiredService<WorkspaceConnectionHandler>();
            Assert.That(services.GetServices<Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandler>().Single(),
                Is.SameAs(connection));
            var session = services.GetRequiredService<WorkspaceModelSession>();
            await session.EnsureLoadedAsync();
            var original = session.Current;
            using var opener = services.GetRequiredService<Func<ModelOpeningViewModel>>()();
            var before = Directory.GetFiles(Path.GetTempPath(), "bloom-model-*");
            string temporary = null;
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var waiting = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var source = new Mock<Stream>();
            source.Setup(stream => stream.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                .Returns(async (Memory<byte> buffer, CancellationToken token) =>
                {
                    ready.TrySetResult();
                    return await waiting.Task.WaitAsync(token);
                });
            var upload = opener.OpenStreamAsync("model.json", 100, async _ =>
            {
                await Task.Yield();
                temporary = Directory.GetFiles(Path.GetTempPath(), "bloom-model-*").Except(before).Single();
                return source.Object;
            });
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await connection.OnConnectionDownAsync(null, CancellationToken.None);
            Assert.That(await upload, Is.False);
            source.Verify(stream => stream.DisposeAsync(), Times.Once);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(File.Exists(temporary), Is.False);
                Assert.That(session.Current, Is.SameAs(original));
            }
            await connection.OnConnectionUpAsync(null, CancellationToken.None);
            using var file = new WorkspaceModelFile("Recovery", "part");
            var bytes = await File.ReadAllBytesAsync(file.Uri.LocalPath);
            Assert.That(await opener.OpenAsync("recovery.json", bytes.Length, _ => new MemoryStream(bytes)), Is.True);
        }

        [Test]
        public async Task VerifyCancellationDuringStreamAcquisitionCleansTemporaryAndAllowsRetry()
        {
            using var services = Services();
            var session = services.GetRequiredService<WorkspaceModelSession>();
            await session.EnsureLoadedAsync();
            var original = session.Current;
            using var opener = services.GetRequiredService<Func<ModelOpeningViewModel>>()();
            var before = Directory.GetFiles(Path.GetTempPath(), "bloom-model-*");
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var acquisition = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);
            string temporary = null;
            var upload = opener.OpenStreamAsync("model.json", 100, async token =>
            {
                temporary = Directory.GetFiles(Path.GetTempPath(), "bloom-model-*").Except(before).Single();
                ready.SetResult();
                return await acquisition.Task.WaitAsync(token);
            }, true);
            await ready.Task;
            opener.Cancel();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(await upload, Is.False);
                Assert.That(File.Exists(temporary), Is.False);
                Assert.That(session.Current, Is.SameAs(original));
            }
            using var file = new WorkspaceModelFile("Recovery", "part");
            var bytes = await File.ReadAllBytesAsync(file.Uri.LocalPath);
            Assert.That(await opener.OpenStreamAsync("recovery.json", bytes.Length,
                _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes)), true), Is.True);
            using var accepted = opener.TakeOpenedContext();
            Assert.That(accepted.Snapshot.Root.DeclaredName, Is.EqualTo("Recovery"));
        }
    }
}