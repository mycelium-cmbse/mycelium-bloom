// ------------------------------------------------------------------------------------------------
// <copyright file="Chip.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Atoms.Chip
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Represents a reusable chip component used to display compact labels, statuses, or ownership indicators.
    /// </summary>
    public partial class Chip : BloomComponentBase
    {
        /// <summary>
        /// Gets or sets the visual variant of the chip.
        /// </summary>
        [Parameter]
        public ChipVariant Variant { get; set; } = ChipVariant.Default;

        /// <summary>
        /// Gets or sets the optional custom color used by the chip.
        /// </summary>
        [Parameter]
        public string Color { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the content rendered inside the chip.
        /// </summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets the inline style containing the optional custom chip color.
        /// </summary>
        private string GetStyle()
        {
            var style = !string.IsNullOrWhiteSpace(this.Color)
                ? $"--mb-chip-color: {this.Color};"
                : string.Empty;

            return style;
        }

        /// <summary>
        /// Gets the final CSS class list applied to the chip.
        /// </summary>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-chip",
                this.GetVariantClass(),
                CssClassBuilder.When("mb-chip--custom-color",
                    !string.IsNullOrWhiteSpace(this.Color)),
                this.Class);

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class matching the selected chip variant.
        /// </summary>
        /// <returns>The CSS class for the selected chip variant.</returns>
        private string GetVariantClass()
        {
            var cssClass = this.Variant switch
            {
                ChipVariant.Success => "mb-chip--success",
                ChipVariant.Warning => "mb-chip--warning",
                ChipVariant.Danger => "mb-chip--danger",
                ChipVariant.Info => "mb-chip--info",
                ChipVariant.Ownership => "mb-chip--ownership",
                ChipVariant.Lifecycle => "mb-chip--lifecycle",
                _ => "mb-chip--default"
            };

            return cssClass;
        }
    }
}
