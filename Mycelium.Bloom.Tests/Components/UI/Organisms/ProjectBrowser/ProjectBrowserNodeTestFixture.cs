// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserNodeTestFixture.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.ProjectBrowser
{
    using Bunit;

    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using SysML2.NET.Core.POCO.Kernel.Packages;
    using SysML2.NET.Core.POCO.Root.Namespaces;

    using ProjectBrowserNodeComponent = Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser.ProjectBrowserNode;

    /// <summary>
    /// Tests the <see cref="ProjectBrowserNodeComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ProjectBrowserNodeTestFixture : BunitContext
    {
        /// <summary>
        /// Disposes the bUnit test context after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            this.Dispose();
        }

        /// <summary>
        /// Verifies that the concrete SDK runtime type is used as the visible type label.
        /// </summary>
        [Test]
        public void VerifyRenderUsesRuntimeTypeNameAsVisibleTypeLabel()
        {
            var node = new ProjectBrowserNodeViewModel(
                "quantities",
                "Quantities",
                new ProjectBrowserNodeMetadata(
                    "quantities",
                    "Quantities",
                    "LibraryPackage",
                    ProjectBrowserElementKind.Unknown,
                    new LibraryPackage()),
                []);

            var component = this.Render<ProjectBrowserNodeComponent>(parameters => parameters
                .Add(component => component.ViewModel, node));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Markup, Does.Contain("LibraryPackage"));
                Assert.That(component.Markup, Does.Not.Contain("Unknown LibraryPackage"));
                Assert.That(component.Find("button").GetAttribute("title"), Does.Contain("LibraryPackage"));
                Assert.That(component.Find("button").GetAttribute("title"), Does.Not.Contain("Unknown"));
            }
        }

        /// <summary>
        /// Verifies that selecting a parent node raises the selected node callback.
        /// </summary>
        [Test]
        public void VerifyRenderRaisesSelectedNode()
        {
            var child = new ProjectBrowserNodeViewModel(
                "quantities/length",
                "Length",
                new ProjectBrowserNodeMetadata(
                    "length",
                    "Quantities::Length",
                    "Namespace",
                    ProjectBrowserElementKind.Namespace,
                    new Namespace()),
                []);

            var node = new ProjectBrowserNodeViewModel(
                "quantities",
                "Quantities",
                new ProjectBrowserNodeMetadata(
                    "quantities",
                    "Quantities",
                    "Namespace",
                    ProjectBrowserElementKind.Namespace,
                    new Namespace()),
                [child]);

            var selectedNode = default(ProjectBrowserNodeViewModel);

            var component = this.Render<ProjectBrowserNodeComponent>(parameters => parameters
                .Add(component => component.ViewModel, node)
                .Add(component => component.OnNodeSelected, selected => selectedNode = selected));

            component.Find("button").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(node.IsExpanded, Is.False);
                Assert.That(node.IsSelected, Is.False);
                Assert.That(selectedNode, Is.SameAs(node));
                Assert.That(component.Markup, Does.Not.Contain("Length"));
            }
        }
    }
}
