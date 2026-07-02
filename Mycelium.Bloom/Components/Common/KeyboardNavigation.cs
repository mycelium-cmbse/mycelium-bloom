// ------------------------------------------------------------------------------------------------
// <copyright file="KeyboardNavigation.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.Common
{
    /// <summary>
    /// Provides helpers for roving keyboard navigation across child items.
    /// </summary>
    public static class KeyboardNavigation
    {
        /// <summary>
        /// Gets the first enabled item index.
        /// </summary>
        /// <typeparam name="TItem">The item type.</typeparam>
        /// <param name="items">The available items.</param>
        /// <param name="isEnabled">The predicate used to determine whether an item is enabled.</param>
        /// <returns>The first enabled item index; otherwise, <c>null</c>.</returns>
        public static int? GetFirstEnabledIndex<TItem>(IReadOnlyList<TItem> items, Func<TItem, bool> isEnabled)
        {
            for (var index = 0; index < items.Count; index++)
            {
                if (isEnabled(items[index]))
                {
                    return index;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the last enabled item index.
        /// </summary>
        /// <typeparam name="TItem">The item type.</typeparam>
        /// <param name="items">The available items.</param>
        /// <param name="isEnabled">The predicate used to determine whether an item is enabled.</param>
        /// <returns>The last enabled item index; otherwise, <c>null</c>.</returns>
        public static int? GetLastEnabledIndex<TItem>(IReadOnlyList<TItem> items, Func<TItem, bool> isEnabled)
        {
            for (var index = items.Count - 1; index >= 0; index--)
            {
                if (isEnabled(items[index]))
                {
                    return index;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the next enabled item index, wrapping at the list boundaries.
        /// </summary>
        /// <typeparam name="TItem">The item type.</typeparam>
        /// <param name="items">The available items.</param>
        /// <param name="currentIndex">The current item index.</param>
        /// <param name="direction">The navigation direction. Use <c>1</c> for forward and <c>-1</c> for backward.</param>
        /// <param name="isEnabled">The predicate used to determine whether an item is enabled.</param>
        /// <returns>The next enabled item index; otherwise, <c>null</c>.</returns>
        public static int? GetNextEnabledIndex<TItem>(IReadOnlyList<TItem> items, int currentIndex, int direction, Func<TItem, bool> isEnabled)
        {
            if (items.Count == 0 || direction == 0)
            {
                return null;
            }

            var normalizedIndex = ((currentIndex % items.Count) + items.Count) % items.Count;

            for (var step = 1; step <= items.Count; step++)
            {
                var index = (normalizedIndex + (step * direction) + items.Count) % items.Count;

                if (isEnabled(items[index]))
                {
                    return index;
                }
            }

            return null;
        }
    }
}
