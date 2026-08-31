// ------------------------------------------------------------------------------------------------
// <copyright file="INavigationRailViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.NavigationRail
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Defines the reactive state and operations required by a navigation rail.
    /// </summary>
    public interface INavigationRailViewModel : IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// Gets the read-only rendering projection of available destinations.
        /// </summary>
        ReadOnlyObservableCollection<NavigationRailItem> NavigationItems { get; }

        /// <summary>
        /// Gets or sets the selected destination, or <see langword="null" /> when selection is cleared.
        /// </summary>
        NavigationRailItem SelectedItem { get; set; }

        /// <summary>
        /// Gets or sets the configured rail presentation mode.
        /// </summary>
        NavigationRailPresentationMode PresentationMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a persistently collapsed rail expands temporarily while hovered.
        /// </summary>
        bool IsExpandOnHoverEnabled { get; set; }
    }
}
