// ------------------------------------------------------------------------------------------------
// <copyright file="IMembersViewModel.cs" company="Starion Group S.A.">
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
    /// Holds the state of the members page of an organization: whether it is busy, the errors of its last load and the rows.
    /// The view decides what to show from them and does the searching and the filtering.
    /// </summary>
    public interface IMembersViewModel : IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// Gets a value indicating whether the ViewModel is loading the people.
        /// </summary>
        bool IsBusy { get; }

        /// <summary>
        /// Gets the errors that the last load returned, or an empty list when it succeeded.
        /// </summary>
        IReadOnlyList<Error> Errors { get; }

        /// <summary>
        /// Gets every person that the account may see, which a later ViewModel can subscribe to or add to.
        /// </summary>
        ISourceList<MemberRowViewModel> Members { get; }

        /// <summary>
        /// Gets the rows of the members list.
        /// </summary>
        ReadOnlyObservableCollection<MemberRowViewModel> Rows { get; }

        /// <summary>
        /// Gets a value indicating whether the current account may invite members.
        /// </summary>
        bool CanInviteMembers { get; }

        /// <summary>
        /// Gets a value indicating whether the current account may see the Outside Collaborators of the organization.
        /// </summary>
        bool CanViewOutsideCollaborators { get; }
    }
}
