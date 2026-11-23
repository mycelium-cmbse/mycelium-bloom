// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramToolRuntimeProvider.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram.Tools
{
    using System.Collections.Immutable;

    using Mycelium.Bloom.ViewModel.Diagram.Tools;

    /// <summary>Resolves renderer-owned runtime instances from their tool IDs.</summary>
    public sealed class DiagramToolRuntimeProvider : IDiagramToolRuntimeProvider
    {
        /// <summary>Maps stable identifiers to their registered factories.</summary>
        private readonly ImmutableDictionary<string, DiagramToolRuntimeRegistration> registrations;

        /// <summary>Indexes the factories without creating or retaining runtime instances.</summary>
        /// <param name="registrations">The runtime factories registered in DI.</param>
        public DiagramToolRuntimeProvider(IEnumerable<DiagramToolRuntimeRegistration> registrations)
        {
            ArgumentNullException.ThrowIfNull(registrations);
            this.registrations = registrations.ToImmutableDictionary(registration => registration.ToolId, StringComparer.Ordinal);
        }

        /// <summary>Creates the runtime configured for the specified placement.</summary>
        /// <param name="placement">The applicable placement and its immutable configuration.</param>
        /// <param name="context">The requesting renderer's native diagram context.</param>
        /// <returns>A runtime owned by the requesting renderer.</returns>
        /// <exception cref="ArgumentException">The placement does not supply the required parameter type.</exception>
        /// <exception cref="InvalidOperationException">The tool has no usable runtime factory.</exception>
        public IDiagramTool Create(DiagramToolPlacement placement, DiagramToolContext context)
        {
            if (!placement.Tool.SupportsParameters(placement.Item.Parameters))
            {
                throw new ArgumentException("The diagram tool placement has incompatible parameters.", nameof(placement));
            }

            return this.registrations.TryGetValue(placement.Tool.Id, out var registration) && registration.Create(context, placement.Item.Parameters) is { } runtime
                ? runtime
                : throw new InvalidOperationException($"No runtime is registered for diagram tool '{placement.Tool.Id}'.");
        }
    }
}
