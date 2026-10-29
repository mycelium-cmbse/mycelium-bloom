// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserRowPresentation.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.ProjectBrowser
{
    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Kernel.FeatureValues;
    using SysML2.NET.Core.POCO.Kernel.Connectors;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.Functions;
    using SysML2.NET.Core.POCO.Kernel.Packages;
    using SysML2.NET.Core.POCO.Root.Annotations;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Attributes;
    using SysML2.NET.Core.POCO.Systems.DefinitionAndUsage;
    using SysML2.NET.Core.POCO.Systems.Enumerations;
    using SysML2.NET.Core.POCO.Systems.Metadata;
    using SysML2.NET.Core.POCO.Systems.Ports;
    using SysML2.NET.Core.POCO.Systems.States;
    using SysML2.NET.Core.POCO.Systems.Actions;
    using SysML2.NET.Core.POCO.Systems.VerificationCases;
    using SysML2.NET.Exceptions;

    /// <summary>Contains only available model data for a canonical Browser occurrence.</summary>
    /// <param name="TypeLabel">The resolved feature types.</param>
    /// <param name="ValueLabel">A supported scalar attribute value.</param>
    /// <param name="Visibility">The membership visibility, when a membership exists.</param>
    /// <param name="Indicators">Indicators supported by actual model data.</param>
    public sealed record ProjectBrowserRowPresentation(string TypeLabel, string ValueLabel, string Visibility, IReadOnlyList<string> Indicators)
    {
        /// <summary>Reads supported facets without inventing project ownership or evaluated values.</summary>
        /// <param name="element">The canonical model element.</param>
        /// <param name="membership">The membership for this occurrence.</param>
        /// <param name="isImported">Whether the SDK identifies this occurrence as imported.</param>
        /// <param name="importingNamespace">The namespace defining effective import visibility.</param>
        /// <returns>A read-only row snapshot.</returns>
        public static ProjectBrowserRowPresentation For(IElement element, IMembership membership = null, bool isImported = false, INamespace importingNamespace = null)
        {
            var indicators = new List<string>();
            var typeLabel = Read(() => element is IFeature feature
                ? string.Join(", ", (feature.type ?? []).Select(ElementPresentation.GetDisplayName))
                : null);
            var valueLabel = Read(() => element is IAttributeUsage
                ? ElementPresentation.GetExpressionText(GetSingleValue(element))
                : null);
            var occurrenceMembership = membership ?? element.owningMembership;
            var visibility = Read(() => occurrenceMembership == null ? null
                : (importingNamespace?.VisibilityOf(occurrenceMembership) ?? occurrenceMembership.Visibility).ToString().ToLowerInvariant());
            if (isImported)
            {
                indicators.Add("Imported");
            }

            if (Read(() => element.documentation?.Count > 0
                || element.ownedElement?.Any(child => child is IComment or ITextualRepresentation) == true
                || element.ownedAnnotation?.Any(annotation =>
                    annotation.annotatingElement is IComment or ITextualRepresentation) == true))
            {
                indicators.Add("Documentation");
            }
            if (Read(() => element.ownedElement?.Any(child => child is IMetadataUsage) == true
                || element.ownedAnnotation?.Any(annotation => annotation.annotatingElement is IMetadataUsage) == true))
            {
                indicators.Add("Metadata");
            }
            if (element is ILibraryPackage library)
            {
                indicators.Add("Library");
                if (library.IsStandard)
                {
                    indicators.Add("Standard library");
                }
            }
            if (element is not IEnumerationDefinition
                && (element is IUsage { IsVariation: true } or IDefinition { IsVariation: true }))
            {
                indicators.Add("Variation");
            }
            if (element is IPortUsage port && Read(() => port.isConjugated))
            {
                indicators.Add("Conjugated");
            }
            if (element is IStateUsage { IsParallel: true } or IStateDefinition { IsParallel: true })
            {
                indicators.Add("Parallel");
            }
            if (element is IStateUsage state && Read(() => IsInitialState(state)))
            {
                indicators.Add("Initial");
            }
            if (element is IVerificationCaseUsage or IVerificationCaseDefinition
                && Read(() => GetVerdict(element)) is { } verdict)
            {
                indicators.Add($"Verdict: {verdict}");
            }

            return new(typeLabel, valueLabel, visibility, indicators);
        }

        /// <summary>Reads a single explicit value; multiple or absent values are not guessed.</summary>
        /// <param name="element">The feature owning the value.</param>
        /// <returns>The canonical expression, or null for ambiguous or missing values.</returns>
        private static IExpression GetSingleValue(IElement element)
        {
            var values = element?.OwnedRelationship?.OfType<IFeatureValue>().ToArray() ?? [];
            return values.Length == 1 ? values[0].value : null;
        }

        /// <summary>Recognizes the explicit entry-to-substate succession defined by SysML 7.18.2.</summary>
        /// <param name="state">The nested state being presented.</param>
        /// <returns>Whether the state is the unique unconditional initial target of its parent.</returns>
        private static bool IsInitialState(IStateUsage state)
        {
            var owner = state.owner;
            var entry = owner switch
            {
                IStateDefinition { IsParallel: false } definition => definition.entryAction,
                IStateUsage { IsParallel: false } usage => usage.entryAction,
                _ => null
            };
            if (entry == null)
            {
                return false;
            }

            var transitions = owner.ownedElement?.OfType<ITransitionUsage>()
                .Where(transition => ReferenceEquals(transition.source, entry)).ToArray() ?? [];
            // Guarded/triggered starts need runtime context; an unguarded transition exposes its succession target.
            if (transitions.Any(transition => transition.guardExpression?.Count > 0 || transition.triggerAction?.Count > 0))
            {
                return false;
            }
            var initial = owner.ownedElement?.OfType<ISuccession>()
                .Where(succession => ReferenceEquals(succession.sourceFeature, entry)).ToArray() ?? [];
            if (initial.Length + transitions.Length != 1)
            {
                return false;
            }
            return transitions.Length == 1
                ? ReferenceEquals(transitions[0].target, state)
                : initial[0].targetFeature is { Count: 1 } targets && ReferenceEquals(targets[0], state);
        }

        /// <summary>Reads an explicit standard verdict reference on the verification result, without execution.</summary>
        /// <param name="verification">The verification definition or usage whose result is presented.</param>
        /// <returns>The referenced VerdictKind value, or null when the result is not explicitly recorded.</returns>
        private static string GetVerdict(IElement verification)
        {
            var result = verification switch
            {
                IVerificationCaseDefinition definition => definition.result,
                IVerificationCaseUsage usage => usage.result,
                _ => null
            };
            if (GetSingleValue(result) is not IFeatureReferenceExpression { referent: IEnumerationUsage literal }
                || literal.owner is not IEnumerationDefinition enumeration
                || enumeration.owner is not ILibraryPackage { IsStandard: true }
                || enumeration.qualifiedName != "VerificationCases::VerdictKind")
            {
                return null;
            }
            return literal.name is "pass" or "fail" or "inconclusive" or "error" ? literal.name : null;
        }

        /// <summary>Reads a facet whose derived SDK data may be incomplete.</summary>
        /// <typeparam name="T">The facet value type.</typeparam>
        /// <param name="read">The SDK read operation.</param>
        /// <returns>The facet value, or its default when the model is incomplete.</returns>
        private static T Read<T>(Func<T> read)
        {
            try
            {
                return read();
            }
            catch (IncompleteModelException)
            {
                return default;
            }
        }
    }
}
