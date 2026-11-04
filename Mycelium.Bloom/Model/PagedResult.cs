// ------------------------------------------------------------------------------------------------
// <copyright file="PagedResult.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    /// <summary>
    /// Holds one page of a list and the size of the whole list.
    /// </summary>
    /// <remarks>
    /// Placeholder transfer object, to be replaced by the paging contract of the Fabric API when the Fabric client is bound.
    /// </remarks>
    /// <typeparam name="TItem">The type of the items of the list.</typeparam>
    /// <param name="Items">The items of the page.</param>
    /// <param name="TotalCount">The number of items of the whole list.</param>
    public sealed record PagedResult<TItem>(IReadOnlyList<TItem> Items, int TotalCount);
}
