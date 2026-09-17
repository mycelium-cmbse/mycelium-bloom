// ------------------------------------------------------------------------------------------------
// <copyright file="ChangeNotificationServiceTestFixture.cs" company="Starion Group S.A.">
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
    using System.Threading.Tasks;

    using Moq;

    using Mycelium.Bloom.Core.ChangeNotifications;

    using SysML2.NET.Core.POCO.Kernel.Packages;
    using SysML2.NET.Core.POCO.Root.Annotations;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;

    [TestFixture]
    public sealed class ChangeNotificationServiceTestFixture
    {
        private static readonly string[] ExpectedProperties = ["DeclaredName", "DeclaredShortName"];
        private HistoricalScheduler scheduler;
        private ChangeNotificationService service;

        [SetUp]
        public void SetUp()
        {
            this.scheduler = new HistoricalScheduler();
            this.service = new ChangeNotificationService(new Mock<IModelRefreshCoordinator>(MockBehavior.Strict).Object, this.scheduler);
        }

        [TearDown]
        public void TearDown()
        {
            this.service.Dispose();
            this.scheduler.AdvanceBy(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void VerifyListenRoutesGlobalAndElementTargets()
        {
            var first = CreateChange();
            var other = CreateChange();
            var all = new List<ChangeEvent>();
            var selected = new List<ChangeEvent>();
            using var global = this.service.Listen().Subscribe(all.Add);
            using var targeted = this.service.Listen(ChangeTarget.Element(first.ElementId)).Subscribe(selected.Add);

            this.service.Publish(first);
            this.service.Publish(other);
            this.Flush();

            Assert.That(all, Is.EqualTo(new[] { first, other }));
            Assert.That(selected, Is.EqualTo(new[] { first }));
        }

        [TestCase(typeof(Package), true)]
        [TestCase(typeof(IPackage), true)]
        [TestCase(typeof(Namespace), true)]
        [TestCase(typeof(INamespace), true)]
        [TestCase(typeof(IElement), true)]
        [TestCase(typeof(Documentation), false)]
        [TestCase(typeof(IDocumentation), false)]
        public void VerifyListenRoutesMetaclassHierarchy(Type targetType, bool matches)
        {
            var received = new List<ChangeEvent>();
            var change = CreateChange();
            using var subscription = this.service.Listen(ChangeTarget.ForType(targetType)).Subscribe(received.Add);

            this.service.Publish(change);
            this.Flush();

            Assert.That(received, matches ? Is.EqualTo(new[] { change }) : Is.Empty);
        }

        [Test]
        public void VerifyListenRoutesSubtreeSelfChildrenAndDescendants()
        {
            var root = Guid.NewGuid();
            var child = Guid.NewGuid();
            var changes = new[]
            {
                CreateChange(elementId: root),
                CreateChange(elementId: child, containment: new NamespacePath([root])),
                CreateChange(containment: new NamespacePath([child, root]))
            };
            var received = new List<ChangeEvent>();
            using var subscription = this.service.Listen(ChangeTarget.Subtree(root)).Subscribe(received.Add);

            foreach (var change in changes)
            {
                this.service.Publish(change);
            }

            this.service.Publish(CreateChange(containment: new NamespacePath([Guid.NewGuid()])));
            this.Flush();

            Assert.That(received, Is.EqualTo(changes));
        }

        [Test]
        public void VerifyListenRoutesMovesToBothPathsOncePerTarget()
        {
            var oldParent = Guid.NewGuid();
            var newParent = Guid.NewGuid();
            var root = Guid.NewGuid();
            var oldEvents = new List<ChangeEvent>();
            var newEvents = new List<ChangeEvent>();
            var rootEvents = new List<ChangeEvent>();
            var unrelatedEvents = new List<ChangeEvent>();
            using var oldSubscription = this.service.Listen(ChangeTarget.Subtree(oldParent)).Subscribe(oldEvents.Add);
            using var newSubscription = this.service.Listen(ChangeTarget.Subtree(newParent)).Subscribe(newEvents.Add);
            using var rootSubscription = this.service.Listen(ChangeTarget.Subtree(root)).Subscribe(rootEvents.Add);
            using var unrelated = this.service.Listen(ChangeTarget.Subtree(Guid.NewGuid())).Subscribe(unrelatedEvents.Add);
            var change = CreateChange(ChangeKind.Moved, containment: new NamespacePath([newParent, root]),
                previousContainment: new NamespacePath([oldParent, root]));

            this.service.Publish(change);
            this.Flush();

            Assert.That(oldEvents, Is.EqualTo(new[] { change }));
            Assert.That(newEvents, Is.EqualTo(new[] { change }));
            Assert.That(rootEvents, Is.EqualTo(new[] { change }));
            Assert.That(unrelatedEvents, Is.Empty);
        }

        [Test]
        public void VerifyListenRoutesDeletionFromCapturedContainment()
        {
            var root = new Mock<INamespace>(MockBehavior.Strict);
            var child = new Mock<IElement>(MockBehavior.Strict);
            var rootId = Guid.NewGuid();
            root.SetupGet(x => x.Id).Returns(rootId);
            root.SetupGet(x => x.owner).Returns((IElement)null);
            child.SetupGet(x => x.owner).Returns(root.Object);
            var path = NamespacePath.Capture(child.Object);
            child.SetupGet(x => x.owner).Throws(new InvalidOperationException("Element has been removed."));
            root.SetupGet(x => x.owner).Throws(new InvalidOperationException("Model has been replaced."));
            var received = new List<ChangeEvent>();
            var deletion = CreateChange(ChangeKind.Deleted, containment: path);
            using var subscription = this.service.Listen(ChangeTarget.Subtree(rootId)).Subscribe(received.Add);

            this.service.Publish(deletion);
            this.Flush();

            Assert.That(received, Is.EqualTo(new[] { deletion }));
        }

        [TestCase(ChangeKind.Created, false)]
        [TestCase(ChangeKind.Created, true)]
        [TestCase(ChangeKind.Updated, false)]
        [TestCase(ChangeKind.Updated, true)]
        [TestCase(ChangeKind.Deleted, false)]
        [TestCase(ChangeKind.Deleted, true)]
        [TestCase(ChangeKind.Moved, false)]
        [TestCase(ChangeKind.Moved, true)]
        public void VerifyPublishMergesCompatibleEchoesIndependentlyOfOrder(ChangeKind kind, bool localFirst)
        {
            var id = Guid.NewGuid();
            var commit = Guid.NewGuid();
            var path = new NamespacePath([Guid.NewGuid()]);
            var previous = kind == ChangeKind.Moved ? new NamespacePath([Guid.NewGuid()]) : null;
            var local = CreateChange(kind, ChangeSource.Local, id, commit, ["DeclaredName"], path, previous);
            var remote = new ChangeEvent(kind, ChangeSource.Remote, id, typeof(IPackage), path,
                ["DeclaredShortName"], commit, previous);
            var received = new List<ChangeEvent>();
            using var subscription = this.service.Listen().Subscribe(received.Add);

            this.service.Publish(localFirst ? local : remote);
            this.scheduler.AdvanceBy(TimeSpan.FromMilliseconds(25));
            this.service.Publish(localFirst ? remote : local);
            this.Flush();
            this.service.Publish(remote);
            this.Flush();

            Assert.That(received, Has.Count.EqualTo(1));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(received[0].Source, Is.EqualTo(ChangeSource.Local));
                Assert.That(received[0].Kind, Is.EqualTo(kind));
                Assert.That(received[0].ChangedProperties, Is.EquivalentTo(ExpectedProperties));
                Assert.That(received[0].CommitId, Is.EqualTo(commit));
                Assert.That(received[0].ElementType, Is.EqualTo(typeof(IPackage)));
                Assert.That(received[0].Containment.NamespaceIds, Is.EqualTo(path.NamespaceIds));
                Assert.That(received[0].ParentNamespaceId, Is.EqualTo(path.ParentNamespaceId));
                Assert.That(received[0].PreviousContainment, Is.SameAs(previous));
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void VerifyPublishPreservesUnspecifiedProperties(bool unspecifiedFirst)
        {
            var unspecified = CreateChange(properties: []);
            var specified = CreateChange(elementId: unspecified.ElementId, commitId: unspecified.CommitId, properties: ["DeclaredName"]);
            var received = new List<ChangeEvent>();
            using var subscription = this.service.Listen().Subscribe(received.Add);

            this.service.Publish(unspecifiedFirst ? unspecified : specified);
            this.service.Publish(unspecifiedFirst ? specified : unspecified);
            this.Flush();

            Assert.That(received, Has.Count.EqualTo(1));
            Assert.That(received[0].ChangedProperties, Is.Empty);
        }

        [Test]
        public void VerifyPublishPreservesDistinctCommitsAndStructuralChanges()
        {
            var id = Guid.NewGuid();
            var changes = Enum.GetValues<ChangeKind>().Select(kind => CreateChange(kind, elementId: id,
                previousContainment: kind == ChangeKind.Moved ? new NamespacePath([]) : null)).ToArray();
            var received = new List<ChangeEvent>();
            using var subscription = this.service.Listen().Subscribe(received.Add);

            foreach (var change in changes)
            {
                this.service.Publish(change);
            }

            this.service.Publish(CreateChange(elementId: id));
            this.Flush();

            Assert.That(received.Take(changes.Length), Is.EqualTo(changes));
            Assert.That(received, Has.Count.EqualTo(5));
            Assert.That(received.Select(change => change.CommitId), Is.Unique);
        }

        [TestCase(ChangeKind.Created)]
        [TestCase(ChangeKind.Deleted)]
        [TestCase(ChangeKind.Moved)]
        public void VerifyPublishRejectsIncompatibleKinds(ChangeKind incompatibleKind)
        {
            var original = CreateChange();
            var incompatible = CreateChange(incompatibleKind, elementId: original.ElementId, commitId: original.CommitId,
                previousContainment: incompatibleKind == ChangeKind.Moved ? new NamespacePath([]) : null);
            var received = new List<ChangeEvent>();
            using var subscription = this.service.Listen().Subscribe(received.Add);
            this.service.Publish(original);

            Assert.That(() => this.service.Publish(incompatible), Throws.ArgumentException);
            this.Flush();

            Assert.That(received, Is.EqualTo(new[] { original }));
        }

        [TestCase("type")]
        [TestCase("containment")]
        [TestCase("previousContainment")]
        public void VerifyPublishRejectsIncompatibleDuplicateContext(string difference)
        {
            var original = CreateChange(ChangeKind.Moved, previousContainment: new NamespacePath([]));
            var incompatible = new ChangeEvent(original.Kind, original.Source, original.ElementId,
                difference == "type" ? typeof(Documentation) : original.ElementType,
                difference == "containment" ? new NamespacePath([Guid.NewGuid()]) : original.Containment,
                [], original.CommitId,
                difference == "previousContainment" ? new NamespacePath([Guid.NewGuid()]) : original.PreviousContainment);
            this.service.Publish(original);

            Assert.That(() => this.service.Publish(incompatible), Throws.ArgumentException);
        }

        [Test]
        public void VerifyPublishWaitsForBatchWindow()
        {
            var received = new List<ChangeEvent>();
            using var subscription = this.service.Listen().Subscribe(received.Add);
            this.service.Publish(CreateChange());
            Assert.That(received, Is.Empty);

            this.scheduler.AdvanceBy(TimeSpan.FromMilliseconds(75) - TimeSpan.FromTicks(1));
            Assert.That(received, Is.Empty);
            this.scheduler.AdvanceBy(TimeSpan.FromTicks(1));
            Assert.That(received, Has.Count.EqualTo(1));
        }

        [Test]
        public void VerifyPublishExpiresDuplicatesWithoutExtendingTheirWindow()
        {
            var change = CreateChange();
            var received = new List<ChangeEvent>();
            using var subscription = this.service.Listen().Subscribe(received.Add);
            this.service.Publish(change);
            this.scheduler.AdvanceBy(TimeSpan.FromSeconds(1));
            this.service.Publish(change);
            this.scheduler.AdvanceBy(TimeSpan.FromSeconds(1));
            this.service.Publish(change);
            this.service.Publish(CreateChange(elementId: change.ElementId));
            this.Flush();

            Assert.That(received, Has.Count.EqualTo(3));
            Assert.That(received[1], Is.SameAs(change));
            Assert.That(received[2].CommitId, Is.Not.EqualTo(change.CommitId));
        }

        [Test]
        public void VerifyPublishEvictsOldestDuplicateAtCapacity()
        {
            var changes = Enumerable.Range(0, 4097).Select(_ => CreateChange()).ToArray();
            var count = 0;
            using var subscription = this.service.Listen().Subscribe(_ => count++);

            foreach (var change in changes)
            {
                this.service.Publish(change);
            }

            this.Flush();
            this.service.Publish(changes[^1]);
            this.service.Publish(changes[0]);
            this.Flush();

            Assert.That(count, Is.EqualTo(4098));
            this.scheduler.AdvanceBy(TimeSpan.FromSeconds(3));
            this.service.Publish(changes[^1]);
            this.Flush();
            Assert.That(count, Is.EqualTo(4099));
        }

        [Test]
        public void VerifyListenSharesTargetsAndReattachesSavedObservable()
        {
            var stream = this.service.Listen(ChangeTarget.ForType(typeof(Namespace)));
            Assert.That(this.service.ActiveObservableCount, Is.Zero);
            var firstEvents = new List<ChangeEvent>();
            var secondEvents = new List<ChangeEvent>();
            var first = stream.Subscribe(firstEvents.Add);
            var peer = this.service.Listen(ChangeTarget.ForType(typeof(INamespace))).Subscribe(secondEvents.Add);
            Assert.That(this.service.ActiveObservableCount, Is.EqualTo(1));

            first.Dispose();
            Assert.That(this.service.ActiveObservableCount, Is.EqualTo(1));
            this.service.Publish(CreateChange());
            this.Flush();
            Assert.That(firstEvents, Is.Empty);
            Assert.That(secondEvents, Has.Count.EqualTo(1));
            peer.Dispose();
            Assert.That(this.service.ActiveObservableCount, Is.Zero);

            using var second = stream.Subscribe(firstEvents.Add);
            this.service.Publish(CreateChange());
            this.Flush();
            Assert.That(firstEvents, Has.Count.EqualTo(1));
            Assert.That(this.service.ActiveObservableCount, Is.EqualTo(1));
        }

        [Test]
        public async Task VerifyListenConcurrentChurnPreservesCanonicalTarget()
        {
            var stream = this.service.Listen();
            var received = new List<ChangeEvent>();
            using (stream.Subscribe(received.Add))
            {
                await Task.WhenAll(Enumerable.Range(0, 64).Select(_ => Task.Run(() =>
                {
                    using var subscription = stream.Subscribe(_ => { });
                    this.service.Publish(CreateChange());
                })));
                Assert.That(this.service.ActiveObservableCount, Is.EqualTo(1));
                this.Flush();
                Assert.That(received, Has.Count.EqualTo(64));
            }

            Assert.That(this.service.ActiveObservableCount, Is.Zero);
        }

        [Test]
        public void VerifyIsEnabledDiscardsPendingAndDisabledChangesWithoutDetachingListeners()
        {
            var change = CreateChange();
            var received = new List<ChangeEvent>();
            using var subscription = this.service.Listen().Subscribe(received.Add);
            this.service.Publish(change);
            this.service.IsEnabled = false;
            this.service.IsEnabled = false;
            this.service.Publish(CreateChange());
            this.service.IsEnabled = true;
            this.service.IsEnabled = true;
            this.Flush();

            Assert.That(received, Is.Empty);
            Assert.That(this.service.ActiveObservableCount, Is.EqualTo(1));
            this.service.Publish(change);
            this.Flush();
            Assert.That(received, Is.EqualTo(new[] { change }));
        }

        [Test]
        public void VerifyDisposeDropsPendingWorkAndCompletesSavedStreams()
        {
            var stream = this.service.Listen();
            var received = new List<ChangeEvent>();
            var completed = false;
            using var subscription = stream.Subscribe(received.Add, () => completed = true);
            this.service.Publish(CreateChange());

            this.service.Dispose();
            Assert.That(this.service.Dispose, Throws.Nothing);
            Assert.That(this.service.ActiveObservableCount, Is.Zero);
            Assert.That(this.service.IsEnabled, Is.False);
            Assert.That(completed, Is.False);
            this.Flush();
            Assert.That(received, Is.Empty);
            Assert.That(completed, Is.True);
            Assert.That(() => this.service.Publish(CreateChange()), Throws.TypeOf<ObjectDisposedException>());
            Assert.That(() => this.service.Listen(), Throws.TypeOf<ObjectDisposedException>());
            Assert.That(() => this.service.IsEnabled = true, Throws.TypeOf<ObjectDisposedException>());
            Exception error = null;
            using var lateSubscription = stream.Subscribe(_ => { }, exception => error = exception);
            Assert.That(error, Is.TypeOf<ObjectDisposedException>());
        }

        [Test]
        public void VerifyListenAllowsCallbackReentrancyAndDisposal()
        {
            var stream = this.service.Listen();
            IDisposable first = null;
            IDisposable replacement = null;
            var received = new List<ChangeEvent>();
            first = stream.Subscribe(_ =>
            {
                first.Dispose();
                replacement = stream.Subscribe(received.Add);
                this.service.Publish(CreateChange());
            });

            try
            {
                this.service.Publish(CreateChange());
                this.Flush();
                this.Flush();
                Assert.That(received, Has.Count.EqualTo(1));
                Assert.That(this.service.ActiveObservableCount, Is.EqualTo(1));
            }
            finally
            {
                first.Dispose();
                replacement?.Dispose();
            }

            Assert.That(this.service.ActiveObservableCount, Is.Zero);
        }

        [Test]
        public void VerifyDisposeFromCallbackStopsLaterEventsAndAllowsPeerDisposal()
        {
            var received = 0;
            var completed = 0;
            IDisposable peer = null;
            using var subscription = this.service.Listen().Subscribe(_ =>
            {
                received++;
                this.service.Dispose();
            }, () =>
            {
                completed++;
                peer.Dispose();
            });
            peer = this.service.Listen(ChangeTarget.ForType(typeof(Package))).Subscribe(_ => { });
            this.service.Publish(CreateChange());
            this.service.Publish(CreateChange());
            this.Flush();

            Assert.That(received, Is.EqualTo(1));
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(this.service.ActiveObservableCount, Is.Zero);
        }

        [Test]
        public void VerifyConstructorAndPublishRejectInvalidBoundaries()
        {
            var coordinator = new Mock<IModelRefreshCoordinator>(MockBehavior.Strict).Object;
            Assert.That(() => new ChangeNotificationService(null, this.scheduler), Throws.ArgumentNullException);
            Assert.That(() => new ChangeNotificationService(coordinator, ImmediateScheduler.Instance), Throws.ArgumentException);
            Assert.That(() => new ChangeNotificationService(coordinator, CurrentThreadScheduler.Instance), Throws.ArgumentException);
            Assert.That(() => this.service.Publish(null), Throws.ArgumentNullException);
        }

        private void Flush()
        {
            this.scheduler.AdvanceBy(TimeSpan.FromMilliseconds(75));
        }

        private static ChangeEvent CreateChange(ChangeKind kind = ChangeKind.Updated, ChangeSource source = ChangeSource.Local,
            Guid? elementId = null, Guid? commitId = null, IEnumerable<string> properties = null,
            NamespacePath containment = null, NamespacePath previousContainment = null)
        {
            return new ChangeEvent(kind, source, elementId ?? Guid.NewGuid(), typeof(Package), containment ?? new NamespacePath([]),
                properties ?? ["DeclaredName"], commitId ?? Guid.NewGuid(), previousContainment);
        }
    }
}
