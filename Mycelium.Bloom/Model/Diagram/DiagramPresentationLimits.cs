// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramPresentationLimits.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    /// <summary>Defines bounded presentation capacity for ordinary and isolated benchmark tabs.</summary>
    public sealed record DiagramPresentationLimits
    {
        private DiagramPresentationLimits(int nodes, int routes, int jsonBytes, double minimumZoom)
        {
            this.Nodes = nodes;
            this.Routes = routes;
            this.JsonBytes = jsonBytes;
            this.MinimumZoom = minimumZoom;
        }

        /// <summary>Gets the unchanged product validation bounds.</summary>
        public static DiagramPresentationLimits Default { get; } = new(500, 1500, 1_048_576, 0.1);

        /// <summary>Gets full-scope metadata capacity for the viewport-based workspace editor.</summary>
        public static DiagramPresentationLimits Workspace { get; } = new(20_000, 60_000, 33_554_432, 0.1);

        /// <summary>Gets the maximum validated containment depth.</summary>
        public int NestingDepth => this == Workspace ? 128 : 1;

        /// <summary>Gets the maximum finite container dimension.</summary>
        public double Dimension => this == Workspace ? 100_000 : 4000;

        /// <summary>Gets the CSS-pixel pan bound, allowing the complete workspace at maximum zoom.</summary>
        public double Pan => this == Workspace ? 1_000_000 : 100_000;

        /// <summary>Gets the bounded capacity for explicitly isolated large-scene experiments.</summary>
        public static DiagramPresentationLimits LargeBenchmark { get; } = new(10_000, 15_000, 16_777_216, 0.005);

        /// <summary>Gets the maximum graphical occurrence count.</summary>
        public int Nodes { get; }

        /// <summary>Gets the maximum connection occurrence count.</summary>
        public int Routes { get; }

        /// <summary>Gets the maximum presentation file size.</summary>
        public int JsonBytes { get; }

        /// <summary>Gets the minimum accepted viewport scale.</summary>
        public double MinimumZoom { get; }
    }
}
