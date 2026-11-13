// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramNotation.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram
{
    using System.Collections.Immutable;

    using Mycelium.Bloom.Extensions;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Core.Types;
    using SysML2.NET.Core.POCO.Kernel.FeatureValues;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Actions;
    using SysML2.NET.Core.POCO.Systems.Allocations;
    using SysML2.NET.Core.POCO.Systems.AnalysisCases;
    using SysML2.NET.Core.POCO.Systems.Attributes;
    using SysML2.NET.Core.POCO.Systems.Calculations;
    using SysML2.NET.Core.POCO.Systems.Cases;
    using SysML2.NET.Core.POCO.Systems.Connections;
    using SysML2.NET.Core.POCO.Systems.Constraints;
    using SysML2.NET.Core.POCO.Systems.DefinitionAndUsage;
    using SysML2.NET.Core.POCO.Systems.Enumerations;
    using SysML2.NET.Core.POCO.Systems.Flows;
    using SysML2.NET.Core.POCO.Systems.Interfaces;
    using SysML2.NET.Core.POCO.Systems.Items;
    using SysML2.NET.Core.POCO.Systems.Metadata;
    using SysML2.NET.Core.POCO.Systems.Occurrences;
    using SysML2.NET.Core.POCO.Systems.Parts;
    using SysML2.NET.Core.POCO.Systems.Ports;
    using SysML2.NET.Core.POCO.Systems.Requirements;
    using SysML2.NET.Core.POCO.Systems.States;
    using SysML2.NET.Core.POCO.Systems.UseCases;
    using SysML2.NET.Core.POCO.Systems.VerificationCases;
    using SysML2.NET.Core.POCO.Systems.Views;
    using SysML2.NET.Exceptions;
    using SysML2.NET.Serializer.TextualNotation.Writers;

    /// <summary>Uses SDK declaration and value writers; graphical keywords come from §8.2.3.</summary>
    public static class DiagramNotation
    {
        /// <summary>Maps canonical SDK declaration kinds to graphical BNF keywords.</summary>
        private static readonly (System.Type Type, string Keyword)[] Keywords =
        [
            (typeof(ISatisfyRequirementUsage), "satisfy requirement"),
            (typeof(IAssertConstraintUsage), "assert constraint"),
            (typeof(IExhibitStateUsage), "exhibit-state"),
            (typeof(IPerformActionUsage), "perform action"),
            (typeof(ISuccessionFlowUsage), "succession flow"),
            (typeof(IIncludeUseCaseUsage), "include use case"),
            (typeof(IEventOccurrenceUsage), "event occurrence"),
            (typeof(IInterfaceDefinition), "interface def"),
            (typeof(IInterfaceUsage), "interface"),
            (typeof(IFlowDefinition), "flow def"),
            (typeof(IFlowUsage), "flow"),
            (typeof(IAllocationDefinition), "allocation def"),
            (typeof(IAllocationUsage), "allocation"),
            (typeof(IConnectionDefinition), "connection def"),
            (typeof(IConnectionUsage), "connection"),
            (typeof(IVerificationCaseDefinition), "verification def"),
            (typeof(IVerificationCaseUsage), "verification"),
            (typeof(IAnalysisCaseDefinition), "analysis def"),
            (typeof(IAnalysisCaseUsage), "analysis"),
            (typeof(IUseCaseDefinition), "use case def"),
            (typeof(IUseCaseUsage), "use case"),
            (typeof(ICaseDefinition), "case def"),
            (typeof(ICaseUsage), "case"),
            (typeof(IConcernDefinition), "concern def"),
            (typeof(IConcernUsage), "concern"),
            (typeof(IRequirementDefinition), "requirement def"),
            (typeof(IRequirementUsage), "requirement"),
            (typeof(IStateDefinition), "state def"),
            (typeof(IStateUsage), "state"),
            (typeof(ICalculationDefinition), "calc def"),
            (typeof(ICalculationUsage), "calc"),
            (typeof(IActionDefinition), "action def"),
            (typeof(IActionUsage), "action"),
            (typeof(IConstraintDefinition), "constraint def"),
            (typeof(IConstraintUsage), "constraint"),
            (typeof(IRenderingDefinition), "rendering def"),
            (typeof(IRenderingUsage), "rendering"),
            (typeof(IViewDefinition), "view def"),
            (typeof(IViewUsage), "view"),
            (typeof(IViewpointDefinition), "viewpoint def"),
            (typeof(IViewpointUsage), "viewpoint"),
            (typeof(IMetadataDefinition), "metadata def"),
            (typeof(IMetadataUsage), "metadata"),
            (typeof(IEnumerationDefinition), "enum def"),
            (typeof(IEnumerationUsage), "enum"),
            (typeof(IAttributeDefinition), "attribute def"),
            (typeof(IAttributeUsage), "attribute"),
            (typeof(IPortDefinition), "port def"),
            (typeof(IPortUsage), "port"),
            (typeof(IPartDefinition), "part def"),
            (typeof(IPartUsage), "part"),
            (typeof(IItemDefinition), "item def"),
            (typeof(IItemUsage), "item"),
            (typeof(IOccurrenceDefinition), "occurrence def"),
            (typeof(IOccurrenceUsage), "occurrence")
        ];

        /// <summary>Builds name notation from canonical SDK semantics and local alias memberships.</summary>
        /// <param name="element">The borrowed canonical SDK element.</param>
        /// <returns>The keyword, SDK declaration, aliases and accepted Bloom symbol.</returns>
        public static DiagramNamePresentation Name(IElement element)
        {
            ArgumentNullException.ThrowIfNull(element);
            var keyword = Keyword(element);
            var additionalKeywords = element switch
            {
                ISatisfyRequirementUsage => ImmutableArray.Create("requirement"),
                IAssertConstraintUsage => ImmutableArray.Create("constraint"),
                IExhibitStateUsage => ImmutableArray.Create("state"),
                _ => ImmutableArray<string>.Empty
            };

            var aliases = element.owningNamespace?.OwnedRelationship
                .OfType<IMembership>()
                .Where(member => ReferenceEquals(member.MemberElement, element) && member is not IOwningMembership)
                .Select(member => member.MemberName).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().ToImmutableArray()
                ?? ImmutableArray<string>.Empty;

            // Reuse the accepted visual symbol only; SDK interfaces above determine graphical semantics.
            return new(keyword, Declaration(element), aliases, ProjectBrowserElementPresentation.For(element.GetType()).SymbolName, additionalKeywords);
        }

        /// <summary>Classifies graphical keywords by canonical SDK interfaces in specificity order.</summary>
        /// <param name="element">The borrowed canonical SDK element.</param>
        /// <returns>The graphical BNF keyword, or null when no supported generic keyword exists.</returns>
        public static string Keyword(IElement element)
        {
            ArgumentNullException.ThrowIfNull(element);
            var matches = Keywords.Where(entry => entry.Type.IsInstanceOfType(element)).ToArray();

            return matches.FirstOrDefault(candidate => !matches.Any(other => other.Type != candidate.Type
                && candidate.Type.IsAssignableFrom(other.Type))).Keyword;
        }

        /// <summary>Formats an SDK declaration using a private cursor without changing canonical relationships.</summary>
        /// <param name="element">The borrowed canonical SDK element.</param>
        /// <param name="includeValue">Whether to append the supported SDK feature value notation.</param>
        /// <returns>The SDK declaration, or explicit unavailable notation for unsupported or incomplete data.</returns>
        public static string Declaration(IElement element, bool includeValue = false)
        {
            ArgumentNullException.ThrowIfNull(element);

            if (element is not INamespace scope)
            {
                return ElementPresentation.GetDisplayName(element);
            }

            try
            {
                using var context = new TextualNotationWriterContext(element.owningNamespace ?? scope);
                // The SDK writer cursor expects declaration relationships before body memberships.
                // Supply a read-only declaration slice rather than reorder the canonical model.
                var declarationRelationships = element.OwnedRelationship.Where(relationship => relationship is ISpecialization
                    || relationship is IOwningMembership membership && membership.MemberElement is IMultiplicity)
                    .Concat(element.OwnedRelationship.OfType<IFeatureValue>()).ToList();
                context.CursorCache.GetOrCreateCursor(element.Id, "ownedRelationship", declarationRelationships);
                var builder = new IndentedStringBuilder();
                switch (element)
                {
                    case IDefinition definition:
                        DefinitionTextualNotationBuilder.BuildDefinitionDeclaration(definition, context, builder);
                        break;
                    case IUsage usage:
                        UsageTextualNotationBuilder.BuildUsageDeclaration(usage, context, builder);
                        break;
                    default:

                        return ElementPresentation.GetDisplayName(element);
                }

                if (includeValue && element is IFeature feature)
                {
                    var expression = feature.OwnedRelationship.OfType<IFeatureValue>().FirstOrDefault()?.value;

                    if (expression is not null && expression.GetExpressionText() is null)
                    {
                        return builder.ToString().Trim() + " — value notation unavailable";
                    }
                    FeatureTextualNotationBuilder.BuildValuePart(feature, context, builder);
                }

                return builder.ToString().Trim();
            }
            catch (IncompleteModelException)
            {
                return "Notation unavailable (incomplete model)";
            }
            catch (NotSupportedException)
            {
                return "Notation unavailable (unsupported SDK declaration)";
            }
        }
    }
}
