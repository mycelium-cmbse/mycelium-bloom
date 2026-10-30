// ------------------------------------------------------------------------------------------------
// <copyright file="Organization.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    /// <summary>
    /// Describes an organization, the tenant boundary of Mycelium.
    /// This is a Mycelium extension: the Systems Modeling API defines no organization.
    /// </summary>
    /// <param name="Id">The stable identifier of the organization.</param>
    /// <param name="Slug">The unique address segment of the organization.</param>
    /// <param name="Name">The name displayed for the organization.</param>
    public sealed record Organization(Guid Id, string Slug, string Name);
}
