// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramBenchmarkOptions.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Configuration
{
    /// <summary>Controls explicitly isolated diagnostic execution without changing production defaults.</summary>
    public sealed class DiagramBenchmarkOptions
    {
        /// <summary>The diagnostic configuration section.</summary>
        public const string SectionName = "DiagramBenchmark";

        /// <summary>The request capability cookie supplied by a local benchmark runner.</summary>
        public const string CookieName = "Bloom.DiagramBenchmark";

        /// <summary>Gets or sets whether diagnostic editors and probes are enabled.</summary>
        public bool Enabled { get; set; }

        /// <summary>Gets or sets whether isolated stress tabs may use the bounded large-scene allowance.</summary>
        public bool LargeSceneEnabled { get; set; }

        /// <summary>Gets or sets the number of seven-part bundles in the isolated on-demand fixture.</summary>
        public int OnDemandTiles { get; set; }

        /// <summary>Gets or sets the diagnostic whole-model control for the same distinct-ID fixture.</summary>
        public bool PreloadOnDemandFixture { get; set; }

        /// <summary>Gets or sets whether isolated large-scene experiments use the browser renderer's asynchronous scheduler.</summary>
        public bool AsyncRendering { get; set; }

        /// <summary>Gets or sets whether the isolated routing control uses endpoint-only orthogonal paths.</summary>
        public bool SimpleRouting { get; set; }

        /// <summary>Gets or sets the per-run capability required before serving the application in diagnostic mode.</summary>
        public string AccessKey { get; set; } = string.Empty;
    }
}
