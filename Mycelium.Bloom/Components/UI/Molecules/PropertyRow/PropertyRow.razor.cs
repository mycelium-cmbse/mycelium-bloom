// ------------------------------------------------------------------------------------------------
// <copyright file="PropertyRow.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Molecules.PropertyRow
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model.Enum;

    public partial class PropertyRow : ComponentBase
    {
        /// <summary>
        /// Gets or sets the property label.
        /// </summary>
        [Parameter]
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the plain text value.
        /// </summary>
        [Parameter]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets custom value content.
        /// </summary>
        [Parameter]
        public RenderFragment ValueContent { get; set; }

        /// <summary>
        /// Gets or sets whether the value should use monospace typography.
        /// </summary>
        [Parameter]
        public bool IsMonospace { get; set; }

        /// <summary>
        /// Gets or sets the row layout variant.
        /// </summary>
        [Parameter]
        public PropertyRowVariant Variant { get; set; } = PropertyRowVariant.Stacked;

        /// <summary>
        /// Gets or sets additional CSS classes.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets unmatched attributes passed to the row element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; }

        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-property-row",
                this.GetVariantClass(),
                this.Class);

            return cssClass;
        }

        private string GetValueCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-property-row__text",
                CssClassBuilder.When("mb-property-row__text--mono", this.IsMonospace));

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class for the selected property row variant.
        /// </summary>
        /// <returns>The CSS class for the selected row variant.</returns>
        private string GetVariantClass()
        {
            var cssClass = this.Variant switch
            {
                PropertyRowVariant.Inline => "mb-property-row--inline",
                _ => "mb-property-row--stacked"
            };

            return cssClass;
        }
    }
}
