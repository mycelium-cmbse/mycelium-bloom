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
    using System.Collections.ObjectModel;

    using Microsoft.Extensions.Options;

    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Model;

    using ReactiveUI;

    /// <summary>
    /// Coordinates rendering-independent editor-group ownership, tab transfers, and logical workspace focus.
    /// </summary>
    public sealed class WorkspaceEditorViewModel : ReactiveObject, IWorkspaceEditorViewModel
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
        /// The editor group that currently has logical workspace focus.
        /// </summary>
        private EditorGroupViewModel focusedGroup;

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
        }

        /// <inheritdoc />
        public int MaximumGroupCount { get; }

        /// <inheritdoc />
        public ReadOnlyObservableCollection<EditorGroupViewModel> Groups => this.readOnlyGroups;

        /// <inheritdoc />
        public EditorGroupViewModel FocusedGroup
        {
            get => this.focusedGroup;
            private set => this.RaiseAndSetIfChanged(ref this.focusedGroup, value);
        }

        /// <inheritdoc />
        public bool TryAddGroup(out EditorGroupViewModel group)
        {
            group = null;

            if (this.groups.Count >= this.MaximumGroupCount)
            {
                return false;
            }

            group = new EditorGroupViewModel();
            this.groups.Add(group);
            this.FocusedGroup = group;

            return true;
        }

        /// <inheritdoc />
        public bool TryOpenTab(
            Guid groupId,
            string title,
            string viewTypeKey,
            out EditorTabItem tab)
        {
            EditorTabItem.ValidateMetadata(title, viewTypeKey);

            tab = null;

            if (!this.TryGetGroup(groupId, out var group))
            {
                return false;
            }

            tab = new EditorTabItem(title, viewTypeKey);
            group.AddTab(tab);
            this.FocusedGroup = group;

            return true;
        }

        /// <inheritdoc />
        public bool ActivateTab(Guid groupId, Guid tabId)
        {
            if (!this.TryGetGroup(groupId, out var group) || !group.TryActivateTab(tabId))
            {
                return false;
            }

            this.FocusedGroup = group;

            return true;
        }

        /// <inheritdoc />
        public bool FocusGroup(Guid groupId)
        {
            if (!this.TryGetGroup(groupId, out var group))
            {
                return false;
            }

            this.FocusedGroup = group;

            return true;
        }

        /// <inheritdoc />
        public bool CloseTab(Guid groupId, Guid tabId)
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
        }

        /// <inheritdoc />
        public bool MoveTab(Guid sourceGroupId, Guid tabId, Guid destinationGroupId)
        {
            if (sourceGroupId == destinationGroupId
                || !this.TryGetGroup(sourceGroupId, out var sourceGroup)
                || !this.TryGetGroup(destinationGroupId, out var destinationGroup)
                || !sourceGroup.TryGetTab(tabId, out var tab))
            {
                return false;
            }

            if (!sourceGroup.TryRemoveTab(tabId, out _))
            {
                return false;
            }

            destinationGroup.AddTab(tab);
            this.FocusedGroup = destinationGroup;

            if (sourceGroup.Tabs.Count == 0)
            {
                this.RemoveEmptyGroup(sourceGroup);
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
