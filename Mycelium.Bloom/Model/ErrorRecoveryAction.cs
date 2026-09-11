// ------------------------------------------------------------------------------------------------
// <copyright file="ErrorRecoveryAction.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    /// <summary>
    /// Describes a recovery action that can execute without an interactive Blazor circuit.
    /// </summary>
    public sealed class ErrorRecoveryAction
    {
        /// <summary>
        /// Gets the action label and accessible name.
        /// </summary>
        public string Label { get; init; }

        /// <summary>
        /// Gets the navigation destination, or null for a browser operation.
        /// </summary>
        public string Href { get; init; }

        /// <summary>
        /// Gets the browser recovery operation, or null for a navigation link.
        /// </summary>
        public string BrowserAction { get; init; }
    }
}
