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

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Defines the reactive state of the organization designated by the current browser location.
    /// </summary>
    public interface IOrganizationContextViewModel : IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// Gets what the current browser location designates.
        /// </summary>
        OrganizationLocation Location { get; }

        /// <summary>
        /// Gets the state of the designated organization.
        /// </summary>
        OrganizationContextState State { get; }

        /// <summary>
        /// Gets the current account, <see langword="null" /> unless the state is <see cref="OrganizationContextState.Ready" />.
        /// </summary>
        Account Account { get; }

        /// <summary>
        /// Gets the designated organization, <see langword="null" /> unless the state is
        /// <see cref="OrganizationContextState.Ready" /> on an organization page.
        /// </summary>
        Organization Organization { get; }

        /// <summary>
        /// Gets what the current account may do in the designated organization, <see langword="null" /> unless the
        /// state is <see cref="OrganizationContextState.Ready" /> on an organization page.
        /// </summary>
        OrganizationCapabilities Capabilities { get; }
    }
}
