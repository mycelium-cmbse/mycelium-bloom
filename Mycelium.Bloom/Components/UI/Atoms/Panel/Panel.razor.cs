using Microsoft.AspNetCore.Components;

namespace Mycelium.Bloom.Components.UI.Atoms.Panel
{
    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Represents a reusable panel component used to wrap content with consistent spacing and layout behavior.
    /// </summary>
    public partial class Panel : ComponentBase
    {
        /// <summary>
        /// Gets or sets the padding applied inside the panel.
        /// </summary>
        [Parameter]
        public PanelPadding Padding { get; set; } = PanelPadding.Medium;

        /// <summary>
        /// Gets or sets a value indicating whether the panel should take the full available height.
        /// </summary>
        [Parameter]
        public bool FullHeight { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether overflowing content should be hidden.
        /// </summary>
        [Parameter]
        public bool OverflowHidden { get; set; }

        /// <summary>
        /// Gets or sets additional CSS classes applied to the panel.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the content rendered inside the panel.
        /// </summary>
        [Parameter]
        [EditorRequired]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets additional unmatched attributes applied to the panel element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the final CSS class list applied to the panel.
        /// </summary>
        private string CssClass
        {
            get
            {
                var cssClass = new CssClassBuilder()
                    .Add("mb-panel")
                    .Add(this.GetPaddingClass())
                    .Add("mb-panel--full-height", this.FullHeight)
                    .Add("mb-panel--overflow-hidden", this.OverflowHidden)
                    .Add(this.Class)
                    .ToString();

                return cssClass;
            }
        }

        /// <summary>
        /// Gets the CSS class matching the selected panel padding.
        /// </summary>
        /// <returns>The CSS class for the selected panel padding.</returns>
        private string GetPaddingClass()
        {
            var cssClass = this.Padding switch
            {
                PanelPadding.None => "mb-panel--padding-none",
                PanelPadding.Small => "mb-panel--padding-small",
                PanelPadding.Large => "mb-panel--padding-large",
                _ => "mb-panel--padding-medium"
            };

            return cssClass;
        }
    }
}
