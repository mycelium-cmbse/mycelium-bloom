// ------------------------------------------------------------------------------------------------
// <copyright file="ChangeEvent.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ChangeNotifications
{
    using System.Collections.Immutable;

    /// <summary>
    /// Describes one committed element change independently of transport and live model objects.
    /// </summary>
    public sealed record ChangeEvent
    {
        /// <summary>Creates and validates an immutable committed change.</summary>
        /// <param name="kind">The kind of committed change.</param>
        /// <param name="source">The mutation origin, independently of its transport.</param>
        /// <param name="elementId">The non-empty stable element identifier.</param>
        /// <param name="elementType">The generated SysML POCO metaclass or metaclass interface.</param>
        /// <param name="containment">The current ancestry, or the last ancestry before deletion.</param>
        /// <param name="changedProperties">Changed property names, with an empty sequence meaning unspecified properties.</param>
        /// <param name="commitId">The non-empty backend commit identifier.</param>
        /// <param name="previousContainment">The complete pre-move ancestry, required only for moved events.</param>
        /// <exception cref="ArgumentException">The change contains an invalid identifier, type, property or containment snapshot.</exception>
        public ChangeEvent(
            ChangeKind kind,
            ChangeSource source,
            Guid elementId,
            Type elementType,
            NamespacePath containment,
            IEnumerable<string> changedProperties,
            Guid commitId,
            NamespacePath previousContainment = null)
        {
            if (!Enum.IsDefined(kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (!Enum.IsDefined(source))
            {
                throw new ArgumentOutOfRangeException(nameof(source));
            }

            if (elementId == Guid.Empty)
            {
                throw new ArgumentException("An element identifier is required.", nameof(elementId));
            }

            if (commitId == Guid.Empty)
            {
                throw new ArgumentException("A commit identifier is required.", nameof(commitId));
            }

            ArgumentNullException.ThrowIfNull(containment);
            ArgumentNullException.ThrowIfNull(changedProperties);

            if ((kind == ChangeKind.Moved) != (previousContainment != null))
            {
                throw new ArgumentException("Previous containment is required exactly for moved events.", nameof(previousContainment));
            }

            if (containment.NamespaceIds.Contains(elementId))
            {
                throw new ArgumentException("An element cannot contain itself.", nameof(containment));
            }

            if (previousContainment?.NamespaceIds.Contains(elementId) == true)
            {
                throw new ArgumentException("An element cannot previously contain itself.", nameof(previousContainment));
            }

            this.ChangedProperties = changedProperties.ToImmutableSortedSet(StringComparer.Ordinal);

            if (this.ChangedProperties.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Changed property names must not be blank.", nameof(changedProperties));
            }

            SysmlMetaclass.Normalize(elementType);
            this.Kind = kind;
            this.Source = source;
            this.ElementId = elementId;
            this.ElementType = elementType;
            this.Containment = containment;
            this.PreviousContainment = previousContainment;
            this.CommitId = commitId;
        }

        /// <summary>Gets the committed change kind.</summary>
        public ChangeKind Kind { get; }

        /// <summary>Gets the mutation origin.</summary>
        public ChangeSource Source { get; }

        /// <summary>Gets the stable element identifier.</summary>
        public Guid ElementId { get; }

        /// <summary>Gets the generated SysML POCO metaclass or metaclass interface.</summary>
        public Type ElementType { get; }

        /// <summary>Gets the nearest containing namespace, or null for an uncontained element.</summary>
        public Guid? ParentNamespaceId => this.Containment.ParentNamespaceId;

        /// <summary>Gets the current ancestry, or the final ancestry before deletion.</summary>
        public NamespacePath Containment { get; }

        /// <summary>Gets the complete pre-move ancestry, or null for other kinds.</summary>
        public NamespacePath PreviousContainment { get; }

        /// <summary>Gets immutable property names, with an empty set indicating unspecified properties.</summary>
        public ImmutableSortedSet<string> ChangedProperties { get; }

        /// <summary>Gets the backend commit identifier.</summary>
        public Guid CommitId { get; }

        /// <summary>Merges compatible pending echoes without discarding origin, structural or unspecified-property semantics.</summary>
        /// <param name="other">Another notification for the same committed element change.</param>
        /// <returns>The merged notification with a canonical metaclass and order-independent metadata.</returns>
        /// <exception cref="ArgumentException">The duplicate disagrees on identity, kind, metaclass or containment.</exception>
        internal ChangeEvent Coalesce(ChangeEvent other)
        {
            if (this.CommitId != other.CommitId || this.ElementId != other.ElementId || this.Kind != other.Kind
                || SysmlMetaclass.Normalize(this.ElementType) != SysmlMetaclass.Normalize(other.ElementType)
                || !this.Containment.NamespaceIds.SequenceEqual(other.Containment.NamespaceIds)
                || (this.PreviousContainment != null
                    && !this.PreviousContainment.NamespaceIds.SequenceEqual(other.PreviousContainment.NamespaceIds)))
            {
                throw new ArgumentException("Duplicate notifications must agree on identity, kind, metaclass and containment.", nameof(other));
            }

            var properties = this.ChangedProperties.IsEmpty || other.ChangedProperties.IsEmpty
                ? ImmutableSortedSet<string>.Empty
                : this.ChangedProperties.Union(other.ChangedProperties);

            return new ChangeEvent(this.Kind,
                this.Source == ChangeSource.Local || other.Source == ChangeSource.Local ? ChangeSource.Local : ChangeSource.Remote,
                this.ElementId, SysmlMetaclass.Normalize(this.ElementType), this.Containment, properties, this.CommitId,
                this.PreviousContainment);
        }
    }
}
