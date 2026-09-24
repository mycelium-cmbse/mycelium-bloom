// ------------------------------------------------------------------------------------------------
// <copyright file="CommitElement.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ChangeNotifications
{
    /// <summary>Identifies one committed element mutation across transports.</summary>
    /// <param name="CommitId">The backend commit identifier.</param>
    /// <param name="ElementId">The stable element identifier.</param>
    internal readonly record struct CommitElement(Guid CommitId, Guid ElementId);
}
