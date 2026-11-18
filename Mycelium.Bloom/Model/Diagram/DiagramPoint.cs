// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramPoint.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    /// <summary>Stores a model-space routing point without modifying canonical geometry.</summary>
    /// <param name="X">The horizontal model-space coordinate.</param>
    /// <param name="Y">The vertical model-space coordinate.</param>
    public sealed record DiagramPoint(double X, double Y);
}
