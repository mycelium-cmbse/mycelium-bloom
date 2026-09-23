// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserLiveUpdatesTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.ProjectBrowser
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Moq;

    using Mycelium.Bloom.Core.ChangeNotifications;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using ReactiveUI;

    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;

    using static Mycelium.Bloom.Tests.Common.ProjectBrowserLiveModel;

    /// <summary>Tests lazy browser state and committed-change reconciliation over the loaded SDK model.</summary>
    [TestFixture]
    public sealed class ProjectBrowserLiveUpdatesTestFixture
    {
        /// <summary>Verifies root-only initialization, incremental expansion and cached identity reuse.</summary>
        [Test]
        public async Task VerifyLazyMaterializationCachesOnlyExpandedChildren()
        {
            using var model = new ProjectBrowserLiveModel();
            var browser = await model.CreateBrowser();
            var root = browser.RootNodes[0];
            var left = Node(browser, model.Left);
            var right = Node(browser, model.Right);
            Assert.That(Nodes(root).Count(), Is.EqualTo(5));
            Assert.That(left.Children, Is.Empty);
            Assert.That(left.AreChildrenLoaded, Is.False);
            Assert.That(right.AreChildrenLoaded, Is.False);

            browser.ToggleNode(left);
            var children = left.Children;
            Assert.That(Nodes(root).Count(), Is.EqualTo(9));
            Assert.That(left.AreChildrenLoaded, Is.True);
            Assert.That(Node(browser, model.Moving).Children, Is.Empty);
            Assert.That(right.Children, Is.Empty);
            browser.ToggleNode(left);
            browser.ToggleNode(left);
            Assert.That(left.Children, Is.SameAs(children));
            Assert.That(Nodes(root).Count(), Is.EqualTo(9));
        }

        /// <summary>Verifies flattened related elements and owned relationships occur once each.</summary>
        [Test]
        public async Task VerifyOwnershipCollectionsDoNotDuplicateRows()
        {
            using var model = new ProjectBrowserLiveModel();
            var browser = await model.CreateBrowser();
            var children = browser.RootNodes[0].Children;
            var expected = model.Root.ownedElement.Concat(model.Root.OwnedRelationship).ToArray();
            Assert.That(children.Select(node => node.SourceElement), Is.EquivalentTo(expected));
            Assert.That(children.Select(node => node.ElementId), Is.Unique);
            Assert.That(children.Count(node => node.SourceElement is IRelationship), Is.EqualTo(2));
            var membership = model.Root.OwnedRelationship[0];
            browser.ToggleNode(Node(browser, membership));
            Assert.That(Nodes(browser.RootNodes[0]).Count(node => node.ElementId == model.Left.ElementId), Is.EqualTo(1));
        }

        /// <summary>Verifies identity overlap between both SDK child collections creates only one browser row.</summary>
        [Test]
        public async Task VerifyOwnershipIdentityOverlapIsDeduplicated()
        {
            using var model = new ProjectBrowserLiveModel();
            var relationship = new OwningMembership { Id = Guid.NewGuid(), ElementId = Guid.NewGuid().ToString() };
            var root = new Mock<INamespace>();
            root.SetupGet(element => element.ElementId).Returns(Guid.NewGuid().ToString());
            root.SetupGet(element => element.DeclaredName).Returns("Overlapping containment");
            root.SetupGet(element => element.ownedElement).Returns([relationship]);
            root.SetupGet(element => element.OwnedRelationship).Returns([relationship]);
            var loader = new Mock<IModelLoaderService>();
            loader.Setup(service => service.LoadQuantitiesModel()).Returns(root.Object);
            using var browser = new ProjectBrowserViewModel(loader.Object, model.Selection, model.Bus, model.Assembler);
            Assert.That(await browser.InitializeAsync(CancellationToken.None), Is.True);
            Assert.That(browser.RootNodes[0].Children, Has.Count.EqualTo(1));
            Assert.That(browser.RootNodes[0].Children[0].SourceElement, Is.SameAs(relationship));
        }

        /// <summary>Verifies branch subscriptions follow expansion and remain independent across browsers.</summary>
        [Test]
        public async Task VerifyExpandedSubscriptionsHaveIndependentLifetimes()
        {
            using var model = new ProjectBrowserLiveModel();
            var first = await model.CreateBrowser();
            var second = await model.CreateBrowser();
            var left = Node(first, model.Left);
            Assert.That(model.Subscribers(model.Left), Is.Zero);
            first.ToggleNode(left);
            Assert.That(model.Subscribers(model.Left), Is.EqualTo(1));
            Assert.That(Node(second, model.Left).Children, Is.Empty);
            first.ToggleNode(left);
            Assert.That(model.Subscribers(model.Left), Is.Zero);
            first.ToggleNode(left);
            Assert.That(model.Creations(model.Left), Is.EqualTo(2));
            second.ToggleNode(Node(second, model.Left));
            Assert.That(model.Subscribers(model.Left), Is.EqualTo(2));
            first.Dispose();
            first.Dispose();
            Assert.That(model.Subscribers(model.Left), Is.EqualTo(1));
            second.Dispose();
            Assert.That(model.Subscribers(model.Left), Is.Zero);
            Assert.That(model.Bus.ActiveObservableCount, Is.Zero);
        }

        /// <summary>Verifies creations refresh expanded membership once and defer collapsed caches until expansion.</summary>
        [TestCase(true)]
        [TestCase(false)]
        public async Task VerifyCreatedReconcilesOnlyExpandedParent(bool expanded)
        {
            using var model = new ProjectBrowserLiveModel();
            var browser = await model.CreateBrowser();
            var root = browser.RootNodes[0];
            var left = Node(browser, model.Left);
            var right = Node(browser, model.Right);
            browser.ToggleNode(left);
            var retained = left.Children.ToArray();
            if (!expanded)
            {
                browser.ToggleNode(left);
            }
            var publications = 0;
            using var observed = left.WhenAnyValue(node => node.Children).Skip(1).Subscribe(_ => publications++);
            var created = model.Add(model.Left, "Created");
            model.Publish(ChangeKind.Created, created);
            Assert.That(publications, Is.EqualTo(expanded ? 1 : 0));
            Assert.That(left.Children.Any(node => node.ElementId == created.ElementId), Is.EqualTo(expanded));
            Assert.That(right.Children, Is.Empty);
            Assert.That(browser.RootNodes[0], Is.SameAs(root));
            if (!expanded)
            {
                browser.ToggleNode(left);
            }
            Assert.That(left.Children.Select(node => node.ElementId), Does.Contain(created.ElementId));
            Assert.That(left.Children, Is.SupersetOf(retained));
        }

        /// <summary>Verifies the structural root anchor ignores updates and refreshes membership without loading collapsed children.</summary>
        [Test]
        public async Task VerifyRootAnchorOnlyInvalidatesStructuralMembership()
        {
            using var model = new ProjectBrowserLiveModel();
            var browser = await model.CreateBrowser();
            var root = browser.RootNodes[0];
            browser.ToggleNode(root);
            var state = browser.RenderState;
            model.Left.DeclaredName = "Renamed";
            model.Publish(ChangeKind.Updated, model.Left);
            Assert.That(browser.RenderState, Is.SameAs(state));
            Assert.That(Node(browser, model.Left).DisplayName, Is.EqualTo("Left"));
            var created = model.Add(model.Left, "New child");
            model.Publish(ChangeKind.Created, created);
            model.Publish(ChangeKind.Created, model.Add(model.Left, "Another child"));
            Assert.That(Node(browser, model.Left).Children, Is.Empty);
            Assert.That(Nodes(root).Count(), Is.EqualTo(5));
            browser.ToggleNode(root);
            browser.ToggleNode(Node(browser, model.Left));
            Assert.That(Node(browser, created).SourceElement, Is.SameAs(created));
        }

        /// <summary>Verifies metadata updates preserve children, expansion, selection and immutable prior snapshots.</summary>
        [Test]
        public async Task VerifyUpdatedPreservesTreeState()
        {
            using var model = new ProjectBrowserLiveModel();
            var browser = await model.CreateBrowser();
            var left = Node(browser, model.Left);
            browser.ToggleNode(left);
            browser.SelectNode(left);
            var children = left.Children;
            var before = browser.RenderState;
            model.Left.DeclaredName = "Renamed";
            model.Publish(ChangeKind.Updated, model.Left);
            Assert.That(Node(browser, model.Left), Is.SameAs(left));
            Assert.That(left.Children, Is.SameAs(children));
            Assert.That(left.IsExpanded, Is.True);
            Assert.That(browser.SelectedNode, Is.SameAs(left));
            Assert.That(left.DisplayName, Is.EqualTo("Renamed"));
            Assert.That(before.Roots[0].Children.Single(row => row.Node == left).DisplayName, Is.EqualTo("Left"));
            Assert.That(browser.RenderState.Roots[0].Children.Single(row => row.Node == left).DisplayName, Is.EqualTo("Renamed"));
        }

        /// <summary>Verifies deletion removes cached identities and only clears selections referring to the deleted element.</summary>
        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public async Task VerifyDeletedReconcilesLocalAndSharedSelection(bool localSelected, bool sharedSelected)
        {
            using var model = new ProjectBrowserLiveModel();
            var browser = await model.CreateBrowser();
            browser.ToggleNode(Node(browser, model.Left));
            var deleted = Node(browser, model.Moving);
            var selected = localSelected ? deleted : Node(browser, model.Right);
            browser.SelectNode(selected);
            model.Selection.SelectedElement = sharedSelected ? model.Moving : model.Right;
            model.Publish(ChangeKind.Deleted, model.Moving, model.Delete(model.Moving));
            Assert.That(Nodes(browser.RootNodes[0]), Does.Not.Contain(deleted));
            Assert.That(browser.SelectedNode, localSelected ? Is.Null : Is.SameAs(selected));
            Assert.That(model.Selection.SelectedElement, sharedSelected ? Is.Null : Is.SameAs(model.Right));
            var count = Nodes(browser.RootNodes[0]).Count();
            browser.FocusElement(model.Moving);
            Assert.That(Nodes(browser.RootNodes[0]).Count(), Is.EqualTo(count));
            Assert.That(browser.SelectedNode, Is.Not.SameAs(deleted));
        }

        /// <summary>Verifies each side of a mixed move independently reconciles or defers membership.</summary>
        [TestCase(true)]
        [TestCase(false)]
        public async Task VerifyMovedBetweenExpandedAndCollapsedParents(bool oldExpanded)
        {
            using var model = new ProjectBrowserLiveModel();
            var browser = await model.CreateBrowser();
            var oldParent = Node(browser, model.Left);
            var newParent = Node(browser, model.Right);
            browser.ToggleNode(oldParent);
            browser.ToggleNode(newParent);
            var moving = Node(browser, model.Moving);
            browser.ToggleNode(moving);
            browser.SelectNode(moving);
            browser.ToggleNode(oldExpanded ? newParent : oldParent);
            var expanded = oldExpanded ? oldParent : newParent;
            var collapsed = oldExpanded ? newParent : oldParent;
            var before = Move(model.Moving, model.Right);
            var expected = expanded.SourceElement.ownedElement.Concat(expanded.SourceElement.OwnedRelationship)
                .Select(element => element.ElementId).Distinct().ToArray();
            var reconciliations = 0;
            using var observed = expanded.WhenAnyValue(node => node.Children).Skip(1)
                .Where(children => children.Select(node => node.ElementId).SequenceEqual(expected))
                .Subscribe(_ => reconciliations++);
            model.Publish(ChangeKind.Moved, model.Moving, before);
            Assert.That(reconciliations, Is.EqualTo(1));
            Assert.That(oldParent.Children, Does.Not.Contain(moving));
            Assert.That(newParent.Children.Contains(moving), Is.EqualTo(!oldExpanded));
            Assert.That(moving.IsExpanded, Is.True);
            Assert.That(browser.SelectedNode, Is.SameAs(moving));
            Assert.That(model.Selection.SelectedElement, Is.SameAs(model.Moving));
            Assert.That(model.Subscribers(model.Moving), Is.EqualTo(oldExpanded ? 0 : 1));
            browser.ToggleNode(collapsed);
            Assert.That(newParent.Children.Single(node => node.ElementId == model.Moving.ElementId), Is.SameAs(moving));
            Assert.That(oldParent.Children.Any(node => node.ElementId == model.Moving.ElementId), Is.False);
            Assert.That(model.Subscribers(model.Moving), Is.EqualTo(1));
            Assert.That(Nodes(browser.RootNodes[0]).Select(node => node.Id), Is.Unique);
        }

        /// <summary>Verifies filtering builds only matching paths and never owns durable expansion subscriptions.</summary>
        [TestCase(true)]
        [TestCase(false)]
        public async Task VerifyFilterMaterializesOnlyMatchingPaths(bool rootExpanded)
        {
            using var model = new ProjectBrowserLiveModel();
            var browser = await model.CreateBrowser();
            var root = browser.RootNodes[0];
            if (!rootExpanded)
            {
                browser.ToggleNode(root);
            }
            Assert.That(Nodes(root).Count(), Is.EqualTo(5));
            for (var cycle = 0; cycle < 3; cycle++)
            {
                browser.FilterText = "Needle";
                Assert.That(Nodes(root).Count(), Is.EqualTo(7));
                Assert.That(browser.FilterPresentation.IsVisible(Node(browser, model.Deep)), Is.True);
                Assert.That(Node(browser, model.Right).Children, Is.Empty);
                Assert.That(Node(browser, model.Left).IsExpanded, Is.False);
                Assert.That(model.Subscribers(model.Left), Is.Zero);
                Assert.That(model.Subscribers(model.Moving), Is.Zero);
                Assert.That(model.Subscribers(model.Root), Is.EqualTo(rootExpanded ? 2 : 1));
                browser.ClearFilter();
                Assert.That(root.IsExpanded, Is.EqualTo(rootExpanded));
                Assert.That(Nodes(root).Count(), Is.EqualTo(7));
            }
        }

        /// <summary>Verifies focus creates only missing path nodes while retaining identities and shared selection.</summary>
        [Test]
        public async Task VerifyFocusElementMaterializesOnlyMissingPath()
        {
            using var model = new ProjectBrowserLiveModel();
            var browser = await model.CreateBrowser();
            var root = browser.RootNodes[0];
            var retained = Nodes(root).ToArray();
            model.Selection.SelectedElement = model.Right;
            browser.FocusElement(model.Deep);
            Assert.That(Nodes(root).Count(), Is.EqualTo(7));
            Assert.That(Nodes(root), Is.SupersetOf(retained));
            Assert.That(browser.SelectedNode.SourceElement, Is.SameAs(model.Deep));
            Assert.That(Node(browser, model.Left).IsExpanded, Is.True);
            Assert.That(Node(browser, model.Moving).IsExpanded, Is.True);
            Assert.That(Node(browser, model.Right).Children, Is.Empty);
            Assert.That(model.Selection.SelectedElement, Is.SameAs(model.Right));
        }

        /// <summary>Verifies unresolved ownership never falls back to constructing the whole tree.</summary>
        [TestCase(false)]
        [TestCase(true)]
        public async Task VerifyFocusElementRejectsDetachedOrCyclicOwnership(bool cyclic)
        {
            using var model = new ProjectBrowserLiveModel();
            var browser = await model.CreateBrowser();
            var detached = model.Add(null, "Detached");
            var target = model.Add(detached, "Target");
            if (cyclic)
            {
                var child = model.Add(target, "Cycle");
                Move(target, child);
            }
            var selected = browser.SelectedNode;
            browser.FocusElement(target);
            Assert.That(Nodes(browser.RootNodes[0]).Count(), Is.EqualTo(5));
            Assert.That(browser.SelectedNode, Is.SameAs(selected));
        }

        /// <summary>Verifies notifications delivered during synchronous materialization are serialized by the owner.</summary>
        [Test]
        public async Task VerifyNotificationDuringMaterializationAndSubsequentCollapse()
        {
            using var model = new ProjectBrowserLiveModel();
            var browser = await model.CreateBrowser();
            var left = Node(browser, model.Left);
            using var entered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            using var observed = left.WhenAnyValue(node => node.Children).Skip(1).Take(1).Subscribe(_ =>
            {
                entered.Set();
                Assert.That(release.Wait(TimeSpan.FromSeconds(10)), Is.True);
            });
            var expansion = Task.Run(() => browser.ToggleNode(left));
            Task notification;
            try
            {
                Assert.That(entered.Wait(TimeSpan.FromSeconds(10)), Is.True);
                model.Left.DeclaredName = "Concurrent rename";
                notification = Task.Run(() => model.Publish(ChangeKind.Updated, model.Left));
            }
            finally
            {
                release.Set();
            }
            await Task.WhenAll(expansion, notification).WaitAsync(TimeSpan.FromSeconds(10));
            Assert.That(left.DisplayName, Is.EqualTo("Concurrent rename"));
            Assert.That(left.Children, Has.Count.EqualTo(4));
            var added = model.Add(model.Left, "After expansion");
            model.Publish(ChangeKind.Created, added);
            Assert.That(Node(browser, added).SourceElement, Is.SameAs(added));
            browser.ToggleNode(left);
            Assert.That(left.IsExpanded, Is.False);
            Assert.That(model.Subscribers(model.Left), Is.Zero);
        }

        /// <summary>Verifies disposed browsers ignore notifications without affecting another owner's subscriptions.</summary>
        [Test]
        public async Task VerifyDisposeStopsNotifications()
        {
            using var model = new ProjectBrowserLiveModel();
            var browser = await model.CreateBrowser();
            var retained = Node(browser, model.Left);
            var state = browser.RenderState;
            browser.Dispose();
            browser.Dispose();
            model.Left.DeclaredName = "After disposal";
            model.Publish(ChangeKind.Updated, model.Left);
            model.Publish(ChangeKind.Created, model.Add(model.Left, "After disposal child"));
            Assert.That(retained.DisplayName, Is.EqualTo("Left"));
            Assert.That(browser.RenderState, Is.SameAs(state));
            Assert.That(model.Bus.ActiveObservableCount, Is.Zero);
        }

        /// <summary>Verifies structural cleanup cannot leave a deleted filter-only row in an immutable presentation.</summary>
        [Test]
        public async Task VerifyDeletedFilterOnlyNodeDisappearsWithoutExpandedSubscriptions()
        {
            using var model = new ProjectBrowserLiveModel();
            var browser = await model.CreateBrowser();
            browser.ToggleNode(browser.RootNodes[0]);
            browser.FilterText = "Needle";
            var deleted = Node(browser, model.Deep);
            browser.SelectNode(deleted);
            model.Publish(ChangeKind.Deleted, model.Deep, model.Delete(model.Deep));
            Assert.That(Nodes(browser.RootNodes[0]), Does.Not.Contain(deleted));
            Assert.That(browser.SelectedNode, Is.Null);
            Assert.That(model.Selection.SelectedElement, Is.Null);
            Assert.That(browser.RenderState.Roots, Is.Empty);
            Assert.That(model.Subscribers(model.Root), Is.EqualTo(1));
        }

        /// <summary>Verifies filtering defers full expanded-cache reconciliation until criteria are cleared.</summary>
        [Test]
        public async Task VerifyStructuralInvalidationWhileFilteringReconcilesOnClear()
        {
            using var model = new ProjectBrowserLiveModel();
            var browser = await model.CreateBrowser();
            var left = Node(browser, model.Left);
            browser.ToggleNode(left);
            var retained = left.Children.ToArray();
            browser.FilterText = "Needle";
            var added = model.Add(model.Left, "New sibling");
            model.Publish(ChangeKind.Created, added);
            Assert.That(left.Children.Any(node => node.ElementId == added.ElementId), Is.False);
            browser.ClearFilter();
            Assert.That(left.Children, Is.SupersetOf(retained));
            Assert.That(Node(browser, added).SourceElement, Is.SameAs(added));
            Assert.That(left.IsExpanded, Is.True);
            Assert.That(model.Subscribers(model.Left), Is.EqualTo(1));
        }
    }
}
