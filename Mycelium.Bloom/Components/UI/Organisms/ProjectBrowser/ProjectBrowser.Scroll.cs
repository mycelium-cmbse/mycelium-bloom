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
        private ElementReference treeViewport;
        private IJSObjectReference scrollModule;

        [Inject] private IJSRuntime JsRuntime { get; set; }

        /// <summary>Gets the caller-owned native scroll snapshot for this tab.</summary>
        [Parameter]
        public BrowserScrollPosition ScrollPosition { get; set; }

        /// <summary>Publishes native scrolling to the owning tab without changing model selection.</summary>
        [Parameter]
        public EventCallback<BrowserScrollPosition> ScrollPositionChanged { get; set; }

        /// <inheritdoc />
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
            }
            catch (OperationCanceledException) when (this.isDisposed)
            {
            }
        }

        private async Task CaptureScrollAsync()
        {
            if (this.isDisposed || this.scrollModule is null || !this.ScrollPositionChanged.HasDelegate)
            {
                return;
            }

            try
            {
                var position =
                    await this.scrollModule.InvokeAsync<BrowserScrollPosition>("readScroll", this.treeViewport);
                if (!this.isDisposed && position is not null)
                {
                    await this.ScrollPositionChanged.InvokeAsync(position);
                }
            }
            catch (JSException) when (this.isDisposed)
            {
            }
            catch (JSDisconnectedException)
            {
            }
        }

        /// <summary>Releases rendering resources without disposing the caller-owned Browser.</summary>
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
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}