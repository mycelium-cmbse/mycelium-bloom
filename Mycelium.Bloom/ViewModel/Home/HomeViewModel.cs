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
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;

    using DynamicData;

    using ErrorOr;

    using Mycelium.Bloom.Core;
    using Mycelium.Bloom.Core.Session;

    using ReactiveUI;

    /// <summary>
    /// Holds the Home page: the projects of the server of the open connection, loaded once when the page opens. Without an open
    /// connection nothing is loaded and the page goes back to the start page.
    /// </summary>
    public sealed partial class HomeViewModel : DisposableViewModel, IHomeViewModel
    {
        /// <summary>
        /// The error kept when the load fails in a way the session does not report.
        /// </summary>
        private static readonly Error LoadFailed = Error.Unexpected(ErrorCodes.Unavailable, "The projects could not be loaded.");

        /// <summary>
        /// The session that reads the projects.
        /// </summary>
        private readonly IServerSessionService sessionService;

        /// <summary>
        /// The logger that receives a load that failed.
        /// </summary>
        private readonly ILogger<HomeViewModel> logger;

        /// <summary>
        /// The projects of the server.
        /// </summary>
        private readonly SourceList<Project> projects = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeViewModel" /> class and starts loading the projects when a connection
        /// is open. The load runs on the synchronization context of the caller, the render context of the page, so the rows and the
        /// busy state change together.
        /// </summary>
        /// <param name="sessionService">The session that reads the projects.</param>
        /// <param name="logger">The logger that receives a load that failed.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="sessionService" /> or <paramref name="logger" /> is <see langword="null" />.</exception>
        public HomeViewModel(IServerSessionService sessionService, ILogger<HomeViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(sessionService);
            ArgumentNullException.ThrowIfNull(logger);

            this.sessionService = sessionService;
            this.logger = logger;
            this.IsConnected = sessionService.ServerAddress is not null;

            var renderContext = SynchronizationContext.Current;
            IScheduler scheduler = renderContext is null ? Scheduler.Immediate : new SynchronizationContextScheduler(renderContext);

            var rowBinding = this.projects.Connect().Bind(out var rows).Subscribe(_ => this.RaisePropertyChanged(nameof(this.Rows)));
            this.Rows = rows;
            this.Disposables.AddRange([rowBinding, this.projects]);

            if (this.IsConnected)
            {
                this.IsBusy = true;
                this.Disposables.Add(Observable.FromAsync(this.LoadAsync, scheduler).Subscribe());
            }
        }

        /// <summary>
        /// Gets a value indicating whether a connection is open; without one the page goes back to the start page.
        /// </summary>
        public bool IsConnected { get; }

        /// <summary>
        /// Gets a value indicating whether the projects are being loaded.
        /// </summary>
        public bool IsBusy
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>
        /// Gets the errors of the last load.
        /// </summary>
        public IReadOnlyList<Error> Errors
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        } = [];

        /// <summary>
        /// Gets the projects of the server, so that a later screen can add a created project without another load.
        /// </summary>
        public ISourceList<Project> Projects => this.projects;

        /// <summary>
        /// Gets the projects bound for the view, in the order the server returns them.
        /// </summary>
        public ReadOnlyObservableCollection<Project> Rows { get; }

        /// <summary>
        /// Loads the projects of the server, or keeps the error of the server.
        /// </summary>
        /// <param name="cancellationToken">The token that cancels the load when the page closes.</param>
        /// <returns>The completion of the load.</returns>
        private async Task LoadAsync(CancellationToken cancellationToken)
        {
            try
            {
                var result = await this.sessionService.GetProjectsAsync(cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (result.IsError)
                {
                    this.Errors = result.Errors;
                    return;
                }

                this.projects.Edit(list => list.AddRange(result.Value));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                this.LogLoadFailure(exception);
                this.Errors = [LoadFailed];
            }
            finally
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    this.IsBusy = false;
                }
            }
        }

        /// <summary>
        /// Logs a load that failed in a way the session does not report.
        /// </summary>
        /// <param name="exception">The failure.</param>
        [LoggerMessage(Level = LogLevel.Error, Message = "The projects of the model server could not be loaded.")]
        private partial void LogLoadFailure(Exception exception);
    }
}
