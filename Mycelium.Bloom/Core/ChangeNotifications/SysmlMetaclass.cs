// ------------------------------------------------------------------------------------------------
// <copyright file="SysmlMetaclass.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ChangeNotifications
{
    using System.Collections.Concurrent;
    using System.Collections.Immutable;
    using System.Reflection;

    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Decorators;

    /// <summary>Resolves generated POCO metaclasses through the SDK's interface inheritance and metadata.</summary>
    internal static class SysmlMetaclass
    {
        /// <summary>Caches canonical interface and ancestry together for each validated SDK type.</summary>
        private static readonly ConcurrentDictionary<Type, ImmutableArray<Type>> Hierarchies = new();

        /// <summary>Normalizes an SDK POCO class or interface to its metaclass interface.</summary>
        /// <param name="type">The generated SDK metaclass type.</param>
        /// <returns>The equivalent SDK metaclass interface.</returns>
        /// <exception cref="ArgumentException">The type is not a generated SDK POCO metaclass.</exception>
        internal static Type Normalize(Type type) => GetHierarchy(type)[0];

        /// <summary>Gets the metaclass and all its SDK metaclass superinterfaces.</summary>
        /// <param name="type">The generated SDK metaclass type.</param>
        /// <returns>The cached interface ancestry including the metaclass itself.</returns>
        internal static ImmutableArray<Type> GetHierarchy(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);
            return Hierarchies.GetOrAdd(type, ResolveHierarchy);
        }

        /// <summary>Matches the supplied SDK class to its implemented interface through their shared XMI identifier.</summary>
        /// <param name="type">The supplied generated type.</param>
        /// <returns>The canonical interface followed by its generated superinterfaces.</returns>
        private static ImmutableArray<Type> ResolveHierarchy(Type type)
        {
            var metadata = GetMetadata(type);
            if (metadata == null)
            {
                throw new ArgumentException("A generated SysML POCO metaclass or metaclass interface is required.", nameof(type));
            }
            var canonical = type.IsInterface ? type : type.GetInterfaces()
                .Single(candidate => GetMetadata(candidate)?.XmiId == metadata.XmiId);
            return [canonical, .. canonical.GetInterfaces().Where(candidate => GetMetadata(candidate) != null)];
        }

        /// <summary>Accepts only attributed POCO metaclasses from the pinned SDK assembly.</summary>
        /// <param name="type">The candidate generated type.</param>
        /// <returns>The SDK metadata, or null for a non-metaclass.</returns>
        private static ClassAttribute GetMetadata(Type type)
            => type.Assembly == typeof(IElement).Assembly && typeof(IElement).IsAssignableFrom(type)
                ? type.GetCustomAttribute<ClassAttribute>(false)
                : null;
    }
}
