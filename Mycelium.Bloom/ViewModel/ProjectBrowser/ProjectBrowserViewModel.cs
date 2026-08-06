// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserViewModel.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.ProjectBrowser
{
    using System.Collections.ObjectModel;
    using System.Globalization;

    using DynamicData;
    using DynamicData.Binding;

    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Model.Enum;

    using ReactiveUI;
    using ReactiveUI.Primitives;
    using ReactiveUI.Primitives.Disposables;

    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Core.Types;
    using SysML2.NET.Core.POCO.Root.Annotations;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.DefinitionAndUsage;

    /// <summary>
    /// Provides tree state and tree-building logic for the project browser.
    /// </summary>
    public sealed class ProjectBrowserViewModel : BloomBaseViewModel, IProjectBrowserViewModel
    {
        /// <summary>
        /// The model loader service used to retrieve SysML models.
        /// </summary>
        private readonly IModelLoaderService modelLoaderService;

        /// <summary>
        /// The shared element selection service.
        /// </summary>
        private readonly IElementSelectionService elementSelectionService;

        /// <summary>
        /// The set of node identifiers already assigned in the current project browser tree.
        /// </summary>
        private readonly HashSet<string> nodeIds = new(StringComparer.Ordinal);

        /// <summary>
        /// Maps source element object identities to their visual project browser nodes.
        /// </summary>
        private readonly Dictionary<IElement, ProjectBrowserNodeViewModel> elementNodes =
            new(ReferenceEqualityComparer.Instance);

        /// <summary>
        /// Owns the root node collection and publishes batched root changes.
        /// </summary>
        private readonly SourceList<ProjectBrowserNodeViewModel> rootNodeSource = new();

        /// <summary>
        /// The read-only root node collection bound from <see cref="rootNodeSource" />.
        /// </summary>
        private readonly ReadOnlyObservableCollection<ProjectBrowserNodeViewModel> rootNodes;

        /// <summary>
        /// Owns commands and subscriptions that live until final view model disposal.
        /// </summary>
        private readonly MultipleDisposable lifetimeDisposables = new();

        /// <summary>
        /// The visual projection of the globally selected element.
        /// </summary>
        private ProjectBrowserNodeViewModel selectedNode;

        /// <summary>
        /// Cancels initialization when the view model is no longer active.
        /// </summary>
        private CancellationDisposable initializationCancellation;

        /// <summary>
        /// A value indicating whether final view model disposal has occurred.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectBrowserViewModel" /> class.
        /// </summary>
        /// <param name="modelLoaderService">The model loader service used to retrieve SysML models.</param>
        /// <param name="elementSelectionService">The shared element selection service.</param>
        public ProjectBrowserViewModel(
            IModelLoaderService modelLoaderService,
            IElementSelectionService elementSelectionService)
        {
            ArgumentNullException.ThrowIfNull(modelLoaderService);
            ArgumentNullException.ThrowIfNull(elementSelectionService);

            this.modelLoaderService = modelLoaderService;
            this.elementSelectionService = elementSelectionService;
            this.Activator = new ViewModelActivator();
            this.InitializeCommand = ReactiveCommand.CreateFromTask(this.InitializeAsync);
            this.ToggleNodeCommand = ReactiveCommand.Create<ProjectBrowserNodeViewModel>(ToggleNode);
            this.SelectNodeCommand = ReactiveCommand.Create<ProjectBrowserNodeViewModel>(this.SelectNode);

            System.ObservableExtensions
                .Subscribe(
                    this.rootNodeSource.Connect().Bind(out var boundRootNodes))
                .DisposeWith(this.lifetimeDisposables);

            this.rootNodes = boundRootNodes;
            this.rootNodeSource.DisposeWith(this.lifetimeDisposables);
            this.InitializeCommand.DisposeWith(this.lifetimeDisposables);
            this.ToggleNodeCommand.DisposeWith(this.lifetimeDisposables);
            this.SelectNodeCommand.DisposeWith(this.lifetimeDisposables);

            System.ObservableExtensions
                .Subscribe(
                    this.InitializeCommand.IsExecuting,
                    this.UpdateLoadingState)
                .DisposeWith(this.lifetimeDisposables);

            System.ObservableExtensions
                .Subscribe(
                    this.InitializeCommand,
                    initialized =>
                    {
                        if (initialized)
                        {
                            this.SetLoaded();
                        }
                    })
                .DisposeWith(this.lifetimeDisposables);

            System.ObservableExtensions
                .Subscribe(
                    this.InitializeCommand.ThrownExceptions,
                    this.HandleInitializationError)
                .DisposeWith(this.lifetimeDisposables);

            this.WhenActivated((MultipleDisposable disposables) =>
            {
                this.initializationCancellation = new CancellationDisposable();
                this.initializationCancellation.DisposeWith(disposables);

                var selectedElementChanges = this.elementSelectionService
                    .WhenAnyValue(service => service.SelectedElement);

                var rootChanges = System.Reactive.Linq.Observable.Select(
                    this.rootNodeSource.Connect(),
                    _ => true);

                System.ObservableExtensions
                    .Subscribe(
                        System.Reactive.Linq.Observable.CombineLatest(
                            selectedElementChanges,
                            rootChanges,
                            (selectedElement, _) => selectedElement),
                        this.ApplySelectedElement)
                    .DisposeWith(disposables);
            });
        }

        /// <inheritdoc />
        public ViewModelActivator Activator { get; }

        /// <summary>
        /// Gets the root nodes displayed by the project browser.
        /// </summary>
        public ReadOnlyObservableCollection<ProjectBrowserNodeViewModel> RootNodes => this.rootNodes;

        /// <summary>
        /// Gets the currently selected node.
        /// </summary>
        public ProjectBrowserNodeViewModel SelectedNode
        {
            get => this.selectedNode;
            private set => this.RaiseAndSetIfChanged(ref this.selectedNode, value);
        }

        /// <inheritdoc />
        public ReactiveCommand<RxVoid, bool> InitializeCommand { get; }

        /// <inheritdoc />
        public ReactiveCommand<ProjectBrowserNodeViewModel, RxVoid> ToggleNodeCommand { get; }

        /// <inheritdoc />
        public ReactiveCommand<ProjectBrowserNodeViewModel, RxVoid> SelectNodeCommand { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;
            this.initializationCancellation?.Dispose();
            this.lifetimeDisposables.Dispose();
        }

        /// <summary>
        /// Initializes the project browser tree from the Quantities SysML model.
        /// </summary>
        /// <param name="commandCancellationToken">Cancels the current command execution.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task<bool> InitializeAsync(CancellationToken commandCancellationToken)
        {
            if (this.IsLoaded)
            {
                return false;
            }

            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                commandCancellationToken,
                this.initializationCancellation?.Token ?? CancellationToken.None);

            var cancellationToken = linkedCancellation.Token;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var model = await Task.Run(
                    this.modelLoaderService.LoadQuantitiesModel,
                    cancellationToken).WaitAsync(cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                this.InitializeTree(model, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                this.SelectDefaultRootNode(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                return true;
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                // Deactivation cancels initialization without presenting a loading failure.
                return false;
            }
        }

        /// <summary>
        /// Builds and exposes the project browser tree from the provided SysML namespace.
        /// </summary>
        /// <param name="model">The loaded SysML namespace model.</param>
        /// <param name="cancellationToken">Cancels tree replacement.</param>
        private void InitializeTree(INamespace model, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(model);
            cancellationToken.ThrowIfCancellationRequested();

            this.ClearTreeIndexesAndOwnedSelection();

            var rootNode = this.BuildNode(model, "root");

            cancellationToken.ThrowIfCancellationRequested();
            this.EditRootNodes(nodes =>
            {
                nodes.Clear();
                nodes.Add(rootNode);
            });
        }

        /// <summary>
        /// Toggles the expanded state of the provided node.
        /// </summary>
        /// <param name="node">The node to expand or collapse.</param>
        private static void ToggleNode(ProjectBrowserNodeViewModel node)
        {
            ArgumentNullException.ThrowIfNull(node);

            if (node.HasChildren)
            {
                node.IsExpanded = !node.IsExpanded;
            }
        }

        /// <summary>
        /// Selects the provided node and clears the previous selection.
        /// </summary>
        /// <param name="node">The node to select.</param>
        private void SelectNode(ProjectBrowserNodeViewModel node)
        {
            ArgumentNullException.ThrowIfNull(node);

            this.elementSelectionService.SelectedElement = node.SourceElement;
        }

        /// <summary>
        /// Selects and expands the first root node when no node is currently selected.
        /// </summary>
        /// <param name="cancellationToken">Cancels default selection.</param>
        private void SelectDefaultRootNode(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (this.RootNodes.Count == 0 || this.elementSelectionService.SelectedElement != null)
            {
                return;
            }

            var rootNode = this.RootNodes[0];

            cancellationToken.ThrowIfCancellationRequested();
            this.SelectNode(rootNode);

            if (rootNode.HasChildren && !rootNode.IsExpanded)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ToggleNode(rootNode);
            }
        }

        /// <summary>
        /// Builds a project browser node from a SysML element.
        /// </summary>
        /// <param name="element">The SysML element represented by the node.</param>
        /// <param name="fallbackId">The fallback identifier used when the element has no identifier.</param>
        /// <returns>The project browser node for the provided SysML element.</returns>
        private ProjectBrowserNodeViewModel BuildNode(IElement element, string fallbackId)
        {
            var runtimeTypeName = element.GetType().Name;
            var elementId = ToDisplayString(element.ElementId);
            var nodeId = this.CreateUniqueNodeId(string.IsNullOrWhiteSpace(elementId) ? fallbackId : elementId);
            var children = this.BuildChildren(element, nodeId);
            var displayName = GetDisplayName(element, runtimeTypeName);
            var qualifiedName = ToDisplayString(element.qualifiedName);
            var elementKind = GetElementKind(element);

            var metadata = new ProjectBrowserNodeMetadata(
                elementId,
                qualifiedName,
                runtimeTypeName,
                elementKind,
                element);

            var node = new ProjectBrowserNodeViewModel(
                nodeId,
                displayName,
                metadata,
                children);

            this.elementNodes.TryAdd(element, node);

            return node;
        }

        /// <summary>
        /// Applies the shared selected element to the visual node projection.
        /// </summary>
        /// <param name="element">The selected element, or <see langword="null" />.</param>
        private void ApplySelectedElement(IElement element)
        {
            ProjectBrowserNodeViewModel node = null;

            if (element != null)
            {
                this.elementNodes.TryGetValue(element, out node);
            }

            if (ReferenceEquals(this.SelectedNode, node))
            {
                return;
            }

            if (this.SelectedNode != null)
            {
                this.SelectedNode.IsSelected = false;
            }

            if (node != null)
            {
                node.IsSelected = true;
            }

            this.SelectedNode = node;
        }

        /// <summary>
        /// Clears the current tree and clears a shared selection owned by that tree.
        /// </summary>
        private void ResetTree()
        {
            this.ClearTreeIndexesAndOwnedSelection();

            if (this.RootNodes.Count > 0)
            {
                this.EditRootNodes(nodes => nodes.Clear());
            }
        }

        /// <summary>
        /// Edits the root collection in one transaction and notifies reactive component observers.
        /// </summary>
        /// <param name="editAction">The batched root-node edit.</param>
        private void EditRootNodes(Action<IExtendedList<ProjectBrowserNodeViewModel>> editAction)
        {
            this.RaisePropertyChanging(nameof(this.RootNodes));
            this.rootNodeSource.Edit(editAction);
            this.RaisePropertyChanged(nameof(this.RootNodes));
        }

        /// <summary>
        /// Clears visual lookup indexes and clears a shared selection owned by the current tree.
        /// </summary>
        private void ClearTreeIndexesAndOwnedSelection()
        {
            var selectedElement = this.elementSelectionService.SelectedElement;
            var shouldClearSelection = selectedElement != null && this.elementNodes.ContainsKey(selectedElement);

            this.nodeIds.Clear();
            this.elementNodes.Clear();

            if (shouldClearSelection)
            {
                this.elementSelectionService.SelectedElement = null;
            }
        }

        /// <summary>
        /// Updates loading state from <see cref="InitializeCommand" /> execution state.
        /// </summary>
        /// <param name="isExecuting">A value indicating whether initialization is executing.</param>
        private void UpdateLoadingState(bool isExecuting)
        {
            if (isExecuting)
            {
                this.StartLoading();
                return;
            }

            this.StopLoading();
        }

        /// <summary>
        /// Resets the tree and exposes a genuine initialization failure.
        /// </summary>
        /// <param name="exception">The initialization failure.</param>
        private void HandleInitializationError(Exception exception)
        {
            this.ResetTree();
            this.SetError(exception.Message);
        }

        /// <summary>
        /// Builds child project browser nodes from the owned elements of a SysML element.
        /// </summary>
        /// <param name="element">The SysML element whose owned elements should be mapped.</param>
        /// <param name="parentNodeId">The identifier of the parent project browser node.</param>
        /// <returns>The child project browser nodes for the provided SysML element.</returns>
        private List<ProjectBrowserNodeViewModel> BuildChildren(IElement element, string parentNodeId)
        {
            var children = new List<ProjectBrowserNodeViewModel>();

            if (element.ownedElement == null)
            {
                return children;
            }

            var index = 0;

            foreach (var childElement in element.ownedElement)
            {
                if (childElement != null)
                {
                    children.Add(this.BuildNode(childElement, string.Create(
                        CultureInfo.InvariantCulture,
                        $"{parentNodeId}/{index}")));
                }

                index++;
            }

            return children;
        }

        /// <summary>
        /// Creates an identifier that is unique within the current project browser tree.
        /// </summary>
        /// <param name="preferredId">The preferred identifier for the node.</param>
        /// <returns>A unique project browser node identifier.</returns>
        private string CreateUniqueNodeId(string preferredId)
        {
            if (this.nodeIds.Add(preferredId))
            {
                return preferredId;
            }

            var suffix = 2;
            var candidateId = string.Create(CultureInfo.InvariantCulture, $"{preferredId}-{suffix}");

            while (!this.nodeIds.Add(candidateId))
            {
                suffix++;
                candidateId = string.Create(CultureInfo.InvariantCulture, $"{preferredId}-{suffix}");
            }

            return candidateId;
        }

        /// <summary>
        /// Gets the best available display name for a SysML element.
        /// </summary>
        /// <param name="element">The SysML element to describe.</param>
        /// <param name="runtimeTypeName">The runtime type name used when the element has no display name.</param>
        /// <returns>The display name for the SysML element.</returns>
        private static string GetDisplayName(IElement element, string runtimeTypeName)
        {
            var declaredName = ToDisplayString(element.DeclaredName);

            if (!string.IsNullOrWhiteSpace(declaredName))
            {
                return declaredName;
            }

            var name = ToDisplayString(element.name);

            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            var qualifiedName = ToDisplayString(element.qualifiedName);

            if (!string.IsNullOrWhiteSpace(qualifiedName))
            {
                return qualifiedName;
            }

            return runtimeTypeName;
        }

        /// <summary>
        /// Gets the broad SysML model element kind for a SysML element.
        /// </summary>
        /// <param name="element">The SysML element.</param>
        /// <returns>The inferred SysML model element kind.</returns>
        private static SysmlModelElementKind GetElementKind(IElement element)
        {
            var elementKind = element switch
            {
                IDocumentation or IComment or IAnnotation or IAnnotatingElement => SysmlModelElementKind.Annotation,
                IImport => SysmlModelElementKind.Import,
                IMembership => SysmlModelElementKind.Membership,
                IRelationship => SysmlModelElementKind.Relationship,
                IDefinition => SysmlModelElementKind.Definition,
                IUsage => SysmlModelElementKind.Usage,
                IFeature => SysmlModelElementKind.Feature,
                IType => SysmlModelElementKind.Type,
                INamespace => SysmlModelElementKind.Namespace,
                _ => SysmlModelElementKind.Unknown
            };

            return elementKind;
        }

        /// <summary>
        /// Converts a SysML SDK value into an invariant display string.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The display string, or an empty string when the value cannot be converted.</returns>
        private static string ToDisplayString(object value)
        {
            var displayString = Convert.ToString(value, CultureInfo.InvariantCulture);

            return displayString ?? string.Empty;
        }
    }
}
