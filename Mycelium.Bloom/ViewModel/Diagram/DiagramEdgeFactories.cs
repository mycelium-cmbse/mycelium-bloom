// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramEdgeFactories.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram
{
    using Mycelium.Bloom.Model.Diagram;

    using SysML2.NET.Core.POCO.Core.Classifiers;
    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Core.Types;
    using SysML2.NET.Core.POCO.Kernel.Connectors;
    using SysML2.NET.Core.POCO.Root.Annotations;
    using SysML2.NET.Core.POCO.Root.Dependencies;
    using SysML2.NET.Core.POCO.Systems.DefinitionAndUsage;
    using SysML2.NET.Core.POCO.Systems.Flows;
    using SysML2.NET.Core.POCO.Systems.Interfaces;

    /// <summary>Composes the existing supported relationship policies for DI registration.</summary>
    public static class DiagramEdgeFactories
    {
        /// <summary>Creates the generic notation registrations without closing the registry to further SDK types.</summary>
        /// <returns>The supported policies and explicit specialized-notation omissions.</returns>
        public static IDiagramEdgeFactory[] CreateDefault() =>
        [
            new DiagramEdgeFactory<IFeatureTyping>(_ => DiagramEdgeNotations.Definition),
            new DiagramEdgeFactory<IRedefinition>(_ => DiagramEdgeNotations.Redefinition),
            new DiagramEdgeFactory<IReferenceSubsetting>(_ => null),
            new DiagramEdgeFactory<ISubsetting>(_ => DiagramEdgeNotations.Subsetting),
            new DiagramEdgeFactory<ISubclassification>(_ => DiagramEdgeNotations.Subclassification),
            new DiagramEdgeFactory<IFeatureMembership>(member => member.MemberElement is IFeature { IsComposite: true }
                ? DiagramEdgeNotations.CompositeMembership : DiagramEdgeNotations.NoncompositeMembership),
            new DiagramEdgeFactory<IDependency>(element => DiagramEdgeNotations.Dependency with
            {
                Label = string.IsNullOrWhiteSpace(element.DeclaredName) ? null : ElementPresentation.GetDisplayName(element)
            }),
            new DiagramEdgeFactory<IFlowUsage>(_ => null),
            new DiagramEdgeFactory<IBindingConnector>(_ => DiagramEdgeNotations.Binding),
            new DiagramEdgeFactory<IInterfaceUsage>(element => DiagramEdgeNotations.Connection with { Label = "«interface» " + DiagramNotation.Declaration(element) }),
            new DiagramEdgeFactory<IConnector>(element => DiagramEdgeNotations.Connection with { Label = DiagramNotation.Declaration(element) }),
            new DiagramEdgeFactory<IComment>(_ => DiagramEdgeNotations.Annotation)
        ];
    }
}
