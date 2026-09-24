// ------------------------------------------------------------------------------------------------
// <copyright file="ChangeNotificationConcurrencyTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Core.ChangeNotifications
{
    using System;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Threading;
    using System.Threading.Tasks;

    using Moq;

    using Mycelium.Bloom.Core.ChangeNotifications;

    using SysML2.NET.Core.POCO.Kernel.Packages;

    [TestFixture]
    public sealed class ChangeNotificationConcurrencyTestFixture
    {
        [Test]
        public async Task VerifyPublishEntersSchedulerOutsideComponentLocks()
        {
            var timeout = TimeSpan.FromSeconds(5);
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var release = new ManualResetEventSlim();
            var clock = new HistoricalScheduler();
            var scheduler = new ScheduleCallbackScheduler(clock, () =>
            {
                entered.TrySetResult();
                if (!release.Wait(timeout))
                {
                    throw new TimeoutException("Scheduling did not leave the admission boundary.");
                }
            });
            using (var service = new ChangeNotificationService(Mock.Of<IModelRefreshCoordinator>(), scheduler))
            {
                var publication = Task.Run(() => service.Publish(CreateChange()));
                try
                {
                    await entered.Task.WaitAsync(timeout);
                    await Task.Run(() =>
                    {
                        service.IsEnabled = false;
                        using var subscription = service.Listen().Subscribe(_ => { });
                    }).WaitAsync(timeout);
                }
                finally
                {
                    release.Set();
                }
                await publication.WaitAsync(timeout);
                clock.AdvanceBy(TimeSpan.FromMilliseconds(75));
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(service.IsEnabled, Is.False);
                    Assert.That(service.ActiveObservableCount, Is.Zero);
                }
            }
            clock.AdvanceBy(TimeSpan.FromMilliseconds(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task VerifyPublishSerializesCallbacksOutsideStateGate(bool disposeDuringCallback)
        {
            var timeout = TimeSpan.FromSeconds(10);
            using var service = new ChangeNotificationService(new Mock<IModelRefreshCoordinator>(MockBehavior.Strict).Object);
            using var barrier = new Barrier(2);
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var notifications = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var activeCallbacks = 0;
            var overlappingCallbacks = 0;
            var received = 0;
            using var subscription = service.Listen().Subscribe(_ =>
            {
                if (Interlocked.Increment(ref activeCallbacks) != 1)
                {
                    Interlocked.Increment(ref overlappingCallbacks);
                }

                try
                {
                    if (Interlocked.Increment(ref received) == 1)
                    {
                        if (!barrier.SignalAndWait(timeout) || !barrier.SignalAndWait(timeout))
                        {
                            notifications.TrySetException(new TimeoutException("Concurrent bus operation did not complete during callback."));
                        }
                    }

                    using var reentrant = service.IsEnabled ? service.Listen().Subscribe(_ => { }) : null;
                    if (Volatile.Read(ref received) == 33)
                    {
                        notifications.TrySetResult();
                    }
                }
                catch (Exception exception)
                {
                    notifications.TrySetException(exception);
                }
                finally
                {
                    Interlocked.Decrement(ref activeCallbacks);
                }
            }, () =>
            {
                if (Interlocked.Increment(ref activeCallbacks) != 1)
                {
                    Interlocked.Increment(ref overlappingCallbacks);
                }

                Interlocked.Decrement(ref activeCallbacks);
                completion.TrySetResult();
            });

            service.Publish(CreateChange());
            var callbackEntered = await Task.Run(() => barrier.SignalAndWait(timeout));
            Assert.That(callbackEntered, Is.True);

            try
            {
                await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() =>
                {
                    using var listener = service.Listen(ChangeTarget.ForType(typeof(Package))).Subscribe(_ => { });
                    service.Publish(CreateChange());
                }))).WaitAsync(timeout);

                if (disposeDuringCallback)
                {
                    service.Dispose();
                    Assert.That(completion.Task.IsCompleted, Is.False);
                }
            }
            finally
            {
                Assert.That(await Task.Run(() => barrier.SignalAndWait(timeout)), Is.True);
            }

            if (!disposeDuringCallback)
            {
                await notifications.Task.WaitAsync(timeout);
                service.Dispose();
            }

            await completion.Task.WaitAsync(timeout);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(overlappingCallbacks, Is.Zero);
                Assert.That(received, Is.EqualTo(disposeDuringCallback ? 1 : 33));
                Assert.That(notifications.Task.IsFaulted, Is.False);
                Assert.That(service.ActiveObservableCount, Is.Zero);
            }
        }

        private static ChangeEvent CreateChange()
        {
            return new ChangeEvent(ChangeKind.Updated, ChangeSource.Local, Guid.NewGuid(), typeof(Package),
                new NamespacePath([]), [], Guid.NewGuid());
        }

        /// <summary>Exposes the caller's scheduler-entry boundary while retaining queued virtual execution.</summary>
        /// <param name="scheduler">The virtual execution scheduler.</param>
        /// <param name="onSchedule">The synchronous scheduling diagnostic.</param>
        private sealed class ScheduleCallbackScheduler(IScheduler scheduler, Action onSchedule) : IScheduler
        {
            /// <inheritdoc />
            public DateTimeOffset Now => scheduler.Now;

            /// <inheritdoc />
            public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
                => this.Schedule(state, TimeSpan.Zero, action);

            /// <inheritdoc />
            public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
                => this.Schedule(state, dueTime - this.Now, action);

            /// <inheritdoc />
            public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
            {
                onSchedule();
                return scheduler.Schedule(state, dueTime, action);
            }
        }
    }
}
