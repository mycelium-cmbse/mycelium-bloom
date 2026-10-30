// ------------------------------------------------------------------------------------------------
// <copyright file="ExpressionPresentation.cs" company="Starion Group S.A.">
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
    using SysML2.NET.Core.POCO.Kernel.Behaviors;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.FeatureValues;
    using SysML2.NET.Core.POCO.Kernel.Functions;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.DefinitionAndUsage;
    using SysML2.NET.Exceptions;


    /// <summary>
    /// Represents one read-only expression group with its canonical element and membership.
    /// </summary>
    public sealed class ExpressionPresentation
    {
        /// <summary>Gets the canonical element, or null for unresolved structure.</summary>
        public IElement Element { get; private init; }

        /// <summary>Gets the canonical owning membership, when available.</summary>
        public IMembership Membership { get; private init; }

        /// <summary>Gets whether element inspection offers more than an anonymous empty feature's inline metadata.</summary>
        public bool CanInspectElement { get; private init; }

        /// <summary>Gets whether membership inspection offers additional structure or information.</summary>
        public bool CanInspectMembership { get; private init; }

        /// <summary>Gets the structural role without assigning a model name.</summary>
        public string Role { get; private init; }

        /// <summary>Gets the semantic name or kind.</summary>
        public string Label { get; private init; }

        /// <summary>Gets whether the compact summary has an expression kind or a declared model name to display.</summary>
        public bool HasSummaryLabel => this.Element is IExpression
                                       || !string.IsNullOrWhiteSpace(this.Element?.DeclaredName)
                                       || !string.IsNullOrWhiteSpace(this.Element?.DeclaredShortName);

        /// <summary>Gets the stored operator, when present.</summary>
        public string Operator { get; private init; }

        /// <summary>Gets the stored string value or SDK-produced scalar notation without reconstructing expression syntax.</summary>
        public string LiteralText { get; private init; }

        /// <summary>Gets an explicit value state when the SDK contains no owned feature value.</summary>
        public string ValueState { get; private init; }

        /// <summary>Gets SDK properties, including explicit incomplete-property states.</summary>
        public IReadOnlyList<KeyValuePair<string, string>> Properties { get; private init; } = [];

        /// <summary>Gets the canonical target without following it as ownership.</summary>
        public IElement Reference { get; private init; }

        /// <summary>Gets the reference role, or null for an element without a reference.</summary>
        public string ReferenceRole { get; private init; }

        /// <summary>Gets the reference label or its absence or resolution state.</summary>
        public string ReferenceLabel { get; private init; }

        /// <summary>Gets children in SDK ownership order.</summary>
        public IReadOnlyList<ExpressionPresentation> Children { get; private init; } = [];

        /// <summary>Gets a visible explanation for incomplete or bounded traversal.</summary>
        public string Notice { get; private init; }

        /// <summary>Gets SDK-generated notation for a displayed expression root.</summary>
        public SdkExpressionText TextualNotation { get; private set; }

        /// <summary>Enumerates the bounded owned projection in canonical order for advanced inspection.</summary>
        public IEnumerable<ExpressionPresentation> OwnedGroups =>
            this.Children.SelectMany(child => child.OwnedGroups).Prepend(this);

        /// <summary>Gets a single owned value path combined visually while retaining every canonical identity.</summary>
        public IReadOnlyList<ExpressionPresentation> DisplayPath => this.GetDisplayPath();

        /// <summary>Collects a single owned value path while preserving canonical identities.</summary>
        /// <returns>The ordered visible path starting at this expression.</returns>
        private IReadOnlyList<ExpressionPresentation> GetDisplayPath()
        {
            var result = new List<ExpressionPresentation> { this };
            var current = this;
            while (current.Notice is null && current.ReferenceRole is null
                                          && string.IsNullOrWhiteSpace(current.Operator)
                                          && (current.Element is IExpression || !current.HasSummaryLabel)
                                          && current.Children.Count == 1
                                          && current.Children[0].Membership is IFeatureValue
                                              or IResultExpressionMembership)
            {
                current = current.Children[0];
                result.Add(current);
            }

            return result;
        }

        /// <summary>Creates a bounded projection without evaluating expressions or mutating SDK objects.</summary>
        /// <param name="element">The canonical selection.</param>
        /// <returns>The selected element's expression groups.</returns>
        public static IReadOnlyList<ExpressionPresentation> Create(IElement element)
        {
            ArgumentNullException.ThrowIfNull(element);
            var nodes = new Builder().BuildSelection(element);
            foreach (var node in nodes)
            {
                PopulateText(node);
            }

            return nodes;
        }

        /// <summary>Requests SDK notation at expression roots while keeping operand inspection separate.</summary>
        private static void PopulateText(ExpressionPresentation node)
        {
            if (node.Element is IExpression expression and not IUsage)
            {
                node.TextualNotation = HasUnavailableStructure(node)
                    ? new(null,
                        "Notation unavailable: the displayed structure is incomplete or exceeds its display limit.")
                    : SdkExpressionText.Create(expression);
                return;
            }

            foreach (var child in node.Children)
            {
                PopulateText(child);
            }
        }

        /// <summary>Protects serialization from cyclic, truncated, or unresolved projected structure.</summary>
        private static bool HasUnavailableStructure(ExpressionPresentation node)
        {
            return node.Notice is not null
                   || node.ReferenceRole is not null && node.Reference is null
                   || node.Children.Any(HasUnavailableStructure);
        }

        /// <summary>Owns traversal limits for one immutable selection snapshot.</summary>
        private sealed class Builder
        {
            /// <summary>Reads one optional SDK property without suppressing unrelated failures.</summary>
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

            /// <summary>Retains inspection unless an anonymous structural feature is confirmed empty.</summary>
            private static bool HasInspectionContent(IElement element)
            {
                if (element is not IFeature feature || element is IExpression or IUsage
                                                    || !string.IsNullOrWhiteSpace(element.DeclaredName)
                                                    || !string.IsNullOrWhiteSpace(element.DeclaredShortName))
                {
                    return true;
                }

                var name = Read(() => element.name, out var incompleteName);
                var relationships = Read(() => element.OwnedRelationship, out var incompleteRelationships);
                var types = Read(() => feature.type, out var incompleteTypes);
                var additionalContent = Read(() => feature.multiplicity is not null
                                                   || feature.documentation is { Count: > 0 } ||
                                                   feature.textualRepresentation is { Count: > 0 }
                                                   || feature.ownedAnnotation is { Count: > 0 },
                    out var incompleteContent);
                return incompleteName || incompleteRelationships || incompleteTypes || incompleteContent ||
                       additionalContent
                       || !string.IsNullOrWhiteSpace(name) || relationships is { Count: > 0 } || types is { Count: > 0 }
                       || feature.AliasIds is { Count: > 0 } || feature.IsComposite || feature.IsOrdered ||
                       feature.IsAbstract
                       || feature.IsDerived || feature.IsEnd || !feature.IsUnique || feature.IsConstant ||
                       feature.IsVariable
                       || feature.IsPortion || feature.IsSufficient || feature.IsImpliedIncluded;
            }

            /// <summary>Keeps memberships with additional information or unresolved structure inspectable.</summary>
            private static bool HasMembershipContent(IMembership membership, bool elementHasContent)
            {
                if (membership is null) return false;
                if (elementHasContent || !string.IsNullOrWhiteSpace(membership.DeclaredName)
                                      || !string.IsNullOrWhiteSpace(membership.DeclaredShortName)) return true;
                var members = Read(() => membership.OwnedRelatedElement, out var incompleteMembers);
                var relationships = Read(() => membership.OwnedRelationship, out var incompleteRelationships);
                return incompleteMembers || incompleteRelationships || members is { Count: > 1 }
                       || relationships is { Count: > 0 };
            }

            /// <summary>Uses available names before a semantic kind, without requiring qualified-name resolution.</summary>
            private static string Name(IElement element)
            {
                return ElementPresentation.GetDisplayName(element);
            }

            /// <summary>Bounds nesting without discarding access to canonical subtree roots.</summary>
            private const int MaximumDepth = 12;

            /// <summary>Bounds projected groups for a single selected element.</summary>
            private const int MaximumElements = 128;

            /// <summary>Tracks only the current ownership path so repeated references are not suppressed.</summary>
            private readonly HashSet<IElement> path = new(ReferenceEqualityComparer.Instance);

            /// <summary>Counts groups already projected for this selection.</summary>
            private int count;

            /// <summary>Projects an expression, parameter, or an element owning expression values.</summary>
            public ExpressionPresentation[] BuildSelection(IElement element)
            {
                if (element is IMembership selectedMembership)
                {
                    return BuildSelectedMembership(selectedMembership);
                }

                var membership = Read(() => element.owningMembership, out _);
                if (element is IExpression || element is IFeature && membership is IParameterMembership)
                {
                    var role = membership switch
                    {
                        IReturnParameterMembership => "Result",
                        IParameterMembership => "Input",
                        _ => "Expression"
                    };
                    return [this.Build(element, membership, role, 0)];
                }

                return this.Children(element, 0, true);
            }

            /// <summary>Projects selected membership contents while retaining their canonical ownership order.</summary>
            private ExpressionPresentation[] BuildSelectedMembership(IMembership selectedMembership)
            {
                var members = Read(() => selectedMembership.OwnedRelatedElement, out var incomplete);
                var role = selectedMembership switch
                {
                    IReturnParameterMembership => "Result",
                    IParameterMembership => "Input",
                    IFeatureValue => "Value",
                    _ => "Owned element"
                };
                if (members is { Count: > 0 })
                {
                    var result = new List<ExpressionPresentation>();
                    foreach (var member in members)
                    {
                        if (this.count >= MaximumElements)
                        {
                            result.Add(Unavailable("Remaining structure",
                                "Display limit reached; inspect the owning element."));
                            break;
                        }

                        result.Add(this.Build(member, selectedMembership, role, 0));
                    }

                    return result.ToArray();
                }

                return
                [
                    new ExpressionPresentation
                    {
                        Membership = selectedMembership, CanInspectMembership = true, Role = role,
                        Label = "Owned element",
                        Notice = incomplete
                            ? "Unresolved owned element (incomplete model)."
                            : "The membership has no resolved owned element."
                    }
                ];
            }

            /// <summary>Projects one canonical element and only its owned expression structure.</summary>
            private ExpressionPresentation Build(IElement element, IMembership membership, string role, int depth)
            {
                this.count++;
                var properties = GetProperties(element, membership);

                var referenceRole = GetReference(element, out var reference, out var referenceLabel);

                IReadOnlyList<ExpressionPresentation> children = [];
                var literal = element is ILiteralExpression ? SdkExpressionText.Create((IExpression)element) : null;
                string valueState = null;
                if (element is IFeature and not IExpression)
                {
                    var owned = Read(() => element.OwnedRelationship, out var incompleteValues);
                    if (incompleteValues) valueState = "Unresolved value (incomplete model)";
                    else if (!(owned ?? []).Any(relationship => relationship is IFeatureValue))
                        valueState = "No explicit value";
                }

                var notice = BuildChildren(element, depth, ref children);

                var canInspect = HasInspectionContent(element);
                return new ExpressionPresentation
                {
                    Element = element,
                    Membership = membership,
                    Role = role,
                    Label = Name(element),
                    CanInspectElement = canInspect,
                    CanInspectMembership = HasMembershipContent(membership, canInspect),
                    Operator = element switch
                    {
                        IFeatureChainExpression chain => chain.Operator,
                        IOperatorExpression operation => operation.Operator,
                        _ => null
                    },
                    LiteralText = element is ILiteralString text ? ElementPresentation.GetLiteral(text) : literal?.Text,
                    ValueState = valueState,
                    Properties = properties.ToArray(),
                    Reference = reference,
                    ReferenceRole = referenceRole,
                    ReferenceLabel = referenceLabel,
                    Children = children,
                    Notice = notice
                };
            }

            /// <summary>Reads the SDK reference and distinguishes absence from incomplete resolution.</summary>
            private static string GetReference(IElement element, out IElement reference, out string referenceLabel)
            {
                var referenceRole = element switch
                {
                    IFeatureChainExpression => "Target",
                    IFeatureReferenceExpression => "Reference",
                    _ => null
                };
                reference = null;
                referenceLabel = null;
                if (referenceRole is not null)
                {
                    reference = Read<IElement>(() => element is IFeatureChainExpression chain
                        ? chain.targetFeature
                        : ((IFeatureReferenceExpression)element).referent, out var incomplete);
                    var relationships = Read(() => element.OwnedRelationship, out var incompleteRelationships);
                    var hasReference =
                        relationships?.Any(candidate => candidate is IMembership and not IParameterMembership) == true;
                    var unavailableLabel = incomplete || incompleteRelationships || hasReference
                        ? "Unresolved reference"
                        : "Not specified";
                    referenceLabel = reference is not null ? Name(reference) : unavailableLabel;
                }

                return referenceRole;
            }

            /// <summary>Bounds ownership traversal and releases the current path even when projection fails.</summary>
            private string BuildChildren(IElement element, int depth,
                ref IReadOnlyList<ExpressionPresentation> children)
            {
                string notice = null;
                if (!this.path.Add(element))
                {
                    notice = "Ownership cycle; expansion stopped.";
                }
                else
                {
                    try
                    {
                        if (depth >= MaximumDepth || this.count >= MaximumElements)
                        {
                            notice = "Display limit reached. Inspect this element to explore its structure.";
                        }
                        else
                        {
                            children = this.Children(element, depth + 1, false);
                        }
                    }
                    finally
                    {
                        this.path.Remove(element);
                    }
                }

                return notice;
            }

            /// <summary>Preserves parameter direction, membership visibility and value provenance.</summary>
            private static List<KeyValuePair<string, string>> GetProperties(IElement element, IMembership membership)
            {
                var properties = new List<KeyValuePair<string, string>>();
                if (element is IFeature feature &&
                    (membership is IParameterMembership || feature.Direction is not null))
                {
                    properties.Add(
                        new("Direction", feature.Direction?.ToString().ToLowerInvariant() ?? "Not specified"));
                }

                if (membership is not null)
                {
                    properties.Add(new("Visibility", membership.Visibility.ToString().ToLowerInvariant()));
                }

                if (membership is IFeatureValue value)
                {
                    properties.Add(new("Default", value.IsDefault ? "true" : "false"));
                    properties.Add(new("Initial", value.IsInitial ? "true" : "false"));
                }

                return properties;
            }

            /// <summary>Preserves membership ordering and every resolved owned value instead of derived arguments.</summary>
            private ExpressionPresentation[] Children(IElement element, int depth, bool valuesOnly)
            {
                var result = new List<ExpressionPresentation>();
                var relationships = Read(() => element.OwnedRelationship, out var incompleteRelationships);
                if (incompleteRelationships)
                {
                    return [Unavailable("Owned structure", "Unavailable (incomplete model)")];
                }

                var operand = 0;
                foreach (var relationship in relationships ?? [])
                {
                    var role = relationship switch
                    {
                        IFeatureValue => "Value",
                        IResultExpressionMembership => "Result expression",
                        IReturnParameterMembership when !valuesOnly => "Result",
                        IParameterMembership when !valuesOnly => $"Operand {++operand}",
                        IFeatureMembership when !valuesOnly => "Owned element",
                        _ => null
                    };
                    if (role is null)
                    {
                        continue;
                    }

                    if (this.count >= MaximumElements)
                    {
                        result.Add(Unavailable("Remaining structure",
                            "Display limit reached; inspect the owning element."));
                        break;
                    }

                    var members = Read(() => relationship.OwnedRelatedElement, out var incompleteOwned);
                    if (members is null || members.Count == 0)
                    {
                        result.Add(new ExpressionPresentation
                        {
                            Role = role,
                            Label = "Unresolved owned element",
                            Membership = relationship as IMembership,
                            CanInspectMembership = relationship is IMembership,
                            Notice = incompleteOwned
                                ? "Unavailable (incomplete model)"
                                : "The membership has no resolved owned element."
                        });
                        this.count++;
                        continue;
                    }

                    AddOwnedMembers(depth, members, result, relationship, role);
                }

                return result.ToArray();
            }

            /// <summary>Projects resolved membership contents within the shared selection display budget.</summary>
            private void AddOwnedMembers(int depth, IReadOnlyList<IElement> members,
                List<ExpressionPresentation> result, IRelationship relationship, string role)
            {
                foreach (var member in members)
                {
                    if (this.count >= MaximumElements)
                    {
                        result.Add(Unavailable("Remaining structure",
                            "Display limit reached; inspect the owning element."));
                        break;
                    }

                    result.Add(this.Build(member, relationship as IMembership, role, depth));
                }
            }

            /// <summary>Represents unavailable structure without inventing a source element.</summary>
            private static ExpressionPresentation Unavailable(string role, string notice)
            {
                return new ExpressionPresentation { Role = role, Label = "Unavailable", Notice = notice };
            }
        }
    }
}
