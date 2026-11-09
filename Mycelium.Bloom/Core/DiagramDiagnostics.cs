// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramDiagnostics.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core
{
    using System.Diagnostics;

    /// <summary>Exposes optional phase observations without changing presentation authority.</summary>
    public static class DiagramDiagnostics
    {
        /// <summary>Gets the process-lifetime instrumentation source, inactive without a listener.</summary>
        public static ActivitySource Source { get; } = new("Mycelium.Bloom.Diagram");
    }
}
