// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.ProjectBrowser
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Bunit;

    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using SysML2.NET.Core.POCO.Root.Namespaces;

    using ProjectBrowserComponent = Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser.ProjectBrowser;

    /// <summary>
    /// Tests the <see cref="ProjectBrowserComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ProjectBrowserTestFixture : BunitContext
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
        /// Verifies that the project browser renders a loading state while the view model is loading.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysLoadingState()
        {
            var viewModel = new ProjectBrowserViewModelStub
            {
                IsLoading = true
            };

            var component = this.Render<ProjectBrowserComponent>(parameters => parameters
                .Add(browser => browser.ViewModel, viewModel));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Markup, Does.Contain("Loading Quantities model"));
                Assert.That(component.Markup, Does.Contain("Preparing the SysML project browser..."));
                Assert.That(component.Markup, Does.Contain("mb-loading-state"));
                Assert.That(viewModel.InitializeAsyncCallCount, Is.Zero);
            }
        }

        /// <summary>
        /// Verifies that the project browser renders a compact error state when loading fails.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysErrorState()
        {
            var viewModel = new ProjectBrowserViewModelStub
            {
                ErrorMessage = "Model load failed"
            };

            var component = this.Render<ProjectBrowserComponent>(parameters => parameters
                .Add(browser => browser.ViewModel, viewModel));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Markup, Does.Contain("Unable to load project browser"));
                Assert.That(component.Markup, Does.Contain("Model load failed"));
                Assert.That(component.Find("[role='alert']"), Is.Not.Null);
                Assert.That(viewModel.InitializeAsyncCallCount, Is.Zero);
            }
        }

        /// <summary>
        /// Verifies that the project browser renders tree nodes when the view model has loaded.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysLoadedTree()
        {
            var node = CreateNode("quantities", "Quantities");
            var viewModel = new ProjectBrowserViewModelStub
            {
                IsLoaded = true,
                RootNodes = [node]
            };

            var component = this.Render<ProjectBrowserComponent>(parameters => parameters
                .Add(browser => browser.ViewModel, viewModel));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Markup, Does.Contain("Quantities"));
                Assert.That(component.Find(".mb-project-browser__tree").GetAttribute("role"), Is.EqualTo("tree"));
                Assert.That(component.Markup, Does.Not.Contain("Loading Quantities model"));
                Assert.That(viewModel.InitializeAsyncCallCount, Is.Zero);
            }
        }

        /// <summary>
        /// Verifies that the project browser initializes an unloaded view model after first render.
        /// </summary>
        [Test]
        public void VerifyOnAfterRenderAsyncInitializesViewModel()
        {
            ProjectBrowserNodeViewModel selectedNode = null;
            var node = CreateNode("quantities", "Quantities");
            var viewModel = new ProjectBrowserViewModelStub();

            viewModel.InitializeHandler = () =>
            {
                viewModel.RootNodes = [node];
                viewModel.IsLoaded = true;
                viewModel.SelectNode(node);

                return Task.CompletedTask;
            };

            var component = this.Render<ProjectBrowserComponent>(parameters => parameters
                .Add(browser => browser.ViewModel, viewModel)
                .Add(browser => browser.SelectedNodeChanged, changedNode =>
                {
                    selectedNode = changedNode;

                    return Task.CompletedTask;
                }));

            component.WaitForAssertion(() => Assert.That(viewModel.InitializeAsyncCallCount, Is.EqualTo(1)));
            component.WaitForAssertion(() => Assert.That(component.Markup, Does.Contain("Quantities")));

            Assert.That(selectedNode, Is.SameAs(node));
        }

        private static ProjectBrowserNodeViewModel CreateNode(string nodeId, string displayName)
        {
            return new ProjectBrowserNodeViewModel(
                nodeId,
                displayName,
                new ProjectBrowserNodeMetadata(
                    nodeId,
                    displayName,
                    "Namespace",
                    ProjectBrowserElementKind.Namespace,
                    new Namespace()),
                []);
        }

        private sealed class ProjectBrowserViewModelStub : IProjectBrowserViewModel
        {
            public IReadOnlyList<ProjectBrowserNodeViewModel> RootNodes { get; set; } = [];

            public ProjectBrowserNodeViewModel SelectedNode { get; private set; }

            public bool IsLoading { get; set; }

            public bool IsLoaded { get; set; }

            public string ErrorMessage { get; set; } = string.Empty;

            public int InitializeAsyncCallCount { get; private set; }

            public Func<Task> InitializeHandler { get; set; } = () => Task.CompletedTask;

            public async Task InitializeAsync()
            {
                this.InitializeAsyncCallCount++;
                this.IsLoading = true;

                try
                {
                    await this.InitializeHandler();
                }
                finally
                {
                    this.IsLoading = false;
                }
            }

            public void Initialize(INamespace model)
            {
                this.IsLoaded = true;
            }

            public void ToggleNode(ProjectBrowserNodeViewModel node)
            {
                node.IsExpanded = !node.IsExpanded;
            }

            public void SelectNode(ProjectBrowserNodeViewModel node)
            {
                if (this.SelectedNode != null)
                {
                    this.SelectedNode.IsSelected = false;
                }

                node.IsSelected = true;
                this.SelectedNode = node;
            }
        }
    }
}
