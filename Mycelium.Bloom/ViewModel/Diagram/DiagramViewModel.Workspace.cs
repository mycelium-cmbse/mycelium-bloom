// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramViewModel.Workspace.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram
{
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.Model.Enum;
    using ReactiveUI;
    using SysML2.NET.Core.POCO.Root.Elements;

    public sealed partial class DiagramViewModel
    {
        private Guid? workspaceContextId;

        /// <summary>Resolves identities only within the captured canonical scope.</summary>
        private Dictionary<string, IElement> workspaceElements;

        private IDisposable workspaceSubscription;
        private readonly DiagramDataOptions workspaceDataOptions;
        private readonly Microsoft.Extensions.Internal.ISystemClock workspaceCacheClock;
        private WorkspaceDiagramIndex workspaceIndex;
        private CancellationTokenSource workspacePreparation;

        /// <summary>Gets completion of the current cancellable scope preparation.</summary>
        public Task Preparation { get; private set; } = Task.CompletedTask;

        /// <summary>Gets current formatted-detail cache statistics independently of fetched payloads and SDK retention.</summary>
        public Microsoft.Extensions.Caching.Memory.MemoryCacheStatistics WorkspaceDetailCache =>
            this.workspaceIndex?.Statistics;

        /// <summary>Gets the number of canonical references retained by this tab's scope index.</summary>
        public int IndexedElementCount => this.workspaceElements?.Count ?? 0;

        /// <summary>Gets this view's captured model context independently of workspace focus.</summary>
        public Guid? ModelContextId => this.workspaceContextId;

        /// <summary>Checks canonical identity in this view's model before forwarding selection.</summary>
        /// <param name="element">The exact selected SDK object.</param>
        /// <returns>Whether the object belongs to this captured scope.</returns>
        public bool ContainsCanonicalElement(IElement element) => element is not null
                                                                  && (!this.IsWorkspaceBound ||
                                                                      this.workspaceElements?.GetValueOrDefault(
                                                                              element.ElementId) is
                                                                      { } candidate &&
                                                                      ReferenceEquals(candidate, element));

        /// <summary>Gets preparation progress or a scope-specific failure.</summary>
        public string PreparationStatus
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        } = string.Empty;

        /// <summary>Observes the active model independently of renderer scheduling.</summary>
        /// <param name="session">The circuit-owned canonical workspace context.</param>
        public void BindWorkspace(WorkspaceModelSession session)
        {
            lock (this.mutationGate)
            {
                ObjectDisposedException.ThrowIf(this.disposed, this);
                if (this.IsWorkspaceBound || this.State is not null)
                {
                    throw new InvalidOperationException("The editor already has a model scope.");
                }

                this.workspaceSubscription =
                    session.WhenAnyValue(value => value.Current).Subscribe(this.UpdateWorkspace);
            }
        }

        /// <summary>Gets whether this tab is bound to a canonical workspace scope.</summary>
        public bool IsWorkspaceBound { get; private set; }

        /// <summary>Gets whether the captured scope is still available in the active context.</summary>
        public bool IsScopeAvailable
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        } = true;

        /// <summary>Gets the bound scope's actual model name.</summary>
        public string ScopeName { get; private set; }

        /// <summary>Gets the current context state for an unmounted editor.</summary>
        public WorkspaceModelStatus WorkspaceStatus { get; private set; }

        /// <summary>Binds a fresh editor once, without retargeting existing presentation on model switches.</summary>
        /// <param name="snapshot">The workspace owner's coherent canonical context.</param>
        public void UpdateWorkspace(WorkspaceModelSnapshot snapshot)
        {
            lock (this.mutationGate)
            {
                if (this.disposed)
                {
                    return;
                }

                this.IsWorkspaceBound = true;
                this.WorkspaceStatus = snapshot.Status;
                if (!this.workspaceContextId.HasValue &&
                    snapshot.Status is WorkspaceModelStatus.Loading or WorkspaceModelStatus.Ready)
                {
                    this.workspaceContextId = snapshot.Id;
                }

                if (this.workspaceContextId == snapshot.Id && snapshot.Status == WorkspaceModelStatus.Ready &&
                    this.State is null && this.workspacePreparation is null)
                {
                    this.Limits = DiagramPresentationLimits.Workspace;
                    this.ScopeName = snapshot.Root.DeclaredName;
                    this.workspacePreparation = CancellationTokenSource.CreateLinkedTokenSource(this.lifetime.Token);
                    this.Preparation = this.PrepareWorkspaceAsync(snapshot, this.workspacePreparation.Token);
                }

                var available = snapshot.Status == WorkspaceModelStatus.Ready && this.workspaceContextId == snapshot.Id;
                if (!available)
                {
                    this.workspacePreparation?.Cancel();
                    this.loadingSubscription?.Dispose();
                    this.workspaceIndex?.Dispose();
                    this.workspaceIndex = null;
                    this.workspaceElements = null;
                    this.selectionRevision++;
                    this.WorkspaceStatus = this.workspaceContextId.HasValue && this.workspaceContextId != snapshot.Id
                        ? WorkspaceModelStatus.Unavailable
                        : snapshot.Status;
                }

                this.IsScopeAvailable = available;
                this.RaisePropertyChanged(nameof(this.CanEditPresentation));
            }
        }

        private async Task PrepareWorkspaceAsync(WorkspaceModelSnapshot snapshot, CancellationToken cancellationToken)
        {
            try
            {
                this.PreparationStatus = "Preparing Interconnection…";
                var index = await WorkspaceDiagramIndex.CreateAsync(snapshot.Root, this.workspaceDataOptions, count =>
                {
                    lock (this.mutationGate)
                    {
                        if (!this.disposed && !cancellationToken.IsCancellationRequested)
                        {
                            this.PreparationStatus = $"Preparing Interconnection… {count:N0} elements indexed";
                        }
                    }
                }, cancellationToken, this.workspaceCacheClock).ConfigureAwait(false);
                lock (this.mutationGate)
                {
                    if (this.disposed || cancellationToken.IsCancellationRequested)
                    {
                        index.Dispose();
                        return;
                    }

                    this.workspaceIndex = index;
                    this.workspaceElements = index.Elements;
                    this.Projection = index.Metadata;
                    this.Region = index.Presentation with { Occurrences = [], Routes = [] };
                    this.StartRegionLoading();
                    this.State = index.Presentation;
                    this.PreparationStatus = string.Empty;
                }
            }
            catch (OperationCanceledException)
            {
                // Closed or superseded owners cannot receive this completion.
            }
            catch (Exception error) when (error is InvalidDataException or InvalidOperationException
                                              or ArgumentException)
            {
                lock (this.mutationGate)
                {
                    if (!this.disposed && !cancellationToken.IsCancellationRequested)
                    {
                        this.PreparationStatus = error.Message;
                    }
                }
            }
        }
    }
}