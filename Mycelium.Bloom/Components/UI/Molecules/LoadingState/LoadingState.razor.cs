// ------------------------------------------------------------------------------------------------
// <copyright file="LoadingState.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Molecules.LoadingState
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;

    /// <summary>
    /// Represents a compact loading state for panels, tables, and workspace areas.
    /// </summary>
    public partial class LoadingState : ComponentBase
    {
        /// <summary>
        /// Gets or sets the loading state title.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = "Loading";

        /// <summary>
        /// Gets or sets the optional loading state description.
        /// </summary>
        [Parameter]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the spinner should be rendered.
        /// </summary>
        [Parameter]
        public bool ShowSpinner { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether skeleton content should be rendered.
        /// </summary>
        [Parameter]
        public bool ShowSkeleton { get; set; }

        /// <summary>
        /// Gets or sets optional custom skeleton content.
        /// </summary>
        [Parameter]
        public RenderFragment SkeletonContent { get; set; }

        /// <summary>
        /// Gets or sets additional CSS classes applied to the loading state.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional unmatched attributes applied to the loading state.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the final CSS class list applied to the loading state.
        /// </summary>
        /// <returns>The loading state CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-loading-state",
                this.Class);

            return cssClass;
        }
    }
}
