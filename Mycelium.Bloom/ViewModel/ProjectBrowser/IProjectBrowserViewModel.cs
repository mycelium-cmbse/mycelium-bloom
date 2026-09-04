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
    using System.Diagnostics.CodeAnalysis;
    using SysML2.NET.Core.POCO.Root.Elements;

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
        /// Gets or sets the committed Contains criterion used to filter nodes by display name or qualified name.
        /// </summary>
        string FilterText { get; set; }

        /// <summary>
        /// Gets the distinct element types available in the currently loaded model.
        /// </summary>
        ReadOnlyObservableCollection<Type> AvailableElementTypes { get; }

        /// <summary>
        /// Gets the element types selected for filtering. An empty collection includes every type.
        /// </summary>
        ReadOnlyObservableCollection<Type> SelectedElementTypes { get; }

        /// <summary>
        /// Gets the current immutable visibility presentation over the canonical tree.
        /// </summary>
        ProjectBrowserFilterPresentation FilterPresentation { get; }

        /// <summary>
        /// Gets the currently selected node, or <see langword="null" /> when no node is selected.
        /// </summary>
        [MaybeNull]
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
        /// Clears every active project browser filter criterion.
        /// </summary>
        void ClearFilter();

        /// <summary>
        /// Adds or removes an available model element type from the active filter.
        /// </summary>
        /// <param name="elementType">The runtime model element type to toggle.</param>
        void ToggleElementTypeFilter(Type elementType);

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

        /// <summary>
        /// Restores local logical focus from canonical model identity without publishing shared selection.
        /// </summary>
        /// <param name="element">The canonical model element to focus.</param>
        void FocusElement(IElement element);
    }
}
