// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramNodeComponentRegistry.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram
{
    /// <summary>Dispatches typed presentations using the most specific registered component.</summary>
    public sealed class DiagramNodeComponentRegistry
    {
        /// <summary>The immutable registration inventory, with later exact-type registrations taking precedence.</summary>
        private readonly DiagramNodeComponentRegistration[] registrations;

        /// <summary>Builds the presentation renderer map from DI registrations.</summary>
        /// <param name="registrations">The component mappings in registration order.</param>
        /// <exception cref="ArgumentNullException">The registration inventory is null.</exception>
        public DiagramNodeComponentRegistry(IEnumerable<DiagramNodeComponentRegistration> registrations)
        {
            ArgumentNullException.ThrowIfNull(registrations);
            this.registrations = registrations.GroupBy(registration => registration.PresentationType).Select(group => group.Last()).ToArray();
        }

        /// <summary>Finds the most specific renderer for a presentation type.</summary>
        /// <param name="presentationType">The runtime presentation type.</param>
        /// <returns>The registered Razor component type.</returns>
        /// <exception cref="InvalidOperationException">No registered component accepts the presentation.</exception>
        public Type GetComponentType(Type presentationType)
        {
            var matches = this.registrations.Where(registration => registration.PresentationType.IsAssignableFrom(presentationType)).ToArray();

            return matches.FirstOrDefault(candidate => !matches.Any(other => other.PresentationType != candidate.PresentationType
                && candidate.PresentationType.IsAssignableFrom(other.PresentationType)))?.ComponentType
                ?? throw new InvalidOperationException("No diagram component accepts this presentation type.");
        }
    }
}
