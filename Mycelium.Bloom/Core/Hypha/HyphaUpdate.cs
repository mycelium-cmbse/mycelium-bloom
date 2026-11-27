// ------------------------------------------------------------------------------------------------
// <copyright file="HyphaUpdate.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Hypha
{
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// One step of a Hypha conversation, independent of the AG-UI events it comes from.
    /// </summary>
    /// <param name="Kind">What this step carries.</param>
    /// <param name="Text">The question, the answer text or the error message; empty for tool calls and the end.</param>
    /// <param name="ToolCallId">The identifier of the tool call, for tool call steps.</param>
    /// <param name="ToolName">The name of the Fabric tool, for tool call steps.</param>
    public sealed record HyphaUpdate(HyphaUpdateKind Kind, string Text = "", string ToolCallId = null, string ToolName = null)
    {
        /// <summary>
        /// Creates an error step with a message shown to the user.
        /// </summary>
        /// <param name="message">The message shown to the user.</param>
        /// <returns>The error step.</returns>
        public static HyphaUpdate Error(string message) => new(HyphaUpdateKind.Error, message);
    }
}
