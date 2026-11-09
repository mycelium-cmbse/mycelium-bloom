// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramDataOptions.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Configuration
{
    /// <summary>Configures the bounded cache of formatted workspace model details.</summary>
    public sealed class DiagramDataOptions
    {
        /// <summary>Gets or sets the maximum estimated UTF-8 detail bytes.</summary>
        public long CapacityBytes { get; set; } = 1_048_576;

        /// <summary>Gets or sets detail sliding expiration in seconds.</summary>
        public int ExpirationSeconds { get; set; } = 120;


    }
}
