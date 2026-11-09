// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramDataCacheTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.Diagram
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Core.ModelLoading;

    [TestFixture]
    public sealed class DiagramDataCacheTestFixture
    {
        private static Mock<IDiagramModelSource> Source(string authorization = "reader")
        {
            var source = new Mock<IDiagramModelSource>(MockBehavior.Strict);
            source.SetupGet(value => value.Scope)
                .Returns(new DiagramReadScope("source", "project/model", "revision", authorization));
            source.Setup(value => value.AuthorizeAsync(It.IsAny<DiagramReadScope>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);
            return source;
        }

        [Test]
        public async Task VerifySharedFetchSurvivesOneCancelledConsumerAndReusesPayload()
        {
            var source = Source();
            var completion =
                new TaskCompletionSource<IReadOnlyDictionary<string, string>>(TaskCreationOptions
                    .RunContinuationsAsynchronously);
            source.Setup(value => value.FetchAsync(source.Object.Scope, It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>())).Returns(completion.Task);
            using var cache = new DiagramDataCache(source.Object, new DiagramDataOptions());
            using var cancelled = new CancellationTokenSource();
            var first = cache.ReadAsync(["part"], true, cancelled.Token);
            var second = cache.ReadAsync(["part"], true, CancellationToken.None);
            cancelled.Cancel();
            Assert.That(async () => await first, Throws.InstanceOf<OperationCanceledException>());
            completion.SetResult(new Dictionary<string, string> { ["part"] = "complete SDK payload" });
            Assert.That((await second)["part"], Is.EqualTo("complete SDK payload"));
            Assert.That((await cache.ReadAsync(["part"], true, CancellationToken.None))["part"],
                Is.EqualTo("complete SDK payload"));
            source.Verify(
                value => value.FetchAsync(source.Object.Scope, It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()), Times.Once);
            Assert.That(cache.Hits, Is.EqualTo(1));
        }

        [Test]
        public async Task VerifyInvalidationRejectsLateResponseAndAllowsRetry()
        {
            var source = Source();
            var completion =
                new TaskCompletionSource<IReadOnlyDictionary<string, string>>(TaskCreationOptions
                    .RunContinuationsAsynchronously);
            source.SetupSequence(value => value.FetchAsync(source.Object.Scope, It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(completion.Task).ReturnsAsync(new Dictionary<string, string> { ["part"] = "new" });
            using var cache = new DiagramDataCache(source.Object, new DiagramDataOptions());
            var old = cache.ReadAsync(["part"], true, CancellationToken.None);
            cache.Invalidate();
            Assert.That(async () => await old, Throws.InstanceOf<OperationCanceledException>());
            var current = await cache.ReadAsync(["part"], true, CancellationToken.None);
            completion.SetResult(new Dictionary<string, string> { ["part"] = "old" });
            Assert.That(current["part"], Is.EqualTo("new"));
            Assert.That((await cache.ReadAsync(["part"], true, CancellationToken.None))["part"], Is.EqualTo("new"));
        }

        [Test]
        public async Task VerifyPermissionRecheckPreventsCachedReadAndScopesRemainIndependent()
        {
            var first = Source("first");
            var second = Source("second");
            first.Setup(value => value.FetchAsync(first.Object.Scope, It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<string, string> { ["part"] = "first payload" });
            second.Setup(value => value.FetchAsync(second.Object.Scope, It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<string, string> { ["part"] = "second payload" });
            using var firstCache = new DiagramDataCache(first.Object, new DiagramDataOptions());
            using var secondCache = new DiagramDataCache(second.Object, new DiagramDataOptions());
            Assert.That((await firstCache.ReadAsync(["part"], true, CancellationToken.None))["part"],
                Is.EqualTo("first payload"));
            Assert.That((await secondCache.ReadAsync(["part"], true, CancellationToken.None))["part"],
                Is.EqualTo("second payload"));
            first.Setup(value => value.AuthorizeAsync(It.IsAny<DiagramReadScope>(), It.IsAny<CancellationToken>()))
                .Throws(new UnauthorizedAccessException());
            Assert.That(async () => await firstCache.ReadAsync(["part"], true, CancellationToken.None),
                Throws.TypeOf<UnauthorizedAccessException>());
        }

        [Test]
        public async Task VerifyFailedReadIsNotCachedAndExportDoesNotPopulateInteractiveCache()
        {
            var source = Source();
            source.SetupSequence(value => value.FetchAsync(source.Object.Scope, It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new System.IO.IOException("Unavailable"))
                .ReturnsAsync(new Dictionary<string, string> { ["part"] = "complete" });
            using var cache = new DiagramDataCache(source.Object, new DiagramDataOptions());
            Assert.That(async () => await cache.ReadAsync(["part"], false, CancellationToken.None),
                Throws.TypeOf<System.IO.IOException>());
            Assert.That((await cache.ReadAsync(["part"], false, CancellationToken.None))["part"],
                Is.EqualTo("complete"));
            Assert.That(cache.Statistics.CurrentEntryCount, Is.Zero);
        }

        [Test]
        public async Task VerifyCapacityAndBatchBounds()
        {
            var source = Source();
            source.Setup(value => value.FetchAsync(source.Object.Scope, It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()))
                .Returns((DiagramReadScope _, IReadOnlyList<string> ids, CancellationToken _) =>
                    Task.FromResult<IReadOnlyDictionary<string, string>>(ids.ToDictionary(id => id,
                        id => new string('x', 60))));
            using var cache = new DiagramDataCache(source.Object,
                new DiagramDataOptions { CapacityBytes = 100, BatchSize = 2 });
            var result = await cache.ReadAsync(["a", "b", "c", "d", "e"], true, CancellationToken.None);
            Assert.That(result, Has.Count.EqualTo(5));
            Assert.That(cache.Statistics.CurrentEstimatedSize, Is.LessThanOrEqualTo(100));
            source.Verify(
                value => value.FetchAsync(source.Object.Scope, It.Is<IReadOnlyList<string>>(ids => ids.Count <= 2),
                    It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Test]
        public void VerifyRevisionChangeRejectsOldPayload()
        {
            var source = Source();
            var original = source.Object.Scope;
            var completion =
                new TaskCompletionSource<IReadOnlyDictionary<string, string>>(TaskCreationOptions
                    .RunContinuationsAsynchronously);
            source.Setup(value =>
                    value.FetchAsync(original, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .Returns(completion.Task);
            using var cache = new DiagramDataCache(source.Object, new DiagramDataOptions());
            var pending = cache.ReadAsync(["part"], true, CancellationToken.None);
            source.SetupGet(value => value.Scope).Returns(original with { Revision = "new-revision" });
            completion.SetResult(new Dictionary<string, string> { ["part"] = "old" });
            Assert.That(async () => await pending, Throws.InstanceOf<OperationCanceledException>());
            Assert.That(cache.Statistics.CurrentEntryCount, Is.Zero);
        }

        [Test]
        public async Task VerifyEvictionAndExpirationReloadPayloads()
        {
            var source = Source();
            var clock = new Clock();
            var reads = 0;
            source.Setup(value => value.FetchAsync(source.Object.Scope, It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()))
                .Returns((DiagramReadScope _, IReadOnlyList<string> ids, CancellationToken _) =>
                {
                    reads++;
                    return Task.FromResult<IReadOnlyDictionary<string, string>>(ids.ToDictionary(id => id,
                        id => new string('x', 60)));
                });
            using var cache = new DiagramDataCache(source.Object,
                new DiagramDataOptions { CapacityBytes = 100, ExpirationSeconds = 10 }, clock);
            await cache.ReadAsync(["a"], true, CancellationToken.None);
            await cache.ReadAsync(["a"], true, CancellationToken.None);
            Assert.That(reads, Is.EqualTo(1));
            await cache.ReadAsync(["b"], true, CancellationToken.None);
            await cache.ReadAsync(["a"], true, CancellationToken.None);
            Assert.That(reads, Is.EqualTo(3));
            clock.UtcNow += TimeSpan.FromSeconds(11);
            await cache.ReadAsync(["a"], true, CancellationToken.None);
            Assert.That(reads, Is.EqualTo(4));
            Assert.That(cache.Statistics.CurrentEstimatedSize, Is.EqualTo(60));
        }

        private sealed class Clock : Microsoft.Extensions.Internal.ISystemClock
        {
            public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UnixEpoch;
        }
    }
}