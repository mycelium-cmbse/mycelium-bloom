// ------------------------------------------------------------------------------------------------
// <copyright file="NamespacePath.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ChangeNotifications
{
    using System.Collections.Immutable;

    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;

    /// <summary>
    /// Captures every containing namespace from the nearest namespace to the model root.
    /// </summary>
    public sealed record NamespacePath
    {
        /// <summary>
        /// Creates a complete containment snapshot, using an empty sequence for an uncontained element.
        /// </summary>
        /// <param name="namespaceIds">The complete nearest-first namespace ancestry, excluding the changed element.</param>
        /// <exception cref="ArgumentException">An identifier is empty or repeated.</exception>
        public NamespacePath(IEnumerable<Guid> namespaceIds)
        {
            ArgumentNullException.ThrowIfNull(namespaceIds);
            this.NamespaceIds = namespaceIds.ToImmutableArray();

            if (this.NamespaceIds.Contains(Guid.Empty)
                || this.NamespaceIds.Distinct().Count() != this.NamespaceIds.Length)
            {
                throw new ArgumentException("Namespace ancestry must contain unique, non-empty identifiers.", nameof(namespaceIds));
            }
        }

        /// <summary>Gets the complete immutable nearest-first namespace ancestry.</summary>
        public ImmutableArray<Guid> NamespaceIds { get; }

        /// <summary>Gets the nearest containing namespace identifier, or null at the model root.</summary>
        public Guid? ParentNamespaceId => this.NamespaceIds.IsEmpty ? null : this.NamespaceIds[0];

        /// <summary>
        /// Captures SDK ownership while the producer holds a coherent model snapshot, before deletion or movement.
        /// </summary>
        /// <param name="element">The SDK element whose containing namespaces are captured.</param>
        /// <returns>An immutable snapshot that does not retain SDK objects.</returns>
        /// <exception cref="InvalidOperationException">The SDK ownership graph contains a cycle.</exception>
        public static NamespacePath Capture(IElement element)
        {
            ArgumentNullException.ThrowIfNull(element);

            var namespaces = new List<Guid>();
            var visited = new HashSet<IElement>(ReferenceEqualityComparer.Instance) { element };

            for (var owner = element.owner; owner != null; owner = owner.owner)
            {
                if (!visited.Add(owner))
                {
                    throw new InvalidOperationException("The element ownership graph contains a cycle.");
                }

                if (owner is INamespace)
                {
                    namespaces.Add(owner.Id);
                }
            }

            return new NamespacePath(namespaces);
        }
    }
}
