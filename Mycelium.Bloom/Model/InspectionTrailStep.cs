// ------------------------------------------------------------------------------------------------
// <copyright file="InspectionTrailStep.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    /// <summary>A resolved presentation of one stable inspection-history identifier, without retaining SDK objects.</summary>
    /// <param name="ElementId">The exact stable model identifier.</param>
    /// <param name="DisplayName">The full accessible model label, or the unavailable identifier.</param>
    /// <param name="IsAvailable">Whether this step can currently be resolved.</param>
    public sealed record InspectionTrailStep(string ElementId, string DisplayName, bool IsAvailable);
}
