// ------------------------------------------------------------------------------------------------
// <copyright file="IConnectViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Session
{
    using System.ComponentModel;

    using ErrorOr;

    using ReactiveUI;
    using ReactiveUI.Primitives;

    /// <summary>
    /// Defines the start page, where the user names the server to connect to.
    /// </summary>
    public interface IConnectViewModel : IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets the address of the server, as the user types it.
        /// </summary>
        string ServerAddress { get; set; }

        /// <summary>
        /// Gets the command that opens the connection to the typed address.
        /// </summary>
        ReactiveCommand<RxVoid, RxVoid> Connect { get; }

        /// <summary>
        /// Gets a value indicating whether a connection is being opened.
        /// </summary>
        bool IsBusy { get; }

        /// <summary>
        /// Gets the errors of the last attempt: an address that is not valid, or a server that cannot be reached.
        /// </summary>
        IReadOnlyList<Error> Errors { get; }

        /// <summary>
        /// Gets a value indicating whether a connection is open, so that the page can leave for the Home page.
        /// </summary>
        bool IsConnected { get; }
    }
}
