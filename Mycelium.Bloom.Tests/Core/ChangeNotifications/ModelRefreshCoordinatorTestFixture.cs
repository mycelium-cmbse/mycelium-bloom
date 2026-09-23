// ------------------------------------------------------------------------------------------------
// <copyright file="ModelRefreshCoordinatorTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Core.ChangeNotifications
{
    using System;
    using System.Reactive.Concurrency;
    using System.Threading;
    using System.Threading.Tasks;

    using Moq;

    using Mycelium.Bloom.Core.ChangeNotifications;

    [TestFixture]
    public sealed class ModelRefreshCoordinatorTestFixture
    {
        [Test]
        public async Task VerifyRequestRefreshAwaitsRegisteredHandlerThroughServiceWhileDisabled()
        {
            using var coordinator = new ModelRefreshCoordinator();
            var scheduler = new HistoricalScheduler();
            using var service = new ChangeNotificationService(coordinator, scheduler);
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var handler = new Mock<IModelRefreshHandler>(MockBehavior.Strict);
            handler.Setup(x => x.ReloadAsync(It.IsAny<CancellationToken>())).Returns(completion.Task);
            using var registration = coordinator.Register(handler.Object);
            service.IsEnabled = false;

            var refresh = service.RequestRefresh();

            Assert.That(refresh.IsCompleted, Is.False);
            handler.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Once);
            completion.SetResult();
            await refresh;
            service.Dispose();
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1));
        }

        [Test]
        public async Task VerifyRequestRefreshPropagatesFailureAndAllowsRecovery()
        {
            using var coordinator = new ModelRefreshCoordinator();
            var failure = new InvalidOperationException("Reload failed.");
            var handler = new Mock<IModelRefreshHandler>(MockBehavior.Strict);
            handler.SetupSequence(x => x.ReloadAsync(It.IsAny<CancellationToken>()))
                .Throws(failure)
                .Returns(Task.FromException(failure))
                .Returns(Task.CompletedTask);
            using var registration = coordinator.Register(handler.Object);

            Assert.That(Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.RequestRefresh()), Is.SameAs(failure));
            Assert.That(Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.RequestRefresh()), Is.SameAs(failure));
            await coordinator.RequestRefresh();
            handler.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Test]
        public async Task VerifyRequestRefreshSerializesHandlers()
        {
            using var coordinator = new ModelRefreshCoordinator();
            var firstCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var secondCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var invocation = 0;
            var handler = new Mock<IModelRefreshHandler>(MockBehavior.Strict);
            handler.Setup(x => x.ReloadAsync(It.IsAny<CancellationToken>())).Returns(() =>
            {
                if (Interlocked.Increment(ref invocation) == 1)
                {
                    return firstCompletion.Task;
                }

                secondStarted.SetResult();
                return secondCompletion.Task;
            });
            using var registration = coordinator.Register(handler.Object);
            var first = coordinator.RequestRefresh();
            var second = coordinator.RequestRefresh();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(invocation, Is.EqualTo(1));
                Assert.That(second.IsCompleted, Is.False);
            }
            firstCompletion.SetResult();
            await first;
            await secondStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.That(second.IsCompleted, Is.False);
            secondCompletion.SetResult();
            await second;
            Assert.That(invocation, Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void VerifyRequestRefreshPropagatesCallerCancellation(bool queued)
        {
            using var coordinator = new ModelRefreshCoordinator();
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var handler = new Mock<IModelRefreshHandler>(MockBehavior.Strict);
            handler.Setup(x => x.ReloadAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken token) => completion.Task.WaitAsync(token));
            using var registration = coordinator.Register(handler.Object);
            using var cancellation = new CancellationTokenSource();
            var refresh = coordinator.RequestRefresh(cancellation.Token);
            var pending = queued ? coordinator.RequestRefresh(cancellation.Token) : refresh;

            cancellation.Cancel();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Assert.CatchAsync<OperationCanceledException>(() => refresh), Is.Not.Null);
                Assert.That(Assert.CatchAsync<OperationCanceledException>(() => pending), Is.Not.Null);
            }
            handler.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void VerifyRequestRefreshRejectsAlreadyCanceledRequestWithoutInvokingHandler()
        {
            using var coordinator = new ModelRefreshCoordinator();
            var handler = new Mock<IModelRefreshHandler>(MockBehavior.Strict);
            using var registration = coordinator.Register(handler.Object);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            Assert.That(Assert.CatchAsync<OperationCanceledException>(() => coordinator.RequestRefresh(cancellation.Token)), Is.Not.Null);
            handler.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestCase("registration")]
        [TestCase("coordinator")]
        [TestCase("service")]
        public void VerifyDisposalCancelsActiveAndQueuedRequests(string owner)
        {
            using var coordinator = new ModelRefreshCoordinator();
            var scheduler = new HistoricalScheduler();
            using var service = new ChangeNotificationService(coordinator, scheduler);
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var handler = new Mock<IModelRefreshHandler>(MockBehavior.Strict);
            handler.Setup(x => x.ReloadAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken token) => completion.Task.WaitAsync(token));
            using var registration = coordinator.Register(handler.Object);
            var active = service.RequestRefresh();
            var pending = service.RequestRefresh();

            switch (owner)
            {
                case "registration":
                    registration.Dispose();
                    break;
                case "coordinator":
                    coordinator.Dispose();
                    break;
                default:
                    service.Dispose();
                    break;
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Assert.CatchAsync<OperationCanceledException>(() => active), Is.Not.Null);
                Assert.That(Assert.CatchAsync<OperationCanceledException>(() => pending), Is.Not.Null);
            }
            handler.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Once);
            service.Dispose();
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1));
        }

        [Test]
        public async Task VerifyRegisterDetachesOwnerAndOldRegistrationCannotRemoveReplacement()
        {
            using var coordinator = new ModelRefreshCoordinator();
            var original = new Mock<IModelRefreshHandler>(MockBehavior.Strict);
            var replacement = new Mock<IModelRefreshHandler>(MockBehavior.Strict);
            replacement.Setup(x => x.ReloadAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var registration = coordinator.Register(original.Object);
            Assert.That(() => coordinator.Register(replacement.Object), Throws.InvalidOperationException);
            registration.Dispose();
            Assert.That(Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.RequestRefresh()), Is.Not.Null);
            using var next = coordinator.Register(replacement.Object);
            registration.Dispose();

            await coordinator.RequestRefresh();

            original.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Never);
            replacement.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task VerifyRegisterReplacementWaitsUntilDetachedReloadReturns()
        {
            using var coordinator = new ModelRefreshCoordinator();
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var original = new Mock<IModelRefreshHandler>(MockBehavior.Strict);
            original.Setup(x => x.ReloadAsync(It.IsAny<CancellationToken>())).Returns(completion.Task);
            var registration = coordinator.Register(original.Object);
            var active = coordinator.RequestRefresh();
            registration.Dispose();
            var replacement = new Mock<IModelRefreshHandler>(MockBehavior.Strict);
            replacement.Setup(x => x.ReloadAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            using var next = coordinator.Register(replacement.Object);
            var pending = coordinator.RequestRefresh();
            replacement.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Never);

            completion.SetResult();
            Assert.That(Assert.CatchAsync<OperationCanceledException>(() => active), Is.Not.Null);
            await pending;

            replacement.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void VerifyRequestRefreshWithoutHandlerFailsThroughService()
        {
            using var coordinator = new ModelRefreshCoordinator();
            var scheduler = new HistoricalScheduler();
            using var service = new ChangeNotificationService(coordinator, scheduler);

            Assert.That(Assert.ThrowsAsync<InvalidOperationException>(() => service.RequestRefresh()), Is.Not.Null);
            service.Dispose();
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1));
        }

        [Test]
        public void VerifyDisposeIsIdempotentAndRejectsNewWork()
        {
            using var coordinator = new ModelRefreshCoordinator();
            var handler = new Mock<IModelRefreshHandler>(MockBehavior.Strict);
            Assert.That(() => coordinator.Register(null), Throws.ArgumentNullException);
            using var registration = coordinator.Register(handler.Object);
            coordinator.Dispose();

            Assert.That(coordinator.Dispose, Throws.Nothing);
            Assert.That(registration.Dispose, Throws.Nothing);
            Assert.That(() => coordinator.Register(handler.Object), Throws.TypeOf<ObjectDisposedException>());
            Assert.That(Assert.ThrowsAsync<ObjectDisposedException>(() => coordinator.RequestRefresh()), Is.Not.Null);
            var scheduler = new HistoricalScheduler();
            using var service = new ChangeNotificationService(coordinator, scheduler);
            service.Dispose();
            Assert.That(Assert.ThrowsAsync<ObjectDisposedException>(() => service.RequestRefresh()), Is.Not.Null);
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1));
        }

        [Test]
        public void VerifyDisposeWithoutRegistrationIsIdempotent()
        {
            var coordinator = new ModelRefreshCoordinator();
            coordinator.Dispose();

            Assert.That(coordinator.Dispose, Throws.Nothing);
        }
    }
}
