// ------------------------------------------------------------------------------------------------
// <copyright file="GeneralDiagram.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.ViewModel.Diagram;

    /// <summary>Captures UI events and borrows caller-owned presentation through one disposable native adapter.</summary>
    public sealed partial class GeneralDiagram : BloomReactiveComponentBase<IGeneralDiagramViewModel>
    {
        /// <summary>The adapter owned by this component's current interactive mount.</summary>
        private GeneralDiagramRenderer renderer;

        /// <summary>The exact ViewModel borrowed by the adapter.</summary>
        private IGeneralDiagramViewModel observed;

        /// <summary>Guards component-local work after unmount.</summary>
        private bool disposed;

        /// <summary>Gets or sets whether the view is hosted inside another graphical occurrence.</summary>
        [Parameter]
        public bool IsNested { get; set; }

        /// <summary>Rebinds the adapter when the caller changes the presentation owner.</summary>
        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            ArgumentNullException.ThrowIfNull(this.ViewModel);
            this.BindRenderer();
        }

        /// <summary>Releases the adapter and base observation without disposing caller-owned state.</summary>
        /// <param name="disposing">Whether managed resources are being released.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                this.disposed = true;
                this.renderer?.Dispose();
            }
        }

        /// <summary>Creates one adapter for the exact current caller-owned ViewModel.</summary>
        private void BindRenderer()
        {
            if (this.RendererInfo.IsInteractive && !this.disposed && !ReferenceEquals(this.observed, this.ViewModel))
            {
                this.renderer?.Dispose();
                this.observed = this.ViewModel;
                this.renderer = new GeneralDiagramRenderer(this.ViewModel);
            }
        }

        /// <summary>Delegates the control's zoom request to the presentation owner.</summary>
        /// <param name="percent">The requested zoom percentage.</param>
        private void ChangeZoom(double percent)
        {
            this.ViewModel.ChangeZoom(percent);
        }

        /// <summary>Delegates the reset action to the presentation owner.</summary>
        private void ResetZoom()
        {
            this.ViewModel.ResetZoom();
        }

        /// <summary>Captures the native canvas dimensions and delegates the fit operation.</summary>
        private void FitToView()
        {
            if (this.renderer?.Diagram.Container is { } container)
            {
                this.ViewModel.FitToView(container.Width, container.Height);
            }
        }
    }
}
