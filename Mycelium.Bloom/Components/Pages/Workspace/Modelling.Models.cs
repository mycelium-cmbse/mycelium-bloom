// ------------------------------------------------------------------------------------------------
// <copyright file="Modelling.Models.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.Pages.Workspace
{
    using System.Reactive.Linq;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;
    using ReactiveUI;

    public sealed partial class Modelling
    {
        /// <summary>Gets or sets the circuit disconnect cancellation boundary.</summary>
        [Microsoft.AspNetCore.Components.Inject]
        private Core.Context.WorkspaceConnectionHandler Connection { get; set; }

        /// <summary>Retains each tab's canonical model until that tab closes.</summary>
        private readonly Dictionary<Guid, WorkspaceModelLease> tabModels = [];

        /// <summary>Owns cancellation for each diagram's pending model attachment.</summary>
        private readonly Dictionary<Guid, CancellationTokenSource> modelOpenings = [];

        /// <summary>Associates each independent diagram with its own Browser tab.</summary>
        private readonly Dictionary<Guid, Guid> diagramBrowsers = [];

        /// <summary>Retains Browser scroll positions across component remounts.</summary>
        private readonly Dictionary<Guid, BrowserScrollPosition> browserScroll = [];

        /// <summary>Reuses views opened from one diagram without merging deliberately independent views.</summary>
        private readonly Dictionary<(Guid Origin, Guid Context), Guid> relatedDiagrams = [];

        /// <summary>Tracks open contexts whose prepared diagrams contain supported parts.</summary>
        private readonly HashSet<Guid> compatibleContexts = [];

        /// <summary>Gets open models already known to contain supported structural content.</summary>
        private IReadOnlyList<WorkspaceModelSnapshot> CompatibleModels
        {
            get
            {
                foreach (var diagram in this.diagramViewModels.Values.Where(value =>
                             value.State?.Occurrences.Length > 0 && value.ModelContextId.HasValue))
                {
                    this.compatibleContexts.Add(diagram.ModelContextId.Value);
                }

                var models = this.WorkspaceModel.OpenModels;
                this.compatibleContexts.IntersectWith(models.Select(model => model.Id));
                return models.Where(model => this.compatibleContexts.Contains(model.Id)).ToArray();
            }
        }

        /// <summary>Resolves the focused tab's model without retargeting other tabs.</summary>
        /// <returns>The tab-owned model or the initial workspace model.</returns>
        private WorkspaceModelSnapshot FocusedModel() =>
            this.WorkspaceEditorViewModel.FocusedGroup?.ActiveTab is { } tab &&
            this.tabModels.TryGetValue(tab.Id, out var lease)
                ? lease.Snapshot
                : this.WorkspaceModel.Current;

        /// <summary>Retains scroll state only while its Browser tab is owned.</summary>
        /// <param name="tabId">The originating Browser tab.</param>
        /// <param name="position">The browser-reported scroll offset.</param>
        private void CaptureBrowserScroll(Guid tabId, BrowserScrollPosition position)
        {
            if (!this.isDisposed && this.projectBrowserViewModels.ContainsKey(tabId))
            {
                this.browserScroll[tabId] = position;
            }
        }

        /// <summary>Attaches an accepted canonical context to a current diagram and its independently owned Browser.</summary>
        /// <param name="invokingTabId">The diagram that owns this opening request.</param>
        /// <param name="accepted">The caller-owned lease retained until attachment completes.</param>
        /// <returns>Completion of attachment and focus, or cancellation without publishing abandoned tabs.</returns>
        private async Task AttachModelAsync(Guid invokingTabId, WorkspaceModelLease accepted)
        {
            if (this.isDisposed || !this.diagramViewModels.TryGetValue(invokingTabId, out var invoking))
            {
                return;
            }

            if (this.modelOpenings.Remove(invokingTabId, out var previous))
            {
                previous.Cancel();
            }

            using var connection = this.Connection.CreateUploadCancellation();
            using var cancellation =
                CancellationTokenSource.CreateLinkedTokenSource(this.lifetimeCancellation.Token, connection.Token);
            this.modelOpenings[invokingTabId] = cancellation;
            IProjectBrowserViewModel browser = null;
            try
            {
                var snapshot = accepted.Snapshot;
                var replaceEmpty = invoking.State is null || invoking.State.Occurrences.Length == 0;
                var target = replaceEmpty || invoking.ModelContextId == snapshot.Id
                    ? invokingTabId
                    : this.relatedDiagrams.GetValueOrDefault((invokingTabId, snapshot.Id));
                var browserTab = this.diagramBrowsers.GetValueOrDefault(target);
                if (!this.tabModels.TryGetValue(browserTab, out var browserModel) ||
                    browserModel.Snapshot.Id != snapshot.Id)
                {
                    browserTab = Guid.Empty;
                }

                if (browserTab == Guid.Empty)
                {
                    browser = this.CreateProjectBrowserViewModel();
                    if (!await browser.InitializeAsync(snapshot.Root, cancellation.Token))
                    {
                        return;
                    }
                }

                if (this.isDisposed || cancellation.IsCancellationRequested
                                    || !this.diagramViewModels.TryGetValue(invokingTabId, out var current) ||
                                    !ReferenceEquals(current, invoking))
                {
                    return;
                }

                var group = this.WorkspaceEditorViewModel.Groups.FirstOrDefault(value =>
                    value.Tabs.Any(tab => tab.Id == invokingTabId));
                if (group is null)
                {
                    return;
                }

                var newlyBound = false;
                if (replaceEmpty && invoking.ModelContextId != snapshot.Id)
                {
                    var replacement = this.DiagramViewModelFactory();
                    replacement.UpdateWorkspace(snapshot);
                    if (this.diagramObservations.Remove(invokingTabId, out var observation))
                    {
                        observation.Dispose();
                    }

                    this.diagramViewModels[invokingTabId] = replacement;
                    invoking.Dispose();
                    if (this.tabModels.Remove(invokingTabId, out var oldLease))
                    {
                        oldLease.Dispose();
                    }

                    this.tabModels.Add(invokingTabId, this.WorkspaceModel.Acquire(snapshot));
                    this.diagramObservations.Add(invokingTabId, replacement
                        .WhenAnyValue(value => value.State, value => value.PreparationStatus)
                        .Select(_ => Observable.FromAsync(() => this.InvokeAsync(() =>
                        {
                            if (!this.isDisposed && this.diagramViewModels.ContainsKey(invokingTabId))
                            {
                                this.StateHasChanged();
                            }
                        }))).Concat().Subscribe(_ => { },
                            error => this.Logger.LogError(error, "Diagram preparation display failed")));
                    newlyBound = true;
                }
                else if (target == Guid.Empty)
                {
                    this.OpenWorkspaceInterconnection(group.Id, snapshot);
                    target = group.ActiveTab.Id;
                    this.relatedDiagrams[(invokingTabId, snapshot.Id)] = target;
                    newlyBound = true;
                }

                if (browser is not null)
                {
                    var browserGroup =
                        this.WorkspaceEditorViewModel.Groups.FirstOrDefault(value =>
                            value.Tabs.Any(IsProjectBrowserTab)) ?? group;
                    if (!this.WorkspaceEditorViewModel.TryOpenTab(browserGroup.Id,
                            snapshot.Root.DeclaredName ?? "Project Browser",
                            ProjectBrowserViewTypeKey, out var openedBrowser))
                    {
                        return;
                    }

                    browserTab = openedBrowser.Id;
                    this.projectBrowserViewModels.Add(browserTab, browser);
                    this.tabModels.Add(browserTab, this.WorkspaceModel.Acquire(snapshot));
                    this.diagramBrowsers[target] = browserTab;
                    browser = null;
                }
                else
                {
                    var browserGroup =
                        this.WorkspaceEditorViewModel.Groups.First(value =>
                            value.Tabs.Any(tab => tab.Id == browserTab));
                    this.WorkspaceEditorViewModel.ActivateTab(browserGroup.Id, browserTab);
                }

                var targetGroup =
                    this.WorkspaceEditorViewModel.Groups.First(value => value.Tabs.Any(tab => tab.Id == target));
                this.WorkspaceEditorViewModel.ActivateTab(targetGroup.Id, target);
                if (newlyBound)
                {
                    await this.FocusPreparedDiagramAsync(target, this.diagramViewModels[target], cancellation.Token);
                }

                this.StateHasChanged();
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
            }
            finally
            {
                browser?.Dispose();
                if (this.modelOpenings.GetValueOrDefault(invokingTabId) == cancellation)
                {
                    this.modelOpenings.Remove(invokingTabId);
                }
            }
        }

        /// <summary>Requests focus after preparation only if the original tab still owns the view.</summary>
        /// <param name="tabId">The intended diagram tab.</param>
        /// <param name="viewModel">The exact tab-owned preparation source.</param>
        /// <param name="token">Cancels focus after supersession, disconnect or disposal.</param>
        /// <returns>Completion of the initial occurrence reveal.</returns>
        private async Task FocusPreparedDiagramAsync(Guid tabId, ViewModel.Diagram.DiagramViewModel viewModel,
            CancellationToken token)
        {
            await viewModel.Preparation.WaitAsync(token);
            if (this.isDisposed || token.IsCancellationRequested ||
                this.diagramViewModels.GetValueOrDefault(tabId) != viewModel)
            {
                return;
            }

            if (viewModel.State?.Occurrences.FirstOrDefault() is { } occurrence)
            {
                await viewModel.RevealRequested.Execute(occurrence.ElementId);
            }
        }
    }
}