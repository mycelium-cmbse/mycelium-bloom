// ------------------------------------------------------------------------------------------------
// <copyright file="HyphaUpdateKind.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Enum
{
    /// <summary>
    /// Defines what one step of a Hypha conversation, live or read back from Fabric, carries.
    /// </summary>
    public enum HyphaUpdateKind
    {
        /// <summary>
        /// A question of the user, read back from the conversation history.
        /// </summary>
        Question,

        /// <summary>
        /// A piece of the answer text, appended to the current answer.
        /// </summary>
        Text,

        /// <summary>
        /// A piece of the reasoning of the model, written before the answer text or a tool call.
        /// </summary>
        Reasoning,

        /// <summary>
        /// Hypha started a call of a Fabric tool.
        /// </summary>
        ToolCallStarted,

        /// <summary>
        /// A call of a Fabric tool returned its result.
        /// </summary>
        ToolCallFinished,

        /// <summary>
        /// The answer is complete.
        /// </summary>
        Finished,

        /// <summary>
        /// The answer failed: Fabric is unreachable, refused the question or reported an error.
        /// </summary>
        Error
    }
}
