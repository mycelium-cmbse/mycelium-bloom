// ------------------------------------------------------------------------------------------------
// <copyright file="BrandMark.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Atoms.BrandMark
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.UI.Common;

    /// <summary>
    /// Renders the repository-owned Bloom SVG mark as decorative or named brand imagery.
    /// </summary>
    public sealed partial class BrandMark : BloomComponentBase
    {
        /// <summary>
        /// Gets or sets the alternative text when the mark conveys meaning independently.
        /// Leave unset when adjacent text already names the brand.
        /// </summary>
        [Parameter]
        public string AccessibleName { get; set; }

        /// <summary>
        /// Gets the component's final CSS class list.
        /// </summary>
        /// <returns>The brand-mark CSS classes.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass("mb-brand-mark");
        }

        /// <summary>
        /// Gets the image alternative text.
        /// </summary>
        /// <returns>The accessible name or an empty decorative alternative.</returns>
        private string GetAlternativeText()
        {
            return string.IsNullOrWhiteSpace(this.AccessibleName)
                ? string.Empty
                : this.AccessibleName;
        }

        /// <summary>
        /// Gets whether assistive technology should ignore the decorative mark.
        /// </summary>
        /// <returns>True when adjacent content supplies the accessible name; otherwise, null.</returns>
        private string GetAriaHidden()
        {
            return string.IsNullOrWhiteSpace(this.AccessibleName) ? "true" : null;
        }
    }
}
