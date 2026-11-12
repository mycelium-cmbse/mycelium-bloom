// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceModelSession.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Context
{
    using DynamicData;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using ReactiveUI;
    using SysML2.NET.Core.POCO.Root.Namespaces;

    /// <summary>Owns the active workspace root independently of individual editor tabs.</summary>
    public sealed class WorkspaceModelSession : ReactiveObject, IDisposable
    {
        /// <summary>Serializes canonical-context publication, lease changes and disposal.</summary>
        private readonly object gate = new();
        /// <summary>Serializes SDK loading operations without holding the publication lock across awaits.</summary>
        private readonly SemaphoreSlim loaderGate = new(1, 1);
        /// <summary>Owns SDK deserialization and isolated candidate assembly.</summary>
        private readonly IModelLoaderService loader;
        /// <summary>Publishes canonical selection for the current consumer.</summary>
        private readonly IElementSelectionService selection;
        /// <summary>Cancels model operations when the circuit session closes.</summary>
        private readonly CancellationTokenSource lifetime = new();
        /// <summary>Reuses the default model initialization while it is in flight.</summary>
        private Task<INamespace> pending;
        /// <summary>Rejects late operations after the owning session closes.</summary>
        private bool disposed;
        /// <summary>Retains canonical contexts only while a consumer or active operation owns a lease.</summary>
        private readonly DynamicData.SourceCache<RetainedContext, Guid> contexts = new(value => value.Snapshot.Id);

        /// <summary>Counts live consumers of one canonical model context.</summary>
        /// <param name="snapshot">The accepted context shared by those consumers.</param>
        private sealed class RetainedContext(WorkspaceModelSnapshot snapshot)
        {
            /// <summary>Gets the immutable identity and canonical root.</summary>
            public WorkspaceModelSnapshot Snapshot { get; } = snapshot;
            /// <summary>Gets or sets the lease count under the session lock.</summary>
            public int Consumers { get; set; }
        }

        /// <summary>Gets contexts retained by open tabs or active opening operations.</summary>
        public IReadOnlyList<WorkspaceModelSnapshot> GetOpenModels()
        {
            lock (this.gate)
            {
                return this.contexts.Items.Select(value => value.Snapshot).ToArray();
            }
        }

        /// <summary>Retains an existing canonical context for another tab.</summary>
        /// <param name="snapshot">The registered context or default workspace scope.</param>
        /// <returns>An independently disposable consumer reference.</returns>
        public WorkspaceModelLease Acquire(WorkspaceModelSnapshot snapshot)
        {
            lock (this.gate)
            {
                ObjectDisposedException.ThrowIf(this.disposed, this);
                var entry = this.contexts.Lookup(snapshot.Id);
                if (!entry.HasValue)
                {
                    if (!ReferenceEquals(snapshot, this.Current) || snapshot.Root is null)
                    {
                        throw new InvalidOperationException("The model context is no longer open.");
                    }

                    this.contexts.AddOrUpdate(new RetainedContext(snapshot));
                }

                return this.Retain(snapshot);
            }
        }

        /// <summary>Accepts an isolated uploaded model without replacing the workspace or other tabs.</summary>
        /// <param name="stream">The complete bounded upload.</param>
        /// <param name="cancellationToken">Cancels this invoking tab's opening operation.</param>
        /// <returns>The accepted context retained until its caller releases ownership.</returns>
        public async Task<WorkspaceModelLease> OpenContextAsync(Stream stream, CancellationToken cancellationToken)
        {
            using var cancellation =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.lifetime.Token);
            await this.loaderGate.WaitAsync(cancellation.Token);
            WorkspaceModelLease lease = null;
            try
            {
                await Task.Run(() => this.loader.OpenIndependentModelAsync(stream, publish =>
                {
                    lock (this.gate)
                    {
                        cancellation.Token.ThrowIfCancellationRequested();
                        ObjectDisposedException.ThrowIf(this.disposed, this);
                        var root = publish();
                        var context =
                            this.contexts.Items.FirstOrDefault(value => ReferenceEquals(value.Snapshot.Root, root));
                        if (context is null)
                        {
                            context = new RetainedContext(new(Guid.NewGuid(), root, WorkspaceModelStatus.Ready));
                            this.contexts.AddOrUpdate(context);
                        }

                        lease = this.Retain(context.Snapshot);
                        return Task.FromResult(root);
                    }
                }, cancellation.Token), cancellation.Token);
                return lease;
            }
            catch
            {
                lease?.Dispose();
                throw;
            }
            finally
            {
                this.loaderGate.Release();
            }
        }

        /// <summary>Creates a consumer lease while the session publication lock is held.</summary>
        /// <param name="snapshot">The registered canonical context.</param>
        /// <returns>The independently disposable consumer lease.</returns>
        private WorkspaceModelLease Retain(WorkspaceModelSnapshot snapshot)
        {
            this.contexts.Lookup(snapshot.Id).Value.Consumers++;
            return new(snapshot, () => this.Release(snapshot));
        }

        /// <summary>Releases the final consumer without clearing another context or its selection.</summary>
        /// <param name="snapshot">The canonical context whose lease ended.</param>
        private void Release(WorkspaceModelSnapshot snapshot)
        {
            lock (this.gate)
            {
                if (this.disposed)
                {
                    return;
                }

                var entry = this.contexts.Lookup(snapshot.Id);
                if (!entry.HasValue || --entry.Value.Consumers != 0)
                {
                    return;
                }

                this.contexts.RemoveKey(snapshot.Id);
                if (!ReferenceEquals(snapshot.Root, this.Current.Root))
                {
                    if (this.selection.SelectedElement is { } selected
                        && ReferenceEquals(this.loader.FindUploadedElement(snapshot.Root, selected.ElementId),
                            selected))
                    {
                        this.selection.SelectedElement = null;
                    }

                    this.loader.ReleaseUploadedModel(snapshot.Root);
                }
            }
        }

        /// <summary>Gets whether the circuit owner has begun disposal.</summary>
        public bool IsDisposed
        {
            get
            {
                lock (this.gate)
                {
                    return this.disposed;
                }
            }
        }

        /// <summary>Creates the circuit-owned context over the existing canonical file loader.</summary>
        /// <param name="loader">The existing canonical model loader.</param>
        /// <param name="selection">The circuit's shared model selection.</param>
        public WorkspaceModelSession(IModelLoaderService loader, IElementSelectionService selection)
        {
            this.loader = loader;
            this.selection = selection;
        }

        /// <summary>Gets the coherent active model state and local load identity.</summary>
        public WorkspaceModelSnapshot Current
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        } = new(Guid.Empty, null, WorkspaceModelStatus.Empty);

        /// <summary>Reuses the active model or starts the application's existing default file load once.</summary>
        public Task<INamespace> EnsureLoadedAsync()
        {
            lock (this.gate)
            {
                ObjectDisposedException.ThrowIf(this.disposed, this);
                if (this.Current.Status == WorkspaceModelStatus.Loading)
                {
                    return this.pending;
                }

                if (this.Current.Id != Guid.Empty)
                {
                    return Task.FromResult(this.Current.Root);
                }

                return this.StartLoad(this.loader.LoadQuantitiesModel);
            }
        }

        /// <summary>Opens a supported local file while rejecting completion from superseded loads.</summary>
        /// <param name="uri">The file URI accepted by the existing model loader.</param>
        public Task<INamespace> OpenAsync(Uri uri)
        {
            ArgumentNullException.ThrowIfNull(uri);
            lock (this.gate)
            {
                ObjectDisposedException.ThrowIf(this.disposed, this);
                return this.StartLoad(() => this.loader.LoadModel(uri));
            }
        }

        /// <summary>Opens a session-only upload while retaining the current model on cancellation or failure.</summary>
        /// <param name="stream">The validated upload stream, or null to reopen Quantities.</param>
        /// <param name="cancellationToken">Cancels the requested model switch.</param>
        /// <param name="expectedContext">The model context in which the user requested opening.</param>
        /// <returns>Whether the requested model became current.</returns>
        public async Task<bool> OpenUploadedAsync(Stream stream,
            Guid expectedContext, CancellationToken cancellationToken)
        {
            Guid previous;
            lock (this.gate)
            {
                ObjectDisposedException.ThrowIf(this.disposed, this);
                if (this.Current.Id != expectedContext)
                {
                    return false;
                }

                previous = this.Current.Id;
            }

            using var cancellation =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.lifetime.Token);
            await this.loaderGate.WaitAsync(cancellation.Token);
            try
            {
                Task<INamespace> Accept(Func<INamespace> publish)
                {
                    lock (this.gate)
                    {
                        cancellation.Token.ThrowIfCancellationRequested();
                        if (this.disposed || this.Current.Id != previous)
                        {
                            throw new OperationCanceledException("The workspace changed before opening completed.");
                        }

                        var root = publish();
                        if (!ReferenceEquals(root, this.Current.Root))
                        {
                            this.Activate(root);
                        }

                        return Task.FromResult(root);
                    }
                }

                await Task.Run(async () => stream is null
                    ? await Accept(this.loader.LoadQuantitiesModel)
                    : await this.loader.OpenUploadedModelAsync(stream, Accept, cancellation.Token), cancellation.Token);
                return true;
            }
            finally
            {
                this.loaderGate.Release();
            }
        }

        /// <summary>Adopts an already-loaded canonical model without copying or assembling it.</summary>
        /// <param name="root">The canonical root supplied by the workspace model owner.</param>
        public void Activate(INamespace root)
        {
            ArgumentNullException.ThrowIfNull(root);
            lock (this.gate)
            {
                ObjectDisposedException.ThrowIf(this.disposed, this);
                this.selection.SelectedElement = null;
                this.Current = new(Guid.NewGuid(), root, WorkspaceModelStatus.Ready);
            }
        }

        /// <summary>Removes the active scope when it closes or access is withdrawn.</summary>
        /// <param name="unavailable">Whether the scope is unavailable rather than explicitly closed.</param>
        public void Clear(bool unavailable = false)
        {
            lock (this.gate)
            {
                if (this.disposed)
                {
                    return;
                }

                this.selection.SelectedElement = null;
                this.Current = new(Guid.NewGuid(), null,
                    unavailable ? WorkspaceModelStatus.Unavailable : WorkspaceModelStatus.Empty);
            }
        }

        /// <summary>Releases owned context without clearing the SDK's shared identity cache.</summary>
        public void Dispose()
        {
            lock (this.gate)
            {
                if (this.disposed)
                {
                    return;
                }

                this.disposed = true;
                this.selection.SelectedElement = null;
                this.Current = new(Guid.NewGuid(), null, WorkspaceModelStatus.Empty);
                this.lifetime.Cancel();
                this.lifetime.Dispose();
                foreach (var context in this.contexts.Items)
                {
                    this.loader.ReleaseUploadedModel(context.Snapshot.Root);
                }

                this.contexts.Dispose();
            }
        }

        /// <summary>Starts one eager file operation under a new local context identity.</summary>
        private Task<INamespace> StartLoad(Func<INamespace> load)
        {
            var id = Guid.NewGuid();
            var completion = new TaskCompletionSource<INamespace>(TaskCreationOptions.RunContinuationsAsynchronously);
            this.pending = completion.Task;
            this.selection.SelectedElement = null;
            this.Current = new(id, null, WorkspaceModelStatus.Loading);
            return this.CompleteLoadAsync(id, load, completion, this.lifetime.Token);
        }

        /// <summary>Publishes only the latest completed model, with serialized access to the SDK loader.</summary>
        private async Task<INamespace> CompleteLoadAsync(Guid id, Func<INamespace> load,
            TaskCompletionSource<INamespace> completion, CancellationToken token)
        {
            INamespace root = null;
            try
            {
                await this.loaderGate.WaitAsync(token);
                try
                {
                    root = await Task.Run(() =>
                    {
                        token.ThrowIfCancellationRequested();
                        lock (this.gate)
                        {
                            if (this.disposed || this.Current.Id != id)
                            {
                                return null;
                            }
                        }

                        return load();
                    }, token);
                }
                finally
                {
                    this.loaderGate.Release();
                }

                lock (this.gate)
                {
                    if (this.disposed || this.Current.Id != id)
                    {
                        root = null;
                    }
                    else
                    {
                        this.Current = new(id, root,
                            root is null ? WorkspaceModelStatus.Failed : WorkspaceModelStatus.Ready);
                    }
                }
            }
            catch (Exception)
            {
                root = null;
                lock (this.gate)
                {
                    if (!this.disposed && this.Current.Id == id)
                    {
                        this.Current = new(id, null, WorkspaceModelStatus.Failed);
                    }
                }
            }
            finally
            {
                completion.TrySetResult(root);
            }

            return root;
        }
    }
}