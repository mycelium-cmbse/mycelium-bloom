// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramOccurrence.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>Identifies one graphical occurrence independently of its canonical semantic element.</summary>
    /// <param name="Id">The view-local graphical identity.</param>
    /// <param name="Element">The borrowed canonical SDK object.</param>
    /// <param name="X">The horizontal model-space coordinate.</param>
    /// <param name="Y">The vertical model-space coordinate.</param>
    /// <param name="Width">The positive occurrence width.</param>
    /// <param name="Height">The positive occurrence height.</param>
    public sealed record DiagramOccurrence(Guid Id, IElement Element, double X, double Y, double Width = 240, double Height = 160);
}
