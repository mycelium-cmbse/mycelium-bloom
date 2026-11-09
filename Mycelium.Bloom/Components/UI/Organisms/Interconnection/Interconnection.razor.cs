// ------------------------------------------------------------------------------------------------
// <copyright file="Interconnection.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Interconnection
{
    using System.Collections.Immutable;
    using System.Reactive;
    using System.Reactive.Subjects;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using Blazor.Diagrams;
    using Blazor.Diagrams.Core.Behaviors;
    using Blazor.Diagrams.Core.Geometry;
    using Blazor.Diagrams.Core.Models;
    using Blazor.Diagrams.Core.Models.Base;
    using Blazor.Diagrams.Core.PathGenerators;
    using Blazor.Diagrams.Options;
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Forms;
    using Microsoft.JSInterop;
    using Microsoft.Extensions.Options;
    using Microsoft.AspNetCore.Components.Web;
    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.ViewModel.Diagram;
    using ReactiveUI;

    /// <summary>Owns mounted rendering resources and dispatches arbitrary ViewModel publications to Blazor.</summary>
    public sealed partial class Interconnection : ComponentBase, IAsyncDisposable
    {
        /// <summary>Owns the current ViewModel observation and external library event adapters.</summary>
        private readonly SerialDisposable observation = new();

        /// <summary>Owns resources attached to the current rendering graph.</summary>
        private CompositeDisposable rendering = new();

        /// <summary>Maps durable occurrence identities to ephemeral library models.</summary>
        private readonly Dictionary<Guid, NodeModel> nodes = [];

        /// <summary>Maps durable connection identities to ephemeral library links.</summary>
        private readonly Dictionary<Guid, DiagramLinkModel> links = [];

        /// <summary>The currently mounted library instance.</summary>
        private BlazorDiagram diagram;

        /// <summary>The last coherent snapshot applied at the renderer boundary.</summary>
        private DiagramPresentationState presented;

        private DiagramPresentationState rendered;
        private DiagramProjection renderedProjection;
        private ImmutableArray<DiagramOccurrence> boundedOccurrences;
        private Rectangle completeBounds;

        /// <summary>The caller-owned state currently observed.</summary>
        private DiagramViewModel observed;

        /// <summary>The root used for canvas-scoped pointer capture and downloads.</summary>
        private ElementReference root;

        /// <summary>The mounted component's collocated module.</summary>
        private IJSObjectReference module;

        /// <summary>The initialization task awaited by final disposal.</summary>
        private Task initialization;

        private readonly CancellationTokenSource exportLifetime = new();

        /// <summary>Prevents JS-dependent library mounting during static rendering.</summary>
        private bool isInteractive;

        /// <summary>Prevents renderer-to-owner feedback while applying a snapshot.</summary>
        private bool applying;

        /// <summary>Marks the end of this rendering lifetime.</summary>
        private bool disposed;

        /// <summary>Gets model-opening actions owned by the workspace composition.</summary>
        [Parameter]
        public RenderFragment ModelActions { get; set; }

        /// <summary>Owns model-bound visibility for this rendering graph.</summary>
        private DiagramVisibility visibility;

        /// <summary>Pins selected renderers until the active pointer gesture completes.</summary>
        private bool interactionActive;

        /// <summary>Identifies the occurrence to focus after its graphical component mounts.</summary>
        private Guid? pendingFocus;

        private int focusAttempts;
        private (string ElementId, bool Explicit)? pendingReveal;

        /// <summary>Retains the last viewport intersection count for the density notice.</summary>
        private int visibleCount;

        /// <summary>Gets whether this mount uses native visibility culling.</summary>
        private bool Virtualized => !this.IsBenchmark || this.ViewportOptions.Value.Enabled;

        /// <summary>Gets the detailed editor minimum independently of serialization capacity.</summary>
        private double MinimumZoom =>
            Math.Max(this.ViewModel.Limits.MinimumZoom, this.ViewportOptions.Value.MinimumZoom);

        /// <summary>Gets or sets the validated viewport configuration.</summary>
        [Inject]
        private IOptions<DiagramViewportOptions> ViewportOptions { get; set; }

        /// <summary>Gets or sets the existing circuit selection authority.</summary>
        [Inject]
        private IContextAwareService Context { get; set; }

        /// <summary>Gets or sets services for detached Razor export.</summary>
        [Inject]
        private IServiceProvider Services { get; set; }

        /// <summary>Gets or sets logging for detached export.</summary>
        [Inject]
        private ILoggerFactory LoggerFactory { get; set; }

        /// <summary>Gets or sets the exact caller-owned state for this editor tab.</summary>
        [Parameter, EditorRequired]
        public DiagramViewModel ViewModel { get; set; }

        /// <summary>Gets or sets whether the composition selected an isolated benchmark scene.</summary>
        [Parameter]
        public bool IsBenchmark { get; set; }

        /// <summary>Gets or sets whether the explicitly enabled developer facility exposes diagnostic controls.</summary>
        [Parameter]
        public bool ShowDiagnostics { get; set; }

        /// <summary>Gets or sets the browser interop boundary.</summary>
        [Inject]
        private IJSRuntime JsRuntime { get; set; }

        /// <summary>Gets or sets logging for failed owned asynchronous work.</summary>
        [Inject]
        private ILogger<Interconnection> Logger { get; set; }

        /// <summary>Rebinds caller-owned state without retaining an earlier tab's subscriptions.</summary>
        protected override void OnParametersSet()
        {
            if (ReferenceEquals(this.observed, this.ViewModel))
            {
                return;
            }

            this.observed = this.ViewModel;
            this.pendingReveal = null;
            this.ClearRenderer();
            var owner = this.ViewModel;
            var owned = new CompositeDisposable();
            this.observation.Disposable = owned;
            owned.Add(owner.WhenAnyValue(
                    value => value.State, value => value.CanEditPresentation, value => value.IsReadOnly,
                    value => value.SelectedOccurrences, value => value.Notice, value => value.Region,
                    value => value.Projection)
                .Select(_ => Observable.FromAsync(() => this.InvokeAsync(() =>
                {
                    if (!this.disposed && ReferenceEquals(owner, this.observed))
                    {
                        this.Reconcile();
                        this.StateHasChanged();
                    }
                })))
                .Concat()
                .Subscribe(_ => { }, error => this.Logger.LogError(error, "Diagram rendering observation failed.")));
            var sharedReveals = owner.IsWorkspaceBound
                ? Observable.Empty<string>()
                : this.Context.WhenAnyValue(value => value.SelectedElement)
                    .Select(element => owner.ContainsCanonicalElement(element) ? element.ElementId : null)
                    .DistinctUntilChanged().Skip(1);
            owned.Add(sharedReveals
                .Select(id => (Id: id, Explicit: false))
                .Merge(owner.RevealRequested.Select(id => (Id: id, Explicit: true)))
                .Select(request => Observable.FromAsync(() => this.InvokeAsync(() =>
                {
                    if (!this.disposed && ReferenceEquals(owner, this.observed))
                    {
                        this.Reveal(request.Id, request.Explicit);
                    }
                }))).Concat().Subscribe(_ => { }, error => this.Logger.LogError(error, "Diagram reveal failed.")));
            if (owner.PendingRevealElementId is { } pending)
            {
                this.Reveal(pending, true);
            }
        }

        /// <summary>Starts browser resources only after interactive rendering begins.</summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (this.disposed)
            {
                return;
            }

            if (!firstRender)
            {
                if (this.pendingFocus is { } focus && this.module is not null &&
                    this.nodes.TryGetValue(focus, out var node) && node.Visible)
                {
                    var mounted = this.diagram;
                    var focused = await this.module.InvokeAsync<bool>("focusOccurrence", this.root, node.Id);
                    if (!this.disposed && this.pendingFocus == focus && ReferenceEquals(mounted, this.diagram))
                    {
                        if (focused)
                        {
                            this.pendingFocus = null;
                        }
                        else if (++this.focusAttempts <= 3)
                        {
                            this.StateHasChanged();
                        }
                    }
                }

                return;
            }

            this.isInteractive = true;
            this.Reconcile();
            this.initialization = this.InitializeBrowserAsync();
            await this.initialization;

            if (!this.disposed)
            {
                this.StateHasChanged();
            }
        }

        /// <summary>Imports and mounts component-scoped browser resources without outliving disposal.</summary>
        private async Task InitializeBrowserAsync()
        {
            var imported = await this.JsRuntime.InvokeAsync<IJSObjectReference>("import",
                "./Components/UI/Organisms/Interconnection/Interconnection.razor.js");
            if (this.disposed)
            {
                await imported.DisposeAsync();
                return;
            }

            this.module = imported;
            await imported.InvokeVoidAsync("mount", this.root);
        }

        /// <summary>Applies only changed topology and presentation, preserving existing model instances.</summary>
        private void Reconcile()
        {
            using var trace = Mycelium.Bloom.Core.DiagramDiagnostics.Source.StartActivity("reconcile");
            if (this.disposed || !this.isInteractive || this.ViewModel.State is not { } state)
            {
                return;
            }

            if (this.ViewModel.UsesRegionalRendering)
            {
                if (this.interactionActive)
                {
                    return;
                }

                var region = this.ViewModel.Region;
                var nodeIds = region?.Occurrences.Select(node => node.Id).ToHashSet() ?? [];
                var routeIds = region?.Routes.Select(route => route.Id).ToHashSet() ?? [];
                state = state with
                {
                    Occurrences = [.. state.Occurrences.Where(node => nodeIds.Contains(node.Id))],
                    Routes = [.. state.Routes.Where(route => routeIds.Contains(route.Id))]
                };
            }

            var previous = this.rendered;
            var topologyChanged = this.diagram is null || previous is null
                                                       || (this.ViewModel.UsesRegionalRendering &&
                                                           !ReferenceEquals(this.renderedProjection,
                                                               this.ViewModel.Projection))
                                                       || !previous.Occurrences.Select(node =>
                                                               (node.Id, node.ElementId, node.ParentId))
                                                           .SequenceEqual(state.Occurrences.Select(node =>
                                                               (node.Id, node.ElementId, node.ParentId)))
                                                       || !previous.Routes.Select(route =>
                                                               (route.Id, route.ElementId, route.SourceId,
                                                                   route.TargetId))
                                                           .SequenceEqual(state.Routes.Select(route =>
                                                               (route.Id, route.ElementId, route.SourceId,
                                                                   route.TargetId)));

            var alignments = topologyChanged || this.presented.Occurrences != this.ViewModel.State.Occurrences
                ? GetPortAlignments(this.ViewModel.State)
                : null;
            if (!topologyChanged && alignments is not null)
            {
                topologyChanged = this.nodes.Any(pair => pair.Value.Ports.Any(port =>
                    port.Alignment != alignments.GetValueOrDefault((pair.Key, port.Id), PortAlignment.Right)));
            }

            this.applying = true;
            try
            {
                if (topologyChanged)
                {
                    this.CreateRenderer(state, alignments);
                }

                foreach (var occurrence in state.Occurrences.OrderBy(node => node.ParentId.HasValue))
                {
                    var node = this.nodes[occurrence.Id];
                    var geometry = occurrence.Geometry;
                    var locked = !this.ViewModel.CanEditPresentation || occurrence.ParentId.HasValue;
                    var changed = node.Locked != locked
                                  || node.Selected != this.ViewModel.SelectedOccurrences.Contains(occurrence.Id);
                    node.Locked = locked;
                    var geometryChanged = node.Position.X != geometry.X || node.Position.Y != geometry.Y
                                                                        || node.Size.Width != geometry.Width ||
                                                                        node.Size.Height != geometry.Height;
                    if (node.Position.X != geometry.X || node.Position.Y != geometry.Y)
                    {
                        node.SetPosition(geometry.X, geometry.Y);
                    }

                    if (geometryChanged)
                    {
                        node.Size = new Size(geometry.Width, geometry.Height);
                        UpdatePorts(node);
                    }

                    this.RestoreSelection(node, this.ViewModel.SelectedOccurrences.Contains(occurrence.Id));
                    if (changed || geometryChanged)
                    {
                        node.Refresh();
                    }
                }

                foreach (var route in state.Routes)
                {
                    var link = this.links[route.Id];
                    var changed = link.Locked != !this.ViewModel.CanEditPresentation
                                  || link.Selected != this.ViewModel.SelectedOccurrences.Contains(route.Id);
                    link.Locked = !this.ViewModel.CanEditPresentation;
                    if (!link.Vertices.Select(vertex => new DiagramPoint(vertex.Position.X, vertex.Position.Y))
                            .SequenceEqual(route.Bends))
                    {
                        changed = true;
                        link.Vertices.Clear();
                        foreach (var point in route.Bends)
                        {
                            link.AddVertex(new Point(point.X, point.Y));
                        }
                    }

                    foreach (var vertex in link.Vertices)
                    {
                        vertex.Locked = link.Locked;
                    }

                    this.RestoreSelection(link, this.ViewModel.SelectedOccurrences.Contains(route.Id));
                    if (changed)
                    {
                        link.Refresh();
                    }
                }

                if (this.diagram.Zoom != state.Viewport.Zoom)
                {
                    this.diagram.SetZoom(state.Viewport.Zoom);
                }

                if (this.diagram.Pan.X != state.Viewport.X || this.diagram.Pan.Y != state.Viewport.Y)
                {
                    this.diagram.SetPan(state.Viewport.X, state.Viewport.Y);
                }

                this.presented = this.ViewModel.State;
                this.rendered = state;
                this.renderedProjection = this.ViewModel.Projection;
                this.UpdateViewport();
            }
            finally
            {
                this.applying = false;
            }
        }

        /// <summary>Builds a rendering graph from validated tab state.</summary>
        /// <param name="state">The accepted presentation.</param>
        /// <param name="alignments">The occurrence-specific boundary sides.</param>
        private void CreateRenderer(DiagramPresentationState state,
            Dictionary<(Guid OccurrenceId, string PortId), PortAlignment> alignments)
        {
            var focus = this.pendingFocus;
            this.ClearRenderer();
            this.pendingFocus = focus;
            this.diagram = new BlazorDiagram(new BlazorDiagramOptions(), registerDefaultBehaviors: false);
            var mounted = this.diagram;
            Action<Model, Blazor.Diagrams.Core.Events.PointerEventArgs> started = (_, _) =>
            {
                if (ReferenceEquals(mounted, this.diagram))
                {
                    this.interactionActive = true;
                }
            };
            mounted.PointerDown += started;
            this.rendering.Add(Disposable.Create(() => mounted.PointerDown -= started));
            this.diagram.SuspendRefresh = true;
            this.diagram.SuspendSorting = true;
            this.diagram.Options.Virtualization.Enabled = this.Virtualized;
            this.diagram.Options.Zoom.Minimum = this.MinimumZoom;
            this.diagram.Options.Zoom.Maximum = 4;
            this.diagram.Options.Links.DefaultPathGenerator = new StraightPathGenerator();
            this.diagram.RegisterComponent<DiagramNodeModel, PartSvg>();
            this.diagram.RegisterComponent<DiagramGroupModel, GroupSvg>();
            this.diagram.RegisterComponent<DiagramLinkModel, ConnectionSvg>();
            this.diagram.RegisterBehavior(new SelectionBehavior(this.diagram));
            this.diagram.RegisterBehavior(new DragMovablesBehavior(this.diagram));
            this.diagram.RegisterBehavior(new PanBehavior(this.diagram));
            this.diagram.RegisterBehavior(new ZoomBehavior(this.diagram));
            this.diagram.RegisterBehavior(new EventsBehavior(this.diagram));

            var childrenByParent = state.Occurrences.Where(node => node.ParentId.HasValue)
                .ToLookup(node => node.ParentId.Value);
            foreach (var occurrence in state.Occurrences.Where(node => !childrenByParent.Contains(node.Id)))
            {
                var node = new DiagramNodeModel(occurrence, this.ViewModel.Projection.Parts[occurrence.ElementId])
                { Visible = !this.Virtualized };
                this.nodes.Add(occurrence.Id, node);
                this.diagram.Nodes.Add(node);
            }

            var pendingGroups = state.Occurrences.Where(node => childrenByParent.Contains(node.Id))
                .ToDictionary(node => node.Id);
            while (pendingGroups.Count > 0)
            {
                var ready = pendingGroups.Values
                    .Where(node => childrenByParent[node.Id].All(child => this.nodes.ContainsKey(child.Id))).ToArray();
                if (ready.Length == 0)
                {
                    throw new InvalidOperationException("Cyclic renderer containment.");
                }

                foreach (var occurrence in ready)
                {
                    var children = childrenByParent[occurrence.Id].Select(child => this.nodes[child.Id]);
                    var group = new DiagramGroupModel(occurrence, this.ViewModel.Projection.Parts[occurrence.ElementId],
                        children)
                    { Visible = !this.Virtualized };
                    this.nodes.Add(occurrence.Id, group);
                    this.diagram.Groups.Add(group);
                    pendingGroups.Remove(occurrence.Id);
                }
            }

            foreach (var (occurrenceId, node) in this.nodes)
            {
                var part = GetPart(node);
                foreach (var port in part.Ports)
                {
                    var model = node.AddPort(new PortModel(port.ElementId, node,
                            alignments.GetValueOrDefault((occurrenceId, port.ElementId), PortAlignment.Right),
                            size: new Size(8, 8))
                    { Initialized = true });
                    Action<Model> initializePort = _ =>
                    {
                        model.Initialized = true;
                        model.RefreshLinks();
                    };
                    model.Changed += initializePort;
                    this.rendering.Add(Disposable.Create(() => model.Changed -= initializePort));
                }

                UpdatePorts(node);
            }

            foreach (var route in state.Routes)
            {
                var link = new DiagramLinkModel(route, this.ViewModel.Projection.Connections[route.ElementId],
                    this.nodes[route.SourceId].Ports.Single(port => port.Id == route.SourcePortId),
                    this.nodes[route.TargetId].Ports.Single(port => port.Id == route.TargetPortId));
                link.Visible = !this.Virtualized;
                this.links.Add(route.Id, link);
                this.diagram.Links.Add(link);
            }

            this.diagram.SuspendSorting = false;
            this.diagram.RefreshOrders();
            this.diagram.SuspendRefresh = false;
            this.visibility = new DiagramVisibility(this.diagram, this.ViewportOptions.Value.Margin);
            this.rendering.Add(this.visibility);
            this.rendering.Add(Observable.FromEvent<SelectableModel>(
                    handler => mounted.SelectionChanged += handler, handler => mounted.SelectionChanged -= handler)
                .Where(_ => !this.applying)
                .Select(model => Observable.FromAsync(() => this.InvokeAsync(async () =>
                {
                    if (!this.disposed && ReferenceEquals(mounted, this.diagram))
                    {
                        var selected = this.nodes.Where(pair => pair.Value.Selected).Select(pair => pair.Key)
                            .Concat(this.links.Where(pair => pair.Value.Selected).Select(pair => pair.Key))
                            .ToImmutableHashSet();
                        var primary = this.nodes.FirstOrDefault(pair => ReferenceEquals(pair.Value, model)).Key;
                        if (primary == Guid.Empty)
                        {
                            primary = this.links.FirstOrDefault(pair => ReferenceEquals(pair.Value, model)).Key;
                        }

                        await this.ViewModel.SelectAsync(selected,
                            model.Selected && primary != Guid.Empty ? primary : null);
                    }
                })))
                .Concat().Subscribe(_ => { }, error => this.Logger.LogError(error, "Diagram selection failed.")));

            var changes = new Subject<Unit>();
            this.rendering.Add(changes);
            this.rendering.Add(changes.Sample(TimeSpan.FromMilliseconds(50))
                .Select(_ => Observable.FromAsync(() => this.InvokeAsync(() =>
                {
                    if (!this.disposed && ReferenceEquals(mounted, this.diagram))
                    {
                        this.UpdateViewport();
                        this.StateHasChanged();
                    }
                }))).Concat()
                .Subscribe(_ => { }, error => this.Logger.LogError(error, "Diagram viewport update failed.")));
            Action changed = () => changes.OnNext(Unit.Default);
            Action resized = () =>
            {
                this.UpdateViewport();
                this.StateHasChanged();
            };
            Action<Model, Blazor.Diagrams.Core.Events.PointerEventArgs> completed = (_, _) =>
            {
                if (!ReferenceEquals(mounted, this.diagram))
                {
                    return;
                }

                this.interactionActive = false;
                this.CapturePresentation();
                this.UpdateViewport();
                if (this.ViewModel.UsesRegionalRendering)
                {
                    this.Reconcile();
                }
            };
            mounted.PointerUp += completed;
            mounted.PanChanged += changed;
            mounted.ZoomChanged += changed;
            mounted.ContainerChanged += resized;
            this.rendering.Add(Disposable.Create(() =>
            {
                mounted.PointerUp -= completed;
                mounted.PanChanged -= changed;
                mounted.ZoomChanged -= changed;
                mounted.ContainerChanged -= resized;
            }));
        }

        /// <summary>Gets the immutable presentation associated with one library node.</summary>
        private static DiagramPart GetPart(NodeModel node) => node switch
        {
            DiagramNodeModel part => part.Part,
            DiagramGroupModel group => group.Part,
            _ => throw new InvalidOperationException("Unsupported diagram node.")
        };

        /// <summary>Restores local selection through the library's own selection authority.</summary>
        private void RestoreSelection(SelectableModel model, bool selected)
        {
            if (selected && !model.Selected)
            {
                this.diagram.SelectModel(model, false);
            }
            else if (!selected && model.Selected)
            {
                this.diagram.UnselectModel(model);
            }
        }

        /// <summary>Positions boundary ports from explicit model-space geometry.</summary>
        internal static void UpdatePorts(NodeModel node)
        {
            var sideCounts = new Dictionary<PortAlignment, int>();
            for (var index = 0; index < node.Ports.Count; index++)
            {
                var port = node.Ports[index];
                var sideIndex = sideCounts.GetValueOrDefault(port.Alignment);
                sideCounts[port.Alignment] = sideIndex + 1;
                var local = port.Alignment switch
                {
                    PortAlignment.Left => new Point(-4, 56 + sideIndex * 20),
                    PortAlignment.Right => new Point(node.Size.Width - 4, 56 + sideIndex * 20),
                    PortAlignment.Top => new Point(node.Size.Width / 2 - 4 + sideIndex * 20, -4),
                    _ => new Point(node.Size.Width / 2 - 4 + sideIndex * 20, node.Size.Height - 4)
                };
                port.Position = new Point(node.Position.X + local.X, node.Position.Y + local.Y);
                port.Initialized = true;
                port.RefreshLinks();
            }
        }

        /// <summary>Indexes boundary sides using each occurrence port's first displayed connection.</summary>
        /// <param name="state">The accepted presentation.</param>
        /// <returns>Boundary sides keyed by occurrence and semantic port identity.</returns>
        internal static Dictionary<(Guid OccurrenceId, string PortId), PortAlignment> GetPortAlignments(
            DiagramPresentationState state)
        {
            using var trace = Mycelium.Bloom.Core.DiagramDiagnostics.Source.StartActivity("port-alignments");
            var geometries = state.Occurrences.ToDictionary(node => node.Id, node => node.Geometry);
            var alignments = new Dictionary<(Guid OccurrenceId, string PortId), PortAlignment>();
            foreach (var route in state.Routes)
            {
                var source = geometries[route.SourceId];
                var target = geometries[route.TargetId];
                alignments.TryAdd((route.SourceId, route.SourcePortId), GetAlignment(source, target));
                alignments.TryAdd((route.TargetId, route.TargetPortId), GetAlignment(target, source));
            }

            return alignments;
        }

        /// <summary>Chooses the dominant direction between two displayed bounds.</summary>
        /// <param name="geometry">The port owner's bounds.</param>
        /// <param name="other">The connected occurrence's bounds.</param>
        /// <returns>The corresponding boundary side.</returns>
        private static PortAlignment GetAlignment(DiagramGeometry geometry, DiagramGeometry other)
        {
            var dx = other.X + other.Width / 2 - geometry.X - geometry.Width / 2;
            var dy = other.Y + other.Height / 2 - geometry.Y - geometry.Height / 2;
            return Math.Abs(dx) >= Math.Abs(dy)
                ? dx >= 0 ? PortAlignment.Right : PortAlignment.Left
                : dy >= 0
                    ? PortAlignment.Bottom
                    : PortAlignment.Top;
        }

        /// <summary>Captures completed presentation changes without semantic mutation.</summary>
        private void CapturePresentation()
        {
            using var trace = Mycelium.Bloom.Core.DiagramDiagnostics.Source.StartActivity("capture-presentation");
            if (this.applying || this.disposed || this.diagram is null || this.presented is null)
            {
                return;
            }

            var owner = this.ViewModel.State;
            if (!ReferenceEquals(owner, this.presented))
            {
                return;
            }

            var viewport = new DiagramViewport(this.diagram.Pan.X, this.diagram.Pan.Y, this.diagram.Zoom);
            var candidate = owner with { Viewport = viewport };
            if (this.ViewModel.CanEditPresentation)
            {
                var occurrences = owner.Occurrences.Select(occurrence =>
                {
                    if (!this.nodes.TryGetValue(occurrence.Id, out var node))
                    {
                        return occurrence;
                    }

                    var geometry = new DiagramGeometry(node.Position.X, node.Position.Y, node.Size.Width,
                        node.Size.Height);
                    return geometry == occurrence.Geometry ? occurrence : occurrence with { Geometry = geometry };
                }).ToImmutableArray();
                var routes = owner.Routes.Select(route =>
                {
                    if (!this.links.TryGetValue(route.Id, out var displayedLink))
                    {
                        return route;
                    }

                    var bends = displayedLink.Vertices
                        .Select(vertex => new DiagramPoint(vertex.Position.X, vertex.Position.Y)).ToImmutableArray();
                    return bends.SequenceEqual(route.Bends) ? route : route with { Bends = bends };
                }).ToImmutableArray();
                candidate = candidate with
                {
                    Occurrences = occurrences.SequenceEqual(owner.Occurrences) ? owner.Occurrences : occurrences,
                    Routes = routes.SequenceEqual(owner.Routes) ? owner.Routes : routes
                };
            }

            if (!this.ViewModel.ApplyRenderedPresentation(owner, candidate, this.nodes.Keys.ToHashSet()))
            {
                this.Reconcile();
            }
        }

        /// <summary>Requests a controlled zoom without resetting the current pan.</summary>
        private void ChangeZoom(double zoom) => this.diagram?.SetZoom(zoom / 100);

        /// <summary>Restores the actual-size zoom.</summary>
        private void ResetZoom() => this.ChangeZoom(100);

        /// <summary>Fits complete or selected bounds without bypassing the detailed editor's scale limit.</summary>
        private Task FitToViewAsync()
        {
            if (this.diagram?.Container is not { } container || this.visibility is null)
            {
                return Task.CompletedTask;
            }

            var selected = this.nodes.Values.Where(node => node.Selected).Select(node => node.GetBounds()).ToArray();
            var bounds = selected.Length > 0
                ? selected.Aggregate((left, right) => left.Union(right))
                : this.CompleteBounds;
            var requested = Math.Min(container.Width / (bounds.Width + 80), container.Height / (bounds.Height + 80));
            var zoom = Math.Clamp(requested, this.MinimumZoom, 4);
            if (requested < this.MinimumZoom && selected.Length == 0)
            {
                this.ViewModel.ApplyPresentation(this.ViewModel.State with
                {
                    Viewport = this.ViewModel.State.Viewport with { Zoom = zoom }
                });
                this.ViewModel.ReportNotice(
                    "Minimum detailed zoom reached; the complete diagram does not fit. Select a part to fit its region.");
            }
            else
            {
                this.ViewModel.ApplyPresentation(this.ViewModel.State with
                {
                    Viewport = new DiagramViewport(
                        (container.Width - bounds.Width * zoom) / 2 - bounds.Left * zoom,
                        (container.Height - bounds.Height * zoom) / 2 - bounds.Top * zoom, zoom)
                });
            }

            this.UpdateViewport();
            return Task.CompletedTask;
        }

        private Rectangle CompleteBounds
        {
            get
            {
                if (!this.ViewModel.UsesRegionalRendering)
                {
                    return this.visibility.Bounds;
                }

                if (this.completeBounds is null || this.boundedOccurrences != this.ViewModel.State.Occurrences)
                {
                    this.boundedOccurrences = this.ViewModel.State.Occurrences;
                    this.completeBounds = this.boundedOccurrences.Select(node => new Rectangle(node.Geometry.X,
                            node.Geometry.Y,
                            node.Geometry.X + node.Geometry.Width, node.Geometry.Y + node.Geometry.Height))
                        .DefaultIfEmpty(Rectangle.Zero).Aggregate((left, right) => left.Union(right));
                }

                return this.completeBounds;
            }
        }

        /// <summary>Clamps navigation to complete model-space bounds and applies coalesced rendering visibility.</summary>
        private void UpdateViewport()
        {
            if (this.disposed || this.diagram?.Container is not { } container || this.visibility is null)
            {
                return;
            }

            if (!this.interactionActive && this.pendingReveal is { } request)
            {
                this.pendingReveal = null;
                this.Reveal(request.ElementId, request.Explicit);
                return;
            }

            var bounds = this.CompleteBounds;
            var zoom = Math.Clamp(this.diagram.Zoom, this.MinimumZoom, 4);
            var padding = this.ViewportOptions.Value.PanPadding;
            var x = Math.Clamp(this.diagram.Pan.X, -bounds.Right * zoom - padding,
                container.Width - bounds.Left * zoom + padding);
            var y = Math.Clamp(this.diagram.Pan.Y, -bounds.Bottom * zoom - padding,
                container.Height - bounds.Top * zoom + padding);
            if (this.diagram.Zoom != zoom)
            {
                this.diagram.SetZoom(zoom);
            }

            if (this.diagram.Pan.X != x || this.diagram.Pan.Y != y)
            {
                this.diagram.SetPan(x, y);
            }

            if (!this.interactionActive)
            {
                this.ViewModel.RequestRegion(container.Width, container.Height, this.ViewportOptions.Value.Margin * 2);
            }

            this.visibleCount = this.Virtualized ? this.visibility.Apply(this.interactionActive) : this.nodes.Count;
            var viewport = new DiagramViewport(x, y, zoom);
            if (viewport != this.ViewModel.State.Viewport)
            {
                this.ViewModel.ApplyPresentation(this.ViewModel.State with { Viewport = viewport });
            }
        }

        /// <summary>Reveals a canonical browser selection without replacing diagram-local multi-selection.</summary>
        private void Reveal(string elementId, bool explicitRequest = false)
        {
            if (this.disposed || elementId is null)
            {
                return;
            }

            if (this.interactionActive || this.diagram?.Container is not { } container)
            {
                this.pendingReveal = (elementId, explicitRequest);
                return;
            }

            if (!explicitRequest && this.nodes.Any(pair =>
                    pair.Value.Visible && pair.Value.Selected && GetPart(pair.Value).ElementId == elementId))
            {
                return;
            }

            this.pendingFocus =
                this.ViewModel.RevealOccurrence(elementId, container.Width, container.Height, this.MinimumZoom);
            if (explicitRequest)
            {
                this.ViewModel.CompleteReveal(elementId);
            }

            this.focusAttempts = 0;
            this.ViewModel.RequestRegion(container.Width, container.Height, this.ViewportOptions.Value.Margin * 2);
            this.StateHasChanged();
        }

        /// <summary>Changes the local inspection setting.</summary>
        private void SetReadOnly(bool value) => this.ViewModel.IsReadOnly = value;

        /// <summary>Reveals the current canonical selection even when its identity has not changed.</summary>
        private void RevealSelection() => this.Reveal(this.Context.SelectedElement?.ElementId, true);

        /// <summary>Adds the browser's shared identity without changing browser focus.</summary>
        private void AddBrowserSelection() => this.ViewModel.AddSharedSelection();

        /// <summary>Downloads the complete native SVG scene with resolved styles.</summary>
        private async Task ExportSvgAsync()
        {
            if (this.module is not null && !this.disposed)
            {
                this.CapturePresentation();
                try
                {
                    var state = this.ViewModel.State;
                    var projection = await this.ViewModel.LoadExportProjectionAsync(this.exportLifetime.Token);
                    if (this.disposed)
                    {
                        return;
                    }

                    using var renderer = new HtmlRenderer(this.Services, this.LoggerFactory);
                    var parameters = !this.ViewModel.UsesRegionalRendering
                        ? new Dictionary<string, object>
                        {
                            [nameof(DiagramSvgExport.Nodes)] = this.nodes.Values.ToArray(),
                            [nameof(DiagramSvgExport.Links)] = this.links.Values.Cast<BaseLinkModel>().ToArray(),
                            [nameof(DiagramSvgExport.Bounds)] = this.visibility.Bounds
                        }
                        : new Dictionary<string, object>
                        {
                            [nameof(DiagramSvgExport.State)] = state,
                            [nameof(DiagramSvgExport.Projection)] = projection
                        };
                    var content = await renderer.Dispatcher.InvokeAsync(async () =>
                    {
                        var output =
                            await renderer.RenderComponentAsync<DiagramSvgExport>(
                                ParameterView.FromDictionary(parameters));
                        return output.ToHtmlString();
                    });
                    if (!this.disposed)
                    {
                        await this.module.InvokeVoidAsync("exportCompleteSvg", this.root, content);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException
                                                  or InvalidOperationException)
                {
                    if (!this.disposed)
                    {
                        this.ViewModel.ReportNotice(
                            "Export could not retrieve complete model details; no file was created.");
                    }
                }
            }
        }

        /// <summary>Downloads prototype presentation state independently of model persistence.</summary>
        private async Task ExportPresentationAsync()
        {
            if (this.module is not null && !this.disposed)
            {
                this.CapturePresentation();
                await this.module.InvokeVoidAsync("download", "interconnection.presentation.json", "application/json",
                    DiagramPresentationSerializer.Export(this.ViewModel.State));
            }
        }

        /// <summary>Reads a bounded user-selected snapshot and restores it only to its original tab owner.</summary>
        private async Task ImportPresentationAsync(InputFileChangeEventArgs args)
        {
            var owner = this.ViewModel;
            try
            {
                await using var stream = args.File.OpenReadStream(this.ViewModel.Limits.JsonBytes);
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();
                if (!this.disposed && ReferenceEquals(owner, this.observed))
                {
                    var imported = owner.Import(json, out var error);
                    owner.ReportNotice(imported ? "Prototype presentation restored." : error);
                }
            }
            catch (IOException)
            {
                if (!this.disposed)
                {
                    owner.ReportNotice("Could not read presentation; choose a JSON file smaller than 1 MiB.");
                }
            }
        }

        /// <summary>Detaches every library event, behavior and grouped child owned by this mount.</summary>
        private void ClearRenderer()
        {
            this.rendering.Dispose();
            this.rendering = new CompositeDisposable();
            if (this.diagram is not null)
            {
                foreach (var group in this.diagram.Groups.ToArray())
                {
                    group.Ungroup();
                }

                this.diagram.UnregisterBehavior<SelectionBehavior>();
                this.diagram.UnregisterBehavior<DragMovablesBehavior>();
                this.diagram.UnregisterBehavior<PanBehavior>();
                this.diagram.UnregisterBehavior<ZoomBehavior>();
                this.diagram.UnregisterBehavior<EventsBehavior>();
            }

            this.diagram = null;
            this.visibility = null;
            this.pendingFocus = null;
            this.interactionActive = false;
            this.presented = null;
            this.rendered = null;
            this.renderedProjection = null;
            this.nodes.Clear();
            this.links.Clear();
        }

        /// <summary>Releases mounted resources while leaving the caller-owned ViewModel alive.</summary>
        public async ValueTask DisposeAsync()
        {
            if (this.disposed)
            {
                return;
            }

            this.CapturePresentation();
            this.disposed = true;
            this.exportLifetime.Cancel();
            this.exportLifetime.Dispose();
            this.observation.Dispose();
            this.ClearRenderer();
            try
            {
                try
                {
                    if (this.initialization is not null)
                    {
                        await this.initialization;
                    }
                }
                finally
                {
                    if (this.module is not null)
                    {
                        try
                        {
                            await this.module.InvokeVoidAsync("unmount", this.root);
                        }
                        finally
                        {
                            await this.module.DisposeAsync();
                        }
                    }
                }
            }
            catch (JSDisconnectedException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }
}