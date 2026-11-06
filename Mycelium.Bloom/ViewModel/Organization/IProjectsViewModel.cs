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

    using DynamicData;

    using ErrorOr;

    /// <summary>
    /// Holds the projects of the organization of the projects page, the most recently updated first, the failure to load them
    /// and whether the ViewModel is loading them. The view decides what to show from these.
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
        /// Gets every project of the organization, which a later ViewModel can subscribe to or add the created project to.
        /// </summary>
        ISourceList<ProjectRowViewModel> Projects { get; }

        /// <summary>
        /// Gets the projects as rows, the most recently updated first.
        /// </summary>
        ReadOnlyObservableCollection<ProjectRowViewModel> Rows { get; }

        /// <summary>
        /// Gets a value indicating whether the current account may create a project.
        /// </summary>
        bool CanCreateProject { get; }
    }
}
