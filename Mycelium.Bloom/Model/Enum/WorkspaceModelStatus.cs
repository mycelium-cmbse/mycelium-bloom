// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceModelStatus.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Enum
{
    /// <summary>Describes availability of the active workspace model.</summary>
    public enum WorkspaceModelStatus
    {
        /// <summary>No model is active.</summary>
        Empty,
        /// <summary>The existing source is loading.</summary>
        Loading,
        /// <summary>A canonical model is available.</summary>
        Ready,
        /// <summary>The source could not be loaded.</summary>
        Failed,
        /// <summary>The scope is no longer available to this workspace.</summary>
        Unavailable
    }
}
