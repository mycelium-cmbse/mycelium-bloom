// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramBoundaryPresentation.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    using Mycelium.Bloom.Model.Enum;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>Places a canonical port or parameter on one occurrence boundary.</summary>
    /// <param name="Element">The borrowed canonical SDK object.</param>
    /// <param name="Label">The already-formatted visible notation.</param>
    /// <param name="Side">The occurrence boundary that carries this feature.</param>
    /// <param name="Offset">The normalized position along that boundary.</param>
    /// <param name="Direction">The optional canonical direction name.</param>
    public sealed record DiagramBoundaryPresentation(IElement Element, string Label, BoundarySide Side, double Offset, string Direction = null);
}
