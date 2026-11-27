// ------------------------------------------------------------------------------------------------
// <copyright file="HyphaChatItem.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Hypha
{
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// One row of the Hypha conversation.
    /// </summary>
    /// <param name="Kind">The kind of row.</param>
    /// <param name="Text">The question, the Markdown answer, the reasoning, the readable tool name or the error message.</param>
    /// <param name="ToolCallId">The identifier of the tool call, for tool call rows.</param>
    /// <param name="IsDone">Whether the tool call returned, for tool call rows, or the reasoning ended, for reasoning rows.</param>
    /// <param name="Duration">How long the model reasoned, for reasoning rows seen live; <see langword="null" /> when read back from Fabric.</param>
    public sealed record HyphaChatItem(HyphaChatItemKind Kind, string Text, string ToolCallId = null, bool IsDone = false, TimeSpan? Duration = null);
}
