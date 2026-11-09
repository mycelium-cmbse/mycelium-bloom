// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramViewModel.Loading.cs" company="Starion Group S.A.">
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
    using System.Reactive.Subjects;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Model.Diagram;
    using ReactiveUI;

    public sealed partial class DiagramViewModel
    {
        private readonly Subject<DiagramPresentationState> regionRequests = new();
        private IDisposable loadingSubscription;
        private IDisposable dataLease;
        private long loadingRevision;
        private DiagramPresentationState requestedState;
        private double requestedWidth;
        private double requestedHeight;
        private bool eagerControl;

        /// <summary>Gets the optional authorized on-demand session.</summary>
        public DiagramModelSession DataSession { get; private set; }

        /// <summary>Gets whether renderer models are restricted to the requested region.</summary>
        public bool UsesRegionalRendering => this.DataSession is not null || this.IsWorkspaceBound;

        /// <summary>Gets the current renderer subset independently of full presentation state.</summary>
        public DiagramPresentationState Region
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>Gets detail-loading failures without treating missing data as semantic deletion.</summary>
        public string LoadingError
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        } = string.Empty;

        /// <summary>Replaces template initialization with metadata from an explicit on-demand source.</summary>
        /// <param name="session">The caller-owned model session.</param>
        /// <param name="eagerControl">Whether an isolated measurement explicitly requests complete loading.</param>
        public void InitializeOnDemand(DiagramModelSession session, bool eagerControl = false)
        {
            lock (this.mutationGate)
            {
                ObjectDisposedException.ThrowIf(this.disposed, this);
                if (!DiagramPresentationSerializer.Validate(session.Source.Presentation, session.Source.Metadata,
                        out var error, this.Limits))
                {
                    throw new ArgumentException(error, nameof(session));
                }

                this.loadingRevision++;
                this.selectionRevision++;
                this.loadingSubscription?.Dispose();
                this.dataLease?.Dispose();
                this.requestedState = null;
                this.DataSession = session;
                this.eagerControl = eagerControl;
                this.dataLease = session.Acquire();
                this.Projection = session.Source.Metadata;
                var presentation = session.Source.Presentation with { DiagramId = Guid.NewGuid() };
                this.Region = presentation with { Occurrences = [], Routes = [] };
                this.StartRegionLoading();
                this.State = presentation;
            }
        }

        private void StartRegionLoading()
        {
            this.loadingSubscription?.Dispose();
            this.loadingSubscription = this.regionRequests
                .Select(region => Observable.FromAsync(token => this.LoadRegionAsync(region, token)))
                .Switch().Subscribe(_ => { });
        }

        /// <summary>Requests only viewport details and bounded endpoint/containment dependencies.</summary>
        /// <param name="width">The pane width in CSS pixels.</param>
        /// <param name="height">The pane height in CSS pixels.</param>
        /// <param name="margin">The prefetch margin in CSS pixels.</param>
        public void RequestRegion(double width, double height, double margin)
        {
            lock (this.mutationGate)
            {
                if (this.disposed || !this.IsScopeAvailable || this.State is null
                    || (this.DataSession is null && this.workspaceIndex is null) || width <= 0 || height <= 0)
                {
                    return;
                }

                if (this.eagerControl && this.requestedState is not null)
                {
                    return;
                }

                if (this.requestedState is { } previous && previous.Occurrences == this.State.Occurrences
                                                        && previous.Routes == this.State.Routes &&
                                                        this.requestedWidth == width && this.requestedHeight == height
                                                        && previous.Viewport.Zoom == this.State.Viewport.Zoom
                                                        && Math.Abs(previous.Viewport.X - this.State.Viewport.X) <
                                                        margin / 2
                                                        && Math.Abs(previous.Viewport.Y - this.State.Viewport.Y) <
                                                        margin / 2)
                {
                    return;
                }

                this.requestedState = this.State;
                this.requestedWidth = width;
                this.requestedHeight = height;
                var region = this.eagerControl ? this.State : SelectRegion(this.State, width, height, margin);
                if (this.Region is not null && this.Region.Occurrences.SequenceEqual(region.Occurrences)
                                            && this.Region.Routes.SequenceEqual(region.Routes))
                {
                    return;
                }

                if (this.workspaceIndex is not null && this.Region is not null
                                                    && this.Region.Occurrences.Select(node =>
                                                            (node.Id, node.ElementId, node.ParentId))
                                                        .SequenceEqual(region.Occurrences.Select(node =>
                                                            (node.Id, node.ElementId, node.ParentId)))
                                                    && this.Region.Routes.Select(route => (route.Id, route.ElementId,
                                                            route.SourceId, route.TargetId))
                                                        .SequenceEqual(region.Routes.Select(route =>
                                                            (route.Id, route.ElementId, route.SourceId,
                                                                route.TargetId))))
                {
                    this.Region = region;
                    return;
                }

                if (!this.eagerControl && this.workspaceIndex is null)
                {
                    this.Region = region;
                }

                this.regionRequests.OnNext(region);
            }
        }

        /// <summary>Reveals a known occurrence even when its semantic selection has not changed.</summary>
        /// <param name="elementId">The canonical identity to reveal.</param>
        /// <param name="width">The pane width.</param>
        /// <param name="height">The pane height.</param>
        /// <param name="minimumZoom">The configured detailed zoom limit.</param>
        /// <returns>The selected occurrence, or null when absent.</returns>
        public Guid? RevealOccurrence(string elementId, double width, double height, double minimumZoom)
        {
            lock (this.mutationGate)
            {
                if (this.disposed || this.State is null)
                {
                    return null;
                }

                var partId = this.Projection.Parts.ContainsKey(elementId)
                    ? elementId
                    : this.Projection.Parts.Values
                        .FirstOrDefault(part => part.Ports.Any(port => port.ElementId == elementId))?.ElementId;
                var occurrences = this.State.Occurrences.Where(node => node.ElementId == partId);
                var occurrence = occurrences.FirstOrDefault(node => this.SelectedOccurrences.Contains(node.Id)) ??
                                 occurrences.FirstOrDefault();
                if (occurrence is null)
                {
                    return null;
                }

                var geometry = occurrence.Geometry;
                var zoom = Math.Max(minimumZoom, this.State.Viewport.Zoom);
                this.ApplyPresentation(this.State with
                {
                    Viewport = new DiagramViewport(
                        width / 2 - (geometry.X + geometry.Width / 2) * zoom,
                        height / 2 - (geometry.Y + geometry.Height / 2) * zoom, zoom)
                });
                return occurrence.Id;
            }
        }

        private async Task LoadRegionAsync(DiagramPresentationState region, CancellationToken cancellationToken)
        {
            long revision;
            DiagramModelSession session;
            lock (this.mutationGate)
            {
                revision = ++this.loadingRevision;
                session = this.DataSession;
                if (this.workspaceIndex is not null)
                {
                    if (!this.disposed && !cancellationToken.IsCancellationRequested)
                    {
                        this.Projection = this.workspaceIndex.Project(region, cancellationToken);
                        this.Region = region;
                    }

                    return;
                }
            }

            if (session is null)
            {
                return;
            }

            var parts = session.Source.Metadata.Parts.ToBuilder();
            var connections = session.Source.Metadata.Connections.ToBuilder();
            var visible = SelectRegion(region, this.requestedWidth, this.requestedHeight, 0).Occurrences
                .Select(node => node.Id).ToHashSet();
            foreach (var ids in region.Occurrences.OrderByDescending(node => visible.Contains(node.Id))
                         .Select(node => node.ElementId).Chunk(28))
            {
                string failure = null;
                try
                {
                    var projections = await session.LoadAsync(ids, !this.eagerControl, cancellationToken);
                    foreach (var projection in projections)
                    {
                        foreach (var pair in projection.Parts)
                        {
                            parts[pair.Key] = pair.Value;
                        }

                        foreach (var pair in projection.Connections)
                        {
                            connections[pair.Key] = pair.Value;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException
                                                  or InvalidOperationException)
                {
                    failure = "Model details unavailable; navigate away and return to retry.";
                    foreach (var id in ids)
                    {
                        parts[id] = parts[id] with { DetailStatus = "Unavailable" };
                    }
                }

                lock (this.mutationGate)
                {
                    if (this.disposed || revision != this.loadingRevision ||
                        cancellationToken.IsCancellationRequested || !ReferenceEquals(session, this.DataSession))
                    {
                        return;
                    }

                    this.LoadingError = failure ?? string.Empty;
                    this.Projection = session.Source.Metadata with
                    {
                        Parts = parts.ToImmutable(),
                        Connections = connections.ToImmutable()
                    };
                }
            }

            lock (this.mutationGate)
            {
                if (this.eagerControl && !this.disposed && revision == this.loadingRevision &&
                    !cancellationToken.IsCancellationRequested)
                {
                    this.Region = region;
                }
            }
        }

        /// <summary>Retrieves all missing details without populating the interactive payload cache.</summary>
        /// <param name="cancellationToken">The export lifetime.</param>
        /// <returns>A complete, consistent export projection.</returns>
        public async Task<DiagramProjection> LoadExportProjectionAsync(CancellationToken cancellationToken)
        {
            var session = this.DataSession;
            lock (this.mutationGate)
            {
                if (this.workspaceIndex is not null)
                {
                    return this.workspaceIndex.Project(this.State, cancellationToken);
                }
            }

            if (session is null)
            {
                return this.Projection;
            }

            var state = this.State;
            var metadata = session.Source.Metadata;
            var parts = metadata.Parts.ToBuilder();
            var connections = metadata.Connections.ToBuilder();
            foreach (var ids in state.Occurrences.Select(node => node.ElementId).Distinct(StringComparer.Ordinal)
                         .Chunk(28))
            {
                var projections = await session.LoadAsync(ids, false, cancellationToken);
                foreach (var projection in projections)
                {
                    foreach (var pair in projection.Parts)
                    {
                        parts[pair.Key] = pair.Value;
                    }

                    foreach (var pair in projection.Connections)
                    {
                        connections[pair.Key] = pair.Value;
                    }
                }
            }

            if (!ReferenceEquals(session, this.DataSession) || this.disposed)
            {
                throw new OperationCanceledException("The model session changed.");
            }

            if (state.Occurrences.Any(node => string.IsNullOrEmpty(parts[node.ElementId].Name)))
            {
                throw new InvalidDataException("Incomplete export details.");
            }

            return metadata with { Parts = parts.ToImmutable(), Connections = connections.ToImmutable() };
        }

        /// <summary>Chooses intersecting occurrences and connections using only full-scene metadata.</summary>
        /// <param name="state">The complete presentation.</param>
        /// <param name="width">The pane width.</param>
        /// <param name="height">The pane height.</param>
        /// <param name="margin">The CSS-pixel margin.</param>
        /// <returns>A renderer subset with complete endpoint and parent geometry.</returns>
        public static DiagramPresentationState SelectRegion(DiagramPresentationState state, double width, double height,
            double margin)
        {
            var viewport = state.Viewport;
            var left = (-viewport.X - margin) / viewport.Zoom;
            var top = (-viewport.Y - margin) / viewport.Zoom;
            var right = (width - viewport.X + margin) / viewport.Zoom;
            var bottom = (height - viewport.Y + margin) / viewport.Zoom;

            bool Intersects(DiagramGeometry geometry) => geometry.X <= right && geometry.X + geometry.Width >= left
                                                                             && geometry.Y <= bottom &&
                                                                             geometry.Y + geometry.Height >= top;

            var byId = state.Occurrences.ToDictionary(node => node.Id);
            var included = state.Occurrences.Where(node => Intersects(node.Geometry)).Select(node => node.Id)
                .ToHashSet();
            var routes = state.Routes.Where(route =>
            {
                var source = byId[route.SourceId].Geometry;
                var target = byId[route.TargetId].Geometry;
                var xs = route.Bends.Select(point => point.X).Append(source.X).Append(source.X + source.Width)
                    .Append(target.X).Append(target.X + target.Width).ToArray();
                var ys = route.Bends.Select(point => point.Y).Append(source.Y).Append(source.Y + source.Height)
                    .Append(target.Y).Append(target.Y + target.Height).ToArray();
                return Intersects(new DiagramGeometry(xs.Min(), ys.Min(), xs.Max() - xs.Min(), ys.Max() - ys.Min()));
            }).ToImmutableArray();
            foreach (var route in routes)
            {
                included.Add(route.SourceId);
                included.Add(route.TargetId);
            }

            foreach (var id in included.ToArray())
            {
                var current = id;
                while (byId[current].ParentId is { } parent && included.Add(parent))
                {
                    current = parent;
                }
            }

            return state with
            {
                Occurrences = [.. state.Occurrences.Where(node => included.Contains(node.Id))],
                Routes = routes
            };
        }
    }
}