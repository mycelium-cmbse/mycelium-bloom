// ------------------------------------------------------------------------------------------------
// <copyright file="AppHeader.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.AppHeader
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;

    /// <summary>
    /// Reusable Bloom application header used at the top of workspace pages.
    /// </summary>
    public partial class AppHeader : ComponentBase
    {
        /// <summary>
        /// Gets or sets the main header title.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = "Mycelium Bloom";

        /// <summary>
        /// Gets or sets the optional header subtitle.
        /// </summary>
        [Parameter]
        public string Subtitle { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the search input should be shown.
        /// </summary>
        [Parameter]
        public bool ShowSearch { get; set; } = true;

        /// <summary>
        /// Gets or sets the current search value.
        /// </summary>
        [Parameter]
        public string SearchValue { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional identifier applied to the search input.
        /// </summary>
        [Parameter]
        public string SearchId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the search value change callback.
        /// </summary>
        [Parameter]
        public EventCallback<string> SearchValueChanged { get; set; }

        /// <summary>
        /// Gets or sets the search input placeholder.
        /// </summary>
        [Parameter]
        public string SearchPlaceholder { get; set; } = "Search model...";

        /// <summary>
        /// Gets or sets whether the global Ctrl+K search shortcut should focus the header search input.
        /// </summary>
        [Parameter]
        public bool EnableSearchShortcut { get; set; }

        /// <summary>
        /// Gets or sets whether the user avatar should be shown.
        /// </summary>
        [Parameter]
        public bool ShowUserAvatar { get; set; } = true;

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
        /// Gets or sets the user avatar background color.
        /// </summary>
        [Parameter]
        public string UserColor { get; set; } = "var(--mb-color-collaborator-c08, #3b82f6)";

        /// <summary>
        /// Gets or sets optional custom brand content.
        /// </summary>
        [Parameter]
        public RenderFragment BrandContent { get; set; }

        /// <summary>
        /// Gets or sets optional action content rendered on the right side.
        /// </summary>
        [Parameter]
        public RenderFragment ActionsContent { get; set; }

        /// <summary>
        /// Gets or sets additional CSS classes.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets unmatched attributes passed to the header element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-app-header",
                this.Class);

            return cssClass;
        }
    }
}
