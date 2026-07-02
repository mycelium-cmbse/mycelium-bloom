// ------------------------------------------------------------------------------------------------
// <copyright file="CommentThreadItem.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    /// <summary>
    /// Represents a collaboration comment rendered by a comment thread.
    /// </summary>
    public class CommentThreadItem
    {
        /// <summary>
        /// Gets or sets the comment identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name of the comment author.
        /// </summary>
        public string AuthorName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the initials rendered in the author avatar.
        /// </summary>
        public string AuthorInitials { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the author avatar color.
        /// </summary>
        public string AuthorColor { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display text for when the comment was created.
        /// </summary>
        public string CreatedAtText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the comment body.
        /// </summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the comment was authored by the current user.
        /// </summary>
        public bool IsCurrentUser { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the comment has been edited.
        /// </summary>
        public bool IsEdited { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the comment has been resolved.
        /// </summary>
        public bool IsResolved { get; set; }
    }
}
