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

    using SysML2.NET.Core.POCO.Kernel.Connectors;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.DefinitionAndUsage;

    /// <summary>Projects a General View's explicit canonical slice using shared notation and layout policies.</summary>
    public class GeneralViewProjector : DiagramProjector, IGeneralViewProjector
    {
        /// <summary>Creates a General View projector over the registered notation and layout strategies.</summary>
        /// <param name="factories">The registered node policies.</param>
        /// <param name="edgeFactories">The optional registered relationship policies.</param>
        /// <param name="layout">The optional bound and initial-placement strategy.</param>
        public GeneralViewProjector(IEnumerable<IDiagramNodeFactory> factories, IEnumerable<IDiagramEdgeFactory> edgeFactories = null, IDiagramLayout layout = null)
            : base(factories, edgeFactories, layout)
        {
        }

        /// <summary>Projects the selected canonical root and its direct members.</summary>
        /// <param name="root">The selected element, or null when the workspace has no available model.</param>
        /// <returns>The bounded read-only slice; unavailable roots yield an empty view.</returns>
        public virtual DiagramProjection CreateForRoot(IElement root)
        {
            var elements = root is null ? Array.Empty<IElement>()
                : new[] { root }.Concat(root.OwnedRelationship.OfType<IMembership>()
                    .Select(member => member.MemberElement).Where(element => element is not null));

            return this.CreateCore(elements, null, false, root);
        }

        /// <summary>Projects a bounded explicit model slice without mutating canonical elements.</summary>
        /// <param name="elements">The explicit canonical elements requested by the caller.</param>
        /// <param name="name">The optional view-frame name.</param>
        /// <param name="omitted">Whether the caller already knows content was omitted.</param>
        /// <returns>The immutable projected view and its omission state.</returns>
        public DiagramProjection Create(IEnumerable<IElement> elements, string name = null, bool omitted = false) => this.CreateCore(elements, name, omitted, null);

        /// <summary>Projects a bounded slice with its canonical reference context.</summary>
        /// <param name="elements">The explicit canonical elements.</param>
        /// <param name="name">The optional view-frame name.</param>
        /// <param name="omitted">Whether content is already known to be omitted.</param>
        /// <param name="referenceSite">The optional canonical source supplying contextual names.</param>
        /// <returns>The immutable typed projection.</returns>
        protected virtual DiagramProjection CreateCore(IEnumerable<IElement> elements, string name, bool omitted, IElement referenceSite)
        {
            ArgumentNullException.ThrowIfNull(elements);
            var maximumElements = this.Layout.MaximumElements;
            var candidates = elements.Distinct<IElement>(ReferenceEqualityComparer.Instance).Take(maximumElements + 1).ToArray();
            omitted |= candidates.Length > maximumElements || candidates.Any(element => element is null);
            var source = candidates.Take(maximumElements).Where(element => element is not null).ToArray();
            var nodes = source.Where(element => element is IDefinition || element is not IRelationship and not IConnector).Select((element, index) => this.Node(element,
                index, referenceSite)).ToImmutableArray();
            var edges = this.ProjectEdges(source, nodes, ref omitted);

            return new(nodes, edges, name, omitted);
        }
    }
}
