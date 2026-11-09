// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramViewportOptions.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Configuration
{
    /// <summary>Configures the prototype's detailed Z editor viewport.</summary>
    public sealed class DiagramViewportOptions
    {
        /// <summary>Gets or sets the detailed editor's minimum scale.</summary>
        public double MinimumZoom { get; set; } = 0.5;

        /// <summary>Gets or sets the screen-space mounting margin.</summary>
        public double Margin { get; set; } = 160;

        /// <summary>Gets or sets the screen-space padding reachable beyond the complete scene.</summary>
        public double PanPadding { get; set; } = 400;

        /// <summary>Gets or sets the visible-node threshold that triggers a density warning without hiding content.</summary>
        public int DensityWarningCount { get; set; } = 500;

        /// <summary>Gets or sets whether isolated benchmark tabs use rendering virtualization.</summary>
        public bool Enabled { get; set; } = true;
    }
}
