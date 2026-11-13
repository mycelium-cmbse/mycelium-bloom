// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectsView.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.ProjectsView
{
    using Humanizer;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.ViewModel.Organization;
    using Mycelium.SDK;

    /// <summary>
    /// Shows the projects page of an organization: its title, the search and the filters, and the projects list.
    /// It flips between the loading, failed, empty and ready content from whether the ViewModel is busy, its errors and its
    /// rows. The search and the two filters narrow the rows here, in the view, and the table pages them: the ViewModel only
    /// holds the projects.
    /// </summary>
    public sealed partial class ProjectsView
    {
        /// <summary>
        /// The label of the choice that applies no criterion to a filter.
        /// </summary>
        private const string AllLabel = "All";

        /// <summary>
        /// The number of projects the table shows on a page; the pager appears once the rows exceed it.
        /// </summary>
        private const int PageSize = 25;

        /// <summary>
        /// Gets the CSS classes of the mode badge of a project, which tint the Concurrent mode.
        /// </summary>
        /// <param name="row">The row of the project.</param>
        /// <returns>The CSS classes of the badge.</returns>
        private static string GetModeCssClass(ProjectRowViewModel row)
        {
            return row.Mode == ProjectMode.Concurrent ? "mb-projects-view__mode mb-projects-view__mode--concurrent" : "mb-projects-view__mode";
        }

        /// <summary>
        /// Gets the lifecycle states that the status filter offers: no criterion first, then every state.
        /// </summary>
        private static IReadOnlyList<ProjectLifecycleKind?> StatusChoices { get; } = [null, .. Enum.GetValues<ProjectLifecycleKind>().Select(state => (ProjectLifecycleKind?)state)];

        /// <summary>
        /// Gets the modes that the mode filter offers: no criterion first, then every mode.
        /// </summary>
        private static IReadOnlyList<ProjectMode?> ModeChoices { get; } = [null, .. Enum.GetValues<ProjectMode>().Select(mode => (ProjectMode?)mode)];

        /// <summary>
        /// Gets or sets the slug of the organization of the page, which a project opened from the table keeps in the address of the
        /// workspace.
        /// </summary>
        [Parameter]
        public string OrganizationSlug { get; set; }

        /// <summary>
        /// Gets or sets the text that the name or the description of the projects must contain.
        /// </summary>
        private string SearchText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the lifecycle state that the projects must have, or <see langword="null" /> for every state.
        /// </summary>
        private ProjectLifecycleKind? StatusFilter { get; set; }

        /// <summary>
        /// Gets or sets the mode that the projects must have, or <see langword="null" /> for every mode.
        /// </summary>
        private ProjectMode? ModeFilter { get; set; }

        /// <summary>
        /// Gets the projects that match the search and the filters.
        /// </summary>
        private IReadOnlyList<ProjectRowViewModel> FilteredRows => [.. this.ViewModel.Rows.Where(this.Matches)];

        /// <summary>
        /// Gets the label of the status filter menu, which names the current choice.
        /// </summary>
        private string StatusFilterLabel => $"Status: {this.StatusFilter?.Humanize() ?? AllLabel}";

        /// <summary>
        /// Gets the label of the mode filter menu, which names the current choice.
        /// </summary>
        private string ModeFilterLabel => $"Mode: {this.ModeFilter?.Humanize() ?? AllLabel}";

        /// <summary>
        /// Gets a value indicating whether the loading of the projects failed.
        /// </summary>
        private bool HasFailed => this.ViewModel.Errors.Count > 0;

        /// <summary>
        /// Gets a value indicating whether the toolbar is shown: it stays for as long as the organization has projects, even
        /// while the search narrows the rows to none, so that the search box keeps its focus, and it stays when a load failed.
        /// </summary>
        private bool ShowsToolbar => this.HasFailed || this.ViewModel.Rows.Count > 0;

        /// <summary>
        /// Gets a value indicating whether the skeleton stands in for the rows, which is only until the first rows arrive.
        /// </summary>
        private bool ShowsSkeleton => this.ViewModel.IsBusy && this.ViewModel.Rows.Count == 0;

        /// <summary>
        /// Gets a value indicating whether the organization has no project at all.
        /// </summary>
        private bool ShowsEmptyOrganization => !this.ViewModel.IsBusy && this.ViewModel.Rows.Count == 0;

        /// <summary>
        /// Gets the words under the table: how many projects it shows out of the projects of the organization.
        /// </summary>
        private string ShowingText => $"Showing {this.FilteredRows.Count} of {"project".ToQuantity(this.ViewModel.Rows.Count)}";

        /// <summary>
        /// Gets the title of the state shown when the search and the filters match no project.
        /// </summary>
        private string NoResultsTitle => string.IsNullOrWhiteSpace(this.SearchText)
            ? "No projects match these filters"
            : $"No projects match \"{this.SearchText.Trim()}\"";

        /// <summary>
        /// Tells whether a row matches the search and the filters.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <returns><see langword="true" /> when the row has the chosen state and mode and its name or description holds the search.</returns>
        private bool Matches(ProjectRowViewModel row)
        {
            var text = this.SearchText.Trim();

            return (this.StatusFilter is null || row.Lifecycle == this.StatusFilter)
                   && (this.ModeFilter is null || row.Mode == this.ModeFilter)
                   && (row.Name.Contains(text, StringComparison.OrdinalIgnoreCase) || row.Description.Contains(text, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Empties the search and resets both filters when the action of the no results state is clicked.
        /// </summary>
        private void HandleClearClicked()
        {
            this.SearchText = string.Empty;
            this.StatusFilter = null;
            this.ModeFilter = null;
        }
    }
}
