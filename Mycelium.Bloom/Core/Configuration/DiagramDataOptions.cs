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
    /// <summary>Configures the dedicated fetched-payload cache and bounded source requests.</summary>
    public sealed class DiagramDataOptions
    {
        /// <summary>Gets or sets the maximum cached UTF-8 payload bytes.</summary>
        public long CapacityBytes { get; set; } = 1_048_576;

        /// <summary>Gets or sets payload sliding expiration in seconds.</summary>
        public int ExpirationSeconds { get; set; } = 120;

        /// <summary>Gets or sets the maximum complete bundles in one request.</summary>
        public int BatchSize { get; set; } = 4;

        /// <summary>Gets or sets the maximum concurrent source requests.</summary>
        public int ConcurrentRequests { get; set; } = 2;
    }
}
