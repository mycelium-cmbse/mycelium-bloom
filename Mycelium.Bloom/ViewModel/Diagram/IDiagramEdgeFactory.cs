// ------------------------------------------------------------------------------------------------
// <copyright file="IDiagramEdgeFactory.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram
{
    using Mycelium.Bloom.Model.Diagram;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>Supplies extensible graphical relationship notation for a registered SDK type.</summary>
    public interface IDiagramEdgeFactory
    {
        /// <summary>Gets the canonical SDK type accepted by this factory.</summary>
        Type ElementType { get; }

        /// <summary>Projects supported notation without changing canonical semantics.</summary>
        /// <param name="element">The canonical SDK relationship, connector or annotation.</param>
        /// <returns>The descriptor, or null when this specific type requires unsupported notation.</returns>
        DiagramEdgeNotation Create(IElement element);
    }
}
