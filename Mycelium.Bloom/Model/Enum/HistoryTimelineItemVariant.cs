// ------------------------------------------------------------------------------------------------
// <copyright file="HistoryTimelineItemVariant.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Enum
{
    /// <summary>
    /// Defines the visual variant of a history timeline item.
    /// </summary>
    public enum HistoryTimelineItemVariant
    {
        /// <summary>
        /// The neutral history item variant.
        /// </summary>
        Neutral,

        /// <summary>
        /// The created history item variant.
        /// </summary>
        Created,

        /// <summary>
        /// The updated history item variant.
        /// </summary>
        Updated,

        /// <summary>
        /// The deleted history item variant.
        /// </summary>
        Deleted,

        /// <summary>
        /// The commented history item variant.
        /// </summary>
        Commented,

        /// <summary>
        /// The reviewed history item variant.
        /// </summary>
        Reviewed,

        /// <summary>
        /// The synced history item variant.
        /// </summary>
        Synced
    }
}
