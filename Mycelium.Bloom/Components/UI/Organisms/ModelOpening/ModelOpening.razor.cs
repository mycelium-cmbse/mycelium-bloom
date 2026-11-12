// ------------------------------------------------------------------------------------------------
// <copyright file="ModelOpening.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.ModelOpening
{
    using System.Reactive.Linq;
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Forms;
    using Microsoft.JSInterop;
    using Mycelium.Bloom.ViewModel;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Core.Context;
    using ReactiveUI;

    /// <summary>Provides the workspace's explicit session-only browser file picker.</summary>
    public sealed partial class ModelOpening
    {
        /// <summary>Owns the current dialog operation and its unpublished accepted lease.</summary>
        private ModelOpeningViewModel viewModel;
        /// <summary>Observes upload status through the renderer dispatcher.</summary>
        private IDisposable subscription;
        /// <summary>Retains only the explicitly selected browser file metadata.</summary>
        private IBrowserFile file;
        /// <summary>Tracks explicit example selection within this dialog.</summary>
        private bool exampleSelected;
        /// <summary>Tracks this dialog instance visibility.</summary>
        private bool open;
        /// <summary>Rejects late dialog completions.</summary>
        private bool disposed;
        /// <summary>Tracks the selected existing canonical context.</summary>
        private string existingId;
        /// <summary>Identifies the invoking DOM action for focus restoration.</summary>
        private ElementReference trigger;
        /// <summary>Requests focus restoration after cancellation.</summary>
        private bool returnFocus;
        /// <summary>Identifies the browser-owned file chooser.</summary>
        private InputFile fileInput;

        /// <summary>Gets the diagram-local action label.</summary>
        [Parameter]
        public string Label { get; set; } = "Open model";

        /// <summary>Gets compatible contexts retained by existing tabs.</summary>
        [Parameter]
        public IReadOnlyList<WorkspaceModelSnapshot> ExistingModels { get; set; } = [];

        /// <summary>Publishes the operation-owned lease to the invoking tab composition.</summary>
        [Parameter]
        public EventCallback<WorkspaceModelLease> Opened { get; set; }

        /// <summary>Gets the circuit-owned canonical model registry.</summary>
        [Inject] private WorkspaceModelSession Session { get; set; }
        /// <summary>Gets browser streaming interop.</summary>
        [Inject] private IJSRuntime JsRuntime { get; set; }
        /// <summary>Gets the existing Blueprint focus authority.</summary>
        [Inject] private BlazorBlueprint.Primitives.Services.IFocusManager FocusManager { get; set; }

        /// <summary>Creates display options from caller-supplied open model contexts.</summary>
        /// <returns>The compatible context choices.</returns>
        private SelectInputOption[] GetExistingOptions() => this.ExistingModels.Select(model => new SelectInputOption
        {
            Value = model.Id.ToString(),
            Label = model.Root.DeclaredName ?? "Unnamed model"
        }).ToArray();

        /// <summary>Restores focus to the invoking action after dialog cancellation.</summary>
        /// <param name="firstRender">Whether this is the initial render.</param>
        /// <returns>Completion of DOM focus restoration.</returns>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (this.returnFocus && !this.disposed)
            {
                this.returnFocus = false;
                await this.FocusManager.FocusFirst(this.trigger);
            }
        }

        /// <summary>Gets the factory for component-owned opening state.</summary>
        [Inject]
        private Func<ModelOpeningViewModel> Factory { get; set; }

        /// <summary>Gets the logger for renderer dispatch failures.</summary>
        [Inject]
        private ILogger<ModelOpening> Logger { get; set; }

        /// <summary>Owns the upload view model and renderer-dispatched status observation.</summary>
        protected override void OnInitialized()
        {
            this.viewModel = this.Factory();
            this.subscription = this.viewModel.WhenAnyValue(model => model.State).Skip(1)
                .Select(_ => Observable.FromAsync(() => this.InvokeAsync(() =>
                {
                    if (!this.disposed)
                    {
                        this.StateHasChanged();
                    }
                })))
                .Concat().Subscribe(_ => { },
                    error => this.Logger.LogWarning(error, "Model opening display update failed"));
        }

        /// <summary>Opens this diagram-local dialog.</summary>
        private void Show() => this.open = true;

        /// <summary>Selects a browser file without beginning an upload.</summary>
        /// <param name="args">The browser file selection.</param>
        private void SelectFile(InputFileChangeEventArgs args)
        {
            this.file = args.File;
            this.exampleSelected = false;
            this.existingId = null;
        }

        /// <summary>Selects the bundled example only after explicit user input.</summary>
        private void SelectExample()
        {
            this.file = null;
            this.exampleSelected = true;
            this.existingId = null;
        }

        /// <summary>Selects an existing canonical context without duplicating its SDK graph.</summary>
        /// <param name="value">The selected session context identity.</param>
        private void SelectExisting(string value)
        {
            this.existingId = value;
            this.file = null;
            this.exampleSelected = false;
        }

        /// <summary>Changes dialog visibility and cancels work when it closes.</summary>
        /// <param name="value">Whether the dialog remains open.</param>
        private void SetOpen(bool value)
        {
            this.open = value;
            if (!value)
            {
                this.viewModel.Cancel();
                this.file = null;
                this.exampleSelected = false;
                this.existingId = null;
                this.returnFocus = true;
            }
        }

        /// <summary>Cancels this opening attempt and restores its invoking action.</summary>
        private void Cancel() => this.SetOpen(false);

        /// <summary>Transfers a successfully accepted lease to the invoking diagram.</summary>
        /// <returns>Completion of attachment or recoverable cancellation.</returns>
        private async Task OpenFileAsync()
        {
            WorkspaceModelLease lease = null;
            try
            {
                if (Guid.TryParse(this.existingId, out var id))
                {
                    var model = this.ExistingModels.FirstOrDefault(value => value.Id == id);
                    if (model is null)
                    {
                        return;
                    }

                    lease = this.Session.Acquire(model);
                }
                else
                {
                    var success = this.exampleSelected
                        ? await this.viewModel.OpenExampleAsync(true)
                        : this.file is { } selected && await this.UploadAsync(selected);
                    if (!success || this.disposed)
                    {
                        return;
                    }

                    lease = this.viewModel.TakeOpenedContext();
                }

                if (lease is null || this.disposed)
                {
                    return;
                }

                this.SetOpen(false);
                this.returnFocus = false;
                await this.Opened.InvokeAsync(lease);
            }
            finally
            {
                lease?.Dispose();
            }
        }

        /// <summary>Owns browser stream references until bounded session opening completes.</summary>
        /// <param name="selected">The explicitly selected browser file.</param>
        /// <returns>Whether the file was accepted.</returns>
        private async Task<bool> UploadAsync(IBrowserFile selected)
        {
            IJSObjectReference module = null;
            IJSStreamReference reference = null;
            try
            {
                return await this.viewModel.OpenStreamAsync(selected.Name, selected.Size, async token =>
                {
                    module = await this.JsRuntime.InvokeAsync<IJSObjectReference>("import", token,
                        "./Components/UI/Organisms/ModelOpening/ModelOpening.razor.js");
                    reference = await module.InvokeAsync<IJSStreamReference>("openFile", token, this.fileInput.Element);
                    return await reference.OpenReadStreamAsync(ModelOpeningViewModel.MaximumBytes, token);
                }, true);
            }
            finally
            {
                await DisposeReferenceAsync(reference);
                await DisposeReferenceAsync(module);
            }
        }

        /// <summary>Releases a browser reference when its circuit remains reachable.</summary>
        /// <param name="reference">The operation-owned reference, when acquired.</param>
        /// <returns>Completion of reference cleanup.</returns>
        private static async ValueTask DisposeReferenceAsync(IAsyncDisposable reference)
        {
            if (reference is null)
            {
                return;
            }

            try
            {
                await reference.DisposeAsync();
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

        /// <summary>Releases owned UI subscriptions and cancels an unfinished upload.</summary>
        public void Dispose()
        {
            this.disposed = true;
            this.subscription?.Dispose();
            this.viewModel?.Dispose();
        }
    }
}