// ------------------------------------------------------------------------------------------------
// <copyright file="IOrganizationContextViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Organization
{
    using System.ComponentModel;

    using ErrorOr;

    using Mycelium.Bloom.Model;
    using Mycelium.SDK.POCO;

    using Organization = Mycelium.SDK.POCO.Organization;

    /// <summary>
    /// Defines the reactive state of the organization designated by the current browser location.
    /// </summary>
    public interface IOrganizationContextViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets what the current browser location designates. The layout sets it on every navigation and resets it
        /// to <see langword="null" /> when it is disposed, which clears the context.
        /// </summary>
        OrganizationLocation Location { get; set; }

        /// <summary>
        /// Gets a value indicating whether the account or the organization designated by the location is being resolved.
        /// </summary>
        bool IsLoading { get; }

        /// <summary>
        /// Gets the current account, <see langword="null" /> until it is resolved.
        /// </summary>
        User Account { get; }

        /// <summary>
        /// Gets the designated organization, <see langword="null" /> until it is resolved or on the account page.
        /// </summary>
        Organization Organization { get; }

        /// <summary>
        /// Gets what the current account may do in the designated organization, <see langword="null" /> until it is
        /// resolved or on the account page.
        /// </summary>
        OrganizationCapabilities Capabilities { get; }

        /// <summary>
        /// Gets the errors that prevented the resolution, empty while it loads and once it succeeded.
        /// </summary>
        IReadOnlyList<Error> Errors { get; }
    }
}
