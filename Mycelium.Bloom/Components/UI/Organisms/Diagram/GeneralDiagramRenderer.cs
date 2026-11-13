// ------------------------------------------------------------------------------------------------
// <copyright file="GeneralDiagramRenderer.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram
{
    using System.ComponentModel;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    using Blazor.Diagrams;
    using Blazor.Diagrams.Core.Behaviors;
    using Blazor.Diagrams.Core.Geometry;
    using Blazor.Diagrams.Options;

    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.ViewModel.Diagram;

    using ReactiveUI;

    /// <summary>Adapts VM-owned presentation to disposable native models and reports native gestures back to the VM.</summary>
    public sealed class GeneralDiagramRenderer : IDisposable
    {
        /// <summary>The tab-owned presentation borrowed by this mount.</summary>
        private readonly IGeneralDiagramViewModel owner;

        /// <summary>Owns observation of the tab's distinct zoom and pan state for this mount.</summary>
        private readonly CompositeDisposable viewportSubscriptions = new();

        /// <summary>Prevents replay while transferring viewport state in either direction.</summary>
        private bool synchronizingViewport;

        /// <summary>Marks completion of this mount's cleanup.</summary>
        private bool disposed;

        /// <summary>Gets the native graph owned solely by this adapter.</summary>
        public BlazorDiagram Diagram { get; }

        /// <summary>Mounts a validated projection without enabling model-editing behaviors.</summary>
        /// <param name="owner">The caller-owned ViewModel, which outlives ordinary renderer unmounts.</param>
        public GeneralDiagramRenderer(IGeneralDiagramViewModel owner)
        {
            ArgumentNullException.ThrowIfNull(owner);
            this.owner = owner;
            this.Diagram = new BlazorDiagram(new BlazorDiagramOptions(), registerDefaultBehaviors: false);
            this.Diagram.RegisterBehavior(new PanBehavior(this.Diagram));
            this.Diagram.RegisterBehavior(new ZoomBehavior(this.Diagram));
            this.Diagram.RegisterBehavior(new EventsBehavior(this.Diagram));
            this.Diagram.Options.AllowMultiSelection = false;
            this.Diagram.Options.Zoom.Minimum = DiagramViewport.MinimumZoom;
            this.Diagram.Options.Zoom.Maximum = DiagramViewport.MaximumZoom;
            this.Diagram.RegisterComponent<GeneralDiagramNodeModel, DiagramNodeWidget>();
            this.Diagram.RegisterComponent<GeneralDiagramLinkModel, DiagramEdge>();
            this.CreateModels();
            this.viewportSubscriptions.Add(this.owner.WhenAnyValue(view => view.Viewport)
                .Select(viewport => viewport.Zoom)
                .DistinctUntilChanged()
                .Subscribe(zoom => this.ApplyViewport(() => this.Diagram.SetZoom(zoom))));
            this.viewportSubscriptions.Add(this.owner.WhenAnyValue(view => view.Viewport)
                .Select(viewport => new Point(viewport.PanX, viewport.PanY))
                .DistinctUntilChanged()
                .Subscribe(pan => this.ApplyViewport(() => this.Diagram.SetPan(pan.X, pan.Y))));
            this.owner.PropertyChanged += this.OwnerChanged;
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
            this.viewportSubscriptions.Dispose();
            this.owner.PropertyChanged -= this.OwnerChanged;
            this.Diagram.ContainerChanged -= this.InitializeViewport;
            this.Diagram.PanChanged -= this.ReportViewport;
            this.Diagram.ZoomChanged -= this.ReportViewport;
            this.Diagram.Links.Clear();
            this.Diagram.Nodes.Clear();
            this.Diagram.UnregisterBehavior<PanBehavior>();
            this.Diagram.UnregisterBehavior<ZoomBehavior>();
            this.Diagram.UnregisterBehavior<EventsBehavior>();
        }

        /// <summary>Creates renderer models from occurrence identities already validated by the ViewModel.</summary>
        private void CreateModels()
        {
            var nodes = new Dictionary<Guid, GeneralDiagramNodeModel>();

            foreach (var node in this.owner.Projection.Nodes)
            {
                var model = new GeneralDiagramNodeModel(node, this.owner);
                nodes.Add(node.Occurrence.Id, model);
                this.Diagram.Nodes.Add(model);
            }

            foreach (var edge in this.owner.Projection.Edges)
            {
                this.Diagram.Links.Add(new GeneralDiagramLinkModel(edge, nodes[edge.Source], nodes[edge.Target], this.owner));
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

        /// <summary>Applies changes raised by the tab-owned ViewModel.</summary>
        /// <param name="sender">The presentation owner that raised the change.</param>
        /// <param name="args">The property whose presentation changed.</param>
        private void OwnerChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(IGeneralDiagramViewModel.Projection))
            {
                this.Diagram.Batch(() =>
                {
                    this.Diagram.Links.Clear();
                    this.Diagram.Nodes.Clear();
                    this.CreateModels();
                });
                this.InitializeViewport();
            }
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
