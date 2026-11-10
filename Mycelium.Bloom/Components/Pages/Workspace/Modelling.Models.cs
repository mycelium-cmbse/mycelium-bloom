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
        private WorkspaceModelSnapshot[] GetCompatibleModels()
        {
            foreach (var diagram in this.diagramViewModels.Values.Where(value =>
                         value.State?.Occurrences.Length > 0 && value.ModelContextId.HasValue))
            {
                this.compatibleContexts.Add(diagram.ModelContextId.Value);
            }

            var models = this.WorkspaceModel.GetOpenModels();
            this.compatibleContexts.IntersectWith(models.Select(model => model.Id));
            return models.Where(model => this.compatibleContexts.Contains(model.Id)).ToArray();
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
                await previous.CancelAsync();
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
                var browserTab = this.FindAssociatedBrowser(target, snapshot.Id);

                if (browserTab == Guid.Empty)
                {
                    browser = this.CreateProjectBrowserViewModel();
                    if (!await browser.InitializeAsync(snapshot.Root, cancellation.Token))
                    {
                        return;
                    }
                }

                if (!this.IsCurrentDiagramRequest(invokingTabId, invoking, cancellation.Token))
                {
                    return;
                }

                var group = this.WorkspaceEditorViewModel.Groups.FirstOrDefault(value =>
                    value.Tabs.Any(tab => tab.Id == invokingTabId));
                if (group is null)
                {
                    return;
                }

                var newlyBound = this.BindDiagram(invokingTabId, invoking, snapshot, replaceEmpty, group, ref target);

                if (!this.AttachBrowser(ref browser, browserTab, target, group, snapshot))
                {
                    return;
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
                // Closed or superseded owners cannot receive this completion.
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

        /// <summary>Finds the Browser owned by a diagram in the requested canonical context.</summary>
        /// <param name="diagramId">The owning diagram tab.</param>
        /// <param name="contextId">The canonical model context.</param>
        /// <returns>The matching tab identity or an empty identity when it must be created.</returns>
        private Guid FindAssociatedBrowser(Guid diagramId, Guid contextId)
        {
            var browserTab = this.diagramBrowsers.GetValueOrDefault(diagramId);
            return this.tabModels.TryGetValue(browserTab, out var model) && model.Snapshot.Id == contextId
                ? browserTab
                : Guid.Empty;
        }

        /// <summary>Binds an empty view or creates a related view without replacing a populated diagram.</summary>
        /// <param name="invokingTabId">The originating view.</param>
        /// <param name="invoking">The still-current originating view model.</param>
        /// <param name="snapshot">The accepted canonical context.</param>
        /// <param name="replaceEmpty">Whether the originating view contains no occurrences.</param>
        /// <param name="group">The originating workspace panel.</param>
        /// <param name="target">Receives the intended diagram tab.</param>
        /// <returns>Whether preparation must complete before focus.</returns>
        private bool BindDiagram(Guid invokingTabId, ViewModel.Diagram.DiagramViewModel invoking,
            WorkspaceModelSnapshot snapshot, bool replaceEmpty, ViewModel.WorkspaceEditor.EditorGroupViewModel group,
            ref Guid target)
        {
            var newlyBound = false;
            if (replaceEmpty && invoking.ModelContextId != snapshot.Id)
            {
                this.RebindEmptyDiagram(invokingTabId, invoking, snapshot);
                newlyBound = true;
            }
            else if (target == Guid.Empty)
            {
                this.OpenWorkspaceInterconnection(group.Id, snapshot);
                target = group.ActiveTab.Id;
                this.relatedDiagrams[(invokingTabId, snapshot.Id)] = target;
                newlyBound = true;
            }

            return newlyBound;
        }

        /// <summary>Publishes or activates the Browser owned by one diagram without retargeting other pairs.</summary>
        /// <param name="browser">The prepared Browser, transferred on successful attachment.</param>
        /// <param name="browserTab">An existing associated Browser tab.</param>
        /// <param name="target">The associated diagram tab.</param>
        /// <param name="group">The invoking workspace group.</param>
        /// <param name="snapshot">The shared canonical model.</param>
        /// <returns>Whether the associated Browser is available.</returns>
        private bool AttachBrowser(ref IProjectBrowserViewModel browser, Guid browserTab, Guid target,
            ViewModel.WorkspaceEditor.EditorGroupViewModel group, WorkspaceModelSnapshot snapshot)
        {
            if (browser is not null)
            {
                var browserGroup =
                    this.WorkspaceEditorViewModel.Groups.FirstOrDefault(value =>
                        value.Tabs.Any(IsProjectBrowserTab)) ?? group;
                if (!this.WorkspaceEditorViewModel.TryOpenTab(browserGroup.Id,
                        snapshot.Root.DeclaredName ?? "Project Browser",
                        ProjectBrowserViewTypeKey, out var openedBrowser))
                {
                    return false;
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

            return true;
        }

        /// <summary>Replaces an empty view's binding while retaining its independent tab identity.</summary>
        /// <param name="invokingTabId">The existing diagram tab.</param>
        /// <param name="invoking">The previous tab-owned view model.</param>
        /// <param name="snapshot">The successfully accepted canonical context.</param>
        private void RebindEmptyDiagram(Guid invokingTabId, ViewModel.Diagram.DiagramViewModel invoking,
            WorkspaceModelSnapshot snapshot)
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
        }

        /// <summary>Checks that an asynchronous completion still belongs to its original live diagram.</summary>
        /// <param name="tabId">The originating tab identity.</param>
        /// <param name="viewModel">The originating tab-owned view model.</param>
        /// <param name="cancellationToken">The request lifetime.</param>
        /// <returns>Whether publication to this diagram remains valid.</returns>
        private bool IsCurrentDiagramRequest(Guid tabId, ViewModel.Diagram.DiagramViewModel viewModel,
            CancellationToken cancellationToken) => !this.isDisposed && !cancellationToken.IsCancellationRequested
            && ReferenceEquals(this.diagramViewModels.GetValueOrDefault(tabId), viewModel);

        /// <summary>Requests focus after preparation only if the original tab still owns the view.</summary>
        /// <param name="tabId">The intended diagram tab.</param>
        /// <param name="viewModel">The exact tab-owned preparation source.</param>
        /// <param name="token">Cancels focus after supersession, disconnect or disposal.</param>
        /// <returns>Completion of the initial occurrence reveal.</returns>
        private async Task FocusPreparedDiagramAsync(Guid tabId, ViewModel.Diagram.DiagramViewModel viewModel,
            CancellationToken token)
        {
            await viewModel.Preparation.WaitAsync(token);
            if (!this.IsCurrentDiagramRequest(tabId, viewModel, token))
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