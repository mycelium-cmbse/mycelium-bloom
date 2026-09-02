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
    using System.Collections.Immutable;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;

    using DynamicData;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Model.Enum;

    using ReactiveUI;

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
        /// Serializes filter mutations with canonical tree publication and final disposal.
        /// </summary>
        private readonly object stateMutationGate = new();

        /// <summary>
        /// The set of node identifiers assigned in the current project browser tree.
        /// </summary>
        private readonly HashSet<string> nodeIds = new(StringComparer.Ordinal);

        /// <summary>
        /// Owns the root node collection and publishes batched root changes.
        /// </summary>
        private readonly SourceList<ProjectBrowserNodeViewModel> rootNodeSource = new();

        /// <summary>
        /// The read-only root node collection bound from <see cref="rootNodeSource" />.
        /// </summary>
        private readonly ReadOnlyObservableCollection<ProjectBrowserNodeViewModel> rootNodes;

        /// <summary>
        /// Keeps the DynamicData binding alive until final disposal.
        /// </summary>
        private readonly IDisposable rootNodeBinding;

        /// <summary>
        /// Cancels initialization when final ViewModel disposal begins.
        /// </summary>
        private readonly CancellationTokenSource lifetimeCancellation = new();

        /// <summary>
        /// The committed criteria and their coherent immutable visibility snapshot.
        /// </summary>
        private (
            string FilterText,
            ImmutableHashSet<SysmlModelElementKind> SelectedElementKinds,
            ProjectBrowserFilterPresentation FilterPresentation) filterState =
            (
                string.Empty,
                ImmutableHashSet<SysmlModelElementKind>.Empty,
                ProjectBrowserFilterPresentation.Inactive);

        /// <summary>
        /// The node selected locally in this project browser.
        /// </summary>
        [AllowNull]
        [MaybeNull]
        private ProjectBrowserNodeViewModel selectedNode;

        /// <summary>
        /// A value indicating whether initialization is currently in progress.
        /// </summary>
        private bool isInitializing;

        /// <summary>
        /// A value indicating whether final ViewModel disposal has occurred.
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

            this.rootNodeBinding = System.ObservableExtensions.Subscribe(
                this.rootNodeSource.Connect().Bind(out var boundRootNodes));

            this.rootNodes = boundRootNodes;
        }

        /// <inheritdoc />
        public ReadOnlyObservableCollection<ProjectBrowserNodeViewModel> RootNodes => this.rootNodes;

        /// <inheritdoc />
        public string FilterText
        {
            get
            {
                lock (this.stateMutationGate)
                {
                    return this.filterState.FilterText;
                }
            }

            set
            {
                lock (this.stateMutationGate)
                {
                    if (this.isDisposed)
                    {
                        return;
                    }

                    var nextFilterText = value ?? string.Empty;

                    if (string.Equals(this.filterState.FilterText, nextFilterText, StringComparison.Ordinal))
                    {
                        return;
                    }

                    this.PublishFilterState(nextFilterText, this.filterState.SelectedElementKinds);
                }
            }
        }

        /// <inheritdoc />
        public IReadOnlySet<SysmlModelElementKind> SelectedElementKinds
        {
            get
            {
                lock (this.stateMutationGate)
                {
                    return this.filterState.SelectedElementKinds;
                }
            }
        }

        /// <inheritdoc />
        public ProjectBrowserFilterPresentation FilterPresentation
        {
            get
            {
                lock (this.stateMutationGate)
                {
                    return this.filterState.FilterPresentation;
                }
            }
        }

        /// <inheritdoc />
        [AllowNull]
        [MaybeNull]
        public ProjectBrowserNodeViewModel SelectedNode
        {
            get => this.selectedNode;
            private set => this.RaiseAndSetIfChanged(ref this.selectedNode, value);
        }

        /// <inheritdoc />
        public async Task<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            if (this.isDisposed
                || cancellationToken.IsCancellationRequested
                || this.IsLoaded
                || this.isInitializing)
            {
                return false;
            }

            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                this.lifetimeCancellation.Token);
            var initializationToken = linkedCancellation.Token;
            this.isInitializing = true;
            this.StartLoading();

            try
            {
                var loadingTask = Task.Run(
                    this.modelLoaderService.LoadQuantitiesModel,
                    CancellationToken.None);

                var model = await loadingTask.WaitAsync(initializationToken);

                initializationToken.ThrowIfCancellationRequested();
                ArgumentNullException.ThrowIfNull(model);

                var stagedNodeIds = new HashSet<string>(StringComparer.Ordinal);
                var rootNode = this.BuildNode(
                    model,
                    "root",
                    stagedNodeIds,
                    initializationToken);

                initializationToken.ThrowIfCancellationRequested();

                return this.TryPublishTree(
                    rootNode,
                    stagedNodeIds,
                    initializationToken);
            }
            catch (Exception) when (initializationToken.IsCancellationRequested || this.isDisposed)
            {
                return false;
            }
            catch (Exception exception)
            {
                this.HandleInitializationError(exception);

                return false;
            }
            finally
            {
                if (!this.isDisposed)
                {
                    this.isInitializing = false;
                    this.StopLoading();
                }
            }
        }

        /// <inheritdoc />
        public void ToggleNode(ProjectBrowserNodeViewModel node)
        {
            ArgumentNullException.ThrowIfNull(node);

            lock (this.stateMutationGate)
            {
                if (!this.isDisposed && !this.filterState.FilterPresentation.IsActive && node.HasChildren)
                {
                    node.IsExpanded = !node.IsExpanded;
                }
            }
        }

        /// <inheritdoc />
        public void ClearFilter()
        {
            lock (this.stateMutationGate)
            {
                if (this.isDisposed)
                {
                    return;
                }

                this.PublishFilterState(
                    string.Empty,
                    ImmutableHashSet<SysmlModelElementKind>.Empty);
            }
        }

        /// <inheritdoc />
        public void ToggleElementKindFilter(SysmlModelElementKind elementKind)
        {
            if (!Enum.IsDefined(elementKind))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(elementKind),
                    elementKind,
                    "The element kind must be a defined value.");
            }

            lock (this.stateMutationGate)
            {
                if (this.isDisposed)
                {
                    return;
                }

                var nextSelectedElementKinds = this.filterState.SelectedElementKinds.SymmetricExcept([elementKind]);

                this.PublishFilterState(this.filterState.FilterText, nextSelectedElementKinds);
            }
        }

        /// <inheritdoc />
        public void SelectNode(ProjectBrowserNodeViewModel node)
        {
            ArgumentNullException.ThrowIfNull(node);

            if (!this.isDisposed)
            {
                this.ApplySelectedNode(node);
                this.elementSelectionService.SelectedElement = node.SourceElement;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            lock (this.stateMutationGate)
            {
                if (this.isDisposed)
                {
                    return;
                }

                this.isDisposed = true;
                this.lifetimeCancellation.Cancel();
                this.rootNodeBinding.Dispose();
                this.rootNodeSource.Dispose();
                this.lifetimeCancellation.Dispose();
            }
        }

        /// <summary>
        /// Publishes a completely staged project browser tree.
        /// </summary>
        /// <param name="rootNode">The staged root node.</param>
        /// <param name="stagedNodeIds">The node identifiers assigned while staging.</param>
        /// <param name="cancellationToken">Cancels publication before staged state is exposed.</param>
        /// <returns><see langword="true" /> when the staged tree was published; otherwise, <see langword="false" />.</returns>
        private bool TryPublishTree(
            ProjectBrowserNodeViewModel rootNode,
            HashSet<string> stagedNodeIds,
            CancellationToken cancellationToken)
        {
            lock (this.stateMutationGate)
            {
                if (this.isDisposed || cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                var currentFilterState = this.filterState;
                var nextFilterPresentation = CreateFilterPresentation(
                    [rootNode],
                    currentFilterState.FilterText,
                    currentFilterState.SelectedElementKinds);
                var filterPresentationChanged =
                    !currentFilterState.FilterPresentation.HasSameVisibilityAs(nextFilterPresentation);

                this.ClearTreeIndexesAndLocalSelection();
                this.nodeIds.UnionWith(stagedNodeIds);

                if (filterPresentationChanged)
                {
                    this.RaisePropertyChanging(nameof(this.FilterPresentation));
                    this.filterState = (
                        currentFilterState.FilterText,
                        currentFilterState.SelectedElementKinds,
                        nextFilterPresentation);
                }

                this.EditRootNodes(nodes =>
                {
                    nodes.Clear();
                    nodes.Add(rootNode);
                });

                this.ApplyDefaultRootSelection();
                this.SetLoaded();

                if (filterPresentationChanged)
                {
                    this.RaisePropertyChanged(nameof(this.FilterPresentation));
                }

                this.RaisePropertyChanged(nameof(this.RootNodes));

                return true;
            }
        }

        /// <summary>
        /// Applies the initial local selection after a complete root publication.
        /// </summary>
        private void ApplyDefaultRootSelection()
        {
            if (this.RootNodes.Count == 0)
            {
                this.ApplySelectedNode(null);

                return;
            }

            var rootNode = this.RootNodes[0];
            this.ApplySelectedNode(rootNode);

            if (rootNode.HasChildren && !rootNode.IsExpanded)
            {
                rootNode.IsExpanded = true;
            }
        }

        /// <summary>
        /// Builds a project browser node from a SysML element without mutating published state.
        /// </summary>
        /// <param name="element">The SysML element represented by the node.</param>
        /// <param name="fallbackId">The fallback identifier used when the element has no identifier.</param>
        /// <param name="stagedNodeIds">The node identifiers assigned while staging.</param>
        /// <param name="cancellationToken">Cancels staged tree construction.</param>
        /// <returns>The project browser node for the provided SysML element.</returns>
        private ProjectBrowserNodeViewModel BuildNode(
            IElement element,
            string fallbackId,
            HashSet<string> stagedNodeIds,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var runtimeTypeName = element.GetType().Name;
            var elementId = element.ElementId.ToDisplayString();
            var nodeId = CreateUniqueNodeId(
                stagedNodeIds,
                string.IsNullOrWhiteSpace(elementId) ? fallbackId : elementId);
            var children = this.BuildChildren(
                element,
                nodeId,
                stagedNodeIds,
                cancellationToken);
            var displayName = GetDisplayName(element, runtimeTypeName);
            var qualifiedName = element.qualifiedName.ToDisplayString();
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

            return node;
        }

        /// <summary>
        /// Applies the selected node to the visual tree projection.
        /// </summary>
        /// <param name="node">The selected node, or <see langword="null" /> when no node is selected.</param>
        private void ApplySelectedNode([AllowNull] ProjectBrowserNodeViewModel node)
        {
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
        /// Clears the current tree and its local selection.
        /// </summary>
        private void ResetTree()
        {
            lock (this.stateMutationGate)
            {
                if (this.isDisposed)
                {
                    return;
                }

                var rootsChanged = this.RootNodes.Count > 0;
                var currentFilterState = this.filterState;
                var nextFilterPresentation = CreateFilterPresentation(
                    [],
                    currentFilterState.FilterText,
                    currentFilterState.SelectedElementKinds);
                var filterPresentationChanged =
                    !currentFilterState.FilterPresentation.HasSameVisibilityAs(nextFilterPresentation);

                this.ClearTreeIndexesAndLocalSelection();

                if (filterPresentationChanged)
                {
                    this.RaisePropertyChanging(nameof(this.FilterPresentation));
                    this.filterState = (
                        currentFilterState.FilterText,
                        currentFilterState.SelectedElementKinds,
                        nextFilterPresentation);
                }

                if (rootsChanged)
                {
                    this.EditRootNodes(nodes => nodes.Clear());
                }

                if (filterPresentationChanged)
                {
                    this.RaisePropertyChanged(nameof(this.FilterPresentation));
                }

                if (rootsChanged)
                {
                    this.RaisePropertyChanged(nameof(this.RootNodes));
                }
            }
        }

        /// <summary>
        /// Edits the root collection in one transaction.
        /// </summary>
        /// <param name="editAction">The batched root-node edit.</param>
        private void EditRootNodes(Action<IExtendedList<ProjectBrowserNodeViewModel>> editAction)
        {
            this.rootNodeSource.Edit(editAction);
        }

        /// <summary>
        /// Clears visual lookup indexes and the selection owned by this project browser.
        /// </summary>
        private void ClearTreeIndexesAndLocalSelection()
        {
            this.ApplySelectedNode(null);
            this.nodeIds.Clear();
        }

        /// <summary>
        /// Resets the tree and exposes a genuine initialization failure.
        /// </summary>
        /// <param name="exception">The initialization failure.</param>
        private void HandleInitializationError(Exception exception)
        {
            if (this.isDisposed)
            {
                return;
            }

            this.ResetTree();
            this.SetError(exception.Message);
        }

        /// <summary>
        /// Publishes complete filter criteria and one coherent visibility state while the state-mutation gate is held.
        /// </summary>
        /// <param name="nextFilterText">The complete next text criterion.</param>
        /// <param name="nextSelectedElementKinds">The complete next selected-kind criterion.</param>
        private void PublishFilterState(
            string nextFilterText,
            ImmutableHashSet<SysmlModelElementKind> nextSelectedElementKinds)
        {
            var currentFilterState = this.filterState;
            var filterTextChanged = !string.Equals(
                currentFilterState.FilterText,
                nextFilterText,
                StringComparison.Ordinal);
            var selectedElementKindsChanged =
                !currentFilterState.SelectedElementKinds.SetEquals(nextSelectedElementKinds);

            if (!filterTextChanged && !selectedElementKindsChanged)
            {
                return;
            }

            var nextFilterPresentation = CreateFilterPresentation(
                this.RootNodes,
                nextFilterText,
                nextSelectedElementKinds);
            var filterPresentationChanged =
                !currentFilterState.FilterPresentation.HasSameVisibilityAs(nextFilterPresentation);

            if (filterTextChanged)
            {
                this.RaisePropertyChanging(nameof(this.FilterText));
            }

            if (selectedElementKindsChanged)
            {
                this.RaisePropertyChanging(nameof(this.SelectedElementKinds));
            }

            if (filterPresentationChanged)
            {
                this.RaisePropertyChanging(nameof(this.FilterPresentation));
            }

            this.filterState = (
                nextFilterText,
                nextSelectedElementKinds,
                filterPresentationChanged
                    ? nextFilterPresentation
                    : currentFilterState.FilterPresentation);

            if (filterTextChanged)
            {
                this.RaisePropertyChanged(nameof(this.FilterText));
            }

            if (selectedElementKindsChanged)
            {
                this.RaisePropertyChanged(nameof(this.SelectedElementKinds));
            }

            if (filterPresentationChanged)
            {
                this.RaisePropertyChanged(nameof(this.FilterPresentation));
            }
        }

        /// <summary>
        /// Captures the complete immutable visibility presentation for the provided canonical roots.
        /// </summary>
        /// <param name="rootNodes">The canonical root nodes.</param>
        /// <param name="filterText">The entered text criterion.</param>
        /// <param name="selectedElementKinds">The selected broad element kinds.</param>
        /// <returns>The coherent visibility presentation.</returns>
        private static ProjectBrowserFilterPresentation CreateFilterPresentation(
            IEnumerable<ProjectBrowserNodeViewModel> rootNodes,
            string filterText,
            ImmutableHashSet<SysmlModelElementKind> selectedElementKinds)
        {
            var matchingText = (filterText ?? string.Empty).Trim();

            if (matchingText.Length == 0 && selectedElementKinds.Count == 0)
            {
                return ProjectBrowserFilterPresentation.Inactive;
            }

            var visibleNodes = ImmutableHashSet.CreateBuilder<ProjectBrowserNodeViewModel>(
                ReferenceEqualityComparer.Instance);

            foreach (var rootNode in rootNodes)
            {
                IncludeVisibleNode(rootNode, matchingText, selectedElementKinds, visibleNodes);
            }

            return ProjectBrowserFilterPresentation.CreateActive(visibleNodes);
        }

        /// <summary>
        /// Adds one matching branch to a post-order visibility projection.
        /// </summary>
        /// <param name="node">The canonical node being evaluated.</param>
        /// <param name="matchingText">The trimmed text criterion.</param>
        /// <param name="selectedElementKinds">The selected broad element kinds.</param>
        /// <param name="visibleNodes">The reference-identity visibility builder.</param>
        /// <returns>
        /// <see langword="true" /> when the node directly matches or owns a visible descendant;
        /// otherwise, <see langword="false" />.
        /// </returns>
        private static bool IncludeVisibleNode(
            ProjectBrowserNodeViewModel node,
            string matchingText,
            ImmutableHashSet<SysmlModelElementKind> selectedElementKinds,
            ImmutableHashSet<ProjectBrowserNodeViewModel>.Builder visibleNodes)
        {
            var hasVisibleDescendant = false;

            foreach (var childNode in node.Children)
            {
                hasVisibleDescendant |= IncludeVisibleNode(
                    childNode,
                    matchingText,
                    selectedElementKinds,
                    visibleNodes);
            }

            if (!hasVisibleDescendant && !DirectlyMatches(node, matchingText, selectedElementKinds))
            {
                return false;
            }

            visibleNodes.Add(node);

            return true;
        }

        /// <summary>
        /// Determines whether a node satisfies every active filter criterion.
        /// </summary>
        /// <param name="node">The canonical node.</param>
        /// <param name="matchingText">The trimmed text criterion.</param>
        /// <param name="selectedElementKinds">The selected broad element kinds.</param>
        /// <returns><see langword="true" /> when every active criterion matches the node.</returns>
        private static bool DirectlyMatches(
            ProjectBrowserNodeViewModel node,
            string matchingText,
            ImmutableHashSet<SysmlModelElementKind> selectedElementKinds)
        {
            var textMatches = matchingText.Length == 0
                              || node.DisplayName?.Contains(
                                  matchingText,
                                  StringComparison.OrdinalIgnoreCase) == true
                              || node.QualifiedName?.Contains(
                                  matchingText,
                                  StringComparison.OrdinalIgnoreCase) == true;

            return textMatches
                   && (selectedElementKinds.Count == 0 || selectedElementKinds.Contains(node.ElementKind));
        }

        /// <summary>
        /// Builds child project browser nodes from the owned elements of a SysML element.
        /// </summary>
        /// <param name="element">The SysML element whose owned elements should be mapped.</param>
        /// <param name="parentNodeId">The identifier of the parent project browser node.</param>
        /// <param name="stagedNodeIds">The node identifiers assigned while staging.</param>
        /// <param name="cancellationToken">Cancels staged tree construction.</param>
        /// <returns>The child project browser nodes for the provided SysML element.</returns>
        private List<ProjectBrowserNodeViewModel> BuildChildren(
            IElement element,
            string parentNodeId,
            HashSet<string> stagedNodeIds,
            CancellationToken cancellationToken)
        {
            var children = new List<ProjectBrowserNodeViewModel>();

            if (element.ownedElement == null)
            {
                return children;
            }

            var index = 0;

            foreach (var childElement in element.ownedElement)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (childElement != null)
                {
                    children.Add(this.BuildNode(
                        childElement,
                        string.Create(CultureInfo.InvariantCulture, $"{parentNodeId}/{index}"),
                        stagedNodeIds,
                        cancellationToken));
                }

                index++;
            }

            return children;
        }

        /// <summary>
        /// Creates an identifier that is unique within the staged project browser tree.
        /// </summary>
        /// <param name="stagedNodeIds">The identifiers already assigned while staging.</param>
        /// <param name="preferredId">The preferred identifier for the node.</param>
        /// <returns>A unique project browser node identifier.</returns>
        private static string CreateUniqueNodeId(HashSet<string> stagedNodeIds, string preferredId)
        {
            if (stagedNodeIds.Add(preferredId))
            {
                return preferredId;
            }

            var suffix = 2;
            var candidateId = string.Create(CultureInfo.InvariantCulture, $"{preferredId}-{suffix}");

            while (!stagedNodeIds.Add(candidateId))
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
            var declaredName = element.DeclaredName.ToDisplayString();

            if (!string.IsNullOrWhiteSpace(declaredName))
            {
                return declaredName;
            }

            var name = element.name.ToDisplayString();

            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            var qualifiedName = element.qualifiedName.ToDisplayString();

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
    }
}
