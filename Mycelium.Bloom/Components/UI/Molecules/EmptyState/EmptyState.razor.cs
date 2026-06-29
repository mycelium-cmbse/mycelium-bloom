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

    using Mycelium.Bloom.Components.Common;

    /// <summary>
    /// Reusable Bloom empty state used when a workspace area has no selected or available content.
    /// </summary>
    public partial class EmptyState : ComponentBase
    {
        /// <summary>
        /// Gets or sets the empty state title.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the empty state description.
        /// </summary>
        [Parameter]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets optional icon content rendered above the title.
        /// </summary>
        [Parameter]
        public RenderFragment IconContent { get; set; }

        /// <summary>
        /// Gets or sets optional action content rendered below the description.
        /// </summary>
        [Parameter]
        public RenderFragment ActionsContent { get; set; }

        /// <summary>
        /// Gets or sets additional CSS classes.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets unmatched attributes passed to the empty state element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-empty-state",
                this.Class);

            return cssClass;
        }
    }
}
