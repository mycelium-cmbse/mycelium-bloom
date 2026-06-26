// ------------------------------------------------------------------------------------------------
// <copyright file="CssClassBuilder.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.Common
{
    /// <summary>
    /// Provides helper methods to build CSS class strings.
    /// </summary>
    public static class CssClassBuilder
    {
        /// <summary>
        /// Builds a CSS class string from the provided class names.
        /// </summary>
        /// <param name="cssClasses">The CSS classes to combine.</param>
        /// <returns>The CSS classes separated by spaces.</returns>
        public static string Build(params string[] cssClasses)
        {
            var validCssClasses = cssClasses
                .Where(cssClass => !string.IsNullOrWhiteSpace(cssClass));

            var cssClass = string.Join(" ", validCssClasses);

            return cssClass;
        }

        /// <summary>
        /// Returns the CSS class when the provided condition is true.
        /// </summary>
        /// <param name="cssClass">The CSS class to return.</param>
        /// <param name="condition">A value indicating whether the CSS class should be returned.</param>
        /// <returns>The CSS class when the condition is true; otherwise, an empty string.</returns>
        public static string When(string cssClass, bool condition)
        {
            var resolvedCssClass = condition ? cssClass : string.Empty;

            return resolvedCssClass;
        }
    }
}
