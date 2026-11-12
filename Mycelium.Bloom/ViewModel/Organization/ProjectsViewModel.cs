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
    using DynamicData;

    using ErrorOr;

    using Mycelium.Bloom.Core.Organization;

    /// <summary>
    /// Loads the projects of the organization of the page once and holds them, the most recently updated first, with the
    /// failure of the load. The projects live in a source list that others can subscribe to or add to, and the rows mirror
    /// it.
    /// </summary>
    public sealed class ProjectsViewModel : ListLoadingViewModel<ProjectRowViewModel>, IProjectsViewModel
    {
        /// <summary>
        /// Provides the projects.
        /// </summary>
        private readonly IOrganizationProjectService projectService;

        /// <summary>
        /// Provides the current time, from which the last update of each project is told.
        /// </summary>
        private readonly TimeProvider timeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectsViewModel" /> class and starts loading the projects.
        /// </summary>
        /// <param name="context">
        /// The context of the organization whose projects are listed, resolved before the page that owns this ViewModel is shown.
        /// </param>
        /// <param name="projectService">The service that provides the projects.</param>
        /// <param name="timeProvider">The provider of the current time.</param>
        /// <param name="logger">The logger that receives a failure to load the projects.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="projectService" />, <paramref name="timeProvider" /> or <paramref name="logger" /> is
        /// <see langword="null" />.
        /// </exception>
        public ProjectsViewModel(IOrganizationContextViewModel context, IOrganizationProjectService projectService, TimeProvider timeProvider, ILogger<ProjectsViewModel> logger)
            : base(context.Organization.Id, "projects", logger)
        {
            ArgumentNullException.ThrowIfNull(projectService);
            ArgumentNullException.ThrowIfNull(timeProvider);

            this.projectService = projectService;
            this.timeProvider = timeProvider;
            this.CanCreateProject = context.Capabilities is { CanCreateProject: true };
            this.BeginLoad();
        }

        /// <summary>
        /// Gets every project of the organization, which a later ViewModel can subscribe to or add the created project to.
        /// </summary>
        public ISourceList<ProjectRowViewModel> Projects => this.Source;

        /// <summary>
        /// Gets a value indicating whether the current account may create a project.
        /// </summary>
        public bool CanCreateProject { get; }

        /// <summary>
        /// Gets the projects of the organization as rows, the most recently updated first, told from one current time.
        /// </summary>
        /// <param name="organizationId">The identifier of the organization.</param>
        /// <param name="cancellationToken">The token that cancels the load.</param>
        /// <returns>The rows, or the errors of the service.</returns>
        protected override async Task<ErrorOr<IReadOnlyList<ProjectRowViewModel>>> LoadRowsAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            var result = await this.projectService.GetProjectsAsync(organizationId, cancellationToken);
            var now = this.timeProvider.GetUtcNow();

            return result.Then(projects => (IReadOnlyList<ProjectRowViewModel>)
                [.. projects.OrderByDescending(project => project.UpdatedOn).Select(project => new ProjectRowViewModel(project, now))]);
        }
    }
}
