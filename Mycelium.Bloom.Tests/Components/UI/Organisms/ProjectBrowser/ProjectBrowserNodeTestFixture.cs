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
    using System.Collections.Generic;
    using System.Threading.Tasks;

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
        public void Render_UsesRuntimeTypeNameAsVisibleTypeLabel()
        {
            var node = new ProjectBrowserNodeViewModel(
                "quantities",
                "quantities",
                "Quantities",
                "Quantities",
                "LibraryPackage",
                ProjectBrowserElementKind.Unknown,
                [],
                new LibraryPackage());

            var component = this.Render<ProjectBrowserNodeComponent>(parameters => parameters
                .Add(component => component.Node, node)
                .Add(component => component.ViewModel, new ProjectBrowserViewModelStub()));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Markup, Does.Contain("«LibraryPackage»"));
                Assert.That(component.Markup, Does.Not.Contain("Unknown LibraryPackage"));
                Assert.That(component.Find("button").GetAttribute("title"), Does.Contain("LibraryPackage"));
                Assert.That(component.Find("button").GetAttribute("title"), Does.Not.Contain("Unknown"));
            }
        }

        private sealed class ProjectBrowserViewModelStub : IProjectBrowserViewModel
        {
            public IReadOnlyList<ProjectBrowserNodeViewModel> RootNodes { get; } = [];

            public ProjectBrowserNodeViewModel SelectedNode { get; private set; }

            public bool IsLoading { get; }

            public bool IsLoaded { get; }

            public string ErrorMessage { get; } = string.Empty;

            public Task InitializeAsync()
            {
                return Task.CompletedTask;
            }

            public void Initialize(INamespace model)
            {
            }

            public void ToggleNode(ProjectBrowserNodeViewModel node)
            {
                node.IsExpanded = !node.IsExpanded;
            }

            public void SelectNode(ProjectBrowserNodeViewModel node)
            {
                this.SelectedNode = node;
            }
        }
    }
}
