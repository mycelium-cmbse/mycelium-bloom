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
    using SysML2.NET.Core.POCO.Root.Namespaces;

    /// <summary>
    /// Defines the state and operations required by the project browser tree.
    /// </summary>
    public interface IProjectBrowserViewModel
    {
        /// <summary>
        /// Gets the root nodes displayed by the project browser.
        /// </summary>
        IReadOnlyList<ProjectBrowserNodeViewModel> RootNodes { get; }

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
        /// <returns>A task representing the asynchronous operation.</returns>
        Task InitializeAsync();

        /// <summary>
        /// Initializes the project browser tree from the provided SysML namespace.
        /// </summary>
        /// <param name="model">The loaded SysML namespace model.</param>
        void Initialize(INamespace model);

        /// <summary>
        /// Toggles the expanded state of the provided node.
        /// </summary>
        /// <param name="node">The node to expand or collapse.</param>
        void ToggleNode(ProjectBrowserNodeViewModel node);

        /// <summary>
        /// Selects the provided node and clears the previous selection.
        /// </summary>
        /// <param name="node">The node to select.</param>
        void SelectNode(ProjectBrowserNodeViewModel node);
    }
}
