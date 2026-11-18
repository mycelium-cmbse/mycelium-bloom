// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramCommentPresentation.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    /// <summary>Describes a folded-corner comment and its canonical body.</summary>
    /// <param name="Occurrence">The independent graphical identity and geometry.</param>
    /// <param name="Name">The comment's accessible name.</param>
    /// <param name="Body">The canonical comment text.</param>
    public record DiagramCommentPresentation(DiagramOccurrence Occurrence, DiagramNamePresentation Name, string Body) : DiagramNodePresentation(Occurrence, Name);
}
