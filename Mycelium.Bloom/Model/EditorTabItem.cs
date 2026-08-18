// ------------------------------------------------------------------------------------------------
// <copyright file="EditorTabItem.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    /// <summary>
    /// Represents one independently identified tab instance in an editor group.
    /// </summary>
    public sealed class EditorTabItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EditorTabItem" /> class.
        /// </summary>
        /// <param name="title">The title presented for the tab.</param>
        /// <param name="viewTypeKey">The rendering-neutral key identifying the kind of view.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="title" /> or <paramref name="viewTypeKey" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="title" /> or <paramref name="viewTypeKey" /> is empty or consists only of
        /// whitespace.
        /// </exception>
        internal EditorTabItem(string title, string viewTypeKey)
        {
            ValidateMetadata(title, viewTypeKey);

            this.Id = Guid.NewGuid();
            this.Title = title;
            this.ViewTypeKey = viewTypeKey;
        }

        /// <summary>
        /// Gets the immutable identity of this tab instance.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the title presented for the tab.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Gets the rendering-neutral key identifying the kind of view.
        /// </summary>
        public string ViewTypeKey { get; }

        /// <summary>
        /// Validates the immutable metadata required to create a tab instance.
        /// </summary>
        /// <param name="title">The title presented for the tab.</param>
        /// <param name="viewTypeKey">The rendering-neutral key identifying the kind of view.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="title" /> or <paramref name="viewTypeKey" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="title" /> or <paramref name="viewTypeKey" /> is empty or consists only of
        /// whitespace.
        /// </exception>
        internal static void ValidateMetadata(string title, string viewTypeKey)
        {
            ArgumentNullException.ThrowIfNull(title);
            ArgumentNullException.ThrowIfNull(viewTypeKey);

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("The tab title cannot be empty or whitespace.", nameof(title));
            }

            if (string.IsNullOrWhiteSpace(viewTypeKey))
            {
                throw new ArgumentException("The view type key cannot be empty or whitespace.", nameof(viewTypeKey));
            }
        }
    }
}
