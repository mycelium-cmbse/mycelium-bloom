// ------------------------------------------------------------------------------------------------
// <copyright file="INavigationRailItemProvider.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.NavigationRail
{
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>
    /// Provides the available navigation destinations for the current application context.
    /// </summary>
    public interface INavigationRailItemProvider
    {
        /// <summary>
        /// Gets the complete destination inventory for the provided contextual values.
        /// </summary>
        /// <param name="lifecycleState">The current project lifecycle state.</param>
        /// <param name="selectedElement">The currently selected model element, or <see langword="null" />.</param>
        /// <returns>The navigation destinations in display order.</returns>
        IReadOnlyList<NavigationRailItem> GetNavigationItems(
            ProjectLifecycleState lifecycleState,
            IElement selectedElement);
    }
}
