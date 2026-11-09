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
        private Action release;

        internal WorkspaceModelLease(WorkspaceModelSnapshot snapshot, Action release)
        {
            this.Snapshot = snapshot;
            this.release = release;
        }

        /// <summary>Gets the immutable model identity and canonical scope.</summary>
        public WorkspaceModelSnapshot Snapshot { get; }

        /// <summary>Releases this consumer without affecting other consumers.</summary>
        public void Dispose() => Interlocked.Exchange(ref this.release, null)?.Invoke();
    }
}
