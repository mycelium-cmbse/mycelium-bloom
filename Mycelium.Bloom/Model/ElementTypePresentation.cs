// ------------------------------------------------------------------------------------------------
// <copyright file="ElementTypePresentation.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    using System.Text.RegularExpressions;

    using Mycelium.Bloom.Core.Presentation;

    using SysML2.NET.Core.POCO.Core.Classifiers;
    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Kernel.Associations;
    using SysML2.NET.Core.POCO.Kernel.Connectors;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.Functions;
    using SysML2.NET.Core.POCO.Kernel.Packages;
    using SysML2.NET.Core.POCO.Root.Annotations;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Actions;
    using SysML2.NET.Core.POCO.Systems.Allocations;
    using SysML2.NET.Core.POCO.Systems.AnalysisCases;
    using SysML2.NET.Core.POCO.Systems.Attributes;
    using SysML2.NET.Core.POCO.Systems.Calculations;
    using SysML2.NET.Core.POCO.Systems.Connections;
    using SysML2.NET.Core.POCO.Systems.Constraints;
    using SysML2.NET.Core.POCO.Systems.DefinitionAndUsage;
    using SysML2.NET.Core.POCO.Systems.Enumerations;
    using SysML2.NET.Core.POCO.Systems.Flows;
    using SysML2.NET.Core.POCO.Systems.Interfaces;
    using SysML2.NET.Core.POCO.Systems.Items;
    using SysML2.NET.Core.POCO.Systems.Metadata;
    using SysML2.NET.Core.POCO.Systems.Parts;
    using SysML2.NET.Core.POCO.Systems.Ports;
    using SysML2.NET.Core.POCO.Systems.Requirements;
    using SysML2.NET.Core.POCO.Systems.States;
    using SysML2.NET.Core.POCO.Systems.VerificationCases;
    using SysML2.NET.Core.POCO.Systems.Views;

    /// <summary>
    /// Describes shared semantic labels without changing canonical model identity.
    /// </summary>
    /// <param name="Kind">The human-readable model category.</param>
    /// <param name="Stereotype">The guillemet kind designator.</param>
    /// <param name="IconName">The existing exported tree icon, when available.</param>
    /// <param name="HasLeadingStereotype">Retained for existing consumers; Browser rows place the kind after the name and feature notation.</param>
    /// <param name="IsModelRoot">Whether this is the loaded model root.</param>
    public sealed record ElementTypePresentation(
        string Kind,
        string Stereotype = null,
        string IconName = null,
        bool HasLeadingStereotype = false,
        bool IsModelRoot = false)
    {
        /// <summary>
        /// The folder icon shared by namespace and package rows.
        /// </summary>
        private const string FolderIconName = "folder";

        /// <summary>
        /// Gets the contextual label for an unnamed element.
        /// </summary>
        public string UnnamedLabel => this.IsModelRoot && this.Kind == "Namespace" ? "Model" : this.Kind;

        /// <summary>
        /// Gets the established Lucide symbol when no exported Browser icon is available.
        /// </summary>
        /// <remarks>The inline kind label distinguishes definitions and usages sharing a symbol.</remarks>
        public string SymbolName => SemanticIconResolver.GetElementSymbol(this.Kind);

        /// <summary>
        /// Maps SDK interfaces in specificity order, before broad feature fallbacks.
        /// </summary>
        /// <param name="elementType">The SDK type or interface.</param>
        /// <param name="isModelRoot">Whether this is the loaded model root.</param>
        /// <returns>The semantic label and available icon.</returns>
        public static ElementTypePresentation For(Type elementType, bool isModelRoot = false)
        {
            ArgumentNullException.ThrowIfNull(elementType);

            ElementTypePresentation presentation = elementType switch
            {
                _ when typeof(ILibraryPackage).IsAssignableFrom(elementType) => new("Library package", "«library package»", FolderIconName),
                _ when typeof(IPackage).IsAssignableFrom(elementType) => new("Package", "«package»", FolderIconName),
                _ when typeof(IInterfaceDefinition).IsAssignableFrom(elementType) => new("Interface definition", "«interface def»"),
                _ when typeof(IInterfaceUsage).IsAssignableFrom(elementType) => new("Interface", "«interface»"),
                _ when typeof(IFlowDefinition).IsAssignableFrom(elementType) => new("Flow definition", "«flow def»"),
                _ when typeof(ISuccessionFlowUsage).IsAssignableFrom(elementType) => new("Succession flow", "«succession flow»"),
                _ when typeof(IFlowUsage).IsAssignableFrom(elementType) => new("Flow", "«flow»"),
                _ when typeof(IAllocationDefinition).IsAssignableFrom(elementType) => new("Allocation definition", "«allocation def»"),
                _ when typeof(IAllocationUsage).IsAssignableFrom(elementType) => new("Allocation", "«allocation»"),
                _ when typeof(IConnectionDefinition).IsAssignableFrom(elementType) => new("Connection definition", "«connection def»"),
                _ when typeof(IConnectionUsage).IsAssignableFrom(elementType) => new("Connection", "«connection»"),
                _ when typeof(IBindingConnector).IsAssignableFrom(elementType) => new("Binding connector", "«binding»"),
                _ when typeof(IConnector).IsAssignableFrom(elementType) => new(GetKindName(elementType)),
                _ when typeof(IAssociation).IsAssignableFrom(elementType) => new("Association", "«association»"),
                _ when typeof(IViewUsage).IsAssignableFrom(elementType) => new("View", "«view»", "network"),
                _ when typeof(IViewDefinition).IsAssignableFrom(elementType) => new("View definition", "«view def»"),
                _ when typeof(IMetadataDefinition).IsAssignableFrom(elementType) => new("Metadata definition", "«metadata def»"),
                _ when typeof(IEnumerationDefinition).IsAssignableFrom(elementType) => new("Enumeration definition", "«enum def»"),
                _ when typeof(IAttributeDefinition).IsAssignableFrom(elementType) => new("Attribute definition", "«attribute def»"),
                _ when typeof(IAttributeUsage).IsAssignableFrom(elementType) => new("Attribute", "«attribute»"),
                _ when typeof(IPortDefinition).IsAssignableFrom(elementType) => new("Port definition", "«port def»"),
                _ when typeof(IPortUsage).IsAssignableFrom(elementType) => new("Port", "«port»", "port"),
                _ when typeof(IPartDefinition).IsAssignableFrom(elementType) => new("Part definition", "«part def»", "box"),
                _ when typeof(IPartUsage).IsAssignableFrom(elementType) => new("Part", "«part»", "component"),
                _ when typeof(IItemDefinition).IsAssignableFrom(elementType) => new("Item definition", "«item def»", "box"),
                _ when typeof(IItemUsage).IsAssignableFrom(elementType) => new("Item", "«item»"),
                _ when typeof(IVerificationCaseDefinition).IsAssignableFrom(elementType) => new("Verification case definition", "«verification def»"),
                _ when typeof(IVerificationCaseUsage).IsAssignableFrom(elementType) => new("Verification case", "«verification»"),
                _ when typeof(IAnalysisCaseDefinition).IsAssignableFrom(elementType) => new("Analysis case definition", "«analysis def»"),
                _ when typeof(IAnalysisCaseUsage).IsAssignableFrom(elementType) => new("Analysis case", "«analysis»"),
                _ when typeof(ISatisfyRequirementUsage).IsAssignableFrom(elementType) => new("Satisfy requirement", "«satisfy»"),
                _ when typeof(IRequirementDefinition).IsAssignableFrom(elementType) => new("Requirement definition", "«requirement def»"),
                _ when typeof(IRequirementUsage).IsAssignableFrom(elementType) => new("Requirement", "«requirement»"),
                _ when typeof(IExhibitStateUsage).IsAssignableFrom(elementType) => new("Exhibit state", "«exhibit»"),
                _ when typeof(IStateDefinition).IsAssignableFrom(elementType) => new("State definition", "«state def»"),
                _ when typeof(IStateUsage).IsAssignableFrom(elementType) => new("State", "«state»"),
                _ when typeof(ICalculationDefinition).IsAssignableFrom(elementType) => new("Calculation definition", "«calc def»"),
                _ when typeof(ICalculationUsage).IsAssignableFrom(elementType) => new("Calculation", "«calc»"),
                _ when typeof(IPerformActionUsage).IsAssignableFrom(elementType) => new("Perform action", "«perform»"),
                _ when typeof(IActionDefinition).IsAssignableFrom(elementType) => new("Action definition", "«action def»"),
                _ when typeof(IActionUsage).IsAssignableFrom(elementType) => new("Action", "«action»"),
                _ when typeof(IConstraintDefinition).IsAssignableFrom(elementType) => new("Constraint definition", "«constraint def»"),
                _ when typeof(IAssertConstraintUsage).IsAssignableFrom(elementType) => new("Constraint assertion", "«assert»"),
                _ when typeof(IConstraintUsage).IsAssignableFrom(elementType) => new("Constraint", "«constraint»"),
                _ when typeof(IComment).IsAssignableFrom(elementType) => new("Comment", "«comment»"),
                _ when typeof(IFeatureChainExpression).IsAssignableFrom(elementType) => new("Feature chain", "«feature chain»"),
                _ when typeof(IFeatureReferenceExpression).IsAssignableFrom(elementType) => new("Feature reference", "«feature reference»"),
                _ when typeof(IOperatorExpression).IsAssignableFrom(elementType) => new("Operator expression", "«operator expression»"),
                _ when typeof(IDefinition).IsAssignableFrom(elementType) => new(GetKindName(elementType)),
                _ when typeof(IUsage).IsAssignableFrom(elementType) => new(GetKindName(elementType)),
                _ when typeof(IClassifier).IsAssignableFrom(elementType) => new(GetKindName(elementType)),
                _ when typeof(IFeature).IsAssignableFrom(elementType) => new("Feature", "«feature»"),
                _ when typeof(INamespace).IsAssignableFrom(elementType) => new("Namespace", "«namespace»", IconName: isModelRoot ? "network" : null),
                _ => new(GetKindName(elementType))
            };

            var stereotype = presentation.Stereotype ?? $"«{presentation.Kind.ToLowerInvariant()}»";
            return presentation with
            {
                Stereotype = stereotype,
                HasLeadingStereotype = presentation.IconName is FolderIconName or "box",
                IsModelRoot = isModelRoot
            };
        }

        /// <summary>
        /// Labels an SDK metaclass without exposing its implementation identifier as prose.
        /// </summary>
        /// <param name="type">The SDK type being presented.</param>
        /// <returns>A readable metaclass label or the neutral element role.</returns>
        public static string GetKindName(System.Type type)
        {
            if (type.Assembly != typeof(IElement).Assembly)
            {
                return "Element";
            }

            var name = type.IsInterface ? type.Name[1..] : type.Name;
            var words = Regex.Replace(name, "(?<=[a-z])(?=[A-Z])", " ", RegexOptions.None, TimeSpan.FromMilliseconds(100));
            return words.Length == 0 ? "Element" : words[..1] + words[1..].ToLowerInvariant();
        }

    }
}
