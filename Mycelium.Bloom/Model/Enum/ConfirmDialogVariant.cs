// ------------------------------------------------------------------------------------------------
// <copyright file="ConfirmDialogVariant.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Enum
{
    /// <summary>
    /// Defines the visual variants available for confirmation dialogs.
    /// </summary>
    public enum ConfirmDialogVariant
    {
        /// <summary>
        /// Represents a standard confirmation request.
        /// </summary>
        Default,

        /// <summary>
        /// Represents a confirmation request that warrants caution.
        /// </summary>
        Warning,

        /// <summary>
        /// Represents a destructive or critical confirmation request.
        /// </summary>
        Danger
    }
}
