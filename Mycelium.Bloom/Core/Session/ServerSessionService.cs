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

    using SysML2.NET.PSM;
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
        /// The most pages read for one list, so that a server that never ends its list does not hold the page or its memory.
        /// </summary>
        private const int MaximumPageCount = 1000;

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

            var opened = await this.RequestAsync(token => this.session.Open(serverAddress, token), cancellationToken);

            if (opened.IsError)
            {
                return opened.Errors;
            }

            this.ServerAddress = serverAddress;
            return Result.Success;
        }

        /// <summary>
        /// Reads every project of the server of the open connection, page after page, in the order the server returns them. A
        /// list whose pages repeat or never end is refused as a server that does not answer as the API expects.
        /// </summary>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>The projects, or an <see cref="ErrorCodes.Unavailable" /> error when no connection is open or the server does not answer as the API expects.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the request is cancelled.</exception>
        public async Task<ErrorOr<IReadOnlyList<Project>>> GetProjectsAsync(CancellationToken cancellationToken)
        {
            if (this.ServerAddress is null || this.session.Client is not { } client)
            {
                return ServerUnavailable;
            }

            var projects = new List<Project>();
            var readCursors = new HashSet<string>();
            Page? next = null;

            do
            {
                var parameters = new QueryParameters { PageAfter = next };
                var page = await this.RequestAsync(token => client.GetProjectsAsync(parameters, token), cancellationToken);

                if (page.IsError)
                {
                    return page.Errors;
                }

                projects.AddRange(page.Value.Records);
                next = page.Value.Next;

                if (next is { } cursor && (!readCursors.Add(cursor.Token) || readCursors.Count > MaximumPageCount))
                {
                    return ServerUnavailable;
                }
            }
            while (next is not null);

            return projects;
        }

        /// <summary>
        /// Sends one request to the server with its own time limit, and turns a server that cannot be reached or does not answer as
        /// the API expects into an <see cref="ErrorCodes.Unavailable" /> error.
        /// </summary>
        /// <typeparam name="T">The type of the answer.</typeparam>
        /// <param name="request">The request, given the token that cancels it.</param>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>The answer, or the <see cref="ErrorCodes.Unavailable" /> error.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the request is cancelled.</exception>
        private async Task<ErrorOr<T>> RequestAsync<T>(Func<CancellationToken, Task<T>> request, CancellationToken cancellationToken)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(ConnectionTimeout);

            try
            {
                return await request(timeout.Token);
            }
            catch (Exception exception) when (exception is HttpRequestException or PsmApiException or JsonException or NotSupportedException || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
            {
                this.LogRequestFailure(exception);
                return ServerUnavailable;
            }
        }

        /// <summary>
        /// Logs a request that failed, with a message that names no address.
        /// </summary>
        /// <param name="exception">The failure.</param>
        [LoggerMessage(Level = LogLevel.Warning, Message = "A request to the model server failed.")]
        private partial void LogRequestFailure(Exception exception);
    }
}
