// ------------------------------------------------------------------------------------------------
// <copyright file="IHomeViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Home
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;

    using DynamicData;

    using ErrorOr;

    /// <summary>
    /// Defines the Home page: the projects of the server of the open connection.
    /// </summary>
    public interface IHomeViewModel : IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// Gets a value indicating whether a connection is open; without one the page goes back to the start page.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Gets a value indicating whether the projects are being loaded.
        /// </summary>
        bool IsBusy { get; }

        /// <summary>
        /// Gets the errors of the last load.
        /// </summary>
        IReadOnlyList<Error> Errors { get; }

        /// <summary>
        /// Gets the projects of the server, so that a later screen can add a created project without another load.
        /// </summary>
        ISourceList<Project> Projects { get; }

        /// <summary>
        /// Gets the projects bound for the view, in the order the server returns them.
        /// </summary>
        ReadOnlyObservableCollection<Project> Rows { get; }
    }
}
