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
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using ReactiveUI;
    using Microsoft.AspNetCore.Components;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;
    using Mycelium.Bloom.ViewModel.Diagram;
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

        /// <summary>The composition key for the Interconnection editor.</summary>
        private const string DiagramViewTypeKey = "interconnection";

        /// <summary>The exact tab-owned presentation state retained across renderer remounts.</summary>
        private readonly Dictionary<Guid, DiagramViewModel> diagramViewModels = [];

        /// <summary>Owns renderer-dispatched preparation observations for diagram tabs.</summary>
        private readonly Dictionary<Guid, IDisposable> diagramObservations = [];

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
            new(ProjectBrowserActionId, "Project Browser", ProjectBrowserViewTypeKey, SymbolIconName.Tree),
            new("open-interconnection", "Interconnection", DiagramViewTypeKey, SymbolIconName.Document)
        ];

        /// <summary>
        /// Owns URL-restoration observation for this routed page instance.
        /// </summary>
        private readonly CompositeDisposable subscriptions = new();

        /// <summary>Cancels model-browser initialization when the routed page ends.</summary>
        private readonly CancellationTokenSource lifetimeCancellation = new();

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

        /// <summary>Gets or sets the caller-owned diagram state factory.</summary>
        [Inject]
        private Func<DiagramViewModel> DiagramViewModelFactory { get; set; }

        /// <summary>Gets or sets the circuit owner of canonical model contexts.</summary>
        [Inject]
        private WorkspaceModelSession WorkspaceModel { get; set; }

        /// <summary>
        /// Gets or sets the logger used for renderer-bound focus failures.
        /// </summary>
        [Inject]
        private ILogger<Modelling> Logger { get; set; }

        /// <summary>
        /// Gets or sets the layout-owned URL context when rendered in the workspace route hierarchy.
        /// </summary>
        [CascadingParameter]
        private IWorkspaceUrlContextService UrlContextService { get; set; }

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
            ArgumentNullException.ThrowIfNull(this.Logger);

            this.WorkspaceEditorViewModel = this.WorkspaceEditorViewModelFactory()
                                            ?? throw new InvalidOperationException(
                                                "The Workspace Editor ViewModel factory returned null.");

            try
            {
                this.InitializeExistingProjectBrowserOwnership();
                this.InitializePlaceholderWorkspace();
                this.ObserveUrlRestorations();
                this.subscriptions.Add(this.WorkspaceModel.WhenAnyValue(session => session.Current)
                    .Select(snapshot =>
                        Observable.FromAsync(() => this.InvokeAsync(() => this.UpdateWorkspace(snapshot))))
                    .Concat().Subscribe(_ => { },
                        exception => this.Logger.LogError(exception, "Workspace model observation failed.")));
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
            this.lifetimeCancellation.Cancel();
            this.lifetimeCancellation.Dispose();
            this.subscriptions.Dispose();
            this.DisposeProjectBrowserViewModels();
            foreach (var diagram in this.diagramViewModels.Values)
            {
                diagram.Dispose();
            }

            this.diagramViewModels.Clear();
            foreach (var observation in this.diagramObservations.Values)
            {
                observation.Dispose();
            }

            this.diagramObservations.Clear();
            foreach (var operation in this.modelOpenings.Values)
            {
                operation.Cancel();
            }

            foreach (var lease in this.tabModels.Values)
            {
                lease.Dispose();
            }

            this.tabModels.Clear();
            this.WorkspaceEditorViewModel?.Dispose();
        }

        /// <summary>
        /// Observes URL restorations supplied by the containing workspace layout.
        /// </summary>
        private void ObserveUrlRestorations()
        {
            if (this.UrlContextService is null)
            {
                return;
            }

            this.subscriptions.Add(
                this.UrlContextService.Restorations
                    .Where(restoration => restoration.SelectedElement is not null
                                          && restoration.ShouldFocusSelectedElement)
                    .Select(restoration => Observable.FromAsync(cancellationToken => this.FocusRestoredElementAsync(
                        restoration.SelectedElement,
                        cancellationToken)))
                    .Concat()
                    .Subscribe(
                        _ => { },
                        exception => this.Logger.LogError(
                            exception,
                            "Project Browser URL focus stopped unexpectedly.")));
        }

        /// <summary>
        /// Focuses the deterministic Project Browser target through the renderer dispatcher.
        /// </summary>
        /// <param name="element">The canonical URL-restored model element.</param>
        /// <param name="cancellationToken">Cancels work after page disposal.</param>
        /// <returns>A task representing renderer dispatch.</returns>
        private Task FocusRestoredElementAsync(
            SysML2.NET.Core.POCO.Root.Elements.IElement element,
            CancellationToken cancellationToken)
        {
            return this.InvokeAsync(() =>
            {
                if (this.isDisposed
                    || cancellationToken.IsCancellationRequested
                    || !this.TryGetRestorationTarget(out var group, out var tab, out var viewModel))
                {
                    return;
                }

                if (this.tabModels.ContainsKey(tab.Id)
                    && !viewModel.RootNodes.Any(root =>
                        ReferenceEquals(root.FindPathTo(element).LastOrDefault()?.SourceElement, element)))
                {
                    var matching = this.projectBrowserViewModels.FirstOrDefault(candidate => candidate.Value.RootNodes
                        .Any(root =>
                            ReferenceEquals(root.FindPathTo(element).LastOrDefault()?.SourceElement, element)));
                    if (matching.Value is null)
                    {
                        return;
                    }

                    group = this.WorkspaceEditorViewModel.Groups.First(candidate =>
                        candidate.Tabs.Any(item => item.Id == matching.Key));
                    tab = group.Tabs.First(item => item.Id == matching.Key);
                    viewModel = matching.Value;
                }

                _ = this.WorkspaceEditorViewModel.ActivateTab(group.Id, tab.Id);
                viewModel.FocusElement(element);
            });
        }

        /// <summary>
        /// Selects exactly one Project Browser according to focused, active, and workspace ordering.
        /// </summary>
        /// <param name="group">The group that owns the selected browser tab.</param>
        /// <param name="tab">The selected browser tab.</param>
        /// <param name="viewModel">The exact ViewModel owned by the selected tab.</param>
        /// <returns><see langword="true" /> when a Project Browser target exists.</returns>
        private bool TryGetRestorationTarget(
            out EditorGroupViewModel group,
            out EditorTabItem tab,
            out IProjectBrowserViewModel viewModel)
        {
            var focusedGroup = this.WorkspaceEditorViewModel.FocusedGroup;

            if (focusedGroup is not null
                && (this.TryGetActiveProjectBrowser(focusedGroup, out tab, out viewModel)
                    || this.TryGetFirstProjectBrowser(focusedGroup, out tab, out viewModel)))
            {
                group = focusedGroup;

                return true;
            }

            foreach (var candidateGroup in this.WorkspaceEditorViewModel.Groups)
            {
                if (this.TryGetActiveProjectBrowser(candidateGroup, out tab, out viewModel))
                {
                    group = candidateGroup;

                    return true;
                }
            }

            foreach (var candidateGroup in this.WorkspaceEditorViewModel.Groups)
            {
                if (this.TryGetFirstProjectBrowser(candidateGroup, out tab, out viewModel))
                {
                    group = candidateGroup;

                    return true;
                }
            }

            group = null;
            tab = null;
            viewModel = null;

            return false;
        }

        /// <summary>
        /// Retrieves the active Project Browser in one group when its exact ViewModel is owned here.
        /// </summary>
        /// <param name="group">The candidate editor group.</param>
        /// <param name="tab">The active Project Browser tab.</param>
        /// <param name="viewModel">The exact ViewModel owned by the tab.</param>
        /// <returns><see langword="true" /> when the active tab is an owned Project Browser.</returns>
        private bool TryGetActiveProjectBrowser(
            EditorGroupViewModel group,
            out EditorTabItem tab,
            out IProjectBrowserViewModel viewModel)
        {
            tab = group.ActiveTab;

            if (tab is not null
                && IsProjectBrowserTab(tab)
                && this.projectBrowserViewModels.TryGetValue(tab.Id, out viewModel))
            {
                return true;
            }

            viewModel = null;

            return false;
        }

        /// <summary>
        /// Retrieves the first Project Browser in one group with an exact owned ViewModel.
        /// </summary>
        /// <param name="group">The candidate editor group.</param>
        /// <param name="tab">The first owned Project Browser tab.</param>
        /// <param name="viewModel">The exact ViewModel owned by the tab.</param>
        /// <returns><see langword="true" /> when an owned Project Browser exists in the group.</returns>
        private bool TryGetFirstProjectBrowser(
            EditorGroupViewModel group,
            out EditorTabItem tab,
            out IProjectBrowserViewModel viewModel)
        {
            foreach (var candidateTab in group.Tabs)
            {
                if (IsProjectBrowserTab(candidateTab)
                    && this.projectBrowserViewModels.TryGetValue(candidateTab.Id, out viewModel))
                {
                    tab = candidateTab;

                    return true;
                }
            }

            tab = null;
            viewModel = null;

            return false;
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
                if (groups.Count == DefaultEditorGroupCount)
                {
                    this.OpenWorkspaceInterconnection(group.Id);
                    this.nextPlaceholderTabNumber++;
                }
                else
                {
                    this.OpenPlaceholderTab(group.Id);
                }
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
                case "open-interconnection":
                    this.OpenWorkspaceInterconnection(groupId);
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
            this.browserScroll.Remove(tab.Id);
            if (this.modelOpenings.Remove(tab.Id, out var opening))
            {
                opening.Cancel();
            }

            this.diagramBrowsers.Remove(tab.Id);
            foreach (var pair in this.diagramBrowsers.Where(pair => pair.Value == tab.Id).ToArray())
            {
                this.diagramBrowsers.Remove(pair.Key);
            }

            foreach (var entry in this.relatedDiagrams
                         .Where(entry => entry.Key.Origin == tab.Id || entry.Value == tab.Id).ToArray())
            {
                this.relatedDiagrams.Remove(entry.Key);
            }

            if (this.diagramObservations.Remove(tab.Id, out var observation))
            {
                observation.Dispose();
            }

            if (this.diagramViewModels.Remove(tab.Id, out var diagram))
            {
                diagram.Dispose();
            }

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

            if (this.tabModels.Remove(tab.Id, out var lease))
            {
                lease.Dispose();
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
            var model = this.FocusedModel();
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
                if (model?.Root is not null)
                {
                    this.tabModels.Add(tab.Id, this.WorkspaceModel.Acquire(model));
                    this.subscriptions.Add(Observable.FromAsync(token => viewModel.InitializeAsync(model.Root, token))
                        .Subscribe(_ => { },
                            error => this.Logger.LogError(error, "Project Browser initialization failed.")));
                }

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

        /// <summary>Forwards browser navigation only to its associated diagram.</summary>
        /// <param name="browserId">The originating Browser tab.</param>
        /// <param name="node">The canonical browser selection.</param>
        /// <returns>The completion of navigation dispatch.</returns>
        private async Task RevealBrowserSelectionAsync(Guid browserId, ProjectBrowserNodeViewModel node)
        {
            var diagramId = this.diagramBrowsers.FirstOrDefault(pair => pair.Value == browserId).Key;
            if (diagramId == Guid.Empty && this.WorkspaceEditorViewModel.FocusedGroup?.ActiveTab is { } focused
                                        && !this.diagramBrowsers.ContainsKey(focused.Id))
            {
                diagramId = focused.Id;
            }

            if (this.diagramViewModels.TryGetValue(diagramId, out var viewModel) && viewModel.IsScopeAvailable
                && viewModel.ContainsCanonicalElement(node.SourceElement))
            {
                await viewModel.RevealRequested.Execute(node.ElementId);
            }
        }

        /// <summary>Opens a structural view bound to the active canonical model root.</summary>
        private void OpenWorkspaceInterconnection(Guid groupId, WorkspaceModelSnapshot model = null)
        {
            var viewModel = this.DiagramViewModelFactory();
            model ??= this.FocusedModel();
            if (model?.Root is not null)
            {
                viewModel.UpdateWorkspace(model);
            }
            else
            {
                viewModel.BindWorkspace(this.WorkspaceModel);
            }

            if (this.WorkspaceEditorViewModel.TryOpenTab(groupId, "Interconnection", DiagramViewTypeKey, out var tab))
            {
                this.diagramViewModels.Add(tab.Id, viewModel);
                if (model?.Root is not null)
                {
                    this.tabModels.Add(tab.Id, this.WorkspaceModel.Acquire(model));
                }

                this.diagramObservations.Add(tab.Id,
                    viewModel.WhenAnyValue(value => value.State, value => value.PreparationStatus,
                            value => value.IsScopeAvailable)
                        .Select(value => (Ready: value.Item1 is not null, value.Item2, value.Item3))
                        .DistinctUntilChanged()
                        .Select(_ => Observable.FromAsync(() => this.InvokeAsync(() =>
                        {
                            if (!this.isDisposed && this.diagramViewModels.ContainsKey(tab.Id))
                            {
                                this.StateHasChanged();
                            }
                        }))).Concat().Subscribe(_ => { },
                            error => this.Logger.LogError(error, "Diagram preparation display failed.")));
                this.subscriptions.Add(Observable.FromAsync(() => this.WorkspaceModel.EnsureLoadedAsync())
                    .Subscribe(_ => { },
                        error => this.Logger.LogError(error, "Workspace model initialization failed.")));
            }
            else
            {
                viewModel.Dispose();
            }
        }

        /// <summary>Applies context changes on the renderer and releases obsolete browser state.</summary>
        private void UpdateWorkspace(WorkspaceModelSnapshot snapshot)
        {
            if (this.isDisposed || this.WorkspaceModel.IsDisposed ||
                !ReferenceEquals(snapshot, this.WorkspaceModel.Current))
            {
                return;
            }

            if (snapshot.Root is not null)
            {
                foreach (var id in this.projectBrowserViewModels.Keys.ToArray())
                {
                    if (!this.tabModels.ContainsKey(id))
                    {
                        this.tabModels.Add(id, this.WorkspaceModel.Acquire(snapshot));
                    }
                }

                foreach (var entry in this.diagramViewModels.Where(entry => entry.Value.IsWorkspaceBound))
                {
                    if (!this.tabModels.ContainsKey(entry.Key))
                    {
                        this.tabModels.Add(entry.Key, this.WorkspaceModel.Acquire(snapshot));
                    }
                }
            }

            this.StateHasChanged();
        }

        /// <summary>Describes unavailable or unsupported scopes without substituting another model.</summary>
        private static string DiagramStatus(DiagramViewModel viewModel) =>
            viewModel.IsScopeAvailable && viewModel.State is null
                ? viewModel.PreparationStatus
                : viewModel.WorkspaceStatus switch
                {
                    WorkspaceModelStatus.Loading => "Loading model…",
                    WorkspaceModelStatus.Empty => "No model loaded",
                    WorkspaceModelStatus.Failed => "The workspace model could not be loaded.",
                    WorkspaceModelStatus.Unavailable =>
                        "This diagram’s model is no longer active. Its layout and scope are retained; open a new tab for the active model.",
                    _ =>
                        "This model has no supported parts for an Interconnection view. Open a compatible model or choose the example to get started."
                };

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