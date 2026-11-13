// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceBreadcrumb.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Molecules.WorkspaceBreadcrumb
{
    using ErrorOr;

    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.WebUtilities;

    using Mycelium.Bloom.Core.Session;
    using Mycelium.Bloom.ViewModel.Workspace;

    /// <summary>
    /// Shows, after the logo of the workspace header, the organization the project was opened from and the project. The project is
    /// named by the address of the workspace, read once when the workspace opens, so that a reload or a shared link opens it again:
    /// without a connection the start page opens first and comes back here, a malformed or unknown project shows the not found
    /// page, and a server that cannot be reached shows the error page.
    /// </summary>
    public sealed partial class WorkspaceBreadcrumb
    {
        /// <summary>
        /// The route of the error page.
        /// </summary>
        private const string ErrorPath = "/Error";

        /// <summary>
        /// The cancellation of a reading that is still running when the breadcrumb is removed.
        /// </summary>
        private readonly CancellationTokenSource closing = new();

        /// <summary>
        /// Gets or sets the factory that creates the state owned by this breadcrumb instance.
        /// </summary>
        [Inject]
        private Func<IWorkspaceBreadcrumbViewModel> ViewModelFactory { get; set; }

        /// <summary>
        /// Gets or sets the navigation manager that reads the address and leaves for the start, not found or error page.
        /// </summary>
        [Inject]
        private NavigationManager NavigationManager { get; set; }

        /// <summary>
        /// Gets the slug of the organization named by the address, or <see langword="null" /> when the project was opened from the
        /// Home page.
        /// </summary>
        private string OrganizationSlug { get; set; }

        /// <summary>
        /// Creates the state of the breadcrumb.
        /// </summary>
        protected override void OnInitialized()
        {
            this.ViewModel = this.ViewModelFactory();
            base.OnInitialized();
        }

        /// <summary>
        /// Opens the project named by the address, or leaves for the start page when no connection is open.
        /// </summary>
        /// <returns>The completion of the opening.</returns>
        protected override async Task OnInitializedAsync()
        {
            var query = QueryHelpers.ParseQuery(this.NavigationManager.ToAbsoluteUri(this.NavigationManager.Uri).Query);

            if (!query.TryGetValue(WorkspaceRoutes.ProjectParameter, out var projectValue))
            {
                return;
            }

            if (!Guid.TryParse(projectValue, out var projectId))
            {
                this.NavigationManager.NotFound();
                return;
            }

            if (!this.ViewModel.IsConnected)
            {
                this.NavigationManager.NavigateTo(WorkspaceRoutes.GetConnectionPath(projectId), replace: true);
                return;
            }

            this.OrganizationSlug = query.TryGetValue(WorkspaceRoutes.OrganizationParameter, out var slug) ? slug.ToString() : null;

            try
            {
                await this.ViewModel.OpenAsync(projectId, this.OrganizationSlug, this.closing.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            this.Redirect(this.ViewModel.Errors);
        }

        /// <summary>
        /// Cancels a reading that is still running and releases the state of the breadcrumb.
        /// </summary>
        /// <param name="disposing">Whether the call comes from <see cref="IDisposable.Dispose" />.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.closing.Cancel();
                this.closing.Dispose();
                this.ViewModel?.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Shows the not found page for a project the server does not know, and the error page for any other failure.
        /// </summary>
        /// <param name="errors">The errors of the opening.</param>
        private void Redirect(IReadOnlyList<Error> errors)
        {
            if (errors.Count == 0)
            {
                return;
            }

            if (errors[0].Type is ErrorType.NotFound)
            {
                this.NavigationManager.NotFound();
            }
            else
            {
                this.NavigationManager.NavigateTo(ErrorPath, replace: true);
            }
        }
    }
}
