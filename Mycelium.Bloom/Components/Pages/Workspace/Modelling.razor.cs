// ------------------------------------------------------------------------------------------------
// <copyright file="Modelling.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.Pages.Workspace
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;
    using Mycelium.Bloom.ViewModel.WorkspaceEditor;

    /// <summary>
    /// Composes the routed engineering editor and owns its feature-specific ViewModels.
    /// </summary>
    public sealed partial class Modelling : ComponentBase, IDisposable
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
        /// The composition-owned action that opens independent Project Browser content.
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
        /// The Project Browser ViewModels owned by their exact durable editor-tab identities.
        /// </summary>
        private readonly Dictionary<Guid, IProjectBrowserViewModel> projectBrowserViewModels = [];

        /// <summary>
        /// The initial-only presentation weights supplied to the editor workspace.
        /// </summary>
        private IReadOnlyDictionary<Guid, double> initialGroupWeights = new Dictionary<Guid, double>();

        /// <summary>
        /// A value indicating whether final component disposal has occurred.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// The number assigned to the next generic placeholder tab.
        /// </summary>
        private int nextPlaceholderTabNumber;

        /// <summary>
        /// Gets or sets the factory that creates editor state owned by this routed page.
        /// </summary>
        [Inject]
        private Func<IWorkspaceEditorViewModel> WorkspaceEditorViewModelFactory { get; set; }

        /// <summary>
        /// Gets or sets the factory that creates Project Browser state owned by an editor tab.
        /// </summary>
        [Inject]
        private Func<IProjectBrowserViewModel> ProjectBrowserViewModelFactory { get; set; }

        /// <summary>
        /// Gets the durable editor state owned by this routed editor page.
        /// </summary>
        private IWorkspaceEditorViewModel WorkspaceEditorViewModel { get; set; }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            base.OnInitialized();

            ArgumentNullException.ThrowIfNull(this.WorkspaceEditorViewModelFactory);
            ArgumentNullException.ThrowIfNull(this.ProjectBrowserViewModelFactory);

            this.WorkspaceEditorViewModel = this.WorkspaceEditorViewModelFactory()
                ?? throw new InvalidOperationException("The Workspace Editor ViewModel factory returned null.");

            try
            {
                this.InitializeExistingProjectBrowserOwnership();
                this.InitializePlaceholderWorkspace();
            }
            catch
            {
                this.Dispose();

                throw;
            }
        }

        /// <summary>
        /// Releases every Project Browser ViewModel still owned by this composition.
        /// </summary>
        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;
            this.DisposeProjectBrowserViewModels();
            this.WorkspaceEditorViewModel?.Dispose();
        }

        /// <summary>
        /// Creates ownership for Project Browser tabs already present in durable workspace state.
        /// </summary>
        private void InitializeExistingProjectBrowserOwnership()
        {
            foreach (var tabId in this.WorkspaceEditorViewModel.RenderState.Groups
                         .SelectMany(group => group.Tabs)
                         .Select(tab => tab.Item)
                         .Where(IsProjectBrowserTab)
                         .Select(tab => tab.Id))
            {
                var viewModel = this.CreateProjectBrowserViewModel();

                if (!this.projectBrowserViewModels.TryAdd(tabId, viewModel))
                {
                    viewModel.Dispose();

                    throw new InvalidOperationException(
                        $"Project Browser ownership already exists for editor tab '{tabId}'.");
                }
            }
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
        private static ActionMenuItem[] CreateAddTabActions()
        {
            return EditorTypes
                .Select(editorType => new ActionMenuItem
                {
                    Id = editorType.ActionId,
                    Label = editorType.Label,
                    Symbol = editorType.Symbol
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
        /// Resets composition-owned placeholder numbering only after a successful close leaves the entire workspace empty.
        /// </summary>
        /// <param name="tab">The exact tab successfully closed by the editor workspace.</param>
        private void HandleTabClosed(EditorTabItem tab)
        {
            ArgumentNullException.ThrowIfNull(tab);

            if (IsProjectBrowserTab(tab)
                && this.projectBrowserViewModels.Remove(tab.Id, out var viewModel))
            {
                viewModel.Dispose();
            }

            if (!this.WorkspaceEditorViewModel.RenderState.Groups
                .SelectMany(group => group.Tabs)
                .Any())
            {
                this.nextPlaceholderTabNumber = 1;
            }
        }

        /// <summary>
        /// Attempts to open independently owned Project Browser content in one workspace group.
        /// </summary>
        /// <param name="groupId">The target editor group.</param>
        /// <returns><see langword="true" /> when the Project Browser tab was opened; otherwise, <see langword="false" />.</returns>
        private bool OpenProjectBrowserTab(Guid groupId)
        {
            var viewModel = this.CreateProjectBrowserViewModel();
            var ownershipTransferred = false;

            try
            {
                if (!this.WorkspaceEditorViewModel.TryOpenTab(
                        groupId,
                        "Project Browser",
                        ProjectBrowserViewTypeKey,
                        out var tab))
                {
                    return false;
                }

                if (!this.projectBrowserViewModels.TryAdd(tab.Id, viewModel))
                {
                    _ = this.WorkspaceEditorViewModel.CloseTab(groupId, tab.Id);

                    return false;
                }

                ownershipTransferred = true;

                return true;
            }
            finally
            {
                if (!ownershipTransferred)
                {
                    viewModel.Dispose();
                }
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
        /// Checks whether an editor tab selects Project Browser content.
        /// </summary>
        /// <param name="tab">The durable editor tab state.</param>
        /// <returns><see langword="true" /> when the Project Browser should be rendered.</returns>
        private static bool IsProjectBrowserTab(EditorTabItem tab)
        {
            return string.Equals(tab.ViewTypeKey, ProjectBrowserViewTypeKey, StringComparison.Ordinal);
        }

        /// <summary>
        /// Retrieves the exact Project Browser ViewModel owned by one durable editor tab.
        /// </summary>
        /// <param name="tab">The durable Project Browser tab.</param>
        /// <param name="viewModel">The caller-owned ViewModel associated with the tab.</param>
        /// <returns><see langword="true" /> when the tab has an owned Project Browser ViewModel.</returns>
        private bool TryGetProjectBrowserViewModel(
            EditorTabItem tab,
            out IProjectBrowserViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(tab);

            return this.projectBrowserViewModels.TryGetValue(tab.Id, out viewModel);
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
        /// Creates one Project Browser ViewModel owned by this composition.
        /// </summary>
        /// <returns>The fresh caller-owned ViewModel.</returns>
        private IProjectBrowserViewModel CreateProjectBrowserViewModel()
        {
            return this.ProjectBrowserViewModelFactory()
                ?? throw new InvalidOperationException("The Project Browser ViewModel factory returned null.");
        }

        /// <summary>
        /// Disposes and forgets all Project Browser ViewModels still owned by this composition.
        /// </summary>
        private void DisposeProjectBrowserViewModels()
        {
            foreach (var viewModel in this.projectBrowserViewModels.Values)
            {
                viewModel.Dispose();
            }

            this.projectBrowserViewModels.Clear();
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
        /// Captures the semantic and visual metadata owned by the Modelling composition for one supported editor type.
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
