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

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Represents user identity information paired with parent-provided actions in a styled Blueprint menu.
    /// </summary>
    public partial class UserMenu : BloomComponentBase
    {
        /// <summary>
        /// Gets or sets the user display name.
        /// </summary>
        [Parameter]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional email address or supporting subtitle.
        /// </summary>
        [Parameter]
        public string Subtitle { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets explicit avatar text, such as user initials.
        /// </summary>
        [Parameter]
        public string AvatarText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional avatar background color.
        /// </summary>
        [Parameter]
        public string AvatarBackgroundColor { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional avatar border color.
        /// </summary>
        [Parameter]
        public string AvatarBorderColor { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the account-menu actions.
        /// </summary>
        [Parameter]
        public IReadOnlyList<ActionMenuItem> Items { get; set; } = [];

        /// <summary>
        /// Gets or sets the callback invoked when an enabled action is selected.
        /// </summary>
        [Parameter]
        public EventCallback<ActionMenuItem> ItemSelected { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether only the avatar and chevron are shown.
        /// </summary>
        [Parameter]
        public bool Compact { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user menu is disabled.
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets an explicit accessible menu label.
        /// </summary>
        [Parameter]
        public string MenuAriaLabel { get; set; } = string.Empty;

        /// <summary>
        /// Gets the final CSS class list applied to the user-menu root.
        /// </summary>
        /// <returns>The user-menu CSS class list.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass(
                "mb-user-menu",
                CssClassBuilder.When("mb-user-menu--compact", this.Compact));
        }

        /// <summary>
        /// Gets the text displayed by the Blueprint avatar fallback.
        /// </summary>
        /// <returns>The explicit avatar text or generated initials.</returns>
        private string GetAvatarText()
        {
            if (!string.IsNullOrWhiteSpace(this.AvatarText))
            {
                return this.AvatarText;
            }

            var initials = string.Join(
                string.Empty,
                this.DisplayName
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Take(2)
                    .Select(part => char.ToUpperInvariant(part[0])));

            return initials;
        }

        /// <summary>
        /// Gets the application colors consumed by the public Blueprint avatar classes.
        /// </summary>
        /// <returns>The avatar CSS variables, or null when no custom colors are configured.</returns>
        private string GetAvatarStyle()
        {
            var style = CssStyleBuilder.Build(
                ("--mb-user-avatar-background", this.AvatarBackgroundColor),
                ("--mb-user-avatar-border", this.AvatarBorderColor));

            return string.IsNullOrWhiteSpace(style) ? null : style;
        }

        /// <summary>
        /// Gets the accessible trigger label.
        /// </summary>
        /// <returns>The configured label or a display-name-aware fallback.</returns>
        private string GetMenuAriaLabel()
        {
            if (!string.IsNullOrWhiteSpace(this.MenuAriaLabel))
            {
                return this.MenuAriaLabel;
            }

            return string.IsNullOrWhiteSpace(this.DisplayName)
                ? "Open user menu"
                : $"Open user menu for {this.DisplayName}";
        }

        /// <summary>
        /// Forwards an enabled menu action to the parent.
        /// </summary>
        /// <param name="item">The selected action.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleItemSelectedAsync(ActionMenuItem item)
        {
            if (!this.Disabled && !item.Disabled)
            {
                await this.ItemSelected.InvokeAsync(item);
            }
        }
    }
}
