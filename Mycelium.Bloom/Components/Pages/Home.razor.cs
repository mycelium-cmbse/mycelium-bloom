// ------------------------------------------------------------------------------------------------
// <copyright file="Home.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.Pages
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.NavigationRail;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;
    using Mycelium.Bloom.ViewModel.WorkspaceEditor;

    /// <summary>
    /// Composes the full-application Bloom workspace from reusable structural components.
    /// </summary>
    public partial class Home : ComponentBase
    {
        /// <summary>
        /// The number of editor groups represented by the native desktop design when configuration permits it.
        /// </summary>
        private const int DefaultEditorGroupCount = 3;

        /// <summary>
        /// The composition-owned key used by the Project Browser editor.
        /// </summary>
        private const string ProjectBrowserViewTypeKey = "project-browser";

        /// <summary>
        /// The rendering-neutral key used by structural editor placeholders.
        /// </summary>
        private const string PlaceholderViewTypeKey = "placeholder";

        /// <summary>
        /// The Figma-derived relative weights for the default three-group composition.
        /// </summary>
        private static readonly double[] DefaultEditorGroupWeights = [300d, 320d, 868d];

        /// <summary>
        /// The initial-only presentation weights supplied to the editor workspace.
        /// </summary>
        private IReadOnlyDictionary<Guid, double> initialGroupWeights = new Dictionary<Guid, double>();

        /// <summary>
        /// A value indicating whether the shell currently reserves the collapsed navigation width.
        /// </summary>
        private bool isNavigationCollapsed = true;

        /// <summary>
        /// The number assigned to the next generic placeholder tab.
        /// </summary>
        private int nextPlaceholderTabNumber;

        /// <summary>
        /// Gets or sets the navigation state resolved once by the workspace composition root.
        /// </summary>
        [Inject]
        private INavigationRailViewModel NavigationViewModel { get; set; }

        /// <summary>
        /// Gets or sets the Project Browser state retained by the workspace composition root.
        /// </summary>
        [Inject]
        private IProjectBrowserViewModel ProjectBrowserViewModel { get; set; }

        /// <summary>
        /// Gets or sets the durable editor state resolved once by the workspace composition root.
        /// </summary>
        [Inject]
        private IWorkspaceEditorViewModel WorkspaceEditorViewModel { get; set; }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            base.OnInitialized();

            ArgumentNullException.ThrowIfNull(this.NavigationViewModel);
            ArgumentNullException.ThrowIfNull(this.ProjectBrowserViewModel);
            ArgumentNullException.ThrowIfNull(this.WorkspaceEditorViewModel);

            this.isNavigationCollapsed = this.NavigationViewModel.PresentationMode switch
            {
                NavigationRailPresentationMode.Expanded => false,
                NavigationRailPresentationMode.Collapsed => true,
                NavigationRailPresentationMode.ExpandOnHover => true,
                _ => throw CreateInvalidPresentationModeException(
                    this.NavigationViewModel.PresentationMode)
            };

            this.InitializePlaceholderWorkspace();
        }

        /// <summary>
        /// Creates the Figma-sized structural group set only for a fresh workspace and within its configured limit.
        /// </summary>
        private void InitializePlaceholderWorkspace()
        {
            var groups = this.WorkspaceEditorViewModel.Groups;
            this.nextPlaceholderTabNumber = groups.Sum(group => group.Tabs.Count) + 1;

            if (groups.Count != 1 || groups[0].Tabs.Count != 0)
            {
                return;
            }

            var requestedGroupCount = Math.Min(
                DefaultEditorGroupCount,
                this.WorkspaceEditorViewModel.MaximumGroupCount);

            this.OpenProjectBrowserTab(groups[0].Id);

            while (groups.Count < requestedGroupCount
                   && this.WorkspaceEditorViewModel.TryAddGroup(out var group))
            {
                this.OpenPlaceholderTab(group.Id);
            }

            if (groups.Count == DefaultEditorGroupCount)
            {
                this.initialGroupWeights = groups
                    .Select((group, index) => new KeyValuePair<Guid, double>(
                        group.Id,
                        DefaultEditorGroupWeights[index]))
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
            }
        }

        /// <summary>
        /// Handles one per-group request by opening generic structural content in the exact owning group.
        /// </summary>
        /// <param name="groupId">The group requesting a placeholder tab.</param>
        private void HandleAddTabRequested(Guid groupId)
        {
            this.OpenPlaceholderTab(groupId);
        }

        /// <summary>
        /// Updates component-local shell width presentation from the rail's effective state.
        /// </summary>
        /// <param name="isCollapsed">Whether the rail is currently presented as collapsed.</param>
        private void HandleNavigationCollapsedChanged(bool isCollapsed)
        {
            this.isNavigationCollapsed = isCollapsed;
        }

        /// <summary>
        /// Attempts to open the retained Project Browser content in the first workspace group.
        /// </summary>
        /// <param name="groupId">The target editor group.</param>
        private void OpenProjectBrowserTab(Guid groupId)
        {
            if (this.WorkspaceEditorViewModel.TryOpenTab(
                    groupId,
                    "Project Browser",
                    ProjectBrowserViewTypeKey,
                    out _))
            {
                this.nextPlaceholderTabNumber++;
            }
        }

        /// <summary>
        /// Attempts to open one rendering-neutral placeholder tab in a workspace-owned group.
        /// </summary>
        /// <param name="groupId">The target editor group.</param>
        private void OpenPlaceholderTab(Guid groupId)
        {
            var title = $"Editor {this.nextPlaceholderTabNumber}";

            if (this.WorkspaceEditorViewModel.TryOpenTab(
                    groupId,
                    title,
                    PlaceholderViewTypeKey,
                    out _))
            {
                this.nextPlaceholderTabNumber++;
            }
        }

        /// <summary>
        /// Checks whether an editor tab selects the retained Project Browser content.
        /// </summary>
        /// <param name="tab">The durable editor tab state.</param>
        /// <returns><see langword="true" /> when the Project Browser should be rendered.</returns>
        private static bool IsProjectBrowserTab(EditorTabItem tab)
        {
            return string.Equals(tab.ViewTypeKey, ProjectBrowserViewTypeKey, StringComparison.Ordinal);
        }

        /// <summary>
        /// Creates the exception raised when a navigation ViewModel exposes an unsupported presentation mode.
        /// </summary>
        /// <param name="presentationMode">The unsupported presentation mode.</param>
        /// <returns>The exception describing the unsupported value.</returns>
        private static ArgumentOutOfRangeException CreateInvalidPresentationModeException(
            NavigationRailPresentationMode presentationMode)
        {
            return new ArgumentOutOfRangeException(nameof(presentationMode), presentationMode, null);
        }
    }
}
