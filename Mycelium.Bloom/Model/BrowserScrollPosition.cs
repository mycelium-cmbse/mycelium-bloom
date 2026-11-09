// ------------------------------------------------------------------------------------------------
// <copyright file="BrowserScrollPosition.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    /// <summary>Retains native tree scrolling for one Browser tab between mounts.</summary>
    /// <param name="Left">The horizontal scroll offset.</param>
    /// <param name="Top">The vertical scroll offset.</param>
    public sealed record BrowserScrollPosition(double Left, double Top);
}
