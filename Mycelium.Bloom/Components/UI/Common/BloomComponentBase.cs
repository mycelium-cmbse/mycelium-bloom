// ------------------------------------------------------------------------------------------------
// <copyright file="BloomComponentBase.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Common
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;

    /// <summary>
    /// Provides common parameters shared by reusable Bloom UI components.
    /// </summary>
    public class BloomComponentBase : ComponentBase
    {
        /// <summary>
        /// Gets or sets additional CSS classes applied to the component root element.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional unmatched attributes applied to the component root element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Builds the CSS class list for a component root and appends the configured custom class.
        /// </summary>
        /// <param name="cssClasses">The component-owned CSS classes.</param>
        /// <returns>The root CSS classes separated by spaces.</returns>
        protected string BuildRootCssClass(params string[] cssClasses)
        {
            var rootCssClass = CssClassBuilder.Build([.. cssClasses, this.Class]);

            return rootCssClass;
        }
    }
}
