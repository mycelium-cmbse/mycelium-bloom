// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramCanvas.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;
    using Microsoft.JSInterop;

    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Components.UI.Organisms.Diagram.Tools;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.ViewModel.Diagram;

    /// <summary>Captures UI events and borrows caller-owned presentation through one disposable native adapter.</summary>
    public sealed partial class DiagramCanvas : BloomReactiveComponentBase<IDiagramViewModel>
    {
        /// <summary>The adapter owned by this component's current interactive mount.</summary>
        private DiagramRenderer renderer;

        /// <summary>The mounted native canvas with browser-only pointer resources.</summary>
        private DiagramCanvasHost host;

        /// <summary>Gets the presentation styles supplied by layout and the mounted tool.</summary>
        private string CanvasStyle => $"--diagram-boundary-label-maximum-width:{DiagramLayoutMetrics.BoundaryLabelMaximumWidth}px;--diagram-tool-cursor:{this.renderer?.Pointer.Cursor}";

        /// <summary>The exact ViewModel borrowed by the adapter.</summary>
        private IDiagramViewModel observed;

        /// <summary>Records availability of the native browser API before creating its canvas.</summary>
        private bool scriptReady;

        /// <summary>Guards component-local work after unmount.</summary>
        private bool disposed;

        /// <summary>Gets or sets whether the view is hosted inside another graphical occurrence.</summary>
        [Parameter]
        public bool IsNested { get; set; }

        /// <summary>Gets or sets the runtime that loads diagram support for this interactive view.</summary>
        [Inject]
        private IJSRuntime ScriptRuntime { get; set; }

        /// <summary>Gets or sets the registered factories for renderer-owned tool runtimes.</summary>
        [Inject]
        private DiagramToolRuntimeProvider ToolRuntimes { get; set; }

        /// <summary>Loads native script support before parameter binding can create a native canvas.</summary>
        /// <returns>Completion of interactive script initialization.</returns>
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            if (this.RendererInfo.IsInteractive)
            {
                try
                {
                    await using var module = await this.ScriptRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/UI/Organisms/Diagram/DiagramCanvas.razor.js");
                    await module.InvokeVoidAsync("ensureDiagramAssets");
                    this.scriptReady = true;
                }
                catch (JSDisconnectedException)
                {
                    // A disconnected circuit cannot initialize or release browser references.
                }
            }
        }

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
                this.ReleaseRenderer();
            }
        }

        /// <summary>Creates one adapter for the exact current caller-owned ViewModel.</summary>
        private void BindRenderer()
        {
            if (this.scriptReady && !this.disposed && !ReferenceEquals(this.observed, this.ViewModel))
            {
                this.ReleaseRenderer();
                this.observed = this.ViewModel;
                this.renderer = new DiagramRenderer(this.ViewModel, this.ToolRuntimes);
                this.renderer.Pointer.Changed += this.ToolPresentationChanged;
            }
        }

        /// <summary>Detaches this component before disposing its mounted runtimes.</summary>
        private void ReleaseRenderer()
        {
            if (this.renderer is not null)
            {
                this.renderer.Pointer.Changed -= this.ToolPresentationChanged;
                this.renderer.Dispose();
            }
        }

        /// <summary>Renders the current tool cursor, preview and temporary navigation attributes.</summary>
        private void ToolPresentationChanged() => _ = this.InvokeAsync(this.StateHasChanged);

        /// <summary>Routes focused canvas key presses and releases browser capture on explicit cancellation.</summary>
        /// <param name="args">The keyboard input originating in this canvas.</param>
        /// <returns>Completion of any needed browser capture cleanup.</returns>
        private async Task KeyDown(KeyboardEventArgs args)
        {
            this.RouteKey(args, true);

            if (args.Key == "Escape" && this.host is not null)
            {
                await this.host.ReleasePointerCaptureAsync();
            }
        }

        /// <summary>Routes focused canvas key releases.</summary>
        /// <param name="args">The keyboard input originating in this canvas.</param>
        private void KeyUp(KeyboardEventArgs args) => this.RouteKey(args, false);

        /// <summary>Passes keyboard input to the mounted runtime dispatcher.</summary>
        /// <param name="args">The browser key and modifiers.</param>
        /// <param name="pressed">Whether this is a key press.</param>
        private void RouteKey(KeyboardEventArgs args, bool pressed)
        {
            if (!args.Repeat || args.Code != "Space")
            {
                this.renderer?.Pointer.KeyChanged(args.Key, args.Code, pressed, args.AltKey || args.CtrlKey || args.MetaKey || args.ShiftKey);
            }
        }

        /// <summary>Cancels the interrupted gesture without affecting a different pointer.</summary>
        /// <param name="args">The pointer whose capture was lost or cancelled.</param>
        /// <returns>Completion of browser capture cleanup for the interrupted pointer.</returns>
        private async Task PointerCancelled(PointerEventArgs args)
        {
            if (this.renderer?.Pointer.CancelPointer(args.PointerId) == true && this.host is not null)
            {
                await this.host.ReleasePointerCaptureAsync();
            }
        }

        /// <summary>Cancels temporary navigation when focus leaves the mounted canvas.</summary>
        /// <returns>Completion of the browser focus check and cleanup.</returns>
        private async Task FocusOut()
        {
            var pointer = this.renderer?.Pointer;
            var canvas = this.host;

            if (pointer?.IsInteracting == true && canvas is not null && !await canvas.HasFocusAsync()
                && !this.disposed && ReferenceEquals(pointer, this.renderer?.Pointer))
            {
                pointer.Cancel();
                await canvas.ReleasePointerCaptureAsync();
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
