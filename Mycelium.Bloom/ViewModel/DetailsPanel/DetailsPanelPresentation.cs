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
    using SysML2.NET.Core.POCO.Kernel.Functions;
    using SysML2.NET.Core.POCO.Kernel.Behaviors;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Exceptions;

    /// <summary>
    /// Projects a selected element into read-only property groups supported by the SDK.
    /// </summary>
    public sealed class DetailsPanelPresentation
    {
        /// <summary>Identifies multiplicity consistently across properties and canonical links.</summary>
        private const string MultiplicityLabel = "Multiplicity";

        /// <summary>Describes an SDK property blocked by incomplete model data.</summary>
        private const string IncompleteProperty = "Unavailable (incomplete model)";

        /// <summary>Describes an owned value whose expression cannot be resolved.</summary>
        private const string UnresolvedValue = "Unresolved value";

        /// <summary>
        /// Initializes a property snapshot for the current selection.
        /// </summary>
        /// <param name="element">The selected canonical element.</param>
        public DetailsPanelPresentation(IElement element)
        {
            ArgumentNullException.ThrowIfNull(element);

            this.Element = element;
            this.ModelLinks = ElementLinkPresentation.Create(element);
            this.Expressions = ExpressionPresentation.Create(element);
            this.ExpressionNotations = this.Expressions.SelectMany(GetNotationRoots).ToArray();
            this.DisplayName = this.Expressions.FirstOrDefault(node => ReferenceEquals(node.Element, element))?.Label
                ?? GetDisplayName(element);
            var identity = new List<KeyValuePair<string, string>>();
            var advanced = new List<KeyValuePair<string, string>>();
            Add(advanced, "Element ID", element.ElementId);
            if (element.Id.ToString() != element.ElementId) Add(advanced, "SDK ID", element.Id.ToString());
            AddIdentityProperties(element, identity, advanced);

            var classification = GetClassification(element, advanced);

            var attributes = new List<KeyValuePair<string, string>>();

            foreach (var attribute in GetOwnedMembers(element, attributes).OfType<IAttributeUsage>())
            {
                attributes.Add(new(GetDisplayName(attribute), GetValue(attribute)));
            }

            var value = new List<KeyValuePair<string, string>>();

            if (element is IAttributeUsage)
            {
                value.Add(new("Value", GetValue(element)));
            }
            else if (element is IFeature and not IExpression and not IUsage && this.Expressions.Count == 0)
            {
                var description = GetValue(element);
                value.Add(new("Value", description));
            }

            var documents = Read(() => element is IDocumentation documentation
                    ? new[] { documentation }
                    : element.documentation?.ToArray() ?? [], out var incompleteDocumentation);
            this.Documentation = (documents ?? [])
                .Where(document => document != null)
                .Select(document => document.Body)
                .Where(body => !string.IsNullOrWhiteSpace(body))
                .ToArray();

            var annotation = GetAnnotation(element);

            this.Sections = new (string Title, IReadOnlyList<KeyValuePair<string, string>> Properties, IReadOnlyList<string> Paragraphs)[]
            {
                ("Identity", identity.ToArray(), []),
                ("Attributes", attributes.ToArray(), []),
                ("Annotation", annotation.ToArray(), []),
                ("Documentation", incompleteDocumentation
                    ? [new("Documentation", IncompleteProperty)] : [], this.Documentation),
                ("Value", value.ToArray(), []),
                ("Classification", classification.ToArray(), [])
            }.Where(section => section.Properties.Count > 0 || section.Paragraphs.Count > 0).ToArray();
            this.AdvancedProperties = advanced.ToArray();
            var identityInExpression = this.Expressions.SelectMany(node => node.OwnedGroups)
                .Any(node => ReferenceEquals(node.Element, element) || ReferenceEquals(node.Membership, element));
            this.ExpressionAdvancedProperties = identityInExpression
                ? this.AdvancedProperties.Where(property => property.Key is not "Element ID" and not "SDK ID").ToArray()
                : this.AdvancedProperties;
        }

        /// <summary>Collects SDK classification and feature flags without resolving model semantics locally.</summary>
        private List<KeyValuePair<string, string>> GetClassification(IElement element, List<KeyValuePair<string, string>> advanced)
        {
            var classification = new List<KeyValuePair<string, string>>();
            Add(classification, MultiplicityLabel, ReadText(() => ElementPresentation.GetMultiplicity(element)));
            if (element is IType && this.ModelLinks.Any(link => link.Role == MultiplicityLabel && link.Element is not null)
                                 && !classification.Any(property => property.Key == MultiplicityLabel))
            {
                Add(classification, MultiplicityLabel, "Nonliteral or unresolved bounds; inspect Multiplicity in model links");
            }

            if (element is IType type)
            {
                Add(advanced, "Is abstract", type.IsAbstract.ToString().ToLowerInvariant());
            }

            if (element is IFeature feature)
            {
                var types = Read(() => feature.type, out var incompleteTypes);
                Add(classification, "Typed by", incompleteTypes ? IncompleteProperty
                    : string.Join(", ", (types ?? []).Where(candidate => candidate != null)
                        .Select(candidate => ReadText(() => string.IsNullOrWhiteSpace(candidate.qualifiedName)
                            ? GetDisplayName(candidate) : candidate.qualifiedName))));
                Add(classification, "Composition", feature.IsComposite ? "composite" : "reference");
                Add(advanced, "Is ordered", feature.IsOrdered.ToString().ToLowerInvariant());
                Add(advanced, "Is unique", feature.IsUnique.ToString().ToLowerInvariant());
            }

            if (element is IUsage usage)
            {
                Add(advanced, "Is variation", usage.IsVariation.ToString().ToLowerInvariant());
            }

            return classification;
        }

        /// <summary>Preserves comment and textual representation content.</summary>
        private static List<KeyValuePair<string, string>> GetAnnotation(IElement element)
        {
            var annotation = new List<KeyValuePair<string, string>>();
            if (element is IComment comment and not IDocumentation)
            {
                Add(annotation, "Body", comment.Body);
                Add(annotation, "Locale", comment.Locale);
            }
            if (element is ITextualRepresentation representation)
            {
                Add(annotation, "Body", representation.Body);
                Add(annotation, "Language", representation.Language);
            }

            return annotation;
        }

        /// <summary>Collects declared and SDK-derived identity with naming provenance.</summary>
        private static void AddIdentityProperties(IElement element, List<KeyValuePair<string, string>> identity, List<KeyValuePair<string, string>> advanced)
        {
            Add(identity, "Declared name", element.DeclaredName);
            Add(identity, "Declared short name", element.DeclaredShortName);
            var effectiveName = Read(() => element.name, out var incompleteName);
            var effectiveShortName = Read(() => element.shortName, out var incompleteShortName);
            Add(identity, "Effective name", incompleteName ? "Unresolved (incomplete model)" : effectiveName);
            Add(identity, "Effective short name", incompleteShortName ? "Unresolved (incomplete model)" : effectiveShortName);
            Add(advanced, "Model kind", ElementPresentation.GetKindName(element.GetType()));
            Add(advanced, "Aliases", string.Join(", ", element.AliasIds ?? []));
            if (element is IRelationship relationship) Add(advanced, "Is implied", relationship.IsImplied.ToString().ToLowerInvariant());
            if (element is IFeature namedFeature)
            {
                var namingFeature = Read(namedFeature.NamingFeature, out var incompleteNaming);
                var source = element switch
                {
                    _ when !string.IsNullOrWhiteSpace(element.DeclaredName) => "Declared name",
                    _ when !string.IsNullOrWhiteSpace(element.DeclaredShortName) => "Declared short name",
                    _ when namingFeature is not null => "SDK naming feature (see model links)",
                    _ when incompleteNaming || incompleteName || incompleteShortName => "Unresolved name",
                    _ when !string.IsNullOrWhiteSpace(effectiveName) || !string.IsNullOrWhiteSpace(effectiveShortName) => "SDK effective name",
                    _ => "Anonymous feature"
                };
                Add(identity, "Name source", source);
            }
            Add(identity, "Qualified name", ReadText(() => element.qualifiedName));

            var membership = Read(() => element.owningMembership, out var incompleteMembership);
            if (incompleteMembership)
            {
                Add(identity, "Visibility", IncompleteProperty);
            }
            else if (element is IMembership || membership is not null)
            {
                Add(identity, "Visibility", (element as IMembership ?? membership).Visibility.ToString().ToLowerInvariant());
            }
        }

        /// <summary>
        /// Gets the selected element's heading.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>Gets the canonical element described by this snapshot.</summary>
        public IElement Element { get; }

        /// <summary>Gets direct canonical links, including structures omitted from the browser.</summary>
        public IReadOnlyList<ElementLinkPresentation> ModelLinks { get; }

        /// <summary>Gets preserved identifiers and low-level flags shown only on request.</summary>
        public IReadOnlyList<KeyValuePair<string, string>> AdvancedProperties { get; }

        /// <summary>Gets selected-element metadata without repeating identity already retained by expression groups.</summary>
        public IReadOnlyList<KeyValuePair<string, string>> ExpressionAdvancedProperties { get; }

        /// <summary>Gets the selected element's owned expression structure.</summary>
        public IReadOnlyList<ExpressionPresentation> Expressions { get; }

        /// <summary>Gets the outermost SDK notation groups without repeating nested literal text.</summary>
        public IReadOnlyList<ExpressionPresentation> ExpressionNotations { get; }

        /// <summary>Collects notation already produced by the SDK adapter in presentation order.</summary>
        private static IEnumerable<ExpressionPresentation> GetNotationRoots(ExpressionPresentation node)
        {
            return node.TextualNotation is not null ? [node] : node.Children.SelectMany(GetNotationRoots);
        }

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
        /// Describes every owned value without evaluating expressions or concealing absent scalar values.
        /// </summary>
        private string GetValue(IElement element)
        {
            var values = Read(() => element.OwnedRelationship?.OfType<IFeatureValue>().ToArray() ?? [], out var incomplete);
            if (incomplete)
            {
                return UnresolvedValue;
            }
            if (values.Length == 0)
            {
                return "No local value; inherited/default values not resolved";
            }
            var descriptions = new List<string>();
            foreach (var value in values)
            {
                AddValueDescriptions(value, descriptions);
            }
            return descriptions.Count == 1 ? descriptions[0]
                : $"Multiple values ({descriptions.Count}): {string.Join("; ", descriptions.Select((value, index) => $"{index + 1}: {value}"))}";
        }

        /// <summary>Preserves each owned value and its stored binding, default or initialization flags.</summary>
        private void AddValueDescriptions(IFeatureValue value, List<string> descriptions)
        {
            var origin = (value.IsInitial, value.IsDefault) switch
            {
                (true, true) => "Initial default",
                (true, false) => "Initial value",
                (false, true) => "Default value",
                _ => "Local binding"
            };
            var owned = Read(() => value.OwnedRelatedElement?.OfType<IExpression>().ToArray(), out var incompleteOwned);
            if (incompleteOwned)
            {
                descriptions.Add(UnresolvedValue);
            }
            else if (owned is { Length: > 0 })
            {
                descriptions.AddRange(owned.Select(expression => $"{origin}: {this.DescribeValue(expression)}"));
            }
            else
            {
                var expression = Read(() => value.value, out var incompleteValue);
                descriptions.Add(incompleteValue || expression is null ? UnresolvedValue : $"{origin}: {this.DescribeValue(expression)}");
            }
        }

        /// <summary>Distinguishes unsupported notation from an absent or incomplete value.</summary>
        private string DescribeValue(IExpression expression)
        {
            var value = Read(() => ElementPresentation.GetLiteral(expression), out var incomplete);
            if (incomplete) return UnresolvedValue;
            if (value is not null) return value;
            var notation = this.Expressions.SelectMany(node => node.OwnedGroups)
                .FirstOrDefault(node => ReferenceEquals(node.Element, expression) && node.TextualNotation is not null)?.TextualNotation
                ?? SdkExpressionText.Create(expression);
            return notation.Text ?? notation.UnavailableReason;
        }

        /// <summary>Uses available SDK names without eagerly resolving names for declared elements.</summary>
        private static string GetDisplayName(IElement element) => ElementPresentation.GetDisplayName(element);

        /// <summary>Reads one textual property without suppressing unrelated exceptions.</summary>
        private static string ReadText(Func<string> read)
        {
            var value = Read(read, out var incomplete);
            return incomplete ? IncompleteProperty : value;
        }

        /// <summary>Isolates an incomplete SDK property from valid sibling properties.</summary>
        private static T Read<T>(Func<T> read, out bool incomplete)
        {
            try
            {
                incomplete = false;
                return read();
            }
            catch (IncompleteModelException)
            {
                incomplete = true;
                return default;
            }
        }

        /// <summary>
        /// Traverses owned members through relationship intermediaries without entering nested features.
        /// </summary>
        private static IEnumerable<IElement> GetOwnedMembers(IElement element, List<KeyValuePair<string, string>> notices)
        {
            var children = Read(() => element.ownedElement, out var incomplete);
            if (incomplete)
            {
                notices.Add(new("Owned attributes", IncompleteProperty));
            }
            foreach (var child in children ?? [])
            {
                if (child is IRelationship and not IType)
                {
                    foreach (var member in GetOwnedMembers(child, notices))
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
