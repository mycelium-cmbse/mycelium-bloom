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
    using Mycelium.Bloom.Model.Diagram;
    using ReactiveUI;

    public sealed partial class DiagramViewModel
    {
        /// <summary>Publishes regional presentation requests owned by this tab.</summary>
        private readonly Subject<DiagramPresentationState> regionRequests = new();
        /// <summary>Owns the current context's cancellable regional projection pipeline.</summary>
        private IDisposable loadingSubscription;
        /// <summary>Retains the exact accepted state associated with the last region request.</summary>
        private DiagramPresentationState requestedState;
        /// <summary>Retains the last requested pane width in CSS pixels.</summary>
        private double requestedWidth;
        /// <summary>Retains the last requested pane height in CSS pixels.</summary>
        private double requestedHeight;
        /// <summary>Gets whether renderer models are restricted to the requested region.</summary>
        public bool UsesRegionalRendering => this.IsWorkspaceBound;

        /// <summary>Gets the current renderer subset independently of full presentation state.</summary>
        public DiagramPresentationState Region
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        }


        /// <summary>Replaces the context-owned regional projection subscription.</summary>
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
                    || this.workspaceIndex is null || !double.IsFinite(width) || width <= 0
                    || !double.IsFinite(height) || height <= 0 || !double.IsFinite(margin) || margin < 0)
                {
                    return;
                }

                if (this.requestedState is { } previous && previous.Occurrences == this.State.Occurrences
                                                        && previous.Routes == this.State.Routes &&
                                                        (this.requestedWidth, this.requestedHeight, previous.Viewport.Zoom)
                                                            .Equals((width, height, this.State.Viewport.Zoom))
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
                var region = SelectRegion(this.State, width, height, margin);
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

        /// <summary>Publishes regional details under the tab's coherent state lock.</summary>
        /// <param name="region">The requested occurrence and connection subset.</param>
        /// <param name="cancellationToken">Cancels a superseded request.</param>
        /// <returns>Completion of the regional projection.</returns>
        private Task LoadRegionAsync(DiagramPresentationState region, CancellationToken cancellationToken)
        {
            lock (this.mutationGate)
            {
                if (this.workspaceIndex is not null && !this.disposed && !cancellationToken.IsCancellationRequested)
                {
                    this.Projection = this.workspaceIndex.Project(region, cancellationToken);
                    this.Region = region;
                }
            }

            return Task.CompletedTask;
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
