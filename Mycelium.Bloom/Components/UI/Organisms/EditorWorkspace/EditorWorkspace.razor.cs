// ------------------------------------------------------------------------------------------------
// <copyright file="EditorWorkspace.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.EditorWorkspace
{
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Globalization;

    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;
    using Microsoft.JSInterop;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.ViewModel.WorkspaceEditor;

    /// <summary>
    /// Renders independently tabbed, resizable editor groups over caller-owned workspace state.
    /// </summary>
    public sealed partial class EditorWorkspace : BloomComponentBase, IAsyncDisposable
    {
        /// <summary>
        /// The keyboard resize increment expressed as a share of the adjacent pair.
        /// </summary>
        private const double SplitterKeyboardStep = 0.05d;

        /// <summary>
        /// The minimum share retained by either member of an adjacent pair.
        /// </summary>
        private const double MinimumAdjacentShare = 0.10d;

        /// <summary>
        /// The JavaScript export used to release splitter pointer capture.
        /// </summary>
        private const string ReleasePointerFunction = "releasePointer";

        /// <summary>
        /// The generated identity used to scope DOM relationships and JavaScript registrations.
        /// </summary>
        private readonly string workspaceId = CreateGeneratedId("mb-editor-workspace");

        /// <summary>
        /// The component-local normalized weight for every rendered group.
        /// </summary>
        private readonly Dictionary<Guid, double> groupWeights = [];

        /// <summary>
        /// The group instances whose nested notifications are currently observed.
        /// </summary>
        private readonly HashSet<EditorGroupViewModel> observedGroups = [];

        /// <summary>
        /// The tab instances whose metadata notifications are currently observed.
        /// </summary>
        private readonly HashSet<EditorTabItem> observedTabs = [];

        /// <summary>
        /// Coordinates ownership of notification tasks with component disposal.
        /// </summary>
        private readonly object notificationTaskSync = new();

        /// <summary>
        /// The ViewModel graph currently observed by this component.
        /// </summary>
        private IWorkspaceEditorViewModel observedViewModel;

        /// <summary>
        /// The JavaScript module used for pointer capture and deterministic DOM focus.
        /// </summary>
        private IJSObjectReference module;

        /// <summary>
        /// The active pointer-resize baseline, if a splitter owns pointer capture.
        /// </summary>
        private SplitterResizeState splitterResizeState;

        /// <summary>
        /// The identity of an asynchronous pointer-capture request that has not completed yet.
        /// </summary>
        private object pendingSplitterCapture;

        /// <summary>
        /// The component-local version used to reject pointer baselines measured for an outdated group graph.
        /// </summary>
        private long groupGraphVersion;

        /// <summary>
        /// The group selected for presentation at compact component widths.
        /// </summary>
        private Guid? compactGroupId;

        /// <summary>
        /// The DOM element to focus after the next completed render.
        /// </summary>
        private string pendingFocusElementId;

        /// <summary>
        /// A value indicating whether the root-scoped keyboard guard is registered.
        /// </summary>
        private bool keydownGuardRegistered;

        /// <summary>
        /// The in-flight initialization observed by disposal before it releases JavaScript resources.
        /// </summary>
        private Task javaScriptInitializationTask;

        /// <summary>
        /// The dispatcher work owned for synchronous observed-event callbacks.
        /// </summary>
        private Task ownedNotificationTasks = Task.CompletedTask;

        /// <summary>
        /// A value indicating whether component disposal has begun.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// Gets or sets the JavaScript runtime used by the collocated interaction module.
        /// </summary>
        [Inject]
        private IJSRuntime JsRuntime { get; set; }

        /// <summary>
        /// Gets or sets the caller-owned workspace state.
        /// </summary>
        [Parameter]
        [EditorRequired]
        public IWorkspaceEditorViewModel ViewModel { get; set; }

        /// <summary>
        /// Gets or sets the template used to render the exact active tab instance in each group.
        /// </summary>
        [Parameter]
        [EditorRequired]
        public RenderFragment<EditorTabItem> EditorContent { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when a group requests a new tab.
        /// </summary>
        [Parameter]
        public EventCallback<Guid> AddTabRequested { get; set; }

        /// <summary>
        /// Gets or sets the accessible label of the workspace region.
        /// </summary>
        [Parameter]
        public string AriaLabel { get; set; } = "Editor workspace";

        /// <summary>
        /// Gets or sets the accessible-label prefix applied to numbered editor groups.
        /// </summary>
        [Parameter]
        public string GroupAriaLabel { get; set; } = "Editor group";

        /// <summary>
        /// Gets or sets the accessible label of the compact group switcher.
        /// </summary>
        [Parameter]
        public string CompactGroupSwitcherAriaLabel { get; set; } = "Editor groups";

        /// <summary>
        /// Gets or sets optional relative weights sampled only when a ViewModel is attached or replaced.
        /// </summary>
        [Parameter]
        public IReadOnlyDictionary<Guid, double> InitialGroupWeights { get; set; } =
            new Dictionary<Guid, double>();

        /// <inheritdoc />
        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            if (!ReferenceEquals(this.observedViewModel, this.ViewModel))
            {
                await this.ReleaseActivePointerCaptureAsync();
                this.ObserveViewModel(this.ViewModel);
            }
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (this.isDisposed)
            {
                return;
            }

            if (this.module is null || !this.keydownGuardRegistered)
            {
                var initializationTask = this.InitializeJavaScriptResourcesAsync();
                this.javaScriptInitializationTask = initializationTask;

                try
                {
                    await initializationTask;
                }
                finally
                {
                    if (ReferenceEquals(this.javaScriptInitializationTask, initializationTask))
                    {
                        this.javaScriptInitializationTask = null;
                    }
                }
            }

            var currentModule = this.module;

            if (this.isDisposed || currentModule is null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(this.pendingFocusElementId))
            {
                var elementId = this.pendingFocusElementId;
                this.pendingFocusElementId = null;

                try
                {
                    _ = await currentModule.InvokeAsync<bool>("focusElementById", elementId);
                }
                catch (JSDisconnectedException)
                {
                    // The circuit ended before the queued focus could be restored.
                }
                catch (ObjectDisposedException)
                {
                    // Component teardown won the race with the queued focus request.
                }
            }
        }

        /// <summary>
        /// Releases component-owned subscriptions, pointer capture, keyboard guards, and JavaScript resources.
        /// </summary>
        /// <returns>A value task representing asynchronous cleanup.</returns>
        public async ValueTask DisposeAsync()
        {
            Task notificationTasks;

            lock (this.notificationTaskSync)
            {
                if (this.isDisposed)
                {
                    return;
                }

                this.isDisposed = true;
                notificationTasks = this.ownedNotificationTasks;
            }

            var resizeState = this.splitterResizeState;
            this.splitterResizeState = null;
            this.ObserveViewModel(null);

            try
            {
                await notificationTasks;
            }
            finally
            {
                await this.DisposeJavaScriptResourcesAsync(resizeState);
            }
        }

        /// <summary>
        /// Releases JavaScript initialization, pointer capture, keyboard guards, and the collocated module.
        /// </summary>
        /// <param name="resizeState">The pointer capture still owned when disposal began.</param>
        /// <returns>A task representing JavaScript resource cleanup.</returns>
        private async Task DisposeJavaScriptResourcesAsync(SplitterResizeState resizeState)
        {
            if (this.javaScriptInitializationTask is { } initializationTask)
            {
                try
                {
                    await initializationTask;
                }
                catch (JSDisconnectedException)
                {
                    // The circuit ended while the browser resource was being initialized.
                }
                catch (ObjectDisposedException)
                {
                    // The browser resource was released while initialization was completing.
                }
                catch (JSException)
                {
                    // Initialization failure must not prevent already-owned resources from releasing.
                }
            }

            var currentModule = this.module;
            this.module = null;

            if (currentModule is null)
            {
                this.keydownGuardRegistered = false;
                return;
            }

            if (resizeState is not null)
            {
                await TryInvokeJavaScriptCleanupAsync(
                    currentModule,
                    ReleasePointerFunction,
                    resizeState.SeparatorId,
                    resizeState.PointerId);
            }

            if (this.keydownGuardRegistered)
            {
                await TryInvokeJavaScriptCleanupAsync(
                    currentModule,
                    "unregisterKeydownGuards",
                    this.workspaceId);
            }

            await TryDisposeJavaScriptModuleAsync(currentModule);
            this.keydownGuardRegistered = false;
        }

        /// <summary>
        /// Imports the collocated module and registers its root-scoped keyboard guard without outliving disposal.
        /// </summary>
        /// <returns>A task representing JavaScript resource initialization.</returns>
        private async Task InitializeJavaScriptResourcesAsync()
        {
            var currentModule = this.module;
            var ownsImportedModule = currentModule is null;
            var retainImportedModule = false;

            if (ownsImportedModule)
            {
                currentModule = await this.JsRuntime.InvokeAsync<IJSObjectReference>(
                    "import",
                    "./Components/UI/Organisms/EditorWorkspace/EditorWorkspace.razor.js");
            }

            try
            {
                if (this.isDisposed)
                {
                    return;
                }

                var guardRegistered = this.keydownGuardRegistered;

                if (!guardRegistered)
                {
                    guardRegistered = await currentModule.InvokeAsync<bool>(
                        "registerKeydownGuards",
                        this.workspaceId);
                }

                if (this.isDisposed)
                {
                    if (guardRegistered && !this.keydownGuardRegistered)
                    {
                        await currentModule.InvokeVoidAsync("unregisterKeydownGuards", this.workspaceId);
                    }

                    return;
                }

                this.module = currentModule;
                this.keydownGuardRegistered = guardRegistered;
                retainImportedModule = true;
            }
            finally
            {
                if (ownsImportedModule && !retainImportedModule)
                {
                    await TryDisposeJavaScriptModuleAsync(currentModule);
                }
            }
        }

        /// <summary>
        /// Best-effort invokes one browser cleanup operation without preventing later owned resources from releasing.
        /// </summary>
        /// <param name="currentModule">The collocated module being torn down.</param>
        /// <param name="identifier">The cleanup export to invoke.</param>
        /// <param name="arguments">The cleanup export arguments.</param>
        /// <returns>A task representing the cleanup request.</returns>
        private static async ValueTask TryInvokeJavaScriptCleanupAsync(
            IJSObjectReference currentModule,
            string identifier,
            params object[] arguments)
        {
            try
            {
                await currentModule.InvokeVoidAsync(identifier, arguments);
            }
            catch (JSDisconnectedException)
            {
                // The circuit ended, so browser-side cleanup can no longer be requested.
            }
            catch (ObjectDisposedException)
            {
                // Another teardown path already released the browser resource.
            }
            catch (JSException)
            {
                // A failed browser cleanup must not prevent the remaining owned resources from releasing.
            }
        }

        /// <summary>
        /// Best-effort releases the collocated module reference after its owned registrations are cleaned up.
        /// </summary>
        /// <param name="currentModule">The collocated module being released.</param>
        /// <returns>A task representing module disposal.</returns>
        private static async ValueTask TryDisposeJavaScriptModuleAsync(IJSObjectReference currentModule)
        {
            try
            {
                await currentModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The circuit ended, so the browser-side module reference is already unreachable.
            }
            catch (ObjectDisposedException)
            {
                // Another teardown path already released the module reference.
            }
            catch (JSException)
            {
                // Browser teardown failures cannot be recovered during component disposal.
            }
        }

        /// <summary>
        /// Gets the final CSS class list applied to the workspace root.
        /// </summary>
        /// <returns>The workspace root CSS classes.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass("mb-editor-workspace");
        }

        /// <summary>
        /// Gets the CSS classes applied to one editor group.
        /// </summary>
        /// <param name="group">The rendered group.</param>
        /// <returns>The group CSS classes.</returns>
        private string GetGroupCssClass(EditorGroupViewModel group)
        {
            return CssClassBuilder.Build(
                "mb-editor-workspace__group",
                CssClassBuilder.When(
                    "mb-editor-workspace__group--focused",
                    ReferenceEquals(this.ViewModel?.FocusedGroup, group)));
        }

        /// <summary>
        /// Gets the CSS classes applied to a tab and its sibling actions.
        /// </summary>
        /// <param name="isActive">Whether the tab is active.</param>
        /// <returns>The tab-item CSS classes.</returns>
        private static string GetTabItemCssClass(bool isActive)
        {
            return CssClassBuilder.Build(
                "mb-editor-workspace__tab-item",
                CssClassBuilder.When("mb-editor-workspace__tab-item--active", isActive));
        }

        /// <summary>
        /// Gets the CSS classes applied to a compact group button.
        /// </summary>
        /// <param name="group">The represented group.</param>
        /// <returns>The compact-button CSS classes.</returns>
        private string GetCompactButtonCssClass(EditorGroupViewModel group)
        {
            return CssClassBuilder.Build(
                "mb-editor-workspace__compact-button",
                CssClassBuilder.When(
                    "mb-editor-workspace__compact-button--active",
                    this.IsCompactGroupPresented(group)));
        }

        /// <summary>
        /// Gets the normalized flex-weight declaration for a group.
        /// </summary>
        /// <param name="group">The rendered group.</param>
        /// <returns>The inline custom-property declaration.</returns>
        private string GetGroupStyle(EditorGroupViewModel group)
        {
            return $"--mb-editor-group-weight: {this.GetGroupWeight(group.Id).ToString("0.############", CultureInfo.InvariantCulture)};";
        }

        /// <summary>
        /// Gets the cumulative boundary declaration for a splitter.
        /// </summary>
        /// <param name="leftGroupIndex">The index of the group preceding the splitter.</param>
        /// <returns>The inline custom-property declaration.</returns>
        private string GetSplitterStyle(int leftGroupIndex)
        {
            var position = 0d;

            for (var index = 0; index <= leftGroupIndex; index++)
            {
                position += this.GetGroupWeight(this.ViewModel.Groups[index].Id);
            }

            return $"--mb-editor-splitter-position: {(position * 100d).ToString("0.######", CultureInfo.InvariantCulture)}%;";
        }

        /// <summary>
        /// Gets the left group's current percentage within an adjacent pair.
        /// </summary>
        /// <param name="leftGroup">The group preceding the splitter.</param>
        /// <param name="rightGroup">The group following the splitter.</param>
        /// <returns>The rounded percentage exposed through separator ARIA.</returns>
        private int GetAdjacentLeftPercentage(
            EditorGroupViewModel leftGroup,
            EditorGroupViewModel rightGroup)
        {
            return (int)Math.Round(
                this.GetAdjacentLeftShare(leftGroup, rightGroup) * 100d,
                MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Gets the minimum percentage reachable from the separator's current presentation state.
        /// </summary>
        /// <param name="leftGroup">The group preceding the splitter.</param>
        /// <param name="rightGroup">The group following the splitter.</param>
        /// <returns>The rounded minimum percentage exposed through separator ARIA.</returns>
        private int GetAdjacentMinimumPercentage(
            EditorGroupViewModel leftGroup,
            EditorGroupViewModel rightGroup)
        {
            return Math.Min(
                (int)(MinimumAdjacentShare * 100d),
                this.GetAdjacentLeftPercentage(leftGroup, rightGroup));
        }

        /// <summary>
        /// Gets the maximum percentage reachable from the separator's current presentation state.
        /// </summary>
        /// <param name="leftGroup">The group preceding the splitter.</param>
        /// <param name="rightGroup">The group following the splitter.</param>
        /// <returns>The rounded maximum percentage exposed through separator ARIA.</returns>
        private int GetAdjacentMaximumPercentage(
            EditorGroupViewModel leftGroup,
            EditorGroupViewModel rightGroup)
        {
            return Math.Max(
                (int)((1d - MinimumAdjacentShare) * 100d),
                this.GetAdjacentLeftPercentage(leftGroup, rightGroup));
        }

        /// <summary>
        /// Gets the left group's current normalized share within an adjacent pair.
        /// </summary>
        /// <param name="leftGroup">The group preceding the splitter.</param>
        /// <param name="rightGroup">The group following the splitter.</param>
        /// <returns>The left group's adjacent-pair share.</returns>
        private double GetAdjacentLeftShare(
            EditorGroupViewModel leftGroup,
            EditorGroupViewModel rightGroup)
        {
            var leftWeight = this.GetGroupWeight(leftGroup.Id);
            var pairWeight = leftWeight + this.GetGroupWeight(rightGroup.Id);

            return pairWeight <= 0d ? 0.5d : leftWeight / pairWeight;
        }

        /// <summary>
        /// Gets the accessible numbered label for a group.
        /// </summary>
        /// <param name="groupIndex">The zero-based group index.</param>
        /// <returns>The group label.</returns>
        private string GetGroupAccessibleLabel(int groupIndex)
        {
            return $"{this.GroupAriaLabel} {groupIndex + 1}";
        }

        /// <summary>
        /// Gets the accessible label for a group's independent tab list.
        /// </summary>
        /// <param name="groupIndex">The zero-based group index.</param>
        /// <returns>The tab-list label.</returns>
        private string GetTabListAccessibleLabel(int groupIndex)
        {
            return $"{this.GetGroupAccessibleLabel(groupIndex)} tabs";
        }

        /// <summary>
        /// Gets the concise label displayed by a compact group button.
        /// </summary>
        /// <param name="group">The represented group.</param>
        /// <param name="groupIndex">The zero-based group index.</param>
        /// <returns>The compact label.</returns>
        private string GetCompactGroupLabel(EditorGroupViewModel group, int groupIndex)
        {
            return group.ActiveTab?.Title ?? this.GetGroupAccessibleLabel(groupIndex);
        }

        /// <summary>
        /// Gets a unique accessible name for a compact group button even when active tab titles are duplicated.
        /// </summary>
        /// <param name="group">The represented group.</param>
        /// <param name="groupIndex">The zero-based group index.</param>
        /// <returns>The numbered group label with its active tab title when available.</returns>
        private string GetCompactGroupAccessibleLabel(EditorGroupViewModel group, int groupIndex)
        {
            var groupLabel = this.GetGroupAccessibleLabel(groupIndex);

            return group.ActiveTab is { } activeTab
                ? $"{groupLabel}: {activeTab.Title}"
                : groupLabel;
        }

        /// <summary>
        /// Gets the stable group-region element identifier.
        /// </summary>
        /// <param name="groupId">The group identity.</param>
        /// <returns>The generated DOM identifier.</returns>
        private string GetGroupElementId(Guid groupId)
        {
            return $"{this.workspaceId}-group-{groupId:N}";
        }

        /// <summary>
        /// Gets the stable semantic-tab element identifier.
        /// </summary>
        /// <param name="groupId">The owning group identity.</param>
        /// <param name="tabId">The tab identity.</param>
        /// <returns>The generated DOM identifier.</returns>
        private string GetTabElementId(Guid groupId, Guid tabId)
        {
            return $"{this.workspaceId}-group-{groupId:N}-tab-{tabId:N}";
        }

        /// <summary>
        /// Gets the stable active-panel element identifier.
        /// </summary>
        /// <param name="groupId">The owning group identity.</param>
        /// <param name="tabId">The tab identity.</param>
        /// <returns>The generated DOM identifier.</returns>
        private string GetPanelElementId(Guid groupId, Guid tabId)
        {
            return $"{this.workspaceId}-group-{groupId:N}-panel-{tabId:N}";
        }

        /// <summary>
        /// Gets the stable add-tab element identifier for a group.
        /// </summary>
        /// <param name="groupId">The owning group identity.</param>
        /// <returns>The generated DOM identifier.</returns>
        private string GetAddTabElementId(Guid groupId)
        {
            return $"{this.workspaceId}-group-{groupId:N}-add-tab";
        }

        /// <summary>
        /// Gets the stable splitter element identifier for an adjacent pair.
        /// </summary>
        /// <param name="leftGroupId">The left group identity.</param>
        /// <param name="rightGroupId">The right group identity.</param>
        /// <returns>The generated DOM identifier.</returns>
        private string GetSplitterElementId(Guid leftGroupId, Guid rightGroupId)
        {
            return $"{this.workspaceId}-splitter-{leftGroupId:N}-{rightGroupId:N}";
        }

        /// <summary>
        /// Determines whether a group is the compact presentation target.
        /// </summary>
        /// <param name="group">The candidate group.</param>
        /// <returns>True when the group is selected for compact presentation.</returns>
        private bool IsCompactGroupPresented(EditorGroupViewModel group)
        {
            return this.compactGroupId == group.Id;
        }

        /// <summary>
        /// Selects and logically focuses a group through the compact presentation control.
        /// </summary>
        /// <param name="group">The selected group.</param>
        private void SelectCompactGroup(EditorGroupViewModel group)
        {
            this.compactGroupId = group.Id;

            if (this.ViewModel.FocusGroup(group.Id))
            {
                this.QueueFocusForGroup(group);
            }
        }

        /// <summary>
        /// Gives a rendered group logical workspace focus.
        /// </summary>
        /// <param name="group">The group receiving focus.</param>
        private void FocusGroup(EditorGroupViewModel group)
        {
            if (!ReferenceEquals(this.ViewModel.FocusedGroup, group))
            {
                _ = this.ViewModel.FocusGroup(group.Id);
            }
        }

        /// <summary>
        /// Activates a tab through the ViewModel and optionally restores DOM focus after rendering.
        /// </summary>
        /// <param name="group">The owning group.</param>
        /// <param name="tab">The selected tab.</param>
        /// <param name="restoreDomFocus">Whether the semantic tab should receive post-render focus.</param>
        private void ActivateTab(
            EditorGroupViewModel group,
            EditorTabItem tab,
            bool restoreDomFocus)
        {
            if (this.ViewModel.ActivateTab(group.Id, tab.Id) && restoreDomFocus)
            {
                this.pendingFocusElementId = this.GetTabElementId(group.Id, tab.Id);
            }
        }

        /// <summary>
        /// Closes a tab through the ViewModel and restores focus to the resulting focused group.
        /// </summary>
        /// <param name="group">The owning group.</param>
        /// <param name="tab">The tab to close.</param>
        private void CloseTab(EditorGroupViewModel group, EditorTabItem tab)
        {
            _ = this.ViewModel.FocusGroup(group.Id);

            if (this.ViewModel.CloseTab(group.Id, tab.Id))
            {
                this.QueueFocusForGroup(this.ViewModel.FocusedGroup);
            }
        }

        /// <summary>
        /// Requests a new tab for one group after giving that group logical workspace focus.
        /// </summary>
        /// <param name="group">The group requesting the tab.</param>
        /// <returns>A task representing the caller-owned tab request.</returns>
        private async Task RequestAddTabAsync(EditorGroupViewModel group)
        {
            if (!this.AddTabRequested.HasDelegate)
            {
                return;
            }

            this.FocusGroup(group);
            await this.AddTabRequested.InvokeAsync(group.Id);
        }

        /// <summary>
        /// Handles automatic horizontal tab-list keyboard activation.
        /// </summary>
        /// <param name="group">The owning group.</param>
        /// <param name="tab">The currently focused tab.</param>
        /// <param name="args">The keyboard event.</param>
        private void HandleTabKeyDown(
            EditorGroupViewModel group,
            EditorTabItem tab,
            KeyboardEventArgs args)
        {
            var currentIndex = group.Tabs.IndexOf(tab);

            if (currentIndex < 0 || group.Tabs.Count == 0)
            {
                return;
            }

            var targetIndex = args.Key switch
            {
                "ArrowLeft" => (currentIndex - 1 + group.Tabs.Count) % group.Tabs.Count,
                "ArrowRight" => (currentIndex + 1) % group.Tabs.Count,
                "Home" => 0,
                "End" => group.Tabs.Count - 1,
                _ => -1
            };

            if (targetIndex >= 0)
            {
                this.ActivateTab(group, group.Tabs[targetIndex], true);
            }
        }

        /// <summary>
        /// Releases an active splitter capture before presentation attaches to a different ViewModel graph.
        /// </summary>
        /// <returns>A task representing pointer release.</returns>
        private async Task ReleaseActivePointerCaptureAsync()
        {
            var resizeState = this.splitterResizeState;
            this.splitterResizeState = null;
            var currentModule = this.module;

            if (resizeState is null || currentModule is null)
            {
                return;
            }

            await TryInvokeJavaScriptCleanupAsync(
                currentModule,
                ReleasePointerFunction,
                resizeState.SeparatorId,
                resizeState.PointerId);
        }

        /// <summary>
        /// Begins one splitter resize using measured adjacent-group geometry and pointer capture.
        /// </summary>
        /// <param name="leftGroup">The group preceding the splitter.</param>
        /// <param name="rightGroup">The group following the splitter.</param>
        /// <param name="args">The pointer-down event.</param>
        /// <returns>A task representing pointer capture.</returns>
        private async Task BeginSplitterResizeAsync(
            EditorGroupViewModel leftGroup,
            EditorGroupViewModel rightGroup,
            PointerEventArgs args)
        {
            var attachedViewModel = this.observedViewModel;
            var currentModule = this.module;
            var capturedGraphVersion = this.groupGraphVersion;

            if (currentModule is null
                || this.splitterResizeState is not null
                || this.pendingSplitterCapture is not null
                || args.Button != 0)
            {
                return;
            }

            var separatorId = this.GetSplitterElementId(leftGroup.Id, rightGroup.Id);
            var captureIdentity = new object();
            this.pendingSplitterCapture = captureIdentity;

            try
            {
                var geometry = await currentModule.InvokeAsync<double[]>(
                    "capturePointer",
                    separatorId,
                    args.PointerId);

                if (geometry is null
                    || geometry.Length != 3
                    || geometry.Any(value => !double.IsFinite(value))
                    || geometry[2] <= 0d
                    || geometry[0] < 0d
                    || geometry[1] < 0d)
                {
                    await TryInvokeJavaScriptCleanupAsync(
                        currentModule,
                        ReleasePointerFunction,
                        separatorId,
                        args.PointerId);

                    return;
                }

                if (this.isDisposed
                    || capturedGraphVersion != this.groupGraphVersion
                    || !ReferenceEquals(this.observedViewModel, attachedViewModel)
                    || !this.AreCurrentAdjacentGroups(leftGroup.Id, rightGroup.Id))
                {
                    await TryInvokeJavaScriptCleanupAsync(
                        currentModule,
                        ReleasePointerFunction,
                        separatorId,
                        args.PointerId);

                    return;
                }

                var pairWeight = this.GetGroupWeight(leftGroup.Id) + this.GetGroupWeight(rightGroup.Id);
                var measuredLeftShare = Math.Clamp(geometry[0] / geometry[2], 0d, 1d);

                this.splitterResizeState = new SplitterResizeState(
                    separatorId,
                    args.PointerId,
                    leftGroup.Id,
                    rightGroup.Id,
                    args.ClientX,
                    geometry[2],
                    pairWeight,
                    measuredLeftShare);
            }
            finally
            {
                if (ReferenceEquals(this.pendingSplitterCapture, captureIdentity))
                {
                    this.pendingSplitterCapture = null;
                }
            }
        }

        /// <summary>
        /// Applies a captured pointer delta to the measured adjacent pair.
        /// </summary>
        /// <param name="args">The pointer-move event.</param>
        private void HandleSplitterPointerMoved(PointerEventArgs args)
        {
            if (this.splitterResizeState is not { } resizeState
                || resizeState.PointerId != args.PointerId)
            {
                return;
            }

            var deltaShare = (args.ClientX - resizeState.StartClientX) / resizeState.PairWidth;
            var leftShare = ClampAdjacentShare(
                resizeState.InitialLeftShare + deltaShare,
                resizeState.InitialLeftShare);

            this.groupWeights[resizeState.LeftGroupId] = resizeState.PairWeight * leftShare;
            this.groupWeights[resizeState.RightGroupId] = resizeState.PairWeight * (1d - leftShare);
        }

        /// <summary>
        /// Releases pointer capture and completes a splitter resize.
        /// </summary>
        /// <param name="args">The pointer-up or pointer-cancel event.</param>
        /// <returns>A task representing pointer release.</returns>
        private async Task EndSplitterResizeAsync(PointerEventArgs args)
        {
            if (this.splitterResizeState is not { } resizeState
                || resizeState.PointerId != args.PointerId)
            {
                return;
            }

            this.splitterResizeState = null;
            var currentModule = this.module;

            if (currentModule is not null)
            {
                await TryInvokeJavaScriptCleanupAsync(
                    currentModule,
                    ReleasePointerFunction,
                    resizeState.SeparatorId,
                    resizeState.PointerId);
            }
        }

        /// <summary>
        /// Clears a resize whose browser pointer capture ended independently.
        /// </summary>
        /// <param name="args">The lost-capture event.</param>
        private void HandlePointerCaptureLost(PointerEventArgs args)
        {
            if (this.splitterResizeState?.PointerId == args.PointerId)
            {
                this.splitterResizeState = null;
            }
        }

        /// <summary>
        /// Resizes an adjacent pair through its focusable separator.
        /// </summary>
        /// <param name="leftGroup">The group preceding the separator.</param>
        /// <param name="rightGroup">The group following the separator.</param>
        /// <param name="args">The keyboard event.</param>
        private void HandleSplitterKeyDown(
            EditorGroupViewModel leftGroup,
            EditorGroupViewModel rightGroup,
            KeyboardEventArgs args)
        {
            var direction = args.Key switch
            {
                "ArrowLeft" => -1d,
                "ArrowRight" => 1d,
                _ => 0d
            };

            if (direction == 0d)
            {
                return;
            }

            var leftWeight = this.GetGroupWeight(leftGroup.Id);
            var rightWeight = this.GetGroupWeight(rightGroup.Id);
            var pairWeight = leftWeight + rightWeight;
            var currentLeftShare = pairWeight <= 0d ? 0.5d : leftWeight / pairWeight;
            var nextLeftShare = ClampAdjacentShare(
                currentLeftShare + (direction * SplitterKeyboardStep),
                currentLeftShare);

            this.groupWeights[leftGroup.Id] = pairWeight * nextLeftShare;
            this.groupWeights[rightGroup.Id] = pairWeight * (1d - nextLeftShare);
        }

        /// <summary>
        /// Clamps a proposed adjacent share without snapping a valid initial seed that begins outside the standard
        /// keyboard and pointer range.
        /// </summary>
        /// <param name="proposedShare">The proposed left share of the adjacent pair.</param>
        /// <param name="baselineShare">The share at the beginning of the interaction.</param>
        /// <returns>The share constrained to the interaction's reachable range.</returns>
        private static double ClampAdjacentShare(double proposedShare, double baselineShare)
        {
            var minimumShare = Math.Min(MinimumAdjacentShare, baselineShare);
            var maximumShare = Math.Max(1d - MinimumAdjacentShare, baselineShare);

            return Math.Clamp(proposedShare, minimumShare, maximumShare);
        }

        /// <summary>
        /// Determines whether two group identities still form an ordered adjacent pair in the attached graph.
        /// </summary>
        /// <param name="leftGroupId">The expected left group identity.</param>
        /// <param name="rightGroupId">The expected right group identity.</param>
        /// <returns>True when the groups remain adjacent in their original order.</returns>
        private bool AreCurrentAdjacentGroups(Guid leftGroupId, Guid rightGroupId)
        {
            if (this.observedViewModel is null)
            {
                return false;
            }

            for (var groupIndex = 0; groupIndex < this.observedViewModel.Groups.Count - 1; groupIndex++)
            {
                if (this.observedViewModel.Groups[groupIndex].Id == leftGroupId
                    && this.observedViewModel.Groups[groupIndex + 1].Id == rightGroupId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Replaces the observed ViewModel graph and initializes its presentation state once.
        /// </summary>
        /// <param name="viewModel">The new caller-owned ViewModel, or null.</param>
        private void ObserveViewModel(IWorkspaceEditorViewModel viewModel)
        {
            if (this.observedViewModel is not null)
            {
                this.observedViewModel.PropertyChanged -= this.HandleViewModelPropertyChanged;

                if (this.observedViewModel.Groups is INotifyCollectionChanged previousGroups)
                {
                    previousGroups.CollectionChanged -= this.HandleGroupsChanged;
                }
            }

            foreach (var group in this.observedGroups.ToArray())
            {
                this.UnobserveGroup(group);
            }

            this.ReconcileTabSubscriptions();

            this.observedViewModel = viewModel;
            this.groupGraphVersion++;
            this.groupWeights.Clear();
            this.splitterResizeState = null;
            this.pendingSplitterCapture = null;
            this.compactGroupId = null;
            this.pendingFocusElementId = null;

            if (this.observedViewModel is null)
            {
                return;
            }

            this.observedViewModel.PropertyChanged += this.HandleViewModelPropertyChanged;

            if (this.observedViewModel.Groups is INotifyCollectionChanged currentGroups)
            {
                currentGroups.CollectionChanged += this.HandleGroupsChanged;
            }

            this.ReconcileGroupSubscriptions();
            this.InitializeGroupWeights();
            this.compactGroupId = this.observedViewModel.FocusedGroup?.Id
                ?? this.observedViewModel.Groups.FirstOrDefault()?.Id;
        }

        /// <summary>
        /// Attaches and detaches nested group observations to match current membership.
        /// </summary>
        private void ReconcileGroupSubscriptions()
        {
            var currentGroups = this.observedViewModel?.Groups.ToHashSet() ?? [];

            foreach (var removedGroup in this.observedGroups.Except(currentGroups).ToArray())
            {
                this.UnobserveGroup(removedGroup);
            }

            foreach (var addedGroup in currentGroups.Except(this.observedGroups))
            {
                this.ObserveGroup(addedGroup);
            }

            this.ReconcileTabSubscriptions();
        }

        /// <summary>
        /// Observes active-tab and tab-membership changes for one group.
        /// </summary>
        /// <param name="group">The group to observe.</param>
        private void ObserveGroup(EditorGroupViewModel group)
        {
            group.PropertyChanged += this.HandleGroupPropertyChanged;

            if (group.Tabs is INotifyCollectionChanged tabs)
            {
                tabs.CollectionChanged += this.HandleTabsChanged;
            }

            this.observedGroups.Add(group);
        }

        /// <summary>
        /// Detaches observations created for one group.
        /// </summary>
        /// <param name="group">The group to stop observing.</param>
        private void UnobserveGroup(EditorGroupViewModel group)
        {
            group.PropertyChanged -= this.HandleGroupPropertyChanged;

            if (group.Tabs is INotifyCollectionChanged tabs)
            {
                tabs.CollectionChanged -= this.HandleTabsChanged;
            }

            this.observedGroups.Remove(group);
        }

        /// <summary>
        /// Attaches and detaches tab metadata observations to match the tabs in all currently observed groups.
        /// </summary>
        private void ReconcileTabSubscriptions()
        {
            var currentTabs = this.observedGroups
                .SelectMany(group => group.Tabs)
                .ToHashSet();

            foreach (var removedTab in this.observedTabs.Except(currentTabs).ToArray())
            {
                this.UnobserveTab(removedTab);
            }

            foreach (var addedTab in currentTabs.Except(this.observedTabs))
            {
                this.ObserveTab(addedTab);
            }
        }

        /// <summary>
        /// Observes mutable metadata for one rendered tab instance.
        /// </summary>
        /// <param name="tab">The tab to observe.</param>
        private void ObserveTab(EditorTabItem tab)
        {
            tab.PropertyChanged += this.HandleTabPropertyChanged;
            this.observedTabs.Add(tab);
        }

        /// <summary>
        /// Detaches the metadata observation owned for one tab instance.
        /// </summary>
        /// <param name="tab">The tab to stop observing.</param>
        private void UnobserveTab(EditorTabItem tab)
        {
            tab.PropertyChanged -= this.HandleTabPropertyChanged;
            this.observedTabs.Remove(tab);
        }

        /// <summary>
        /// Initializes normalized weights from the initial-only caller input or neutral defaults.
        /// </summary>
        private void InitializeGroupWeights()
        {
            this.groupWeights.Clear();

            if (this.observedViewModel?.Groups.Count is not > 0)
            {
                return;
            }

            foreach (var groupId in this.observedViewModel.Groups.Select(group => group.Id))
            {
                var weight = 1d;

                if (this.InitialGroupWeights is not null
                    && this.InitialGroupWeights.TryGetValue(groupId, out var configuredWeight)
                    && double.IsFinite(configuredWeight)
                    && configuredWeight > 0d)
                {
                    weight = configuredWeight;
                }

                this.groupWeights[groupId] = weight;
            }

            this.NormalizeWeights();
        }

        /// <summary>
        /// Reconciles local weights after groups are added or removed without resetting retained ratios.
        /// </summary>
        private void ReconcileGroupWeights()
        {
            if (this.observedViewModel?.Groups.Count is not > 0)
            {
                this.groupWeights.Clear();
                return;
            }

            var currentIds = this.observedViewModel.Groups.Select(group => group.Id).ToHashSet();

            foreach (var removedId in this.groupWeights.Keys.Where(id => !currentIds.Contains(id)).ToArray())
            {
                this.groupWeights.Remove(removedId);
            }

            var newGroups = this.observedViewModel.Groups
                .Where(group => !this.groupWeights.ContainsKey(group.Id))
                .ToArray();

            if (newGroups.Length == 0)
            {
                this.NormalizeWeights();
                return;
            }

            var totalGroupCount = this.observedViewModel.Groups.Count;
            var retainedGroupCount = totalGroupCount - newGroups.Length;

            if (retainedGroupCount > 0)
            {
                this.NormalizeWeights();
                var retainedTargetShare = (double)retainedGroupCount / totalGroupCount;

                foreach (var retainedId in this.groupWeights.Keys.ToArray())
                {
                    this.groupWeights[retainedId] *= retainedTargetShare;
                }
            }

            var newGroupShare = 1d / totalGroupCount;

            foreach (var newGroup in newGroups)
            {
                this.groupWeights[newGroup.Id] = newGroupShare;
            }

            this.NormalizeWeights();
        }

        /// <summary>
        /// Normalizes all current local group weights to a unit total.
        /// </summary>
        private void NormalizeWeights()
        {
            if (this.groupWeights.Count == 0)
            {
                return;
            }

            var total = this.groupWeights.Values
                .Where(weight => double.IsFinite(weight) && weight > 0d)
                .Sum();

            if (!double.IsFinite(total) || total <= 0d)
            {
                var equalWeight = 1d / this.groupWeights.Count;

                foreach (var groupId in this.groupWeights.Keys.ToArray())
                {
                    this.groupWeights[groupId] = equalWeight;
                }

                return;
            }

            foreach (var groupId in this.groupWeights.Keys.ToArray())
            {
                this.groupWeights[groupId] = this.groupWeights[groupId] / total;
            }
        }

        /// <summary>
        /// Gets a group's local weight with a neutral fallback.
        /// </summary>
        /// <param name="groupId">The group identity.</param>
        /// <returns>The positive local weight.</returns>
        private double GetGroupWeight(Guid groupId)
        {
            return this.groupWeights.TryGetValue(groupId, out var weight) && weight > 0d
                ? weight
                : 1d;
        }

        /// <summary>
        /// Queues focus for the active tab or the focused empty group's surviving control.
        /// </summary>
        /// <param name="group">The group whose surviving control should receive focus.</param>
        private void QueueFocusForGroup(EditorGroupViewModel group)
        {
            if (group?.ActiveTab is { } activeTab)
            {
                this.pendingFocusElementId = this.GetTabElementId(group.Id, activeTab.Id);
            }
            else if (group is not null && ReferenceEquals(this.observedViewModel?.FocusedGroup, group))
            {
                this.pendingFocusElementId = this.AddTabRequested.HasDelegate
                    ? this.GetAddTabElementId(group.Id)
                    : this.GetGroupElementId(group.Id);
            }
        }

        /// <summary>
        /// Reconciles group presentation when membership changes.
        /// </summary>
        /// <param name="sender">The observed group collection.</param>
        /// <param name="args">The collection change.</param>
        private void HandleGroupsChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            this.QueueObservedNotification(() => this.ApplyGroupsChangedAsync(sender));
        }

        /// <summary>
        /// Reconciles one current group-membership notification on the renderer dispatcher.
        /// </summary>
        /// <param name="sender">The collection that raised the notification.</param>
        /// <returns>A task representing pointer-release completion.</returns>
        private async Task ApplyGroupsChangedAsync(object sender)
        {
            if (this.observedViewModel is null
                || !ReferenceEquals(sender, this.observedViewModel.Groups))
            {
                return;
            }

            this.groupGraphVersion++;
            this.pendingSplitterCapture = null;
            var pointerReleaseTask = this.ReleaseActivePointerCaptureAsync();

            try
            {
                this.ReconcileGroupSubscriptions();
                this.ReconcileGroupWeights();

                if (this.compactGroupId is null
                    || this.observedViewModel.Groups.All(group => group.Id != this.compactGroupId))
                {
                    this.compactGroupId = this.observedViewModel.FocusedGroup?.Id
                        ?? this.observedViewModel.Groups.FirstOrDefault()?.Id;
                }

                this.StateHasChanged();
            }
            finally
            {
                await pointerReleaseTask;
            }
        }

        /// <summary>
        /// Refreshes logical-focus-dependent presentation after top-level state changes.
        /// </summary>
        /// <param name="sender">The observed ViewModel.</param>
        /// <param name="args">The property change.</param>
        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            this.QueueObservedNotification(() => this.ApplyViewModelPropertyChangedAsync(sender, args));
        }

        /// <summary>
        /// Applies one current workspace notification on the renderer dispatcher.
        /// </summary>
        /// <param name="sender">The ViewModel that raised the notification.</param>
        /// <param name="args">The property change.</param>
        /// <returns>A completed task.</returns>
        private Task ApplyViewModelPropertyChangedAsync(object sender, PropertyChangedEventArgs args)
        {
            if (!ReferenceEquals(sender, this.observedViewModel))
            {
                return Task.CompletedTask;
            }

            if (string.IsNullOrEmpty(args.PropertyName)
                || args.PropertyName == nameof(IWorkspaceEditorViewModel.FocusedGroup))
            {
                this.compactGroupId = this.observedViewModel.FocusedGroup?.Id ?? this.compactGroupId;
            }

            this.StateHasChanged();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Refreshes active-tab-dependent presentation after nested state changes.
        /// </summary>
        /// <param name="sender">The observed group.</param>
        /// <param name="args">The property change.</param>
        private void HandleGroupPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            this.QueueObservedNotification(() => this.ApplyGroupPropertyChangedAsync(sender));
        }

        /// <summary>
        /// Applies one current group notification on the renderer dispatcher.
        /// </summary>
        /// <param name="sender">The group that raised the notification.</param>
        /// <returns>A completed task.</returns>
        private Task ApplyGroupPropertyChangedAsync(object sender)
        {
            if (sender is not EditorGroupViewModel group || !this.observedGroups.Contains(group))
            {
                return Task.CompletedTask;
            }

            this.StateHasChanged();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Refreshes tab membership after nested collection changes.
        /// </summary>
        /// <param name="sender">The observed tab collection.</param>
        /// <param name="args">The collection change.</param>
        private void HandleTabsChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            this.QueueObservedNotification(() => this.ApplyTabsChangedAsync(sender));
        }

        /// <summary>
        /// Reconciles one current tab-membership notification on the renderer dispatcher.
        /// </summary>
        /// <param name="sender">The collection that raised the notification.</param>
        /// <returns>A completed task.</returns>
        private Task ApplyTabsChangedAsync(object sender)
        {
            if (!this.observedGroups.Any(group => ReferenceEquals(group.Tabs, sender)))
            {
                return Task.CompletedTask;
            }

            this.ReconcileTabSubscriptions();
            this.StateHasChanged();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Refreshes tab presentation after metadata changes on a rendered tab instance.
        /// </summary>
        /// <param name="sender">The observed tab.</param>
        /// <param name="args">The property change.</param>
        private void HandleTabPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            this.QueueObservedNotification(() => this.ApplyTabPropertyChangedAsync(sender));
        }

        /// <summary>
        /// Applies one current tab notification on the renderer dispatcher.
        /// </summary>
        /// <param name="sender">The tab that raised the notification.</param>
        /// <returns>A completed task.</returns>
        private Task ApplyTabPropertyChangedAsync(object sender)
        {
            if (sender is not EditorTabItem tab || !this.observedTabs.Contains(tab))
            {
                return Task.CompletedTask;
            }

            this.StateHasChanged();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Owns dispatcher work requested by one synchronous observed-event callback.
        /// </summary>
        /// <param name="notification">The current-graph work to perform on the renderer dispatcher.</param>
        private void QueueObservedNotification(Func<Task> notification)
        {
            var dispatchStart = new TaskCompletionSource();

            lock (this.notificationTaskSync)
            {
                if (this.isDisposed)
                {
                    return;
                }

                var notificationTask = this.DispatchObservedNotificationAsync(dispatchStart.Task, notification);
                this.ownedNotificationTasks = this.ownedNotificationTasks.IsCompletedSuccessfully
                    ? notificationTask
                    : Task.WhenAll(this.ownedNotificationTasks, notificationTask);
            }

            dispatchStart.SetResult();
        }

        /// <summary>
        /// Marshals one owned notification to the renderer after task ownership is recorded.
        /// </summary>
        /// <param name="dispatchStart">The signal released after task coordination leaves its lock.</param>
        /// <param name="notification">The current-graph work to perform.</param>
        /// <returns>A task representing the dispatched work.</returns>
        private async Task DispatchObservedNotificationAsync(Task dispatchStart, Func<Task> notification)
        {
            await dispatchStart;

            try
            {
                await this.InvokeAsync(async () =>
                {
                    if (!this.isDisposed)
                    {
                        await notification();
                    }
                });
            }
            catch (ObjectDisposedException)
            {
                // The renderer ended before already-owned notification work could run.
            }
            catch (Exception exception)
            {
                try
                {
                    await this.DispatchExceptionAsync(exception);
                }
                catch (ObjectDisposedException)
                {
                    // The renderer ended before the observed failure could be dispatched.
                }
            }
        }

        /// <summary>
        /// Captures the immutable baseline used throughout one pointer resize.
        /// </summary>
        /// <param name="SeparatorId">The separator holding pointer capture.</param>
        /// <param name="PointerId">The captured pointer identity.</param>
        /// <param name="LeftGroupId">The left group identity.</param>
        /// <param name="RightGroupId">The right group identity.</param>
        /// <param name="StartClientX">The pointer's initial viewport coordinate.</param>
        /// <param name="PairWidth">The measured combined adjacent width.</param>
        /// <param name="PairWeight">The component-local combined adjacent weight.</param>
        /// <param name="InitialLeftShare">The measured initial left share.</param>
        private sealed record SplitterResizeState(
            string SeparatorId,
            long PointerId,
            Guid LeftGroupId,
            Guid RightGroupId,
            double StartClientX,
            double PairWidth,
            double PairWeight,
            double InitialLeftShare);
    }
}
