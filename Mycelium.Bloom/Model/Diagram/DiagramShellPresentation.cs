// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramShellPresentation.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    using System.Collections.Immutable;

    /// <summary>Describes the ordered contents of a definition, usage, reference or generic shell.</summary>
    /// <param name="Occurrence">The independent graphical identity and geometry.</param>
    /// <param name="Name">The SDK-formatted name compartment.</param>
    /// <param name="Kind">The shell's graphical variant.</param>
    /// <param name="Compartments">The ordered textual and graphical compartments.</param>
    /// <param name="Boundary">The occurrence-owned boundary features.</param>
    /// <param name="NestedView">The optional graphical view preceding the compartments.</param>
    public record DiagramShellPresentation(DiagramOccurrence Occurrence, DiagramNamePresentation Name, DiagramNodeKind Kind,
        ImmutableArray<DiagramCompartmentPresentation> Compartments, ImmutableArray<DiagramBoundaryPresentation> Boundary, GeneralViewProjection NestedView = null)
        : DiagramNodePresentation(Occurrence, Name);
}
