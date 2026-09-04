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
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    using DynamicData;
    using DynamicData.Binding;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Core.Selection;

    using ReactiveUI;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>
    /// Provides tree, filter, and local selection state for the project browser.
    /// </summary>
    public sealed class ProjectBrowserViewModel : BloomBaseViewModel, IProjectBrowserViewModel
    {
        /// <summary>
        /// Compares derived filter presentations by their visibility semantics.
        /// </summary>
        private static readonly IEqualityComparer<ProjectBrowserFilterPresentation> FilterPresentationComparer =
            EqualityComparer<ProjectBrowserFilterPresentation>.Create(
                static (current, next) => ReferenceEquals(current, next)
                                          || (current is not null
                                              && next is not null
                                              && current.HasSameVisibilityAs(next)),
                static presentation => presentation.IsActive.GetHashCode());

        /// <summary>
        /// The model loader service used to retrieve SysML models.
        /// </summary>
        private readonly IModelLoaderService modelLoaderService;

        /// <summary>
        /// The shared element selection service.
        /// </summary>
        private readonly IElementSelectionService elementSelectionService;

        /// <summary>
        /// Owns the currently materialized root nodes.
        /// </summary>
        private readonly SourceList<ProjectBrowserNodeViewModel> rootNodeSource = new();

        /// <summary>
        /// Owns the distinct non-relationship types present in the materialized model.
        /// </summary>
        private readonly SourceCache<Type, Type> availableElementTypeSource = new(type => type);

        /// <summary>
        /// Owns the selected element types for this project browser instance.
        /// </summary>
        private readonly SourceCache<Type, Type> selectedElementTypeSource = new(type => type);

        /// <summary>
        /// Keeps DynamicData bindings and derived-state subscriptions alive.
        /// </summary>
        private readonly CompositeDisposable subscriptions = new();

        /// <summary>
        /// Cancels initialization when final ViewModel disposal begins.
        /// </summary>
        private readonly CancellationTokenSource lifetimeCancellation = new();

        /// <summary>
        /// The stable read-only root-node projection.
        /// </summary>
        private readonly ReadOnlyObservableCollection<ProjectBrowserNodeViewModel> rootNodes;

        /// <summary>
        /// The stable read-only available-type projection.
        /// </summary>
        private readonly ReadOnlyObservableCollection<Type> availableElementTypes;

        /// <summary>
        /// The stable read-only selected-type projection.
        /// </summary>
        private readonly ReadOnlyObservableCollection<Type> selectedElementTypes;

        /// <summary>
        /// The committed Contains criterion.
        /// </summary>
        private string filterText = string.Empty;

        /// <summary>
        /// The current immutable visibility projection over the canonical tree.
        /// </summary>
        private readonly ObservableAsPropertyHelper<ProjectBrowserFilterPresentation> filterPresentation;

        /// <summary>
        /// The node selected locally in this project browser.
        /// </summary>
        [AllowNull]
        [MaybeNull]
        private ProjectBrowserNodeViewModel selectedNode;

        /// <summary>
        /// The model identity waiting to be reconciled with this browser's materialized tree.
        /// </summary>
        private IElement pendingFocusElement;

        /// <summary>
        /// Tracks whether initialization owns the current load operation.
        /// </summary>
        private int initializationState;

        /// <summary>
        /// Tracks whether final disposal has occurred.
        /// </summary>
        private int disposalState;

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

            this.subscriptions.Add(System.ObservableExtensions.Subscribe(
                this.availableElementTypeSource
                    .Connect()
                    .SortAndBind(
                        out var boundAvailableElementTypes,
                        SortExpressionComparer<Type>.Ascending(type => type.Name))));
            this.availableElementTypes = boundAvailableElementTypes;

            this.subscriptions.Add(System.ObservableExtensions.Subscribe(
                this.selectedElementTypeSource
                    .Connect()
                    .SortAndBind(
                        out var boundSelectedElementTypes,
                        SortExpressionComparer<Type>.Ascending(type => type.Name))));
            this.selectedElementTypes = boundSelectedElementTypes;

            this.filterPresentation = Observable.CombineLatest(
                    this.WhenAnyValue(viewModel => viewModel.FilterText),
                    this.rootNodeSource
                        .Connect()
                        .ToCollection()
                        .StartWith(Array.Empty<ProjectBrowserNodeViewModel>()),
                    this.selectedElementTypeSource
                        .Connect()
                        .ToCollection()
                        .StartWith(Array.Empty<Type>()),
                    CreateFilterPresentation)
                .DistinctUntilChanged(
                    FilterPresentationComparer)
                .ToProperty(
                    this,
                    viewModel => viewModel.FilterPresentation,
                    ProjectBrowserFilterPresentation.Inactive);
            this.subscriptions.Add(this.filterPresentation);

            this.subscriptions.Add(System.ObservableExtensions.Subscribe(
                this.rootNodeSource.Connect().Bind(out var boundRootNodes)));
            this.rootNodes = boundRootNodes;

            this.subscriptions.Add(System.ObservableExtensions.Subscribe(
                Observable.CombineLatest(
                        this.WhenAnyValue(viewModel => viewModel.PendingFocusElement),
                        this.rootNodeSource
                            .Connect()
                            .ToCollection()
                            .StartWith(Array.Empty<ProjectBrowserNodeViewModel>()),
                        CreateFocusPath)
                    .Where(path => path.Count > 0),
                this.ApplyFocusPath));
        }

        /// <summary>
        /// Gets the root nodes displayed by the project browser.
        /// </summary>
        public ReadOnlyObservableCollection<ProjectBrowserNodeViewModel> RootNodes => this.rootNodes;

        /// <summary>
        /// Gets the distinct element types available in the currently loaded model.
        /// </summary>
        public ReadOnlyObservableCollection<Type> AvailableElementTypes => this.availableElementTypes;

        /// <summary>
        /// Gets or sets the committed Contains criterion.
        /// </summary>
        public string FilterText
        {
            get => this.filterText;
            set
            {
                if (!this.IsDisposed)
                {
                    this.RaiseAndSetIfChanged(ref this.filterText, value ?? string.Empty);
                }
            }
        }

        /// <summary>
        /// Gets the element types selected for filtering in this project browser.
        /// </summary>
        public ReadOnlyObservableCollection<Type> SelectedElementTypes => this.selectedElementTypes;

        /// <summary>
        /// Gets the current immutable visibility projection over the canonical tree.
        /// </summary>
        public ProjectBrowserFilterPresentation FilterPresentation => this.filterPresentation.Value;

        /// <summary>
        /// Gets the node selected locally in this project browser.
        /// </summary>
        [AllowNull]
        [MaybeNull]
        public ProjectBrowserNodeViewModel SelectedNode
        {
            get => this.selectedNode;
            private set => this.RaiseAndSetIfChanged(ref this.selectedNode, value);
        }

        /// <summary>
        /// Gets or sets model identity pending local tree-focus reconciliation.
        /// </summary>
        private IElement PendingFocusElement
        {
            get => this.pendingFocusElement;
            set => this.RaiseAndSetIfChanged(ref this.pendingFocusElement, value);
        }

        /// <summary>
        /// Initializes the project browser from the Quantities model.
        /// </summary>
        /// <param name="cancellationToken">Cancels initialization.</param>
        /// <returns><see langword="true" /> when a new tree is loaded; otherwise, <see langword="false" />.</returns>
        public async Task<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            if (this.IsDisposed
                || cancellationToken.IsCancellationRequested
                || this.IsLoaded
                || Interlocked.CompareExchange(ref this.initializationState, 1, 0) != 0)
            {
                return false;
            }

            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                this.lifetimeCancellation.Token);
            var initializationToken = linkedCancellation.Token;
            this.StartLoading();

            try
            {
                var model = await Task.Run(
                        this.modelLoaderService.LoadQuantitiesModel,
                        CancellationToken.None)
                    .WaitAsync(initializationToken);

                initializationToken.ThrowIfCancellationRequested();

                if (model is null)
                {
                    this.HandleInitializationError("The Quantities model is unavailable.");

                    return false;
                }

                var stagedNodeIds = new HashSet<string>(StringComparer.Ordinal);
                var stagedAvailableElementTypes = new HashSet<Type>();
                var rootNode = this.BuildNode(
                    model,
                    "root",
                    stagedNodeIds,
                    stagedAvailableElementTypes,
                    initializationToken);

                initializationToken.ThrowIfCancellationRequested();

                return this.TryPublishTree(
                    rootNode,
                    stagedAvailableElementTypes,
                    initializationToken);
            }
            catch (Exception) when (initializationToken.IsCancellationRequested || this.IsDisposed)
            {
                return false;
            }
            catch (Exception exception)
            {
                this.HandleInitializationError(exception.Message);

                return false;
            }
            finally
            {
                Interlocked.Exchange(ref this.initializationState, 0);

                if (!this.IsDisposed)
                {
                    this.StopLoading();
                }
            }
        }

        /// <summary>
        /// Toggles an expandable node while filtering is inactive.
        /// </summary>
        /// <param name="node">The node to expand or collapse.</param>
        public void ToggleNode(ProjectBrowserNodeViewModel node)
        {
            ArgumentNullException.ThrowIfNull(node);

            if (!this.IsDisposed && !this.FilterPresentation.IsActive && node.HasChildren)
            {
                node.IsExpanded = !node.IsExpanded;
            }
        }

        /// <summary>
        /// Clears the committed text and selected type criteria.
        /// </summary>
        public void ClearFilter()
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.FilterText = string.Empty;
            this.selectedElementTypeSource.Clear();
        }

        /// <summary>
        /// Adds or removes an available runtime model type from the active filter.
        /// </summary>
        /// <param name="elementType">The runtime model element type to toggle.</param>
        public void ToggleElementTypeFilter(Type elementType)
        {
            ArgumentNullException.ThrowIfNull(elementType);

            if (this.IsDisposed)
            {
                return;
            }

            if (typeof(IRelationship).IsAssignableFrom(elementType)
                || !this.availableElementTypeSource.Lookup(elementType).HasValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(elementType),
                    elementType,
                    "The element type must be an available non-relationship model type.");
            }

            if (this.selectedElementTypeSource.Lookup(elementType).HasValue)
            {
                this.selectedElementTypeSource.RemoveKey(elementType);
            }
            else
            {
                this.selectedElementTypeSource.AddOrUpdate(elementType);
            }
        }

        /// <summary>
        /// Selects a local project browser node and updates the shared details context.
        /// </summary>
        /// <param name="node">The node to select.</param>
        public void SelectNode(ProjectBrowserNodeViewModel node)
        {
            ArgumentNullException.ThrowIfNull(node);

            if (!this.IsDisposed)
            {
                this.SelectedNode = node;
                this.elementSelectionService.SelectedElement = node.SourceElement;
            }
        }

        /// <inheritdoc />
        public void FocusElement(IElement element)
        {
            ArgumentNullException.ThrowIfNull(element);

            if (!this.IsDisposed)
            {
                this.PendingFocusElement = element;
            }
        }

        /// <summary>
        /// Cancels loading and releases the reactive collections owned by this ViewModel.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref this.disposalState, 1) != 0)
            {
                return;
            }

            this.lifetimeCancellation.Cancel();
            this.subscriptions.Dispose();
            this.selectedElementTypeSource.Dispose();
            this.availableElementTypeSource.Dispose();
            this.rootNodeSource.Dispose();
            this.lifetimeCancellation.Dispose();
        }

        /// <summary>
        /// Gets a value indicating whether final disposal has occurred.
        /// </summary>
        private bool IsDisposed => Volatile.Read(ref this.disposalState) != 0;

        /// <summary>
        /// Locates the exact node and ancestor path for one pending model identity.
        /// </summary>
        /// <param name="element">The canonical model element awaiting focus.</param>
        /// <param name="rootNodes">The currently materialized canonical roots.</param>
        /// <returns>The ancestor path ending at the target node, or an empty path while unresolved.</returns>
        private static IReadOnlyList<ProjectBrowserNodeViewModel> CreateFocusPath(
            IElement element,
            IReadOnlyCollection<ProjectBrowserNodeViewModel> rootNodes)
        {
            if (element is null)
            {
                return Array.Empty<ProjectBrowserNodeViewModel>();
            }

            foreach (var rootNode in rootNodes)
            {
                var path = new List<ProjectBrowserNodeViewModel>();

                if (TryAddFocusPath(rootNode, element, path))
                {
                    return path;
                }
            }

            return Array.Empty<ProjectBrowserNodeViewModel>();
        }

        /// <summary>
        /// Adds one depth-first canonical node path when it contains the requested model identity.
        /// </summary>
        /// <param name="node">The current canonical node.</param>
        /// <param name="element">The model identity to locate.</param>
        /// <param name="path">The mutable candidate ancestor path.</param>
        /// <returns><see langword="true" /> when the current subtree contains the target.</returns>
        private static bool TryAddFocusPath(
            ProjectBrowserNodeViewModel node,
            IElement element,
            ICollection<ProjectBrowserNodeViewModel> path)
        {
            path.Add(node);

            if (ReferenceEquals(node.SourceElement, element)
                || (!string.IsNullOrWhiteSpace(element.ElementId)
                    && string.Equals(node.ElementId, element.ElementId, StringComparison.Ordinal)))
            {
                return true;
            }

            foreach (var childNode in node.Children)
            {
                if (TryAddFocusPath(childNode, element, path))
                {
                    return true;
                }
            }

            path.Remove(node);

            return false;
        }

        /// <summary>
        /// Applies one resolved local focus path without changing shared application selection.
        /// </summary>
        /// <param name="path">The ancestor path ending at the local target node.</param>
        private void ApplyFocusPath(IReadOnlyList<ProjectBrowserNodeViewModel> path)
        {
            if (this.IsDisposed || path.Count == 0)
            {
                return;
            }

            for (var index = 0; index < path.Count - 1; index++)
            {
                path[index].IsExpanded = true;
            }

            this.SelectedNode = path[^1];
            this.PendingFocusElement = null;
        }

        /// <summary>
        /// Publishes a fully staged tree and its available filter types.
        /// </summary>
        /// <param name="rootNode">The staged root node.</param>
        /// <param name="stagedElementTypes">The staged distinct non-relationship types.</param>
        /// <param name="cancellationToken">Cancels publication before staged state is exposed.</param>
        /// <returns><see langword="true" /> when the staged tree was published; otherwise, <see langword="false" />.</returns>
        private bool TryPublishTree(
            ProjectBrowserNodeViewModel rootNode,
            IReadOnlyCollection<Type> stagedElementTypes,
            CancellationToken cancellationToken)
        {
            if (this.IsDisposed || cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            this.SelectedNode = rootNode;
            rootNode.IsExpanded = rootNode.HasChildren;

            this.availableElementTypeSource.Edit(types =>
            {
                types.Clear();
                types.AddOrUpdate(stagedElementTypes);
            });

            this.rootNodeSource.Edit(nodes =>
            {
                nodes.Clear();
                nodes.Add(rootNode);
            });

            this.SetLoaded();

            return true;
        }

        /// <summary>
        /// Builds a project browser node without mutating published state.
        /// </summary>
        /// <param name="element">The SysML element represented by the node.</param>
        /// <param name="fallbackId">The fallback identifier used when the element has no identifier.</param>
        /// <param name="stagedNodeIds">The node identifiers assigned while staging.</param>
        /// <param name="stagedElementTypes">The distinct non-relationship types found while staging.</param>
        /// <param name="cancellationToken">Cancels staged tree construction.</param>
        /// <returns>The project browser node for the provided SysML element.</returns>
        private ProjectBrowserNodeViewModel BuildNode(
            IElement element,
            string fallbackId,
            HashSet<string> stagedNodeIds,
            HashSet<Type> stagedElementTypes,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var elementType = element.GetType();

            if (element is not IRelationship)
            {
                stagedElementTypes.Add(elementType);
            }

            var elementId = element.ElementId.ToDisplayString();
            var nodeId = CreateUniqueNodeId(
                stagedNodeIds,
                string.IsNullOrWhiteSpace(elementId) ? fallbackId : elementId);
            var children = this.BuildChildren(
                element,
                nodeId,
                stagedNodeIds,
                stagedElementTypes,
                cancellationToken);
            var metadata = new ProjectBrowserNodeMetadata(
                elementId,
                element.qualifiedName.ToDisplayString(),
                element);

            return new ProjectBrowserNodeViewModel(
                nodeId,
                GetDisplayName(element, elementType.Name),
                metadata,
                children);
        }

        /// <summary>
        /// Clears the materialized tree after an initialization failure.
        /// </summary>
        private void ResetTree()
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.SelectedNode = null;
            this.rootNodeSource.Clear();
            this.availableElementTypeSource.Clear();
            this.selectedElementTypeSource.Clear();
        }

        /// <summary>
        /// Resets materialized state and exposes an initialization failure.
        /// </summary>
        /// <param name="errorMessage">The initialization failure message.</param>
        private void HandleInitializationError(string errorMessage)
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.ResetTree();
            this.SetError(errorMessage);
        }

        /// <summary>
        /// Creates a visibility projection from the current roots and criteria.
        /// </summary>
        /// <param name="filterText">The entered text criterion.</param>
        /// <param name="rootNodes">The canonical root nodes.</param>
        /// <param name="selectedElementTypes">The selected concrete element types.</param>
        /// <returns>The immutable visibility presentation.</returns>
        private static ProjectBrowserFilterPresentation CreateFilterPresentation(
            string filterText,
            IReadOnlyCollection<ProjectBrowserNodeViewModel> rootNodes,
            IReadOnlyCollection<Type> selectedElementTypes)
        {
            var matchingText = (filterText ?? string.Empty).Trim();

            if (matchingText.Length == 0 && selectedElementTypes.Count == 0)
            {
                return ProjectBrowserFilterPresentation.Inactive;
            }

            var visibleNodes = ImmutableHashSet.CreateBuilder<ProjectBrowserNodeViewModel>(
                ReferenceEqualityComparer.Instance);

            foreach (var rootNode in rootNodes)
            {
                IncludeVisibleNode(
                    rootNode,
                    matchingText,
                    selectedElementTypes,
                    visibleNodes,
                    ancestorDisplayNameMatches: false);
            }

            return ProjectBrowserFilterPresentation.CreateActive(visibleNodes);
        }

        /// <summary>
        /// Adds a matching node and its ancestor chain to a visibility projection.
        /// </summary>
        /// <param name="node">The canonical node being evaluated.</param>
        /// <param name="matchingText">The trimmed text criterion.</param>
        /// <param name="selectedElementTypes">The selected concrete element types.</param>
        /// <param name="visibleNodes">The reference-identity visibility builder.</param>
        /// <param name="ancestorDisplayNameMatches">
        /// Whether an ancestor's display name already matches the text criterion.
        /// </param>
        /// <returns>Whether the node directly matches or owns a visible descendant.</returns>
        private static bool IncludeVisibleNode(
            ProjectBrowserNodeViewModel node,
            string matchingText,
            IReadOnlyCollection<Type> selectedElementTypes,
            ImmutableHashSet<ProjectBrowserNodeViewModel>.Builder visibleNodes,
            bool ancestorDisplayNameMatches)
        {
            var displayNameMatches = ContainsText(node.DisplayName, matchingText);
            var hasVisibleDescendant = false;

            foreach (var childNode in node.Children)
            {
                hasVisibleDescendant |= IncludeVisibleNode(
                    childNode,
                    matchingText,
                    selectedElementTypes,
                    visibleNodes,
                    ancestorDisplayNameMatches || displayNameMatches);
            }

            if (!hasVisibleDescendant
                && !DirectlyMatches(
                    node,
                    matchingText,
                    selectedElementTypes,
                    displayNameMatches,
                    ancestorDisplayNameMatches))
            {
                return false;
            }

            visibleNodes.Add(node);

            return true;
        }

        /// <summary>
        /// Determines whether a node satisfies every active criterion.
        /// </summary>
        /// <param name="node">The canonical node.</param>
        /// <param name="matchingText">The trimmed text criterion.</param>
        /// <param name="selectedElementTypes">The selected concrete element types.</param>
        /// <param name="displayNameMatches">Whether the node's display name matches the text criterion.</param>
        /// <param name="ancestorDisplayNameMatches">
        /// Whether an ancestor's display name already matches the text criterion.
        /// </param>
        /// <returns>Whether every active criterion matches the node.</returns>
        private static bool DirectlyMatches(
            ProjectBrowserNodeViewModel node,
            string matchingText,
            IReadOnlyCollection<Type> selectedElementTypes,
            bool displayNameMatches,
            bool ancestorDisplayNameMatches)
        {
            var textMatches = matchingText.Length == 0
                              || displayNameMatches
                              || (!ancestorDisplayNameMatches
                                  && ContainsText(node.QualifiedName, matchingText));

            return textMatches
                   && (selectedElementTypes.Count == 0 || selectedElementTypes.Contains(node.ElementType));
        }

        /// <summary>
        /// Determines whether source text contains a non-empty criterion using ordinal-ignore-case comparison.
        /// </summary>
        /// <param name="source">The source text.</param>
        /// <param name="matchingText">The trimmed text criterion.</param>
        /// <returns>Whether the criterion occurs in the source text.</returns>
        private static bool ContainsText(string source, string matchingText)
        {
            return matchingText.Length > 0
                   && source?.Contains(matchingText, StringComparison.OrdinalIgnoreCase) == true;
        }

        /// <summary>
        /// Builds child nodes from a SysML element's owned elements.
        /// </summary>
        /// <param name="element">The element whose owned elements are mapped.</param>
        /// <param name="parentNodeId">The parent node identifier.</param>
        /// <param name="stagedNodeIds">The node identifiers assigned while staging.</param>
        /// <param name="stagedElementTypes">The distinct non-relationship types found while staging.</param>
        /// <param name="cancellationToken">Cancels staged tree construction.</param>
        /// <returns>The child nodes for the provided element.</returns>
        private List<ProjectBrowserNodeViewModel> BuildChildren(
            IElement element,
            string parentNodeId,
            HashSet<string> stagedNodeIds,
            HashSet<Type> stagedElementTypes,
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
                        stagedElementTypes,
                        cancellationToken));
                }

                index++;
            }

            return children;
        }

        /// <summary>
        /// Creates an identifier that is unique within the staged tree.
        /// </summary>
        /// <param name="stagedNodeIds">The identifiers already assigned.</param>
        /// <param name="preferredId">The preferred node identifier.</param>
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
        /// <param name="runtimeTypeName">The runtime type name used as a fallback.</param>
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

            return string.IsNullOrWhiteSpace(qualifiedName)
                ? runtimeTypeName
                : qualifiedName;
        }
    }
}
