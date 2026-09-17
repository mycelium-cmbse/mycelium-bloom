// ------------------------------------------------------------------------------------------------
// <copyright file="ChangeTarget.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ChangeNotifications
{
    /// <summary>Provides value-based subscription keys with explicit target semantics.</summary>
    public sealed record ChangeTarget
    {
        /// <summary>Creates a validated subscription key.</summary>
        /// <param name="scope">The target scope.</param>
        /// <param name="id">The element or namespace identifier.</param>
        /// <param name="elementType">The canonical metaclass interface.</param>
        private ChangeTarget(TargetScope scope, Guid id = default, Type elementType = null)
        {
            this.Scope = scope;
            this.Id = id;
            this.ElementType = elementType;
        }

        /// <summary>Gets the target for all committed changes.</summary>
        public static ChangeTarget Global { get; } = new(TargetScope.Global);

        /// <summary>Gets the key's explicit scope.</summary>
        internal TargetScope Scope { get; }

        /// <summary>Gets the element or namespace identifier.</summary>
        internal Guid Id { get; }

        /// <summary>Gets the canonical metaclass interface.</summary>
        internal Type ElementType { get; }

        /// <summary>Creates a target for changes to exactly one element.</summary>
        /// <param name="elementId">The non-empty element identifier.</param>
        /// <returns>The element target.</returns>
        public static ChangeTarget Element(Guid elementId)
        {
            return CreateIdentifierTarget(TargetScope.Element, elementId);
        }

        /// <summary>Creates a target for a namespace itself and all its containment descendants.</summary>
        /// <param name="namespaceId">The non-empty namespace identifier.</param>
        /// <returns>The subtree target.</returns>
        public static ChangeTarget Subtree(Guid namespaceId)
        {
            return CreateIdentifierTarget(TargetScope.Subtree, namespaceId);
        }

        /// <summary>Creates a target for a SysML metaclass and its derived metaclasses.</summary>
        /// <param name="elementType">A generated SDK POCO class or metaclass interface.</param>
        /// <returns>The normalized metaclass target.</returns>
        public static ChangeTarget ForType(Type elementType)
        {
            return new ChangeTarget(TargetScope.Type, elementType: SysmlMetaclass.Normalize(elementType));
        }

        /// <summary>Validates and creates a target identified by a stable model identifier.</summary>
        /// <param name="scope">The element or subtree scope.</param>
        /// <param name="id">The non-empty identifier.</param>
        /// <returns>The validated target.</returns>
        private static ChangeTarget CreateIdentifierTarget(TargetScope scope, Guid id)
        {
            return id != Guid.Empty
                ? new ChangeTarget(scope, id)
                : throw new ArgumentException("A target identifier is required.", nameof(id));
        }

        /// <summary>Distinguishes target kinds without runtime object-shape checks.</summary>
        internal enum TargetScope
        {
            /// <summary>Matches every change.</summary>
            Global,
            /// <summary>Matches an element identifier.</summary>
            Element,
            /// <summary>Matches containment ancestry.</summary>
            Subtree,
            /// <summary>Matches metaclass ancestry.</summary>
            Type
        }
    }
}
