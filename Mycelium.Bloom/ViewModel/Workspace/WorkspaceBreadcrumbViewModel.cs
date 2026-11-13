// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceBreadcrumbViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Workspace
{
    using ErrorOr;

    using Mycelium.Bloom.Core;
    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.Core.Session;

    using ReactiveUI;

    using Organization = Mycelium.SDK.POCO.Organization;

    /// <summary>
    /// Holds the breadcrumb of the workspace header: the project opened in the workspace, read from the server of the open
    /// connection, and the organization it was opened from. An organization that cannot be read only drops its crumb, because the
    /// project stays the one the user opened.
    /// </summary>
    public sealed class WorkspaceBreadcrumbViewModel : DisposableViewModel, IWorkspaceBreadcrumbViewModel
    {
        /// <summary>
        /// The error kept when the opening fails in a way the services do not report, so that the header shows the error page instead
        /// of ending the circuit.
        /// </summary>
        private static readonly Error OpenFailed = Error.Unexpected(ErrorCodes.Unavailable, "The project could not be opened.");

        /// <summary>
        /// The session that reads the project.
        /// </summary>
        private readonly IServerSessionService sessionService;

        /// <summary>
        /// The service that reads the organization the project was opened from.
        /// </summary>
        private readonly IOrganizationContextService organizationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkspaceBreadcrumbViewModel" /> class.
        /// </summary>
        /// <param name="sessionService">The session that reads the project.</param>
        /// <param name="organizationService">The service that reads the organization the project was opened from.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="sessionService" /> or <paramref name="organizationService" /> is <see langword="null" />.</exception>
        public WorkspaceBreadcrumbViewModel(IServerSessionService sessionService, IOrganizationContextService organizationService)
        {
            ArgumentNullException.ThrowIfNull(sessionService);
            ArgumentNullException.ThrowIfNull(organizationService);

            this.sessionService = sessionService;
            this.organizationService = organizationService;
        }

        /// <summary>
        /// Gets a value indicating whether a connection is open, without which no project can be opened.
        /// </summary>
        public bool IsConnected => this.sessionService.ServerAddress is not null;

        /// <summary>
        /// Gets the errors of the last opening: a project the server does not know, or a server that cannot be reached.
        /// </summary>
        public IReadOnlyList<Error> Errors
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        } = [];

        /// <summary>
        /// Gets the opened project, or <see langword="null" /> before it is read.
        /// </summary>
        public Project Project
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>
        /// Gets the organization the project was opened from, or <see langword="null" /> when it was opened from the Home page.
        /// </summary>
        public Organization Organization
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>
        /// Reads the project and, when it was opened from an organization, that organization.
        /// </summary>
        /// <param name="projectId">The identifier of the project on the server.</param>
        /// <param name="organizationSlug">The slug of the organization, or <see langword="null" /> from the Home page.</param>
        /// <param name="cancellationToken">The token that cancels the reading when the breadcrumb is removed.</param>
        /// <returns>The completion of the reading.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the reading is cancelled.</exception>
        public async Task OpenAsync(Guid projectId, string organizationSlug, CancellationToken cancellationToken)
        {
            try
            {
                var project = await this.sessionService.GetProjectAsync(projectId, cancellationToken);

                if (project.IsError)
                {
                    this.Errors = project.Errors;
                    return;
                }

                if (!string.IsNullOrEmpty(organizationSlug))
                {
                    var context = await this.organizationService.GetOrganizationContextAsync(organizationSlug, cancellationToken);
                    this.Organization = context.IsError ? null : context.Value.Organization;
                }

                this.Project = project.Value;
                this.Errors = [];
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                this.Errors = [OpenFailed];
            }
        }
    }
}
