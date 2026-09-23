// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserViewModel.Live.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.ProjectBrowser
{
    using System.Collections.Immutable;
    using System.Globalization;
    using System.Reactive;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;

    using DynamicData;

    using Mycelium.Bloom.Core.ChangeNotifications;

    using ReactiveUI;

    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Dal;

    public sealed partial class ProjectBrowserViewModel
    {
        /// <summary>Serializes browser mutations from UI callers and notification delivery.</summary>
        private readonly object stateGate = new();

        /// <summary>Provides transport-independent committed changes.</summary>
        private readonly IChangeNotificationService changeNotificationService;

        /// <summary>Provides canonical SDK identities already loaded by the model owner.</summary>
        private readonly IAssembler assembler;

        /// <summary>Owns the active namespace subscriptions through DynamicData membership.</summary>
        private readonly SourceCache<Guid, Guid> liveTargets = new(id => id);

        /// <summary>Invalidates affected child caches that are not being reconciled live.</summary>
        private readonly SerialDisposable collapsedCacheInvalidation = new();

        /// <summary>Identifies the root observed by the cache-only stream.</summary>
        private Guid? invalidationRootId;

        /// <summary>Prevents duplicate identity cleanup without suppressing per-parent cache invalidation.</summary>
        private ChangeEvent lastLiveChange;

        /// <summary>Identifies the structural change whose original parent set is retained across subscription callbacks.</summary>
        private ChangeEvent affectedParentChange;

        /// <summary>Retains both sides of a move before either callback changes cached ownership.</summary>
        private HashSet<ProjectBrowserNodeViewModel> affectedParents = [];

        /// <summary>Defers catalogue rescanning after cache-only invalidation until a browser operation needs it.</summary>
        private bool typeCatalogueInvalidated;

        /// <summary>Invalidates derived filtering after coherent tree changes.</summary>
        private readonly BehaviorSubject<Unit> modelChanges = new(Unit.Default);

        /// <summary>Indexes only nodes materialized by expansion or search.</summary>
        private readonly Dictionary<string, ProjectBrowserNodeViewModel> materializedNodes = new(StringComparer.Ordinal);

        /// <summary>Reserves stable row identities for the browser lifetime.</summary>
        private readonly HashSet<string> nodeIds = new(StringComparer.Ordinal);

        /// <summary>Reuses unchanged row projections to limit recursive component rendering.</summary>
        private readonly Dictionary<string, ProjectBrowserNodeRenderState> rowPresentations = new(StringComparer.Ordinal);

        /// <summary>Identifies the current model independently of its expanded state.</summary>
        private ProjectBrowserNodeViewModel modelRoot;

        /// <summary>Defers presentation publication until a compound mutation completes.</summary>
        private int mutationDepth;

        /// <summary>Holds the latest immutable presentation.</summary>
        private ProjectBrowserRenderState renderState = ProjectBrowserRenderState.Empty;

        /// <summary>Gets the latest coherent state for renderer-bound observation.</summary>
        public ProjectBrowserRenderState RenderState => Volatile.Read(ref this.renderState);

        /// <summary>Connects subtree observation to expanded namespaces and the model-root invalidation anchor.</summary>
        private void InitializeLiveUpdates()
        {
            this.subscriptions.Add(this.collapsedCacheInvalidation);
            this.subscriptions.Add(this.liveTargets.Connect()
                .MergeMany(id => this.changeNotificationService.Listen(ChangeTarget.Subtree(id)))
                .DistinctUntilChanged(change => (change.CommitId, change.ElementId))
                .Subscribe(this.ApplyChange));
        }

        /// <summary>Applies a compound browser operation and publishes its resulting presentation.</summary>
        /// <param name="operation">The owner-side state transition.</param>
        private void Mutate(Action operation)
        {
            lock (this.stateGate)
            {
                if (this.IsDisposed)
                {
                    return;
                }

                this.mutationDepth++;
                try
                {
                    if (this.typeCatalogueInvalidated)
                    {
                        this.RefreshTypeCatalogue();
                    }
                    operation();
                }
                finally
                {
                    this.mutationDepth--;
                    if (this.mutationDepth == 0 && !this.IsDisposed)
                    {
                        this.modelChanges.OnNext(Unit.Default);
                        if (!this.FilterPresentation.IsActive && this.modelRoot != null)
                        {
                            this.ReconcileExpandedCaches(this.modelRoot);
                        }
                        this.ReconcileLiveTargets();
                        var state = this.CaptureRenderState();
                        this.RaiseAndSetIfChanged(ref this.renderState, state, nameof(this.RenderState));
                    }
                }
            }
        }

        /// <summary>Changes expansion without discarding previously materialized child identities.</summary>
        /// <param name="node">The branch being changed.</param>
        /// <param name="expanded">Whether its children should become visible.</param>
        private void SetNodeExpanded(ProjectBrowserNodeViewModel node, bool expanded)
        {
            this.Mutate(() =>
            {
                if (!this.materializedNodes.TryGetValue(node.Id, out var current) || !ReferenceEquals(current, node))
                {
                    return;
                }

                if (expanded)
                {
                    this.EnsureChildren(node);
                }

                node.SetExpanded(expanded && node.HasChildren);
            });
        }

        /// <summary>Reconciles structurally invalidated visible caches without filling untouched partial focus paths.</summary>
        /// <param name="node">The next branch in the durable expansion hierarchy.</param>
        private void ReconcileExpandedCaches(ProjectBrowserNodeViewModel node)
        {
            if (!node.IsExpanded)
            {
                return;
            }
            if (node.ChildrenInvalidated)
            {
                this.EnsureChildren(node);
            }
            foreach (var child in node.Children)
            {
                this.ReconcileExpandedCaches(child);
            }
        }

        /// <summary>
        /// Synchronously materializes one branch from already-loaded SDK containment under the owner gate.
        /// No asynchronous child load can complete after a newer collapse or invalidation.
        /// </summary>
        /// <param name="node">The branch whose children are required.</param>
        private void EnsureChildren(ProjectBrowserNodeViewModel node)
        {
            if (node.AreChildrenLoaded && !node.ChildrenInvalidated)
            {
                return;
            }

            var children = new List<ProjectBrowserNodeViewModel>();
            var index = 0;
            foreach (var element in GetChildElements(node.SourceElement))
            {
                if (element is null || IsAncestorElement(node, element))
                {
                    continue;
                }

                var child = this.FindMaterialized(element);
                if (child is null)
                {
                    child = this.BuildNode(element, string.Create(CultureInfo.InvariantCulture, $"{node.Id}/{index}"),
                        this.lifetimeCancellation.Token);
                }

                if (child.Parent != null && !ReferenceEquals(child.Parent, node))
                {
                    child.Parent.ForgetChild(child);
                }
                child.Parent = node;
                child.UpdateElement(element, GetDisplayName(element, element.GetType().Name));
                children.Add(child);
                index++;
            }

            node.SetChildren(children.ToImmutableArray());
            foreach (var child in children.Where(child => child.IsExpanded && child.ChildrenInvalidated))
            {
                this.EnsureChildren(child);
            }
        }

        /// <summary>Includes owned relationships alongside their flattened owned elements without duplicate identities.</summary>
        /// <param name="element">The canonical parent.</param>
        /// <returns>The ordered direct children represented by the browser.</returns>
        internal static IEnumerable<IElement> GetChildElements(IElement element)
        {
            return (element.ownedElement ?? []).Concat(element.OwnedRelationship ?? [])
                .Where(child => child != null)
                .DistinctBy(child => string.IsNullOrWhiteSpace(child.ElementId) ? (object)child : child.ElementId);
        }

        /// <summary>Follows the collection owner for relationship rows and the derived owner for ordinary elements.</summary>
        /// <param name="element">The canonical child.</param>
        /// <returns>The parent used by the browser's flattened containment tree.</returns>
        private static IElement GetBrowserOwner(IElement element)
        {
            return element is IRelationship relationship ? relationship.OwningRelatedElement ?? element.owner : element.owner;
        }

        /// <summary>Prevents malformed containment cycles from creating recursive tree rows.</summary>
        /// <param name="node">The proposed parent.</param>
        /// <param name="element">The proposed child element.</param>
        /// <returns>Whether the child already appears in the parent's containment path.</returns>
        private static bool IsAncestorElement(ProjectBrowserNodeViewModel node, IElement element)
        {
            for (var ancestor = node; ancestor != null; ancestor = ancestor.Parent)
            {
                if (ReferenceEquals(ancestor.SourceElement, element))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Finds retained browser identity for a canonical SDK element.</summary>
        /// <param name="element">The current SDK identity.</param>
        /// <returns>The retained node, or null before materialization.</returns>
        private ProjectBrowserNodeViewModel FindMaterialized(IElement element)
        {
            if (element is null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(element.ElementId)
                && this.materializedNodes.TryGetValue(element.ElementId, out var node))
            {
                return node;
            }

            return this.materializedNodes.Values.FirstOrDefault(candidate => ReferenceEquals(candidate.SourceElement, element));
        }

        /// <summary>Reads the loaded SDK cache without allocating browser descendants.</summary>
        /// <returns>The canonical elements currently available to this browser.</returns>
        private IEnumerable<IElement> GetModelElements()
        {
            return this.assembler.Cache?.Values.Select(entry => entry.Value)
                .Where(element => element != null && this.BelongsToModel(element)) ?? [];
        }

        /// <summary>Scopes SDK cache entries to the browser's current containment root.</summary>
        /// <param name="element">The candidate model element.</param>
        /// <returns>Whether its canonical ownership chain reaches this model.</returns>
        private bool BelongsToModel(IElement element)
        {
            var visited = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
            for (var current = element; current != null && visited.Add(current);
                 current = GetBrowserOwner(current))
            {
                if (ReferenceEquals(current, this.modelRoot?.SourceElement))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Builds the filter catalogue from SDK metadata rather than materializing the hierarchy.</summary>
        /// <returns>The non-relationship metaclasses in the loaded model.</returns>
        private HashSet<Type> GetModelElementTypes()
        {
            return this.GetModelElements().Where(element => element is not IRelationship)
                .Select(element => element.GetType()).ToHashSet();
        }

        /// <summary>Materializes only containment paths required by a model-wide filter.</summary>
        /// <param name="text">The normalized text criterion.</param>
        /// <param name="types">The selected metaclasses.</param>
        /// <param name="visibleNodes">The matching identities and required ancestors.</param>
        private void MaterializeFilterPaths(string text, IReadOnlyCollection<Type> types,
            ImmutableHashSet<ProjectBrowserNodeViewModel>.Builder visibleNodes)
        {
            foreach (var element in this.GetModelElements().Where(element => MatchesSourceElement(element, text, types)))
            {
                this.MaterializePath(element);
                for (var node = this.FindMaterialized(element); node != null; node = node.Parent)
                {
                    visibleNodes.Add(node);
                }
            }
        }

        /// <summary>Evaluates canonical metadata without allocating browser descendants.</summary>
        /// <param name="element">The canonical candidate.</param>
        /// <param name="text">The normalized text criterion.</param>
        /// <param name="types">The selected concrete metaclasses.</param>
        /// <returns>Whether the candidate itself matches the criteria.</returns>
        private static bool MatchesSourceElement(IElement element, string text, IReadOnlyCollection<Type> types)
        {
            if (types.Count > 0 && !types.Contains(element.GetType()))
            {
                return false;
            }
            if (text.Length == 0 || ContainsText(GetDisplayName(element, element.GetType().Name), text))
            {
                return true;
            }
            var visited = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
            for (var owner = GetBrowserOwner(element); owner != null && visited.Add(owner); owner = GetBrowserOwner(owner))
            {
                if (ContainsText(GetDisplayName(owner, owner.GetType().Name), text))
                {
                    return false;
                }
            }
            return ContainsText(element.qualifiedName, text);
        }

        /// <summary>Materializes one canonical ownership path without altering manual expansion.</summary>
        /// <param name="element">The element that must become addressable.</param>
        private void MaterializePath(IElement element)
        {
            if (this.modelRoot is null)
            {
                return;
            }

            var path = new Stack<IElement>();
            var visited = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
            for (var current = element; current != null && visited.Add(current); current = GetBrowserOwner(current))
            {
                path.Push(current);
                if (ReferenceEquals(current, this.modelRoot.SourceElement))
                {
                    path.Pop();
                    var parent = this.modelRoot;
                    while (path.TryPop(out var childElement))
                    {
                        var child = this.FindMaterialized(childElement)
                            ?? this.BuildNode(childElement, string.Create(CultureInfo.InvariantCulture, $"{parent.Id}/match"),
                                this.lifetimeCancellation.Token);
                        if (child.Parent != null && !ReferenceEquals(child.Parent, parent))
                        {
                            child.Parent.ForgetChild(child);
                        }
                        child.Parent = parent;
                        child.UpdateElement(childElement, GetDisplayName(childElement, childElement.GetType().Name));
                        parent.RetainChild(child);
                        parent = child;
                    }

                    return;
                }
            }
        }

        /// <summary>Reconciles subscription resources for the model anchor and visible expanded branches.</summary>
        private void ReconcileLiveTargets()
        {
            var targets = new HashSet<Guid>();
            if (this.modelRoot != null)
            {
                this.AddExpandedTargets(this.modelRoot, targets);
            }

            Guid? rootId = Guid.TryParse(this.modelRoot?.ElementId, out var id) && id != Guid.Empty ? id : null;
            if (this.invalidationRootId != rootId)
            {
                this.invalidationRootId = rootId;
                this.collapsedCacheInvalidation.Disposable = rootId.HasValue
                    ? this.changeNotificationService.Listen(ChangeTarget.Subtree(rootId.Value))
                        .Where(change => change.Kind is ChangeKind.Created or ChangeKind.Deleted or ChangeKind.Moved)
                        .Subscribe(this.InvalidateUnobservedCaches)
                    : Disposable.Empty;
            }

            this.liveTargets.Edit(cache =>
            {
                cache.RemoveKeys(cache.Keys.Where(id => !targets.Contains(id)).ToArray());
                cache.AddOrUpdate(targets.Where(id => !cache.Lookup(id).HasValue));
            });
        }

        /// <summary>Collects namespace targets from durable expansion independently of filtered presentation.</summary>
        /// <param name="node">The branch in the durable expansion hierarchy.</param>
        /// <param name="targets">The distinct namespace identities.</param>
        private void AddExpandedTargets(ProjectBrowserNodeViewModel node, HashSet<Guid> targets)
        {
            if (!node.IsExpanded)
            {
                return;
            }

            AddTarget(node, targets);
            foreach (var child in node.Children)
            {
                this.AddExpandedTargets(child, targets);
            }
        }

        /// <summary>Uses the closest containing namespace for non-namespace tree branches.</summary>
        /// <param name="node">The expanded branch.</param>
        /// <param name="targets">The namespace identities to observe.</param>
        private static void AddTarget(ProjectBrowserNodeViewModel node, HashSet<Guid> targets)
        {
            for (var current = node; current != null; current = current.Parent)
            {
                if (current.SourceElement is INamespace && Guid.TryParse(current.ElementId, out var id) && id != Guid.Empty)
                {
                    targets.Add(id);
                    return;
                }
            }
        }

        /// <summary>Reconciles affected cached branches after the model owner has applied a committed change.</summary>
        /// <param name="change">The committed change with captured containment.</param>
        private void ApplyChange(ChangeEvent change)
        {
            this.Mutate(() =>
            {
                if (this.modelRoot is null)
                {
                    return;
                }

                this.lastLiveChange = change;
                var id = change.ElementId.ToString();
                this.materializedNodes.TryGetValue(id, out var node);
                var element = this.assembler.Cache.TryGetValue(change.ElementId, out var cached)
                    ? cached.Value : node?.SourceElement;
                var affected = this.GetAffectedParents(change, node, element);

                ReconcileChangedNode(change, node, element);

                if (change.Kind is ChangeKind.Created or ChangeKind.Deleted or ChangeKind.Moved)
                {
                    foreach (var parent in affected.Where(this.IsLiveBranch))
                    {
                        parent.ChildrenInvalidated = true;
                        this.EnsureChildren(parent);
                    }

                    this.RefreshTypeCatalogue();
                }
            });
        }

        /// <summary>Reconciles a changed cached identity without rebuilding unrelated membership.</summary>
        /// <param name="change">The committed operation.</param>
        /// <param name="node">The retained browser identity.</param>
        /// <param name="element">The canonical SDK element when available.</param>
        private void ReconcileChangedNode(ChangeEvent change, ProjectBrowserNodeViewModel node, IElement element)
        {
            if (change.Kind == ChangeKind.Deleted)
            {
                if (node != null)
                {
                    this.RemoveDeletedNode(node);
                }
            }
            else
            {
                if (element != null)
                {
                    node?.UpdateElement(element, GetDisplayName(element, element.GetType().Name));
                    if (change.Kind == ChangeKind.Moved && node != null)
                    {
                        node.Parent?.ForgetChild(node);
                        node.Parent = this.FindMaterialized(GetBrowserOwner(element));
                    }
                }
            }
        }

        /// <summary>
        /// Invalidates each affected parent independently of live reconciliation on the other side of a move.
        /// It prunes obsolete identities but never fetches children or executes live reconciliation.
        /// </summary>
        /// <param name="change">A structural change observed by the model-root invalidation stream.</param>
        private void InvalidateUnobservedCaches(ChangeEvent change)
        {
            lock (this.stateGate)
            {
                if (this.IsDisposed || this.modelRoot is null)
                {
                    return;
                }

                this.materializedNodes.TryGetValue(change.ElementId.ToString(), out var node);
                var element = this.assembler.Cache.TryGetValue(change.ElementId, out var cached)
                    ? cached.Value : node?.SourceElement;
                var invalidated = this.GetAffectedParents(change, node, element)
                    .Where(parent => !this.IsLiveBranch(parent)).ToHashSet();
                foreach (var parent in invalidated)
                {
                    parent.ChildrenInvalidated = true;
                }

                if (ReferenceEquals(change, this.lastLiveChange) || this.IsCoveredByLiveTarget(change))
                {
                    this.PublishInvalidatedRows(invalidated);
                    return;
                }

                if (change.Kind == ChangeKind.Deleted && node != null)
                {
                    this.RemoveDeletedNode(node);
                }
                else if (change.Kind == ChangeKind.Moved && node != null && element != null)
                {
                    node.Parent?.ForgetChild(node);
                    node.Parent = this.FindMaterialized(GetBrowserOwner(element));
                }

                this.typeCatalogueInvalidated = true;
                this.ReconcileLiveTargets();
                this.PublishInvalidatedRows(invalidated);
            }
        }

        /// <summary>Updates cached caret and selection presentation without loading children or reconciling live branches.</summary>
        /// <param name="invalidated">The parents whose membership is no longer current.</param>
        private void PublishInvalidatedRows(HashSet<ProjectBrowserNodeViewModel> invalidated)
        {
            var roots = this.renderState.Roots.Where(row => ReferenceEquals(row.Node, this.modelRoot))
                .Select(row => this.RefreshInvalidatedRow(row, invalidated)).Where(row => row != null).ToImmutableArray();
            if (this.renderState.Roots.SequenceEqual(roots)
                && ReferenceEquals(this.renderState.SelectedNode, this.SelectedNode))
            {
                return;
            }
            var state = this.renderState with { Roots = roots, SelectedNode = this.SelectedNode };
            this.RaiseAndSetIfChanged(ref this.renderState, state, nameof(this.RenderState));
        }

        /// <summary>Preserves immutable row identity except along a changed cached caret path.</summary>
        /// <param name="row">The existing presentation.</param>
        /// <param name="invalidated">The invalidated cached parents.</param>
        /// <returns>The retained or updated presentation, or null when the row is no longer visible.</returns>
        private ProjectBrowserNodeRenderState RefreshInvalidatedRow(ProjectBrowserNodeRenderState row,
            HashSet<ProjectBrowserNodeViewModel> invalidated)
        {
            if (row.Node.ExpansionRequested is null)
            {
                return null;
            }
            var children = row.Children.Where(child => ReferenceEquals(child.Node.Parent, row.Node))
                .Select(child => this.RefreshInvalidatedRow(child, invalidated)).Where(child => child != null).ToImmutableArray();
            var filtering = this.FilterPresentation.IsActive;
            if (filtering && children.IsEmpty
                && !MatchesSourceElement(row.Node.SourceElement, this.FilterText.Trim(), this.selectedElementTypes))
            {
                return null;
            }
            var hasChildren = invalidated.Contains(row.Node) ? row.Node.HasChildren : row.HasChildren;
            if (filtering)
            {
                hasChildren = children.Length > 0;
            }
            return hasChildren == row.HasChildren && row.Children.SequenceEqual(children)
                ? row : row with { HasChildren = hasChildren, IsExpanded = row.IsExpanded && hasChildren, Children = children };
        }

        /// <summary>Identifies an expanded branch eligible for immediate child reconciliation.</summary>
        /// <param name="node">The affected cached parent.</param>
        /// <returns>Whether its children are durably expanded and currently rendered without filtering.</returns>
        private bool IsLiveBranch(ProjectBrowserNodeViewModel node)
        {
            return node.IsExpanded && this.IsBranchVisible(node) && !this.FilterPresentation.IsActive;
        }

        /// <summary>Checks whether live delivery owns identity cleanup after per-parent invalidation.</summary>
        /// <param name="change">The captured structural change.</param>
        /// <returns>Whether an expanded namespace receives the event.</returns>
        private bool IsCoveredByLiveTarget(ChangeEvent change)
        {
            return change.Containment.NamespaceIds
                .Concat(change.PreviousContainment?.NamespaceIds ?? [])
                .Append(change.ElementId).Any(id => this.liveTargets.Lookup(id).HasValue);
        }

        /// <summary>Finds cached old, current and captured namespace parents without materializing new branches.</summary>
        /// <param name="change">The committed containment information.</param>
        /// <param name="node">The retained changed node, if materialized.</param>
        /// <param name="element">The current SDK element, if it still exists.</param>
        /// <returns>The affected cached parents.</returns>
        private HashSet<ProjectBrowserNodeViewModel> GetAffectedParents(ChangeEvent change,
            ProjectBrowserNodeViewModel node, IElement element)
        {
            if (ReferenceEquals(change, this.affectedParentChange))
            {
                return this.affectedParents;
            }
            var parents = new HashSet<ProjectBrowserNodeViewModel>();
            if (node?.Parent != null)
            {
                parents.Add(node.Parent);
            }
            if (element != null && this.FindMaterialized(GetBrowserOwner(element)) is { } currentParent)
            {
                parents.Add(currentParent);
            }
            foreach (var id in new[] { change.ParentNamespaceId, change.PreviousContainment?.ParentNamespaceId })
            {
                if (id.HasValue && this.materializedNodes.TryGetValue(id.Value.ToString(), out var parent))
                {
                    parents.Add(parent);
                }
            }
            this.affectedParentChange = change;
            this.affectedParents = parents;
            return parents;
        }

        /// <summary>Refreshes available type metadata without allocating tree nodes.</summary>
        private void RefreshTypeCatalogue()
        {
            var availableTypes = this.GetModelElementTypes();
            this.availableElementTypeSource.Edit(cache =>
            {
                cache.RemoveKeys(cache.Keys.Where(type => !availableTypes.Contains(type)).ToArray());
                cache.AddOrUpdate(availableTypes);
            });
            this.typeCatalogueInvalidated = false;
        }

        /// <summary>Checks manual or filtered visibility without expanding ancestors.</summary>
        /// <param name="node">The branch being considered.</param>
        /// <returns>Whether every ancestor exposes the branch.</returns>
        private bool IsBranchVisible(ProjectBrowserNodeViewModel node)
        {
            for (var parent = node.Parent; parent != null; parent = parent.Parent)
            {
                if (!parent.IsExpanded && !this.FilterPresentation.IsActive)
                {
                    return false;
                }
            }

            return this.FilterPresentation.IsVisible(node);
        }

        /// <summary>Releases deleted cached identities and clears selection only when it references a deleted element.</summary>
        /// <param name="node">The deleted browser identity.</param>
        private void RemoveDeletedNode(ProjectBrowserNodeViewModel node)
        {
            node.Parent?.ForgetChild(node);
            foreach (var child in node.Children.Where(child => ReferenceEquals(child.Parent, node)).ToArray())
            {
                this.RemoveDeletedNode(child);
            }

            if (ReferenceEquals(this.SelectedNode, node))
            {
                this.SelectedNode = null;
            }

            if (ReferenceEquals(this.elementSelectionService.SelectedElement, node.SourceElement)
                || this.elementSelectionService.SelectedElement?.ElementId == node.ElementId)
            {
                this.elementSelectionService.SelectedElement = null;
            }

            node.ExpansionRequested = null;
            this.materializedNodes.Remove(node.Id);
            this.nodeIds.Remove(node.Id);
            this.rowPresentations.Remove(node.Id);
            if (ReferenceEquals(node, this.modelRoot))
            {
                this.modelRoot = null;
                this.rootNodeSource.Clear();
            }
        }

        /// <summary>Captures one complete renderer-facing presentation under the owner gate.</summary>
        /// <returns>The immutable browser state.</returns>
        private ProjectBrowserRenderState CaptureRenderState()
        {
            var state = new ProjectBrowserRenderState(this.rootNodes.Where(this.FilterPresentation.IsVisible).Select(this.CaptureNode).ToImmutableArray(),
                this.SelectedNode, this.FilterText, this.availableElementTypes.ToImmutableArray(),
                this.selectedElementTypes.ToImmutableArray(), this.FilterPresentation, this.IsLoading,
                this.IsLoaded, this.ErrorMessage);
            var previous = this.renderState;
            return state.Roots.SequenceEqual(previous.Roots)
                && ReferenceEquals(state.SelectedNode, previous.SelectedNode)
                && state.FilterText == previous.FilterText
                && state.AvailableElementTypes.SequenceEqual(previous.AvailableElementTypes)
                && state.SelectedElementTypes.SequenceEqual(previous.SelectedElementTypes)
                && ReferenceEquals(state.FilterPresentation, previous.FilterPresentation)
                && state.IsLoading == previous.IsLoading && state.IsLoaded == previous.IsLoaded
                && state.ErrorMessage == previous.ErrorMessage ? previous : state;
        }

        /// <summary>Captures visible descendants while retaining stable node identities.</summary>
        /// <param name="node">The row to capture.</param>
        /// <returns>The immutable row presentation.</returns>
        private ProjectBrowserNodeRenderState CaptureNode(ProjectBrowserNodeViewModel node)
        {
            node.UpdateElement(node.SourceElement, GetDisplayName(node.SourceElement, node.ElementType.Name));
            var children = node.Children.Where(this.FilterPresentation.IsVisible).ToArray();
            var hasChildren = this.FilterPresentation.IsActive ? children.Length > 0 : node.HasChildren;
            var expanded = hasChildren && (node.IsExpanded || this.FilterPresentation.IsActive);
            var childStates = expanded ? children.Select(this.CaptureNode).ToImmutableArray() : [];
            if (this.rowPresentations.TryGetValue(node.Id, out var previous)
                && previous.DisplayName == node.DisplayName && previous.QualifiedName == node.QualifiedName
                && previous.ElementType == node.ElementType && previous.HasChildren == hasChildren
                && previous.IsExpanded == expanded && previous.Children.SequenceEqual(childStates))
            {
                return previous;
            }

            var state = new ProjectBrowserNodeRenderState(node, node.DisplayName, node.QualifiedName,
                node.ElementType, hasChildren, expanded, childStates);
            this.rowPresentations[node.Id] = state;
            return state;
        }
    }
}
