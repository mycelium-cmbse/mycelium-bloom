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
    }
}
