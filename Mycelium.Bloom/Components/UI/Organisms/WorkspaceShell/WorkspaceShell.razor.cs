// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceShell.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.WorkspaceShell
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Common;

    /// <summary>
    /// Represents a reusable top-level engineering workspace layout.
    /// </summary>
    /// <remarks>
    /// The simultaneous three-region layout is intended for component widths above 45rem. At 45rem and below,
    /// the shell exposes one full-width pane at a time through its accessible pane switcher. The compact layout is
    /// supported down to 20rem; narrower embedding widths are outside the supported presentation target.
    /// </remarks>
    public partial class WorkspaceShell : BloomComponentBase, IAsyncDisposable
    {
        /// <summary>
        /// Identifies the workspace pane exposed at compact component widths.
        /// </summary>
        private enum NarrowPane
        {
            /// <summary>
            /// The left navigation pane.
            /// </summary>
            Left,

            /// <summary>
            /// The primary canvas pane.
            /// </summary>
            Main,

            /// <summary>
            /// The right details pane.
            /// </summary>
            Right
        }

        /// <summary>
        /// The stable identifier of the left panel region.
        /// </summary>
        private readonly string leftPanelId = CreateGeneratedId("mb-workspace-navigation");

        /// <summary>
        /// The stable identifier of the main content region.
        /// </summary>
        private readonly string mainContentId = CreateGeneratedId("mb-workspace-canvas");

        /// <summary>
        /// The stable identifier of the right panel region.
        /// </summary>
        private readonly string rightPanelId = CreateGeneratedId("mb-workspace-details");

        /// <summary>
        /// Gets or sets the pane shown by the compact-width pane switcher.
        /// </summary>
        private NarrowPane ActiveNarrowPane { get; set; } = NarrowPane.Main;

        /// <summary>The root used by component-local geometry and focus handling.</summary>
        private ElementReference rootElement;

        /// <summary>The owned browser module and per-instance interaction registration.</summary>
        private IJSObjectReference module, interaction;

        /// <summary>The callback reference owned by this shell.</summary>
        private DotNetObjectReference<WorkspaceShell> selfReference;

        /// <summary>The single in-flight browser initialization.</summary>
        private Task initialization;

        /// <summary>Prevents late browser callbacks after shell teardown.</summary>
        private bool isDisposed;

        /// <summary>Gets or sets the browser runtime for geometry and pointer capture.</summary>
        [Inject]
        private IJSRuntime JsRuntime { get; set; }

        /// <summary>Gets or sets whether the mounted right region is folded and inert.</summary>
        [Parameter]
        public bool RightPanelCollapsed { get; set; }

        /// <summary>Gets or sets whether the right region exposes a generic resize separator.</summary>
        [Parameter]
        public bool RightPanelResizable { get; set; }

        /// <summary>Gets or sets the caller-owned preferred right width in CSS pixels.</summary>
        [Parameter]
        public double RightPanelWidth { get; set; } = 380;

        /// <summary>Gets or sets the callback for a completed user resize.</summary>
        [Parameter]
        public EventCallback<double> RightPanelWidthChanged { get; set; }

        /// <summary>Gets or sets the minimum right width in CSS pixels.</summary>
        [Parameter]
        public double RightPanelMinimumWidth { get; set; } = 260;

        /// <summary>Gets or sets the maximum right width before responsive constraints.</summary>
        [Parameter]
        public double RightPanelMaximumWidth { get; set; } = 600;

        /// <summary>Gets or sets the preferred minimum central content width.</summary>
        [Parameter]
        public double MinimumMainContentWidth { get; set; } = 320;

        /// <summary>Gets or sets the accessible separator action label.</summary>
        [Parameter]
        public string RightPanelResizeLabel { get; set; } = "Resize right panel";

        /// <summary>Gets or sets an optional focus-return control for folded content.</summary>
        [Parameter]
        public string RightPanelFocusReturnId { get; set; }

        /// <summary>Gets or sets an optional stable identifier for the right region boundary.</summary>
        [Parameter]
        public string RightPanelTestId { get; set; }

        /// <summary>Reveals the right pane in the compact switcher without changing controlled collapse state.</summary>
        public void RevealRightPanel()
        {
            if (!this.isDisposed && this.ShouldRenderRightPanel())
            {
                this.ActiveNarrowPane = NarrowPane.Right;
                this.StateHasChanged();
            }
        }

        /// <summary>Forwards a bounded user resize to the sole preferred-width authority.</summary>
        /// <param name="width">The measured user-selected width in CSS pixels.</param>
        /// <returns>The controlled width callback.</returns>
        [JSInvokable]
        public Task CommitRightPanelWidth(double width)
        {
            return !this.isDisposed && this.RightPanelResizable && !this.RightPanelCollapsed
                   && this.ShouldRenderRightPanel() && double.IsFinite(width)
                ? this.RightPanelWidthChanged.InvokeAsync(Math.Clamp(width, this.RightPanelMinimumWidth, this.RightPanelMaximumWidth))
                : Task.CompletedTask;
        }

        /// <summary>Initializes optional browser interaction and applies controlled parameters after rendering.</summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (this.isDisposed || (!this.RightPanelResizable && !this.RightPanelCollapsed && this.initialization is null))
            {
                return;
            }
            this.initialization ??= this.InitializeInteractionAsync();
            await this.initialization;
            if (!this.isDisposed && this.interaction is not null)
            {
                await this.interaction.InvokeVoidAsync("update", this.RightPanelWidth, this.RightPanelMinimumWidth,
                    this.RightPanelMaximumWidth, this.MinimumMainContentWidth, this.RightPanelFocusReturnId);
            }
        }

        /// <summary>Creates one browser registration while retaining ownership through asynchronous teardown.</summary>
        private async Task InitializeInteractionAsync()
        {
            this.module = await this.JsRuntime.InvokeAsync<IJSObjectReference>("import",
                "./Components/UI/Organisms/WorkspaceShell/WorkspaceShell.razor.js");
            if (!this.isDisposed && this.module is not null)
            {
                this.selfReference = DotNetObjectReference.Create(this);
                this.interaction = await this.module.InvokeAsync<IJSObjectReference>("create", this.rootElement, this.selfReference);
            }
        }

        /// <summary>Releases geometry observation, pointer capture and browser callback references.</summary>
        public async ValueTask DisposeAsync()
        {
            if (this.isDisposed)
            {
                return;
            }
            this.isDisposed = true;
            try
            {
                if (this.initialization is not null)
                {
                    await this.initialization;
                }
                if (this.interaction is not null)
                {
                    await this.interaction.InvokeVoidAsync("dispose");
                    await this.interaction.DisposeAsync();
                }
                if (this.module is not null)
                {
                    await this.module.DisposeAsync();
                }
            }
            catch (JSDisconnectedException)
            {
            }
            finally
            {
                this.selfReference?.Dispose();
            }
        }

        /// <summary>
        /// Gets or sets the accessible label of the workspace.
        /// </summary>
        [Parameter]
        public string AriaLabel { get; set; } = "Engineering workspace";

        /// <summary>
        /// Gets or sets the accessible label of the header region.
        /// </summary>
        [Parameter]
        public string HeaderAriaLabel { get; set; } = "Workspace header";

        /// <summary>
        /// Gets or sets the accessible label of the left panel.
        /// </summary>
        [Parameter]
        public string LeftPanelAriaLabel { get; set; } = "Workspace navigation";

        /// <summary>
        /// Gets or sets the accessible label of the main content region.
        /// </summary>
        [Parameter]
        public string MainContentAriaLabel { get; set; } = "Workspace content";

        /// <summary>
        /// Gets or sets the accessible label of the right panel.
        /// </summary>
        [Parameter]
        public string RightPanelAriaLabel { get; set; } = "Workspace details";

        /// <summary>
        /// Gets or sets the accessible label of the status region.
        /// </summary>
        [Parameter]
        public string StatusBarAriaLabel { get; set; } = "Workspace status";

        /// <summary>
        /// Gets or sets the visible compact-switcher label for the left panel.
        /// </summary>
        [Parameter]
        public string CompactLeftPanelLabel { get; set; } = "Navigation";

        /// <summary>
        /// Gets or sets the visible compact-switcher label for the primary content.
        /// </summary>
        [Parameter]
        public string CompactMainContentLabel { get; set; } = "Canvas";

        /// <summary>
        /// Gets or sets the visible compact-switcher label for the right panel.
        /// </summary>
        [Parameter]
        public string CompactRightPanelLabel { get; set; } = "Details";

        /// <summary>
        /// Gets or sets a value indicating whether the shell uses its full-application presentation.
        /// </summary>
        /// <remarks>
        /// The embedded presentation remains the default for compatibility with existing consumers.
        /// </remarks>
        [Parameter]
        public bool FullApplication { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the left panel is visible.
        /// </summary>
        [Parameter]
        public bool LeftPanelVisible { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the left panel uses its collapsed desktop width.
        /// </summary>
        [Parameter]
        public bool LeftPanelCollapsed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the right panel is visible.
        /// </summary>
        [Parameter]
        public bool RightPanelVisible { get; set; } = true;

        /// <summary>
        /// Gets or sets optional workspace header content.
        /// </summary>
        [Parameter]
        public RenderFragment Header { get; set; }

        /// <summary>
        /// Gets or sets optional left navigation or sidebar content.
        /// </summary>
        [Parameter]
        public RenderFragment LeftPanel { get; set; }

        /// <summary>
        /// Gets or sets the primary workspace content.
        /// </summary>
        [Parameter]
        [EditorRequired]
        public RenderFragment MainContent { get; set; }

        /// <summary>
        /// Gets or sets optional right detail or inspector content.
        /// </summary>
        [Parameter]
        public RenderFragment RightPanel { get; set; }

        /// <summary>
        /// Gets or sets optional bottom status content.
        /// </summary>
        [Parameter]
        public RenderFragment StatusBar { get; set; }

        /// <summary>
        /// Gets or sets optional content layered over the workspace.
        /// </summary>
        [Parameter]
        public RenderFragment OverlayContent { get; set; }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (this.RightPanelResizable && (!double.IsFinite(this.RightPanelWidth)
                || !double.IsFinite(this.RightPanelMinimumWidth) || this.RightPanelMinimumWidth <= 0
                || !double.IsFinite(this.RightPanelMaximumWidth) || this.RightPanelMaximumWidth < this.RightPanelMinimumWidth
                || !double.IsFinite(this.MinimumMainContentWidth) || this.MinimumMainContentWidth < 0))
            {
                throw new ArgumentOutOfRangeException(nameof(this.RightPanelWidth), "Panel widths must define finite, ordered bounds.");
            }

            if ((this.ActiveNarrowPane == NarrowPane.Left && !this.ShouldRenderLeftPanel())
                || (this.ActiveNarrowPane == NarrowPane.Right && (!this.ShouldRenderRightPanel() || this.RightPanelCollapsed)))
            {
                this.ActiveNarrowPane = NarrowPane.Main;
            }
        }

        /// <summary>
        /// Gets the final CSS class list applied to the workspace shell.
        /// </summary>
        /// <returns>The workspace-shell CSS class list.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass(
                "mb-workspace-shell",
                CssClassBuilder.When(
                    "mb-workspace-shell--full-application",
                    this.FullApplication),
                CssClassBuilder.When(
                    "mb-workspace-shell--left-panel-collapsed",
                    this.LeftPanelCollapsed),
                CssClassBuilder.When("mb-workspace-shell--right-resizable", this.RightPanelResizable));
        }

        /// <summary>
        /// Gets a value indicating whether the left panel has content and is visible.
        /// </summary>
        /// <returns>True when the left panel should render; otherwise, false.</returns>
        private bool ShouldRenderLeftPanel()
        {
            return this.LeftPanelVisible && this.LeftPanel is not null;
        }

        /// <summary>
        /// Gets a value indicating whether the right panel has content and is visible.
        /// </summary>
        /// <returns>True when the right panel should render; otherwise, false.</returns>
        private bool ShouldRenderRightPanel()
        {
            return this.RightPanelVisible && this.RightPanel is not null;
        }

        /// <summary>
        /// Gets a value indicating whether compact widths need pane-selection controls.
        /// </summary>
        /// <returns>True when at least one optional side pane is present; otherwise, false.</returns>
        private bool ShouldRenderPaneSwitcher()
        {
            return this.ShouldRenderLeftPanel() || this.ShouldRenderRightPanel();
        }

        /// <summary>
        /// Selects the pane exposed at compact component widths.
        /// </summary>
        /// <param name="pane">The pane requested by the user.</param>
        private void SelectNarrowPane(NarrowPane pane)
        {
            this.ActiveNarrowPane = pane;
        }

        /// <summary>
        /// Gets a string-valued pressed state for a compact pane button.
        /// </summary>
        /// <param name="pane">The represented pane.</param>
        /// <returns>True when the pane is active; otherwise, false.</returns>
        private string GetPaneAriaPressed(NarrowPane pane)
        {
            return this.ActiveNarrowPane == pane ? "true" : "false";
        }

        /// <summary>
        /// Gets the CSS classes for a compact pane button.
        /// </summary>
        /// <param name="pane">The represented pane.</param>
        /// <returns>The pane-button class list.</returns>
        private string GetPaneButtonCssClass(NarrowPane pane)
        {
            return CssClassBuilder.Build(
                "mb-workspace-shell__pane-button",
                CssClassBuilder.When(
                    "mb-workspace-shell__pane-button--active",
                    this.ActiveNarrowPane == pane));
        }
    }
}
