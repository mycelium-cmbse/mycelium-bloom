// ------------------------------------------------------------------------------------------------
// <copyright file="PendingChange.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ChangeNotifications
{
    /// <summary>Holds an aggregate whose mutation and retention belong exclusively to the accumulator.</summary>
    /// <param name="Key">The duplicate identity.</param>
    /// <param name="Event">The immutable event aggregate.</param>
    /// <param name="Generation">The notification generation at admission.</param>
    internal sealed record PendingChange(CommitElement Key, ChangeEvent Event, long Generation)
    {
        /// <summary>Gets or sets the aggregate until the accumulator freezes its batch.</summary>
        internal ChangeEvent Event { get; set; } = Event;

        /// <summary>Gets or sets the expiry assigned after delivery completes.</summary>
        internal DateTimeOffset ExpiresAt { get; set; }

        /// <summary>Gets or sets whether compatible echoes may still contribute metadata.</summary>
        internal bool IsPending { get; set; } = true;
    }
}
