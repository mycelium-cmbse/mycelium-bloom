// ------------------------------------------------------------------------------------------------
// <copyright file="DisplayStringFormatter.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.Common
{
    using System.Globalization;

    /// <summary>
    /// Provides culture-independent conversion for values rendered in the user interface.
    /// </summary>
    public static class DisplayStringFormatter
    {
        /// <summary>
        /// Converts a value into an invariant display string.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The converted value, or an empty string when no value is available.</returns>
        public static string ToDisplayString(object value)
        {
            var displayString = Convert.ToString(value, CultureInfo.InvariantCulture);

            return displayString ?? string.Empty;
        }
    }
}
