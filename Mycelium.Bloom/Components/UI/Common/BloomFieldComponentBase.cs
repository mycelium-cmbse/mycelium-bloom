// ------------------------------------------------------------------------------------------------
// <copyright file="BloomFieldComponentBase.cs" company="Starion Group S.A.">
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
    /// Provides common parameters shared by reusable Bloom form field components.
    /// </summary>
    public class BloomFieldComponentBase : BloomComponentBase
    {
        /// <summary>
        /// Gets or sets the identifier of the form field element.
        /// </summary>
        [Parameter]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the form field element.
        /// </summary>
        [Parameter]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the label displayed for the form field.
        /// </summary>
        [Parameter]
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the help text displayed for the form field.
        /// </summary>
        [Parameter]
        public string HelpText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error text displayed for the form field.
        /// </summary>
        [Parameter]
        public string ErrorText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the form field is disabled.
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the form field is required.
        /// </summary>
        [Parameter]
        public bool Required { get; set; }

        /// <summary>
        /// Gets a value indicating whether the form field has an error.
        /// </summary>
        protected bool HasError => !string.IsNullOrWhiteSpace(this.ErrorText);
    }
}
