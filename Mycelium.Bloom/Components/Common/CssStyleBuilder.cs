// ------------------------------------------------------------------------------------------------
// <copyright file="CssStyleBuilder.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.Common
{
    /// <summary>
    /// Provides helper methods to build inline CSS declaration strings.
    /// </summary>
    public static class CssStyleBuilder
    {
        /// <summary>
        /// Builds an inline style string from declarations with non-empty properties and values.
        /// </summary>
        /// <param name="declarations">The CSS property and value pairs to combine.</param>
        /// <returns>The valid CSS declarations separated by spaces.</returns>
        public static string Build(params (string Property, string Value)[] declarations)
        {
            var validDeclarations = declarations
                .Where(declaration => !string.IsNullOrWhiteSpace(declaration.Property) &&
                                      !string.IsNullOrWhiteSpace(declaration.Value))
                .Select(declaration => $"{declaration.Property.Trim()}: {declaration.Value.Trim()};");

            var style = string.Join(" ", validDeclarations);

            return style;
        }
    }
}
