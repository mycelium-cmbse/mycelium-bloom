// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceModelLease.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    /// <summary>Retains one canonical model context for a tab or an unfinished opening operation.</summary>
    public sealed class WorkspaceModelLease : IDisposable
    {
        /// <summary>Releases this lease exactly once through the owning model session.</summary>
        private Action release;

        /// <summary>Gets the immutable model identity and canonical scope.</summary>
        public WorkspaceModelSnapshot Snapshot { get; }

        /// <summary>Retains one consumer of an accepted canonical model context.</summary>
        /// <param name="snapshot">The accepted model identity and root.</param>
        /// <param name="release">Releases this consumer from the owning session.</param>
        internal WorkspaceModelLease(WorkspaceModelSnapshot snapshot, Action release)
        {
            this.Snapshot = snapshot;
            this.release = release;
        }

        /// <summary>Releases this consumer without affecting other consumers.</summary>
        public void Dispose() => Interlocked.Exchange(ref this.release, null)?.Invoke();
    }
}
