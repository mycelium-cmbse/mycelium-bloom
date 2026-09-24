// ------------------------------------------------------------------------------------------------
// <copyright file="ChangeAccumulator.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ChangeNotifications
{
    using System.Reactive.Concurrency;

    /// <summary>Owns synchronous duplicate validation, pending batches and bounded delivered-key retention.</summary>
    /// <param name="scheduler">The deterministic expiry clock.</param>
    internal sealed class ChangeAccumulator(IScheduler scheduler)
    {
        /// <summary>Defines retention after delivery rather than admission.</summary>
        private static readonly TimeSpan DeduplicationWindow = TimeSpan.FromSeconds(2);

        /// <summary>Bounds delivered keys without evicting pending work.</summary>
        private const int MaximumRememberedChanges = 4096;

        /// <summary>Serializes synchronous producer validation with batch freezing and retention.</summary>
        private readonly object gate = new();

        /// <summary>Indexes pending, frozen and remembered aggregates by committed identity.</summary>
        private readonly Dictionary<CommitElement, PendingChange> changes = new();

        /// <summary>Orders expiry and capacity eviction of delivered changes only.</summary>
        private readonly Queue<PendingChange> delivered = new();

        /// <summary>Accumulates the next batch in admission order.</summary>
        private List<PendingChange> pending = [];

        /// <summary>Identifies work invalidated by disabling or disposal.</summary>
        private long generation;

        /// <summary>Reserves exactly one window trigger until the next batch is taken.</summary>
        private bool windowRequested;

        /// <summary>Controls admission and delivery independently of subscriptions.</summary>
        private bool isEnabled = true;

        /// <summary>Closes admission permanently.</summary>
        private bool isDisposed;

        /// <summary>Gets or sets admission, discarding pending and remembered work when disabled.</summary>
        internal bool IsEnabled
        {
            get
            {
                lock (this.gate)
                {
                    return this.isEnabled && !this.isDisposed;
                }
            }
            set
            {
                lock (this.gate)
                {
                    ObjectDisposedException.ThrowIf(this.isDisposed, typeof(ChangeNotificationService));
                    if (this.isEnabled == value)
                    {
                        return;
                    }
                    this.isEnabled = value;
                    if (!value)
                    {
                        this.Clear();
                    }
                }
            }
        }

        /// <summary>Admits or merges an event and reserves a window without invoking the scheduler pipeline.</summary>
        /// <param name="change">The validated immutable event.</param>
        /// <returns>Whether the caller must enqueue a new batching window after admission returns.</returns>
        internal bool Accept(ChangeEvent change)
        {
            lock (this.gate)
            {
                ObjectDisposedException.ThrowIf(this.isDisposed, typeof(ChangeNotificationService));
                if (!this.isEnabled)
                {
                    return false;
                }
                this.Expire(scheduler.Now);
                var key = new CommitElement(change.CommitId, change.ElementId);
                if (this.changes.TryGetValue(key, out var existing))
                {
                    if (existing.IsPending)
                    {
                        existing.Event = existing.Event.Coalesce(change);
                    }
                    return false;
                }
                var admitted = new PendingChange(key, change, this.generation);
                this.changes.Add(key, admitted);
                this.pending.Add(admitted);
                if (this.windowRequested)
                {
                    return false;
                }
                this.windowRequested = true;
                return true;
            }
        }

        /// <summary>Freezes the pending aggregates and permits the next producer to request a window.</summary>
        /// <returns>The frozen batch in admission order.</returns>
        internal List<PendingChange> TakeBatch()
        {
            lock (this.gate)
            {
                var batch = this.pending;
                this.pending = [];
                this.windowRequested = false;
                foreach (var change in batch)
                {
                    change.IsPending = false;
                }
                return batch;
            }
        }

        /// <summary>Checks whether a frozen aggregate still belongs to the enabled service lifetime.</summary>
        /// <param name="change">The frozen aggregate.</param>
        /// <returns>Whether delivery may continue.</returns>
        internal bool CanDeliver(PendingChange change)
        {
            lock (this.gate)
            {
                return !this.isDisposed && this.isEnabled && change.Generation == this.generation;
            }
        }

        /// <summary>Starts expiry and capacity retention only after all target deliveries finish.</summary>
        /// <param name="change">The delivered aggregate.</param>
        internal void Remember(PendingChange change)
        {
            lock (this.gate)
            {
                if (this.isDisposed || !this.isEnabled || change.Generation != this.generation)
                {
                    return;
                }
                var now = scheduler.Now;
                this.Expire(now);
                if (this.delivered.Count == MaximumRememberedChanges)
                {
                    this.changes.Remove(this.delivered.Dequeue().Key);
                }
                change.ExpiresAt = now + DeduplicationWindow;
                this.delivered.Enqueue(change);
            }
        }

        /// <summary>Rejects new API work after final disposal.</summary>
        internal void ThrowIfDisposed()
        {
            lock (this.gate)
            {
                ObjectDisposedException.ThrowIf(this.isDisposed, typeof(ChangeNotificationService));
            }
        }

        /// <summary>Closes admission and drops all retained work once.</summary>
        /// <returns>Whether this call closed the accumulator.</returns>
        internal bool Stop()
        {
            lock (this.gate)
            {
                if (this.isDisposed)
                {
                    return false;
                }
                this.isDisposed = true;
                this.Clear();
                return true;
            }
        }

        /// <summary>Invalidates snapshots and releases accumulated state under its owning gate.</summary>
        private void Clear()
        {
            this.generation++;
            this.changes.Clear();
            this.delivered.Clear();
            this.pending.Clear();
        }

        /// <summary>Evicts expired delivered identities during active work only.</summary>
        /// <param name="now">The injected scheduler's current time.</param>
        private void Expire(DateTimeOffset now)
        {
            while (this.delivered.TryPeek(out var oldest) && oldest.ExpiresAt <= now)
            {
                this.changes.Remove(this.delivered.Dequeue().Key);
            }
        }
    }
}
