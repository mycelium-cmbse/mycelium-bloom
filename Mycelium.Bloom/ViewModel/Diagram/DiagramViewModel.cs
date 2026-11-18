// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram
{
    using System.Collections.Immutable;

    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Model.Diagram;

    using ReactiveUI;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>Owns presentation and nested-view lifetimes for one tab, borrowing canonical objects and selection.</summary>
    public class DiagramViewModel : BloomBaseViewModel, IDiagramViewModel
    {
        /// <summary>The circuit-owned selection authority.</summary>
        private readonly IElementSelectionService selection;

        /// <summary>Constructs nested state with the same caller-owned factory contract.</summary>
        private readonly Func<DiagramProjection, IDiagramViewModel> createNestedView;

        /// <summary>The view's single subscription to canonical selection.</summary>
        private readonly IDisposable selectionSubscription;

        /// <summary>The reactive presentations indexed by graphical node identity.</summary>
        private readonly Dictionary<Guid, DiagramNodeViewModel> nodeStates = new();

        /// <summary>The reactive presentations indexed by graphical relationship identity.</summary>
        private readonly Dictionary<Guid, DiagramEdgeViewModel> edgeStates = new();

        /// <summary>Finds affected occurrences using canonical reference equality across models.</summary>
        private readonly Dictionary<IElement, List<DiagramElementViewModel>> selectionTargets = new(ReferenceEqualityComparer.Instance);

        /// <summary>The previously selected occurrence decorations, not a second semantic selection authority.</summary>
        private IReadOnlyList<DiagramElementViewModel> selectedTargets = Array.Empty<DiagramElementViewModel>();

        /// <summary>The coherent current projection snapshot.</summary>
        private DiagramProjection currentProjection;

        /// <summary>The child state owned by each parent occurrence and immutable projection.</summary>
        private readonly Dictionary<(Guid Occurrence, DiagramProjection Projection), IDiagramViewModel> nestedViews = new();

        /// <summary>The coherent retained viewport.</summary>
        private DiagramViewport viewport = new();

        /// <summary>Prevents operations from extending a closed tab's lifetime.</summary>
        private bool disposed;

        /// <summary>Gets the current immutable graphical projection.</summary>
        public DiagramProjection Projection
        {
            get => this.currentProjection;
            private set => this.RaiseAndSetIfChanged(ref this.currentProjection, value);
        }

        /// <summary>Gets the viewport retained by this view.</summary>
        public DiagramViewport Viewport
        {
            get => this.viewport;
            private set => this.RaiseAndSetIfChanged(ref this.viewport, value);
        }

        /// <summary>Creates tab state over validated presentation and observes shared selection.</summary>
        /// <param name="projection">The immutable view with occurrence-scoped graphical identities.</param>
        /// <param name="selection">The circuit-owned canonical selection service.</param>
        /// <param name="createNestedView">The optional factory for caller-owned nested views.</param>
        public DiagramViewModel(DiagramProjection projection, IElementSelectionService selection,
            Func<DiagramProjection, IDiagramViewModel> createNestedView = null)
        {
            ArgumentNullException.ThrowIfNull(projection);
            ArgumentNullException.ThrowIfNull(selection);
            this.selection = selection;
            this.createNestedView = createNestedView ?? (child => new DiagramViewModel(child, selection));
            this.UpdateProjection(projection);
            this.selectionSubscription = this.selection.WhenAnyValue(service => service.SelectedElement).Subscribe(this.ApplySelection);
        }

        /// <summary>Gets retained reactive state for one graphical node.</summary>
        /// <param name="occurrenceId">The graphical node identity.</param>
        /// <returns>The tab-owned node state.</returns>
        public DiagramNodeViewModel GetNodeViewModel(Guid occurrenceId) => this.nodeStates[occurrenceId];

        /// <summary>Gets retained reactive state for one graphical relationship.</summary>
        /// <param name="occurrenceId">The graphical relationship identity.</param>
        /// <returns>The tab-owned relationship state.</returns>
        public DiagramEdgeViewModel GetEdgeViewModel(Guid occurrenceId) => this.edgeStates[occurrenceId];

        /// <summary>Replaces validated graphical presentation while retaining compatible occurrence state.</summary>
        /// <param name="projection">The new immutable projection.</param>
        public void UpdateProjection(DiagramProjection projection)
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);
            ArgumentNullException.ThrowIfNull(projection);
            DiagramProjectionValidator.Validate(projection);

            if (Equals(this.Projection, projection))
            {
                return;
            }

            this.UpdateNodes(projection);
            this.UpdateEdges(projection);
            this.selectionTargets.Clear();

            foreach (var node in this.nodeStates.Values)
            {
                this.RegisterSelectionTarget(node);

                foreach (var boundary in node.BoundaryFeatures)
                {
                    this.RegisterSelectionTarget(boundary);
                }
            }

            foreach (var edge in this.edgeStates.Values)
            {
                this.RegisterSelectionTarget(edge);
            }

            this.ApplySelection(this.selection.SelectedElement);
            this.ReleaseRemovedNestedViews(projection);
            this.Projection = projection;
        }

        /// <summary>Selects the supplied represented canonical element.</summary>
        /// <param name="element">The represented canonical object, or null.</param>
        public void Select(IElement element)
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);

            if (element is not null && this.selectionTargets.ContainsKey(element))
            {
                this.selection.SelectedElement = element;
            }
        }

        /// <summary>Changes the zoom percentage while preserving pan.</summary>
        /// <param name="percent">The finite requested zoom percentage.</param>
        public void ChangeZoom(double percent)
        {
            this.SetViewport(this.Viewport.PanX, this.Viewport.PanY, Finite(percent) / 100);
        }

        /// <summary>Restores the zoom to one hundred percent.</summary>
        public void ResetZoom()
        {
            this.ChangeZoom(100);
        }

        /// <summary>Replaces the finite, bounded viewport snapshot.</summary>
        /// <param name="panX">The finite horizontal translation.</param>
        /// <param name="panY">The finite vertical translation.</param>
        /// <param name="zoom">The finite scale, clamped to supported bounds.</param>
        /// <param name="isInitialized">Whether the viewport has been explicitly initialized.</param>
        public void SetViewport(double panX, double panY, double zoom, bool isInitialized = true)
        {
            if (this.disposed)
            {
                return;
            }

            this.Viewport = new DiagramViewport(Finite(panX), Finite(panY), Math.Clamp(Finite(zoom), DiagramViewport.MinimumZoom, DiagramViewport.MaximumZoom), isInitialized);
        }

        /// <summary>Fits the represented content within the supplied canvas bounds.</summary>
        /// <param name="width">The available canvas width.</param>
        /// <param name="height">The available canvas height.</param>
        public void FitToView(double width, double height)
        {
            if (width <= 0 || height <= 0 || !double.IsFinite(width) || !double.IsFinite(height) || this.Projection.Nodes.IsEmpty)
            {
                return;
            }

            var bounds = DiagramBoundsCalculator.Measure(this.Projection);
            var zoom = Math.Clamp(Math.Min(width / (bounds.Right - bounds.Left), height / (bounds.Bottom - bounds.Top)), DiagramViewport.MinimumZoom, 1);
            this.SetViewport((width - (bounds.Right - bounds.Left) * zoom) / 2 - bounds.Left * zoom,
                (height - (bounds.Bottom - bounds.Top) * zoom) / 2 - bounds.Top * zoom, zoom);
        }

        /// <summary>Gets or creates nested state owned by the exact parent occurrence.</summary>
        /// <param name="occurrenceId">The parent graphical identity.</param>
        /// <param name="projection">The immutable nested projection.</param>
        /// <returns>The retained caller-owned nested state.</returns>
        public IDiagramViewModel GetNestedView(Guid occurrenceId, DiagramProjection projection)
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);
            ArgumentNullException.ThrowIfNull(projection);

            if (!this.nestedViews.TryGetValue((occurrenceId, projection), out var nested))
            {
                nested = this.createNestedView(projection);
                this.nestedViews.Add((occurrenceId, projection), nested);
            }

            return nested;
        }

        /// <summary>Detaches observation and releases child state without disposing canonical objects or the shared service.</summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Releases view-owned subscriptions and child state for derived diagram implementations.</summary>
        /// <param name="disposing">Whether managed resources are being released.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.selectionSubscription.Dispose();

            foreach (var nested in this.nestedViews.Values)
            {
                nested.Dispose();
            }

            this.nestedViews.Clear();
            this.selectionTargets.Clear();
            this.selectedTargets = Array.Empty<DiagramElementViewModel>();
            this.nodeStates.Clear();
            this.edgeStates.Clear();
        }

        /// <summary>Updates only previous and current selection decorations, including repeated occurrences.</summary>
        /// <param name="element">The current canonical selection, or null.</param>
        private void ApplySelection(IElement element)
        {
            IReadOnlyList<DiagramElementViewModel> targets = element is not null && this.selectionTargets.TryGetValue(element, out var matches)
                ? matches
                : Array.Empty<DiagramElementViewModel>();

            foreach (var previous in this.selectedTargets.Where(previous => !targets.Contains(previous)))
            {
                previous.SetSelected(false);
            }

            foreach (var target in targets)
            {
                target.SetSelected(true);
            }

            this.selectedTargets = targets;
        }

        /// <summary>Registers a canonical reference without confusing models that reuse semantic IDs.</summary>
        /// <param name="target">The occurrence presentation that can display selection.</param>
        private void RegisterSelectionTarget(DiagramElementViewModel target)
        {
            if (!this.selectionTargets.TryGetValue(target.Element, out var targets))
            {
                targets = [];
                this.selectionTargets.Add(target.Element, targets);
            }

            targets.Add(target);
        }

        /// <summary>Reconciles node presentation identity independently of mounted native resources.</summary>
        /// <param name="projection">The validated incoming snapshot.</param>
        private void UpdateNodes(DiagramProjection projection)
        {
            var retained = new Dictionary<Guid, DiagramNodeViewModel>();

            foreach (var node in projection.Nodes)
            {
                var state = this.nodeStates.TryGetValue(node.Occurrence.Id, out var existing) && ReferenceEquals(existing.Element, node.Occurrence.Element)
                    ? existing
                    : new DiagramNodeViewModel(node, this);
                state.Update(node);
                retained.Add(node.Occurrence.Id, state);
            }

            this.nodeStates.Clear();

            foreach (var pair in retained)
            {
                this.nodeStates.Add(pair.Key, pair.Value);
            }
        }

        /// <summary>Reconciles relationship presentation while retaining unchanged graphical identities.</summary>
        /// <param name="projection">The validated incoming snapshot.</param>
        private void UpdateEdges(DiagramProjection projection)
        {
            var retained = new Dictionary<Guid, DiagramEdgeViewModel>();

            foreach (var edge in projection.Edges)
            {
                var state = this.edgeStates.TryGetValue(edge.Id, out var existing) && ReferenceEquals(existing.Element, edge.Element)
                    ? existing
                    : new DiagramEdgeViewModel(edge, this);
                state.Update(edge);
                retained.Add(edge.Id, state);
            }

            this.edgeStates.Clear();

            foreach (var pair in retained)
            {
                this.edgeStates.Add(pair.Key, pair.Value);
            }
        }

        /// <summary>Releases child subscriptions whose graphical occurrence, canonical parent or nested snapshot is no longer represented.</summary>
        /// <param name="projection">The incoming parent snapshot.</param>
        private void ReleaseRemovedNestedViews(DiagramProjection projection)
        {
            var previousParents = this.Projection?.Nodes.ToDictionary(node => node.Occurrence.Id, node => node.Occurrence.Element);
            var retained = projection.Nodes.OfType<DiagramShellPresentation>().Where(node => previousParents is not null
                && previousParents.TryGetValue(node.Occurrence.Id, out var parent) && ReferenceEquals(parent, node.Occurrence.Element))
                .SelectMany(node => node.Compartments.Select(compartment => compartment.GraphicalView)
                .Append(node.NestedView).Where(view => view is not null).Select(view => (node.Occurrence.Id, view))).ToHashSet();

            foreach (var key in this.nestedViews.Keys.Where(key => !retained.Contains(key)).ToArray())
            {
                this.nestedViews[key].Dispose();
                this.nestedViews.Remove(key);
            }
        }

        /// <summary>Rejects a non-finite viewport value before it reaches the native renderer.</summary>
        /// <param name="value">The requested translation, zoom, or zoom percentage.</param>
        /// <returns>The unchanged finite value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The value is NaN or infinite.</exception>
        private static double Finite(double value)
        {
            return double.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }

    }
}
