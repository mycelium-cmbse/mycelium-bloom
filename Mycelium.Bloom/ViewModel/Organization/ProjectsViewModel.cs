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
    using System.Reactive.Linq;

    using ErrorOr;

    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    using ReactiveUI;
    using ReactiveUI.Primitives.Concurrency;

    using RxVoid = ReactiveUI.Primitives.RxVoid;

    /// <summary>
    /// Loads the projects of the organization of the page and holds what the projects page shows. The page loads only
    /// its own content, the projects and their counts, on top of what the layout already loaded. The counts are
    /// loaded once; a search or a filter reloads the projects only.
    /// </summary>
    public sealed partial class ProjectsViewModel : DisposableViewModel, IProjectsViewModel
    {
        /// <summary>
        /// The text shown when the projects cannot be retrieved. The cause is logged, not shown.
        /// </summary>
        private const string LoadFailureMessage = "The projects could not be loaded. Try again later.";

        /// <summary>
        /// The number of projects that the page asks for. The page has no pager yet.
        /// </summary>
        private const int PageSize = 50;

        /// <summary>
        /// The label of the entry, and of the choice, that applies no criterion to a filter.
        /// </summary>
        private const string AllLabel = "All";

        /// <summary>
        /// The identifier of the entry that applies no criterion. It is not a value of the enumeration, so picking it clears
        /// the filter.
        /// </summary>
        private const string AllId = "all";

        /// <summary>
        /// The pause in typing after which the search reloads the projects.
        /// </summary>
        private static readonly TimeSpan SearchDelay = TimeSpan.FromMilliseconds(300);

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
        /// Holds the label of the status filter menu, which follows the status filter.
        /// </summary>
        private readonly ObservableAsPropertyHelper<string> statusFilterLabel;

        /// <summary>
        /// Holds the entries of the status filter menu, which follow the status filter.
        /// </summary>
        private readonly ObservableAsPropertyHelper<IReadOnlyList<ActionMenuItem>> statusFilterItems;

        /// <summary>
        /// Holds the label of the mode filter menu, which follows the mode filter.
        /// </summary>
        private readonly ObservableAsPropertyHelper<string> modeFilterLabel;

        /// <summary>
        /// Holds the entries of the mode filter menu, which follow the mode filter.
        /// </summary>
        private readonly ObservableAsPropertyHelper<IReadOnlyList<ActionMenuItem>> modeFilterItems;

        /// <summary>
        /// The counts of the projects of the organization, once they are loaded.
        /// </summary>
        private ProjectCounts counts;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectsViewModel" /> class and starts loading the projects.
        /// </summary>
        /// <param name="context">
        /// The context of the organization whose projects are listed, resolved before the page that owns this ViewModel is shown.
        /// </param>
        /// <param name="projectService">The service that provides the projects and their counts.</param>
        /// <param name="timeProvider">The provider of the current time.</param>
        /// <param name="logger">The logger that receives a failure to load the projects.</param>
        /// <exception cref="ArgumentNullException">Thrown when an argument is <see langword="null" />.</exception>
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

            this.SelectStatusCommand = ReactiveCommand.Create<ActionMenuItem>(
                item => this.StatusFilter = Enum.TryParse<ProjectLifecycleState>(item.Id, out var state) ? state : null);
            this.SelectModeCommand = ReactiveCommand.Create<ActionMenuItem>(
                item => this.ModeFilter = Enum.TryParse<ProjectMode>(item.Id, out var mode) ? mode : null);
            this.ClearFiltersCommand = ReactiveCommand.Create(() =>
            {
                this.SearchText = string.Empty;
                this.StatusFilter = null;
                this.ModeFilter = null;
            });

            var status = this.WhenAnyValue(model => model.StatusFilter);
            var mode = this.WhenAnyValue(model => model.ModeFilter);

            this.statusFilterLabel = status
                .Select(state => GetFilterLabel("Status", ProjectLabelFormatter.GetLifecycleLabel, state))
                .ToProperty(this, model => model.StatusFilterLabel, scheduler: ImmediateSequencer.Instance);
            this.statusFilterItems = status
                .Select(state => CreateItems(ProjectLabelFormatter.GetLifecycleLabel, state))
                .ToProperty(this, model => model.StatusFilterItems, scheduler: ImmediateSequencer.Instance);
            this.modeFilterLabel = mode
                .Select(value => GetFilterLabel("Mode", ProjectLabelFormatter.GetModeLabel, value))
                .ToProperty(this, model => model.ModeFilterLabel, scheduler: ImmediateSequencer.Instance);
            this.modeFilterItems = mode
                .Select(value => CreateItems(ProjectLabelFormatter.GetModeLabel, value))
                .ToProperty(this, model => model.ModeFilterItems, scheduler: ImmediateSequencer.Instance);

            var commandFailures = Observable
                .Merge(this.SelectStatusCommand.ThrownExceptions, this.SelectModeCommand.ThrownExceptions, this.ClearFiltersCommand.ThrownExceptions)
                .Subscribe(this.LogCommandFailure);

            // A search waits for a pause in typing, but emptying it applies at once so that clearing does not wait.
            var loads = this.WhenAnyValue(model => model.SearchText)
                .Select(text => text.Length == 0 ? Observable.Return(text) : Observable.Timer(SearchDelay).Select(_ => text))
                .Switch()
                .CombineLatest(status, mode, (text, state, value) => new ProjectFilter(text.Trim(), state, value))
                .DistinctUntilChanged()
                .Select(filter => Observable.FromAsync(cancellationToken => this.LoadAsync(organizationId, filter, cancellationToken)))
                .Switch()
                .Subscribe();

            // The commands, the menu labels and entries, the observation of the failures of the commands and the reloads that
            // follow the search and the filters are released together; the running reload is cancelled on disposal.
            this.Disposables.AddRange([this.SelectStatusCommand, this.SelectModeCommand, this.ClearFiltersCommand, this.statusFilterLabel,
                this.statusFilterItems, this.modeFilterLabel, this.modeFilterItems, commandFailures, loads]);
        }

        /// <summary>
        /// Gets what the page shows below its title.
        /// </summary>
        public ProjectsListState State
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        } = ProjectsListState.Loading;

        /// <summary>
        /// Gets the number of projects and the number in each lifecycle state, in words.
        /// </summary>
        public string Subtitle
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        } = string.Empty;

        /// <summary>
        /// Gets how many projects the list shows out of the projects of the organization, in words.
        /// </summary>
        public string ShowingText
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        } = string.Empty;

        /// <summary>
        /// Gets or sets the text that the name or the description of the projects must contain. The list follows it once
        /// the typing pauses.
        /// </summary>
        public string SearchText
        {
            get;
            set => this.RaiseAndSetIfChanged(ref field, value ?? string.Empty);
        } = string.Empty;

        /// <summary>
        /// Gets the lifecycle state that the projects must be in, or <see langword="null" /> for any.
        /// </summary>
        public ProjectLifecycleState? StatusFilter
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>
        /// Gets the mode that the projects must have, or <see langword="null" /> for any.
        /// </summary>
        public ProjectMode? ModeFilter
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>
        /// Gets the label of the status filter menu, which names the current choice.
        /// </summary>
        public string StatusFilterLabel => this.statusFilterLabel.Value;

        /// <summary>
        /// Gets the entries of the status filter menu, the current choice marked.
        /// </summary>
        public IReadOnlyList<ActionMenuItem> StatusFilterItems => this.statusFilterItems.Value;

        /// <summary>
        /// Gets the label of the mode filter menu, which names the current choice.
        /// </summary>
        public string ModeFilterLabel => this.modeFilterLabel.Value;

        /// <summary>
        /// Gets the entries of the mode filter menu, the current choice marked.
        /// </summary>
        public IReadOnlyList<ActionMenuItem> ModeFilterItems => this.modeFilterItems.Value;

        /// <summary>
        /// Gets the title of the state shown when the search and the filters match no project.
        /// </summary>
        public string NoResultsTitle
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        } = string.Empty;

        /// <summary>
        /// Gets the command that applies the entry picked in the status filter menu.
        /// </summary>
        public ReactiveCommand<ActionMenuItem, RxVoid> SelectStatusCommand { get; }

        /// <summary>
        /// Gets the command that applies the entry picked in the mode filter menu.
        /// </summary>
        public ReactiveCommand<ActionMenuItem, RxVoid> SelectModeCommand { get; }

        /// <summary>
        /// Gets the command that empties the search and resets both filters.
        /// </summary>
        public ReactiveCommand<RxVoid, RxVoid> ClearFiltersCommand { get; }

        /// <summary>
        /// Gets a value indicating whether the current account may create a project.
        /// </summary>
        public bool CanCreateProject { get; }

        /// <summary>
        /// Gets the rows of the projects list, the most recently updated project first.
        /// </summary>
        public IReadOnlyList<ProjectRowViewModel> Rows
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        } = [];

        /// <summary>
        /// Gets the page size of the table of the projects: every row of the page that was loaded, and at least one because a
        /// table cannot have a page size of zero.
        /// </summary>
        /// <returns>The number of rows that the table shows on its one page.</returns>
        public int GetTablePageSize()
        {
            return Math.Max(this.Rows.Count, 1);
        }

        /// <summary>
        /// Writes a number of projects in words.
        /// </summary>
        /// <param name="count">The number of projects.</param>
        /// <returns>The number followed by "project" or "projects".</returns>
        private static string CountProjects(int count)
        {
            return count == 1 ? "1 project" : $"{count} projects";
        }

        /// <summary>
        /// Writes the number of projects and the number in each lifecycle state in words.
        /// </summary>
        /// <param name="counts">The counts of the projects of the organization.</param>
        /// <returns>The subtitle of the page.</returns>
        private static string BuildSubtitle(ProjectCounts counts)
        {
            return $"{CountProjects(counts.Total)} · {counts.Open} open · {counts.Preparation} in preparation · "
                   + $"{counts.Review} in review · {counts.Archived} archived";
        }

        /// <summary>
        /// Writes the label of a filter menu, which names the current choice.
        /// </summary>
        /// <typeparam name="TValue">The enumeration that the filter chooses from.</typeparam>
        /// <param name="name">The name of the filter.</param>
        /// <param name="getLabel">Gives the label of a value.</param>
        /// <param name="selected">The current choice, or <see langword="null" /> when no criterion applies.</param>
        /// <returns>The name of the filter followed by the label of the current choice.</returns>
        private static string GetFilterLabel<TValue>(string name, Func<TValue, string> getLabel, TValue? selected)
            where TValue : struct, Enum
        {
            return $"{name}: {(selected is { } value ? getLabel(value) : AllLabel)}";
        }

        /// <summary>
        /// Creates the entries of a filter menu: the entry that applies no criterion, then one entry per value of the
        /// enumeration, in its order.
        /// </summary>
        /// <typeparam name="TValue">The enumeration that the filter chooses from.</typeparam>
        /// <param name="getLabel">Gives the label of a value.</param>
        /// <param name="selected">The current choice, or <see langword="null" /> when no criterion applies.</param>
        /// <returns>The entries, the current choice marked.</returns>
        private static IReadOnlyList<ActionMenuItem> CreateItems<TValue>(Func<TValue, string> getLabel, TValue? selected)
            where TValue : struct, Enum
        {
            return
            [
                new ActionMenuItem { Id = AllId, Label = AllLabel, IsSelected = selected is null },
                .. Enum.GetValues<TValue>()
                    .Select(value => new ActionMenuItem { Id = value.ToString(), Label = getLabel(value), IsSelected = selected.Equals(value) })
            ];
        }

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
        /// shows them, or shows the failure when either request returns an error or fails. A load that a newer filter or the
        /// disposal cancelled shows nothing.
        /// </summary>
        /// <param name="organizationId">The identifier of the organization.</param>
        /// <param name="filter">The criteria that the projects must match.</param>
        /// <param name="cancellationToken">The token that cancels the load.</param>
        /// <returns>A task that completes when the projects are shown or the failure is shown.</returns>
        private async Task LoadAsync(Guid organizationId, ProjectFilter filter, CancellationToken cancellationToken)
        {
            try
            {
                var projectsRequest = this.projectService.GetProjectsAsync(organizationId, filter, new PagedRequest(0, PageSize), cancellationToken);
                var countsRequest = this.counts is null
                    ? this.projectService.GetProjectCountsAsync(organizationId, cancellationToken)
                    : Task.FromResult<ErrorOr<ProjectCounts>>(this.counts);
                await Task.WhenAll(projectsRequest, countsRequest);
                var projects = await projectsRequest;
                var countsResult = await countsRequest;
                cancellationToken.ThrowIfCancellationRequested();

                if (projects.IsError || countsResult.IsError)
                {
                    this.LogLoadError(projects.IsError ? projects.FirstError.Code : countsResult.FirstError.Code);
                    this.ShowFailure();
                    return;
                }

                this.counts = countsResult.Value;
                this.Show(projects.Value, filter);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // A newer filter or the disposal replaced this load: what it would show is out of date.
            }
            catch (Exception exception)
            {
                this.LogLoadFailure(exception);
                this.ShowFailure();
            }
        }

        /// <summary>
        /// Shows the projects that match the filter, or the empty state when the organization has no project at all. A
        /// filter that matches nothing leaves the list ready, with no rows.
        /// </summary>
        /// <param name="projects">The page of projects that match the filter.</param>
        /// <param name="filter">The criteria that the projects match.</param>
        private void Show(PagedResult<ProjectSummary> projects, ProjectFilter filter)
        {
            var now = this.timeProvider.GetUtcNow();

            this.Rows = [.. projects.Items.Select(project => new ProjectRowViewModel(project, now))];
            this.Subtitle = BuildSubtitle(this.counts);
            this.ShowingText = $"Showing {projects.TotalCount} of {CountProjects(this.counts.Total)}";
            this.NoResultsTitle = filter.SearchText.Length == 0
                ? "No projects match these filters"
                : $"No projects match \"{filter.SearchText}\"";
            this.ErrorMessage = string.Empty;
            this.State = this.counts.Total == 0 ? ProjectsListState.Empty : ProjectsListState.Ready;
        }

        /// <summary>
        /// Shows that the projects could not be retrieved.
        /// </summary>
        private void ShowFailure()
        {
            this.ErrorMessage = LoadFailureMessage;
            this.State = ProjectsListState.Error;
        }
    }
}
