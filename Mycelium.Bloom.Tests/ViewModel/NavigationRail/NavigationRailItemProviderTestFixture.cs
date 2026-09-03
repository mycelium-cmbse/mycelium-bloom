// ------------------------------------------------------------------------------------------------
// <copyright file="NavigationRailItemProviderTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.NavigationRail
{
    using System.Linq;

    using BlazorBlueprint.Icons.Lucide.Data;

    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.NavigationRail;

    using SysML2.NET.Core.POCO.Root.Namespaces;

    [TestFixture]
    public sealed class NavigationRailItemProviderTestFixture
    {
        private static readonly string[] ExpectedIds =
        [
            "modelling",
            "part-browser",
            "glossary",
            "reference-data",
            "reporting",
            "relationship-matrix",
            "requirements",
            "cases",
            "3d-view",
            "variants",
            "validation",
            "version-history",
            "reviews",
            "publication",
            "dashboard",
            "settings"
        ];

        private static readonly string[] ExpectedLabels =
        [
            "Modelling",
            "Part Browser",
            "Glossary",
            "Reference data",
            "Reporting",
            "Relationship Matrix",
            "Requirements",
            "Cases",
            "3D view",
            "Variants",
            "Validation",
            "Version History",
            "Reviews",
            "Publication",
            "Dashboard",
            "Settings"
        ];

        private static readonly string[] ExpectedIconNames =
        [
            "list-tree",
            "panels-top-left",
            "book-open",
            "database",
            "code",
            "table-2",
            "file-check-2",
            "clipboard-check",
            "box",
            "git-branch",
            "circle-check",
            "history",
            "eye",
            "upload",
            "layout-dashboard",
            "settings"
        ];

        private static readonly string[] ExpectedGroupKeys =
        [
            "modelling",
            "modelling",
            "modelling",
            "modelling",
            "views",
            "views",
            "engineering",
            "engineering",
            "engineering",
            "engineering",
            "engineering",
            "process",
            "process",
            "process",
            "utility",
            "utility"
        ];

        private static readonly string[] ExpectedGroupLabels =
        [
            "",
            "",
            "",
            "",
            "VIEWS",
            "",
            "ENGINEERING",
            "",
            "",
            "",
            "",
            "PROCESS",
            "",
            "",
            "",
            ""
        ];

        private static readonly string[] ExpectedHrefs =
        [
            "/workspace/modeling",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "/workspace/dashboard",
            null
        ];

        [Test]
        public void VerifyGetNavigationItemsReturnsExpectedInventory()
        {
            var provider = new NavigationRailItemProvider();
            var navigationItems = provider.GetNavigationItems(ProjectLifecycleState.Preparation, null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(navigationItems, Has.Count.EqualTo(ExpectedIds.Length));
                Assert.That(navigationItems.Select(item => item.Id), Is.EqualTo(ExpectedIds));
                Assert.That(navigationItems.Select(item => item.Label), Is.EqualTo(ExpectedLabels));
                Assert.That(navigationItems.Select(item => item.IconName), Is.EqualTo(ExpectedIconNames));
                Assert.That(navigationItems.Select(item => item.GroupKey), Is.EqualTo(ExpectedGroupKeys));
                Assert.That(navigationItems.Select(item => item.GroupLabel), Is.EqualTo(ExpectedGroupLabels));
                Assert.That(navigationItems.Select(item => item.Href), Is.EqualTo(ExpectedHrefs));
                Assert.That(navigationItems.Count(item => item.Href is not null), Is.EqualTo(2));
                Assert.That(navigationItems.Select(item => item.Id).Distinct().Count(), Is.EqualTo(ExpectedIds.Length));
                Assert.That(navigationItems.All(item => !string.IsNullOrWhiteSpace(item.IconName)), Is.True);
                Assert.That(navigationItems.All(item => LucideIconData.IconExists(item.IconName)), Is.True);
            }
        }

        [TestCase(ProjectLifecycleState.Preparation)]
        [TestCase(ProjectLifecycleState.Open)]
        [TestCase(ProjectLifecycleState.Review)]
        [TestCase(ProjectLifecycleState.Archived)]
        public void VerifyGetNavigationItemsIsInvariantAcrossLifecycleStates(
            ProjectLifecycleState lifecycleState)
        {
            var provider = new NavigationRailItemProvider();
            var expectedItems = provider.GetNavigationItems(ProjectLifecycleState.Preparation, null);

            Assert.That(provider.GetNavigationItems(lifecycleState, null), Is.SameAs(expectedItems));
        }

        [Test]
        public void VerifyGetNavigationItemsIsInvariantAcrossSelectedElements()
        {
            var provider = new NavigationRailItemProvider();
            var expectedItems = provider.GetNavigationItems(ProjectLifecycleState.Open, null);

            Assert.That(
                provider.GetNavigationItems(ProjectLifecycleState.Open, new Namespace()),
                Is.SameAs(expectedItems));
        }
    }
}
