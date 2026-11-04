// ------------------------------------------------------------------------------------------------
// <copyright file="IProjectsViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Organization
{
    using System.ComponentModel;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    using ReactiveUI;
    using ReactiveUI.Primitives;

    /// <summary>
    /// Holds the state of the projects page of an organization.
    /// </summary>
    public interface IProjectsViewModel : IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// Gets what the page shows below its title.
        /// </summary>
        ProjectsListState State { get; }

        /// <summary>
        /// Gets the text that explains why the projects could not be retrieved, or an empty text.
        /// </summary>
        string ErrorMessage { get; }

        /// <summary>
        /// Gets the number of projects and the number in each lifecycle state, in words.
        /// </summary>
        string Subtitle { get; }

        /// <summary>
        /// Gets how many projects the list shows out of the projects of the organization, in words.
        /// </summary>
        string ShowingText { get; }

        /// <summary>
        /// Gets or sets the text that the name or the description of the projects must contain. The list follows it once
        /// the typing pauses.
        /// </summary>
        string SearchText { get; set; }

        /// <summary>
        /// Gets the label of the status filter menu, which names the current choice.
        /// </summary>
        string StatusFilterLabel { get; }

        /// <summary>
        /// Gets the entries of the status filter menu, the current choice marked.
        /// </summary>
        IReadOnlyList<ActionMenuItem> StatusFilterItems { get; }

        /// <summary>
        /// Gets the label of the mode filter menu, which names the current choice.
        /// </summary>
        string ModeFilterLabel { get; }

        /// <summary>
        /// Gets the entries of the mode filter menu, the current choice marked.
        /// </summary>
        IReadOnlyList<ActionMenuItem> ModeFilterItems { get; }

        /// <summary>
        /// Gets the title of the state shown when the search and the filters match no project.
        /// </summary>
        string NoResultsTitle { get; }

        /// <summary>
        /// Gets the command that applies the entry picked in the status filter menu.
        /// </summary>
        ReactiveCommand<ActionMenuItem, RxVoid> SelectStatusCommand { get; }

        /// <summary>
        /// Gets the command that applies the entry picked in the mode filter menu.
        /// </summary>
        ReactiveCommand<ActionMenuItem, RxVoid> SelectModeCommand { get; }

        /// <summary>
        /// Gets the command that empties the search and resets both filters.
        /// </summary>
        ReactiveCommand<RxVoid, RxVoid> ClearFiltersCommand { get; }

        /// <summary>
        /// Gets a value indicating whether the current account may create a project.
        /// </summary>
        bool CanCreateProject { get; }

        /// <summary>
        /// Gets the rows of the projects list, the most recently updated project first.
        /// </summary>
        IReadOnlyList<ProjectRowViewModel> Rows { get; }
    }
}
