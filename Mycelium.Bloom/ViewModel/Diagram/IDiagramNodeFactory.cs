// ------------------------------------------------------------------------------------------------
// <copyright file="IDiagramNodeFactory.cs" company="Starion Group S.A.">
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

    /// <summary>Projects one supported SDK element into typed node notation.</summary>
    public interface IDiagramNodeFactory
    {
        /// <summary>Gets the SDK element type accepted by this factory.</summary>
        Type ElementType { get; }

        /// <summary>Creates notation while preserving the supplied canonical occurrence.</summary>
        /// <param name="occurrence">The borrowed canonical element and graphical geometry.</param>
        /// <param name="source">The optional canonical reference site supplying view-local names.</param>
        /// <returns>The typed node presentation.</returns>
        DiagramNodePresentation Create(DiagramOccurrence occurrence, IElement source = null);
    }
}
