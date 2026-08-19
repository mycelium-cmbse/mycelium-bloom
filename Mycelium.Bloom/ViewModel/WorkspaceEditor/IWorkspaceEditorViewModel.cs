// ------------------------------------------------------------------------------------------------
// <copyright file="IWorkspaceEditorViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.WorkspaceEditor
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;

    using Mycelium.Bloom.Model;

    /// <summary>
    /// Defines rendering-independent editor-group and tab state for one workspace.
    /// </summary>
    public interface IWorkspaceEditorViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets the application-configured maximum number of editor groups supported by the workspace.
        /// </summary>
        int MaximumGroupCount { get; }

        /// <summary>
        /// Gets the ordered, read-only collection of editor groups.
        /// </summary>
        ReadOnlyObservableCollection<EditorGroupViewModel> Groups { get; }

        /// <summary>
        /// Gets the editor group that currently has logical workspace focus.
        /// </summary>
        EditorGroupViewModel FocusedGroup { get; }

        /// <summary>
        /// Attempts to append and focus a new empty editor group.
        /// </summary>
        /// <param name="group">
        /// The created group when the group limit permits creation; otherwise, <see langword="null" />.
        /// </param>
        /// <returns>
        /// <see langword="true" /> when a group was created; otherwise, <see langword="false" />.
        /// </returns>
        bool TryAddGroup(out EditorGroupViewModel group);

        /// <summary>
        /// Attempts to create, append, activate, and focus a tab in an existing editor group.
        /// </summary>
        /// <param name="groupId">The identity of the group that will own the tab.</param>
        /// <param name="title">The title presented for the tab.</param>
        /// <param name="viewTypeKey">The rendering-neutral key identifying the kind of view.</param>
        /// <param name="tab">
        /// The created tab when the target group exists; otherwise, <see langword="null" />.
        /// </param>
        /// <returns>
        /// <see langword="true" /> when a tab was opened; otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="title" /> or <paramref name="viewTypeKey" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="title" /> or <paramref name="viewTypeKey" /> is empty or consists only of
        /// whitespace.
        /// </exception>
        bool TryOpenTab(
            Guid groupId,
            string title,
            string viewTypeKey,
            out EditorTabItem tab);

        /// <summary>
        /// Attempts to activate an owned tab and focus its editor group.
        /// </summary>
        /// <param name="groupId">The identity of the group expected to own the tab.</param>
        /// <param name="tabId">The identity of the tab to activate.</param>
        /// <returns>
        /// <see langword="true" /> when the group owns the tab; otherwise, <see langword="false" />.
        /// </returns>
        bool ActivateTab(Guid groupId, Guid tabId);

        /// <summary>
        /// Attempts to give an editor group logical workspace focus.
        /// </summary>
        /// <param name="groupId">The identity of the group to focus.</param>
        /// <returns>
        /// <see langword="true" /> when the group exists; otherwise, <see langword="false" />.
        /// </returns>
        bool FocusGroup(Guid groupId);

        /// <summary>
        /// Attempts to close a tab and reconcile its group's active tab and workspace membership.
        /// </summary>
        /// <param name="groupId">The identity of the group expected to own the tab.</param>
        /// <param name="tabId">The identity of the tab to close.</param>
        /// <returns>
        /// <see langword="true" /> when the group owned and closed the tab; otherwise, <see langword="false" />.
        /// </returns>
        bool CloseTab(Guid groupId, Guid tabId);

        /// <summary>
        /// Attempts to transfer a tab between editor groups and focus its destination.
        /// </summary>
        /// <param name="sourceGroupId">The identity of the group expected to own the tab.</param>
        /// <param name="tabId">The identity of the tab to transfer.</param>
        /// <param name="destinationGroupId">The identity of the group that will receive the tab.</param>
        /// <returns>
        /// <see langword="true" /> when the tab was transferred between distinct groups; otherwise,
        /// <see langword="false" />.
        /// </returns>
        bool MoveTab(Guid sourceGroupId, Guid tabId, Guid destinationGroupId);
    }
}
