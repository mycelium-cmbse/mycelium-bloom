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
        private const string WorkspaceGroupKey = "workspace";

        private static readonly string[] ExpectedIds =
            ["model", "views", "engineering", "process"];

        private static readonly string[] ExpectedLabels =
            ["Model", "Views", "Engineering", "Process"];

        private static readonly string[] ExpectedIconNames =
            ["boxes", "panels-top-left", "wrench", "workflow"];

        private static readonly string[] ExpectedGroupKeys =
            [WorkspaceGroupKey, WorkspaceGroupKey, WorkspaceGroupKey, WorkspaceGroupKey];

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
