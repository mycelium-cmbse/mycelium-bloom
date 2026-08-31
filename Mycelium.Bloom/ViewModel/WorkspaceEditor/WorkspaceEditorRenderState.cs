// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceEditorRenderState.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.WorkspaceEditor
{
    using System.Collections.Immutable;

    using Mycelium.Bloom.Model;

    /// <summary>
    /// Provides one coherent, immutable rendering snapshot of an editor workspace.
    /// </summary>
    public sealed class WorkspaceEditorRenderState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkspaceEditorRenderState" /> class.
        /// </summary>
        /// <param name="revision">The monotonically increasing rendering revision.</param>
        /// <param name="focusedGroupId">The focused group identity.</param>
        /// <param name="groups">The ordered immutable group snapshots.</param>
        public WorkspaceEditorRenderState(
            long revision,
            Guid? focusedGroupId,
            ImmutableArray<WorkspaceEditorGroupRenderState> groups)
        {
            this.Revision = revision;
            this.FocusedGroupId = focusedGroupId;
            this.Groups = groups.IsDefault
                ? ImmutableArray<WorkspaceEditorGroupRenderState>.Empty
                : groups;
        }

        /// <summary>
        /// Gets the monotonically increasing rendering revision.
        /// </summary>
        public long Revision { get; }

        /// <summary>
        /// Gets the focused group identity.
        /// </summary>
        public Guid? FocusedGroupId { get; }

        /// <summary>
        /// Gets the ordered immutable group snapshots.
        /// </summary>
        public ImmutableArray<WorkspaceEditorGroupRenderState> Groups { get; }
    }

    /// <summary>
    /// Provides one immutable rendering snapshot of an editor group.
    /// </summary>
    public sealed class WorkspaceEditorGroupRenderState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkspaceEditorGroupRenderState" /> class.
        /// </summary>
        /// <param name="id">The group identity.</param>
        /// <param name="activeTabId">The active tab identity.</param>
        /// <param name="tabs">The ordered immutable tab snapshots.</param>
        public WorkspaceEditorGroupRenderState(
            Guid id,
            Guid? activeTabId,
            ImmutableArray<WorkspaceEditorTabRenderState> tabs)
        {
            this.Id = id;
            this.ActiveTabId = activeTabId;
            this.Tabs = tabs.IsDefault
                ? ImmutableArray<WorkspaceEditorTabRenderState>.Empty
                : tabs;
        }

        /// <summary>
        /// Gets the group identity.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the active tab identity.
        /// </summary>
        public Guid? ActiveTabId { get; }

        /// <summary>
        /// Gets the ordered immutable tab snapshots.
        /// </summary>
        public ImmutableArray<WorkspaceEditorTabRenderState> Tabs { get; }
    }

    /// <summary>
    /// Provides one immutable rendering snapshot of an editor tab.
    /// </summary>
    public sealed class WorkspaceEditorTabRenderState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkspaceEditorTabRenderState" /> class.
        /// </summary>
        /// <param name="id">The tab identity.</param>
        /// <param name="title">The captured tab title.</param>
        /// <param name="item">The exact tab instance supplied to the editor-content contract.</param>
        public WorkspaceEditorTabRenderState(Guid id, string title, EditorTabItem item)
        {
            ArgumentNullException.ThrowIfNull(title);
            ArgumentNullException.ThrowIfNull(item);

            this.Id = id;
            this.Title = title;
            this.Item = item;
        }

        /// <summary>
        /// Gets the tab identity.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the captured tab title.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Gets the exact tab instance supplied to the existing editor-content contract.
        /// </summary>
        public EditorTabItem Item { get; }
    }
}
