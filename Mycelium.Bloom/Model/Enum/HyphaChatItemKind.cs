// ------------------------------------------------------------------------------------------------
// <copyright file="HyphaChatItemKind.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Enum
{
    /// <summary>
    /// Defines the rows of the Hypha conversation.
    /// </summary>
    public enum HyphaChatItemKind
    {
        /// <summary>
        /// A question of the user.
        /// </summary>
        Question,

        /// <summary>
        /// Answer text of Hypha, in Markdown.
        /// </summary>
        Answer,

        /// <summary>
        /// The reasoning of the model, folded under one line.
        /// </summary>
        Reasoning,

        /// <summary>
        /// One call of a Fabric tool.
        /// </summary>
        ToolCall,

        /// <summary>
        /// A failure explained to the user.
        /// </summary>
        Error
    }
}
