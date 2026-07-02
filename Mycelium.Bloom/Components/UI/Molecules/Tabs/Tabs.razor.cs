// ------------------------------------------------------------------------------------------------
// <copyright file="Tabs.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Molecules.Tabs
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;
    using Microsoft.JSInterop;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Reusable Bloom tabs component for switching between compact workspace sections.
    /// </summary>
    public partial class Tabs : ComponentBase, IAsyncDisposable
    {
        /// <summary>
        /// The JavaScript module used to prevent browser scrolling for handled navigation keys.
        /// </summary>
        private const string KeyboardNavigationModulePath = "/js/keyboardNavigation.js";

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
        /// Gets or sets the available tab items.
        /// </summary>
        [Parameter]
        public IReadOnlyList<TabItem> Items { get; set; } = [];

        /// <summary>
        /// Gets or sets the currently active tab value.
        /// </summary>
        [Parameter]
        public string ActiveValue { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the active tab value change callback.
        /// </summary>
        [Parameter]
        public EventCallback<string> ActiveValueChanged { get; set; }

        /// <summary>
        /// Gets or sets whether the tabs should fill the available width.
        /// </summary>
        [Parameter]
        public bool FullWidth { get; set; }

        /// <summary>
        /// Gets or sets additional CSS classes.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets unmatched attributes passed to the tab list element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; }

        /// <summary>
        /// Gets or sets the tab element references.
        /// </summary>
        private ElementReference[] TabElements { get; set; } = [];

        /// <summary>
        /// Gets or sets the tab index that should receive focus after rendering.
        /// </summary>
        private int? PendingFocusTabIndex { get; set; }

        /// <summary>
        /// Releases asynchronous resources used by the tabs.
        /// </summary>
        /// <returns>A value task representing the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await this.DisposeAsyncCore();
        }

        /// <summary>
        /// Keeps the tab reference collection aligned with the available tab items.
        /// </summary>
        protected override void OnParametersSet()
        {
            if (this.TabElements.Length != this.Items.Count)
            {
                this.TabElements = new ElementReference[this.Items.Count];
            }
        }

        /// <summary>
        /// Focuses a pending tab after rendering.
        /// </summary>
        /// <param name="firstRender">A value indicating whether this is the first render.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await this.RegisterKeyboardNavigationAsync();
            }

            if (this.PendingFocusTabIndex is { } tabIndex && tabIndex >= 0 && tabIndex < this.TabElements.Length)
            {
                this.PendingFocusTabIndex = null;

                await this.TabElements[tabIndex].FocusAsync(true);
            }
        }

        /// <summary>
        /// Gets the final CSS class list applied to the tabs.
        /// </summary>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-tabs",
                CssClassBuilder.When("mb-tabs--full-width", this.FullWidth),
                this.Class);

            return cssClass;
        }

        /// <summary>
        /// Checks whether the provided tab item is currently active.
        /// </summary>
        /// <param name="item">The tab item to check.</param>
        /// <returns>A value indicating whether the tab item is active.</returns>
        private bool IsActive(TabItem item)
        {
            var isActive = string.Equals(item.Value, this.ActiveValue, StringComparison.Ordinal);

            return isActive;
        }

        /// <summary>
        /// Gets the CSS class for a tab item.
        /// </summary>
        /// <param name="item">The tab item.</param>
        /// <returns>The tab item CSS class.</returns>
        private string GetTabClass(TabItem item)
        {
            var cssClass = CssClassBuilder.Build(
                "mb-tabs__item",
                CssClassBuilder.When("mb-tabs__item--active", this.IsActive(item)),
                CssClassBuilder.When("mb-tabs__item--disabled", item.Disabled));

            return cssClass;
        }

        /// <summary>
        /// Gets the tab index for a tab item.
        /// </summary>
        /// <param name="item">The tab item.</param>
        /// <returns>The tab index value.</returns>
        private int GetTabIndex(TabItem item)
        {
            var tabIndex = this.IsTabbable(item)
                ? 0
                : -1;

            return tabIndex;
        }

        /// <summary>
        /// Selects the provided tab item when it is not disabled.
        /// </summary>
        /// <param name="item">The tab item to select.</param>
        private async Task SelectTabAsync(TabItem item)
        {
            if (!item.Disabled)
            {
                this.ActiveValue = item.Value;

                await this.ActiveValueChanged.InvokeAsync(item.Value);
            }
        }

        /// <summary>
        /// Handles arrow-key tab navigation.
        /// </summary>
        /// <param name="args">The keyboard event arguments.</param>
        /// <param name="itemIndex">The source item index.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleTabKeyDownAsync(KeyboardEventArgs args, int itemIndex)
        {
            switch (args.Key)
            {
                case "ArrowRight":
                case "Right":
                    await this.SelectAndFocusTabAsync(KeyboardNavigation.GetNextEnabledIndex(this.Items, itemIndex, 1, IsItemEnabled));
                    break;
                case "ArrowLeft":
                case "Left":
                    await this.SelectAndFocusTabAsync(KeyboardNavigation.GetNextEnabledIndex(this.Items, itemIndex, -1, IsItemEnabled));
                    break;
                case "Home":
                    await this.SelectAndFocusTabAsync(KeyboardNavigation.GetFirstEnabledIndex(this.Items, IsItemEnabled));
                    break;
                case "End":
                    await this.SelectAndFocusTabAsync(KeyboardNavigation.GetLastEnabledIndex(this.Items, IsItemEnabled));
                    break;
            }
        }

        /// <summary>
        /// Selects and focuses a tab by index.
        /// </summary>
        /// <param name="tabIndex">The tab index.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SelectAndFocusTabAsync(int? tabIndex)
        {
            if (tabIndex is { } selectedTabIndex)
            {
                this.PendingFocusTabIndex = selectedTabIndex;

                await this.SelectTabAsync(this.Items[selectedTabIndex]);
            }
        }

        /// <summary>
        /// Checks whether a tab is the active tabbable item.
        /// </summary>
        /// <param name="item">The tab item.</param>
        /// <returns>A value indicating whether the tab item should be tabbable.</returns>
        private bool IsTabbable(TabItem item)
        {
            if (item.Disabled)
            {
                return false;
            }

            if (this.IsActive(item))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(this.ActiveValue))
            {
                return false;
            }

            var firstEnabledIndex = KeyboardNavigation.GetFirstEnabledIndex(this.Items, IsItemEnabled);

            return firstEnabledIndex is { } itemIndex && ReferenceEquals(this.Items[itemIndex], item);
        }

        /// <summary>
        /// Checks whether a tab item is enabled.
        /// </summary>
        /// <param name="item">The tab item to check.</param>
        /// <returns>A value indicating whether the tab item is enabled.</returns>
        private static bool IsItemEnabled(TabItem item)
        {
            return !item.Disabled;
        }

        /// <summary>
        /// Registers handled navigation keys so the browser does not scroll the page.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task RegisterKeyboardNavigationAsync()
        {
            this.KeyboardNavigationModule = await this.JsRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                KeyboardNavigationModulePath);

            await this.KeyboardNavigationModule.InvokeVoidAsync(
                "registerNavigationKeyPrevention",
                this.RootElement);
        }

        /// <summary>
        /// Asynchronously disposes keyboard navigation JavaScript resources.
        /// </summary>
        /// <returns>A value task representing the asynchronous dispose operation.</returns>
        private async ValueTask DisposeAsyncCore()
        {
            if (this.KeyboardNavigationModule is not null)
            {
                try
                {
                    await this.KeyboardNavigationModule.InvokeVoidAsync("disposeNavigationKeyPrevention", this.RootElement);
                    await this.KeyboardNavigationModule.DisposeAsync();

                    this.KeyboardNavigationModule = null;
                }
                catch (JSDisconnectedException)
                {
                    // The circuit is already disconnected, so there is nothing left to clean up on the client.
                }
            }
        }
    }
}
