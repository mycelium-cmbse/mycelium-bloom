// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramPackagePresentation.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    /// <summary>Describes a package's tabbed namespace notation.</summary>
    /// <param name="Occurrence">The independent graphical identity and geometry.</param>
    /// <param name="Name">The SDK-formatted package name.</param>
    public record DiagramPackagePresentation(DiagramOccurrence Occurrence, DiagramNamePresentation Name) : DiagramNodePresentation(Occurrence, Name);
}
