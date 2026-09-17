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
        /// <summary>Maps the finite SDK class/interface inventory to canonical metaclass interfaces.</summary>
        private static readonly Lazy<ImmutableDictionary<Type, Type>> CanonicalTypes = new(CreateCanonicalTypes);

        /// <summary>Caches ancestry only for validated SDK metaclass interfaces.</summary>
        private static readonly ConcurrentDictionary<Type, ImmutableArray<Type>> Hierarchies = new();

        /// <summary>Normalizes an SDK POCO class or interface to its metaclass interface.</summary>
        /// <param name="type">The generated SDK metaclass type.</param>
        /// <returns>The equivalent SDK metaclass interface.</returns>
        /// <exception cref="ArgumentException">The type is not a generated SDK POCO metaclass.</exception>
        internal static Type Normalize(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return CanonicalTypes.Value.TryGetValue(type, out var canonicalType)
                ? canonicalType
                : throw new ArgumentException("A generated SysML POCO metaclass or metaclass interface is required.", nameof(type));
        }

        /// <summary>Gets the metaclass and all its SDK metaclass superinterfaces.</summary>
        /// <param name="type">The generated SDK metaclass type.</param>
        /// <returns>The cached interface ancestry including the metaclass itself.</returns>
        internal static ImmutableArray<Type> GetHierarchy(Type type)
        {
            return Hierarchies.GetOrAdd(Normalize(type), canonicalType =>
                [canonicalType, .. canonicalType.GetInterfaces().Where(CanonicalTypes.Value.ContainsKey)]);
        }

        /// <summary>Matches generated class and interface metadata without assuming CLR class inheritance.</summary>
        /// <returns>The immutable metaclass normalization table.</returns>
        private static ImmutableDictionary<Type, Type> CreateCanonicalTypes()
        {
            var metaclasses = typeof(IElement).Assembly.GetTypes()
                .Where(typeof(IElement).IsAssignableFrom)
                .Select(type => (Type: type, Metadata: type.GetCustomAttribute<ClassAttribute>(false)))
                .Where(entry => entry.Metadata != null)
                .ToArray();
            var interfaces = metaclasses.Where(entry => entry.Type.IsInterface)
                .ToDictionary(entry => entry.Metadata.XmiId, entry => entry.Type, StringComparer.Ordinal);

            return metaclasses.ToImmutableDictionary(entry => entry.Type, entry => interfaces[entry.Metadata.XmiId]);
        }
    }
}
