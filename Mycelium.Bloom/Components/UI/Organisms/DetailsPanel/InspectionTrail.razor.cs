// ------------------------------------------------------------------------------------------------
// <copyright file="InspectionTrail.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.DetailsPanel
{
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Model;

    /// <summary>Renders the URI-derived reference inspection trail, independently of model ownership.</summary>
    public sealed partial class InspectionTrail : ComponentBase, IDisposable, IAsyncDisposable
    {
        /// <summary>Owns the subscription for the currently cascaded workspace service.</summary>
        private readonly SerialDisposable subscription = new();

        /// <summary>The service observed during this component lifetime.</summary>
        private IWorkspaceUrlContextService observedContext;

        /// <summary>The latest resolved presentation, never an independent navigation history.</summary>
        private IReadOnlyList<InspectionTrailStep> steps = Array.Empty<InspectionTrailStep>();

        /// <summary>Prevents renderer work after disposal.</summary>
        private bool isDisposed;

        /// <summary>The last requested whole-pixel panel minimum, independent of navigation state.</summary>
        private int requiredWidth = 380;

        /// <summary>The currently rendered trail root.</summary>
        private ElementReference trailRoot;

        /// <summary>The DOM root registered with the current observer.</summary>
        private ElementReference observedRoot;

        /// <summary>The registered root identity used to avoid duplicate observer registration.</summary>
        private string observedRootId;

        /// <summary>The owned presentation-only browser module.</summary>
        private IJSObjectReference module;

        /// <summary>The single in-flight module initialization.</summary>
        private Task initialization;

        /// <summary>The callback reference owned by this component.</summary>
        private DotNetObjectReference<InspectionTrail> callbackReference;

        /// <summary>Gets or sets the scoped resize-observer interop boundary.</summary>
        [Inject]
        private IJSRuntime JsRuntime { get; set; }

        /// <summary>Gets or sets the logger for unexpected restoration-rendering failures.</summary>
        [Inject]
        private ILogger<InspectionTrail> Logger { get; set; }

        /// <summary>Gets or sets the layout's measured minimum-width callback.</summary>
        [Parameter]
        public EventCallback<double> MinimumWidthChanged { get; set; }

        /// <summary>Gets or sets the existing layout-owned navigation authority.</summary>
        [CascadingParameter]
        public IWorkspaceUrlContextService UrlContext { get; set; }

        /// <summary>Gets or sets whether Properties is displaying a distinct reference.</summary>
        [Parameter]
        public bool IsInspecting { get; set; }

        /// <summary>Gets or sets the effective identifier, preventing display of stale trail endpoints.</summary>
        [Parameter]
        public string DisplayedElementId { get; set; }

        /// <summary>Gets a value indicating whether the current trail agrees with Properties.</summary>
        private bool IsVisible => this.IsInspecting && this.steps.Count >= 2
            && this.steps[^1].ElementId == this.DisplayedElementId;

        /// <summary>Registers a header-width observer only while a trail is rendered.</summary>
        /// <param name="firstRender">Whether this is the component's first render.</param>
        /// <returns>The component-local observer registration or release.</returns>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (this.isDisposed)
            {
                return;
            }

            if (!this.IsVisible)
            {
                if (this.module is not null && this.observedRootId is not null)
                {
                    await this.module.InvokeVoidAsync("stopObserving", this.observedRoot);
                    this.observedRootId = null;
                }

                await this.SetRequiredWidth(380);
                return;
            }

            this.initialization ??= this.InitializeModuleAsync();
            await this.initialization;
            if (!this.isDisposed && this.module is not null && this.observedRootId != this.trailRoot.Id)
            {
                if (this.observedRootId is not null)
                {
                    await this.module.InvokeVoidAsync("stopObserving", this.observedRoot);
                }

                this.callbackReference ??= DotNetObjectReference.Create(this);
                this.observedRoot = this.trailRoot;
                this.observedRootId = this.trailRoot.Id;
            }

            if (!this.isDisposed && this.module is not null)
            {
                await this.module.InvokeVoidAsync("observeTrail", this.trailRoot, this.callbackReference);
            }
        }

        /// <summary>Loads presentation-only interop without retaining a module after disposal.</summary>
        /// <returns>The module import and any cleanup required by concurrent disposal.</returns>
        private async Task InitializeModuleAsync()
        {
            var imported = await this.JsRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/UI/Organisms/DetailsPanel/InspectionTrail.razor.js");
            if (this.isDisposed && imported is not null)
            {
                await imported.DisposeAsync();
            }
            else
            {
                this.module = imported;
            }
        }

        /// <summary>Requests enough panel width for both visible names within existing layout limits.</summary>
        /// <param name="width">The measured full header width in CSS pixels.</param>
        /// <returns>The layout callback when the bounded minimum changes.</returns>
        [JSInvokable]
        public Task SetRequiredWidth(double width) => this.InvokeAsync(() =>
        {
            if (this.isDisposed || !double.IsFinite(width))
            {
                return Task.CompletedTask;
            }

            var boundedWidth = (int)Math.Clamp(Math.Ceiling(width), 380, 600);
            if (this.requiredWidth == boundedWidth)
            {
                return Task.CompletedTask;
            }

            this.requiredWidth = boundedWidth;
            return this.MinimumWidthChanged.InvokeAsync(boundedWidth);
        });

        /// <summary>Releases renderer subscriptions, callbacks and the owned browser observer.</summary>
        /// <returns>The asynchronous observer and module cleanup.</returns>
        public async ValueTask DisposeAsync()
        {
            this.Dispose();
            try
            {
                if (this.initialization is not null)
                {
                    await this.initialization;
                }

                if (this.module is not null)
                {
                    var owned = this.module;
                    this.module = null;
                    try
                    {
                        if (this.observedRootId is not null)
                        {
                            await owned.InvokeVoidAsync("stopObserving", this.observedRoot);
                        }
                    }
                    finally
                    {
                        await owned.DisposeAsync();
                    }
                }
            }
            catch (Exception exception) when (exception is JSDisconnectedException or ObjectDisposedException)
            {
                // The browser/circuit was released before observer cleanup completed.
            }
        }

        /// <summary>Observes only the current workspace's existing replayed restorations.</summary>
        protected override void OnParametersSet()
        {
            if (ReferenceEquals(this.observedContext, this.UrlContext))
            {
                return;
            }

            this.observedContext = this.UrlContext;
            this.steps = Array.Empty<InspectionTrailStep>();
            var context = this.UrlContext;
            this.subscription.Disposable = context?.Restorations?
                .Select(restoration => Observable.FromAsync(() => this.InvokeAsync(() =>
                {
                    if (!this.isDisposed && ReferenceEquals(context, this.UrlContext))
                    {
                        this.steps = restoration.InspectionTrail;
                        this.StateHasChanged();
                    }
                })))
                .Concat()
                .Subscribe(_ => { }, exception => this.Logger.LogError(exception, "Inspection trail rendering stopped unexpectedly."));
        }

        /// <summary>Requests a new URI entry through the current authority; indices alone cannot select stale targets.</summary>
        /// <param name="index">The rendered earlier step's index.</param>
        /// <param name="elementId">The stable identifier guarding against stale menu actions.</param>
        /// <returns>The navigation operation, or a completed task after disposal.</returns>
        private Task Navigate(int index, string elementId) => this.isDisposed || this.UrlContext is null
            ? Task.CompletedTask : this.UrlContext.InspectTrailStepAsync(index, elementId);

        /// <summary>Stops restoration rendering and releases the component-owned callback reference.</summary>
        public void Dispose()
        {
            this.isDisposed = true;
            this.subscription.Dispose();
            this.callbackReference?.Dispose();
        }
    }
}
