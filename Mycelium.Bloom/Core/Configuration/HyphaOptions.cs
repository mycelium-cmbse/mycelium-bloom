// ------------------------------------------------------------------------------------------------
// <copyright file="HyphaOptions.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Configuration
{
    /// <summary>
    /// Provides strongly typed application configuration for the Hypha assistant hosted by Fabric.
    /// </summary>
    public sealed class HyphaOptions
    {
        /// <summary>
        /// The configuration section containing the Hypha settings.
        /// </summary>
        public const string SectionName = "Hypha";

        /// <summary>
        /// Gets or sets the absolute address of the Fabric host that serves the Hypha AG-UI endpoint.
        /// </summary>
        public Uri Endpoint { get; set; }
    }
}
