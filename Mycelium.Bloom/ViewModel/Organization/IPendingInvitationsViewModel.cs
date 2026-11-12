// ------------------------------------------------------------------------------------------------
// <copyright file="IPendingInvitationsViewModel.cs" company="Starion Group S.A.">
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
    /// Holds the state of the pending invitations of an organization: whether it is busy, the errors of its load and the rows.
    /// The view shows nothing when it is not busy and has no row.
    /// </summary>
    public interface IPendingInvitationsViewModel : IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// Gets a value indicating whether the ViewModel is loading the invitations.
        /// </summary>
        bool IsBusy { get; }

        /// <summary>
        /// Gets the errors that the load returned, or an empty list when it succeeded.
        /// </summary>
        IReadOnlyList<Error> Errors { get; }

        /// <summary>
        /// Gets the rows of the pending invitations, the most recently sent first.
        /// </summary>
        ReadOnlyObservableCollection<InvitationRowViewModel> Rows { get; }

        /// <summary>
        /// Gets the pending invitations, which the dialog that invites a member can add to, so that an invitation sent joins the
        /// list without a new request.
        /// </summary>
        ISourceList<InvitationRowViewModel> Invitations { get; }
    }
}
