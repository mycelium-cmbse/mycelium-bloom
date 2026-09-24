// ------------------------------------------------------------------------------------------------
// <copyright file="ModelRefreshRegistration.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ChangeNotifications
{
    /// <summary>Pairs a model owner with cancellation for its accepted refresh requests.</summary>
    /// <param name="Handler">The connected current-model owner.</param>
    internal sealed record ModelRefreshRegistration(IModelRefreshHandler Handler)
    {
        /// <summary>Gets cancellation owned and released by the coordinator.</summary>
        internal CancellationTokenSource Cancellation { get; } = new();
    }
}
