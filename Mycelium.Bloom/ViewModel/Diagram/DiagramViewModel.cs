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
    using System.Reactive.Linq;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.SDK;
    using ReactiveUI;
    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>Owns one editor tab's durable, presentation-only interconnection state.</summary>
    public sealed partial class DiagramViewModel : ReactiveObject, IDisposable
    {
        /// <summary>Serializes snapshot publication against asynchronous resolution and final disposal.</summary>
        private readonly object mutationGate = new();

        /// <summary>The circuit's existing selection and lifecycle authority.</summary>
        private readonly IContextAwareService context;

        /// <summary>The canonical identity resolution boundary.</summary>
        private readonly IElementIdResolver resolver;

        /// <summary>Cancels pending canonical selections when the tab closes.</summary>
        private readonly CancellationTokenSource lifetime = new();

        /// <summary>Derives presentation permission from the local inspection setting and lifecycle.</summary>
        private readonly ObservableAsPropertyHelper<bool> canEditPresentation;

        /// <summary>Tracks the owner lifetime under the mutation gate.</summary>
        private bool disposed;

        /// <summary>Rejects canonical selection completions superseded by a later selection.</summary>
        private long selectionRevision;

        /// <summary>Creates caller-owned tab state using the circuit's shared services.</summary>
        /// <param name="context">The circuit context and selection authority.</param>
        /// <param name="resolver">The existing canonical identity resolver.</param>
        /// <param name="dataOptions">The scoped read-cache configuration.</param>
        /// <param name="cacheClock">An optional clock for deterministic cache expiration.</param>
        public DiagramViewModel(IContextAwareService context, IElementIdResolver resolver,
            Microsoft.Extensions.Options.IOptions<DiagramDataOptions> dataOptions = null,
            Microsoft.Extensions.Internal.ISystemClock cacheClock = null)
        {
            this.context = context;
            this.resolver = resolver;
            this.workspaceDataOptions = dataOptions?.Value ?? new DiagramDataOptions();
            this.workspaceCacheClock = cacheClock;
            this.RevealRequested = ReactiveCommand.Create<string, string>(elementId =>
            {
                lock (this.mutationGate)
                {
                    if (this.disposed)
                    {
                        return null;
                    }

                    this.PendingRevealElementId = elementId;
                    return elementId;
                }
            });
            this.canEditPresentation = context.WhenAnyValue(value => value.LifecycleState)
                .CombineLatest(this.WhenAnyValue(value => value.IsReadOnly),
                    (lifecycle, readOnly) =>
                        !readOnly && lifecycle is ProjectLifecycleKind.Preparation or ProjectLifecycleKind.Open)
                .ToProperty(this, value => value.CanEditPresentation);
        }

        /// <summary>Gets the explicit navigation intent independently of semantic selection changes.</summary>
        public ReactiveCommand<string, string> RevealRequested { get; }

        /// <summary>Gets explicit navigation awaiting a mounted renderer.</summary>
        public string PendingRevealElementId
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>Consumes a navigation request without clearing a newer intent.</summary>
        /// <param name="elementId">The identity handled by the mounted renderer.</param>
        public void CompleteReveal(string elementId)
        {
            lock (this.mutationGate)
            {
                if (!this.disposed && this.PendingRevealElementId == elementId)
                {
                    this.PendingRevealElementId = null;
                }
            }
        }

        /// <summary>Gets the immutable supported semantic projection.</summary>
        public DiagramProjection Projection
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>Gets the immutable capacity selected before this tab is initialized.</summary>
        public DiagramPresentationLimits Limits { get; private set; } = DiagramPresentationLimits.Default;

        /// <summary>Gets the authoritative presentation snapshot.</summary>
        public DiagramPresentationState State
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>Gets the local occurrence selection, independently of browser focus.</summary>
        public ImmutableHashSet<Guid> SelectedOccurrences
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        } = [];

        /// <summary>Gets or sets the local read-only state.</summary>
        public bool IsReadOnly
        {
            get;
            set
            {
                lock (this.mutationGate)
                {
                    if (!this.disposed)
                    {
                        this.RaiseAndSetIfChanged(ref field, value);
                    }
                }
            }
        }

        /// <summary>Gets whether presentation changes are currently permitted.</summary>
        public bool CanEditPresentation => this.IsScopeAvailable && this.canEditPresentation.Value;

        /// <summary>Gets the latest transient presentation operation result.</summary>
        public string Notice
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        } = string.Empty;

        /// <summary>Reports a completed UI operation while its tab still exists.</summary>
        /// <param name="message">The user-facing result.</param>
        public void ReportNotice(string message)
        {
            lock (this.mutationGate)
            {
                if (!this.disposed)
                {
                    this.Notice = message;
                }
            }
        }

        /// <summary>Adds the browser's shared selection as a graphical occurrence.</summary>
        /// <returns>Whether the selected element is supported in this model scope.</returns>
        public bool AddSharedSelection()
        {
            lock (this.mutationGate)
            {
                if (this.disposed || this.State is null || !this.CanEditPresentation)
                {
                    return false;
                }

                var elementId = this.context.SelectedElement?.ElementId;
                if (elementId is null || !this.Projection.Parts.ContainsKey(elementId)
                                      || this.workspaceElements is not null &&
                                      !ReferenceEquals(this.workspaceElements.GetValueOrDefault(elementId),
                                          this.context.SelectedElement))
                {
                    this.Notice = "Select a part from this diagram scope in Project Browser first.";
                    return false;
                }

                return this.AddExisting(elementId);
            }
        }

        /// <summary>Adds an editable manual waypoint to the selected or first connection.</summary>
        public void AddManualBend()
        {
            lock (this.mutationGate)
            {
                if (this.disposed || !this.CanEditPresentation || this.State is null || this.State.Routes.Length == 0)
                {
                    return;
                }

                var route = this.State.Routes.FirstOrDefault(item => this.SelectedOccurrences.Contains(item.Id)) ??
                            this.State.Routes[0];
                var source = this.State.Occurrences.Single(node => node.Id == route.SourceId).Geometry;
                var target = this.State.Occurrences.Single(node => node.Id == route.TargetId).Geometry;
                var updated = route with
                {
                    Bends =
                    [
                        new DiagramPoint((source.X + source.Width + target.X) / 2,
                            Math.Max(source.Y, target.Y) + 180)
                    ]
                };
                var candidate = this.State with { Routes = this.State.Routes.Replace(route, updated) };
                if (!this.ValidatePresentation(candidate))
                {
                    return;
                }

                this.SelectedOccurrences = [route.Id];
                this.State = candidate;
            }
        }

        /// <summary>Publishes validated renderer geometry without modifying canonical model objects.</summary>
        /// <param name="candidate">The completed presentation change.</param>
        /// <returns>Whether the change was accepted.</returns>
        public bool ApplyPresentation(DiagramPresentationState candidate)
        {
            lock (this.mutationGate)
            {
                if (this.disposed || !this.IsScopeAvailable || this.State is null || candidate is null
                    || candidate.DiagramId != this.State.DiagramId
                    || !this.ValidateCandidate(candidate))
                {
                    return false;
                }

                if (!this.CanEditPresentation && (candidate.Occurrences != this.State.Occurrences ||
                                                  candidate.Routes != this.State.Routes))
                {
                    return false;
                }

                this.State = candidate;
                return true;
            }
        }

        private bool ValidateCandidate(DiagramPresentationState candidate)
        {
            if (candidate.Occurrences == this.State.Occurrences && candidate.Routes == this.State.Routes
                                                                && candidate.Version == this.State.Version &&
                                                                candidate.ModelScope == this.State.ModelScope)
            {
                return candidate.Viewport is { Zoom: <= 4 } viewport && double.IsFinite(viewport.Zoom)
                                                                     && viewport.Zoom >= this.Limits.MinimumZoom &&
                                                                     double.IsFinite(viewport.X) &&
                                                                     double.IsFinite(viewport.Y)
                                                                     && Math.Abs(viewport.X) <= this.Limits.Pan &&
                                                                     Math.Abs(viewport.Y) <= this.Limits.Pan;
            }

            return DiagramPresentationSerializer.Validate(candidate, this.Projection, out _, this.Limits);
        }

        /// <summary>Completes group translation for descendants outside the mounted region before normal validation.</summary>
        /// <param name="expected">The exact snapshot observed by the renderer.</param>
        /// <param name="candidate">The completed graphical changes.</param>
        /// <param name="renderedIds">The occurrences whose geometry the renderer supplied.</param>
        /// <returns>Whether the complete presentation change was accepted.</returns>
        public bool ApplyRenderedPresentation(DiagramPresentationState expected, DiagramPresentationState candidate,
            IReadOnlySet<Guid> renderedIds)
        {
            lock (this.mutationGate)
            {
                if (this.disposed || expected is null || candidate is null || renderedIds is null
                    || !ReferenceEquals(expected, this.State) || candidate.Occurrences.IsDefault)
                {
                    return false;
                }

                var original = expected.Occurrences.ToDictionary(node => node.Id);
                if (candidate.Occurrences.Any(node =>
                        node is null || node.Geometry is null || !original.ContainsKey(node.Id)))
                {
                    return false;
                }

                var moved = candidate.Occurrences.Where(node =>
                        renderedIds.Contains(node.Id) && node.Geometry != original[node.Id].Geometry)
                    .ToDictionary(node => node.Id,
                        node => new DiagramPoint(node.Geometry.X - original[node.Id].Geometry.X,
                            node.Geometry.Y - original[node.Id].Geometry.Y));
                if (moved.Count == 0)
                {
                    return this.ApplyPresentation(candidate);
                }

                var occurrences = candidate.Occurrences.Select(node =>
                {
                    if (renderedIds.Contains(node.Id))
                    {
                        return node;
                    }

                    var current = node;
                    while (current.ParentId is { } parent)
                    {
                        if (moved.TryGetValue(parent, out var delta))
                        {
                            return node with
                            {
                                Geometry = node.Geometry with
                                {
                                    X = node.Geometry.X + delta.X,
                                    Y = node.Geometry.Y + delta.Y
                                }
                            };
                        }

                        current = original[parent];
                    }

                    return node;
                }).ToImmutableArray();
                return this.ApplyPresentation(candidate with { Occurrences = occurrences });
            }
        }

        /// <summary>Adds a graphical occurrence of an existing supported element in this scope.</summary>
        /// <param name="elementId">The stable semantic identity from the browser or chooser.</param>
        /// <returns>Whether an occurrence was added.</returns>
        public bool AddExisting(string elementId)
        {
            lock (this.mutationGate)
            {
                if (this.disposed || !this.CanEditPresentation || this.State is null
                    || elementId is null || !this.Projection.Parts.ContainsKey(elementId))
                {
                    return false;
                }

                if (this.State.Occurrences.Length >= this.Limits.Nodes)
                {
                    this.Notice = $"This diagram supports at most {this.Limits.Nodes:N0} occurrences.";
                    return false;
                }

                var viewport = this.State.Viewport;
                var node = new DiagramOccurrence(Guid.NewGuid(), elementId,
                    new DiagramGeometry((80 - viewport.X) / viewport.Zoom, (80 - viewport.Y) / viewport.Zoom, 240, 270),
                    null);
                var candidate = this.State with { Occurrences = this.State.Occurrences.Add(node) };
                if (!this.ValidatePresentation(candidate))
                {
                    return false;
                }

                this.State = candidate;
                return true;
            }
        }

        /// <summary>Publishes local selection and resolves the selected canonical object for Details.</summary>
        /// <param name="occurrenceIds">The renderer's complete local selection.</param>
        /// <param name="primaryId">The occurrence most recently selected, or null.</param>
        /// <returns>A task completing canonical selection publication.</returns>
        public Task SelectAsync(ImmutableHashSet<Guid> occurrenceIds, Guid? primaryId) =>
            this.SelectAsync(occurrenceIds, primaryId, CancellationToken.None);

        /// <summary>Resolves selection only while both its tab and mounted renderer remain alive.</summary>
        /// <param name="occurrenceIds">The complete local selection.</param>
        /// <param name="primaryId">The most recently selected occurrence.</param>
        /// <param name="cancellationToken">The mounted renderer lifetime.</param>
        /// <returns>A task completing canonical selection publication.</returns>
        public async Task SelectAsync(ImmutableHashSet<Guid> occurrenceIds, Guid? primaryId,
            CancellationToken cancellationToken)
        {
            string elementId;
            CancellationToken token;
            long revision;
            IElement workspaceElement;
            bool workspaceBound;

            lock (this.mutationGate)
            {
                if (this.disposed || !this.IsScopeAvailable || this.State is null ||
                    cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var validIds = this.State.Occurrences.Select(node => node.Id)
                    .Concat(this.State.Routes.Select(route => route.Id)).ToImmutableHashSet();
                this.SelectedOccurrences = occurrenceIds.Intersect(validIds);
                revision = ++this.selectionRevision;
                elementId = this.State.Occurrences.FirstOrDefault(node => node.Id == primaryId)?.ElementId
                            ?? this.State.Routes.FirstOrDefault(route => route.Id == primaryId)?.ElementId;
                token = this.lifetime.Token;
                workspaceBound = this.IsWorkspaceBound;
                workspaceElement = this.workspaceElements?.GetValueOrDefault(elementId ?? string.Empty);
            }

            if (elementId is null)
            {
                return;
            }

            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, cancellationToken);
                var element = workspaceBound
                    ? workspaceElement
                    : await this.resolver.ResolveAsync(elementId, linked.Token);

                lock (this.mutationGate)
                {
                    if (!this.disposed && this.IsScopeAvailable && !token.IsCancellationRequested &&
                        !cancellationToken.IsCancellationRequested && element is not null
                        && revision == this.selectionRevision
                        && this.SelectedOccurrences.Contains(primaryId.Value))
                    {
                        this.context.SelectedElement = element;
                    }
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested ||
                                                     cancellationToken.IsCancellationRequested)
            {
                // Closed or superseded owners cannot receive this completion.
            }
        }

        /// <summary>Restores a validated presentation atomically while retaining the current tab identity.</summary>
        /// <param name="json">The presentation file.</param>
        /// <param name="error">The import failure, when present.</param>
        /// <returns>Whether the presentation was restored.</returns>
        public bool Import(string json, out string error)
        {
            lock (this.mutationGate)
            {
                error = "Presentation changes are unavailable.";

                if (this.disposed || !this.CanEditPresentation || this.State is null
                    || !DiagramPresentationSerializer.TryImport(json, this.Projection, out var imported, out error,
                        this.Limits))
                {
                    return false;
                }

                this.SelectedOccurrences = [];
                this.State = imported with { DiagramId = this.State.DiagramId };
                return true;
            }
        }

        /// <summary>Releases tab-owned reactive state and cancels pending selections.</summary>
        public void Dispose()
        {
            lock (this.mutationGate)
            {
                if (this.disposed)
                {
                    return;
                }

                this.disposed = true;
                this.lifetime.Cancel();
                this.lifetime.Dispose();
                this.canEditPresentation.Dispose();
                this.workspaceSubscription?.Dispose();
                this.workspacePreparation?.Cancel();
                this.workspacePreparation?.Dispose();
                this.workspaceIndex?.Dispose();
                this.workspaceIndex = null;
                this.workspaceElements = null;
                this.RevealRequested.Dispose();
                this.loadingSubscription?.Dispose();
                this.regionRequests.Dispose();

            }
        }

        /// <summary>Reports invalid generated presentation before publishing any state changes.</summary>
        private bool ValidatePresentation(DiagramPresentationState candidate)
        {
            if (DiagramPresentationSerializer.Validate(candidate, this.Projection, out var error, this.Limits))
            {
                return true;
            }

            this.Notice = error;
            return false;
        }

    }
}