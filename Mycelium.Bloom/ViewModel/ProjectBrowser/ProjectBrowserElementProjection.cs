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
    using SysML2.NET.Core.Core.Types;
    using SysML2.NET.Core.Root.Namespaces;
    using SysML2.NET.Exceptions;

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
                IFeature feature when IsEmptyExpressionSupport(feature) => false,
                _ => true
            };
        }

        /// <summary>
        /// Identifies anonymous expression parameter wrappers whose remaining content survives descendant traversal.
        /// </summary>
        private static bool IsEmptyExpressionSupport(IFeature feature)
        {
            if (feature is IExpression || !IsAnonymous(feature) || !HasExpressionSupportRole(feature)
                || feature.IsAbstract || feature.IsComposite || feature.IsDerived || feature.IsEnd
                || feature.IsOrdered || !feature.IsUnique || feature.IsConstant || feature.IsVariable
                || feature.IsPortion || feature.IsSufficient || feature.IsImpliedIncluded
                || feature.multiplicity != null || feature.type?.Count > 0
                || feature.documentation?.Count > 0 || feature.textualRepresentation?.Count > 0
                || feature.ownedAnnotation?.Count > 0 || feature.AliasIds?.Count > 0
                || feature.owningMembership?.Visibility != VisibilityKind.Public
                || feature.OwnedRelationship?.Any(relationship => relationship switch
                {
                    IFeatureValue value => value.IsDefault || value.IsInitial || !IsAnonymous(value),
                    IFeatureMembership membership => membership.Visibility != VisibilityKind.Public || !IsAnonymous(membership),
                    _ => true
                }) == true)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Requires SDK ownership and membership roles specific to an embedded expression's parameters or chain target.
        /// </summary>
        private static bool HasExpressionSupportRole(IFeature feature)
        {
            return feature.owningMembership switch
            {
                IReturnParameterMembership when feature.Direction == FeatureDirectionKind.Out
                    && feature.owner is IExpression expression => IsEmbeddedExpression(expression),
                IParameterMembership and not IReturnParameterMembership when feature.Direction == FeatureDirectionKind.In
                    && feature.owner is IExpression expression => IsEmbeddedExpression(expression),
                IFeatureMembership when feature.Direction == null && feature.owner is IFeature
                {
                    owningMembership: IParameterMembership,
                    owner: IFeatureChainExpression chain
                } => IsEmbeddedExpression(chain),
                _ => false
            };
        }

        /// <summary>
        /// Distinguishes anonymous value and result expressions from independently modeled expressions and usages.
        /// </summary>
        private static bool IsEmbeddedExpression(IExpression expression)
        {
            if (expression is IUsage or IClassifier || !IsAnonymous(expression))
            {
                return false;
            }

            return expression.owningMembership is IFeatureValue or IResultExpressionMembership
                || (expression.owningMembership is IFeatureMembership
                    && expression.owner is IExpression owner && IsEmbeddedExpression(owner));
        }

        /// <summary>
        /// Requires both declared and SDK-resolved names to be absent before treating an element as structural.
        /// </summary>
        private static bool IsAnonymous(IElement element)
        {
            try
            {
                return string.IsNullOrWhiteSpace(element.DeclaredName)
                    && string.IsNullOrWhiteSpace(element.DeclaredShortName)
                    && string.IsNullOrWhiteSpace(element.name)
                    && string.IsNullOrWhiteSpace(element.shortName);
            }
            catch (IncompleteModelException)
            {
                return false;
            }
        }
    }
}
