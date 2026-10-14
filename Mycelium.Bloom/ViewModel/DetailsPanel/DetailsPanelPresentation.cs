// ------------------------------------------------------------------------------------------------
// <copyright file="DetailsPanelPresentation.cs" company="Starion Group S.A.">
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
    using SysML2.NET.Core.POCO.Kernel.FeatureValues;
    using SysML2.NET.Core.POCO.Root.Annotations;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Systems.Attributes;
    using SysML2.NET.Core.POCO.Systems.DefinitionAndUsage;

    /// <summary>
    /// Projects a selected element into read-only property groups supported by the SDK.
    /// </summary>
    public sealed class DetailsPanelPresentation
    {
        /// <summary>
        /// Initializes a property snapshot for the current selection.
        /// </summary>
        /// <param name="element">The selected canonical element.</param>
        public DetailsPanelPresentation(IElement element)
        {
            ArgumentNullException.ThrowIfNull(element);

            this.DisplayName = ElementPresentation.GetDisplayName(element);
            var identity = new List<KeyValuePair<string, string>>();
            Add(identity, "ID", element.ElementId);
            Add(identity, "Declared name", element.DeclaredName);
            Add(identity, "Declared short name", element.DeclaredShortName);
            Add(identity, "Qualified name", element.qualifiedName);

            if (element.owningMembership is { } membership)
            {
                Add(identity, "Visibility", membership.Visibility.ToString().ToLowerInvariant());
            }

            var classification = new List<KeyValuePair<string, string>>();
            Add(classification, "Multiplicity", ElementPresentation.GetMultiplicity(element));

            if (element is IType type)
            {
                Add(classification, "Is abstract", type.IsAbstract ? "true" : "false");
            }

            if (element is IFeature feature)
            {
                var typeNames = feature.type?.Where(candidate => candidate != null)
                    .Select(candidate => new[] { candidate.qualifiedName, candidate.name, candidate.DeclaredName }
                        .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)))
                    .Where(name => !string.IsNullOrWhiteSpace(name));
                Add(classification, "Typed by", string.Join(", ", typeNames ?? []));
                Add(classification, "Composition", feature.IsComposite ? "composite" : "reference");
                Add(classification, "Is ordered", feature.IsOrdered ? "true" : "false");
                Add(classification, "Is unique", feature.IsUnique ? "true" : "false");
            }

            if (element is IUsage usage)
            {
                Add(classification, "Is variation", usage.IsVariation ? "true" : "false");
            }

            var attributes = new List<KeyValuePair<string, string>>();

            foreach (var attribute in GetOwnedMembers(element).OfType<IAttributeUsage>())
            {
                Add(attributes, ElementPresentation.GetDisplayName(attribute), GetValue(attribute));
            }

            var value = new List<KeyValuePair<string, string>>();

            if (element is IAttributeUsage)
            {
                Add(value, "Value", GetValue(element));
            }

            this.Documentation = (element is IDocumentation documentation
                    ? new[] { documentation }
                    : element.documentation?.ToArray() ?? [])
                .Where(document => document != null)
                .Select(document => document.Body)
                .Where(body => !string.IsNullOrWhiteSpace(body))
                .ToArray();

            this.Sections = new (string Title, IReadOnlyList<KeyValuePair<string, string>> Properties, IReadOnlyList<string> Paragraphs)[]
            {
                ("Identity", identity.ToArray(), []),
                ("Attributes", attributes.ToArray(), []),
                ("Documentation", [], this.Documentation),
                ("Value", value.ToArray(), []),
                ("Classification", classification.ToArray(), [])
            }.Where(section => section.Properties.Count > 0 || section.Paragraphs.Count > 0).ToArray();
        }

        /// <summary>
        /// Gets the selected element's heading.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the populated property groups in display order.
        /// </summary>
        public IReadOnlyList<(string Title, IReadOnlyList<KeyValuePair<string, string>> Properties, IReadOnlyList<string> Paragraphs)> Sections { get; }

        /// <summary>
        /// Gets the selected element's documentation bodies as plain text.
        /// </summary>
        public IReadOnlyList<string> Documentation { get; }

        /// <summary>
        /// Adds an available SDK value to a property group.
        /// </summary>
        private static void Add(List<KeyValuePair<string, string>> properties, string label, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                properties.Add(new KeyValuePair<string, string>(label, value));
            }
        }

        /// <summary>
        /// Resolves a single literal feature value without evaluating expressions.
        /// </summary>
        private static string GetValue(IElement element)
        {
            var values = element.OwnedRelationship?.OfType<IFeatureValue>().ToArray() ?? [];

            return values.Length == 1 ? ElementPresentation.GetLiteral(values[0].value) : null;
        }

        /// <summary>
        /// Traverses owned members through relationship intermediaries without entering nested features.
        /// </summary>
        private static IEnumerable<IElement> GetOwnedMembers(IElement element)
        {
            foreach (var child in element.ownedElement ?? [])
            {
                if (child is IRelationship and not IType)
                {
                    foreach (var member in GetOwnedMembers(child))
                    {
                        yield return member;
                    }
                }
                else if (child != null)
                {
                    yield return child;
                }
            }
        }
    }
}
