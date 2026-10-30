// ------------------------------------------------------------------------------------------------
// <copyright file="DetailsPanelPresentation.Semantics.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.DetailsPanel
{
    using Mycelium.Bloom.ViewModel.ProjectBrowser;
    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Core.Types;
    using SysML2.NET.Core.POCO.Kernel.Connectors;
    using SysML2.NET.Core.POCO.Kernel.FeatureValues;
    using SysML2.NET.Core.POCO.Kernel.Functions;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.Multiplicities;
    using SysML2.NET.Core.POCO.Root.Annotations;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Attributes;
    using SysML2.NET.Core.POCO.Systems.DefinitionAndUsage;
    using SysML2.NET.Core.POCO.Systems.Enumerations;
    using SysML2.NET.Core.POCO.Systems.Metadata;
    using SysML2.NET.Core.POCO.Systems.Requirements;
    using SysML2.NET.Core.POCO.Systems.States;

    public sealed partial class DetailsPanelPresentation
    {
        /// <summary>Gets the sections in their semantic destination and design order.</summary>
        public IReadOnlyList<PropertySectionPresentation> ContentSections { get; }

        /// <summary>Gets the established semantic kind, shared with Browser classification.</summary>
        public string Kind => ProjectBrowserElementPresentation.For(this.Element.GetType()).Stereotype;

        /// <summary>Gets compact type and value information for the selection summary.</summary>
        public string Summary { get; private set; }

        /// <summary>Gets the explicit feature composition badge for the selected element.</summary>
        public string Composition { get; private set; }

        /// <summary>Gets the number of direct owned attributes in the selected element.</summary>
        public int AttributeCount { get; private set; }

        /// <summary>Builds the read-only Part/Attribute shell and explicitly supported semantic sections.</summary>
        /// <returns>Sections containing available model information, without synthetic SDK dumps.</returns>
        private IReadOnlyList<PropertySectionPresentation> BuildContentSections()
        {
            var sections = new List<PropertySectionPresentation>();
            var identity = this.Sections.Single(section => section.Title == "Identity");
            sections.Add(identity with
            {
                Properties = BuildIdentityFields(identity)
            });
            var memberNotices = new List<KeyValuePair<string, string>>();
            var members = GetOwnedMembers(this.Element, memberNotices).ToArray();
            var attributes = members.OfType<IAttributeUsage>().ToArray();
            this.AttributeCount = attributes.Length;
            if (attributes.Length > 0)
            {
                sections.Add(new("Attributes", [], [])
                {
                    Links = attributes.Select(attribute => new ElementLinkPresentation(GetDisplayName(attribute),
                        attribute,
                        GetCompactValue(attribute) ?? this.GetValue(attribute))).ToArray()
                });
            }

            if (memberNotices.Count > 0)
            {
                sections.Add(new("Owned features", memberNotices, []));
            }

            sections.AddRange(
                this.Sections.Where(section => section.Title is "Documentation" or "Annotation" or "Value"));
            var classification = this.Sections.FirstOrDefault(section => section.Title == "Classification");
            var fields = classification?.Properties.ToList() ?? [];
            fields.AddRange(this.AdvancedProperties.Where(property =>
                property.Key is "Is abstract" or "Is variation" or "Is ordered" or "Is unique"));
            Add(fields, "Kind", this.Kind);
            var typeLinks = new List<ElementLinkPresentation>();
            if (this.Element is IFeature feature)
            {
                this.Composition = feature.IsComposite ? "composite" : "ref";
                Add(fields, "Direction", feature.Direction.ToString().ToLowerInvariant());
                Add(fields, "Is constant", feature.IsConstant.ToString().ToLowerInvariant());
                Add(fields, "Is variable", feature.IsVariable.ToString().ToLowerInvariant());
                var types = Read(() => feature.type, out _);
                typeLinks.AddRange((types ?? []).Select(type => Link("Typed by", type) with { Category = "typing" }));
                if (typeLinks.Count > 0)
                {
                    fields.RemoveAll(property => property.Key == "Typed by");
                }

                this.Summary = this.Element is IAttributeUsage
                    ? GetCompactValue(this.Element)
                    : string.Join(", ", typeLinks.Select(link => link.Label));
            }

            if (this.Element is IDefinition definition)
            {
                Add(fields, "Is variation", definition.IsVariation.ToString().ToLowerInvariant());
            }

            sections.Add(new("Classification", fields, []) { Links = typeLinks });
            this.AddExpressions(sections);
            this.AddState(sections, members);
            this.AddEnds(sections);
            this.AddAnnotations(sections, members);
            this.AddRelationships(sections, members);
            return sections
                .Where(section => section.Properties.Count + section.Paragraphs.Count + section.Links.Count > 0)
                .ToArray();
        }

        /// <summary>Labels SDK effective names consistently while retaining a differing or absent declaration.</summary>
        /// <param name="identity">The cached identity facts, including incomplete-model explanations.</param>
        /// <returns>Design-facing identity fields without synthesizing a name.</returns>
        private static IReadOnlyList<KeyValuePair<string, string>> BuildIdentityFields(PropertySectionPresentation identity)
        {
            var facts = identity.Properties.ToDictionary(property => property.Key, property => property.Value);
            var effectiveName = facts.GetValueOrDefault("Effective name");
            var effectiveShortName = facts.GetValueOrDefault("Effective short name");
            var fields = new List<KeyValuePair<string, string>> { new("name", effectiveName ?? "No effective name") };
            Add(fields, "shortName", effectiveShortName);
            Add(fields, "qualifiedName", facts.GetValueOrDefault("Qualified name"));
            Add(fields, "visibility", facts.GetValueOrDefault("Visibility"));
            if (facts.GetValueOrDefault("Declared name") != effectiveName)
            {
                Add(fields, "declaredName", facts.GetValueOrDefault("Declared name"));
            }
            if (effectiveShortName is not null && facts.GetValueOrDefault("Declared short name") != effectiveShortName)
            {
                Add(fields, "declaredShortName", facts.GetValueOrDefault("Declared short name") ?? "Not declared");
            }
            return fields;
        }

        /// <summary>Places stored expressions and their canonical references beside the owning values.</summary>
        /// <param name="sections">The sections being assembled.</param>
        private void AddExpressions(List<PropertySectionPresentation> sections)
        {
            var links = this.Expressions.SelectMany(expression => expression.OwnedGroups)
                .Where(expression => expression.Reference != null)
                .Select(expression => Link(expression.ReferenceRole ?? "Reference", expression.Reference))
                .DistinctBy(link => (link.Role, link.Element)).ToArray();
            var values = this.ExpressionNotations.Select(expression => new KeyValuePair<string, string>(expression.Role,
                expression.TextualNotation.Text ?? "Expression is not supported for display")).ToArray();
            if (this.Element is not IAttributeUsage)
            {
                sections.Add(new("Expressions", values, []) { Links = links });
            }
            else
            {
                var valuesAndReferences = this.ExpressionNotations
                    .Where(expression => expression.Element is IExpression and not IUsage)
                    .Select(expression => Link("Value expression", expression.Element))
                    .Concat(links).DistinctBy(link => (link.Role, link.Element)).ToArray();
                sections.Add(new("Value references", [], []) { Links = valuesAndReferences });
            }

            if (this.Element is IType type && Read(() => type.multiplicity, out _) is IMultiplicityRange range)
            {
                var bounds = new List<ElementLinkPresentation>();
                AddLink(bounds, "Lower bound", () => range.lowerBound, false);
                AddLink(bounds, "Upper bound", () => range.upperBound);
                sections.Add(new("Multiplicity", [], []) { Links = bounds });
            }
        }

        /// <summary>Describes state structure, explicit initial state, and transition roles.</summary>
        /// <param name="sections">The sections being assembled.</param>
        /// <param name="members">Direct owned members, without descending into nested features.</param>
        private void AddState(List<PropertySectionPresentation> sections, IElement[] members)
        {
            if (this.Element is IStateDefinition or IStateUsage)
            {
                var parallel = this.Element is IStateDefinition definition
                    ? definition.IsParallel
                    : ((IStateUsage)this.Element).IsParallel;
                var structure = new List<ElementLinkPresentation>();
                var states = members.OfType<IStateUsage>().Where(state => state is not ITransitionUsage).ToArray();
                var initial = states
                    .Where(state => ProjectBrowserRowPresentation.For(state).Indicators.Contains("Initial")).ToArray();
                foreach (var state in initial)
                {
                    structure.Add(Link("Initial state", state));
                }

                structure.AddRange(states.Select(state => Link("Nested state", state)));
                sections.Add(new("State structure", [new("Execution", parallel ? "Parallel" : "Exclusive")],
                        initial.Length == 0 && !parallel ? ["No unconditional initial state resolved"] : [])
                { Links = structure });
                var actions = new List<ElementLinkPresentation>();
                AddLink(actions, "Entry",
                    () => this.Element is IStateDefinition state
                        ? state.entryAction
                        : ((IStateUsage)this.Element).entryAction, false);
                AddLink(actions, "Do",
                    () => this.Element is IStateDefinition state
                        ? state.doAction
                        : ((IStateUsage)this.Element).doAction, false);
                AddLink(actions, "Exit",
                    () => this.Element is IStateDefinition state
                        ? state.exitAction
                        : ((IStateUsage)this.Element).exitAction, false);
                sections.Add(new("State behaviour", [], []) { Links = actions });
            }

            var transitions = this.Element is ITransitionUsage selected
                ? new[] { selected }
                : members.OfType<ITransitionUsage>().ToArray();
            foreach (var transition in transitions)
            {
                var links = new List<ElementLinkPresentation> { Link("Transition", transition) };
                AddLink(links, "Source", () => transition.source);
                AddLink(links, "Target", () => transition.target);
                AddLinks(links, "Trigger", () => transition.triggerAction);
                AddLinks(links, "Guard", () => transition.guardExpression);
                AddLinks(links, "Effect", () => transition.effectAction);
                sections.Add(new("Transitions", [], []) { Links = links });
            }

            foreach (var succession in members.OfType<ISuccession>().Where(member => member is not ITransitionUsage))
            {
                var links = new List<ElementLinkPresentation>();
                AddLink(links, "Source", () => succession.sourceFeature);
                var targets = Read(() => succession.targetFeature, out _);
                links.AddRange((targets ?? []).Select(target => Link("Target", target)));
                sections.Add(new("Control sequencing", [], []) { Links = links, Tab = "Relations" });
            }
        }

        /// <summary>Displays connector and association ends with referents, multiplicity and direction.</summary>
        /// <param name="sections">The sections being assembled.</param>
        private void AddEnds(List<PropertySectionPresentation> sections)
        {
            if (this.Element is not IType type)
            {
                return;
            }

            var ends = Read(() => this.Element is IConnector connector ? connector.connectorEnd : type.endFeature,
                out var incomplete);
            if (incomplete)
            {
                sections.Add(new("Ends", [], [IncompleteProperty]));
            }

            foreach (var end in ends ?? [])
            {
                var links = new List<ElementLinkPresentation> { Link("End", end) };
                foreach (var subsetting in end.OwnedRelationship?.OfType<IReferenceSubsetting>() ?? [])
                {
                    AddLink(links, "Referent", () => subsetting.ReferencedFeature);
                }

                var fields = new List<KeyValuePair<string, string>>();
                Add(fields, "Multiplicity", ReadText(() => ElementPresentation.GetMultiplicity(end)));
                Add(fields, "Direction", end.Direction.ToString().ToLowerInvariant());
                sections.Add(new("Ends", fields, []) { Links = links });
            }
        }

        /// <summary>Preserves owned and applied annotation bodies without inventing author or audit data.</summary>
        /// <param name="sections">The sections being assembled.</param>
        /// <param name="members">Direct owned members.</param>
        private void AddAnnotations(List<PropertySectionPresentation> sections, IElement[] members)
        {
            var applied =
                Read(
                    () => this.Element.ownedAnnotation?.Select(annotation => annotation.annotatingElement)
                        .OfType<IElement>().ToArray(), out _);
            var annotations = members.Concat(applied ?? []).Append(this.Element)
                .Distinct(ReferenceEqualityComparer.Instance).OfType<IElement>();
            foreach (var annotation in annotations)
            {
                if (annotation is IComment comment and not IDocumentation)
                {
                    sections.Add(new("Comments",
                            string.IsNullOrWhiteSpace(comment.Locale) ? [] : [new("Locale", comment.Locale)],
                            [comment.Body ?? ""])
                    { Tab = "Comments", Links = [Link("Comment", comment)] });
                }

                if (annotation is ITextualRepresentation text)
                {
                    sections.Add(new("Textual representation", [new("Language", text.Language ?? "Not specified")],
                            [text.Body ?? ""])
                    { Tab = "Comments", Links = [Link("Representation", text)] });
                }

                if (annotation is IMetadataUsage metadata)
                {
                    var links = new List<ElementLinkPresentation> { Link("Annotation", metadata) };
                    AddLink(links, "Definition", () => metadata.metadataDefinition);
                    var values = GetOwnedMembers(metadata, []).OfType<IFeature>()
                        .Select(attribute =>
                            new KeyValuePair<string, string>(GetDisplayName(attribute), this.GetValue(attribute)))
                        .ToArray();
                    sections.Add(new("Metadata", values, []) { Links = links });
                }
            }
        }

        /// <summary>Places import declarations, specializations and other relationships in named sections.</summary>
        /// <param name="sections">The sections being assembled.</param>
        /// <param name="members">Direct owned members.</param>
        private void AddRelationships(List<PropertySectionPresentation> sections, IElement[] members)
        {
            var relationships = this.Element.OwnedRelationship ?? [];
            foreach (var import in relationships.OfType<IImport>()
                         .Concat(this.Element is IImport selectedImport ? [selectedImport] : []))
            {
                var links = new List<ElementLinkPresentation>();
                AddLink(links, "Target", () => import.importedElement);
                sections.Add(new("Imports", [
                    new("Kind", import is INamespaceImport ? "Namespace import" : "Membership import"),
                    new("Recursive", import.IsRecursive.ToString().ToLowerInvariant()),
                    new("Import all", import.IsImportAll.ToString().ToLowerInvariant()),
                    new("Visibility", import.Visibility.ToString().ToLowerInvariant())
                ], [])
                { Links = links, Tab = "Relations" });
            }

            var generalizations = new List<ElementLinkPresentation>();
            foreach (var specialization in relationships.OfType<ISpecialization>())
            {
                AddLink(generalizations, ElementPresentation.GetKindName(specialization.GetType()),
                    () => specialization.General);
                generalizations[^1] = generalizations[^1] with { Category = "specialization" };
            }

            foreach (var conjugation in relationships.OfType<IConjugation>())
            {
                AddLink(generalizations, "Conjugates", () => conjugation.OriginalType);
            }

            var hierarchyNotices = new List<string>();
            if (this.Element is IType selectedType)
            {
                generalizations.AddRange(GetLoadedSpecializations(selectedType, hierarchyNotices));
            }

            sections.Add(new("Specializations / Type hierarchy", [], hierarchyNotices)
            { Links = generalizations, Tab = "Relations" });
            if (this.Element is IType conjugated && Read(() => conjugated.ownedConjugator, out _) is not null)
            {
                var inherited = Read(() => conjugated.inheritedFeature, out var incomplete);
                sections.Add(new("Inherited features", [], incomplete ? [IncompleteProperty] : [])
                {
                    Tab = "Relations",
                    Links = (inherited ?? []).Select(feature =>
                        Link(
                            ReadText(() => conjugated.DirectionOf(feature)?.ToString().ToLowerInvariant()) ??
                            "Unspecified direction", feature)).ToArray()
                });
            }

            foreach (var constraint in relationships.OfType<IRequirementConstraintMembership>())
            {
                var links = new List<ElementLinkPresentation>();
                AddLink(links, constraint.Kind.ToString(),
                    () => constraint.referencedConstraint ?? constraint.ownedConstraint);
                sections.Add(new("Constraints", [], []) { Links = links });
            }

            if (this.Element is IEnumerationDefinition)
            {
                foreach (var value in members.OfType<IEnumerationUsage>())
                {
                    var documentation = Read(() => value.documentation, out _);
                    var properties = value.OwnedRelationship?.OfType<IFeatureValue>().Any() == true
                        ? new KeyValuePair<string, string>[] { new("Value", this.GetValue(value)) }
                        : [];
                    sections.Add(new("Enumeration values", properties,
                            (documentation ?? []).Select(document => document.Body)
                            .Where(body => !string.IsNullOrWhiteSpace(body)).ToArray())
                    { Links = [Link("Name", value)] });
                }
            }

            var variants = relationships.OfType<IVariantMembership>()
                .Select(membership => Link("Variant", membership.MemberElement)).ToArray();
            if (this.Element is not IEnumerationDefinition)
            {
                sections.Add(new("Variants", [], []) { Links = variants });
            }

            var memberships = relationships.OfType<IMembership>()
                .Where(membership => membership is not IOwningMembership)
                .Select(membership =>
                    Link($"{membership.MemberName ?? "Member"} ({membership.Visibility.ToString().ToLowerInvariant()})",
                        membership.MemberElement)).ToArray();
            sections.Add(new("Memberships", [], []) { Links = memberships, Tab = "Relations" });
            var other = relationships.Where(relationship => relationship is not IMembership and not ISpecialization
                    and not IImport and not IConjugation and not IAnnotation)
                .Concat(members.OfType<IRelationship>().Where(relationship => relationship is not IType)).ToList();
            if (this.Element is IRelationship selectedRelationship)
            {
                other.Add(selectedRelationship);
            }

            foreach (var relationship in other.Distinct())
            {
                var sources = Read(() => relationship.Source, out _);
                var targets = Read(() => relationship.Target, out _);
                sections.Add(new("Relationships",
                    [new("Kind", ElementPresentation.GetKindName(relationship.GetType()))], [])
                {
                    Tab = "Relations",
                    Links = (sources ?? []).Select(source => Link("Source", source))
                        .Concat((targets ?? []).Select(target => Link("Target", target))).ToArray()
                });
            }
        }

        /// <summary>Finds direct inverse specializations in the loaded ownership graph without following reference edges.</summary>
        /// <param name="type">The canonical selected type.</param>
        /// <param name="notices">Explanations when the bounded loaded graph is incomplete.</param>
        /// <returns>Canonical specific types in model ownership order.</returns>
        private static IReadOnlyList<ElementLinkPresentation> GetLoadedSpecializations(IType type, List<string> notices)
        {
            IElement root = type;
            var ancestors = new HashSet<IElement>(ReferenceEqualityComparer.Instance) { root };
            while (Read(() => root.owner, out _) is { } owner && ancestors.Add(owner))
            {
                root = owner;
            }

            var pending = new Queue<IElement>();
            pending.Enqueue(root);
            var visited = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
            var links = new List<ElementLinkPresentation>();
            while (pending.TryDequeue(out var element) && visited.Count < 4096)
            {
                if (!visited.Add(element))
                {
                    continue;
                }

                if (element is ISpecialization specialization &&
                    ReferenceEquals(Read(() => specialization.General, out _), type))
                {
                    AddLink(links, "Specialized by", () => specialization.Specific);
                }

                var children = Read(() => element.ownedElement, out var incomplete);
                if (incomplete && !notices.Contains(IncompleteProperty))
                {
                    notices.Add(IncompleteProperty);
                }

                foreach (var child in children ?? [])
                {
                    if (child is not null)
                    {
                        pending.Enqueue(child);
                    }
                }

                foreach (var relationship in element.OwnedRelationship ?? [])
                {
                    pending.Enqueue(relationship);
                }
            }

            if (pending.Count > 0)
            {
                notices.Add(
                    "Some specializations are not shown because the loaded model exceeds the inspection limit.");
            }

            return links.DistinctBy(link => link.Element, ReferenceEqualityComparer.Instance).ToArray();
        }

        /// <summary>Creates a canonical reference, using supported expression text where appropriate.</summary>
        /// <param name="role">The semantic role.</param>
        /// <param name="element">The unchanged SDK target.</param>
        /// <returns>A navigable reference or an explicit unresolved value.</returns>
        private static ElementLinkPresentation Link(string role, IElement element)
        {
            return new(role, element, element is null
                ? "Not resolved"
                : element is ILiteralInfinity && role is "Lower bound" or "Upper bound"
                    ? "*"
                : element is IExpression expression and not IUsage
                    ? ExpressionText(expression)
                    : GetDisplayName(element));
        }

        /// <summary>Reads a reference without hiding incomplete data behind an absent section.</summary>
        /// <param name="links">The reference collection.</param>
        /// <param name="role">The semantic role.</param>
        /// <param name="read">The SDK property accessor.</param>
        /// <param name="required">Whether an absent target needs a visible explanation.</param>
        private static void AddLink(List<ElementLinkPresentation> links, string role, Func<IElement> read,
            bool required = true)
        {
            var target = Read(read, out var incomplete);
            if (incomplete || required || target is not null)
            {
                links.Add(incomplete ? new(role, null, IncompleteProperty) : Link(role, target));
            }
        }

        /// <summary>Preserves every reference in an SDK role collection.</summary>
        /// <param name="links">The reference collection.</param>
        /// <param name="role">The semantic role.</param>
        /// <param name="read">The SDK collection accessor.</param>
        private static void AddLinks(List<ElementLinkPresentation> links, string role, Func<IEnumerable<IElement>> read)
        {
            var targets = Read(() => read()?.ToArray(), out var incomplete);
            if (incomplete)
            {
                links.Add(new(role, null, IncompleteProperty));
            }
            else
            {
                links.AddRange((targets ?? []).Select(target => Link(role, target)));
            }
        }

        /// <summary>Formats only bounded supported expressions without evaluation.</summary>
        /// <param name="expression">The stored expression.</param>
        /// <returns>Stored notation or a clear unsupported state.</returns>
        private static string ExpressionText(IExpression expression)
        {
            return ElementPresentation.GetExpressionText(expression) ?? "Expression is not supported for display";
        }

        /// <summary>Formats an unambiguous stored value without calling singular SDK accessors on malformed multi-value data.</summary>
        /// <param name="element">The feature owning the value.</param>
        /// <returns>Compact supported notation, or null when a detailed value state is needed.</returns>
        private static string GetCompactValue(IElement element)
        {
            var values = element.OwnedRelationship?.OfType<IFeatureValue>().ToArray() ?? [];
            if (values.Length != 1 || values[0].OwnedRelatedElement?.OfType<IExpression>().Count() > 1)
            {
                return null;
            }

            return Read(() => ElementPresentation.GetExpressionText(values[0].value), out _);
        }
    }
}
