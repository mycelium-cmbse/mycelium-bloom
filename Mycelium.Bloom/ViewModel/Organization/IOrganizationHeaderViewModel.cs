// ------------------------------------------------------------------------------------------------
// <copyright file="IOrganizationHeaderViewModel.cs" company="Starion Group S.A.">
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

    using ReactiveUI;

    /// <summary>
    /// Defines the reactive state and the operations of the header of the account and organization pages.
    /// </summary>
    public interface IOrganizationHeaderViewModel : IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// Gets what the header displays. It follows the organization context and never shows a stale organization.
        /// </summary>
        OrganizationHeaderPresentation Presentation { get; }

        /// <summary>
        /// Gets the command that turns the selection of a menu entry into the route to navigate to.
        /// </summary>
        ReactiveCommand<ActionMenuItem, string> SelectItemCommand { get; }
    }
}
