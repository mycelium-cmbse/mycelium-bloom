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
        /// The composition-owned action that creates generic empty editor content.
        /// </summary>
        private const string EmptyEditorActionId = "empty-editor";

        /// <summary>
        /// The composition-owned action that opens the retained Project Browser content.
        /// </summary>
        private const string ProjectBrowserActionId = "open-project-browser";

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
        /// The composition-owned presentation metadata for editor kinds available in this workspace.
        /// </summary>
        private static readonly EditorTypePresentation[] EditorTypes =
        [
            new(EmptyEditorActionId, "Empty editor", PlaceholderViewTypeKey, SymbolIconName.Document),
            new(ProjectBrowserActionId, "Project Browser", ProjectBrowserViewTypeKey, SymbolIconName.Tree)
        ];

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

            if (this.OpenProjectBrowserTab(groups[0].Id))
            {
                this.nextPlaceholderTabNumber++;
            }

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
        /// Creates the generic add-tab actions from the workspace's current coherent rendering state.
        /// </summary>
        /// <returns>The actions available to every editor group.</returns>
        private ActionMenuItem[] CreateAddTabActions()
        {
            return EditorTypes
                .Select(editorType => new ActionMenuItem
                {
                    Id = editorType.ActionId,
                    Label = editorType.Label,
                    Symbol = editorType.Symbol,
                    Disabled = editorType.ActionId == ProjectBrowserActionId
                        && this.HasProjectBrowserTab()
                })
                .ToArray();
        }

        /// <summary>
        /// Handles one composition-owned add-tab action for the exact requesting group.
        /// </summary>
        /// <param name="groupId">The group requesting new editor content.</param>
        /// <param name="item">The selected generic action metadata.</param>
        private void HandleAddTabActionSelected(Guid groupId, ActionMenuItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            switch (item.Id)
            {
                case EmptyEditorActionId:
                    this.OpenPlaceholderTab(groupId);
                    break;
                case ProjectBrowserActionId:
                    _ = this.OpenProjectBrowserTab(groupId);
                    break;
            }
        }

        /// <summary>
        /// Updates component-local shell width presentation from the rail's persistent layout state.
        /// </summary>
        /// <param name="isCollapsed">Whether the rail is currently presented as collapsed.</param>
        private void HandleNavigationLayoutCollapsedChanged(bool isCollapsed)
        {
            this.isNavigationCollapsed = isCollapsed;
        }

        /// <summary>
        /// Resets composition-owned placeholder numbering only after a successful close leaves the entire workspace empty.
        /// </summary>
        /// <param name="tab">The exact tab successfully closed by the editor workspace.</param>
        private void HandleTabClosed(EditorTabItem tab)
        {
            ArgumentNullException.ThrowIfNull(tab);

            if (!this.WorkspaceEditorViewModel.RenderState.Groups
                .SelectMany(group => group.Tabs)
                .Any())
            {
                this.nextPlaceholderTabNumber = 1;
            }
        }

        /// <summary>
        /// Attempts to open the retained Project Browser content in one workspace group when it is not already open.
        /// </summary>
        /// <param name="groupId">The target editor group.</param>
        /// <returns><see langword="true" /> when the Project Browser tab was opened; otherwise, <see langword="false" />.</returns>
        private bool OpenProjectBrowserTab(Guid groupId)
        {
            if (this.HasProjectBrowserTab())
            {
                return false;
            }

            return this.WorkspaceEditorViewModel.TryOpenTab(
                groupId,
                "Project Browser",
                ProjectBrowserViewTypeKey,
                out _);
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
        /// Gets the composition-owned Lucide icon name for one immutable editor tab type.
        /// </summary>
        /// <param name="tab">The canonical editor tab.</param>
        /// <returns>The icon name, or an empty string when the tab type has no composition metadata.</returns>
        private static string GetEditorTabIconName(EditorTabItem tab)
        {
            ArgumentNullException.ThrowIfNull(tab);

            var editorType = EditorTypes.FirstOrDefault(candidate => string.Equals(
                candidate.ViewTypeKey,
                tab.ViewTypeKey,
                StringComparison.Ordinal));

            return editorType?.Symbol.ToLucideName() ?? string.Empty;
        }

        /// <summary>
        /// Checks the current coherent workspace snapshot for the retained Project Browser tab.
        /// </summary>
        /// <returns><see langword="true" /> when the Project Browser exists anywhere in the workspace.</returns>
        private bool HasProjectBrowserTab()
        {
            return this.WorkspaceEditorViewModel.RenderState.Groups
                .SelectMany(group => group.Tabs)
                .Any(tab => IsProjectBrowserTab(tab.Item));
        }

        /// <summary>
        /// Creates the exact numbered accessible label for one group's add-tab trigger.
        /// </summary>
        /// <param name="groupId">The group represented by the trigger.</param>
        /// <returns>The numbered trigger label.</returns>
        private string GetAddTabAriaLabel(Guid groupId)
        {
            var groups = this.WorkspaceEditorViewModel.RenderState.Groups;

            for (var index = 0; index < groups.Length; index++)
            {
                if (groups[index].Id == groupId)
                {
                    return $"Add tab to Editor group {index + 1}";
                }
            }

            return "Add tab to editor group";
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

        /// <summary>
        /// Captures the semantic and visual metadata owned by the Home composition for one supported editor type.
        /// </summary>
        /// <param name="ActionId">The menu action identifier.</param>
        /// <param name="Label">The editor label.</param>
        /// <param name="ViewTypeKey">The opaque tab view-type key.</param>
        /// <param name="Symbol">The shared menu and tab symbol.</param>
        private sealed record EditorTypePresentation(
            string ActionId,
            string Label,
            string ViewTypeKey,
            SymbolIconName Symbol);
    }
}
