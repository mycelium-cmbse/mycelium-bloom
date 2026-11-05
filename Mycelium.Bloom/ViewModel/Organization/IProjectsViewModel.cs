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
    using System.Collections.ObjectModel;
    using System.ComponentModel;

    using ErrorOr;

    using Mycelium.Bloom.Model;
    using Mycelium.SDK;

    using ReactiveUI;
    using ReactiveUI.Primitives;

    /// <summary>
    /// Holds the projects of the organization of the projects page, the search and the filters that narrow them, and
    /// whether the ViewModel is loading them. The view decides what to show from these.
    /// </summary>
    public interface IProjectsViewModel : IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// Gets a value indicating whether the ViewModel is loading the projects.
        /// </summary>
        bool IsBusy { get; }

        /// <summary>
        /// Gets the errors that the last load returned, or an empty list when it succeeded.
        /// </summary>
        IReadOnlyList<Error> Errors { get; }

        /// <summary>
        /// Gets the number of projects of the organization, in total and by lifecycle state, or <see langword="null" /> until
        /// they are loaded.
        /// </summary>
        ProjectCounts Counts { get; }

        /// <summary>
        /// Gets or sets the text that the name or the description of the projects must contain. The projects follow it once
        /// the typing pauses.
        /// </summary>
        string SearchText { get; set; }

        /// <summary>
        /// Gets or sets the lifecycle state that the projects must be in, or <see langword="null" /> for any.
        /// </summary>
        ProjectLifecycleKind? StatusFilter { get; set; }

        /// <summary>
        /// Gets or sets the mode that the projects must have, or <see langword="null" /> for any.
        /// </summary>
        ProjectMode? ModeFilter { get; set; }

        /// <summary>
        /// Gets the rows of the projects list, the most recently updated project first.
        /// </summary>
        ReadOnlyObservableCollection<ProjectRowViewModel> Rows { get; }

        /// <summary>
        /// Gets a value indicating whether the current account may create a project.
        /// </summary>
        bool CanCreateProject { get; }

        /// <summary>
        /// Gets the command that empties the search and resets both filters.
        /// </summary>
        ReactiveCommand<RxVoid, RxVoid> ClearFiltersCommand { get; }
    }
}
