// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramCanvasSession.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram
{
    using System.Collections.Immutable;

    using Mycelium.Bloom.Model.Diagram;

    /// <summary>Validates one mounted renderer's callbacks without owning durable diagram state.</summary>
    public sealed class DiagramCanvasSession : IDisposable
    {
        /// <summary>The tab-owned authority.</summary>
        private readonly DiagramViewModel viewModel;

        /// <summary>Serializes command validation and session disposal.</summary>
        private readonly object gate = new();

        /// <summary>Cancels canonical resolution when this renderer unmounts.</summary>
        private readonly CancellationTokenSource lifetime = new();

        /// <summary>The unique mount identity.</summary>
        private readonly Guid id = Guid.NewGuid();

        /// <summary>The last projected immutable state reference.</summary>
        private DiagramPresentationState observed;

        /// <summary>The last projected permission.</summary>
        private bool canEdit;

        /// <summary>The current presentation revision.</summary>
        private long revision;

        /// <summary>The last processed browser command.</summary>
        private long sequence;

        /// <summary>Whether this mount has ended.</summary>
        private bool disposed;

        /// <summary>Creates a mount-scoped validation boundary.</summary>
        /// <param name="viewModel">The caller-owned authoritative state.</param>
        public DiagramCanvasSession(DiagramViewModel viewModel)
        {
            this.viewModel = viewModel;
        }

        /// <summary>Captures a bounded, acyclic snapshot or null after disposal.</summary>
        /// <returns>The current renderer projection.</returns>
        public DiagramCanvasSnapshot Capture()
        {
            lock (this.gate)
            {
                if (this.disposed)
                {
                    return null;
                }

                var state = this.viewModel.State;
                var permission = this.viewModel.CanEditPresentation;
                if (!ReferenceEquals(this.observed, state) || this.canEdit != permission)
                {
                    this.observed = state;
                    this.canEdit = permission;
                    this.revision++;
                }

                return new(this.id, this.revision, this.sequence, state, this.viewModel.Projection,
                    permission, this.viewModel.SelectedOccurrences);
            }
        }

        /// <summary>Accepts only the next completed command for this mount and revision.</summary>
        /// <param name="change">The browser's bounded proposal.</param>
        /// <returns>The decision and recovery state.</returns>
        public DiagramCanvasResult Apply(DiagramCanvasChange change)
        {
            lock (this.gate)
            {
                var snapshot = this.Capture();
                if (snapshot is null)
                {
                    return new(false, null);
                }

                if (change is null || change.SessionId != this.id || change.Revision != this.revision
                    || change.Sequence != this.sequence + 1)
                {
                    return new(false, snapshot);
                }

                this.sequence = change.Sequence;
                var accepted = this.viewModel.ApplyCanvasChange(snapshot.State, change);
                return new(accepted, this.Capture());
            }
        }

        /// <summary>Validates occurrence selection before canonical resolution.</summary>
        /// <param name="sessionId">The mounted renderer identity.</param>
        /// <param name="ids">The complete local occurrence selection.</param>
        /// <param name="primary">The selected occurrence used by Details.</param>
        /// <returns>A task completing selection publication.</returns>
        public Task SelectAsync(Guid sessionId, Guid[] ids, Guid? primary)
        {
            lock (this.gate)
            {
                if (this.disposed || sessionId != this.id || ids is null || ids.Length > 2000
                    || ids.Distinct().Count() != ids.Length || (primary.HasValue && !ids.Contains(primary.Value)))
                {
                    return Task.CompletedTask;
                }

                var valid = this.viewModel.State.Occurrences.Select(node => node.Id)
                    .Concat(this.viewModel.State.Routes.Select(route => route.Id)).ToHashSet();
                return ids.Any(id => !valid.Contains(id)) ? Task.CompletedTask
                    : this.viewModel.SelectAsync(ids.ToImmutableHashSet(), primary, this.lifetime.Token);
            }
        }

        /// <summary>Rejects late callbacks and cancels mount-owned selections.</summary>
        public void Dispose()
        {
            lock (this.gate)
            {
                if (this.disposed)
                {
                    return;
                }

                this.disposed = true;
                this.lifetime.Cancel();
                this.lifetime.Dispose();
            }
        }
    }
}
