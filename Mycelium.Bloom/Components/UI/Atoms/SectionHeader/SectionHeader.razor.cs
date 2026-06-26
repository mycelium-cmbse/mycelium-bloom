// ------------------------------------------------------------------------------------------------
// <copyright file="SectionHeader.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Atoms.SectionHeader
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;

    /// <summary>
    /// Reusable compact section header used to group content inside Bloom panels.
    /// </summary>
    public partial class SectionHeader : ComponentBase
    {
        /// <summary>
        /// Gets or sets the section label.
        /// </summary>
        [Parameter]
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets optional actions rendered on the right side of the header.
        /// </summary>
        [Parameter]
        public RenderFragment Actions { get; set; }

        /// <summary>
        /// Gets or sets additional CSS classes.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets unmatched attributes passed to the section header element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the final CSS class list applied to the section header.
        /// </summary>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-section-header",
                this.Class);

            return cssClass;
        }
    }
}
