// ------------------------------------------------------------------------------------------------
// <copyright file="Skeleton.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Atoms.Skeleton
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Represents a reusable placeholder used while Bloom content is loading.
    /// </summary>
    public partial class Skeleton : ComponentBase
    {
        /// <summary>
        /// Gets or sets the skeleton placeholder variant.
        /// </summary>
        [Parameter]
        public SkeletonVariant Variant { get; set; } = SkeletonVariant.Text;

        /// <summary>
        /// Gets or sets the number of placeholders to render.
        /// </summary>
        [Parameter]
        public int Lines { get; set; } = 1;

        /// <summary>
        /// Gets or sets the optional inline width applied to each placeholder.
        /// </summary>
        [Parameter]
        public string Width { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional inline height applied to each placeholder.
        /// </summary>
        [Parameter]
        public string Height { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional CSS classes applied to the skeleton wrapper.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional unmatched attributes applied to the skeleton wrapper.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the final CSS class list applied to the skeleton wrapper.
        /// </summary>
        /// <returns>The skeleton CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-skeleton",
                this.GetVariantClass(),
                this.Class);

            return cssClass;
        }

        /// <summary>
        /// Gets the number of placeholders to render.
        /// </summary>
        /// <returns>The safe line count.</returns>
        private int GetLineCount()
        {
            var lineCount = Math.Max(1, this.Lines);

            return lineCount;
        }

        /// <summary>
        /// Gets the inline style applied to each skeleton placeholder.
        /// </summary>
        /// <returns>The inline width and height styles when provided.</returns>
        private string GetLineStyle()
        {
            var styleParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(this.Width))
            {
                styleParts.Add($"width: {this.Width};");
            }

            if (!string.IsNullOrWhiteSpace(this.Height))
            {
                styleParts.Add($"height: {this.Height};");
            }

            var style = styleParts.Count == 0
                ? null
                : string.Join(" ", styleParts);

            return style;
        }

        /// <summary>
        /// Gets the CSS class matching the selected skeleton variant.
        /// </summary>
        /// <returns>The skeleton variant CSS class.</returns>
        private string GetVariantClass()
        {
            var cssClass = this.Variant switch
            {
                SkeletonVariant.Circle => "mb-skeleton--circle",
                SkeletonVariant.Rectangle => "mb-skeleton--rectangle",
                _ => "mb-skeleton--text"
            };

            return cssClass;
        }
    }
}
