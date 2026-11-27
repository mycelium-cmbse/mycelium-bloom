// ------------------------------------------------------------------------------------------------
// <copyright file="IHyphaClient.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Hypha
{
    using ErrorOr;

    /// <summary>
    /// Talks to the Hypha agent of Fabric over AG-UI.
    /// </summary>
    public interface IHyphaClient
    {
        /// <summary>
        /// Sends one question and streams the answer. Failures end the stream with an error step, never an exception;
        /// cancelling the token closes the stream, which makes Fabric stop the answer.
        /// </summary>
        /// <param name="question">The question of the user.</param>
        /// <param name="context">The project, user and selection sent with the question.</param>
        /// <param name="cancellationToken">Stops the answer.</param>
        /// <returns>The steps of the answer, ending with <see cref="Model.Enum.HyphaUpdateKind.Finished" /> or an error.</returns>
        IAsyncEnumerable<HyphaUpdate> AskAsync(string question, HyphaContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Reads back the conversation Fabric keeps for one thread.
        /// </summary>
        /// <param name="threadId">The conversation identifier.</param>
        /// <param name="cancellationToken">Cancels the request.</param>
        /// <returns>The steps of the conversation, or an error when Fabric cannot be read.</returns>
        Task<ErrorOr<IReadOnlyList<HyphaUpdate>>> GetHistoryAsync(string threadId, CancellationToken cancellationToken);
    }
}
