// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserElementPresentation.cs" company="Starion Group S.A.">
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
    using SysML2.NET.Core.POCO.Kernel.Associations;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.Functions;
    using SysML2.NET.Core.POCO.Kernel.Connectors;
    using SysML2.NET.Core.POCO.Kernel.Packages;
    using SysML2.NET.Core.POCO.Root.Annotations;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Attributes;
    using SysML2.NET.Core.POCO.Systems.Connections;
    using SysML2.NET.Core.POCO.Systems.Constraints;
    using SysML2.NET.Core.POCO.Systems.DefinitionAndUsage;
    using SysML2.NET.Core.POCO.Systems.Flows;
    using SysML2.NET.Core.POCO.Systems.Parts;
    using SysML2.NET.Core.POCO.Systems.Ports;
    using SysML2.NET.Core.POCO.Systems.States;
    using SysML2.NET.Core.POCO.Systems.Views;

    /// <summary>
    /// Describes a browser row without exposing implementation type names or changing navigation policy.
    /// </summary>
    /// <param name="Kind">The human-readable model category.</param>
    /// <param name="Stereotype">The supported SysML notation, when established.</param>
    /// <param name="IconName">The exported tree icon, or null for the neutral element fallback.</param>
    /// <param name="HasLeadingStereotype">Whether the stereotype precedes the disclosure and icon.</param>
    /// <param name="IsModelRoot">Whether this element is the loaded model root.</param>
    public sealed record ProjectBrowserElementPresentation(
        string Kind,
        string Stereotype = null,
        string IconName = null,
        bool HasLeadingStereotype = false,
        bool IsModelRoot = false)
    {
        /// <summary>
        /// Gets the label for an element without an available model name.
        /// </summary>
        public string UnnamedLabel => this.IsModelRoot && this.Kind == "Namespace" ? "Model" : this.Kind;

        /// <summary>
        /// Maps SDK interfaces in specificity order to read-only row presentation.
        /// </summary>
        /// <param name="elementType">The concrete SDK type or an SDK interface.</param>
        /// <param name="isModelRoot">Whether this element is the loaded model root.</param>
        /// <returns>The semantic presentation, including a neutral fallback for unrecognized types.</returns>
        public static ProjectBrowserElementPresentation For(Type elementType, bool isModelRoot = false)
        {
            ArgumentNullException.ThrowIfNull(elementType);

            ProjectBrowserElementPresentation presentation = elementType switch
            {
                _ when typeof(IPackage).IsAssignableFrom(elementType) => new("Package", "«package»", "folder", true),
                _ when typeof(IFlowDefinition).IsAssignableFrom(elementType) => new("Flow definition"),
                _ when typeof(IConnectionDefinition).IsAssignableFrom(elementType) => new("Connection definition"),
                _ when typeof(ISuccessionFlowUsage).IsAssignableFrom(elementType) => new("Succession flow"),
                _ when typeof(IFlowUsage).IsAssignableFrom(elementType) => new("Flow"),
                _ when typeof(IConnectionUsage).IsAssignableFrom(elementType) => new("Connection"),
                _ when typeof(IConnector).IsAssignableFrom(elementType) => new(
                    ElementPresentation.GetKindName(elementType)),
                _ when typeof(IAssociation).IsAssignableFrom(elementType) => new("Association"),
                _ when typeof(IViewUsage).IsAssignableFrom(elementType) => new("View", IconName: "network"),
                _ when typeof(IViewDefinition).IsAssignableFrom(elementType) => new("View definition"),
                _ when typeof(IPartDefinition).IsAssignableFrom(elementType) => new("Part definition", "«part def»",
                    "box", true),
                _ when typeof(IPartUsage).IsAssignableFrom(elementType) => new("Part", "«part»", "component"),
                _ when typeof(IPortDefinition).IsAssignableFrom(elementType) => new("Port definition"),
                _ when typeof(IPortUsage).IsAssignableFrom(elementType) => new("Port", "«port»", "port"),
                _ when typeof(IAttributeDefinition).IsAssignableFrom(elementType) => new("Attribute definition"),
                _ when typeof(IAttributeUsage).IsAssignableFrom(elementType) => new("Attribute", "«attribute»"),
                _ when typeof(IStateUsage).IsAssignableFrom(elementType) => new("State"),
                _ when typeof(IStateDefinition).IsAssignableFrom(elementType) => new("State definition"),
                _ when typeof(IAssertConstraintUsage).IsAssignableFrom(elementType) => new("Constraint assertion"),
                _ when typeof(IConstraintUsage).IsAssignableFrom(elementType) => new("Constraint"),
                _ when typeof(IComment).IsAssignableFrom(elementType) => new("Comment"),
                _ when typeof(IFeatureChainExpression).IsAssignableFrom(elementType) => new("Feature chain"),
                _ when typeof(IFeatureReferenceExpression).IsAssignableFrom(elementType) => new("Feature reference"),
                _ when typeof(IOperatorExpression).IsAssignableFrom(elementType) => new("Operator expression"),
                _ when typeof(IExpression).IsAssignableFrom(elementType) => new(
                    ElementPresentation.GetKindName(elementType)),
                _ when typeof(IDefinition).IsAssignableFrom(elementType) => new(
                    ElementPresentation.GetKindName(elementType)),
                _ when typeof(IUsage).IsAssignableFrom(elementType) =>
                    new(ElementPresentation.GetKindName(elementType)),
                _ when typeof(IClassifier).IsAssignableFrom(elementType) => new(
                    ElementPresentation.GetKindName(elementType)),
                _ when typeof(IFeature).IsAssignableFrom(elementType) => new("Feature"),
                _ when typeof(INamespace).IsAssignableFrom(elementType) => new(
                    "Namespace",
                    IconName: isModelRoot ? "network" : null),
                _ => new(ElementPresentation.GetKindName(elementType))
            };

            return presentation with { IsModelRoot = isModelRoot };
        }
    }
}