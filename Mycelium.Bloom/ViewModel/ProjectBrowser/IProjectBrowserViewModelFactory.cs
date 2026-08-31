// ------------------------------------------------------------------------------------------------
// <copyright file="IProjectBrowserViewModelFactory.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.ProjectBrowser
{
    /// <summary>
    /// Creates independently owned Project Browser ViewModels.
    /// </summary>
    public interface IProjectBrowserViewModelFactory
    {
        /// <summary>
        /// Creates a fresh Project Browser ViewModel for one logical editor tab.
        /// </summary>
        /// <returns>The newly created caller-owned ViewModel.</returns>
        IProjectBrowserViewModel Create();
    }
}
