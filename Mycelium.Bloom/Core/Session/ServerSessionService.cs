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
    /// Holds the connection to a server through the session of the SysML2.NET REST client, and the projects read when it opened,
    /// so that the pages need no further call to list them. A server that cannot be reached, that does not answer as the API
    /// expects within ten seconds per request, or whose pages of projects never end, gives one <see cref="ErrorCodes.Unavailable" />
    /// error; the log message names no address, so that a typed address only reaches the logs through the text of the failure.
    /// </summary>
    public sealed partial class ServerSessionService : IServerSessionService
    {
        /// <summary>
        /// The number of pages of projects read at most, so that a server that keeps sending a next page cannot hold the page.
        /// </summary>
        private const int MaximumPageCount = 1000;

        /// <summary>
        /// The time a server has to answer a request before the attempt fails, so that a server that never answers does not hold
        /// the page.
        /// </summary>
        private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// The error returned when the server cannot be reached or does not answer as the API expects.
        /// </summary>
        private static readonly Error ServerUnavailable = Error.Failure(ErrorCodes.Unavailable, "Cannot connect to this server");

        /// <summary>
        /// The logger that receives a failed request.
        /// </summary>
        private readonly ILogger<ServerSessionService> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerSessionService" /> class.
        /// </summary>
        /// <param name="session">The session of the SysML2.NET REST client.</param>
        /// <param name="logger">The logger that receives a failed request.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="session" /> or <paramref name="logger" /> is <see langword="null" />.</exception>
        public ServerSessionService(ISession session, ILogger<ServerSessionService> logger)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(logger);

            this.Session = session;
            this.logger = logger;
        }

        /// <summary>
        /// Gets the session of the SysML2.NET REST client, through which other services and ViewModels reach the server of the
        /// open connection.
        /// </summary>
        public ISession Session { get; }

        /// <summary>
        /// Gets the address of the server of the open connection, or <see langword="null" /> when no connection is open.
        /// </summary>
        public Uri ServerAddress { get; private set; }

        /// <summary>
        /// Gets the projects of the server, read when the connection opened, or an empty list when no connection is open.
        /// </summary>
        public IReadOnlyList<Project> Projects { get; private set; } = [];

        /// <summary>
        /// Opens a connection to a server, replacing any open one, and reads every page of its projects: the first one comes with
        /// the opening, the next ones follow the cursor of the page before. The open connection and its projects are dropped as
        /// soon as the attempt starts, so a failed attempt leaves none.
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
            this.Projects = [];

            var page = await this.RequestAsync(token => this.Session.Open(serverAddress, token), cancellationToken);
            var projects = new List<Project>();
            var readCursors = new HashSet<string>();

            while (!page.IsError)
            {
                projects.AddRange(page.Value.Records);

                if (page.Value.Next is not { } cursor)
                {
                    this.Projects = projects;
                    this.ServerAddress = serverAddress;
                    return Result.Success;
                }

                if (!readCursors.Add(cursor.Token) || readCursors.Count >= MaximumPageCount)
                {
                    return ServerUnavailable;
                }

                var parameters = new QueryParameters { PageAfter = cursor };
                page = await this.RequestAsync(token => this.Session.Client.GetProjectsAsync(parameters, token), cancellationToken);
            }

            return page.Errors;
        }

        /// <summary>
        /// Sends one request to the server with its own timeout, and turns a server that cannot be reached or does not answer as
        /// the API expects into an <see cref="ErrorCodes.Unavailable" /> error.
        /// </summary>
        /// <typeparam name="T">The type of the answer.</typeparam>
        /// <param name="request">The request, which receives the token that cancels it.</param>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>The answer, or an <see cref="ErrorCodes.Unavailable" /> error.</returns>
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
