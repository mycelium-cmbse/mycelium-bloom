// ------------------------------------------------------------------------------------------------
// <copyright file="ServerSessionService.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Session
{
    using System.Text.Json;

    using ErrorOr;

    using SysML2.NET.REST.Exceptions;

    using SySML2.NET.REST;

    /// <summary>
    /// Holds the connection to a server through the session of the SysML2.NET REST client. A server that cannot be reached, or
    /// that does not answer as the API expects within ten seconds, gives one <see cref="ErrorCodes.Unavailable" /> error; the
    /// log message names no address, so that a typed address only reaches the logs through the text of the failure.
    /// </summary>
    public sealed partial class ServerSessionService : IServerSessionService
    {
        /// <summary>
        /// The time a server has to answer before the attempt fails, so that a server that never answers does not hold the page.
        /// </summary>
        private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// The error returned when the server cannot be reached or does not answer as the API expects.
        /// </summary>
        private static readonly Error ServerUnavailable = Error.Failure(ErrorCodes.Unavailable, "Cannot connect to this server");

        /// <summary>
        /// The session of the SysML2.NET REST client that holds the connection.
        /// </summary>
        private readonly ISession session;

        /// <summary>
        /// The logger that receives a failed connection.
        /// </summary>
        private readonly ILogger<ServerSessionService> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerSessionService" /> class.
        /// </summary>
        /// <param name="session">The session of the SysML2.NET REST client.</param>
        /// <param name="logger">The logger that receives a failed connection.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="session" /> or <paramref name="logger" /> is <see langword="null" />.</exception>
        public ServerSessionService(ISession session, ILogger<ServerSessionService> logger)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(logger);

            this.session = session;
            this.logger = logger;
        }

        /// <summary>
        /// Gets the address of the server of the open connection, or <see langword="null" /> when no connection is open.
        /// </summary>
        public Uri ServerAddress { get; private set; }

        /// <summary>
        /// Opens a connection to a server, replacing any open one, and checks that it answers with its projects. The open
        /// connection is dropped as soon as the attempt starts, so a failed attempt leaves none.
        /// </summary>
        /// <param name="serverAddress">The absolute address of the server.</param>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>Success, or an <see cref="ErrorCodes.Unavailable" /> error when the server does not answer as the API expects.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="serverAddress" /> is <see langword="null" />.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the request is cancelled.</exception>
        public async Task<ErrorOr<Success>> OpenAsync(Uri serverAddress, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(serverAddress);

            this.ServerAddress = null;

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(ConnectionTimeout);

            try
            {
                await this.session.Open(serverAddress, timeout.Token);
            }
            catch (Exception exception) when (exception is HttpRequestException or PsmApiException or JsonException or NotSupportedException || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
            {
                this.LogConnectionFailure(exception);
                return ServerUnavailable;
            }

            this.ServerAddress = serverAddress;
            return Result.Success;
        }

        /// <summary>
        /// Logs a connection that failed, with a message that names no address.
        /// </summary>
        /// <param name="exception">The failure.</param>
        [LoggerMessage(Level = LogLevel.Warning, Message = "The connection to the model server failed.")]
        private partial void LogConnectionFailure(Exception exception);
    }
}
