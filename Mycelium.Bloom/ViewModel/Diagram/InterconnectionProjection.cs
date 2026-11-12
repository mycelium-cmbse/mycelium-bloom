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
