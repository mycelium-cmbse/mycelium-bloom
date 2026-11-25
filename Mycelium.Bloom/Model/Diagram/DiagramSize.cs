// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramSize.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    /// <summary>Retains occurrence-local dimensions independently of canonical model data.</summary>
    /// <param name="Width">The positive model-space width.</param>
    /// <param name="Height">The positive model-space height.</param>
    public sealed record DiagramSize(double Width, double Height);
}
