// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserElementProjection.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.ProjectBrowser
{
    using SysML2.NET.Core.POCO.Core.Classifiers;
    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Core.Types;
    using SysML2.NET.Core.POCO.Kernel.Behaviors;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.FeatureValues;
    using SysML2.NET.Core.POCO.Kernel.Functions;
    using SysML2.NET.Core.POCO.Kernel.Connectors;
    using SysML2.NET.Core.POCO.Kernel.Packages;
    using SysML2.NET.Core.POCO.Root.Annotations;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Systems.DefinitionAndUsage;
    using Mycelium.Bloom.ViewModel.DetailsPanel;
    using SysML2.NET.Core.Core.Types;
    using SysML2.NET.Core.Root.Namespaces;

    /// <summary>
    /// Separates navigable model elements from relationship and property representations.
    /// </summary>
    public static class ProjectBrowserElementProjection
    {
        /// <summary>
        /// Determines whether an element retains its own browser row.
        /// </summary>
        /// <param name="element">The canonical SDK element.</param>
        /// <returns>Whether the element is independently navigable.</returns>
        public static bool IsNavigable(IElement element)
        {
            return element switch
            {
                null => false,
                IPackage or IClassifier or IUsage or IConnector => true,
                IRelationship or IDocumentation or IMultiplicity => false,
                IFeature feature when IsExpressionSupportFeature(feature) => false,
                _ => true
            };
        }

        /// <summary>
        /// Determines whether a feature owns only a direct reference value that belongs in Properties.
        /// </summary>
        /// <param name="element">The canonical SDK element.</param>
        /// <returns>Whether the element is a reference-valued feature without expression-support ownership.</returns>
        public static bool IsReferenceValuedFeature(IElement element)
        {
            if (element is not IFeature feature
                || feature.owningMembership is IParameterMembership
                || IsExpressionSupportFeature(feature))
            {
                return false;
            }

            var relationships = feature.OwnedRelationship;
            if (relationships is null)
            {
                return false;
            }

            var values = relationships.OfType<IFeatureValue>().ToArray();
            var hasRedefinition = relationships.Any(relationship => relationship is IRedefinition);
            return values.Length > 0
                   && relationships.All(relationship => relationship is IFeatureValue or IRedefinition)
                   && values.All(value =>
                       (value.value is IFeatureReferenceExpression or IFeatureChainExpression)
                       && (hasRedefinition
                           ? !HasOwnedCompoundExpression(value.value)
                           : IsReferenceExpressionStructure(value.value)));
        }

        /// <summary>
        /// Gets an operator or reference label from the SDK without deriving model names locally.
        /// </summary>
        /// <param name="element">The canonical SDK element.</param>
        /// <returns>A semantic operator, serialized reference, or ordinary element label.</returns>
        public static string GetDisplayName(IElement element)
        {
            return GetDisplayName(element, false, out _);
        }

        /// <summary>
        /// Gets the display label using the element's known position in the loaded model.
        /// </summary>
        /// <param name="element">The canonical SDK element.</param>
        /// <param name="isModelRoot">Whether the element is the loaded model root.</param>
        /// <param name="uiDisplayLabel">The UI fallback label, when one was required for the model root.</param>
        /// <returns>A semantic operator, serialized reference, SDK name, or contextual fallback label.</returns>
        public static string GetDisplayName(IElement element, bool isModelRoot, out string uiDisplayLabel)
        {
            ArgumentNullException.ThrowIfNull(element);

            uiDisplayLabel = null;
            if (element is IOperatorExpression { Operator: { Length: > 0 } operatorText })
            {
                return $"Operator {operatorText}";
            }

            if (element is IExpression expression
                && expression is (IFeatureReferenceExpression or IFeatureChainExpression)
                && SdkExpressionText.Create(expression).Text is { Length: > 0 } notation)
            {
                return notation;
            }

            var presentation = ProjectBrowserElementPresentation.For(element.GetType(), isModelRoot);
            var displayName = ElementPresentation.GetDisplayName(element, presentation, out var isFallbackLabel);
            if (isModelRoot && isFallbackLabel)
            {
                uiDisplayLabel = displayName;
            }

            return displayName;
        }

        /// <summary>
        /// Determines whether an element is a direct feature-reference expression.
        /// </summary>
        /// <param name="element">The canonical SDK element.</param>
        /// <returns>Whether the element is a feature-reference expression.</returns>
        public static bool IsLeafReferenceExpression(IElement element)
        {
            return element is IFeatureReferenceExpression && !HasOwnedCompoundExpression(element);
        }

        /// <summary>
        /// Identifies unnamed expression parameter wrappers whose expressions survive descendant traversal.
        /// </summary>
        private static bool IsExpressionSupportFeature(IFeature feature)
        {
            if (feature is IExpression || !IsDeclaredAnonymous(feature) || !HasExpressionSupportRole(feature)
                || feature.IsAbstract || feature.IsComposite || feature.IsDerived || feature.IsEnd
                || feature.IsOrdered || !feature.IsUnique || feature.IsConstant || feature.IsVariable
                || feature.IsPortion || feature.IsSufficient || feature.IsImpliedIncluded
                || feature.multiplicity != null || feature.type?.Count > 0
                || feature.documentation?.Count > 0 || feature.textualRepresentation?.Count > 0
                || feature.ownedAnnotation?.Count > 0 || feature.AliasIds?.Count > 0
                || (feature.owningMembership?.Visibility != VisibilityKind.Public
                    && feature.owningMembership is not IParameterMembership
                    && feature.owningMembership is not IReturnParameterMembership)
                || feature.OwnedRelationship?.Any(relationship => relationship switch
                {
                    IFeatureValue value => !IsExpressionSupportValue(value),
                    IFeatureMembership membership => membership.Visibility != VisibilityKind.Public
                                                     || !IsDeclaredAnonymous(membership),
                    _ => true
                }) == true)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Retains only ordinary, unnamed expression values on structural parameter features.
        /// </summary>
        private static bool IsExpressionSupportValue(IFeatureValue value)
        {
            return !value.IsDefault
                   && !value.IsInitial
                   && value.OwnedRelatedElement is { Count: > 0 } members
                   && members.All(member => member switch
                   {
                       IExpression expression => IsDeclaredAnonymous(expression),
                       IFeature feature => IsExpressionSupportFeature(feature),
                       _ => false
                   });
        }

        /// <summary>
        /// Ensures a reference-valued feature does not own additional inspectable model elements.
        /// </summary>
        private static bool IsReferenceExpressionStructure(IElement element)
        {
            if (element is IFeatureReferenceExpression)
            {
                return !HasOwnedCompoundExpression(element);
            }

            if (element is IExpression and not IFeatureChainExpression)
            {
                return false;
            }

            if (element is IFeature feature
                && element is not IFeatureChainExpression
                && !IsExpressionSupportFeature(feature))
            {
                return false;
            }

            if (element is not IRelationship and not IFeature and not IFeatureChainExpression)
            {
                return false;
            }

            return (element.ownedElement ?? []).All(IsReferenceExpressionStructure);
        }

        /// <summary>Finds meaningful nested expressions without interpreting SDK ownership as syntax.</summary>
        private static bool HasOwnedCompoundExpression(IElement element)
        {
            return (element.ownedElement ?? []).Any(child =>
                child is IExpression and not IFeatureReferenceExpression
                || HasOwnedCompoundExpression(child));
        }

        /// <summary>
        /// Requires SDK ownership and membership roles specific to an embedded expression's parameters or chain target.
        /// </summary>
        private static bool HasExpressionSupportRole(IFeature feature)
        {
            return feature.owningMembership switch
            {
                IReturnParameterMembership when feature.Direction == FeatureDirectionKind.Out
                                                && feature.owner is IExpression expression => IsEmbeddedExpression(
                    expression),
                IParameterMembership and not IReturnParameterMembership when feature.Direction ==
                                                                             FeatureDirectionKind.In
                                                                             && feature.owner is IExpression expression
                    => IsEmbeddedExpression(expression),
                IFeatureMembership when feature.Direction == null && feature.owner is IFeature
                {
                    owningMembership: IParameterMembership,
                    owner: IFeatureChainExpression chain
                } => IsEmbeddedExpression(chain),
                IFeatureValue when feature.Direction == null && feature.owner is IFeature ownerFeature
                    => HasExpressionSupportRole(ownerFeature),
                IFeatureMembership when feature.Direction == null && feature.owner is IFeature ownerFeature
                    => HasExpressionSupportRole(ownerFeature),
                _ => false
            };
        }

        /// <summary>
        /// Distinguishes anonymous value and result expressions from independently modeled expressions and usages.
        /// </summary>
        private static bool IsEmbeddedExpression(IExpression expression)
        {
            if (expression is IUsage || !IsDeclaredAnonymous(expression))
            {
                return false;
            }

            return expression.owningMembership is IFeatureValue or IResultExpressionMembership
                   || (expression.owningMembership is IFeatureMembership
                       && expression.owner is IExpression owner && IsEmbeddedExpression(owner));
        }

        /// <summary>
        /// Checks declared names without treating the SDK's generated metaclass label as a model name.
        /// </summary>
        private static bool IsDeclaredAnonymous(IElement element)
        {
            return string.IsNullOrWhiteSpace(element.DeclaredName)
                   && string.IsNullOrWhiteSpace(element.DeclaredShortName);
        }
    }
}