// ------------------------------------------------------------------------------------------------
// <copyright file="CountingSynchronizationContext.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.Organization
{
    using System.Threading;

    /// <summary>
    /// Runs every posted callback at once and counts them, to tell whether work was handed to the context.
    /// </summary>
    internal sealed class CountingSynchronizationContext : SynchronizationContext
    {
        private int posts;

        /// <summary>
        /// Gets the number of callbacks posted to this context.
        /// </summary>
        public int Posts => Volatile.Read(ref this.posts);

        /// <summary>
        /// Counts the callback, then runs it.
        /// </summary>
        /// <param name="d">The callback.</param>
        /// <param name="state">The state passed to the callback.</param>
        public override void Post(SendOrPostCallback d, object state)
        {
            Interlocked.Increment(ref this.posts);
            d(state);
        }
    }
}
