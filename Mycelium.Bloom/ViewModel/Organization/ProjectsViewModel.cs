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

    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.Model;
    using Mycelium.SDK;

    using ReactiveUI;

    using RxVoid = ReactiveUI.Primitives.RxVoid;

    /// <summary>
    /// Loads the projects of the organization of the page and the counts of its projects. The counts are loaded once; a
    /// search or a filter reloads the projects only. Every load runs on the synchronization context that the ViewModel was
    /// created on, which is the one of the renderer, so that the rows are never changed while the view reads them.
    /// </summary>
    public sealed partial class ProjectsViewModel : DisposableViewModel, IProjectsViewModel
    {
        /// <summary>
        /// The pause in typing after which the search reloads the projects.
        /// </summary>
        private static readonly TimeSpan SearchDelay = TimeSpan.FromMilliseconds(300);

        /// <summary>
        /// The error reported when the projects cannot be retrieved for a reason that the service did not name. The cause is
        /// logged, not shown.
        /// </summary>
        private static readonly Error LoadFailed = Error.Unexpected("Projects.LoadFailed", "The projects could not be loaded.");

        /// <summary>
        /// Provides the projects and their counts.
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
        /// Holds the rows of the projects list.
        /// </summary>
        private readonly SourceList<ProjectRowViewModel> rowSource = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectsViewModel" /> class and starts loading the projects.
        /// </summary>
        /// <param name="context">
        /// The context of the organization whose projects are listed, resolved before the page that owns this ViewModel is shown.
        /// </param>
        /// <param name="projectService">The service that provides the projects and their counts.</param>
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

            var rowBinding = this.rowSource.Connect().Bind(out var rows).Subscribe();
            this.Rows = rows;

            this.ClearFiltersCommand = ReactiveCommand.Create(() =>
            {
                this.SearchText = string.Empty;
                this.StatusFilter = null;
                this.ModeFilter = null;
            });

            var commandFailures = this.ClearFiltersCommand.ThrownExceptions.Subscribe(this.LogCommandFailure);

            var renderContext = SynchronizationContext.Current;
            IScheduler scheduler = renderContext is null ? Scheduler.Immediate : new SynchronizationContextScheduler(renderContext);

            var loads = this.WhenAnyValue(model => model.SearchText)
                .Select(text => text.Length == 0 ? Observable.Return(text) : Observable.Timer(SearchDelay).Select(_ => text))
                .Switch()
                .CombineLatest(this.WhenAnyValue(model => model.StatusFilter), this.WhenAnyValue(model => model.ModeFilter),
                    (text, lifecycle, mode) => new ProjectFilter(text.Trim(), lifecycle, mode))
                .DistinctUntilChanged()
                .ObserveOn(scheduler)
                .Select(filter => Observable.FromAsync(cancellationToken => this.LoadAsync(organizationId, filter, cancellationToken)))
                .Switch()
                .Subscribe();

            this.Disposables.AddRange([loads, commandFailures, this.ClearFiltersCommand, rowBinding, this.rowSource]);
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
        /// Gets the errors that the last load returned, or an empty list when it succeeded.
        /// </summary>
        public IReadOnlyList<Error> Errors
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        } = [];

        /// <summary>
        /// Gets the number of projects of the organization, in total and by lifecycle state, or <see langword="null" /> until
        /// they are loaded.
        /// </summary>
        public ProjectCounts Counts
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>
        /// Gets or sets the text that the name or the description of the projects must contain. The projects follow it once
        /// the typing pauses.
        /// </summary>
        public string SearchText
        {
            get;
            set => this.RaiseAndSetIfChanged(ref field, value ?? string.Empty);
        } = string.Empty;

        /// <summary>
        /// Gets or sets the lifecycle state that the projects must be in, or <see langword="null" /> for any.
        /// </summary>
        public ProjectLifecycleKind? StatusFilter
        {
            get;
            set => this.RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>
        /// Gets or sets the mode that the projects must have, or <see langword="null" /> for any.
        /// </summary>
        public ProjectMode? ModeFilter
        {
            get;
            set => this.RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>
        /// Gets the rows of the projects list, the most recently updated project first.
        /// </summary>
        public ReadOnlyObservableCollection<ProjectRowViewModel> Rows { get; }

        /// <summary>
        /// Gets a value indicating whether the current account may create a project.
        /// </summary>
        public bool CanCreateProject { get; }

        /// <summary>
        /// Gets the command that empties the search and resets both filters.
        /// </summary>
        public ReactiveCommand<RxVoid, RxVoid> ClearFiltersCommand { get; }

        /// <summary>
        /// Logs the failure to load the projects.
        /// </summary>
        /// <param name="exception">The failure.</param>
        [LoggerMessage(Level = LogLevel.Error, Message = "The projects of the organization could not be loaded.")]
        private partial void LogLoadFailure(Exception exception);

        /// <summary>
        /// Logs the code of the error that the project service returned instead of the projects or their counts.
        /// </summary>
        /// <param name="code">The code of the error.</param>
        [LoggerMessage(Level = LogLevel.Warning, Message = "The projects of the organization could not be loaded: {Code}.")]
        private partial void LogLoadError(string code);

        /// <summary>
        /// Logs the failure of a command of the page.
        /// </summary>
        /// <param name="exception">The failure.</param>
        [LoggerMessage(Level = LogLevel.Error, Message = "A command of the projects page failed.")]
        private partial void LogCommandFailure(Exception exception);

        /// <summary>
        /// Gets the projects that match a filter, and the counts when they are not loaded yet, in parallel requests, then
        /// shows them, or keeps the errors when either request returns an error or fails. A load that a newer filter or the
        /// disposal cancelled shows nothing and leaves the ViewModel busy for the newer load.
        /// </summary>
        /// <param name="organizationId">The identifier of the organization.</param>
        /// <param name="filter">The criteria that the projects must match.</param>
        /// <param name="cancellationToken">The token that cancels the load.</param>
        /// <returns>A task that completes when the projects are shown or the errors are kept.</returns>
        private async Task LoadAsync(Guid organizationId, ProjectFilter filter, CancellationToken cancellationToken)
        {
            this.IsBusy = true;

            try
            {
                var projectsRequest = this.projectService.GetProjectsAsync(organizationId, filter, cancellationToken);
                var countsRequest = this.Counts is null
                    ? this.projectService.GetProjectCountsAsync(organizationId, cancellationToken)
                    : Task.FromResult<ErrorOr<ProjectCounts>>(this.Counts);
                await Task.WhenAll(projectsRequest, countsRequest);
                var projects = await projectsRequest;
                var counts = await countsRequest;
                cancellationToken.ThrowIfCancellationRequested();

                if (projects.IsError || counts.IsError)
                {
                    var errors = projects.IsError ? projects.Errors : counts.Errors;
                    this.LogLoadError(errors[0].Code);
                    this.rowSource.Clear();
                    this.Counts = null;
                    this.Errors = errors;
                    return;
                }

                var now = this.timeProvider.GetUtcNow();
                this.rowSource.Edit(rows =>
                {
                    rows.Clear();
                    rows.AddRange(projects.Value.Select(project => new ProjectRowViewModel(project, now)));
                });
                this.Counts = counts.Value;
                this.Errors = [];
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                this.LogLoadFailure(exception);
                this.rowSource.Clear();
                this.Counts = null;
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
