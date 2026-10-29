// ------------------------------------------------------------------------------------------------
// <copyright file="ElementLinkPresentation.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.DetailsPanel
{
    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Core.Types;
    using SysML2.NET.Core.POCO.Kernel.Multiplicities;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Exceptions;

    /// <summary>Preserves a canonical SDK relationship endpoint with its presentation role.</summary>
    /// <param name="Role">The SDK relationship role.</param>
    /// <param name="Element">The canonical target, or null when unavailable.</param>
    /// <param name="Label">The available name or explicit resolution state.</param>
    public sealed record ElementLinkPresentation(string Role, IElement Element, string Label)
    {
        /// <summary>Gets the semantic relation category used by the scoped theme treatment.</summary>
        public string Category { get; init; } = "neutral";

        /// <summary>Gets available target identity for compact relation rows without following references.</summary>
        public string Detail
        {
            get
            {
                if (this.Element is null)
                {
                    return null;
                }
                var kind = ElementPresentation.GetKindName(this.Element.GetType());
                try
                {
                    var qualifiedName = this.Element.qualifiedName;
                    return string.IsNullOrWhiteSpace(qualifiedName) || qualifiedName == this.Label
                        ? kind : $"{kind} · {qualifiedName}";
                }
                catch (IncompleteModelException)
                {
                    return kind;
                }
            }
        }

        /// <summary>Projects direct SDK links without resolving inheritance or following references as ownership.</summary>
        /// <param name="element">The selected canonical object.</param>
        /// <returns>Inspectable targets and individual incomplete-property notices.</returns>
        public static IReadOnlyList<ElementLinkPresentation> Create(IElement element)
        {
            var links = new List<ElementLinkPresentation>();
            Add("Owner", () => element.owner);
            Add("Owning relationship", () => element.OwningRelationship);
            AddMany(links, "Owned relationship", () => element.OwnedRelationship);
            AddMany(links, "Owned element", () => element.ownedElement);
            if (element is IRelationship relationship)
            {
                Add("Owning element", () => relationship.OwningRelatedElement);
                AddMany(links, "Related owned element", () => relationship.OwnedRelatedElement);
                AddMany(links, "Source", () => relationship.Source);
                AddMany(links, "Target", () => relationship.Target);
            }

            if (element is IMembership membership)
            {
                Add("Member", () => membership.MemberElement, true);
            }

            if (element is IFeature feature)
            {
                Add("Naming feature", feature.NamingFeature);
            }

            if (element is IType type)
            {
                Add("Multiplicity", () => type.multiplicity);
            }

            if (element is IMultiplicityRange range)
            {
                Add("Lower bound", () => range.lowerBound);
                Add("Upper bound", () => range.upperBound, true);
            }

            return links.ToArray();

            void Add(string role, Func<IElement> read, bool required = false)
            {
                try
                {
                    var target = read();
                    if (target is not null)
                    {
                        links.Add(new(role, target, ElementPresentation.GetDisplayName(target)));
                    }
                    else if (required)
                    {
                        links.Add(new(role, null, "No resolved target"));
                    }
                }
                catch (IncompleteModelException)
                {
                    links.Add(new(role, null, "Unresolved (incomplete model)"));
                }
            }
        }

        /// <summary>Preserves each direct SDK target and reports incomplete collections independently.</summary>
        /// <param name="links">The canonical reference collection.</param>
        /// <param name="role">The SDK relationship role.</param>
        /// <param name="read">The SDK collection accessor.</param>
        private static void AddMany(List<ElementLinkPresentation> links, string role, Func<IEnumerable<IElement>> read)
        {
            try
            {
                var targets = read()?.ToArray() ?? [];
                for (var index = 0; index < targets.Length; index++)
                {
                    var target = targets[index];
                    links.Add(new($"{role} {index + 1}", target,
                        target is null ? "Unresolved target" : ElementPresentation.GetDisplayName(target)));
                }
            }
            catch (IncompleteModelException)
            {
                links.Add(new(role, null, "Unresolved (incomplete model)"));
            }
        }
    }
}
