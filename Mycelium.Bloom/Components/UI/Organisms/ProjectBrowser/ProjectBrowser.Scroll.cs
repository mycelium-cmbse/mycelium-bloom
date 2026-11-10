// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowser.Scroll.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using Mycelium.Bloom.Model;

    public sealed partial class ProjectBrowser : IAsyncDisposable
    {
        /// <summary>Cancels pending scroll reads when this Browser mount is disposed.</summary>
        private readonly CancellationTokenSource scrollReadCancellation = new();

        /// <summary>Identifies this Browser instance's scrollable DOM viewport.</summary>
        private ElementReference treeViewport;
        /// <summary>Owns scroll interop for this component mount.</summary>
        private IJSObjectReference scrollModule;

        /// <summary>Gets or sets browser interop for this mounted Browser.</summary>
        [Inject]
        private IJSRuntime JsRuntime { get; set; }

        /// <summary>Gets the caller-owned native scroll snapshot for this tab.</summary>
        [Parameter]
        public BrowserScrollPosition ScrollPosition { get; set; }

        /// <summary>Publishes native scrolling to the owning tab without changing model selection.</summary>
        [Parameter]
        public EventCallback<BrowserScrollPosition> ScrollPositionChanged { get; set; }

        /// <summary>Restores only this tab's scroll snapshot after the Browser mounts.</summary>
        /// <param name="firstRender">Whether this is the first interactive render.</param>
        /// <returns>Completion of module initialization and scroll restoration.</returns>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (!firstRender || !this.ScrollPositionChanged.HasDelegate || this.isDisposed)
            {
                return;
            }

            try
            {
                var imported = await this.JsRuntime.InvokeAsync<IJSObjectReference>("import",
                    "./Components/UI/Organisms/ProjectBrowser/ProjectBrowser.razor.js");
                if (this.isDisposed)
                {
                    if (imported is not null)
                    {
                        await imported.DisposeAsync();
                    }

                    return;
                }

                this.scrollModule = imported;
                if (imported is not null && this.ScrollPosition is not null)
                {
                    await imported.InvokeVoidAsync("restoreScroll", this.treeViewport, this.ScrollPosition);
                }
            }
            catch (JSDisconnectedException)
            {
                // Closed or superseded owners cannot receive this completion.
            }
            catch (OperationCanceledException) when (this.isDisposed)
            {
                // Closed or superseded owners cannot receive this completion.
            }
        }

        /// <summary>Publishes a native scroll snapshot only while its owning Browser remains mounted.</summary>
        /// <returns>Completion of the current scroll read.</returns>
        private async Task CaptureScrollAsync()
        {
            if (this.isDisposed || this.scrollModule is null || !this.ScrollPositionChanged.HasDelegate)
            {
                return;
            }

            var cancellationToken = this.scrollReadCancellation.Token;
            try
            {
                var position =
                    await this.scrollModule.InvokeAsync<BrowserScrollPosition>("readScroll", cancellationToken,
                        this.treeViewport);
                cancellationToken.ThrowIfCancellationRequested();
                if (position is not null)
                {
                    await this.ScrollPositionChanged.InvokeAsync(position);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Closed owners cannot receive this completion.
            }
            catch (JSException) when (cancellationToken.IsCancellationRequested)
            {
                // Closed or superseded owners cannot receive this completion.
            }
            catch (JSDisconnectedException)
            {
                // Closed or superseded owners cannot receive this completion.
            }
        }

        /// <summary>Releases rendering resources without disposing the caller-owned Browser.</summary>
        /// <returns>Completion of owned JavaScript module disposal.</returns>
        public async ValueTask DisposeAsync()
        {
            this.Dispose();
            var module = this.scrollModule;
            this.scrollModule = null;
            if (module is null)
            {
                return;
            }

            try
            {
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Closed or superseded owners cannot receive this completion.
            }
            catch (OperationCanceledException)
            {
                // Closed or superseded owners cannot receive this completion.
            }
        }
    }
}
