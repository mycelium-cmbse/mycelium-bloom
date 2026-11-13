// ------------------------------------------------------------------------------------------------
// <copyright file="GeneralViewProjector.cs" company="Starion Group S.A.">
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

    using SysML2.NET.Core.POCO.Core.Classifiers;
    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Core.Types;
    using SysML2.NET.Core.POCO.Kernel.Connectors;
    using SysML2.NET.Core.POCO.Kernel.Packages;
    using SysML2.NET.Core.POCO.Root.Annotations;
    using SysML2.NET.Core.POCO.Root.Dependencies;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Attributes;
    using SysML2.NET.Core.POCO.Systems.DefinitionAndUsage;
    using SysML2.NET.Core.POCO.Systems.Flows;
    using SysML2.NET.Core.POCO.Systems.Interfaces;
    using SysML2.NET.Core.POCO.Systems.Ports;

    /// <summary>Projects an explicit general-view slice, preserving canonical object references.</summary>
    public static class GeneralViewProjector
    {
        /// <summary>Projects the selected canonical root and its direct members.</summary>
        /// <param name="root">The selected element, or null when the workspace has no available model.</param>
        /// <returns>The bounded read-only slice; unavailable roots yield an empty view.</returns>
        public static GeneralViewProjection CreateForRoot(IElement root)
        {
            var elements = root is null ? Array.Empty<IElement>()
                : new[] { root }.Concat(root.OwnedRelationship.OfType<IMembership>()
                    .Select(member => member.MemberElement).Where(element => element is not null));

            return Create(elements);
        }

        /// <summary>Projects a bounded explicit model slice without mutating canonical elements.</summary>
        /// <param name="elements">The explicit canonical elements requested by the caller.</param>
        /// <param name="name">The optional view-frame name.</param>
        /// <param name="omitted">Whether the caller already knows content was omitted.</param>
        /// <returns>The immutable projected view and its omission state.</returns>
        public static GeneralViewProjection Create(IEnumerable<IElement> elements, string name = null, bool omitted = false)
        {
            ArgumentNullException.ThrowIfNull(elements);
            // This rendering slice is bounded; regional navigation belongs to the specialized views.
            const int maximumElements = 200;
            var candidates = elements.Distinct<IElement>(ReferenceEqualityComparer.Instance).Take(maximumElements + 1).ToArray();
            omitted |= candidates.Length > maximumElements || candidates.Any(element => element is null);
            var source = candidates.Take(maximumElements).Where(element => element is not null).ToArray();
            var nodes = source.Where(element => element is IDefinition || element is not IRelationship and not IConnector).Select((element, index) => Node(element,
                index)).ToImmutableArray();
            var lookup = new Dictionary<IElement, Guid>(ReferenceEqualityComparer.Instance);

            foreach (var node in nodes)
            {
                lookup.Add(node.Occurrence.Element, node.Occurrence.Id);
            }

            foreach (var node in nodes)
            {
                foreach (var boundary in node.Boundary)
                {
                    lookup.TryAdd(boundary.Element, node.Occurrence.Id);
                }
            }

            var edges = ImmutableArray.CreateBuilder<DiagramEdgePresentation>();
            var relationships = source.OfType<IRelationship>().Concat(source.SelectMany(element => element.OwnedRelationship))
                .Distinct<IRelationship>(ReferenceEqualityComparer.Instance);
            omitted = ProjectConnectors(omitted, source, lookup, edges);
            omitted = ProjectRelationships(omitted, relationships, source, lookup, edges);

            foreach (var comment in source.OfType<IComment>())
            {
                foreach (var target in comment.annotatedElement)
                {
                    if (lookup.TryGetValue(comment, out var from) && lookup.TryGetValue(target, out var to))
                    {
                        edges.Add(new(Guid.NewGuid(), comment, from, to, DiagramRelationshipKind.Annotation, null));
                    }
                    else
                    {
                        omitted = true;
                    }
                }
            }

            return new(nodes, edges.ToImmutable(), name, omitted);
        }

        /// <summary>Projects supported binary relationships and reports omitted or unavailable endpoints.</summary>
        /// <param name="omitted">Whether the caller already knows content was omitted.</param>
        /// <param name="relationships">The distinct canonical relationships in the requested slice.</param>
        /// <param name="source">The canonical source slice or source occurrence adapter.</param>
        /// <param name="lookup">The mapping from canonical object references to graphical occurrence identities.</param>
        /// <param name="edges">The builder receiving supported edge presentations.</param>
        /// <returns>Whether content remains omitted after relationship projection.</returns>
        private static bool ProjectRelationships(bool omitted, IEnumerable<IRelationship> relationships, IElement[] source, Dictionary<IElement, Guid> lookup,
            ImmutableArray<DiagramEdgePresentation>.Builder edges)
        {
            foreach (var relationship in relationships)
            {
                omitted |= ProjectRelationship(relationship, source, lookup, edges);
            }

            return omitted;
        }

        /// <summary>Projects one supported binary relationship, preserving canonical direction.</summary>
        /// <param name="relationship">The canonical SDK relationship.</param>
        /// <param name="source">The explicit requested slice.</param>
        /// <param name="lookup">The mapping from canonical references to occurrence identities.</param>
        /// <param name="edges">Receives the supported relationship presentation.</param>
        /// <returns>Whether this relationship has requested content that cannot be represented.</returns>
        private static bool ProjectRelationship(IRelationship relationship, IElement[] source, Dictionary<IElement, Guid> lookup,
            ImmutableArray<DiagramEdgePresentation>.Builder edges)
        {
            // Connector ends and typing are already represented by the edge declaration and end labels.

            if (relationship.OwningRelatedElement is IConnector && relationship is IEndFeatureMembership or IFeatureTyping)
            {
                return false;
            }

            if (relationship is IMembership inlineMember && IsInlineAttribute(inlineMember, source, lookup))
            {
                return false;
            }

            var kind = Kind(relationship);

            if (kind is null)
            {
                if (relationship is IOwningMembership ownedMember)
                {
                    // Namespace ownership has distinct BNF notation; a displayed node is not an ownership edge.
                    // A connector, however, is represented by its usage edge rather than a separate node.
                    return ownedMember.MemberElement is not IConnector
                        || !edges.Any(edge => ReferenceEquals(edge.Element, ownedMember.MemberElement));
                }

                return relationship is not IDefinition and not IConnector && source.Any(element => ReferenceEquals(element, relationship));
            }

            var sources = relationship is IMembership membership ? new IElement[] { membership.membershipOwningNamespace } : relationship.Source.ToArray();
            var targets = relationship is IMembership member ? new[] { member.MemberElement } : relationship.Target.ToArray();

            if (sources.Length != 1 || targets.Length != 1)
            {
                return true;
            }

            var start = sources[0];
            var end = targets[0];

            if (start is null || end is null || !lookup.TryGetValue(start, out var from) || !lookup.TryGetValue(end, out var to))
            {
                return true;
            }

            if (from == to && relationship is IMembership)
            {
                return false;
            }

            edges.Add(new DiagramEdgePresentation(Guid.NewGuid(), relationship, from, to, kind.Value, Label(relationship, kind.Value), SourceFeature: start, TargetFeature: end));

            return false;
        }

        /// <summary>Checks whether an attribute membership is already shown in a visible node's textual compartment.</summary>
        /// <param name="membership">The canonical membership being projected.</param>
        /// <param name="source">The explicit slice whose non-relationship elements have node occurrences.</param>
        /// <param name="lookup">The represented nodes and boundary features keyed by canonical reference.</param>
        /// <returns>Whether the attribute is inline rather than an independently requested occurrence.</returns>
        private static bool IsInlineAttribute(IMembership membership, IElement[] source, Dictionary<IElement, Guid> lookup)
        {
            return membership.MemberElement is IAttributeUsage && !lookup.ContainsKey(membership.MemberElement)
                && membership.membershipOwningNamespace is { } owner && lookup.ContainsKey(owner)
                && source.Any(element => ReferenceEquals(element, owner) && element is not IConnector);
        }

        /// <summary>Projects binary connections and bindings without approximating specialized flows.</summary>
        /// <param name="omitted">Whether the caller already knows content was omitted.</param>
        /// <param name="source">The canonical source slice or source occurrence adapter.</param>
        /// <param name="lookup">The mapping from canonical object references to graphical occurrence identities.</param>
        /// <param name="edges">The builder receiving supported edge presentations.</param>
        /// <returns>Whether content remains omitted after connector projection.</returns>
        private static bool ProjectConnectors(bool omitted, IElement[] source, Dictionary<IElement, Guid> lookup, ImmutableArray<DiagramEdgePresentation>.Builder edges)
        {
            foreach (var connector in source.OfType<IConnector>())
            {
                if (connector is IFlowUsage)
                {
                    // Flow payload and direction require the specialized projection; do not mislabel a plain connection.
                    omitted = true;

                    continue;
                }

                var ends = connector.relatedFeature.ToArray();

                if (ends.Length != 2 || !lookup.TryGetValue(ends[0], out var from) || !lookup.TryGetValue(ends[1], out var to))
                {
                    omitted = true;

                    continue;
                }

                var kind = connector is IBindingConnector ? DiagramRelationshipKind.Binding : DiagramRelationshipKind.Connection;
                var prefix = connector is IInterfaceUsage ? "«interface» " : "";
                var connectorEnds = connector.connectorEnd.ToArray();
                edges.Add(new(Guid.NewGuid(), connector, from, to, kind, prefix + Label(connector, kind),
                    connectorEnds.Length == 2 ? EndLabel(connectorEnds[0]) : null,
                    connectorEnds.Length == 2 ? EndLabel(connectorEnds[1]) : null, ends[0], ends[1]));
            }

            return omitted;
        }

        /// <summary>Formats optional connector-end notation using canonical SDK metadata.</summary>
        /// <param name="end">The canonical connector end.</param>
        /// <returns>The role, SDK multiplicity and ordering adornment for that end.</returns>
        private static DiagramEndLabel EndLabel(IFeature end)
        {
            return new DiagramEndLabel(end.DeclaredName, ElementPresentation.GetMultiplicity(end), end.IsOrdered ? "{ordered}" : null);
        }

        /// <summary>Projects generic slots for one canonical definition, usage, package or annotation.</summary>
        /// <param name="element">The borrowed canonical SDK element.</param>
        /// <param name="index">The nonnegative position in the initial grid layout.</param>
        /// <returns>The reusable node presentation with an independent occurrence identity.</returns>
        public static DiagramNodePresentation Node(IElement element, int index = 0)
        {
            var kind = element switch
            {
                IPackage => DiagramNodeKind.Package,
                IComment => DiagramNodeKind.Comment,
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
            var row = index / 3;

            return new(new(Guid.NewGuid(), element, 90 + index % 3 * 360, 90 + row * 270), kind,
                DiagramNotation.Name(element), compartments, boundary, (element as IComment)?.Body);
        }

        /// <summary>Classifies supported relationships using canonical SDK interfaces.</summary>
        /// <param name="relationship">The canonical SDK relationship.</param>
        /// <returns>The supported notation kind, or null when no generic notation is available.</returns>
        public static DiagramRelationshipKind? Kind(IRelationship relationship) => relationship switch
        {
            IFeatureTyping => DiagramRelationshipKind.Definition,
            IRedefinition => DiagramRelationshipKind.Redefinition,
            IReferenceSubsetting => null,
            ISubsetting => DiagramRelationshipKind.Subsetting,
            ISubclassification => DiagramRelationshipKind.Subclassification,
            IFeatureMembership { MemberElement: IFeature { IsComposite: true } } => DiagramRelationshipKind.CompositeMembership,
            IFeatureMembership => DiagramRelationshipKind.NoncompositeMembership,
            IDependency => DiagramRelationshipKind.Dependency,
            _ => null
        };

        /// <summary>Selects shared edge notation and delegates connection declarations to the SDK.</summary>
        /// <param name="element">The borrowed canonical SDK element.</param>
        /// <param name="kind">The supported generic relationship notation.</param>
        /// <returns>The visible label, or null when the BNF requires only endpoint notation.</returns>
        public static string Label(IElement element, DiagramRelationshipKind kind) => kind switch
        {
            DiagramRelationshipKind.Binding => "=",
            DiagramRelationshipKind.Subsetting => null,
            DiagramRelationshipKind.Redefinition => null,
            DiagramRelationshipKind.Connection => DiagramNotation.Declaration(element),
            DiagramRelationshipKind.Dependency => string.IsNullOrWhiteSpace(element.DeclaredName) ? null : ElementPresentation.GetDisplayName(element),
            _ => null
        };
    }
}
