// ------------------------------------------------------------------------------------------------
// <copyright file="ModelOpeningState.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    /// <summary>Describes one session-only model opening operation.</summary>
    /// <param name="Busy">Whether an opening operation is running.</param>
    /// <param name="Status">The user-visible operation stage.</param>
    /// <param name="Progress">The completed upload percentage.</param>
    /// <param name="Error">The recoverable user-visible failure.</param>
    public sealed record ModelOpeningState(bool Busy = false, string Status = "", int Progress = 0, string Error = "");
}
