// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramLayoutOptions.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    /// <summary>Configures bounded initial placement without enabling interactive resizing or routing.</summary>
    public sealed record DiagramLayoutOptions
    {
        /// <summary>Gets the maximum canonical elements considered in one projection.</summary>
        public int MaximumElements { get; init; } = 200;

        /// <summary>Gets the number of initial grid columns.</summary>
        public int Columns { get; init; } = 3;

        /// <summary>Gets the initial model-space grid origin.</summary>
        public DiagramPoint Origin { get; init; } = new(90, 90);

        /// <summary>Gets the horizontal and vertical distance between grid positions.</summary>
        public DiagramPoint Spacing { get; init; } = new(360, 270);
    }
}
