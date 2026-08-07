// ------------------------------------------------------------------------------------------------
// <copyright file="IProjectBrowserViewModel.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.ProjectBrowser
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;

    /// <summary>
    /// Defines the state and operations required by the project browser tree.
    /// </summary>
    public interface IProjectBrowserViewModel : IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// Gets the root nodes displayed by the project browser.
        /// </summary>
        ReadOnlyObservableCollection<ProjectBrowserNodeViewModel> RootNodes { get; }

        /// <summary>
        /// Gets the currently selected node.
        /// </summary>
        ProjectBrowserNodeViewModel SelectedNode { get; }

        /// <summary>
        /// Gets a value indicating whether the project browser is loading.
        /// </summary>
        bool IsLoading { get; }

        /// <summary>
        /// Gets a value indicating whether the project browser has loaded.
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        /// Gets the project browser loading error message.
        /// </summary>
        string ErrorMessage { get; }

        /// <summary>
        /// Initializes the project browser tree from the Quantities SysML model.
        /// </summary>
        /// <param name="cancellationToken">Cancels initialization.</param>
        /// <returns><see langword="true" /> when a new tree is loaded; otherwise, <see langword="false" />.</returns>
        Task<bool> InitializeAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Toggles a project browser node.
        /// </summary>
        /// <param name="node">The node to expand or collapse.</param>
        void ToggleNode(ProjectBrowserNodeViewModel node);

        /// <summary>
        /// Selects a project browser node.
        /// </summary>
        /// <param name="node">The node to select.</param>
        void SelectNode(ProjectBrowserNodeViewModel node);
    }
}
