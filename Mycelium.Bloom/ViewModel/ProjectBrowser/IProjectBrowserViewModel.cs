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

    using ReactiveUI;
    using ReactiveUI.Primitives;

    /// <summary>
    /// Defines the state and operations required by the project browser tree.
    /// </summary>
    public interface IProjectBrowserViewModel : INotifyPropertyChanged, IActivatableViewModel, IDisposable
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
        /// Gets the command that initializes the project browser tree from the Quantities SysML model.
        /// </summary>
        ReactiveCommand<RxVoid, bool> InitializeCommand { get; }

        /// <summary>
        /// Gets the command that toggles a project browser node.
        /// </summary>
        ReactiveCommand<ProjectBrowserNodeViewModel, RxVoid> ToggleNodeCommand { get; }

        /// <summary>
        /// Gets the command that selects a project browser node.
        /// </summary>
        ReactiveCommand<ProjectBrowserNodeViewModel, RxVoid> SelectNodeCommand { get; }
    }
}
