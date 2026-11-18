// ------------------------------------------------------------------------------------------------
// <copyright file="IDiagramLayout.cs" company="Starion Group S.A.">
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

    /// <summary>Provides bounded initial graphical occurrence placement for any diagram projector.</summary>
    public interface IDiagramLayout
    {
        /// <summary>Gets the maximum requested canonical elements retained by the projector.</summary>
        int MaximumElements { get; }

        /// <summary>Creates initial geometry and an independent graphical identity.</summary>
        /// <param name="element">The canonical semantic object.</param>
        /// <param name="index">The nonnegative initial placement index.</param>
        /// <returns>The new graphical occurrence.</returns>
        DiagramOccurrence CreateOccurrence(IElement element, int index);
    }
}
