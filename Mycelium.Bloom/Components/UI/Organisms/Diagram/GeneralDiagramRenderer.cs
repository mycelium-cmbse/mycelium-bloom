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

    using Blazor.Diagrams;
    using Blazor.Diagrams.Core.Behaviors;
    using Blazor.Diagrams.Options;

    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.ViewModel.Diagram;

    /// <summary>Adapts VM-owned presentation to disposable native models and reports native gestures back to the VM.</summary>
    public sealed class GeneralDiagramRenderer : IDisposable
    {
        /// <summary>The tab-owned presentation borrowed by this mount.</summary>
        private readonly IGeneralDiagramViewModel owner;

        /// <summary>Prevents native event feedback while applying a coherent VM snapshot.</summary>
        private bool applyingViewport;

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
            this.ApplyViewport();
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
            if (args.PropertyName == nameof(IGeneralDiagramViewModel.Viewport))
            {
                this.ApplyViewport();
            }
            else if (args.PropertyName == nameof(IGeneralDiagramViewModel.Projection))
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

        /// <summary>Restores native drawing state from the authoritative VM snapshot without event feedback.</summary>
        private void ApplyViewport()
        {
            this.applyingViewport = true;

            try
            {
                var viewport = this.owner.Viewport;

                // These are exact retained values, not results of independent geometric calculations.
                // Equality prevents redundant native notifications; an epsilon would discard requested state.

                if (!this.Diagram.Zoom.Equals(viewport.Zoom))
                {
                    this.Diagram.SetZoom(viewport.Zoom);
                }

                if (!this.Diagram.Pan.X.Equals(viewport.PanX) || !this.Diagram.Pan.Y.Equals(viewport.PanY))
                {
                    this.Diagram.SetPan(viewport.PanX, viewport.PanY);
                }
            }
            finally
            {
                this.applyingViewport = false;
            }
        }

        /// <summary>Captures a native navigation gesture without retaining a second presentation owner.</summary>
        private void ReportViewport()
        {
            if (!this.applyingViewport)
            {
                this.owner.SetViewport(this.Diagram.Pan.X, this.Diagram.Pan.Y, this.Diagram.Zoom);
            }
        }
    }
}
