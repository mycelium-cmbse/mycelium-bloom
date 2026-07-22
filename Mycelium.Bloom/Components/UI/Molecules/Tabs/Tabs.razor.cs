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

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Reusable Bloom tabs component for switching between compact workspace sections.
    /// </summary>
    public partial class Tabs : BloomComponentBase, IAsyncDisposable
    {
        /// <summary>
        /// The tab-list root that owns the element-scoped keyboard listener.
        /// </summary>
        private ElementReference tabListElement;

        /// <summary>
        /// The rendered tab element references used for keyboard focus movement.
        /// </summary>
        private ElementReference[] tabElements = [];

        /// <summary>
        /// The tab index that should receive focus after the next render.
        /// </summary>
        private int? pendingFocusTabIndex;

        /// <summary>
        /// The element-scoped keyboard-default registration.
        /// </summary>
        private KeyboardDefaultPreventionRegistration keyboardDefaultPreventionRegistration;

        /// <summary>
        /// A value indicating whether the component has been disposed.
        /// </summary>
        private bool isDisposed;

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
        /// Keeps the tab reference collection aligned with the available items.
        /// </summary>
        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (this.isDisposed)
            {
                return;
            }

            if (this.tabElements.Length != this.Items.Count)
            {
                this.tabElements = new ElementReference[this.Items.Count];
            }
        }

        /// <summary>
        /// Moves focus to the tab selected through keyboard navigation.
        /// </summary>
        /// <param name="firstRender">A value indicating whether this is the first render.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender && !this.isDisposed)
            {
                this.keyboardDefaultPreventionRegistration = new KeyboardDefaultPreventionRegistration(this.JsRuntime);

                await this.keyboardDefaultPreventionRegistration.RegisterAsync(
                    this.tabListElement,
                    [
                        new KeyboardDefaultPreventionRule(
                            "[role='tab']",
                            "ArrowRight",
                            "Right",
                            "ArrowLeft",
                            "Left",
                            "Home",
                            "End")
                    ]);
            }

            if (!this.isDisposed
                && this.pendingFocusTabIndex is { } tabIndex
                && tabIndex >= 0
                && tabIndex < this.tabElements.Length)
            {
                this.pendingFocusTabIndex = null;

                await this.tabElements[tabIndex].FocusAsync(true);
            }
        }

        /// <summary>
        /// Releases the element-scoped keyboard-default registration.
        /// </summary>
        /// <returns>A value task representing the asynchronous cleanup.</returns>
        public async ValueTask DisposeAsync()
        {
            this.isDisposed = true;
            this.pendingFocusTabIndex = null;

            if (this.keyboardDefaultPreventionRegistration is not null)
            {
                await this.keyboardDefaultPreventionRegistration.DisposeAsync();
                this.keyboardDefaultPreventionRegistration = null;
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets the final CSS class list applied to the tabs.
        /// </summary>
        private string GetCssClass()
        {
            var cssClass = this.BuildRootCssClass(
                "mb-tabs",
                CssClassBuilder.When("mb-tabs--full-width", this.FullWidth));

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
        /// Gets the roving tab index for an item.
        /// </summary>
        /// <param name="itemIndex">The item index.</param>
        /// <returns>Zero for the active enabled tab, or the first enabled fallback; otherwise, minus one.</returns>
        private int GetTabIndex(int itemIndex)
        {
            var tabbableTabIndex = this.GetTabbableTabIndex();

            return tabbableTabIndex == itemIndex ? 0 : -1;
        }

        /// <summary>
        /// Selects the provided tab item when it is not disabled.
        /// </summary>
        /// <param name="item">The tab item to select.</param>
        private async Task SelectTabAsync(TabItem item)
        {
            if (!this.isDisposed && !item.Disabled)
            {
                await this.ActiveValueChanged.InvokeAsync(item.Value);
            }
        }

        /// <summary>
        /// Handles horizontal arrow, Home, and End tab navigation.
        /// </summary>
        /// <param name="args">The keyboard event arguments.</param>
        /// <param name="itemIndex">The source item index.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleTabKeyDownAsync(KeyboardEventArgs args, int itemIndex)
        {
            if (this.isDisposed)
            {
                return;
            }

            var destinationIndex = args.Key switch
            {
                "ArrowRight" or "Right" => this.FindNextEnabledTabIndex(itemIndex, 1),
                "ArrowLeft" or "Left" => this.FindNextEnabledTabIndex(itemIndex, -1),
                "Home" => this.FindFirstEnabledTabIndex(),
                "End" => this.FindLastEnabledTabIndex(),
                _ => null
            };

            if (destinationIndex is { } selectedIndex)
            {
                this.pendingFocusTabIndex = selectedIndex;

                await this.SelectTabAsync(this.Items[selectedIndex]);
            }
        }

        /// <summary>
        /// Gets the enabled tab that participates in the page tab order.
        /// </summary>
        /// <returns>The active enabled tab index, the first enabled tab index as a fallback, or null.</returns>
        private int? GetTabbableTabIndex()
        {
            for (var index = 0; index < this.Items.Count; index++)
            {
                if (!this.Items[index].Disabled && this.IsActive(this.Items[index]))
                {
                    return index;
                }
            }

            return this.FindFirstEnabledTabIndex();
        }

        /// <summary>
        /// Finds the next enabled tab in the requested direction, wrapping at either end.
        /// </summary>
        /// <param name="itemIndex">The source item index.</param>
        /// <param name="step">One to move forward, or minus one to move backward.</param>
        /// <returns>The next enabled tab index, or null when no tab is enabled.</returns>
        private int? FindNextEnabledTabIndex(int itemIndex, int step)
        {
            for (var offset = 1; offset <= this.Items.Count; offset++)
            {
                var candidateIndex = (itemIndex + (offset * step)) % this.Items.Count;

                if (candidateIndex < 0)
                {
                    candidateIndex += this.Items.Count;
                }

                if (!this.Items[candidateIndex].Disabled)
                {
                    return candidateIndex;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the first enabled tab.
        /// </summary>
        /// <returns>The first enabled tab index, or null when no tab is enabled.</returns>
        private int? FindFirstEnabledTabIndex()
        {
            for (var index = 0; index < this.Items.Count; index++)
            {
                if (!this.Items[index].Disabled)
                {
                    return index;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the last enabled tab.
        /// </summary>
        /// <returns>The last enabled tab index, or null when no tab is enabled.</returns>
        private int? FindLastEnabledTabIndex()
        {
            for (var index = this.Items.Count - 1; index >= 0; index--)
            {
                if (!this.Items[index].Disabled)
                {
                    return index;
                }
            }

            return null;
        }
    }
}
