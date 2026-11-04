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

    using Mycelium.Bloom.Model;

    /// <summary>
    /// Shows the projects page of an organization: its title and counts, the search and the filters, and the projects
    /// list with its loading, empty, no results and error states.
    /// </summary>
    public sealed partial class ProjectsView
    {
        /// <summary>
        /// Applies the entry picked in the status filter menu.
        /// </summary>
        /// <param name="item">The picked entry.</param>
        /// <returns>A task that completes when the filter is applied.</returns>
        private async Task HandleStatusSelectedAsync(ActionMenuItem item)
        {
            await this.ViewModel.SelectStatusCommand.Execute(item);
        }

        /// <summary>
        /// Applies the entry picked in the mode filter menu.
        /// </summary>
        /// <param name="item">The picked entry.</param>
        /// <returns>A task that completes when the filter is applied.</returns>
        private async Task HandleModeSelectedAsync(ActionMenuItem item)
        {
            await this.ViewModel.SelectModeCommand.Execute(item);
        }

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
