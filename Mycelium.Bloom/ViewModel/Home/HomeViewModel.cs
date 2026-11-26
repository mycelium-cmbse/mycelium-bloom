// ------------------------------------------------------------------------------------------------
// <copyright file="HomeViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Home
{
    using DynamicData;

    using ErrorOr;

    using Mycelium.SDK.REST;

    /// <summary>
    /// Holds the Home page: the projects of the server of the open connection, read from the server when the page opens, since
    /// the session keeps no list of its own that could be out of date. Without an open connection nothing is read and the page
    /// goes back to the start page.
    /// </summary>
    public sealed class HomeViewModel : ListLoadingViewModel<Project>, IHomeViewModel
    {
        /// <summary>
        /// The session that reads the projects of the server.
        /// </summary>
        private readonly ISessionService sessionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeViewModel" /> class and starts reading the projects of the open
        /// connection.
        /// </summary>
        /// <param name="sessionService">The session that holds the connection and reads its projects.</param>
        /// <param name="logger">The logger that receives a failure to read the projects.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="sessionService" /> or <paramref name="logger" /> is <see langword="null" />.
        /// </exception>
        public HomeViewModel(ISessionService sessionService, ILogger<HomeViewModel> logger)
            : base("projects", logger)
        {
            ArgumentNullException.ThrowIfNull(sessionService);

            this.sessionService = sessionService;
            this.IsConnected = sessionService.ServerAddress is not null;

            if (this.IsConnected)
            {
                this.BeginLoad();
            }
        }

        /// <summary>
        /// Gets a value indicating whether a connection is open; without one the page goes back to the start page.
        /// </summary>
        public bool IsConnected { get; }

        /// <summary>
        /// Gets the projects of the server, so that a later screen can add a created project without another read.
        /// </summary>
        public ISourceList<Project> Projects => this.Source;

        /// <summary>
        /// Reads every project of the server, in the order the server returns them.
        /// </summary>
        /// <param name="cancellationToken">The token that cancels the read.</param>
        /// <returns>The projects, or the errors of the session.</returns>
        protected override Task<ErrorOr<IReadOnlyList<Project>>> LoadRowsAsync(CancellationToken cancellationToken)
        {
            return this.sessionService.GetProjectsAsync(cancellationToken: cancellationToken);
        }
    }
}
