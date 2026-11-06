// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectsViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Organization
{
    using System.Collections.ObjectModel;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;

    using DynamicData;

    using ErrorOr;

    using Mycelium.Bloom.Core;
    using Mycelium.Bloom.Core.Organization;

    using ReactiveUI;

    /// <summary>
    /// Loads the projects of the organization of the page once and holds them, the most recently updated first, with the
    /// failure of the load. The projects live in a source list that others can subscribe to or add to, and the rows mirror
    /// it. The load runs on the synchronization context that the ViewModel was created on, which is the one of the
    /// renderer, so that the rows are never changed while the view reads them.
    /// </summary>
    public sealed partial class ProjectsViewModel : DisposableViewModel, IProjectsViewModel
    {
        /// <summary>
        /// The error reported when the projects cannot be retrieved for a reason that the service did not name. The cause is
        /// logged, not shown.
        /// </summary>
        private static readonly Error LoadFailed = Error.Unexpected(ErrorCodes.Unavailable, "The projects could not be loaded.");

        /// <summary>
        /// Provides the projects.
        /// </summary>
        private readonly IOrganizationProjectService projectService;

        /// <summary>
        /// Provides the current time, from which the last update of each project is told.
        /// </summary>
        private readonly TimeProvider timeProvider;

        /// <summary>
        /// Receives the failure to load the projects.
        /// </summary>
        private readonly ILogger<ProjectsViewModel> logger;

        /// <summary>
        /// Holds every project of the organization.
        /// </summary>
        private readonly SourceList<ProjectRowViewModel> projects = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectsViewModel" /> class and starts loading the projects.
        /// </summary>
        /// <param name="context">
        /// The context of the organization whose projects are listed, resolved before the page that owns this ViewModel is shown.
        /// </param>
        /// <param name="projectService">The service that provides the projects.</param>
        /// <param name="timeProvider">The provider of the current time.</param>
        /// <param name="logger">The logger that receives a failure to load the projects.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="projectService" />, <paramref name="timeProvider" /> or <paramref name="logger" /> is <see langword="null" />.</exception>
        public ProjectsViewModel(IOrganizationContextViewModel context, IOrganizationProjectService projectService, TimeProvider timeProvider,
            ILogger<ProjectsViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(projectService);
            ArgumentNullException.ThrowIfNull(timeProvider);
            ArgumentNullException.ThrowIfNull(logger);

            this.projectService = projectService;
            this.timeProvider = timeProvider;
            this.logger = logger;
            this.CanCreateProject = context.Capabilities is { CanCreateProject: true };

            var organizationId = context.Organization.Id;
            var renderContext = SynchronizationContext.Current;
            IScheduler scheduler = renderContext is null ? Scheduler.Immediate : new SynchronizationContextScheduler(renderContext);

            var rowBinding = this.projects.Connect().ObserveOn(scheduler).Bind(out var rows).Subscribe(_ => this.RaisePropertyChanged(nameof(this.Rows)));
            this.Rows = rows;

            var load = Observable.Return(organizationId)
                .ObserveOn(scheduler)
                .Select(id => Observable.FromAsync(cancellationToken => this.LoadAsync(id, cancellationToken)))
                .Switch()
                .Subscribe();

            this.Disposables.AddRange([load, rowBinding, this.projects]);
        }

        /// <summary>
        /// Gets a value indicating whether the ViewModel is loading the projects.
        /// </summary>
        public bool IsBusy
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        } = true;

        /// <summary>
        /// Gets the errors that the load returned, or an empty list when it succeeded.
        /// </summary>
        public IReadOnlyList<Error> Errors
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        } = [];

        /// <summary>
        /// Gets every project of the organization, which a later ViewModel can subscribe to or add the created project to.
        /// </summary>
        public ISourceList<ProjectRowViewModel> Projects => this.projects;

        /// <summary>
        /// Gets the projects as rows, the most recently updated first.
        /// </summary>
        public ReadOnlyObservableCollection<ProjectRowViewModel> Rows { get; }

        /// <summary>
        /// Gets a value indicating whether the current account may create a project.
        /// </summary>
        public bool CanCreateProject { get; }

        /// <summary>
        /// Logs the failure to load the projects.
        /// </summary>
        /// <param name="exception">The failure.</param>
        [LoggerMessage(Level = LogLevel.Error, Message = "The projects of the organization could not be loaded.")]
        private partial void LogLoadFailure(Exception exception);

        /// <summary>
        /// Logs the error code of a refused load of the projects.
        /// </summary>
        /// <param name="code">The code of the error.</param>
        [LoggerMessage(Level = LogLevel.Warning, Message = "The projects of the organization could not be loaded: {Code}.")]
        private partial void LogLoadError(string code);

        /// <summary>
        /// Loads the projects of the organization, then shows them or the failure. A load that the disposal cancelled shows
        /// nothing.
        /// </summary>
        /// <param name="organizationId">The identifier of the organization.</param>
        /// <param name="cancellationToken">The token that cancels the load.</param>
        /// <returns>A task that completes when the projects or the failure are shown.</returns>
        private async Task LoadAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            this.IsBusy = true;

            try
            {
                var result = await this.projectService.GetProjectsAsync(organizationId, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                if (result.IsError)
                {
                    this.LogLoadError(result.FirstError.Code);
                    this.projects.Clear();
                    this.Errors = result.Errors;
                    return;
                }

                var now = this.timeProvider.GetUtcNow();
                this.projects.Edit(list =>
                {
                    list.Clear();
                    list.AddRange(result.Value.OrderByDescending(project => project.UpdatedOn).Select(project => new ProjectRowViewModel(project, now)));
                });
                this.Errors = [];
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                this.LogLoadFailure(exception);
                this.projects.Clear();
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
    }
}
