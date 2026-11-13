// ------------------------------------------------------------------------------------------------
// <copyright file="IServerSessionService.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Session
{
    using ErrorOr;

    using SysML2.NET.PSM.DTO;

    /// <summary>
    /// Holds the connection of the current user to a server that implements the OMG Systems Modeling API and Services, one per
    /// circuit, so that every page of the user works against the same server.
    /// </summary>
    public interface IServerSessionService
    {
        /// <summary>
        /// Gets the address of the server of the open connection, or <see langword="null" /> when no connection is open.
        /// </summary>
        Uri ServerAddress { get; }

        /// <summary>
        /// Opens a connection to a server, replacing any open one, and checks that it answers with its projects. The open
        /// connection is dropped as soon as the attempt starts, so a failed attempt leaves none.
        /// </summary>
        /// <param name="serverAddress">The absolute address of the server.</param>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>Success, or an <see cref="ErrorCodes.Unavailable" /> error when the server does not answer as the API expects.</returns>
        Task<ErrorOr<Success>> OpenAsync(Uri serverAddress, CancellationToken cancellationToken);

        /// <summary>
        /// Reads every project of the server of the open connection, page after page, in the order the server returns them. A
        /// list whose pages repeat or never end is refused as a server that does not answer as the API expects.
        /// </summary>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>The projects, or an <see cref="ErrorCodes.Unavailable" /> error when no connection is open or the server does not answer as the API expects.</returns>
        Task<ErrorOr<IReadOnlyList<Project>>> GetProjectsAsync(CancellationToken cancellationToken);
    }
}
