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
    using System.Reactive.Linq;

    using Mycelium.Bloom.Extensions;
    using Mycelium.SDK;

    /// <summary>
    /// Shows the projects page of an organization: its title and counts, the search and the filters, and the projects list.
    /// It flips between the loading, failed, empty and ready content from whether the ViewModel is busy, its errors and its
    /// rows, and it pages the rows that the ViewModel holds.
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
        /// Gets the lifecycle states that the status filter offers: no criterion first, then every state.
        /// </summary>
        private static IReadOnlyList<ProjectLifecycleKind?> StatusChoices { get; } = [null, .. Enum.GetValues<ProjectLifecycleKind>().Select(state => (ProjectLifecycleKind?)state)];

        /// <summary>
        /// Gets the modes that the mode filter offers: no criterion first, then every mode.
        /// </summary>
        private static IReadOnlyList<ProjectMode?> ModeChoices { get; } = [null, .. Enum.GetValues<ProjectMode>().Select(mode => (ProjectMode?)mode)];

        /// <summary>
        /// Gets the label of the status filter menu, which names the current choice.
        /// </summary>
        private string StatusFilterLabel => $"Status: {this.ViewModel.StatusFilter?.GetLabel() ?? AllLabel}";

        /// <summary>
        /// Gets the label of the mode filter menu, which names the current choice.
        /// </summary>
        private string ModeFilterLabel => $"Mode: {this.ViewModel.ModeFilter?.GetLabel() ?? AllLabel}";

        /// <summary>
        /// Gets a value indicating whether the loading of the projects failed.
        /// </summary>
        private bool HasFailed => this.ViewModel.Errors.Count > 0;

        /// <summary>
        /// Gets a value indicating whether the search or a filter narrows the projects.
        /// </summary>
        private bool HasFilters => this.ViewModel.SearchText.Length > 0 || this.ViewModel.StatusFilter is not null || this.ViewModel.ModeFilter is not null;

        /// <summary>
        /// Gets a value indicating whether the toolbar is shown: it stays for as long as the organization has projects, even
        /// while a reload narrows the rows to none, so that the search box keeps its focus, and it stays when a load failed.
        /// </summary>
        private bool ShowsToolbar => this.HasFailed || this.ViewModel.Counts is { Total: > 0 };

        /// <summary>
        /// Gets a value indicating whether the skeleton stands in for the rows, which is only until the first rows arrive.
        /// </summary>
        private bool ShowsSkeleton => this.ViewModel.IsBusy && this.ViewModel.Rows.Count == 0;

        /// <summary>
        /// Gets a value indicating whether the organization has no project at all.
        /// </summary>
        private bool ShowsEmptyOrganization => !this.ViewModel.IsBusy && this.ViewModel.Rows.Count == 0 && !this.HasFilters;

        /// <summary>
        /// Gets the title of the state shown when the search and the filters match no project.
        /// </summary>
        private string NoResultsTitle => this.ViewModel.SearchText.Length == 0
            ? "No projects match these filters"
            : $"No projects match \"{this.ViewModel.SearchText.Trim()}\"";

        /// <summary>
        /// Empties the search and resets both filters when the action of the no results state is clicked.
        /// </summary>
        /// <returns>A task that completes when the search and the filters are cleared.</returns>
        private async Task HandleClearClickedAsync()
        {
            await this.ViewModel.ClearFiltersCommand.Execute();
        }
    }
}
