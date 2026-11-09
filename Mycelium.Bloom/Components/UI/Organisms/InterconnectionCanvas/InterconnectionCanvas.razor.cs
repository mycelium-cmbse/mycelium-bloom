// ------------------------------------------------------------------------------------------------
// <copyright file="InterconnectionCanvas.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.InterconnectionCanvas
{
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Forms;
    using Microsoft.JSInterop;
    using Microsoft.Extensions.Options;

    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.ViewModel.Diagram;

    using ReactiveUI;

    /// <summary>Owns the browser renderer and its mount-scoped interop validation boundary.</summary>
    public sealed partial class InterconnectionCanvas : ComponentBase, IAsyncDisposable
    {
        /// <summary>Owns tab-state observation while this component is mounted.</summary>
        private readonly SerialDisposable observation = new();

        /// <summary>The caller-owned state currently observed.</summary>
        private DiagramViewModel observed;

        /// <summary>The component-owned validation session.</summary>
        private DiagramCanvasSession session;

        /// <summary>The browser-owned canvas subtree.</summary>
        private ElementReference canvas;

        /// <summary>The component's collocated browser module.</summary>
        private IJSObjectReference module;

        /// <summary>The callback reference retained only while mounted.</summary>
        private DotNetObjectReference<InterconnectionCanvas> reference;

        /// <summary>The initialization task joined by final disposal.</summary>
        private Task initialization;

        /// <summary>Whether browser mounting completed.</summary>
        private bool mounted;

        /// <summary>Whether final disposal has begun.</summary>
        private bool disposed;

        /// <summary>Gets or sets the exact caller-owned tab state.</summary>
        [Parameter, EditorRequired]
        public DiagramViewModel ViewModel { get; set; }

        /// <summary>Gets or sets whether the composition selected an isolated benchmark scene.</summary>
        [Parameter]
        public bool IsBenchmark { get; set; }

        /// <summary>Gets or sets whether the explicitly enabled developer facility exposes diagnostic controls.</summary>
        [Parameter]
        public bool ShowDiagnostics { get; set; }

        /// <summary>Gets or sets browser interop.</summary>
        [Inject]
        private IJSRuntime JsRuntime { get; set; }

        /// <summary>Gets or sets isolated rendering experiment configuration.</summary>
        [Inject]
        private IOptions<DiagramBenchmarkOptions> BenchmarkOptions { get; set; }

        /// <summary>Gets or sets logging for asynchronous rendering failures.</summary>
        [Inject]
        private ILogger<InterconnectionCanvas> Logger { get; set; }

        /// <summary>Binds a mount to its exact caller-owned tab.</summary>
        protected override void OnParametersSet()
        {
            if (ReferenceEquals(this.observed, this.ViewModel))
            {
                return;
            }

            if (this.observed is not null)
            {
                throw new InvalidOperationException("A mounted canvas cannot change tab ownership.");
            }

            this.observed = this.ViewModel;
            this.session = new DiagramCanvasSession(this.ViewModel);
            this.observation.Disposable = this.ViewModel.WhenAnyValue(value => value.State,
                    value => value.CanEditPresentation, value => value.SelectedOccurrences, value => value.Notice)
                .Select(_ => Observable.FromAsync(() => this.InvokeAsync(this.PublishAsync)))
                .Concat()
                .Subscribe(_ => { }, error => this.Logger.LogError(error, "Canvas observation failed."));
        }

        /// <summary>Starts browser resources after interactive rendering begins.</summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && !this.disposed)
            {
                this.initialization = this.InitializeAsync();
                await this.initialization;
            }
        }

        /// <summary>Imports and mounts the browser adapter without outliving disposal.</summary>
        private async Task InitializeAsync()
        {
            var imported = await this.JsRuntime.InvokeAsync<IJSObjectReference>("import",
                "./Components/UI/Organisms/InterconnectionCanvas/InterconnectionCanvas.razor.js");
            if (this.disposed)
            {
                await imported.DisposeAsync();
                return;
            }

            this.module = imported;
            this.reference = DotNetObjectReference.Create(this);
            await imported.InvokeVoidAsync("mount", this.canvas, this.reference, this.session.Capture(), this.ViewModel.Limits.MinimumZoom,
                this.IsBenchmark && this.BenchmarkOptions.Value.Enabled && this.BenchmarkOptions.Value.LargeSceneEnabled
                && this.BenchmarkOptions.Value.AsyncRendering,
                this.IsBenchmark && this.BenchmarkOptions.Value.Enabled && this.BenchmarkOptions.Value.LargeSceneEnabled
                && this.BenchmarkOptions.Value.SimpleRouting);
            if (!this.disposed)
            {
                this.mounted = true;
                await this.PublishAsync();
            }
        }

        /// <summary>Publishes authoritative state only through the renderer dispatcher.</summary>
        private async Task PublishAsync()
        {
            if (!this.disposed)
            {
                if (this.mounted)
                {
                    await this.module.InvokeVoidAsync("update", this.canvas, this.session.Capture());
                }

                if (!this.disposed)
                {
                    this.StateHasChanged();
                }
            }
        }

        /// <summary>Validates a completed browser gesture at the authoritative boundary.</summary>
        /// <param name="change">The bounded proposal.</param>
        /// <returns>The decision and current recovery state.</returns>
        [JSInvokable]
        public Task<DiagramCanvasResult> PresentationChanged(DiagramCanvasChange change) =>
            this.InvokeAsyncResult(() => this.session.Apply(change));

        /// <summary>Resolves local occurrence selection through the shared canonical selection service.</summary>
        /// <param name="sessionId">The mount identity.</param>
        /// <param name="ids">The complete local selection.</param>
        /// <param name="primary">The primary occurrence.</param>
        /// <returns>A task completing canonical selection publication.</returns>
        [JSInvokable]
        public Task SelectionChanged(Guid sessionId, Guid[] ids, Guid? primary) =>
            this.disposed ? Task.CompletedTask : this.InvokeAsync(() => this.session.SelectAsync(sessionId, ids, primary));

        /// <summary>Returns current authority after interrupted browser communication.</summary>
        /// <returns>The current snapshot or null after disposal.</returns>
        [JSInvokable]
        public Task<DiagramCanvasSnapshot> GetSnapshot() => this.InvokeAsyncResult(this.session.Capture);

        /// <summary>Crosses the renderer dispatcher while retaining a typed callback result.</summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="operation">The session operation.</param>
        /// <returns>The operation result.</returns>
        private async Task<T> InvokeAsyncResult<T>(Func<T> operation)
        {
            T result = default;
            if (!this.disposed)
            {
                await this.InvokeAsync(() => result = this.disposed ? default : operation());
            }

            return result;
        }

        /// <summary>Requests a local browser zoom gesture.</summary>
        /// <param name="zoom">The requested percentage.</param>
        /// <returns>The browser operation.</returns>
        private Task ChangeZoom(double zoom) => this.BrowserActionAsync("zoom", zoom / 100);

        /// <summary>Restores actual-size zoom.</summary>
        /// <returns>The browser operation.</returns>
        private Task ResetZoom() => this.ChangeZoom(100);

        /// <summary>Fits the complete scene independently of viewport visibility.</summary>
        /// <returns>The browser operation.</returns>
        private Task FitToViewAsync() => this.BrowserActionAsync("fit");

        /// <summary>Exports all native SVG content.</summary>
        /// <returns>The browser operation.</returns>
        private Task ExportSvgAsync() => this.BrowserActionAsync("export");

        /// <summary>Invokes an operation only on an active mount.</summary>
        /// <param name="action">The browser operation name.</param>
        /// <param name="value">The optional scalar argument.</param>
        /// <returns>The browser operation.</returns>
        private async Task BrowserActionAsync(string action, double value = 0)
        {
            if (this.mounted && !this.disposed)
            {
                await this.module.InvokeVoidAsync("action", this.canvas, action, value);
            }
        }

        /// <summary>Sets local inspection policy.</summary>
        /// <param name="value">Whether presentation changes are forbidden.</param>
        private void SetReadOnly(bool value) => this.ViewModel.IsReadOnly = value;

        /// <summary>Adds the shared canonical selection as an occurrence.</summary>
        private void AddBrowserSelection() => this.ViewModel.AddSharedSelection();

        /// <summary>Exports accepted prototype presentation state.</summary>
        /// <returns>The browser download operation.</returns>
        private async Task ExportPresentationAsync()
        {
            if (this.mounted && !this.disposed)
            {
                if (!await this.module.InvokeAsync<bool>("settle", this.canvas) || this.disposed)
                {
                    return;
                }

                await this.module.InvokeVoidAsync("download", "interconnection.presentation.json", "application/json",
                    DiagramPresentationSerializer.Export(this.ViewModel.State));
            }
        }

        /// <summary>Imports bounded presentation state into the original caller-owned tab.</summary>
        /// <param name="args">The selected file.</param>
        /// <returns>The bounded file read.</returns>
        private async Task ImportPresentationAsync(InputFileChangeEventArgs args)
        {
            try
            {
                await using var stream = args.File.OpenReadStream(this.ViewModel.Limits.JsonBytes);
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();
                if (!this.disposed)
                {
                    var imported = this.ViewModel.Import(json, out var error);
                    this.ViewModel.ReportNotice(imported ? "Prototype presentation restored." : error);
                }
            }
            catch (IOException)
            {
                if (!this.disposed)
                {
                    this.ViewModel.ReportNotice("Could not read presentation; choose a JSON file smaller than 1 MiB.");
                }
            }
        }

        /// <summary>Releases mount resources while preserving the caller-owned tab.</summary>
        public async ValueTask DisposeAsync()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.observation.Dispose();
            this.session?.Dispose();
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
                            await this.module.InvokeVoidAsync("unmount", this.canvas);
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
            finally
            {
                this.reference?.Dispose();
            }
        }
    }
}
