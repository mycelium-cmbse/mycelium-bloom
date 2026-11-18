// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramNodePresentation.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    /// <summary>Identifies the common name and geometry of a typed graphical node.</summary>
    /// <param name="Occurrence">The independent graphical identity and geometry.</param>
    /// <param name="Name">The SDK-formatted name compartment.</param>
    public abstract record DiagramNodePresentation(DiagramOccurrence Occurrence, DiagramNamePresentation Name);
}
