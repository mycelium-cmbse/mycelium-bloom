// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceEditorViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.WorkspaceEditor
{
    using System.Collections.Immutable;
    using System.Collections.ObjectModel;
    using System.Reactive;
    using System.Reactive.Linq;

    using DynamicData;
    using DynamicData.Binding;

    using Microsoft.Extensions.Options;

    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Model;

    using ReactiveUI;

    /// <summary>
    /// Coordinates rendering-independent editor-group ownership, tab transfers, and logical workspace focus.
    /// </summary>
    public sealed class WorkspaceEditorViewModel : ReactiveObject, IWorkspaceEditorViewModel, IDisposable
    {
        /// <summary>
        /// The mutable editor-group collection owned exclusively by this ViewModel.
        /// </summary>
        private readonly ObservableCollection<EditorGroupViewModel> groups = [];

        /// <summary>
        /// The stable read-only projection of <see cref="groups" />.
        /// </summary>
        private readonly ReadOnlyObservableCollection<EditorGroupViewModel> readOnlyGroups;

        /// <summary>
        /// Aggregates rendering-relevant changes throughout the owned editor graph.
        /// </summary>
        private readonly IDisposable renderStateSubscription;

        /// <summary>
        /// The editor group that currently has logical workspace focus.
        /// </summary>
        private EditorGroupViewModel focusedGroup;

        /// <summary>
        /// The latest coherent immutable rendering snapshot.
        /// </summary>
        private WorkspaceEditorRenderState renderState;

        /// <summary>
        /// The latest published rendering revision.
        /// </summary>
        private long renderRevision;

        /// <summary>
        /// The number of nested owner mutations currently being composed.
        /// </summary>
        private int mutationDepth;

        /// <summary>
        /// A value indicating whether a rendering-relevant change occurred during the current mutation.
        /// </summary>
        private bool renderStateDirty;

        /// <summary>
        /// A value indicating whether final disposal has occurred.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkspaceEditorViewModel" /> class using the configured
        /// editor-group limit and creates one empty, focused editor group.
        /// </summary>
        /// <param name="options">Provides the validated application-configured editor-group limit.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="options" /> is <see langword="null" />.
        /// </exception>
        public WorkspaceEditorViewModel(IOptions<WorkspaceEditorOptions> options)
        {
            ArgumentNullException.ThrowIfNull(options);

            this.MaximumGroupCount = options.Value.MaximumGroupCount;
            this.readOnlyGroups = new ReadOnlyObservableCollection<EditorGroupViewModel>(this.groups);

            var initialGroup = new EditorGroupViewModel();

            this.groups.Add(initialGroup);
            this.focusedGroup = initialGroup;
            this.renderState = this.CaptureRenderState(this.renderRevision);

            this.renderStateSubscription = this.groups
                .ToObservableChangeSet()
                .Publish(groupChanges => Observable.Merge(
                    groupChanges.Select(_ => Unit.Default),
                    groupChanges
                        .AutoRefresh(group => group.ActiveTab)
                        .Select(_ => Unit.Default),
                    groupChanges.MergeMany(group => group.Tabs
                        .ToObservableChangeSet()
                        .AutoRefresh(tab => tab.Title)
                        .Select(_ => Unit.Default))))
                .Subscribe(_ => this.InvalidateRenderState());
        }

        /// <inheritdoc />
        public int MaximumGroupCount { get; }

        /// <inheritdoc />
        public WorkspaceEditorRenderState RenderState => this.renderState;

        /// <inheritdoc />
        public ReadOnlyObservableCollection<EditorGroupViewModel> Groups => this.readOnlyGroups;

        /// <inheritdoc />
        public EditorGroupViewModel FocusedGroup
        {
            get => this.focusedGroup;

            private set
            {
                if (ReferenceEquals(this.focusedGroup, value))
                {
                    return;
                }

                this.RaiseAndSetIfChanged(ref this.focusedGroup, value);
                this.MarkRenderStateDirty();
            }
        }

        /// <inheritdoc />
        public bool TryAddGroup(out EditorGroupViewModel group)
        {
            EditorGroupViewModel createdGroup = null;

            var added = this.ExecuteMutation(() =>
            {
                if (this.groups.Count >= this.MaximumGroupCount)
                {
                    return false;
                }

                createdGroup = new EditorGroupViewModel();
                this.groups.Add(createdGroup);
                this.FocusedGroup = createdGroup;

                return true;
            });

            group = createdGroup;

            return added;
        }

        /// <inheritdoc />
        public bool TrySplitGroup(Guid groupId, out EditorGroupViewModel group)
        {
            EditorGroupViewModel createdGroup = null;

            var split = this.ExecuteMutation(() =>
            {
                if (this.groups.Count >= this.MaximumGroupCount
                    || !this.TryGetGroup(groupId, out var leftGroup))
                {
                    return false;
                }

                createdGroup = new EditorGroupViewModel();
                var insertionIndex = this.groups.IndexOf(leftGroup) + 1;
                this.groups.Insert(insertionIndex, createdGroup);
                this.FocusedGroup = createdGroup;

                return true;
            });

            group = createdGroup;

            return split;
        }

        /// <inheritdoc />
        public bool TryMoveTabToNewGroup(
            Guid sourceGroupId,
            Guid tabId,
            Guid splitAfterGroupId,
            out EditorGroupViewModel group)
        {
            EditorGroupViewModel createdGroup = null;

            var moved = this.ExecuteMutation(() =>
            {
                if (this.groups.Count >= this.MaximumGroupCount
                    || !this.TryGetGroup(sourceGroupId, out var sourceGroup)
                    || !this.TryGetGroup(splitAfterGroupId, out var splitAfterGroup)
                    || !sourceGroup.TryGetTab(tabId, out _)
                    || (ReferenceEquals(sourceGroup, splitAfterGroup) && sourceGroup.Tabs.Count == 1))
                {
                    return false;
                }

                if (!sourceGroup.TryRemoveTab(tabId, out var movedTab))
                {
                    return false;
                }

                createdGroup = new EditorGroupViewModel();
                var insertionIndex = this.groups.IndexOf(splitAfterGroup) + 1;
                this.groups.Insert(insertionIndex, createdGroup);
                createdGroup.AddTab(movedTab);
                this.FocusedGroup = createdGroup;

                if (sourceGroup.Tabs.Count == 0)
                {
                    this.RemoveEmptyGroup(sourceGroup);
                }

                return true;
            });

            group = createdGroup;

            return moved;
        }

        /// <inheritdoc />
        public bool TryOpenTab(
            Guid groupId,
            string title,
            string viewTypeKey,
            out EditorTabItem tab)
        {
            EditorTabItem.ValidateMetadata(title, viewTypeKey);
            EditorTabItem createdTab = null;

            var opened = this.ExecuteMutation(() =>
            {
                if (!this.TryGetGroup(groupId, out var group))
                {
                    return false;
                }

                createdTab = new EditorTabItem(title, viewTypeKey);
                group.AddTab(createdTab);
                this.FocusedGroup = group;

                return true;
            });

            tab = createdTab;

            return opened;
        }

        /// <inheritdoc />
        public bool ActivateTab(Guid groupId, Guid tabId)
        {
            return this.ExecuteMutation(() =>
            {
                if (!this.TryGetGroup(groupId, out var group) || !group.TryActivateTab(tabId))
                {
                    return false;
                }

                this.FocusedGroup = group;

                return true;
            });
        }

        /// <inheritdoc />
        public bool FocusGroup(Guid groupId)
        {
            return this.ExecuteMutation(() =>
            {
                if (!this.TryGetGroup(groupId, out var group))
                {
                    return false;
                }

                this.FocusedGroup = group;

                return true;
            });
        }

        /// <inheritdoc />
        public bool CloseTab(Guid groupId, Guid tabId)
        {
            return this.ExecuteMutation(() =>
            {
                if (!this.TryGetGroup(groupId, out var group)
                    || !group.TryRemoveTab(tabId, out _))
                {
                    return false;
                }

                if (group.Tabs.Count == 0 && this.groups.Count > 1)
                {
                    this.RemoveEmptyGroup(group);
                }

                return true;
            });
        }

        /// <inheritdoc />
        public bool MoveTab(Guid sourceGroupId, Guid tabId, Guid destinationGroupId)
        {
            if (sourceGroupId == destinationGroupId)
            {
                return false;
            }

            return this.MoveTab(sourceGroupId, tabId, destinationGroupId, null);
        }

        /// <inheritdoc />
        public bool MoveTab(
            Guid sourceGroupId,
            Guid tabId,
            Guid destinationGroupId,
            Guid? beforeTabId)
        {
            return this.ExecuteMutation(() =>
            {
                if (!this.TryGetGroup(sourceGroupId, out var sourceGroup)
                    || !this.TryGetGroup(destinationGroupId, out var destinationGroup)
                    || !sourceGroup.TryGetTab(tabId, out var tab))
                {
                    return false;
                }

                EditorTabItem destinationAnchor = null;

                if (beforeTabId.HasValue
                    && !destinationGroup.TryGetTab(beforeTabId.Value, out destinationAnchor))
                {
                    return false;
                }

                if (ReferenceEquals(sourceGroup, destinationGroup))
                {
                    return sourceGroup.TryReorderTab(tab, destinationAnchor);
                }

                if (!sourceGroup.TryRemoveTab(tabId, out var movedTab))
                {
                    return false;
                }

                destinationGroup.InsertTab(movedTab, destinationAnchor);
                this.FocusedGroup = destinationGroup;

                if (sourceGroup.Tabs.Count == 0)
                {
                    this.RemoveEmptyGroup(sourceGroup);
                }

                return true;
            });
        }

        /// <summary>
        /// Releases the centralized rendering aggregation pipeline.
        /// </summary>
        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;
            this.renderStateSubscription.Dispose();
        }

        /// <summary>
        /// Executes one public state transition and batches its rendering publication.
        /// </summary>
        /// <param name="mutation">The state transition.</param>
        /// <returns>The transition result, or false after disposal.</returns>
        private bool ExecuteMutation(Func<bool> mutation)
        {
            if (this.isDisposed)
            {
                return false;
            }

            this.mutationDepth++;

            try
            {
                return mutation();
            }
            finally
            {
                this.mutationDepth--;

                if (this.mutationDepth == 0 && this.renderStateDirty)
                {
                    this.PublishRenderState();
                }
            }
        }

        /// <summary>
        /// Marks rendering state dirty from the owner-side reactive graph.
        /// </summary>
        private void InvalidateRenderState()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.MarkRenderStateDirty();

            if (this.mutationDepth == 0)
            {
                this.PublishRenderState();
            }
        }

        /// <summary>
        /// Marks the current owner mutation as rendering-relevant.
        /// </summary>
        private void MarkRenderStateDirty()
        {
            this.renderStateDirty = true;
        }

        /// <summary>
        /// Captures and publishes a structurally distinct rendering snapshot.
        /// </summary>
        private void PublishRenderState()
        {
            this.renderStateDirty = false;

            var candidateState = this.CaptureRenderState(this.renderRevision + 1);

            if (AreStructurallyEqual(this.renderState, candidateState))
            {
                return;
            }

            this.renderRevision++;
            this.RaiseAndSetIfChanged(ref this.renderState, candidateState, nameof(this.RenderState));
        }

        /// <summary>
        /// Captures all durable data required by editor rendering.
        /// </summary>
        /// <param name="revision">The revision assigned when this state is published.</param>
        /// <returns>The coherent immutable rendering snapshot.</returns>
        private WorkspaceEditorRenderState CaptureRenderState(long revision)
        {
            var groupStates = ImmutableArray.CreateBuilder<WorkspaceEditorGroupRenderState>(this.groups.Count);

            foreach (var group in this.groups)
            {
                var tabStates = ImmutableArray.CreateBuilder<WorkspaceEditorTabRenderState>(group.Tabs.Count);

                foreach (var tab in group.Tabs)
                {
                    tabStates.Add(new WorkspaceEditorTabRenderState(tab.Id, tab.Title, tab));
                }

                groupStates.Add(new WorkspaceEditorGroupRenderState(
                    group.Id,
                    group.ActiveTab?.Id,
                    tabStates.MoveToImmutable()));
            }

            return new WorkspaceEditorRenderState(
                revision,
                this.focusedGroup?.Id,
                groupStates.MoveToImmutable());
        }

        /// <summary>
        /// Compares captured rendering state explicitly without relying on collection or record equality.
        /// </summary>
        /// <param name="left">The currently published state.</param>
        /// <param name="right">The candidate state.</param>
        /// <returns>True when every rendering-relevant value is structurally equal.</returns>
        private static bool AreStructurallyEqual(
            WorkspaceEditorRenderState left,
            WorkspaceEditorRenderState right)
        {
            if (left is null
                || right is null
                || left.FocusedGroupId != right.FocusedGroupId
                || left.Groups.Length != right.Groups.Length)
            {
                return false;
            }

            for (var groupIndex = 0; groupIndex < left.Groups.Length; groupIndex++)
            {
                var leftGroup = left.Groups[groupIndex];
                var rightGroup = right.Groups[groupIndex];

                if (leftGroup.Id != rightGroup.Id
                    || leftGroup.ActiveTabId != rightGroup.ActiveTabId
                    || leftGroup.Tabs.Length != rightGroup.Tabs.Length)
                {
                    return false;
                }

                for (var tabIndex = 0; tabIndex < leftGroup.Tabs.Length; tabIndex++)
                {
                    var leftTab = leftGroup.Tabs[tabIndex];
                    var rightTab = rightGroup.Tabs[tabIndex];

                    if (leftTab.Id != rightTab.Id
                        || !string.Equals(leftTab.Title, rightTab.Title, StringComparison.Ordinal)
                        || !ReferenceEquals(leftTab.Item, rightTab.Item))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Attempts to retrieve a workspace-owned editor group by identity.
        /// </summary>
        /// <param name="groupId">The identity of the group to retrieve.</param>
        /// <param name="group">
        /// The matching group when found; otherwise, <see langword="null" />.
        /// </param>
        /// <returns>
        /// <see langword="true" /> when the group belongs to this workspace; otherwise, <see langword="false" />.
        /// </returns>
        private bool TryGetGroup(Guid groupId, out EditorGroupViewModel group)
        {
            if (groupId != Guid.Empty)
            {
                foreach (var candidateGroup in this.groups)
                {
                    if (candidateGroup.Id == groupId)
                    {
                        group = candidateGroup;

                        return true;
                    }
                }
            }

            group = null;

            return false;
        }

        /// <summary>
        /// Removes a redundant empty group while keeping logical focus on a workspace-owned group.
        /// </summary>
        /// <param name="group">The empty group to remove while at least one other group remains.</param>
        private void RemoveEmptyGroup(EditorGroupViewModel group)
        {
            var groupIndex = this.groups.IndexOf(group);

            if (ReferenceEquals(this.FocusedGroup, group))
            {
                var nextFocusIndex = groupIndex < this.groups.Count - 1
                    ? groupIndex + 1
                    : groupIndex - 1;

                this.FocusedGroup = this.groups[nextFocusIndex];
            }

            this.groups.RemoveAt(groupIndex);
        }
    }
}
