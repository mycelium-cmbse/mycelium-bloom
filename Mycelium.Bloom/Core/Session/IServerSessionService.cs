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

    using SySML2.NET.REST;

    /// <summary>
    /// Holds the connection of the current user to a server that implements the OMG Systems Modeling API and Services, one per
    /// circuit, so that every page of the user works against the same server.
    /// </summary>
    public interface IServerSessionService
    {
        /// <summary>
        /// Gets the session of the SysML2.NET REST client, through which other services and ViewModels reach the server of the
        /// open connection. The connection is opened through <see cref="OpenAsync" /> only, which keeps
        /// <see cref="ServerAddress" /> and <see cref="Projects" /> in step with it.
        /// </summary>
        ISession Session { get; }

        /// <summary>
        /// Gets the address of the server of the open connection, or <see langword="null" /> when no connection is open.
        /// </summary>
        Uri ServerAddress { get; }

        /// <summary>
        /// Gets the projects of the server, read when the connection opened, or an empty list when no connection is open.
        /// </summary>
        IReadOnlyList<Project> Projects { get; }

        /// <summary>
        /// Opens a connection to a server, replacing any open one, and reads every page of its projects. The open connection
        /// and its projects are dropped as soon as the attempt starts, so a failed attempt leaves none.
        /// </summary>
        /// <param name="serverAddress">The absolute address of the server.</param>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>Success, or an <see cref="ErrorCodes.Unavailable" /> error when the server does not answer as the API expects.</returns>
        Task<ErrorOr<Success>> OpenAsync(Uri serverAddress, CancellationToken cancellationToken);
    }
}
