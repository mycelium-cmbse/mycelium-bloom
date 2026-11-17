// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramCanvasHost.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    using NativeDiagramCanvas = Blazor.Diagrams.Components.DiagramCanvas;

    /// <summary>Orders library canvas initialization before its observer and interop-reference cleanup.</summary>
    /// <remarks>
    /// This adapter must inherit the library canvas to preserve its cascading graph, JS observer and asynchronous lifetime.
    /// It cannot also inherit BloomReactiveComponentBase; the surrounding DiagramCanvas owns reactive tab binding instead.
    /// </remarks>
    public sealed class DiagramCanvasHost : NativeDiagramCanvas, IAsyncDisposable
    {
        /// <summary>The library's in-flight first-render initialization.</summary>
        private Task initialization = Task.CompletedTask;

        /// <summary>The shared asynchronous cleanup operation.</summary>
        private Task disposal;

        /// <summary>Prevents another render from starting after cleanup begins.</summary>
        private bool disposed;

        /// <summary>The browser helper owned by this mounted native canvas.</summary>
        private IJSObjectReference interactionModule;

        /// <summary>The browser listener lifetime owned by this mount.</summary>
        private IJSObjectReference interactionHandle;

        /// <summary>The interop reference used only while this canvas is mounted.</summary>
        private DotNetObjectReference<DiagramCanvasHost> interactionReference;

        /// <summary>Gets or sets the current mount's native interaction routing.</summary>
        [Parameter]
        public DiagramPointerBehavior Interaction { get; set; }

        /// <summary>Tracks the library's initial bounds query and observer registration.</summary>
        /// <param name="firstRender">Whether this is the initial interactive render.</param>
        /// <returns>Completion of library canvas initialization.</returns>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (this.disposed)
            {
                return;
            }

            if (firstRender)
            {
                this.initialization = this.InitializeAsync();
                await this.initialization;
            }
            else
            {
                await base.OnAfterRenderAsync(false);
            }
        }

        /// <summary>Releases the library resources after any pending registration completes.</summary>
        /// <returns>The idempotent cleanup operation.</returns>
        public new ValueTask DisposeAsync() => new(this.disposal ??= this.DisposeCoreAsync());

        /// <summary>Changes temporary navigation without changing the retained tool.</summary>
        /// <param name="active">Whether Space is currently held within this canvas.</param>
        [JSInvokable]
        public void SetTemporaryPan(bool active)
        {
            if (!this.disposed && this.Interaction is not null)
            {
                this.Interaction.TemporaryPan = active;
            }
        }

        /// <summary>Cancels captured pointer work after blur, escape or browser cancellation.</summary>
        [JSInvokable]
        public void CancelInteraction()
        {
            if (!this.disposed && this.Interaction is not null)
            {
                this.Interaction.TemporaryPan = false;
                this.Interaction.Cancel();
            }
        }

        /// <summary>Initializes native geometry before attaching browser pointer-lifetime support.</summary>
        /// <returns>Completion of all mount-owned browser registration.</returns>
        private async Task InitializeAsync()
        {
            await base.OnAfterRenderAsync(true);

            if (this.disposed || this.Interaction is null)
            {
                return;
            }

            try
            {
                this.interactionModule = await this.JSRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/UI/Organisms/Diagram/DiagramCanvas.razor.js");
                this.interactionReference = DotNetObjectReference.Create(this);
                this.interactionHandle = await this.interactionModule.InvokeAsync<IJSObjectReference>("attachDiagramInteraction", this.elementReference, this.interactionReference);
            }
            catch (JSDisconnectedException)
            {
                // Browser references are unavailable after the circuit disconnects.
            }
        }

        /// <summary>Awaits initial registration before disposing the library reference.</summary>
        /// <returns>Completion of registration and owned library cleanup.</returns>
        private async Task DisposeCoreAsync()
        {
            this.disposed = true;

            try
            {
                await this.initialization;
            }
            finally
            {
                try
                {
                    if (this.interactionHandle is not null)
                    {
                        await this.interactionHandle.InvokeVoidAsync("dispose");
                        await this.interactionHandle.DisposeAsync();
                    }

                    if (this.interactionModule is not null)
                    {
                        await this.interactionModule.DisposeAsync();
                    }
                }
                catch (JSDisconnectedException)
                {
                    // A disconnected circuit has already released browser listeners.
                }
                finally
                {
                    this.interactionReference?.Dispose();
                    await base.DisposeAsync();
                }
            }
        }
    }
}
