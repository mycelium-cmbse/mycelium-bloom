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
    using System.Collections.ObjectModel;

    using DynamicData;

    using Mycelium.Bloom.Core.Session;

    using ReactiveUI;

    /// <summary>
    /// Holds the Home page: the projects of the server of the open connection, which the session read when the connection
    /// opened, so that the page needs no call of its own. Without an open connection the page goes back to the start page.
    /// </summary>
    public sealed class HomeViewModel : DisposableViewModel, IHomeViewModel
    {
        /// <summary>
        /// The projects of the server.
        /// </summary>
        private readonly SourceList<Project> projects = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeViewModel" /> class with the projects of the open connection.
        /// </summary>
        /// <param name="sessionService">The session that holds the connection and its projects.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="sessionService" /> is <see langword="null" />.</exception>
        public HomeViewModel(IServerSessionService sessionService)
        {
            ArgumentNullException.ThrowIfNull(sessionService);

            this.IsConnected = sessionService.ServerAddress is not null;

            var rowBinding = this.projects.Connect().Bind(out var rows).Subscribe(_ => this.RaisePropertyChanged(nameof(this.Rows)));
            this.Rows = rows;
            this.Disposables.AddRange([rowBinding, this.projects]);
            this.projects.AddRange(sessionService.Projects);
        }

        /// <summary>
        /// Gets a value indicating whether a connection is open; without one the page goes back to the start page.
        /// </summary>
        public bool IsConnected { get; }

        /// <summary>
        /// Gets the projects of the server, so that a later screen can add a created project without another read.
        /// </summary>
        public ISourceList<Project> Projects => this.projects;

        /// <summary>
        /// Gets the projects bound for the view, in the order the server returns them.
        /// </summary>
        public ReadOnlyObservableCollection<Project> Rows { get; }
    }
}
