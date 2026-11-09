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
    public partial class ModelOpening
    {
        private ModelOpeningViewModel viewModel;
        private IDisposable subscription;
        private IBrowserFile file;
        private bool exampleSelected;
        private bool open;
        private bool disposed;
        private string existingId;
        private ElementReference trigger;
        private bool returnFocus;
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

        [Inject] private WorkspaceModelSession Session { get; set; }
        [Inject] private IJSRuntime JsRuntime { get; set; }
        [Inject] private BlazorBlueprint.Primitives.Services.IFocusManager FocusManager { get; set; }

        private SelectInputOption[] ExistingOptions => this.ExistingModels.Select(model => new SelectInputOption
        {
            Value = model.Id.ToString(),
            Label = model.Root.DeclaredName ?? "Unnamed model"
        }).ToArray();

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        private void Show() => this.open = true;

        private void SelectFile(InputFileChangeEventArgs args)
        {
            this.file = args.File;
            this.exampleSelected = false;
            this.existingId = null;
        }

        private void SelectExample()
        {
            this.file = null;
            this.exampleSelected = true;
            this.existingId = null;
        }

        private void SelectExisting(string value)
        {
            this.existingId = value;
            this.file = null;
            this.exampleSelected = false;
        }

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

        private void Cancel() => this.SetOpen(false);

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
            }
            catch (OperationCanceledException)
            {
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