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
    using SysML2.NET.Core.POCO.Kernel.Connectors;
    using SysML2.NET.Core.POCO.Kernel.FeatureValues;
    using SysML2.NET.Core.POCO.Kernel.Functions;
    using SysML2.NET.Core.POCO.Root.Annotations;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Allocations;
    using SysML2.NET.Core.POCO.Systems.Connections;
    using SysML2.NET.Core.POCO.Systems.DefinitionAndUsage;
    using SysML2.NET.Core.POCO.Systems.Enumerations;
    using SysML2.NET.Core.POCO.Systems.Flows;
    using SysML2.NET.Core.POCO.Systems.Interfaces;
    using SysML2.NET.Core.POCO.Systems.Metadata;
    using SysML2.NET.Core.POCO.Systems.Requirements;
    using SysML2.NET.Core.POCO.Systems.States;

    /// <summary>
    /// Applies the reviewed Browser placement policy before broad SDK interface fallbacks.
    /// Excluded subtrees belong to Properties; only ordinary membership wrappers are traversed.
    /// </summary>
    public static class ProjectBrowserElementProjection
    {
        /// <summary>Determines whether an element retains a canonical Browser row.</summary>
        /// <param name="element">The canonical SDK element.</param>
        /// <returns>Whether the element is structural or grouped.</returns>
        public static bool IsNavigable(IElement element)
        {
            return element != null && !IsElidedMembership(element) && !IsExcluded(element);
        }

        /// <summary>Recognizes transparent membership wrappers, including aliases and variants.</summary>
        /// <param name="element">The element to classify.</param>
        /// <returns>Whether traversal should continue through this wrapper.</returns>
        public static bool IsElidedMembership(IElement element)
        {
            return element is IMembership && !IsDetailMembership(element);
        }

        /// <summary>Recognizes roles whose complete contents stay on their visible owner.</summary>
        /// <param name="element">A membership or the element occupying that role.</param>
        /// <returns>Whether this role belongs exclusively to Properties.</returns>
        private static bool IsDetailMembership(IElement element)
        {
            return element is IEndFeatureMembership or IStateSubactionMembership or ITransitionFeatureMembership
                or IRequirementConstraintMembership or IFeatureValue or IResultExpressionMembership;
        }

        /// <summary>Checks role-specific exclusions before usage, classifier and expression interfaces.</summary>
        /// <param name="element">The canonical model element.</param>
        /// <param name="membership">The membership for this occurrence, when traversing an alias or variant.</param>
        /// <returns>Whether the complete subtree is presented on its owner.</returns>
        public static bool IsExcluded(IElement element, IMembership membership = null)
        {
            if (element == null || IsDetailMembership(element)
                || IsDetailMembership(membership ?? element.owningMembership)
                || element is IFeature { IsEnd: true }
                || (element is IEnumerationUsage
                    && (element.owner is IEnumerationDefinition
                        || membership?.membershipOwningNamespace is IEnumerationDefinition)))
            {
                return true;
            }

            return element switch
            {
                IComment or ITextualRepresentation or IMetadataUsage or ITransitionUsage
                    or IMultiplicity or IImport => true,
                // The review explicitly groups SuccessionFlowUsage as a flow. Plain successions
                // describe control sequencing on their owner, even when they also implement IUsage/IConnector.
                IFlowUsage => false,
                ISuccession => true,
                // Domain usages (including constraints and calculations) may also be expressions.
                // Their owning role and explicit detail-only kinds have already been checked.
                IClassifier or IUsage or IConnector => false,
                IExpression => true,
                IMembership => false,
                IRelationship => true,
                _ => false
            };
        }

        /// <summary>Gets the reviewed collapsible group for one occurrence.</summary>
        /// <param name="element">The canonical model element.</param>
        /// <param name="membership">The membership for this occurrence.</param>
        /// <returns>The kind-group label, or null for structural elements.</returns>
        public static string GetGroup(IElement element, IMembership membership = null)
        {
            if ((membership ?? element.owningMembership) is IVariantMembership && element is IUsage)
            {
                return "Variants";
            }

            return element switch
            {
                IInterfaceDefinition or IInterfaceUsage => "Interfaces",
                IFlowDefinition or IFlowUsage => "Flows",
                IAllocationDefinition or IAllocationUsage => "Allocations",
                ISatisfyRequirementUsage => "Satisfies",
                IConnectionDefinition or IConnectionUsage or IBindingConnector => "Connections",
                _ => null
            };
        }

        /// <summary>Gets the SDK name or contextual fallback without changing the model.</summary>
        /// <param name="element">The canonical SDK element.</param>
        /// <returns>The display name.</returns>
        public static string GetDisplayName(IElement element)
        {
            return GetDisplayName(element, false, out _);
        }

        /// <summary>Gets a name using the element's known position in the loaded model.</summary>
        /// <param name="element">The canonical SDK element.</param>
        /// <param name="isModelRoot">Whether the element is the loaded root.</param>
        /// <param name="uiDisplayLabel">The UI fallback for an unnamed root.</param>
        /// <returns>The SDK name or contextual fallback.</returns>
        public static string GetDisplayName(IElement element, bool isModelRoot, out string uiDisplayLabel)
        {
            ArgumentNullException.ThrowIfNull(element);
            var presentation = ProjectBrowserElementPresentation.For(element.GetType(), isModelRoot);
            var name = ElementPresentation.GetDisplayName(element, presentation, out var isFallbackLabel);
            uiDisplayLabel = isModelRoot && isFallbackLabel ? name : null;
            return name;
        }
    }
}
