// ------------------------------------------------------------------------------------------------
// <copyright file="PageHeader.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Molecules.PageHeader
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Common;

    /// <summary>
    /// Represents the title block of a page: a title, an optional subtitle and the actions of the page on the right.
    /// </summary>
    public sealed partial class PageHeader : BloomComponentBase
    {
        /// <summary>
        /// Gets or sets the title of the page.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional subtitle of the page.
        /// </summary>
        [Parameter]
        public string Subtitle { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the actions of the page, rendered on the right of the title.
        /// </summary>
        [Parameter]
        public RenderFragment ActionsContent { get; set; }
    }
}
