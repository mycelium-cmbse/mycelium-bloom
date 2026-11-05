// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectFilter.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    using Mycelium.SDK;

    /// <summary>
    /// Narrows the projects of an organization. The criteria combine: a project matches when it satisfies all of them.
    /// </summary>
    /// <remarks>
    /// Placeholder transfer object, to be replaced by the query of the Fabric API when the Fabric client is bound.
    /// </remarks>
    /// <param name="SearchText">The text that the name or the description contains, whatever the case, as a literal substring and not as a pattern; empty for any.</param>
    /// <param name="Lifecycle">The lifecycle state that the project is in, or <see langword="null" /> for any.</param>
    /// <param name="Mode">The mode of the project, or <see langword="null" /> for any.</param>
    public sealed record ProjectFilter(string SearchText, ProjectLifecycleKind? Lifecycle, ProjectMode? Mode);
}
