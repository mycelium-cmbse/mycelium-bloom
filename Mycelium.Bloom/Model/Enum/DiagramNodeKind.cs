// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramNodeKind.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Enum
{
    /// <summary>Selects a reusable general-node shape.</summary>
    public enum DiagramNodeKind
    {

        /// <summary>A square-cornered definition.</summary>
        Definition,

        /// <summary>A rounded usage.</summary>
        Usage,

        /// <summary>A dotted reference usage.</summary>
        Reference,

        /// <summary>A general element without specialized notation.</summary>
        Element
    }
}
