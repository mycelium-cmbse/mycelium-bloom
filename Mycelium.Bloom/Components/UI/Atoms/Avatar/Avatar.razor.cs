// ------------------------------------------------------------------------------------------------
// <copyright file="Avatar.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Atoms.Avatar
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Represents a reusable avatar component used to display initials, users, or overflow indicators.
    /// </summary>
    public partial class Avatar : BloomComponentBase
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
        /// Gets the inline style containing custom avatar CSS variables.
        /// </summary>
        private string GetStyle()
        {
            var style = CssStyleBuilder.Build(
                ("--mb-avatar-background", this.BackgroundColor),
                ("--mb-avatar-border", this.BorderColor));

            return string.IsNullOrWhiteSpace(style) ? null : style;
        }

        /// <summary>
        /// Gets the final CSS class list applied to the avatar.
        /// </summary>
        private string GetCssClass()
        {
            var cssClass = this.BuildRootCssClass(
                "mb-avatar",
                this.GetSizeClass(),
                this.GetVariantClass());

            return cssClass;
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
