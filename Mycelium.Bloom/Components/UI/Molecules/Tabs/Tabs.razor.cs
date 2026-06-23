using Microsoft.AspNetCore.Components;

namespace Mycelium.Bloom.Components.UI.Molecules.Tabs
{
    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Reusable Bloom tabs component for switching between compact workspace sections.
    /// </summary>
    public partial class Tabs : ComponentBase
    {
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

        private string CssClass
        {
            get
            {
                var cssClass = new CssClassBuilder()
                    .Add("mb-tabs")
                    .Add("mb-tabs--full-width", this.FullWidth)
                    .Add(this.Class)
                    .ToString();

                return cssClass;
            }
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
            var cssClass = new CssClassBuilder()
                .Add("mb-tabs__item")
                .Add("mb-tabs__item--active", this.IsActive(item))
                .Add("mb-tabs__item--disabled", item.Disabled)
                .ToString();

            return cssClass;
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
    }
}
