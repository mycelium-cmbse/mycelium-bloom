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
        /// <summary>
        /// The group key shared by the top-level modelling destinations.
        /// </summary>
        private const string ModellingGroupKey = "modelling";

        /// <summary>
        /// The group key shared by reporting and relationship destinations.
        /// </summary>
        private const string ViewsGroupKey = "views";

        /// <summary>
        /// The group key shared by engineering destinations.
        /// </summary>
        private const string EngineeringGroupKey = "engineering";

        /// <summary>
        /// The group key shared by process destinations.
        /// </summary>
        private const string ProcessGroupKey = "process";

        /// <summary>
        /// The group key shared by utility destinations.
        /// </summary>
        private const string UtilityGroupKey = "utility";

        /// <summary>
        /// The cached read-only workspace destination inventory.
        /// </summary>
        private static readonly ReadOnlyCollection<NavigationRailItem> NavigationItems =
            Array.AsReadOnly<NavigationRailItem>(
            [
                new() { Id = "modelling", Label = "Modelling", IconName = "list-tree", Href = "/workspace/modeling", GroupKey = ModellingGroupKey },
                new() { Id = "part-browser", Label = "Part Browser", IconName = "panels-top-left", GroupKey = ModellingGroupKey },
                new() { Id = "glossary", Label = "Glossary", IconName = "book-open", GroupKey = ModellingGroupKey },
                new() { Id = "reference-data", Label = "Reference data", IconName = "database", GroupKey = ModellingGroupKey },
                new() { Id = "reporting", Label = "Reporting", IconName = "code", GroupKey = ViewsGroupKey, GroupLabel = "VIEWS" },
                new() { Id = "relationship-matrix", Label = "Relationship Matrix", IconName = "table-2", GroupKey = ViewsGroupKey },
                new() { Id = "requirements", Label = "Requirements", IconName = "file-check-2", GroupKey = EngineeringGroupKey, GroupLabel = "ENGINEERING" },
                new() { Id = "cases", Label = "Cases", IconName = "clipboard-check", GroupKey = EngineeringGroupKey },
                new() { Id = "3d-view", Label = "3D view", IconName = "box", GroupKey = EngineeringGroupKey },
                new() { Id = "variants", Label = "Variants", IconName = "git-branch", GroupKey = EngineeringGroupKey },
                new() { Id = "validation", Label = "Validation", IconName = "circle-check", GroupKey = EngineeringGroupKey },
                new() { Id = "version-history", Label = "Version History", IconName = "history", GroupKey = ProcessGroupKey, GroupLabel = "PROCESS" },
                new() { Id = "reviews", Label = "Reviews", IconName = "eye", GroupKey = ProcessGroupKey },
                new() { Id = "publication", Label = "Publication", IconName = "upload", GroupKey = ProcessGroupKey },
                new() { Id = "dashboard", Label = "Dashboard", IconName = "layout-dashboard", Href = "/workspace/dashboard", GroupKey = UtilityGroupKey },
                new() { Id = "settings", Label = "Settings", IconName = "settings", GroupKey = UtilityGroupKey }
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
