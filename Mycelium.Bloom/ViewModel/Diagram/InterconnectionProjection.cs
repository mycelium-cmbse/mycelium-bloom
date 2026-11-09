// ------------------------------------------------------------------------------------------------
// <copyright file="InterconnectionProjection.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram
{
    using System.Collections.Immutable;
    using System.Globalization;

    using Mycelium.Bloom.Model.Diagram;

    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.FeatureValues;
    using SysML2.NET.Core.POCO.Kernel.Multiplicities;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Systems.Attributes;
    using SysML2.NET.Core.POCO.Systems.Connections;
    using SysML2.NET.Core.POCO.Systems.Parts;
    using SysML2.NET.Core.POCO.Systems.Ports;

    /// <summary>Projects a bounded interconnection slice from the loaded SDK containment graph.</summary>
    public static class InterconnectionProjection
    {
        /// <summary>Maps parts and explicit binary connection-end references without inventing edges.</summary>
        /// <param name="root">The canonical model root.</param>
        /// <returns>The supported projection and omitted-relationship diagnostics.</returns>
        public static DiagramProjection Create(IElement root)
        {
            ArgumentNullException.ThrowIfNull(root);
            var elements = Traverse(root).ToArray();
            var diagnostics = ImmutableArray.CreateBuilder<string>();
            var partElements = elements.OfType<IPartUsage>().Where(part => part is not IConnectionUsage).ToArray();
            var parts = partElements.ToImmutableDictionary(part => part.ElementId, part => new DiagramPart(
                part.ElementId,
                FormatHeader(part),
                part.owner is IPartUsage owner && owner is not IConnectionUsage ? owner.ElementId : null,
                part.ownedElement.OfType<IAttributeUsage>().Select(attribute => FormatAttribute(attribute, diagnostics)).ToImmutableArray(),
                part.ownedElement.OfType<IPortUsage>().Select(port => new DiagramPort(
                    port.ElementId, Name(port), port.Direction)).ToImmutableArray()));
            var portOwners = parts.Values.SelectMany(part => part.Ports.Select(port => (port.ElementId, Part: part)))
                .ToDictionary(pair => pair.ElementId, pair => pair.Part, StringComparer.Ordinal);
            var connections = ImmutableDictionary.CreateBuilder<string, DiagramConnection>(StringComparer.Ordinal);

            foreach (var connection in elements.OfType<IConnectionUsage>())
            {
                var ends = connection.ownedEndFeature;

                if (ends.Count != 2
                    || ends[0].ownedReferenceSubsetting?.ReferencedFeature is not IPortUsage source
                    || ends[1].ownedReferenceSubsetting?.ReferencedFeature is not IPortUsage target
                    || !portOwners.TryGetValue(source.ElementId, out var sourcePart)
                    || !portOwners.TryGetValue(target.ElementId, out var targetPart))
                {
                    diagnostics.Add($"Connection {Name(connection)} ({connection.ElementId}): requires two resolved references to owned ports.");
                    continue;
                }

                connections.Add(connection.ElementId, new DiagramConnection(
                    connection.ElementId, Name(connection), sourcePart.ElementId, source.ElementId,
                    targetPart.ElementId, target.ElementId,
                    FormatMultiplicity(ends[0], diagnostics), FormatMultiplicity(ends[1], diagnostics)));
            }

            foreach (var relationship in elements.OfType<IRelationship>().Where(relationship =>
                         relationship is not IConnectionUsage
                         && relationship is not SysML2.NET.Core.POCO.Root.Namespaces.IOwningMembership
                         && relationship is not IFeatureTyping
                         && relationship is not IReferenceSubsetting))
            {
                diagnostics.Add($"Unsupported relationship {relationship.GetType().Name} ({relationship.ElementId}); no edge projected.");
            }

            return new DiagramProjection(root.ElementId, parts, connections.ToImmutable(), diagnostics.ToImmutable());
        }

        /// <summary>Traverses SDK-owned elements once per canonical identity.</summary>
        /// <param name="root">The root of the bounded model scope.</param>
        /// <returns>The contained canonical elements.</returns>
        public static IEnumerable<IElement> Traverse(IElement root)
        {
            var visited = new HashSet<Guid>();
            var pending = new Stack<IElement>();
            pending.Push(root);

            while (pending.TryPop(out var element))
            {
                if (!visited.Add(element.Id))
                {
                    continue;
                }

                yield return element;

                foreach (var child in element.ownedElement.Concat(element.OwnedRelationship))
                {
                    pending.Push(child);
                }
            }
        }

        /// <summary>Formats the available canonical element name.</summary>
        internal static string Name(IElement element) => string.IsNullOrWhiteSpace(element.DeclaredName)
            ? element.GetType().Name : element.DeclaredName;

        /// <summary>Formats only explicit owned feature typing.</summary>
        internal static string FormatHeader(IPartUsage part)
        {
            var types = part.ownedTyping.Select(typing => typing.Type).Where(type => type is not null).Select(Name).Distinct();
            var typeName = string.Join(", ", types);
            return string.IsNullOrWhiteSpace(typeName) ? Name(part) : $"{Name(part)} : {typeName}";
        }

        /// <summary>Formats supported literal values and reports other expressions.</summary>
        internal static string FormatAttribute(IAttributeUsage attribute, ImmutableArray<string>.Builder diagnostics)
        {
            var value = attribute.OwnedRelationship.OfType<IFeatureValue>().SingleOrDefault()?.value;
            var text = value switch
            {
                ILiteralString literal => literal.Value,
                ILiteralInteger literal => literal.Value.ToString(CultureInfo.InvariantCulture),
                ILiteralRational literal => literal.Value.ToString(CultureInfo.InvariantCulture),
                null => "—",
                _ => null
            };

            if (text is null)
            {
                diagnostics.Add($"Attribute {attribute.ElementId}: unsupported value expression.");
            }

            return $"{Name(attribute)} : {text ?? "(expression)"}";
        }

        /// <summary>Formats literal bounds without evaluating arbitrary SDK expressions.</summary>
        internal static string FormatMultiplicity(IFeature end, ImmutableArray<string>.Builder diagnostics)
        {
            if (end.multiplicity is not IMultiplicityRange range)
            {
                return string.Empty;
            }

            var bounds = range.bound.Select(bound => bound switch
            {
                ILiteralInteger literal => literal.Value.ToString(CultureInfo.InvariantCulture),
                ILiteralInfinity => "*",
                _ => null
            }).ToArray();

            if (bounds.Length is < 1 or > 2 || bounds.Any(bound => bound is null))
            {
                diagnostics.Add($"Connection end {end.ElementId}: unsupported multiplicity expression.");
                return string.Empty;
            }

            return $"[{string.Join("..", bounds)}]";
        }
    }
}
