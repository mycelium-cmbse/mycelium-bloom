// ------------------------------------------------------------------------------------------------
// <copyright file="UserMenu.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Molecules.UserMenu
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;
    using Microsoft.JSInterop;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Reusable Bloom user menu for app header account actions.
    /// </summary>
    public partial class UserMenu : ComponentBase, IAsyncDisposable
    {
        /// <summary>
        /// The base CSS class applied to user menu items.
        /// </summary>
        private const string ItemCssClass = "mb-user-menu__item";

        /// <summary>
        /// Gets or sets the JavaScript runtime.
        /// </summary>
        [Inject]
        private IJSRuntime JsRuntime { get; set; }

        /// <summary>
        /// Gets or sets the JavaScript module used to prevent handled navigation key defaults.
        /// </summary>
        private IJSObjectReference KeyboardNavigationModule { get; set; }

        /// <summary>
        /// Gets or sets the root element reference.
        /// </summary>
        private ElementReference RootElement { get; set; }

        /// <summary>
        /// Gets or sets the user display name.
        /// </summary>
        [Parameter]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user initials shown inside the avatar.
        /// </summary>
        [Parameter]
        public string UserInitials { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user email address.
        /// </summary>
        [Parameter]
        public string UserEmail { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user role.
        /// </summary>
        [Parameter]
        public string UserRole { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user avatar background color.
        /// </summary>
        [Parameter]
        public string UserColor { get; set; } = "var(--mb-color-collaborator-c08, #3b82f6)";

        /// <summary>
        /// Gets or sets the available menu items.
        /// </summary>
        [Parameter]
        public IReadOnlyList<ActionMenuItem> Items { get; set; } = [];

        /// <summary>
        /// Gets or sets the callback invoked when an enabled menu item is selected.
        /// </summary>
        [Parameter]
        public EventCallback<ActionMenuItem> ItemSelected { get; set; }

        /// <summary>
        /// Gets or sets whether the trigger should show the user name and metadata.
        /// </summary>
        [Parameter]
        public bool ShowUserText { get; set; } = true;

        /// <summary>
        /// Gets or sets additional CSS classes.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets unmatched attributes passed to the user menu wrapper.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets whether the dropdown menu is currently open.
        /// </summary>
        private bool IsOpen { get; set; }

        /// <summary>
        /// Gets or sets whether focus is currently inside the user menu.
        /// </summary>
        private bool HasFocusWithin { get; set; }

        /// <summary>
        /// Gets or sets the item index that should receive focus after rendering.
        /// </summary>
        private int? PendingFocusItemIndex { get; set; }

        /// <summary>
        /// Gets or sets whether the trigger should receive focus after rendering.
        /// </summary>
        private bool ShouldFocusTrigger { get; set; }

        /// <summary>
        /// Gets or sets the trigger element reference.
        /// </summary>
        private ElementReference TriggerElement { get; set; }

        /// <summary>
        /// Gets or sets the menu item element references.
        /// </summary>
        private ElementReference[] ItemElements { get; set; } = [];

        /// <summary>
        /// Releases asynchronous resources used by the user menu.
        /// </summary>
        /// <returns>A value task representing the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await this.DisposeAsyncCore();

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Keeps the item reference collection aligned with the available menu items.
        /// </summary>
        protected override void OnParametersSet()
        {
            if (this.ItemElements.Length != this.Items.Count)
            {
                this.ItemElements = new ElementReference[this.Items.Count];
            }
        }

        /// <summary>
        /// Focuses pending keyboard targets after rendering.
        /// </summary>
        /// <param name="firstRender">A value indicating whether this is the first render.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                this.KeyboardNavigationModule = await KeyboardNavigation.RegisterNavigationKeyPreventionAsync(this.JsRuntime, this.RootElement);
            }

            if (this.PendingFocusItemIndex is { } itemIndex && itemIndex >= 0 && itemIndex < this.ItemElements.Length)
            {
                this.PendingFocusItemIndex = null;

                await this.ItemElements[itemIndex].FocusAsync(true);
            }

            if (this.ShouldFocusTrigger)
            {
                this.ShouldFocusTrigger = false;

                await this.TriggerElement.FocusAsync(true);
            }
        }

        /// <summary>
        /// Gets the final CSS class list applied to the user menu wrapper.
        /// </summary>
        /// <returns>The user menu CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-user-menu",
                this.Class);

            return cssClass;
        }

        /// <summary>
        /// Gets the metadata shown in the compact trigger.
        /// </summary>
        /// <returns>The user role when available; otherwise, the user email address.</returns>
        private string GetUserMeta()
        {
            var userMeta = !string.IsNullOrWhiteSpace(this.UserRole)
                ? this.UserRole
                : this.UserEmail;

            return userMeta;
        }

        /// <summary>
        /// Gets the avatar title.
        /// </summary>
        /// <returns>The avatar title.</returns>
        private string GetAvatarTitle()
        {
            var title = !string.IsNullOrWhiteSpace(this.UserName)
                ? this.UserName
                : this.UserEmail;

            return title;
        }

        /// <summary>
        /// Gets the trigger title.
        /// </summary>
        /// <returns>The trigger title.</returns>
        private string GetTriggerTitle()
        {
            var title = !string.IsNullOrWhiteSpace(this.UserName)
                ? $"Open user menu for {this.UserName}"
                : "Open user menu";

            return title;
        }

        /// <summary>
        /// Gets the trigger aria-label.
        /// </summary>
        /// <returns>The trigger aria-label.</returns>
        private string GetTriggerAriaLabel()
        {
            var ariaLabel = !string.IsNullOrWhiteSpace(this.UserName)
                ? $"Open user menu for {this.UserName}"
                : "Open user menu";

            return ariaLabel;
        }

        /// <summary>
        /// Toggles whether the menu is open.
        /// </summary>
        private void ToggleMenu()
        {
            if (this.IsOpen)
            {
                this.CloseMenu();
            }
            else
            {
                this.OpenMenu();
            }
        }

        /// <summary>
        /// Selects the provided menu item when it is enabled.
        /// </summary>
        /// <param name="item">The selected menu item.</param>
        private async Task SelectItemAsync(ActionMenuItem item)
        {
            if (!item.Disabled)
            {
                this.CloseMenu();

                await this.ItemSelected.InvokeAsync(item);
            }
        }

        /// <summary>
        /// Handles trigger keyboard shortcuts.
        /// </summary>
        /// <param name="args">The keyboard event arguments.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private Task HandleTriggerKeyDownAsync(KeyboardEventArgs args)
        {
            switch (args.Key)
            {
                case "ArrowDown":
                case "Down":
                    this.OpenMenu(KeyboardNavigation.GetFirstEnabledIndex(this.Items, ActionMenuItemHelper.IsEnabled));
                    break;
                case "ArrowUp":
                case "Up":
                    this.OpenMenu(KeyboardNavigation.GetLastEnabledIndex(this.Items, ActionMenuItemHelper.IsEnabled));
                    break;
                case "Escape":
                    this.CloseMenu();
                    break;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles menu item keyboard navigation.
        /// </summary>
        /// <param name="args">The keyboard event arguments.</param>
        /// <param name="itemIndex">The source item index.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private Task HandleItemKeyDownAsync(KeyboardEventArgs args, int itemIndex)
        {
            switch (args.Key)
            {
                case "ArrowDown":
                case "Down":
                    this.FocusItem(KeyboardNavigation.GetNextEnabledIndex(this.Items, itemIndex, 1, ActionMenuItemHelper.IsEnabled));
                    break;
                case "ArrowUp":
                case "Up":
                    this.FocusItem(KeyboardNavigation.GetNextEnabledIndex(this.Items, itemIndex, -1, ActionMenuItemHelper.IsEnabled));
                    break;
                case "Home":
                    this.FocusItem(KeyboardNavigation.GetFirstEnabledIndex(this.Items, ActionMenuItemHelper.IsEnabled));
                    break;
                case "End":
                    this.FocusItem(KeyboardNavigation.GetLastEnabledIndex(this.Items, ActionMenuItemHelper.IsEnabled));
                    break;
                case "Escape":
                    this.CloseMenu(true);
                    break;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Tracks focus entering the user menu.
        /// </summary>
        private void HandleFocusIn()
        {
            this.HasFocusWithin = true;
        }

        /// <summary>
        /// Closes the menu when focus leaves the user menu.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleFocusOutAsync()
        {
            this.HasFocusWithin = false;

            await Task.Delay(100);

            if (!this.HasFocusWithin)
            {
                this.CloseMenu();
            }
        }

        /// <summary>
        /// Opens the menu and optionally focuses an item.
        /// </summary>
        /// <param name="itemIndex">The optional item index to focus.</param>
        private void OpenMenu(int? itemIndex = null)
        {
            this.IsOpen = true;
            this.PendingFocusItemIndex = itemIndex;
        }

        /// <summary>
        /// Closes the menu.
        /// </summary>
        /// <param name="focusTrigger">A value indicating whether the trigger should receive focus.</param>
        private void CloseMenu(bool focusTrigger = false)
        {
            this.IsOpen = false;
            this.PendingFocusItemIndex = null;
            this.ShouldFocusTrigger = focusTrigger;
        }

        /// <summary>
        /// Focuses the provided item index after rendering.
        /// </summary>
        /// <param name="itemIndex">The item index to focus.</param>
        private void FocusItem(int? itemIndex)
        {
            this.PendingFocusItemIndex = itemIndex;
        }

        /// <summary>
        /// Asynchronously disposes keyboard navigation JavaScript resources.
        /// </summary>
        /// <returns>A value task representing the asynchronous dispose operation.</returns>
        private async ValueTask DisposeAsyncCore()
        {
            if (this.KeyboardNavigationModule is not null)
            {
                await KeyboardNavigation.DisposeNavigationKeyPreventionAsync(this.KeyboardNavigationModule, this.RootElement);

                this.KeyboardNavigationModule = null;
            }
        }
    }
}
