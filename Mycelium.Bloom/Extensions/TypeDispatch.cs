// ------------------------------------------------------------------------------------------------
// <copyright file="TypeDispatch.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Extensions
{
    /// <summary>Selects an assignable registration before its broader fallbacks.</summary>
    public static class TypeDispatch
    {
        /// <summary>Finds a most-specific match, retaining registration order for equal or unrelated candidates.</summary>
        /// <typeparam name="T">The registration value type.</typeparam>
        /// <param name="registrations">The ordered registration inventory.</param>
        /// <param name="actualType">The runtime type being dispatched.</param>
        /// <param name="getType">Gets each registration's accepted type.</param>
        /// <returns>The first most-specific registration, or the default value when none matches.</returns>
        public static T Find<T>(IEnumerable<T> registrations, Type actualType, Func<T, Type> getType)
        {
            ArgumentNullException.ThrowIfNull(registrations);
            ArgumentNullException.ThrowIfNull(actualType);
            ArgumentNullException.ThrowIfNull(getType);
            var matches = registrations.Where(entry => getType(entry).IsAssignableFrom(actualType)).ToArray();

            return matches.FirstOrDefault(candidate => !matches.Any(other => getType(other) != getType(candidate)
                && getType(candidate).IsAssignableFrom(getType(other))));
        }
    }
}
