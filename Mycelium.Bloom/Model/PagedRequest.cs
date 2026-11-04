// ------------------------------------------------------------------------------------------------
// <copyright file="PagedRequest.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    /// <summary>
    /// Asks for one page of a list.
    /// </summary>
    /// <remarks>
    /// Placeholder transfer object, to be replaced by the paging contract of the Fabric API when the Fabric client is bound.
    /// The service decides the largest page it answers.
    /// </remarks>
    /// <param name="PageIndex">The zero-based index of the page.</param>
    /// <param name="PageSize">The maximum number of items of the page.</param>
    public sealed record PagedRequest(int PageIndex, int PageSize);
}
