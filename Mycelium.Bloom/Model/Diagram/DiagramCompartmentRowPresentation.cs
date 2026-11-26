// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramCompartmentRowPresentation.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>Associates formatted compartment text with its optional canonical semantic element.</summary>
    /// <param name="Text">The already-formatted declaration or textual content.</param>
    /// <param name="Element">The borrowed canonical element, or null for nonselectable text.</param>
    public sealed record DiagramCompartmentRowPresentation(string Text, IElement Element = null);
}
