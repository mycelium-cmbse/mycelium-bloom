// ------------------------------------------------------------------------------------------------
// <copyright file="NavigationRailItemProvider.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.NavigationRail
{
    using System.Collections.ObjectModel;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>
    /// Provides the fixed application workspace navigation inventory.
    /// </summary>
    public sealed class NavigationRailItemProvider : INavigationRailItemProvider
    {
        private const string WorkspaceGroupKey = "workspace";

        private static readonly ReadOnlyCollection<NavigationRailItem> NavigationItems =
            Array.AsReadOnly<NavigationRailItem>(
            [
                new() { Id = "model", Label = "Model", IconName = "boxes", GroupKey = WorkspaceGroupKey },
                new() { Id = "views", Label = "Views", IconName = "panels-top-left", GroupKey = WorkspaceGroupKey },
                new() { Id = "engineering", Label = "Engineering", IconName = "wrench", GroupKey = WorkspaceGroupKey },
                new() { Id = "process", Label = "Process", IconName = "workflow", GroupKey = WorkspaceGroupKey }
            ]);

        /// <inheritdoc />
        public IReadOnlyList<NavigationRailItem> GetNavigationItems(
            ProjectLifecycleState lifecycleState,
            IElement selectedElement)
        {
            return NavigationItems;
        }
    }
}
