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
    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Represents a subtle placeholder displayed while content is loading.
    /// </summary>
    public partial class Skeleton : BloomComponentBase
    {
        /// <summary>
        /// Gets or sets the skeleton placeholder variant.
        /// </summary>
        [Parameter]
        public SkeletonVariant Variant { get; set; } = SkeletonVariant.Text;

        /// <summary>
        /// Gets or sets the number of text lines to render.
        /// </summary>
        [Parameter]
        public int Lines { get; set; } = 1;

        /// <summary>
        /// Gets or sets the optional width applied to each placeholder item.
        /// </summary>
        [Parameter]
        public string Width { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional height applied to each placeholder item.
        /// </summary>
        [Parameter]
        public string Height { get; set; } = string.Empty;

        /// <summary>
        /// Gets the final CSS class list applied to the skeleton wrapper.
        /// </summary>
        /// <returns>The skeleton CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = this.BuildRootCssClass(
                "mb-skeleton",
                this.GetVariantClass());

            return cssClass;
        }

        /// <summary>
        /// Gets the number of placeholder items to render.
        /// </summary>
        /// <returns>The safe item count for the selected variant.</returns>
        private int GetLineCount()
        {
            var lineCount = this.Variant == SkeletonVariant.Text
                ? Math.Max(1, this.Lines)
                : 1;

            return lineCount;
        }

        /// <summary>
        /// Gets the optional inline dimensions applied to a placeholder item.
        /// </summary>
        /// <returns>The inline width and height declarations.</returns>
        private string GetItemStyle()
        {
            var style = CssStyleBuilder.Build(
                ("width", this.Width),
                ("height", this.Height));

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
