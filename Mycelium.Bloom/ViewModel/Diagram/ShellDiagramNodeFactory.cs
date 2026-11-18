// ------------------------------------------------------------------------------------------------
// <copyright file="ShellDiagramNodeFactory.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram
{
    using System.Collections.Immutable;

    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.Model.Enum;

    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Attributes;
    using SysML2.NET.Core.POCO.Systems.DefinitionAndUsage;
    using SysML2.NET.Core.POCO.Systems.Ports;

    /// <summary>Provides generic shell notation after more specific node factories have been considered.</summary>
    public sealed class ShellDiagramNodeFactory : IDiagramNodeFactory
    {
        /// <summary>Gets the fallback SDK element interface.</summary>
        public Type ElementType => typeof(IElement);

        /// <summary>Projects supported shell contents without modifying the canonical element.</summary>
        /// <param name="occurrence">The borrowed canonical element and graphical geometry.</param>
        /// <param name="source">The optional canonical reference site supplying view-local names.</param>
        /// <returns>The shell presentation with its ordered compartments and boundary features.</returns>
        public DiagramNodePresentation Create(DiagramOccurrence occurrence, IElement source = null)
        {
            var element = occurrence.Element;
            var kind = element switch
            {
                IDefinition => DiagramNodeKind.Definition,
                IUsage { isReference: true } => DiagramNodeKind.Reference,
                IUsage => DiagramNodeKind.Usage,
                _ => DiagramNodeKind.Element
            };
            var attributes = element.OwnedRelationship.OfType<IMembership>().Select(member => member.MemberElement).OfType<IAttributeUsage>().ToArray();
            var compartments = attributes.Length == 0 ? ImmutableArray<DiagramCompartmentPresentation>.Empty
                : [new("attributes", attributes.Select(attribute => DiagramNotation.Declaration(attribute, true)).ToImmutableArray())];
            var ports = element.OwnedRelationship.OfType<IMembership>().Select(member => member.MemberElement).OfType<IFeature>()
                .Where(feature => feature is IPortUsage || feature.Direction is not null).ToArray();
            var boundary = ports.Select((port, position) => new DiagramBoundaryPresentation(port, DiagramNotation.Declaration(port),
                BoundarySide.Right, (position + 1d) / (ports.Length + 1), port.Direction?.ToString())).ToImmutableArray();

            return new DiagramShellPresentation(occurrence, DiagramNotation.Name(element, source), kind, compartments, boundary);
        }
    }
}
