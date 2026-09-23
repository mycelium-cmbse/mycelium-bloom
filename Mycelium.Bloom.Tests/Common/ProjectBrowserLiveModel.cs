// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserLiveModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging.Abstractions;

    using Moq;

    using Mycelium.Bloom.Core.ChangeNotifications;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Dal;

    /// <summary>Provides canonical SDK containment and a virtual-time notification bus for browser contracts.</summary>
    internal sealed class ProjectBrowserLiveModel : IDisposable
    {
        /// <summary>Stores the SDK containment collection for owned relationships.</summary>
        private static readonly PropertyInfo Relationships = typeof(IElement).Assembly
            .GetType("SysML2.NET.Core.POCO.Root.Elements.IContainedElement", true).GetProperty("OwnedRelationship");

        /// <summary>Stores the SDK containment collection for related elements.</summary>
        private static readonly PropertyInfo RelatedElements = typeof(IElement).Assembly
            .GetType("SysML2.NET.Core.POCO.Root.Elements.IContainedRelationship", true).GetProperty("OwnedRelatedElement");

        /// <summary>Owns refresh coordination for the test bus.</summary>
        private readonly ModelRefreshCoordinator refresh = new();

        /// <summary>Owns created browsers.</summary>
        private readonly List<ProjectBrowserViewModel> browsers = [];

        /// <summary>Tracks active subscriptions at the public service boundary.</summary>
        private readonly Dictionary<ChangeTarget, int> subscriptions = [];

        /// <summary>Tracks subscription creation independently of shared bus subjects.</summary>
        private readonly Dictionary<ChangeTarget, int> creations = [];

        /// <summary>Creates two branches with descendants that are initially unmaterialized.</summary>
        internal ProjectBrowserLiveModel()
        {
            this.Bus = new ChangeNotificationService(this.refresh, this.Scheduler);
            this.Root = this.Add(null, "Root");
            this.Left = this.Add(this.Root, "Left");
            this.Right = this.Add(this.Root, "Right");
            this.Moving = this.Add(this.Left, "Moving");
            this.Deep = this.Add(this.Moving, "Needle");
            this.Add(this.Left, "Left spare");
            this.Add(this.Right, "Right spare");
        }

        /// <summary>Gets the canonical SDK cache.</summary>
        internal Assembler Assembler { get; } = new(NullLoggerFactory.Instance);

        /// <summary>Gets the shared selection authority.</summary>
        internal ContextAwareService Selection { get; } = new();

        /// <summary>Gets the deterministic dispatch scheduler.</summary>
        internal HistoricalScheduler Scheduler { get; } = new();

        /// <summary>Gets the real scoped notification implementation.</summary>
        internal ChangeNotificationService Bus { get; }

        /// <summary>Gets the model root.</summary>
        internal Namespace Root { get; }

        /// <summary>Gets the original parent branch.</summary>
        internal Namespace Left { get; }

        /// <summary>Gets the destination branch.</summary>
        internal Namespace Right { get; }

        /// <summary>Gets the movable nested branch.</summary>
        internal Namespace Moving { get; }

        /// <summary>Gets the deep focus and filter target.</summary>
        internal Namespace Deep { get; }

        /// <summary>Gets active subscriptions for a namespace, including its root anchor when applicable.</summary>
        /// <param name="element">The target namespace.</param>
        /// <returns>The number of subscribers.</returns>
        internal int Subscribers(IElement element) => this.subscriptions.GetValueOrDefault(ChangeTarget.Subtree(element.Id));

        /// <summary>Gets cumulative subscription creation for a namespace.</summary>
        /// <param name="element">The target namespace.</param>
        /// <returns>The number of subscriptions created.</returns>
        internal int Creations(IElement element) => this.creations.GetValueOrDefault(ChangeTarget.Subtree(element.Id));

        /// <summary>Creates an independently owned browser using the shared model and bus.</summary>
        /// <returns>The initialized browser.</returns>
        internal async Task<ProjectBrowserViewModel> CreateBrowser()
        {
            var loader = new Mock<IModelLoaderService>(MockBehavior.Strict);
            loader.Setup(service => service.LoadQuantitiesModel()).Returns(this.Root);
            var notifications = new Mock<IChangeNotificationService>(MockBehavior.Strict);
            notifications.Setup(service => service.Listen(It.IsAny<ChangeTarget>())).Returns((ChangeTarget target) =>
                Observable.Defer(() =>
                {
                    this.subscriptions[target] = this.subscriptions.GetValueOrDefault(target) + 1;
                    this.creations[target] = this.creations.GetValueOrDefault(target) + 1;
                    return this.Bus.Listen(target).Finally(() => this.subscriptions[target]--);
                }));
            var browser = new ProjectBrowserViewModel(loader.Object, this.Selection, notifications.Object, this.Assembler);
            this.browsers.Add(browser);
            Assert.That(await browser.InitializeAsync(CancellationToken.None), Is.True);
            return browser;
        }

        /// <summary>Adds a canonical namespace through SDK containment.</summary>
        /// <param name="parent">The containing element, or null for a root.</param>
        /// <param name="name">The declared name.</param>
        /// <returns>The new namespace.</returns>
        internal Namespace Add(IElement parent, string name)
        {
            var id = Guid.NewGuid();
            var element = new Namespace { Id = id, ElementId = id.ToString(), DeclaredName = name };
            this.Assembler.Cache[id] = new(() => element);
            if (parent != null)
            {
                var membershipId = Guid.NewGuid();
                var membership = new OwningMembership { Id = membershipId, ElementId = membershipId.ToString() };
                ((ICollection<IElement>)RelatedElements.GetValue(membership)).Add(element);
                OwnedRelationships(parent).Add(membership);
                this.Assembler.Cache[membershipId] = new(() => membership);
            }
            return element;
        }

        /// <summary>Moves a canonical element while preserving its ownership relationship.</summary>
        /// <param name="element">The element to move.</param>
        /// <param name="parent">The new owner.</param>
        /// <returns>The captured old containment.</returns>
        internal static NamespacePath Move(IElement element, IElement parent)
        {
            var before = NamespacePath.Capture(element);
            var membership = element.OwningRelationship;
            OwnedRelationships(element.owner).Remove(membership);
            OwnedRelationships(parent).Add(membership);
            return before;
        }

        /// <summary>Removes a canonical element and its membership from the model.</summary>
        /// <param name="element">The element being deleted.</param>
        /// <returns>The captured deletion ancestry.</returns>
        internal NamespacePath Delete(IElement element)
        {
            var before = NamespacePath.Capture(element);
            var membership = element.OwningRelationship;
            OwnedRelationships(element.owner).Remove(membership);
            this.Assembler.Cache.TryRemove(element.Id, out _);
            this.Assembler.Cache.TryRemove(membership.Id, out _);
            return before;
        }

        /// <summary>Publishes a committed change and advances only virtual time.</summary>
        /// <param name="kind">The committed operation.</param>
        /// <param name="element">The affected element.</param>
        /// <param name="previous">The captured old path for moves or deletions.</param>
        internal void Publish(ChangeKind kind, IElement element, NamespacePath previous = null)
        {
            this.Bus.Publish(new ChangeEvent(kind, ChangeSource.Remote, element.Id, element.GetType(),
                kind == ChangeKind.Deleted ? previous : NamespacePath.Capture(element), ["DeclaredName"],
                Guid.NewGuid(), kind == ChangeKind.Moved ? previous : null));
            this.Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(100));
        }

        /// <summary>Enumerates only browser nodes already materialized through public child state.</summary>
        /// <param name="node">The subtree root.</param>
        /// <returns>The existing node identities.</returns>
        internal static IEnumerable<ProjectBrowserNodeViewModel> Nodes(ProjectBrowserNodeViewModel node)
        {
            return new[] { node }.Concat(node.Children.SelectMany(Nodes));
        }

        /// <summary>Finds an already materialized canonical identity.</summary>
        /// <param name="browser">The owning browser.</param>
        /// <param name="element">The source identity.</param>
        /// <returns>The corresponding browser node.</returns>
        internal static ProjectBrowserNodeViewModel Node(ProjectBrowserViewModel browser, IElement element)
        {
            return Nodes(browser.RootNodes[0]).Single(node => node.ElementId == element.ElementId);
        }

        /// <summary>Gets the mutable SDK collection used by containment operations.</summary>
        /// <param name="element">The owner.</param>
        /// <returns>The canonical collection.</returns>
        internal static ICollection<IRelationship> OwnedRelationships(IElement element)
        {
            return (ICollection<IRelationship>)Relationships.GetValue(element);
        }

        /// <summary>Releases every browser before its shared service.</summary>
        public void Dispose()
        {
            foreach (var browser in this.browsers)
            {
                browser.Dispose();
            }
            this.Bus.Dispose();
            this.refresh.Dispose();
        }
    }
}
