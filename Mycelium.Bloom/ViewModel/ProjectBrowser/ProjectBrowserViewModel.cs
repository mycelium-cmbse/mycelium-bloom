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
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;

    using DynamicData;

    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Model.Enum;

    using static Mycelium.Bloom.Components.Common.DisplayStringFormatter;

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
        /// The set of node identifiers assigned in the current project browser tree.
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
        /// Keeps the DynamicData binding alive until final disposal.
        /// </summary>
        private readonly IDisposable rootNodeBinding;

        /// <summary>
        /// Reconciles global selection whenever either selection or roots change.
        /// </summary>
        private readonly IDisposable selectionProjectionSubscription;

        /// <summary>
        /// Cancels initialization when final ViewModel disposal begins.
        /// </summary>
        private readonly CancellationTokenSource lifetimeCancellation = new();

        /// <summary>
        /// The visual projection of the globally selected element.
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

            var selectedElementChanges = this.elementSelectionService
                .WhenAnyValue(service => service.SelectedElement);

            var rootChanges = System.Reactive.Linq.Observable.Select(
                this.rootNodeSource.Connect(),
                _ => true);

            this.selectionProjectionSubscription = System.ObservableExtensions.Subscribe(
                System.Reactive.Linq.Observable.CombineLatest(
                    selectedElementChanges,
                    rootChanges,
                    (selectedElement, _) => selectedElement),
                this.ApplySelectedElement);
        }

        /// <inheritdoc />
        public ReadOnlyObservableCollection<ProjectBrowserNodeViewModel> RootNodes => this.rootNodes;

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
                var stagedElementNodes = new Dictionary<IElement, ProjectBrowserNodeViewModel>(
                    ReferenceEqualityComparer.Instance);
                var rootNode = this.BuildNode(
                    model,
                    "root",
                    stagedNodeIds,
                    stagedElementNodes,
                    initializationToken);

                initializationToken.ThrowIfCancellationRequested();

                return this.TryPublishTree(
                    rootNode,
                    stagedNodeIds,
                    stagedElementNodes,
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

            if (!this.isDisposed && node.HasChildren)
            {
                node.IsExpanded = !node.IsExpanded;
            }
        }

        /// <inheritdoc />
        public void SelectNode(ProjectBrowserNodeViewModel node)
        {
            ArgumentNullException.ThrowIfNull(node);

            if (!this.isDisposed)
            {
                this.elementSelectionService.SelectedElement = node.SourceElement;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;
            this.lifetimeCancellation.Cancel();
            this.selectionProjectionSubscription.Dispose();
            this.rootNodeBinding.Dispose();
            this.rootNodeSource.Dispose();
            this.lifetimeCancellation.Dispose();
        }

        /// <summary>
        /// Publishes a completely staged project browser tree.
        /// </summary>
        /// <param name="rootNode">The staged root node.</param>
        /// <param name="stagedNodeIds">The node identifiers assigned while staging.</param>
        /// <param name="stagedElementNodes">The reference-identity lookup built while staging.</param>
        /// <param name="cancellationToken">Cancels publication before staged state is exposed.</param>
        /// <returns><see langword="true" /> when the staged tree was published; otherwise, <see langword="false" />.</returns>
        private bool TryPublishTree(
            ProjectBrowserNodeViewModel rootNode,
            HashSet<string> stagedNodeIds,
            Dictionary<IElement, ProjectBrowserNodeViewModel> stagedElementNodes,
            CancellationToken cancellationToken)
        {
            if (this.isDisposed || cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            this.ClearTreeIndexesAndOwnedSelection();
            this.nodeIds.UnionWith(stagedNodeIds);

            foreach (var elementNode in stagedElementNodes)
            {
                this.elementNodes.Add(elementNode.Key, elementNode.Value);
            }

            this.EditRootNodes(nodes =>
            {
                nodes.Clear();
                nodes.Add(rootNode);
            });

            this.SelectDefaultRootNode();
            this.SetLoaded();

            return true;
        }

        /// <summary>
        /// Selects and expands the first root node when no global selection exists.
        /// </summary>
        private void SelectDefaultRootNode()
        {
            if (this.RootNodes.Count == 0 || this.elementSelectionService.SelectedElement != null)
            {
                return;
            }

            var rootNode = this.RootNodes[0];

            this.SelectNode(rootNode);

            if (rootNode.HasChildren && !rootNode.IsExpanded)
            {
                this.ToggleNode(rootNode);
            }
        }

        /// <summary>
        /// Builds a project browser node from a SysML element without mutating published state.
        /// </summary>
        /// <param name="element">The SysML element represented by the node.</param>
        /// <param name="fallbackId">The fallback identifier used when the element has no identifier.</param>
        /// <param name="stagedNodeIds">The node identifiers assigned while staging.</param>
        /// <param name="stagedElementNodes">The reference-identity lookup built while staging.</param>
        /// <param name="cancellationToken">Cancels staged tree construction.</param>
        /// <returns>The project browser node for the provided SysML element.</returns>
        private ProjectBrowserNodeViewModel BuildNode(
            IElement element,
            string fallbackId,
            HashSet<string> stagedNodeIds,
            Dictionary<IElement, ProjectBrowserNodeViewModel> stagedElementNodes,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var runtimeTypeName = element.GetType().Name;
            var elementId = ToDisplayString(element.ElementId);
            var nodeId = CreateUniqueNodeId(
                stagedNodeIds,
                string.IsNullOrWhiteSpace(elementId) ? fallbackId : elementId);
            var children = this.BuildChildren(
                element,
                nodeId,
                stagedNodeIds,
                stagedElementNodes,
                cancellationToken);
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

            stagedElementNodes.TryAdd(element, node);

            return node;
        }

        /// <summary>
        /// Applies the shared selected element to the visual node projection.
        /// </summary>
        /// <param name="element">The selected element, or <see langword="null" />.</param>
        private void ApplySelectedElement([AllowNull] IElement element)
        {
            if (this.isDisposed)
            {
                return;
            }

            if (element != null && this.elementNodes.TryGetValue(element, out var node))
            {
                this.ApplySelectedNode(node);

                return;
            }

            this.ApplySelectedNode(null);
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
        /// Clears the current tree and any shared selection owned by that tree.
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
        /// Edits the root collection in one transaction.
        /// </summary>
        /// <param name="editAction">The batched root-node edit.</param>
        private void EditRootNodes(Action<IExtendedList<ProjectBrowserNodeViewModel>> editAction)
        {
            this.rootNodeSource.Edit(editAction);
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
        /// Builds child project browser nodes from the owned elements of a SysML element.
        /// </summary>
        /// <param name="element">The SysML element whose owned elements should be mapped.</param>
        /// <param name="parentNodeId">The identifier of the parent project browser node.</param>
        /// <param name="stagedNodeIds">The node identifiers assigned while staging.</param>
        /// <param name="stagedElementNodes">The reference-identity lookup built while staging.</param>
        /// <param name="cancellationToken">Cancels staged tree construction.</param>
        /// <returns>The child project browser nodes for the provided SysML element.</returns>
        private List<ProjectBrowserNodeViewModel> BuildChildren(
            IElement element,
            string parentNodeId,
            HashSet<string> stagedNodeIds,
            Dictionary<IElement, ProjectBrowserNodeViewModel> stagedElementNodes,
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
                        stagedElementNodes,
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
    }
}
