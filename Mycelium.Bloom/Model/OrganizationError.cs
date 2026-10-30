// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationError.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Describes an expected refusal of an organization service, reported as data so that a ViewModel can map it to a state.
    /// </summary>
    /// <param name="Code">The machine-readable code of the refusal.</param>
    /// <param name="Message">The human-readable description of the refusal.</param>
    public sealed record OrganizationError(OrganizationErrorCode Code, string Message);
}
