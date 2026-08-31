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
    using ReactiveUI;

    /// <summary>
    /// Represents one independently identified tab instance in an editor group.
    /// </summary>
    public sealed class EditorTabItem : ReactiveObject
    {
        /// <summary>
        /// The title presented for the tab.
        /// </summary>
        private string title;

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
            this.title = title;
            this.ViewTypeKey = viewTypeKey;
        }

        /// <summary>
        /// Gets the immutable identity of this tab instance.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets or sets the title presented for the tab.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the assigned value is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the assigned value is empty or consists only of whitespace.
        /// </exception>
        public string Title
        {
            get => this.title;
            set
            {
                ValidateTitle(value);
                this.RaiseAndSetIfChanged(ref this.title, value);
            }
        }

        /// <summary>
        /// Gets the rendering-neutral key identifying the kind of view.
        /// </summary>
        public string ViewTypeKey { get; }

        /// <summary>
        /// Validates the metadata required to create a tab instance.
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

            ValidateTitle(title);

            if (string.IsNullOrWhiteSpace(viewTypeKey))
            {
                throw new ArgumentException("The view type key cannot be empty or whitespace.", nameof(viewTypeKey));
            }
        }

        /// <summary>
        /// Validates a tab title before it becomes durable tab state.
        /// </summary>
        /// <param name="title">The title presented for the tab.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="title" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="title" /> is empty or consists only of whitespace.
        /// </exception>
        private static void ValidateTitle(string title)
        {
            ArgumentNullException.ThrowIfNull(title);

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("The tab title cannot be empty or whitespace.", nameof(title));
            }
        }
    }
}
