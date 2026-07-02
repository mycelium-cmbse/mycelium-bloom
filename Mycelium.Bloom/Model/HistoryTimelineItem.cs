// ------------------------------------------------------------------------------------------------
// <copyright file="HistoryTimelineItem.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Represents an item rendered by the history timeline.
    /// </summary>
    public class HistoryTimelineItem
    {
        /// <summary>
        /// Gets or sets the history item identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the history item title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional history item description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name of the actor.
        /// </summary>
        public string ActorName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the initials rendered in the actor avatar.
        /// </summary>
        public string ActorInitials { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the actor avatar color.
        /// </summary>
        public string ActorColor { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display text for when the history item occurred.
        /// </summary>
        public string TimestampText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the visual variant of the history item.
        /// </summary>
        public HistoryTimelineItemVariant Variant { get; set; } = HistoryTimelineItemVariant.Neutral;
    }
}
