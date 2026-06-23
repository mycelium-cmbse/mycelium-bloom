using Microsoft.AspNetCore.Components;

namespace Mycelium.Bloom.Components.UI.Atoms.Chip
{
    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Represents a reusable chip component used to display compact labels, statuses, or ownership indicators.
    /// </summary>
    public partial class Chip : ComponentBase
    {
        /// <summary>
        /// Gets or sets the visual variant of the chip.
        /// </summary>
        [Parameter]
        public ChipVariant Variant { get; set; } = ChipVariant.Default;

        /// <summary>
        /// Gets or sets the optional custom color used by the chip.
        /// </summary>
        [Parameter]
        public string Color { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional CSS classes applied to the chip.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the content rendered inside the chip.
        /// </summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets additional unmatched attributes applied to the chip element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the inline style containing the optional custom chip color.
        /// </summary>
        private string Style => !string.IsNullOrWhiteSpace(this.Color)
            ? $"--mb-chip-color: {this.Color};"
            : string.Empty;

        /// <summary>
        /// Gets the final CSS class list applied to the chip.
        /// </summary>
        private string CssClass
        {
            get
            {
                var cssClass = new CssClassBuilder()
                    .Add("mb-chip")
                    .Add(this.GetVariantClass())
                    .Add("mb-chip--custom-color", !string.IsNullOrWhiteSpace(this.Color))
                    .Add(this.Class)
                    .ToString();

                return cssClass;
            }
        }

        /// <summary>
        /// Gets the CSS class matching the selected chip variant.
        /// </summary>
        /// <returns>The CSS class for the selected chip variant.</returns>
        private string GetVariantClass()
        {
            var cssClass = this.Variant switch
            {
                ChipVariant.Success => "mb-chip--success",
                ChipVariant.Warning => "mb-chip--warning",
                ChipVariant.Danger => "mb-chip--danger",
                ChipVariant.Info => "mb-chip--info",
                ChipVariant.Ownership => "mb-chip--ownership",
                ChipVariant.Lifecycle => "mb-chip--lifecycle",
                _ => "mb-chip--default"
            };

            return cssClass;
        }
    }
}
