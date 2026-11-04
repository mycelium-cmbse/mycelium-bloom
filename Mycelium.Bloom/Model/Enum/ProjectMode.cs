// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectMode.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Enum
{
    /// <summary>
    /// Defines how the members of a project work on its model.
    /// </summary>
    /// <remarks>
    /// Placeholder mode of the projects page, to be replaced by the mode of the Fabric API when the Fabric client is
    /// bound.
    /// </remarks>
    public enum ProjectMode
    {
        /// <summary>
        /// The members edit the model in the regular way.
        /// </summary>
        Regular,

        /// <summary>
        /// The members work on the model together in a concurrent design session.
        /// </summary>
        ConcurrentDesign
    }
}
