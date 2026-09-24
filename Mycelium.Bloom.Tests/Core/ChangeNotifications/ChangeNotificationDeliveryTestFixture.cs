// ------------------------------------------------------------------------------------------------
// <copyright file="ChangeNotificationDeliveryTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Core.ChangeNotifications
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Concurrency;

    using Microsoft.Extensions.Logging;

    using Moq;

    using Mycelium.Bloom.Core.ChangeNotifications;

    using SysML2.NET.Core.POCO.Kernel.Packages;

    /// <summary>Verifies scheduler demand, pending identity retention and isolated subscriber delivery.</summary>
    [TestFixture]
    public sealed class ChangeNotificationDeliveryTestFixture
    {
        /// <summary>Contains the property union expected from compatible transport representations.</summary>
        private static readonly string[] MergedProperties = ["DeclaredName", "DeclaredShortName"];

        /// <summary>Controls queued work and records idle scheduler activity.</summary>
        private CountingScheduler scheduler;

        /// <summary>Receives consumer callback failures.</summary>
        private Mock<ILogger<ChangeNotificationService>> logger;

        /// <summary>Owns the notification subscriptions under test.</summary>
        private ChangeNotificationService service;

        /// <summary>Creates a bus using virtual time and observable logging.</summary>
        [SetUp]
        public void SetUp()
        {
            this.scheduler = new CountingScheduler();
            this.logger = new Mock<ILogger<ChangeNotificationService>>();
            this.service = new ChangeNotificationService(Mock.Of<IModelRefreshCoordinator>(), this.scheduler, this.logger.Object);
        }

        /// <summary>Completes every retained subscription on the virtual scheduler.</summary>
        [TearDown]
        public void TearDown()
        {
            this.service.Dispose();
            this.scheduler.Clock.AdvanceBy(TimeSpan.FromMilliseconds(1));
        }

        /// <summary>Verifies an idle bus schedules nothing and never reads the deduplication clock.</summary>
        [Test]
        public void VerifyIdleServicePerformsNoScheduledOrCleanupWork()
        {
            var received = new List<ChangeEvent>();
            using var subscription = this.service.Listen().Subscribe(received.Add);
            this.scheduler.Clock.AdvanceBy(TimeSpan.FromMinutes(1));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.scheduler.ExecutedActions, Is.Zero);
                Assert.That(this.scheduler.ScheduledActions, Is.Zero);
                Assert.That(this.scheduler.ClockReads, Is.Zero);
                Assert.That(received, Is.Empty);
            }

            this.service.Publish(CreateChange());
            this.Flush();
            Assert.That(received, Has.Count.EqualTo(1));
            var executed = this.scheduler.ExecutedActions;
            var scheduled = this.scheduler.ScheduledActions;
            var clockReads = this.scheduler.ClockReads;
            this.scheduler.Clock.AdvanceBy(TimeSpan.FromMinutes(1));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.scheduler.ExecutedActions, Is.EqualTo(executed));
                Assert.That(this.scheduler.ScheduledActions, Is.EqualTo(scheduled));
                Assert.That(this.scheduler.ClockReads, Is.EqualTo(clockReads));
                Assert.That(received, Has.Count.EqualTo(1));
            }
        }

        /// <summary>Verifies later input cannot extend a batch opened by the first change after an idle interval.</summary>
        [Test]
        public void VerifyPublishStartsBoundedBatchWindow()
        {
            var received = new List<ChangeEvent>();
            using var subscription = this.service.Listen().Subscribe(received.Add);
            this.scheduler.Clock.AdvanceBy(TimeSpan.FromMilliseconds(20));
            var first = CreateChange();
            var second = CreateChange();
            this.service.Publish(first);
            this.scheduler.Clock.AdvanceBy(TimeSpan.FromMilliseconds(50));
            this.service.Publish(second);
            this.scheduler.Clock.AdvanceBy(TimeSpan.FromMilliseconds(25) - TimeSpan.FromTicks(1));
            Assert.That(received, Is.Empty);
            this.scheduler.Clock.AdvanceBy(TimeSpan.FromTicks(1));
            Assert.That(received, Is.EqualTo(new[] { first, second }));
        }

        /// <summary>Verifies pending echoes still merge after scheduler time slips beyond remembered-key expiry.</summary>
        /// <param name="kind">The structural or metadata change kind.</param>
        /// <param name="ingestionStarted">Whether the change has already reached its Rx buffer.</param>
        [TestCase(ChangeKind.Created, false)]
        [TestCase(ChangeKind.Created, true)]
        [TestCase(ChangeKind.Updated, false)]
        [TestCase(ChangeKind.Updated, true)]
        [TestCase(ChangeKind.Deleted, false)]
        [TestCase(ChangeKind.Deleted, true)]
        [TestCase(ChangeKind.Moved, false)]
        [TestCase(ChangeKind.Moved, true)]
        public void VerifyPendingEchoSurvivesSchedulerDelay(ChangeKind kind, bool ingestionStarted)
        {
            var received = new List<ChangeEvent>();
            using var subscription = this.service.Listen().Subscribe(received.Add);
            var change = CreateChange(kind);
            this.service.Publish(change);
            if (ingestionStarted)
            {
                this.scheduler.Clock.AdvanceBy(TimeSpan.FromMilliseconds(1));
            }
            this.scheduler.Clock.Sleep(TimeSpan.FromSeconds(3));
            this.service.Publish(LocalEcho(change));
            Assert.That(received, Is.Empty);
            this.Flush();
            Assert.That(received, Has.Count.EqualTo(1));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(received[0].Source, Is.EqualTo(ChangeSource.Local));
                Assert.That(received[0].ChangedProperties, Is.EquivalentTo(MergedProperties));
                Assert.That(received[0].Kind, Is.EqualTo(kind));
                Assert.That(received[0].Containment, Is.SameAs(change.Containment));
                Assert.That(received[0].PreviousContainment, Is.SameAs(change.PreviousContainment));
            }
            this.service.Publish(change);
            this.Flush();
            Assert.That(received, Has.Count.EqualTo(1));
        }

        /// <summary>Verifies the remembered-key capacity never evicts a queued aggregate or its coalesced metadata.</summary>
        [Test]
        public void VerifyPendingEchoSurvivesCapacityPressure()
        {
            var received = new List<ChangeEvent>();
            using var subscription = this.service.Listen().Subscribe(received.Add);
            var changes = Enumerable.Range(0, 4097).Select(_ => CreateChange()).ToArray();
            foreach (var change in changes)
            {
                this.service.Publish(change);
            }
            this.service.Publish(LocalEcho(changes[0]));
            this.service.Publish(LocalEcho(changes[^1]));
            this.Flush();
            Assert.That(received, Has.Count.EqualTo(changes.Length));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(received.Select(change => (change.CommitId, change.ElementId)), Is.Unique);
                Assert.That(received[0].Source, Is.EqualTo(ChangeSource.Local));
                Assert.That(received[^1].Source, Is.EqualTo(ChangeSource.Local));
                Assert.That(received[0].ChangedProperties, Is.EquivalentTo(MergedProperties));
                Assert.That(received[^1].ChangedProperties, Is.EquivalentTo(MergedProperties));
            }
            this.service.Publish(changes[^1]);
            this.Flush();
            Assert.That(received, Has.Count.EqualTo(changes.Length));
        }

        /// <summary>Verifies an echo cannot replace or expire an aggregate frozen behind a delayed callback.</summary>
        [Test]
        public void VerifyFrozenEchoSurvivesDelayedDelivery()
        {
            var first = CreateChange();
            var delayed = CreateChange();
            var received = new List<ChangeEvent>();
            using var subscription = this.service.Listen().Subscribe(change =>
            {
                received.Add(change);
                if (change == first)
                {
                    this.scheduler.Clock.Sleep(TimeSpan.FromSeconds(3));
                    this.service.Publish(LocalEcho(delayed));
                }
            });
            this.service.Publish(first);
            this.service.Publish(delayed);
            this.Flush();
            Assert.That(received, Is.EqualTo(new[] { first, delayed }));

            this.service.Publish(delayed);
            this.Flush();
            Assert.That(received, Has.Count.EqualTo(2));
        }

        /// <summary>Verifies one failing subscription cannot interrupt peers, other targets or future batches.</summary>
        /// <param name="failingFirst">Whether the failing subscriber precedes its peer in the shared subject.</param>
        [TestCase(true)]
        [TestCase(false)]
        public void VerifyPublishIsolatesThrowingSubscriber(bool failingFirst)
        {
            var received = new List<ChangeEvent>();
            var otherTarget = new List<ChangeEvent>();
            var failure = new InvalidOperationException("Listener failure.");
            using var first = this.service.Listen().Subscribe(change =>
            {
                if (failingFirst)
                {
                    throw failure;
                }
                received.Add(change);
            });
            using var second = this.service.Listen().Subscribe(change =>
            {
                if (!failingFirst)
                {
                    throw failure;
                }
                received.Add(change);
            });
            using var typed = this.service.Listen(ChangeTarget.ForType(typeof(Package))).Subscribe(otherTarget.Add);
            var changes = new[] { CreateChange(), CreateChange(), CreateChange() };
            this.service.Publish(changes[0]);
            this.service.Publish(changes[1]);
            Assert.That(this.Flush, Throws.Nothing);
            this.service.Publish(changes[2]);
            Assert.That(this.Flush, Throws.Nothing);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(received, Is.EqualTo(changes));
                Assert.That(otherTarget, Is.EqualTo(changes));
            }
            this.VerifyLoggedFailure(failure);
        }

        /// <summary>Verifies a failing completion callback cannot prevent peer or other-target completion.</summary>
        [Test]
        public void VerifyDisposeIsolatesThrowingCompletion()
        {
            var failure = new InvalidOperationException("Completion failure.");
            var completed = 0;
            using var failing = this.service.Listen().Subscribe(_ => { }, () => throw failure);
            using var peer = this.service.Listen().Subscribe(_ => { }, () => completed++);
            using var typed = this.service.Listen(ChangeTarget.ForType(typeof(Package))).Subscribe(_ => { }, () => completed++);
            this.service.Dispose();
            Assert.That(this.Flush, Throws.Nothing);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(completed, Is.EqualTo(2));
                Assert.That(this.service.ActiveObservableCount, Is.Zero);
            }
            this.VerifyLoggedFailure(failure);
        }

        /// <summary>Verifies the original listener exception reaches the application logger.</summary>
        /// <param name="failure">The exact failure raised by the subscriber.</param>
        private void VerifyLoggedFailure(Exception failure)
        {
            this.logger.Verify(logger => logger.Log(LogLevel.Error, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString().Contains("subscriber failed")),
                failure, It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        /// <summary>Advances the queued batch by its bounded window.</summary>
        private void Flush() => this.scheduler.Clock.AdvanceBy(TimeSpan.FromMilliseconds(75));

        /// <summary>Creates one independent committed event with captured containment.</summary>
        /// <param name="kind">The committed operation.</param>
        /// <returns>The remote event.</returns>
        private static ChangeEvent CreateChange(ChangeKind kind = ChangeKind.Updated)
        {
            return new ChangeEvent(kind, ChangeSource.Remote, Guid.NewGuid(), typeof(Package),
                new NamespacePath([Guid.NewGuid()]), ["DeclaredName"], Guid.NewGuid(),
                kind == ChangeKind.Moved ? new NamespacePath([Guid.NewGuid()]) : null);
        }

        /// <summary>Adds local origin and complementary properties to the same committed event.</summary>
        /// <param name="change">The pending remote representation.</param>
        /// <returns>The compatible local echo.</returns>
        private static ChangeEvent LocalEcho(ChangeEvent change)
        {
            return new ChangeEvent(change.Kind, ChangeSource.Local, change.ElementId, change.ElementType,
                change.Containment, ["DeclaredShortName"], change.CommitId, change.PreviousContainment);
        }

        /// <summary>Counts work without altering the virtual scheduler's ordering or time.</summary>
        private sealed class CountingScheduler : IScheduler
        {
            /// <summary>Gets the controllable virtual clock and work queue.</summary>
            internal HistoricalScheduler Clock { get; } = new();

            /// <summary>Gets the number of admitted scheduler actions.</summary>
            internal int ScheduledActions { get; private set; }

            /// <summary>Gets the number of actions that have executed.</summary>
            internal int ExecutedActions { get; private set; }

            /// <summary>Gets the number of clock reads by the notification pipeline.</summary>
            internal int ClockReads { get; private set; }

            /// <summary>Gets the instrumented virtual time.</summary>
            public DateTimeOffset Now
            {
                get
                {
                    this.ClockReads++;
                    return this.Clock.Now;
                }
            }

            /// <inheritdoc />
            public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
                => this.Schedule(state, TimeSpan.Zero, action);

            /// <inheritdoc />
            public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
                => this.Schedule(state, dueTime - this.Now, action);

            /// <inheritdoc />
            public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
            {
                this.ScheduledActions++;
                return this.Clock.Schedule(state, dueTime, (_, value) =>
                {
                    this.ExecutedActions++;
                    return action(this, value);
                });
            }
        }
    }
}
