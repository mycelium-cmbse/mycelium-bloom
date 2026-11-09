// ------------------------------------------------------------------------------------------------
// <copyright file="IDiagramModelSource.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ModelLoading
{
    using System.Collections.Immutable;
    using Mycelium.Bloom.Model.Diagram;

    /// <summary>Identifies an immutable, authorization-scoped model read.</summary>
    /// <param name="Source">The source identity.</param>
    /// <param name="Model">The project/model identity.</param>
    /// <param name="Revision">The source's immutable revision.</param>
    /// <param name="Authorization">The source's authorization scope.</param>
    public sealed record DiagramReadScope(string Source, string Model, string Revision, string Authorization);

    /// <summary>Supplies complete dependency bundles without requiring a complete model graph.</summary>
    public interface IDiagramModelSource
    {
        /// <summary>Gets the immutable source scope.</summary>
        DiagramReadScope Scope { get; }

        /// <summary>Gets lightweight identity, port and endpoint metadata without semantic details.</summary>
        DiagramProjection Metadata { get; }

        /// <summary>Gets initial full-scene presentation metadata.</summary>
        DiagramPresentationState Presentation { get; }

        /// <summary>Gets the bounded dependency bundle owning each semantic identity.</summary>
        ImmutableDictionary<string, string> Bundles { get; }

        /// <summary>Checks current read access, including cache hits.</summary>
        /// <param name="scope">The requested immutable scope.</param>
        /// <param name="cancellationToken">The caller lifetime.</param>
        /// <returns>A task completing access validation or throwing when access is denied.</returns>
        ValueTask AuthorizeAsync(DiagramReadScope scope, CancellationToken cancellationToken);

        /// <summary>Fetches complete SDK JSON dependency bundles at the requested revision.</summary>
        /// <param name="scope">The immutable read scope.</param>
        /// <param name="bundleIds">Canonical root identities requested in one bounded batch.</param>
        /// <param name="cancellationToken">The source-session lifetime.</param>
        /// <returns>Payloads keyed by bundle root identity, with no omitted successful entries.</returns>
        Task<IReadOnlyDictionary<string, string>> FetchAsync(DiagramReadScope scope, IReadOnlyList<string> bundleIds, CancellationToken cancellationToken);
    }
}
