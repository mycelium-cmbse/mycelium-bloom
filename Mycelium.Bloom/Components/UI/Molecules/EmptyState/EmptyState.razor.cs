// ------------------------------------------------------------------------------------------------
// <copyright file="EmptyState.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Molecules.EmptyState
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.UI.Common;

    /// <summary>
    /// Represents a neutral empty state for content regions without available data.
    /// </summary>
    public partial class EmptyState : BloomComponentBase
    {
        /// <summary>
        /// Gets or sets the empty state title.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional empty state description.
        /// </summary>
        [Parameter]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets optional icon content rendered before the title.
        /// </summary>
        [Parameter]
        public RenderFragment IconContent { get; set; }

        /// <summary>
        /// Gets or sets optional action content rendered after the description.
        /// </summary>
        [Parameter]
        public RenderFragment ActionContent { get; set; }

        /// <summary>
        /// Gets the final CSS class list applied to the empty state.
        /// </summary>
        /// <returns>The empty state CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = this.BuildRootCssClass("mb-empty-state");

            return cssClass;
        }
    }
}
