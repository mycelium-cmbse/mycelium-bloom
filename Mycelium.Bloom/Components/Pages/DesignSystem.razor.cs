// ------------------------------------------------------------------------------------------------
// <copyright file="DesignSystem.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.Pages
{
    using System.Globalization;
    using Microsoft.AspNetCore.Components;
    using Microsoft.Extensions.Options;
    using Microsoft.JSInterop;

    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.NavigationRail;
    using Mycelium.Bloom.ViewModel.WorkspaceEditor;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>
    /// Provides a development-only composition surface for the reusable Bloom component library.
    /// </summary>
    public partial class DesignSystem : ComponentBase, IAsyncDisposable
    {
        /// <summary>
        /// Identifies this page instance as the current owner of the document theme preview.
        /// </summary>
        private readonly string themeOwnerId = $"mb-design-system-theme-{Guid.NewGuid():N}";

        /// <summary>
        /// Publishes context changes for the page-owned navigation-rail preview.
        /// </summary>
        private ContextAwareService navigationRailPreviewContext;

        /// <summary>
        /// References the page-scoped theme module after interactive rendering begins.
        /// </summary>
        private IJSObjectReference themeModule;

        /// <summary>
        /// Gets or sets the JavaScript runtime used to apply the preview theme to the document root.
        /// </summary>
        [Inject]
        private IJSRuntime JsRuntime { get; set; }

        /// <summary>
        /// Gets or sets the active page-level preview theme.
        /// </summary>
        private string ThemeName { get; set; } = "light";

        /// <summary>
        /// Gets the local select options used by the form examples.
        /// </summary>
        private IReadOnlyList<SelectInputOption> SelectOptions { get; } =
        [
            new() { Value = "preparation", Label = "Preparation" },
            new() { Value = "open", Label = "Open" },
            new() { Value = "review", Label = "In review" },
            new() { Value = "verification", Label = "Verification pending across multiple engineering workspaces" },
            new() { Value = "archived", Label = "Archived", Disabled = true }
        ];

        /// <summary>
        /// Gets the local actions used by independent action-menu examples.
        /// </summary>
        private IReadOnlyList<ActionMenuItem> ActionMenuItems { get; } =
        [
            new() { Id = "open", Label = "Open details", Description = "Inspect the selected element", Symbol = SymbolIconName.Inspect },
            new() { Id = "duplicate", Label = "Duplicate into another architecture workspace", Description = "Create a local copy", Symbol = SymbolIconName.Copy },
            new() { Id = "publish", Label = "Publish", Disabled = true },
            new() { Id = "delete", Label = "Delete", Symbol = SymbolIconName.Delete, Destructive = true, SeparatorBefore = true }
        ];

        /// <summary>
        /// Gets the local secondary actions used by the split-button example.
        /// </summary>
        private IReadOnlyList<ActionMenuItem> SplitButtonItems { get; } =
        [
            new() { Id = "save-draft", Label = "Save as draft", Symbol = SymbolIconName.Document },
            new() { Id = "save-copy", Label = "Save a copy", Symbol = SymbolIconName.Copy },
            new() { Id = "save-protected", Label = "Save protected copy", Symbol = SymbolIconName.Document, Disabled = true },
            new() { Id = "discard", Label = "Discard changes", Symbol = SymbolIconName.Delete, Destructive = true, SeparatorBefore = true }
        ];

        /// <summary>
        /// Gets the local account actions used by the user-menu examples.
        /// </summary>
        private IReadOnlyList<ActionMenuItem> UserMenuItems { get; } =
        [
            new() { Id = "profile", Label = "Profile", Description = "Manage local presentation settings", Symbol = SymbolIconName.User },
            new() { Id = "preferences", Label = "Preferences", Symbol = SymbolIconName.Preferences },
            new() { Id = "sign-out", Label = "Sign out", Symbol = SymbolIconName.SignOut, Destructive = true, SeparatorBefore = true }
        ];

        /// <summary>
        /// Gets the local project choices used by the project-switcher examples.
        /// </summary>
        private IReadOnlyList<ProjectSwitcherItem> ProjectItems { get; } =
        [
            new() { Id = "orbital", Name = "Orbital Platform", Description = "Systems engineering", Initial = "O" },
            new() { Id = "lunar", Name = "Lunar Habitat", Description = "Concept development", Initial = "L" },
            new() { Id = "payload", Name = "Payload Study", Description = "Read-only archive", Initial = "P", Disabled = true },
            new() { Id = "deep-space", Name = "Deep-space exploration architecture workspace", Description = "Long-name truncation example", Initial = "D" }
        ];

        /// <summary>
        /// Gets the representative destinations used only by the navigation-rail preview.
        /// </summary>
        private IReadOnlyList<NavigationRailItem> NavigationRailPreviewItems { get; } =
        [
            new() { Id = "overview", Label = "Overview", IconName = "layout-dashboard", GroupKey = "model" },
            new() { Id = "structure", Label = "Structure", IconName = "boxes", GroupKey = "model" },
            new() { Id = "views", Label = "Views", IconName = "panels-top-left", GroupKey = "analysis" },
            new() { Id = "relationships", Label = "Relationships", IconName = "git-compare-arrows", GroupKey = "analysis" },
            new() { Id = "activity", Label = "Activity", IconName = "history", GroupKey = "workspace" },
            new() { Id = "settings", Label = "Settings", IconName = "settings", GroupKey = "workspace" }
        ];

        /// <summary>
        /// Gets the deterministic element names used to demonstrate left-panel scrolling.
        /// </summary>
        private IReadOnlyList<string> WorkspaceNavigationItems { get; } =
        [
            "Vehicle",
            "Mission",
            "Payload",
            "Thermal control",
            "Power distribution",
            "Communications",
            "Attitude control",
            "Structures",
            "Propulsion",
            "Flight software",
            "Ground segment",
            "Interfaces",
            "Requirements",
            "Verification",
            "Allocations",
            "Views"
        ];

        /// <summary>
        /// Gets the deterministic property labels used to demonstrate right-panel scrolling.
        /// </summary>
        private IReadOnlyList<string> WorkspacePropertyItems { get; } =
        [
            "Name",
            "Identifier",
            "Definition",
            "Owner",
            "Lifecycle",
            "Multiplicity",
            "Direction",
            "Type",
            "Documentation",
            "Constraints",
            "Relationships",
            "Modified"
        ];

        /// <summary>
        /// Gets or sets the interactive search value.
        /// </summary>
        private string SearchValue { get; set; } = "architecture";

        /// <summary>
        /// Gets or sets the controlled value of the primary shortcut-search example.
        /// </summary>
        private string PrimaryShortcutSearchValue { get; set; } = "architecture";

        /// <summary>
        /// Gets or sets the controlled value of the newest shortcut-search example.
        /// </summary>
        private string SecondaryShortcutSearchValue { get; set; } = "interfaces";

        /// <summary>
        /// Gets or sets a value indicating whether the newest shortcut registration is rendered.
        /// </summary>
        private bool SecondaryShortcutSearchVisible { get; set; } = true;

        /// <summary>
        /// Gets or sets the interactive text-input value.
        /// </summary>
        private string TextInputValue { get; set; } = "Thermal subsystem";

        /// <summary>
        /// Gets or sets the interactive select value.
        /// </summary>
        private string SelectValue { get; set; } = "review";

        /// <summary>
        /// Gets or sets the second independent interactive select value.
        /// </summary>
        private string SecondarySelectValue { get; set; } = "preparation";

        /// <summary>
        /// Gets or sets the interactive text-area value.
        /// </summary>
        private string TextAreaValue { get; set; } = "Document the interface assumptions for the next review.";

        /// <summary>
        /// Gets or sets the interactive checkbox state.
        /// </summary>
        private bool CheckboxChecked { get; set; } = true;

        /// <summary>
        /// Gets or sets the interactive toggle state.
        /// </summary>
        private bool ToggleChecked { get; set; }

        /// <summary>
        /// Gets or sets the selected tab value.
        /// </summary>
        private string ActiveTabValue { get; set; } = "overview";

        /// <summary>
        /// Gets or sets the selected manual review tab value.
        /// </summary>
        private string ActiveReviewTabValue { get; set; } = "summary";

        /// <summary>
        /// Gets or sets the latest selected breadcrumb label.
        /// </summary>
        private string LastBreadcrumbSelection { get; set; } = "None";

        /// <summary>
        /// Gets or sets the latest selected menu action.
        /// </summary>
        private string LastMenuAction { get; set; } = "None";

        /// <summary>
        /// Gets or sets the number of standalone menu selections delivered to the page.
        /// </summary>
        private int ActionMenuSelectionCount { get; set; }

        /// <summary>
        /// Gets or sets the latest split-button action.
        /// </summary>
        private string LastSplitButtonAction { get; set; } = "None";

        /// <summary>
        /// Gets or sets the latest selected user-menu action.
        /// </summary>
        private string LastUserMenuAction { get; set; } = "None";

        /// <summary>
        /// Gets or sets the selected project of the first switcher.
        /// </summary>
        private string PrimaryProjectId { get; set; } = "orbital";

        /// <summary>
        /// Gets or sets the selected project of the second switcher.
        /// </summary>
        private string SecondaryProjectId { get; set; } = "lunar";

        /// <summary>
        /// Gets or sets the latest selected project label.
        /// </summary>
        private string LastProjectSelection { get; set; } = "Orbital Platform";

        /// <summary>
        /// Gets or sets a value indicating whether the modal example is open.
        /// </summary>
        private bool ModalOpen { get; set; }

        /// <summary>
        /// Gets or sets the compact-modal invoking control.
        /// </summary>
        private ElementReference CompactModalTrigger { get; set; }

        /// <summary>
        /// Gets or sets the wide-modal invoking control.
        /// </summary>
        private ElementReference WideModalTrigger { get; set; }

        /// <summary>
        /// Gets or sets the focus target captured for the active modal example.
        /// </summary>
        private ElementReference? ModalFocusReturnTarget { get; set; }

        /// <summary>
        /// Gets or sets the size used by the active modal example.
        /// </summary>
        private ModalSize ActiveModalSize { get; set; } = ModalSize.Small;

        /// <summary>
        /// Gets or sets the latest modal result.
        /// </summary>
        private string LastModalAction { get; set; } = "Not opened";

        /// <summary>
        /// Gets or sets the number of close-state requests observed for the active modal cycle.
        /// </summary>
        private int ModalStateChangeCount { get; set; }

        /// <summary>
        /// Gets or sets the number of completed close callbacks observed for the active modal cycle.
        /// </summary>
        private int ModalCloseCallbackCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the confirmation example is open.
        /// </summary>
        private bool ConfirmDialogOpen { get; set; }

        /// <summary>
        /// Gets or sets the default confirmation invoking control.
        /// </summary>
        private ElementReference DefaultConfirmDialogTrigger { get; set; }

        /// <summary>
        /// Gets or sets the warning confirmation invoking control.
        /// </summary>
        private ElementReference WarningConfirmDialogTrigger { get; set; }

        /// <summary>
        /// Gets or sets the danger confirmation invoking control.
        /// </summary>
        private ElementReference DangerConfirmDialogTrigger { get; set; }

        /// <summary>
        /// Gets or sets the loading confirmation invoking control.
        /// </summary>
        private ElementReference LoadingConfirmDialogTrigger { get; set; }

        /// <summary>
        /// Gets or sets the focus target captured for the active confirmation example.
        /// </summary>
        private ElementReference? ConfirmDialogFocusReturnTarget { get; set; }

        /// <summary>
        /// Gets or sets the active confirmation-dialog variant.
        /// </summary>
        private ConfirmDialogVariant ActiveConfirmDialogVariant { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the active confirmation is externally loading.
        /// </summary>
        private bool ConfirmDialogIsConfirming { get; set; }

        /// <summary>
        /// Gets or sets the latest confirmation result.
        /// </summary>
        private string LastConfirmationAction { get; set; } = "None";

        /// <summary>
        /// Gets or sets the search value used by the workspace header example.
        /// </summary>
        private string WorkspaceSearchValue { get; set; } = "thermal";

        /// <summary>
        /// Gets or sets the controlled project used by the workspace header example.
        /// </summary>
        private string WorkspaceProjectId { get; set; } = "orbital";

        /// <summary>
        /// Gets or sets the controlled zoom percentage used by workspace examples.
        /// </summary>
        private double WorkspaceZoom { get; set; } = 100d;

        /// <summary>
        /// Gets or sets a value indicating whether the workspace example shows its left panel.
        /// </summary>
        private bool WorkspaceLeftPanelVisible { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the workspace example shows its right panel.
        /// </summary>
        private bool WorkspaceRightPanelVisible { get; set; } = true;

        /// <summary>
        /// Gets or sets the real workspace state used by the canonical editor-workspace preview.
        /// </summary>
        private WorkspaceEditorViewModel EditorWorkspacePreviewViewModel { get; set; }

        /// <summary>
        /// Gets or sets the Figma-derived initial proportions used by the canonical editor-workspace preview.
        /// </summary>
        private IReadOnlyDictionary<Guid, double> EditorWorkspacePreviewWeights { get; set; }

        /// <summary>
        /// Gets or sets the independent real workspace state used by the compact editor-workspace preview.
        /// </summary>
        private WorkspaceEditorViewModel CompactEditorWorkspacePreviewViewModel { get; set; }

        /// <summary>
        /// Gets or sets the page-owned ViewModel used by the navigation-rail preview.
        /// </summary>
        private NavigationRailViewModel NavigationRailPreviewViewModel { get; set; }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            this.EditorWorkspacePreviewViewModel = CreateEditorWorkspacePreviewViewModel();
            this.EditorWorkspacePreviewWeights = new Dictionary<Guid, double>
            {
                [this.EditorWorkspacePreviewViewModel.Groups[0].Id] = 300d,
                [this.EditorWorkspacePreviewViewModel.Groups[1].Id] = 320d,
                [this.EditorWorkspacePreviewViewModel.Groups[2].Id] = 868d
            };
            this.CompactEditorWorkspacePreviewViewModel = CreateEditorWorkspacePreviewViewModel();

            this.navigationRailPreviewContext = new ContextAwareService
            {
                LifecycleState = ProjectLifecycleState.Open,
                SelectedElement = null
            };

            this.NavigationRailPreviewViewModel = new NavigationRailViewModel(
                this.navigationRailPreviewContext,
                new FixedNavigationRailItemProvider(this.NavigationRailPreviewItems));

            this.NavigationRailPreviewViewModel.SelectedItem = this.NavigationRailPreviewItems.Single(item => item.Id == "structure");
            this.NavigationRailPreviewViewModel.PresentationMode = NavigationRailPresentationMode.ExpandOnHover;

            base.OnInitialized();
        }

        /// <summary>
        /// Creates deterministic, rendering-neutral editor state for an isolated Design System preview.
        /// </summary>
        /// <returns>A real workspace ViewModel containing three groups and representative tabs.</returns>
        private static WorkspaceEditorViewModel CreateEditorWorkspacePreviewViewModel()
        {
            var viewModel = new WorkspaceEditorViewModel(
                Options.Create(new WorkspaceEditorOptions
                {
                    MaximumGroupCount = 3
                }));
            var firstGroup = viewModel.Groups[0];

            _ = viewModel.TryOpenTab(firstGroup.Id, "Editor A", "generic-document", out var firstTab);
            _ = viewModel.TryOpenTab(firstGroup.Id, "Editor B", "generic-preview", out _);
            _ = viewModel.ActivateTab(firstGroup.Id, firstTab.Id);

            _ = viewModel.TryAddGroup(out var secondGroup);
            _ = viewModel.TryOpenTab(secondGroup.Id, "Editor C", "generic-document", out _);

            _ = viewModel.TryAddGroup(out var thirdGroup);
            _ = viewModel.TryOpenTab(thirdGroup.Id, "Editor D", "generic-surface", out var fourthTab);
            _ = viewModel.TryOpenTab(thirdGroup.Id, "Editor E", "generic-surface", out _);
            _ = viewModel.ActivateTab(thirdGroup.Id, fourthTab.Id);

            return viewModel;
        }

        /// <summary>
        /// Opens generic preview content in the requested canonical editor group.
        /// </summary>
        /// <param name="groupId">The target editor-group identity.</param>
        private void HandleEditorWorkspaceAddTabRequested(Guid groupId)
        {
            OpenEditorWorkspacePreviewTab(this.EditorWorkspacePreviewViewModel, groupId);
        }

        /// <summary>
        /// Opens generic preview content in the requested compact editor group.
        /// </summary>
        /// <param name="groupId">The target editor-group identity.</param>
        private void HandleCompactEditorWorkspaceAddTabRequested(Guid groupId)
        {
            OpenEditorWorkspacePreviewTab(this.CompactEditorWorkspacePreviewViewModel, groupId);
        }

        /// <summary>
        /// Supplies rendering-neutral placeholder metadata at the Design System composition boundary.
        /// </summary>
        /// <param name="viewModel">The preview workspace that owns the requested group.</param>
        /// <param name="groupId">The target editor-group identity.</param>
        private static void OpenEditorWorkspacePreviewTab(
            WorkspaceEditorViewModel viewModel,
            Guid groupId)
        {
            _ = viewModel.TryOpenTab(groupId, "Untitled editor", "generic-placeholder", out _);
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                return;
            }

            this.themeModule = await this.JsRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./Components/Pages/DesignSystem.razor.js");

            await this.ApplyThemeAsync();
        }

        /// <summary>
        /// Selects and applies a supported document-level preview theme.
        /// </summary>
        /// <param name="themeName">The supported theme name.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SetThemeAsync(string themeName)
        {
            if (!string.Equals(themeName, "light", StringComparison.Ordinal) &&
                !string.Equals(themeName, "dark", StringComparison.Ordinal))
            {
                throw new ArgumentOutOfRangeException(nameof(themeName), themeName, "Only light and dark preview themes are supported.");
            }

            this.ThemeName = themeName;
            await this.ApplyThemeAsync();
        }

        /// <summary>
        /// Applies the selected preview theme to the document root when the module is ready.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ApplyThemeAsync()
        {
            if (this.themeModule is not null)
            {
                await this.themeModule.InvokeVoidAsync(
                    "applyTheme",
                    this.themeOwnerId,
                    this.ThemeName);
            }
        }

        /// <summary>
        /// Gets the CSS classes for a theme option.
        /// </summary>
        /// <param name="themeName">The theme represented by the option.</param>
        /// <returns>The theme-option CSS class list.</returns>
        private string GetThemeButtonCssClass(string themeName)
        {
            return string.Equals(this.ThemeName, themeName, StringComparison.Ordinal)
                ? "mb-design-system__theme-button mb-design-system__theme-button--selected"
                : "mb-design-system__theme-button";
        }

        /// <summary>
        /// Gets the accessible pressed state for a theme option.
        /// </summary>
        /// <param name="themeName">The theme represented by the option.</param>
        /// <returns>True when the theme option is selected; otherwise, false.</returns>
        private string GetThemeAriaPressed(string themeName)
        {
            return string.Equals(this.ThemeName, themeName, StringComparison.Ordinal) ? "true" : "false";
        }

        /// <summary>
        /// Gets or sets the latest application-header callback result.
        /// </summary>
        private string LastWorkspaceAction { get; set; } = "None";

        /// <summary>
        /// Gets or sets the latest canvas-toolbar callback result.
        /// </summary>
        private string LastCanvasToolbarAction { get; set; } = "None";

        /// <summary>
        /// Gets or sets the parent-owned active tool used by the canvas-toolbar examples.
        /// </summary>
        private string ActiveCanvasToolbarTool { get; set; } = "Select";

        /// <summary>
        /// Gets or sets the latest zoom-controls callback result.
        /// </summary>
        private string LastZoomAction { get; set; } = "Zoom is 100%";

        /// <summary>
        /// Gets or sets the latest status-bar callback result.
        /// </summary>
        private string LastStatusAction { get; set; } = "No status action selected";

        /// <summary>
        /// Updates the interactive search value.
        /// </summary>
        /// <param name="value">The updated value.</param>
        private void HandleSearchValueChanged(string value)
        {
            this.SearchValue = value;
        }

        /// <summary>
        /// Updates the primary shortcut-search value.
        /// </summary>
        /// <param name="value">The updated value.</param>
        private void HandlePrimaryShortcutSearchValueChanged(string value)
        {
            this.PrimaryShortcutSearchValue = value;
        }

        /// <summary>
        /// Updates the newest shortcut-search value.
        /// </summary>
        /// <param name="value">The updated value.</param>
        private void HandleSecondaryShortcutSearchValueChanged(string value)
        {
            this.SecondaryShortcutSearchValue = value;
        }

        /// <summary>
        /// Adds or removes the newest search shortcut registration for lifecycle verification.
        /// </summary>
        private void ToggleSecondaryShortcutSearch()
        {
            this.SecondaryShortcutSearchVisible = !this.SecondaryShortcutSearchVisible;
        }

        /// <summary>
        /// Updates the interactive text-input value.
        /// </summary>
        /// <param name="value">The updated value.</param>
        private void HandleTextInputValueChanged(string value)
        {
            this.TextInputValue = value;
        }

        /// <summary>
        /// Updates the interactive select value.
        /// </summary>
        /// <param name="value">The selected value.</param>
        private void HandleSelectValueChanged(string value)
        {
            this.SelectValue = value;
        }

        /// <summary>
        /// Updates the second independent select value.
        /// </summary>
        /// <param name="value">The selected value.</param>
        private void HandleSecondarySelectValueChanged(string value)
        {
            this.SecondarySelectValue = value;
        }

        /// <summary>
        /// Applies an external controlled-value update to the primary Select example.
        /// </summary>
        private void SetPrimarySelectToOpen()
        {
            this.SelectValue = "open";
        }

        /// <summary>
        /// Restores the primary Select example to its deterministic initial value.
        /// </summary>
        private void ResetPrimarySelect()
        {
            this.SelectValue = "review";
        }

        /// <summary>
        /// Updates the interactive text-area value.
        /// </summary>
        /// <param name="value">The updated value.</param>
        private void HandleTextAreaValueChanged(string value)
        {
            this.TextAreaValue = value;
        }

        /// <summary>
        /// Updates the interactive checkbox state.
        /// </summary>
        /// <param name="isChecked">The updated checked state.</param>
        private void HandleCheckboxChanged(bool isChecked)
        {
            this.CheckboxChecked = isChecked;
        }

        /// <summary>
        /// Updates the interactive toggle state.
        /// </summary>
        /// <param name="isChecked">The updated checked state.</param>
        private void HandleToggleChanged(bool isChecked)
        {
            this.ToggleChecked = isChecked;
        }

        /// <summary>
        /// Updates the workspace-header search value.
        /// </summary>
        /// <param name="value">The updated local search value.</param>
        private void HandleWorkspaceSearchValueChanged(string value)
        {
            this.WorkspaceSearchValue = value;
        }

        /// <summary>
        /// Updates the workspace-header project selection and result text.
        /// </summary>
        /// <param name="projectId">The selected local project identifier.</param>
        private void HandleWorkspaceProjectChanged(string projectId)
        {
            this.WorkspaceProjectId = projectId;
            this.LastWorkspaceAction = $"Selected {this.GetProjectName(projectId)}";
        }

        /// <summary>
        /// Records the workspace-header share action.
        /// </summary>
        private void HandleShareWorkspace()
        {
            this.LastWorkspaceAction = "Share requested";
        }

        /// <summary>
        /// Records the workspace-header validation action.
        /// </summary>
        private void HandleValidateWorkspace()
        {
            this.LastWorkspaceAction = "Validation requested";
        }

        /// <summary>
        /// Records the compact-header action.
        /// </summary>
        private void HandleCompactHeaderAction()
        {
            this.LastWorkspaceAction = "Compact action requested";
        }

        /// <summary>
        /// Records the selected canvas-toolbar action.
        /// </summary>
        /// <param name="action">The local tool label.</param>
        private void HandleCanvasToolbarAction(string action)
        {
            this.LastCanvasToolbarAction = action;
        }

        /// <summary>
        /// Updates the parent-owned active canvas tool and records the callback result.
        /// </summary>
        /// <param name="tool">The selected local tool label.</param>
        private void HandleCanvasToolbarToolChanged(string tool)
        {
            this.ActiveCanvasToolbarTool = tool;
            this.LastCanvasToolbarAction = tool;
        }

        /// <summary>
        /// Gets the presentation classes for a selectable canvas-toolbar action.
        /// </summary>
        /// <param name="tool">The local tool label.</param>
        /// <returns>The canvas-toolbar action classes.</returns>
        private string GetCanvasToolbarToolCssClass(string tool)
        {
            return this.IsCanvasToolbarToolActive(tool)
                ? "mb-design-system__canvas-toolbar-action mb-design-system__canvas-toolbar-action--active"
                : "mb-design-system__canvas-toolbar-action";
        }

        /// <summary>
        /// Gets the pressed-state value for a selectable canvas-toolbar action.
        /// </summary>
        /// <param name="tool">The local tool label.</param>
        /// <returns>True when the tool is active; otherwise, false.</returns>
        private string GetCanvasToolbarToolAriaPressed(string tool)
        {
            return this.IsCanvasToolbarToolActive(tool) ? "true" : "false";
        }

        /// <summary>
        /// Gets a value indicating whether the provided canvas tool is active.
        /// </summary>
        /// <param name="tool">The local tool label.</param>
        /// <returns>True when the tool is active; otherwise, false.</returns>
        private bool IsCanvasToolbarToolActive(string tool)
        {
            return string.Equals(this.ActiveCanvasToolbarTool, tool, StringComparison.Ordinal);
        }

        /// <summary>
        /// Updates the parent-owned workspace zoom percentage.
        /// </summary>
        /// <param name="zoom">The requested zoom percentage.</param>
        private void HandleWorkspaceZoomChanged(double zoom)
        {
            this.WorkspaceZoom = zoom;
            this.LastZoomAction = $"Zoom changed to {zoom.ToString("0.#", CultureInfo.InvariantCulture)}%";
        }

        /// <summary>
        /// Resets the local workspace zoom percentage.
        /// </summary>
        private void HandleResetWorkspaceZoom()
        {
            this.WorkspaceZoom = 100d;
            this.LastZoomAction = "Zoom reset to 100%";
        }

        /// <summary>
        /// Applies a deterministic fit-to-view zoom percentage.
        /// </summary>
        private void HandleFitWorkspaceToView()
        {
            this.WorkspaceZoom = 75d;
            this.LastZoomAction = "Fit to view at 75%";
        }

        /// <summary>
        /// Records activation of the status-bar detail action.
        /// </summary>
        private void HandleStatusDetails()
        {
            this.LastStatusAction = "Status details requested";
        }

        /// <summary>
        /// Toggles the local workspace left-panel visibility.
        /// </summary>
        private void ToggleWorkspaceLeftPanel()
        {
            this.WorkspaceLeftPanelVisible = !this.WorkspaceLeftPanelVisible;
        }

        /// <summary>
        /// Toggles the local workspace right-panel visibility.
        /// </summary>
        private void ToggleWorkspaceRightPanel()
        {
            this.WorkspaceRightPanelVisible = !this.WorkspaceRightPanelVisible;
        }

        /// <summary>
        /// Updates the selected tab.
        /// </summary>
        /// <param name="value">The selected tab value.</param>
        private void HandleTabChanged(string value)
        {
            this.ActiveTabValue = value;
        }

        /// <summary>
        /// Updates the selected manual review tab.
        /// </summary>
        /// <param name="value">The selected review tab value.</param>
        private void HandleReviewTabChanged(string value)
        {
            this.ActiveReviewTabValue = value;
        }

        /// <summary>
        /// Gets an explicit ARIA selected state for a controlled direct Blueprint tab.
        /// </summary>
        /// <param name="activeValue">The active controlled tab value.</param>
        /// <param name="tabValue">The candidate tab value.</param>
        /// <returns>The lowercase ARIA Boolean value.</returns>
        private static string GetTabAriaSelected(string activeValue, string tabValue)
        {
            return string.Equals(activeValue, tabValue, StringComparison.Ordinal) ? "true" : "false";
        }

        /// <summary>
        /// Records the selected breadcrumb.
        /// </summary>
        /// <param name="label">The selected breadcrumb label.</param>
        private void HandleBreadcrumbSelected(string label)
        {
            this.LastBreadcrumbSelection = label;
        }

        /// <summary>
        /// Records an action-menu selection.
        /// </summary>
        /// <param name="item">The selected action.</param>
        private void HandleActionMenuItemSelected(ActionMenuItem item)
        {
            this.LastMenuAction = item.Label;
            this.ActionMenuSelectionCount++;
        }

        /// <summary>
        /// Records the split-button primary action.
        /// </summary>
        private void HandleSplitButtonPrimaryAction()
        {
            this.LastSplitButtonAction = "Save";
        }

        /// <summary>
        /// Records a split-button secondary action.
        /// </summary>
        /// <param name="item">The selected secondary action.</param>
        private void HandleSplitButtonItemSelected(ActionMenuItem item)
        {
            this.LastSplitButtonAction = item.Label;
        }

        /// <summary>
        /// Records a user-menu action.
        /// </summary>
        /// <param name="item">The selected account action.</param>
        private void HandleUserMenuItemSelected(ActionMenuItem item)
        {
            this.LastUserMenuAction = item.Label;
        }

        /// <summary>
        /// Updates the first project switcher and records the selection.
        /// </summary>
        /// <param name="projectId">The selected project identifier.</param>
        private void HandlePrimaryProjectChanged(string projectId)
        {
            this.PrimaryProjectId = projectId;
            this.LastProjectSelection = this.GetProjectName(projectId);
        }

        /// <summary>
        /// Updates the second project switcher and records the selection.
        /// </summary>
        /// <param name="projectId">The selected project identifier.</param>
        private void HandleSecondaryProjectChanged(string projectId)
        {
            this.SecondaryProjectId = projectId;
            this.LastProjectSelection = this.GetProjectName(projectId);
        }

        /// <summary>
        /// Gets a project name from the local showcase data.
        /// </summary>
        /// <param name="projectId">The project identifier.</param>
        /// <returns>The matching project name.</returns>
        private string GetProjectName(string projectId)
        {
            return this.ProjectItems.First(item => string.Equals(item.Id, projectId, StringComparison.Ordinal)).Name;
        }

        /// <summary>
        /// Opens the modal example with the requested size.
        /// </summary>
        /// <param name="size">The modal size.</param>
        /// <param name="focusReturnTarget">The invoking control that receives focus after closing.</param>
        private void OpenModal(ModalSize size, ElementReference focusReturnTarget)
        {
            this.ActiveModalSize = size;
            this.ModalFocusReturnTarget = focusReturnTarget;
            this.ModalStateChangeCount = 0;
            this.ModalCloseCallbackCount = 0;
            this.ModalOpen = true;
            this.LastModalAction = $"Opened {size.ToString().ToLowerInvariant()} modal";
        }

        /// <summary>
        /// Updates the modal open state.
        /// </summary>
        /// <param name="isOpen">The updated open state.</param>
        private void HandleModalOpenChanged(bool isOpen)
        {
            this.ModalOpen = isOpen;

            if (!isOpen)
            {
                this.ModalStateChangeCount++;
                this.LastModalAction = "Closed modal";
            }
        }

        /// <summary>
        /// Records completion of a dialog-requested close cycle.
        /// </summary>
        private void HandleModalClosed()
        {
            this.ModalCloseCallbackCount++;
        }

        /// <summary>
        /// Closes the active modal from its footer action.
        /// </summary>
        private void CloseModal()
        {
            this.HandleModalOpenChanged(false);
        }

        /// <summary>
        /// Opens a confirmation-dialog variant.
        /// </summary>
        /// <param name="variant">The dialog variant.</param>
        /// <param name="focusReturnTarget">The invoking control that receives focus after closing.</param>
        /// <param name="isConfirming">A value indicating whether the actions render in their externally controlled loading state.</param>
        private void OpenConfirmDialog(
            ConfirmDialogVariant variant,
            ElementReference focusReturnTarget,
            bool isConfirming = false)
        {
            this.ActiveConfirmDialogVariant = variant;
            this.ConfirmDialogFocusReturnTarget = focusReturnTarget;
            this.ConfirmDialogIsConfirming = isConfirming;
            this.ConfirmDialogOpen = true;
            this.LastConfirmationAction = $"Opened {variant.ToString().ToLowerInvariant()} confirmation";
        }

        /// <summary>
        /// Updates the confirmation-dialog open state.
        /// </summary>
        /// <param name="isOpen">The updated open state.</param>
        private void HandleConfirmDialogOpenChanged(bool isOpen)
        {
            this.ConfirmDialogOpen = isOpen;

            if (!isOpen)
            {
                this.ConfirmDialogIsConfirming = false;
            }
        }

        /// <summary>
        /// Records confirmation of the active dialog.
        /// </summary>
        private void HandleConfirmed()
        {
            this.LastConfirmationAction = $"Confirmed {this.ActiveConfirmDialogVariant.ToString().ToLowerInvariant()} action";
        }

        /// <summary>
        /// Records cancellation of the active dialog.
        /// </summary>
        private void HandleCancelled()
        {
            this.LastConfirmationAction = $"Cancelled {this.ActiveConfirmDialogVariant.ToString().ToLowerInvariant()} action";
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (this.NavigationRailPreviewViewModel is not null)
            {
                this.NavigationRailPreviewViewModel.Dispose();
            }

            this.EditorWorkspacePreviewViewModel?.Dispose();
            this.CompactEditorWorkspacePreviewViewModel?.Dispose();

            var module = this.themeModule;
            this.themeModule = null;

            if (module is not null)
            {
                try
                {
                    await module.InvokeVoidAsync("releaseTheme", this.themeOwnerId);
                    await module.DisposeAsync();
                }
                catch (JSDisconnectedException)
                {
                    // The circuit has already ended, so the browser no longer accepts cleanup calls.
                }
                catch (ObjectDisposedException)
                {
                    // The renderer disposed the JavaScript module before component cleanup completed.
                }
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Supplies the fixed representative destination inventory owned by this preview.
        /// </summary>
        private sealed class FixedNavigationRailItemProvider : INavigationRailItemProvider
        {
            /// <summary>
            /// The destinations returned for every preview context.
            /// </summary>
            private readonly IReadOnlyList<NavigationRailItem> navigationItems;

            /// <summary>
            /// Initializes a new instance of the <see cref="FixedNavigationRailItemProvider" /> class.
            /// </summary>
            /// <param name="navigationItems">The preview destinations in display order.</param>
            public FixedNavigationRailItemProvider(IReadOnlyList<NavigationRailItem> navigationItems)
            {
                ArgumentNullException.ThrowIfNull(navigationItems);

                this.navigationItems = navigationItems;
            }

            /// <inheritdoc />
            public IReadOnlyList<NavigationRailItem> GetNavigationItems(
                ProjectLifecycleState lifecycleState,
                IElement selectedElement)
            {
                return this.navigationItems;
            }
        }
    }
}
