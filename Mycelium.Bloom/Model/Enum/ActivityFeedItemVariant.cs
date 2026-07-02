// ------------------------------------------------------------------------------------------------
// <copyright file="ActivityFeedItemVariant.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Enum
{
    /// <summary>
    /// Defines the visual variant of an activity feed item.
    /// </summary>
    public enum ActivityFeedItemVariant
    {
        /// <summary>
        /// The neutral activity item variant.
        /// </summary>
        Neutral,

        /// <summary>
        /// The created activity item variant.
        /// </summary>
        Created,

        /// <summary>
        /// The updated activity item variant.
        /// </summary>
        Updated,

        /// <summary>
        /// The deleted activity item variant.
        /// </summary>
        Deleted,

        /// <summary>
        /// The commented activity item variant.
        /// </summary>
        Commented,

        /// <summary>
        /// The reviewed activity item variant.
        /// </summary>
        Reviewed,

        /// <summary>
        /// The synced activity item variant.
        /// </summary>
        Synced,

        /// <summary>
        /// The joined activity item variant.
        /// </summary>
        Joined,

        /// <summary>
        /// The left activity item variant.
        /// </summary>
        Left
    }
}
