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
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Provides a development-only composition surface for the reusable Bloom component library.
    /// </summary>
    public partial class DesignSystem : ComponentBase
    {
        /// <summary>
        /// Gets the local select options used by the form examples.
        /// </summary>
        private IReadOnlyList<SelectInputOption> SelectOptions { get; } =
        [
            new() { Value = "preparation", Label = "Preparation" },
            new() { Value = "open", Label = "Open" },
            new() { Value = "review", Label = "In review" },
            new() { Value = "archived", Label = "Archived", Disabled = true }
        ];

        /// <summary>
        /// Gets the local tab items used by the tabs example.
        /// </summary>
        private IReadOnlyList<TabItem> TabItems { get; } =
        [
            new() { Value = "overview", Label = "Overview" },
            new() { Value = "properties", Label = "Properties" },
            new() { Value = "relationships", Label = "Relationships" },
            new() { Value = "history", Label = "History", Disabled = true }
        ];

        /// <summary>
        /// Gets the local breadcrumb items used by the navigation example.
        /// </summary>
        private IReadOnlyList<BreadcrumbItem> BreadcrumbItems { get; } =
        [
            new() { Id = "workspace", Label = "Workspace", Target = "workspace" },
            new() { Id = "projects", Label = "Projects", Target = "projects" },
            new() { Id = "restricted", Label = "Restricted", Disabled = true },
            new() { Id = "architecture", Label = "Architecture", IsCurrent = true }
        ];

        /// <summary>
        /// Gets the local actions used by independent action-menu examples.
        /// </summary>
        private IReadOnlyList<ActionMenuItem> ActionMenuItems { get; } =
        [
            new() { Id = "open", Label = "Open details", Description = "Inspect the selected element", Icon = "O" },
            new() { Id = "duplicate", Label = "Duplicate", Description = "Create a local copy", Icon = "D" },
            new() { Id = "publish", Label = "Publish", Disabled = true },
            new() { Id = "delete", Label = "Delete", Destructive = true, SeparatorBefore = true }
        ];

        /// <summary>
        /// Gets the local secondary actions used by the split-button example.
        /// </summary>
        private IReadOnlyList<ActionMenuItem> SplitButtonItems { get; } =
        [
            new() { Id = "save-draft", Label = "Save as draft" },
            new() { Id = "save-copy", Label = "Save a copy" }
        ];

        /// <summary>
        /// Gets the local account actions used by the user-menu examples.
        /// </summary>
        private IReadOnlyList<ActionMenuItem> UserMenuItems { get; } =
        [
            new() { Id = "profile", Label = "Profile", Description = "Manage local presentation settings" },
            new() { Id = "preferences", Label = "Preferences" },
            new() { Id = "sign-out", Label = "Sign out", Destructive = true, SeparatorBefore = true }
        ];

        /// <summary>
        /// Gets the local project choices used by the project-switcher examples.
        /// </summary>
        private IReadOnlyList<ProjectSwitcherItem> ProjectItems { get; } =
        [
            new() { Id = "orbital", Name = "Orbital Platform", Description = "Systems engineering", Initial = "O" },
            new() { Id = "lunar", Name = "Lunar Habitat", Description = "Concept development", Initial = "L" },
            new() { Id = "payload", Name = "Payload Study", Description = "Read-only archive", Initial = "P", Disabled = true }
        ];

        /// <summary>
        /// Gets the standalone notification examples that remain visible until dismissed.
        /// </summary>
        private List<ToastNotification> StandaloneNotifications { get; } =
        [
            new() { Id = "standalone-info", Title = "Information", Message = "A neutral update is available.", Variant = ToastNotificationVariant.Info },
            new() { Id = "standalone-success", Title = "Saved", Message = "The local example was saved.", Variant = ToastNotificationVariant.Success },
            new() { Id = "standalone-warning", Title = "Review needed", Message = "Check the pending values.", Variant = ToastNotificationVariant.Warning },
            new() { Id = "standalone-danger", Title = "Connection lost", Message = "This non-dismissible state remains visible.", Variant = ToastNotificationVariant.Danger, IsDismissible = false }
        ];

        /// <summary>
        /// Gets the notifications currently displayed by the toast-container example.
        /// </summary>
        private List<ToastNotification> ToastNotifications { get; } = [];

        /// <summary>
        /// Gets or sets the interactive search value.
        /// </summary>
        private string SearchValue { get; set; } = "architecture";

        /// <summary>
        /// Gets or sets the interactive text-input value.
        /// </summary>
        private string TextInputValue { get; set; } = "Thermal subsystem";

        /// <summary>
        /// Gets or sets the interactive select value.
        /// </summary>
        private string SelectValue { get; set; } = "review";

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
        /// Gets or sets the latest selected breadcrumb label.
        /// </summary>
        private string LastBreadcrumbSelection { get; set; } = "None";

        /// <summary>
        /// Gets or sets the open state of the first standalone action menu.
        /// </summary>
        private bool PrimaryActionMenuOpen { get; set; }

        /// <summary>
        /// Gets or sets the open state of the second standalone action menu.
        /// </summary>
        private bool SecondaryActionMenuOpen { get; set; }

        /// <summary>
        /// Gets or sets the latest selected menu action.
        /// </summary>
        private string LastMenuAction { get; set; } = "None";

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
        /// Gets or sets the size used by the active modal example.
        /// </summary>
        private ModalSize ActiveModalSize { get; set; } = ModalSize.Small;

        /// <summary>
        /// Gets or sets the latest modal result.
        /// </summary>
        private string LastModalAction { get; set; } = "Not opened";

        /// <summary>
        /// Gets or sets a value indicating whether the confirmation example is open.
        /// </summary>
        private bool ConfirmDialogOpen { get; set; }

        /// <summary>
        /// Gets or sets the active confirmation-dialog variant.
        /// </summary>
        private ConfirmDialogVariant ActiveConfirmDialogVariant { get; set; }

        /// <summary>
        /// Gets or sets the latest confirmation result.
        /// </summary>
        private string LastConfirmationAction { get; set; } = "None";

        /// <summary>
        /// Gets or sets the next deterministic toast sequence number.
        /// </summary>
        private int NextToastNumber { get; set; } = 1;

        /// <summary>
        /// Updates the interactive search value.
        /// </summary>
        /// <param name="value">The updated value.</param>
        private void HandleSearchValueChanged(string value)
        {
            this.SearchValue = value;
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
        /// Updates the selected tab.
        /// </summary>
        /// <param name="value">The selected tab value.</param>
        private void HandleTabChanged(string value)
        {
            this.ActiveTabValue = value;
        }

        /// <summary>
        /// Records the selected breadcrumb.
        /// </summary>
        /// <param name="item">The selected breadcrumb item.</param>
        private void HandleBreadcrumbSelected(BreadcrumbItem item)
        {
            this.LastBreadcrumbSelection = item.Label;
        }

        /// <summary>
        /// Updates the open state of the primary action menu.
        /// </summary>
        /// <param name="isOpen">The updated open state.</param>
        private void HandlePrimaryActionMenuOpenChanged(bool isOpen)
        {
            this.PrimaryActionMenuOpen = isOpen;
        }

        /// <summary>
        /// Updates the open state of the secondary action menu.
        /// </summary>
        /// <param name="isOpen">The updated open state.</param>
        private void HandleSecondaryActionMenuOpenChanged(bool isOpen)
        {
            this.SecondaryActionMenuOpen = isOpen;
        }

        /// <summary>
        /// Records an action-menu selection.
        /// </summary>
        /// <param name="item">The selected action.</param>
        private void HandleActionMenuItemSelected(ActionMenuItem item)
        {
            this.LastMenuAction = item.Label;
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
        private void OpenModal(ModalSize size)
        {
            this.ActiveModalSize = size;
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
                this.LastModalAction = "Closed modal";
            }
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
        private void OpenConfirmDialog(ConfirmDialogVariant variant)
        {
            this.ActiveConfirmDialogVariant = variant;
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

        /// <summary>
        /// Dismisses a standalone notification example.
        /// </summary>
        /// <param name="notificationId">The notification identifier.</param>
        private void DismissStandaloneNotification(string notificationId)
        {
            this.StandaloneNotifications.RemoveAll(notification =>
                string.Equals(notification.Id, notificationId, StringComparison.Ordinal));
        }

        /// <summary>
        /// Dismisses a notification from the toast-container example.
        /// </summary>
        /// <param name="notificationId">The notification identifier.</param>
        private void DismissToastNotification(string notificationId)
        {
            this.ToastNotifications.RemoveAll(notification =>
                string.Equals(notification.Id, notificationId, StringComparison.Ordinal));
        }

        /// <summary>
        /// Adds a deterministic local notification to the toast-container example.
        /// </summary>
        private void AddToastNotification()
        {
            var sequenceNumber = this.NextToastNumber++;

            this.ToastNotifications.Add(new ToastNotification
            {
                Id = $"container-sample-{sequenceNumber}",
                Title = $"Sample notification {sequenceNumber}",
                Message = "Added from the local showcase controls.",
                Variant = sequenceNumber % 2 == 0
                    ? ToastNotificationVariant.Warning
                    : ToastNotificationVariant.Info
            });
        }

        /// <summary>
        /// Restores the initial toast-container notifications.
        /// </summary>
        private void ResetToastNotifications()
        {
            this.ToastNotifications.Clear();
            this.ToastNotifications.AddRange(
            [
                new() { Id = "container-sync", Title = "Model synchronized", Message = "Local changes are up to date.", Variant = ToastNotificationVariant.Success },
                new() { Id = "container-review", Title = "Review ready", Message = "Two items are ready for inspection.", Variant = ToastNotificationVariant.Info }
            ]);

            this.NextToastNumber = 3;
        }
    }
}
