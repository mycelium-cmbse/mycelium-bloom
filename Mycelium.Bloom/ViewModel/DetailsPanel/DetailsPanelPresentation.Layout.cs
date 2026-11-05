// ------------------------------------------------------------------------------------------------
// <copyright file="DetailsPanelPresentation.Layout.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.DetailsPanel
{
    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Kernel.Functions;
    using SysML2.NET.Core.POCO.Systems.DefinitionAndUsage;
    using SysML2.NET.Core.POCO.Systems.Attributes;
    using SysML2.NET.Core.POCO.Systems.States;

    public sealed partial class DetailsPanelPresentation
    {
        /// <summary>Gets the visual grouping of the unchanged semantic section snapshot.</summary>
        public IReadOnlyList<PropertySectionPresentation> DisplaySections { get; }

        /// <summary>Gets whether the supplied variant includes a separate selection summary.</summary>
        public bool ShowSelectionSummary => this.Element is not IStateDefinition and not IStateUsage;

        /// <summary>Gets whether the summary contains a supported stored Attribute value.</summary>
        public bool HasValueSummary => this.Element is IAttributeUsage && !string.IsNullOrWhiteSpace(this.Summary);

        /// <summary>Applies the supplied visual hierarchy without replacing semantic mappings or canonical targets.</summary>
        /// <returns>Read-only sections for the panel.</returns>
        private IReadOnlyList<PropertySectionPresentation> BuildDisplaySections()
        {
            var sections = this.ContentSections.Select(section => section with
            {
                Layout = section.Tab == "Relations" ? "relations" : section.Title switch
                {
                    "Attributes" => "attributes",
                    "Comments" => "comment",
                    "Textual representation" => "text",
                    _ => "fields"
                },
                Count = section.Tab == "Relations" || section.Title == "Attributes" ? section.Links.Count : null
            }).ToList();

            var typing = this.ContentSections.FirstOrDefault(section => section.Title == "Classification")?.Links ?? [];
            if (typing.Count > 0)
            {
                sections.Insert(0, new("Typing", [], [])
                {
                    Tab = "Relations",
                    Layout = "relations",
                    Count = typing.Count,
                    Links = typing
                });
                var hierarchy = sections.FindIndex(section => section.Title == "Specializations / Type hierarchy");
                if (hierarchy >= 0)
                {
                    var unique = sections[hierarchy].Links.Where(link =>
                        !link.Role.Contains("typing", StringComparison.OrdinalIgnoreCase)).ToArray();
                    sections[hierarchy] = sections[hierarchy] with { Links = unique, Count = unique.Length };
                }
            }

            var ownedFeatures = GetOwnedMembers(this.Element, []).OfType<IFeature>()
                .Where(feature => this.Element is not IAttributeUsage || feature is not IExpression || feature is IUsage)
                .Select(feature => Link(ElementPresentation.GetKindName(feature.GetType()), feature)).ToArray();
            if (ownedFeatures.Length > 0)
            {
                sections.Add(new("Owned features", [], [])
                {
                    Tab = "Relations",
                    Layout = "relations",
                    Links = ownedFeatures,
                    Count = ownedFeatures.Length
                });
            }

            this.ArrangeStateSections(sections);
            return sections.Where(section => section.Properties.Count + section.Links.Count +
                section.Paragraphs.Count + section.Cards.Count > 0).ToArray();
        }

        /// <summary>Groups the resolved states and every transition role into the supplied card hierarchy.</summary>
        /// <param name="sections">The visual section collection.</param>
        private void ArrangeStateSections(List<PropertySectionPresentation> sections)
        {
            var structure = this.ContentSections.FirstOrDefault(section => section.Title == "State structure");
            if (structure is null)
            {
                return;
            }

            var states = structure.Links.Where(link => link.Role == "Nested state").ToArray();
            var initial = structure.Links.Where(link => link.Role == "Initial state").ToArray();
            var transitions = this.ContentSections.Where(section => section.Title == "Transitions").ToArray();
            var machine = new List<PropertySectionPresentation>
            {
                new("State machine", structure.Properties.Concat(new KeyValuePair<string, string>[]
                {
                    new("State usages", states.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    new("Parallel regions", states.Count(link => link.Element is IStateUsage { IsParallel: true }).ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    new("Transitions", transitions.Length.ToString(System.Globalization.CultureInfo.InvariantCulture))
                }).ToArray(), structure.Paragraphs) { Links = initial }
            };
            var ordinary = states.Where(link => link.Element is IStateUsage { IsParallel: false }).ToArray();
            machine.Add(new("State usages", [], [])
            {
                Layout = "cards",
                Count = ordinary.Length,
                Cards = ordinary.Select(link => CreateStateCard(link,
                    initial.Any(candidate => ReferenceEquals(candidate.Element, link.Element)))).ToArray()
            });
            foreach (var region in states.Where(link => link.Element is IStateUsage { IsParallel: true }))
            {
                var children = GetOwnedMembers(region.Element, []).OfType<IStateUsage>()
                    .Where(state => state is not ITransitionUsage).ToArray();
                machine.Add(new($"Parallel region · {region.Label}", [], [])
                {
                    Layout = "cards",
                    Count = children.Length,
                    Links = [region],
                    Cards = children.Select(state => CreateStateCard(Link("Nested state", state), false)).ToArray()
                });
            }
            machine.Add(new("Transitions", [], [])
            {
                Layout = "cards",
                Count = transitions.Length,
                Cards = transitions.Select(section => new PropertyCardPresentation(section.Links[0], section.Links.Skip(1).ToArray())).ToArray()
            });
            sections.RemoveAll(section => section.Title is "State structure" or "Transitions");
            var identity = sections.FindIndex(section => section.Title == "Identity");
            sections.InsertRange(identity + 1, machine);
        }

        /// <summary>Projects the existing SDK action roles of one nested state.</summary>
        /// <param name="heading">The unchanged canonical state link.</param>
        /// <param name="initial">Whether the existing initial-state interpretation selected this state.</param>
        /// <returns>A state card with available entry, do and exit links.</returns>
        private static PropertyCardPresentation CreateStateCard(ElementLinkPresentation heading, bool initial)
        {
            var state = (IStateUsage)heading.Element;
            var actions = new List<ElementLinkPresentation>();
            AddLink(actions, "Entry", () => state.entryAction, false);
            AddLink(actions, "Do", () => state.doAction, false);
            AddLink(actions, "Exit", () => state.exitAction, false);
            return new(heading, actions, initial ? "initial" : null);
        }
    }
}
