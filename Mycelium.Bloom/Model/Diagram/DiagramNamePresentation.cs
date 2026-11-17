// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramNamePresentation.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    using System.Collections.Immutable;

    /// <summary>Carries SDK declaration text and caller-selected graphical name metadata.</summary>
    /// <param name="Keyword">The graphical BNF keyword, without guillemets.</param>
    /// <param name="Declaration">The declaration emitted by the SDK.</param>
    /// <param name="Aliases">The selected alias names.</param>
    /// <param name="SymbolName">The optional Bloom symbol name.</param>
    /// <param name="AdditionalKeywords">Optional subsequent BNF keyword lines for a specialized name compartment.</param>
    public sealed record DiagramNamePresentation(string Keyword, string Declaration, ImmutableArray<string> Aliases, string SymbolName = null,
        ImmutableArray<string> AdditionalKeywords = default);
}
