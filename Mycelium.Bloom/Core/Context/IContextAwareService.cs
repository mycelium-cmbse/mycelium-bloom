// ------------------------------------------------------------------------------------------------
// <copyright file="IContextAwareService.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Context
{
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Provides the circuit-scoped application context used by context-aware features.
    /// </summary>
    public interface IContextAwareService : IElementSelectionService
    {
        /// <summary>
        /// Gets or sets the current project lifecycle state.
        /// </summary>
        ProjectLifecycleState LifecycleState { get; set; }
    }
}
