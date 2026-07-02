// ------------------------------------------------------------------------------------------------
// <copyright file="IProjectBrowserViewModelService.cs" company="Starion Group S.A.">
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
    /// Defines operations to create project browser view models.
    /// </summary>
    public interface IProjectBrowserViewModelService
    {
        /// <summary>
        /// Creates a project browser view model for the cached Quantities model.
        /// </summary>
        /// <returns>A fresh project browser view model instance.</returns>
        IProjectBrowserViewModel CreateQuantitiesProjectBrowserViewModel();

        /// <summary>
        /// Creates a project browser view model initialized from the provided namespace root.
        /// For future implmentation that requires Models that are different from the Quantities model.
        /// </summary>
        /// <param name="namespaceRoot">The SysML namespace root used to initialize the project browser.</param>
        /// <returns>A fresh project browser view model instance.</returns>
        IProjectBrowserViewModel CreateFromNamespace(INamespace namespaceRoot);
    }
}
