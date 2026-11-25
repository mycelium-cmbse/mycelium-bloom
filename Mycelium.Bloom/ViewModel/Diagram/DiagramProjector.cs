// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramProjector.cs" company="Starion Group S.A.">
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

    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Kernel.Connectors;
    using SysML2.NET.Core.POCO.Root.Annotations;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Attributes;
    using SysML2.NET.Core.POCO.Systems.DefinitionAndUsage;

    /// <summary>Shares canonical node, connector, relationship and annotation projection between diagram kinds.</summary>
    public abstract class DiagramProjector : IDiagramElementProjector
    {
        /// <summary>The registered node policies in deterministic fallback order.</summary>
        private readonly IDiagramNodeFactory[] factories;

        /// <summary>The registered edge policies in deterministic fallback order.</summary>
        private readonly IDiagramEdgeFactory[] edgeFactories;

        /// <summary>Gets the reusable bound and initial-placement strategy.</summary>
        protected IDiagramLayout Layout { get; }

        /// <summary>Creates projection infrastructure over canonical SDK-type policies.</summary>
        /// <param name="factories">The registered node policies.</param>
        /// <param name="edgeFactories">The optional registered relationship policies.</param>
        /// <param name="layout">The optional initial-layout strategy.</param>
        protected DiagramProjector(IEnumerable<IDiagramNodeFactory> factories, IEnumerable<IDiagramEdgeFactory> edgeFactories = null, IDiagramLayout layout = null)
        {
            ArgumentNullException.ThrowIfNull(factories);
            this.factories = factories.ToArray();
            this.edgeFactories = (edgeFactories ?? DiagramEdgeFactories.CreateDefault()).ToArray();
            this.Layout = layout ?? new GridDiagramLayout();
        }

        /// <summary>Projects shared relationships against canonical references already represented by node occurrences.</summary>
        /// <param name="source">The bounded canonical source slice.</param>
        /// <param name="nodes">The typed graphical node occurrences.</param>
        /// <param name="omitted">Receives any unsupported or unavailable requested content.</param>
        /// <returns>The supported binary relationships and annotation links.</returns>
        protected virtual ImmutableArray<DiagramEdgePresentation> ProjectEdges(IElement[] source, ImmutableArray<DiagramNodePresentation> nodes, ref bool omitted)
        {
            var lookup = CreateOccurrenceLookup(nodes);
            var edges = ImmutableArray.CreateBuilder<DiagramEdgePresentation>();
            var relationships = source.OfType<IRelationship>().Concat(source.SelectMany(element => element.OwnedRelationship))
                .Distinct<IRelationship>(ReferenceEqualityComparer.Instance);
            omitted = this.ProjectConnectors(omitted, source, lookup, edges);
            omitted = this.ProjectRelationships(omitted, relationships, source, lookup, edges);
            omitted = this.ProjectAnnotations(omitted, source.OfType<IComment>(), lookup, edges);

            return edges.ToImmutable();
        }

        /// <summary>Resolves a registered descriptor using the same specific-first dispatch as node and keyword policies.</summary>
        /// <param name="element">The canonical SDK element.</param>
        /// <returns>The descriptor, or null for unsupported notation.</returns>
        public DiagramEdgeNotation GetEdgeNotation(IElement element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return TypeDispatch.Find(this.edgeFactories, element.GetType(), factory => factory.ElementType)?.Create(element);
        }

        /// <summary>Registers canonical node and boundary references while preserving standalone occurrence precedence.</summary>
        /// <param name="nodes">The typed occurrences in projection order.</param>
        /// <returns>The canonical-reference lookup used by relationship and annotation endpoints.</returns>
        private static Dictionary<IElement, Guid> CreateOccurrenceLookup(ImmutableArray<DiagramNodePresentation> nodes)
        {
            var lookup = new Dictionary<IElement, Guid>(ReferenceEqualityComparer.Instance);

            foreach (var occurrence in nodes.Select(node => node.Occurrence))
            {
                lookup.TryAdd(occurrence.Element, occurrence.Id);
            }

            foreach (var shell in nodes.OfType<DiagramShellPresentation>())
            {
                foreach (var boundary in shell.Boundary)
                {
                    lookup.TryAdd(boundary.Element, shell.Occurrence.Id);
                }
            }

            return lookup;
        }

        /// <summary>Projects represented annotation targets and retains explicit omission state.</summary>
        /// <param name="omitted">Whether requested content is already known to be omitted.</param>
        /// <param name="comments">The canonical comments in projection order.</param>
        /// <param name="lookup">The canonical-reference mapping to node occurrences.</param>
        /// <param name="edges">The builder receiving supported annotation edges.</param>
        /// <returns>Whether any requested content remains omitted.</returns>
        private bool ProjectAnnotations(bool omitted, IEnumerable<IComment> comments, Dictionary<IElement, Guid> lookup,
            ImmutableArray<DiagramEdgePresentation>.Builder edges)
        {
            foreach (var comment in comments)
            {
                var notation = this.GetEdgeNotation(comment);

                foreach (var target in comment.annotatedElement)
                {
                    if (notation is not null && lookup.TryGetValue(comment, out var from) && lookup.TryGetValue(target, out var to))
                    {
                        edges.Add(new(Guid.NewGuid(), comment, from, to, notation, notation.Label));
                    }
                    else
                    {
                        omitted = true;
                    }
                }
            }

            return omitted;
        }

        /// <summary>Projects supported binary relationships and reports omitted or unavailable endpoints.</summary>
        /// <param name="omitted">Whether the caller already knows content was omitted.</param>
        /// <param name="relationships">The distinct canonical relationships in the requested slice.</param>
        /// <param name="source">The canonical source slice or source occurrence adapter.</param>
        /// <param name="lookup">The mapping from canonical object references to graphical occurrence identities.</param>
        /// <param name="edges">The builder receiving supported edge presentations.</param>
        /// <returns>Whether content remains omitted after relationship projection.</returns>
        private bool ProjectRelationships(bool omitted, IEnumerable<IRelationship> relationships, IElement[] source, Dictionary<IElement, Guid> lookup,
            ImmutableArray<DiagramEdgePresentation>.Builder edges)
        {
            foreach (var relationship in relationships)
            {
                omitted |= this.ProjectRelationship(relationship, source, lookup, edges);
            }

            return omitted;
        }

        /// <summary>Projects one supported binary relationship, preserving canonical direction.</summary>
        /// <param name="relationship">The canonical SDK relationship.</param>
        /// <param name="source">The explicit requested slice.</param>
        /// <param name="lookup">The mapping from canonical references to occurrence identities.</param>
        /// <param name="edges">Receives the supported relationship presentation.</param>
        /// <returns>Whether this relationship has requested content that cannot be represented.</returns>
        private bool ProjectRelationship(IRelationship relationship, IElement[] source, Dictionary<IElement, Guid> lookup,
            ImmutableArray<DiagramEdgePresentation>.Builder edges)
        {
            if (relationship is IConnector)
            {
                return false;
            }

            // Connector ends and typing are already represented by the edge declaration and end labels.

            if (relationship.OwningRelatedElement is IConnector && relationship is IEndFeatureMembership or IFeatureTyping)
            {
                return false;
            }

            if (relationship is IMembership inlineMember && IsInlineAttribute(inlineMember, source, lookup))
            {
                return false;
            }

            var notation = this.GetEdgeNotation(relationship);

            if (notation is null)
            {
                return HasOmittedRelationshipContent(relationship, source, edges);
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

            edges.Add(new DiagramEdgePresentation(Guid.NewGuid(), relationship, from, to, notation, notation.Label, SourceFeature: start, TargetFeature: end));

            return false;
        }

        /// <summary>Reports requested content that has no supported standalone relationship notation.</summary>
        /// <param name="relationship">The canonical relationship without a supported edge kind.</param>
        /// <param name="source">The explicit requested slice.</param>
        /// <param name="edges">The already represented connector and relationship presentations.</param>
        /// <returns>Whether the relationship has unrepresented requested content.</returns>
        private static bool HasOmittedRelationshipContent(IRelationship relationship, IElement[] source, ImmutableArray<DiagramEdgePresentation>.Builder edges)
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
        private bool ProjectConnectors(bool omitted, IElement[] source, Dictionary<IElement, Guid> lookup, ImmutableArray<DiagramEdgePresentation>.Builder edges)
        {
            foreach (var connector in source.OfType<IConnector>())
            {
                var notation = this.GetEdgeNotation(connector);

                if (notation is null)
                {
                    omitted = true;

                    continue;
                }

                var ends = connector.relatedFeature.ToArray();

                if (ends.Length != 2 || !lookup.TryGetValue(ends[0], out var from) || !lookup.TryGetValue(ends[1], out var to))
                {
                    omitted = true;

                    continue;
                }

                var connectorEnds = connector.connectorEnd.ToArray();
                edges.Add(new(Guid.NewGuid(), connector, from, to, notation, notation.Label,
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

        /// <summary>Adds graphical notation using the existing SDK-type policies and canonical relationship projection.</summary>
        /// <param name="projection">The retained immutable slice.</param>
        /// <param name="element">The canonical object borrowed from the Browser.</param>
        /// <param name="position">The finite model-space position.</param>
        /// <returns>The extended projection, or null for unsupported elements or a full view.</returns>
        public virtual DiagramProjection AddElement(DiagramProjection projection, IElement element, DiagramPoint position)
        {
            if (element is null || element is IRelationship and not IDefinition || element is IConnector
                || !double.IsFinite(position.X) || !double.IsFinite(position.Y) || projection.Nodes.Length >= this.Layout.MaximumElements)
            {
                return null;
            }

            var node = this.Node(element);
            node = node with { Occurrence = node.Occurrence with { X = position.X, Y = position.Y } };
            var nodes = projection.Nodes.Add(node);
            var source = this.RelationshipSources(projection, nodes);
            var omitted = projection.HasOmittedContent;
            var edges = projection.Edges.ToBuilder();

            foreach (var existingNode in projection.Nodes)
            {
                var candidates = projection.Nodes.Remove(existingNode).Insert(0, existingNode).Insert(0, node);
                var projected = this.ProjectEdges(source, candidates, ref omitted);

                foreach (var edge in projected)
                {
                    if ((edge.Source == node.Occurrence.Id || edge.Target == node.Occurrence.Id)
                        && !edges.Any(existing => ReferenceEquals(existing.Element, edge.Element) && existing.Source == edge.Source && existing.Target == edge.Target))
                    {
                        edges.Add(edge);
                    }
                }
            }

            return projection with { Nodes = nodes, Edges = edges.ToImmutable(), HasOmittedContent = omitted };
        }

        /// <summary>Finds supported existing relationships within the represented elements' canonical ownership graphs.</summary>
        /// <param name="projection">The retained canonical relationship references.</param>
        /// <param name="nodes">The current graphical occurrences.</param>
        /// <returns>The reference-distinct source slice for existing relationship policies.</returns>
        private IElement[] RelationshipSources(DiagramProjection projection, ImmutableArray<DiagramNodePresentation> nodes)
        {
            var source = new HashSet<IElement>(projection.Edges.Select(edge => edge.Element), ReferenceEqualityComparer.Instance);
            var represented = new HashSet<IElement>(nodes.Select(node => node.Occurrence.Element)
                .Concat(nodes.OfType<DiagramShellPresentation>().SelectMany(node => node.Boundary.Select(boundary => boundary.Element))), ReferenceEqualityComparer.Instance);
            source.UnionWith(nodes.Select(node => node.Occurrence.Element));

            foreach (var element in OwnedElements(nodes.Select(node => node.Occurrence.Element)))
            {
                if (element is IConnector connector && this.HasRepresentedEndpoints(connector, represented))
                {
                    source.Add(connector);
                }

                foreach (var relationship in element.OwnedRelationship.Where(relationship => this.HasRepresentedEndpoints(relationship, represented)))
                {
                    source.Add(relationship);
                }
            }

            return source.ToArray();
        }

        /// <summary>Checks registered non-membership notation without interpreting unsupported relationship semantics.</summary>
        /// <param name="relationship">The existing canonical relationship.</param>
        /// <param name="represented">The canonical nodes and boundary features available as graphical endpoints.</param>
        /// <returns>Whether the existing relationship connects represented elements.</returns>
        private bool HasRepresentedEndpoints(IRelationship relationship, HashSet<IElement> represented)
        {
            if (relationship is IMembership || this.GetEdgeNotation(relationship) is null)
            {
                return false;
            }

            var endpoints = relationship is IConnector connector ? connector.relatedFeature.Cast<IElement>().ToArray()
                : relationship.Source.Concat(relationship.Target).ToArray();
            return endpoints.Length > 1 && endpoints.All(represented.Contains);
        }

        /// <summary>Visits each available owned element once without following relationship endpoints or imports.</summary>
        /// <param name="elements">The canonical elements whose ownership roots define the available graph.</param>
        /// <returns>The reference-distinct canonical ownership closure.</returns>
        private static IEnumerable<IElement> OwnedElements(IEnumerable<IElement> elements)
        {
            var pending = new Stack<IElement>(elements.Select(OwnershipRoot).Distinct<IElement>(ReferenceEqualityComparer.Instance));
            var visited = new HashSet<IElement>(ReferenceEqualityComparer.Instance);

            while (pending.TryPop(out var element))
            {
                if (!visited.Add(element))
                {
                    continue;
                }

                yield return element;

                foreach (var child in element.ownedElement)
                {
                    pending.Push(child);
                }
            }
        }

        /// <summary>Finds an element's available ownership root using canonical references.</summary>
        /// <param name="element">The represented canonical element.</param>
        /// <returns>The highest available owner, bounded by repeated canonical references.</returns>
        private static IElement OwnershipRoot(IElement element)
        {
            var visited = new HashSet<IElement>(ReferenceEqualityComparer.Instance) { element };

            while (element.owner is { } owner && visited.Add(owner))
            {
                element = owner;
            }

            return element;
        }

        /// <summary>Projects generic slots for one canonical definition, usage, package or annotation.</summary>
        /// <param name="element">The borrowed canonical SDK element.</param>
        /// <param name="index">The nonnegative position in the initial grid layout.</param>
        /// <param name="source">The optional canonical reference site.</param>
        /// <returns>The reusable node presentation with an independent occurrence identity.</returns>
        public virtual DiagramNodePresentation Node(IElement element, int index = 0, IElement source = null)
        {
            ArgumentNullException.ThrowIfNull(element);
            var factory = TypeDispatch.Find(this.factories, element.GetType(), candidate => candidate.ElementType)
                ?? throw new InvalidOperationException("No diagram node factory accepts this SDK element.");
            var occurrence = this.Layout.CreateOccurrence(element, index);

            return factory.Create(occurrence, source);
        }
    }
}
