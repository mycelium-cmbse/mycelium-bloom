namespace Mycelium.Bloom.Components.UI.Atoms.Avatar
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Represents a reusable avatar component used to display initials, users, or overflow indicators.
    /// </summary>
    public partial class Avatar : ComponentBase
    {
        /// <summary>
        /// Gets or sets the text displayed inside the avatar.
        /// </summary>
        [Parameter]
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional title used for accessibility or tooltip display.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional custom avatar background color.
        /// </summary>
        [Parameter]
        public string BackgroundColor { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional custom avatar border color.
        /// </summary>
        [Parameter]
        public string BorderColor { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the avatar size.
        /// </summary>
        [Parameter]
        public AvatarSize Size { get; set; } = AvatarSize.Medium;

        /// <summary>
        /// Gets or sets the avatar visual variant.
        /// </summary>
        [Parameter]
        public AvatarVariant Variant { get; set; } = AvatarVariant.User;

        /// <summary>
        /// Gets or sets additional CSS classes applied to the avatar.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional unmatched attributes applied to the avatar element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the inline style containing custom avatar CSS variables.
        /// </summary>
        private string Style
        {
            get
            {
                var styles = new List<string>();

                if (!string.IsNullOrWhiteSpace(this.BackgroundColor))
                {
                    styles.Add($"--mb-avatar-background: {this.BackgroundColor}");
                }

                if (!string.IsNullOrWhiteSpace(this.BorderColor))
                {
                    styles.Add($"--mb-avatar-border: {this.BorderColor}");
                }

                var style = styles.Count > 0
                    ? string.Join("; ", styles)
                    : null;

                return style;
            }
        }

        /// <summary>
        /// Gets the final CSS class list applied to the avatar.
        /// </summary>
        private string CssClass
        {
            get
            {
                var cssClass = new CssClassBuilder()
                    .Add("mb-avatar")
                    .Add(this.GetSizeClass())
                    .Add(this.GetVariantClass())
                    .Add(this.Class)
                    .ToString();

                return cssClass;
            }
        }

        /// <summary>
        /// Gets the CSS class matching the selected avatar size.
        /// </summary>
        /// <returns>The CSS class for the selected avatar size.</returns>
        private string GetSizeClass()
        {
            var cssClass = this.Size switch
            {
                AvatarSize.Small => "mb-avatar--small",
                AvatarSize.Large => "mb-avatar--large",
                _ => "mb-avatar--medium"
            };

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class matching the selected avatar variant.
        /// </summary>
        /// <returns>The CSS class for the selected avatar variant.</returns>
        private string GetVariantClass()
        {
            var cssClass = this.Variant switch
            {
                AvatarVariant.More => "mb-avatar--more",
                _ => "mb-avatar--user"
            };

            return cssClass;
        }
    }
}
