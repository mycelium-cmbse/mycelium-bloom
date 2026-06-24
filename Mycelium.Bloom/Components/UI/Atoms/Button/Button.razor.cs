namespace Mycelium.Bloom.Components.UI.Atoms.Button
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Represents a reusable button component with configurable variant, size, icons, loading state, and click handling.
    /// </summary>
    public partial class Button : ComponentBase
    {
        /// <summary>
        /// Gets or sets the visual variant of the button.
        /// </summary>
        [Parameter]
        public ButtonVariant Variant { get; set; } = ButtonVariant.Primary;

        /// <summary>
        /// Gets or sets the size of the button.
        /// </summary>
        [Parameter]
        public ButtonSize Size { get; set; } = ButtonSize.Medium;

        /// <summary>
        /// Gets or sets the HTML button type.
        /// </summary>
        [Parameter]
        public string Type { get; set; } = "button";

        /// <summary>
        /// Gets or sets a value indicating whether the button is disabled.
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the button is in a loading state.
        /// </summary>
        [Parameter]
        public bool IsLoading { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the button should take the full available width.
        /// </summary>
        [Parameter]
        public bool FullWidth { get; set; }

        /// <summary>
        /// Gets or sets additional CSS classes applied to the button.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets optional content rendered before the button content.
        /// </summary>
        [Parameter]
        public RenderFragment StartIcon { get; set; }

        /// <summary>
        /// Gets or sets optional content rendered after the button content.
        /// </summary>
        [Parameter]
        public RenderFragment EndIcon { get; set; }

        /// <summary>
        /// Gets or sets the main content rendered inside the button.
        /// </summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the button is clicked.
        /// </summary>
        [Parameter]
        public EventCallback<MouseEventArgs> OnClick { get; set; }

        /// <summary>
        /// Gets or sets additional unmatched attributes applied to the button element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets a value indicating whether the button should be rendered as disabled.
        /// </summary>
        private bool IsDisabled => this.Disabled || this.IsLoading;

        /// <summary>
        /// Gets the final CSS class list applied to the button.
        /// </summary>
        private string CssClass
        {
            get
            {
                var cssClass = CssClassBuilder.Build(
                    "mb-button",
                    this.GetVariantClass(),
                    this.GetSizeClass(),
                    CssClassBuilder.When("mb-button--full-width", this.FullWidth),
                    CssClassBuilder.When("mb-button--disabled", this.IsDisabled),
                    this.Class);

                return cssClass;
            }
        }

        /// <summary>
        /// Gets the CSS class matching the selected button variant.
        /// </summary>
        /// <returns>The CSS class for the selected button variant.</returns>
        private string GetVariantClass()
        {
            var cssClass = this.Variant switch
            {
                ButtonVariant.Secondary => "mb-button--secondary",
                ButtonVariant.Ghost => "mb-button--ghost",
                ButtonVariant.Danger => "mb-button--danger",
                _ => "mb-button--primary"
            };

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class matching the selected button size.
        /// </summary>
        /// <returns>The CSS class for the selected button size.</returns>
        private string GetSizeClass()
        {
            var cssClass = this.Size switch
            {
                ButtonSize.Small => "mb-button--small",
                ButtonSize.Large => "mb-button--large",
                _ => "mb-button--medium"
            };

            return cssClass;
        }
    }
}
