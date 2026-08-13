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
        /// Gets the selected destination identifier.
        /// </summary>
        string SelectedItemId { get; }

        /// <summary>
        /// Gets the configured rail presentation mode.
        /// </summary>
        NavigationRailPresentationMode PresentationMode { get; }

        /// <summary>
        /// Gets a value indicating whether the rail currently uses its icon-first presentation.
        /// </summary>
        bool IsCollapsed { get; }

        /// <summary>
        /// Selects an available destination by its stable identifier.
        /// </summary>
        /// <param name="itemId">The destination identifier.</param>
        void SelectItem(string itemId);

        /// <summary>
        /// Switches between the fixed expanded and collapsed modes.
        /// </summary>
        void TogglePresentation();

        /// <summary>
        /// Applies a presentation mode and clears any temporary hover expansion.
        /// </summary>
        /// <param name="mode">The presentation mode to apply.</param>
        void SetPresentationMode(NavigationRailPresentationMode mode);

        /// <summary>
        /// Applies the pointer-enter transition for hover expansion.
        /// </summary>
        void HandlePointerEntered();

        /// <summary>
        /// Applies the pointer-leave transition for hover expansion.
        /// </summary>
        void HandlePointerExited();
    }
}
