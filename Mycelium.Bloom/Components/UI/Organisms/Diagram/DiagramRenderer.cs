// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramRenderer.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram
{
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    using Blazor.Diagrams;
    using Blazor.Diagrams.Core.Behaviors;
    using Blazor.Diagrams.Core.Geometry;
    using Blazor.Diagrams.Core.Models;
    using Blazor.Diagrams.Options;

    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.ViewModel.Diagram;

    using ReactiveUI;

    /// <summary>Adapts VM-owned presentation to disposable native models and reports native gestures back to the VM.</summary>
    public sealed class DiagramRenderer : IDiagramRenderer
    {
        /// <summary>The tab-owned presentation borrowed by this mount.</summary>
        private readonly IDiagramViewModel owner;

        /// <summary>Owns observation of the tab's viewport and projection for this mount.</summary>
        private readonly CompositeDisposable tabSubscriptions = new();

        /// <summary>Owns geometry observation for the current projection.</summary>
        private readonly CompositeDisposable nodeSubscriptions = new();

        /// <summary>Prevents replay while transferring viewport state in either direction.</summary>
        private bool synchronizingViewport;

        /// <summary>Marks completion of this mount's cleanup.</summary>
        private bool disposed;

        /// <summary>Gets the native graph owned solely by this adapter.</summary>
        public BlazorDiagram Diagram { get; }

        /// <summary>Gets mode routing and temporary gesture state owned by this mount.</summary>
        public IDiagramPointerBehavior Pointer { get; }

        /// <summary>Mounts a validated projection without enabling model-editing behaviors.</summary>
        /// <param name="owner">The caller-owned ViewModel, which outlives ordinary renderer unmounts.</param>
        /// <param name="createPointer">The registered factory returning the native behavior subclass required by Blazor.Diagrams registration.</param>
        public DiagramRenderer(IDiagramViewModel owner, Func<BlazorDiagram, IDiagramViewModel, DiagramPointerBehavior> createPointer)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(createPointer);
            this.owner = owner;
            this.Diagram = new BlazorDiagram(new BlazorDiagramOptions(), registerDefaultBehaviors: false);
            var pointer = createPointer(this.Diagram, owner);
            this.Pointer = pointer;
            this.Diagram.RegisterBehavior(pointer);
            this.Diagram.RegisterBehavior(new DragMovablesBehavior(this.Diagram));
            this.Diagram.RegisterBehavior(new ZoomBehavior(this.Diagram));
            this.Diagram.Options.AllowMultiSelection = false;
            this.Diagram.Options.Zoom.Minimum = DiagramViewport.MinimumZoom;
            this.Diagram.Options.Zoom.Maximum = DiagramViewport.MaximumZoom;
            this.Diagram.RegisterComponent<DiagramNodeModel, DiagramNodeWidget>();
            this.Diagram.RegisterComponent<DiagramLinkModel, DiagramEdge>();
            this.CreateModels();
            this.tabSubscriptions.Add(this.owner.WhenAnyValue(view => view.Viewport)
                .Select(viewport => viewport.Zoom)
                .DistinctUntilChanged()
                .Subscribe(zoom => this.ApplyViewport(() => this.Diagram.SetZoom(zoom))));
            this.tabSubscriptions.Add(this.owner.WhenAnyValue(view => view.Viewport)
                .Select(viewport => new Point(viewport.PanX, viewport.PanY))
                .DistinctUntilChanged()
                .Subscribe(pan => this.ApplyViewport(() => this.Diagram.SetPan(pan.X, pan.Y))));
            this.tabSubscriptions.Add(this.owner.WhenAnyValue(view => view.Projection).Skip(1).Subscribe(_ => this.RefreshProjection()));
            this.Diagram.ContainerChanged += this.InitializeViewport;
            this.Diagram.PanChanged += this.ReportViewport;
            this.Diagram.ZoomChanged += this.ReportViewport;
        }

        /// <summary>Detaches every installed handler and behavior, leaving tab state and canonical objects alive.</summary>
        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.tabSubscriptions.Dispose();
            this.nodeSubscriptions.Dispose();
            this.Diagram.ContainerChanged -= this.InitializeViewport;
            this.Diagram.PanChanged -= this.ReportViewport;
            this.Diagram.ZoomChanged -= this.ReportViewport;
            this.Diagram.UnregisterBehavior<DiagramPointerBehavior>();
            this.Diagram.Links.Clear();
            this.Diagram.Nodes.Clear();
            this.Diagram.UnregisterBehavior<DragMovablesBehavior>();
            this.Diagram.UnregisterBehavior<ZoomBehavior>();
        }

        /// <summary>Creates renderer models from occurrence identities already validated by the ViewModel.</summary>
        private void CreateModels()
        {
            var nodes = new Dictionary<Guid, DiagramNodeModel>();

            foreach (var node in this.owner.Projection.Nodes)
            {
                var model = new DiagramNodeModel(node, this.owner);
                nodes.Add(node.Occurrence.Id, model);
                this.Diagram.Nodes.Add(model);
                model.Moving += this.ReportPosition;
                this.nodeSubscriptions.Add(Disposable.Create(() => model.Moving -= this.ReportPosition));
                this.nodeSubscriptions.Add(model.ViewModel.WhenAnyValue(value => value.Position).Subscribe(position => ApplyPosition(model, position)));
                this.nodeSubscriptions.Add(model.ViewModel.WhenAnyValue(value => value.Size).Subscribe(model.ApplySize));
            }

            foreach (var edge in this.owner.Projection.Edges)
            {
                this.Diagram.Links.Add(new DiagramLinkModel(edge, nodes[edge.Source], nodes[edge.Target], this.owner));
            }
        }

        /// <summary>Reports native movement to the tab owner or restores prohibited geometry.</summary>
        /// <param name="moving">The native occurrence whose geometry changed.</param>
        private void ReportPosition(NodeModel moving)
        {
            var node = (DiagramNodeModel)moving;

            if (this.owner.MoveNode(node.Presentation.Occurrence.Id, node.Position.X, node.Position.Y))
            {
                return;
            }

            ApplyPosition(node, node.ViewModel.Position);
        }

        /// <summary>Applies retained geometry without replaying equal native movement notifications.</summary>
        /// <param name="node">The mounted graphical occurrence.</param>
        /// <param name="position">The tab-owned model-space position.</param>
        private static void ApplyPosition(DiagramNodeModel node, DiagramPoint position)
        {
            if (!(node.Position.X, node.Position.Y).Equals((position.X, position.Y)))
            {
                node.SetPosition(position.X, position.Y);
            }
        }

        /// <summary>Delegates initial fitting once the native canvas has supplied its dimensions.</summary>
        private void InitializeViewport()
        {
            if (!this.owner.Viewport.IsInitialized && this.Diagram.Container is { } container)
            {
                this.owner.FitToView(container.Width, container.Height);
            }
        }

        /// <summary>Replaces native models when the tab publishes a new projection.</summary>
        private void RefreshProjection()
        {
            this.Pointer.Cancel();
            this.nodeSubscriptions.Clear();
            this.Diagram.Batch(() =>
            {
                this.Diagram.Links.Clear();
                this.Diagram.Nodes.Clear();
                this.CreateModels();
            });
            this.InitializeViewport();
        }

        /// <summary>Applies a distinct VM-owned viewport value without replaying native gesture notifications.</summary>
        /// <param name="apply">The native setter for the changed zoom or pan value.</param>
        private void ApplyViewport(Action apply)
        {
            if (this.synchronizingViewport)
            {
                return;
            }

            this.synchronizingViewport = true;

            try
            {
                apply();
            }
            finally
            {
                this.synchronizingViewport = false;
            }
        }

        /// <summary>Captures a native navigation gesture without retaining a second presentation owner.</summary>
        private void ReportViewport()
        {
            if (this.synchronizingViewport)
            {
                return;
            }

            this.synchronizingViewport = true;

            try
            {
                this.owner.SetViewport(this.Diagram.Pan.X, this.Diagram.Pan.Y, this.Diagram.Zoom);
            }
            finally
            {
                this.synchronizingViewport = false;
            }
        }
    }
}
