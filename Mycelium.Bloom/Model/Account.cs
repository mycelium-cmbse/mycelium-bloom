// ------------------------------------------------------------------------------------------------
// <copyright file="Account.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    /// <summary>
    /// Describes a Mycelium account. An account exists independently of the organizations it belongs to.
    /// This is a Mycelium extension: the Systems Modeling API defines no account.
    /// </summary>
    /// <param name="Id">The stable identifier of the account.</param>
    /// <param name="DisplayName">The name displayed for the account.</param>
    /// <param name="Email">The email address of the account.</param>
    public sealed record Account(Guid Id, string DisplayName, string Email);
}
