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

    using Microsoft.Extensions.DependencyInjection;

    using Mycelium.Bloom.Tests.Common;
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

            this.Services.AddSingleton<IProjectBrowserViewModel>(viewModel);

            var component = this.Render<ProjectBrowserComponent>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Markup, Does.Contain("Loading Quantities model"));
                Assert.That(component.Markup, Does.Contain("Preparing the SysML project browser..."));
                Assert.That(component.Markup, Does.Contain("mb-project-browser__state"));
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

            this.Services.AddSingleton<IProjectBrowserViewModel>(viewModel);

            var component = this.Render<ProjectBrowserComponent>();

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
            var node = ProjectBrowserNodeTestFactory.CreateNamespaceNode("quantities", "Quantities");
            var viewModel = new ProjectBrowserViewModelStub
            {
                IsLoaded = true,
                RootNodes = [node]
            };

            this.Services.AddSingleton<IProjectBrowserViewModel>(viewModel);

            var component = this.Render<ProjectBrowserComponent>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Markup, Does.Contain("Quantities"));
                Assert.That(component.Find(".mb-project-browser__tree").GetAttribute("role"), Is.EqualTo("tree"));
                Assert.That(component.Markup, Does.Not.Contain("Loading Quantities model"));
                Assert.That(viewModel.InitializeAsyncCallCount, Is.Zero);
            }
        }

        /// <summary>
        /// Verifies that the project browser initializes an unloaded view model.
        /// </summary>
        [Test]
        public void VerifyOnInitializedAsyncInitializesViewModel()
        {
            ProjectBrowserNodeViewModel selectedNode = null;
            var node = ProjectBrowserNodeTestFactory.CreateNamespaceNode("quantities", "Quantities");
            var viewModel = new ProjectBrowserViewModelStub();

            viewModel.InitializeHandler = () =>
            {
                viewModel.RootNodes = [node];
                viewModel.IsLoaded = true;
                viewModel.SelectNode(node);

                return Task.CompletedTask;
            };

            this.Services.AddSingleton<IProjectBrowserViewModel>(viewModel);

            var component = this.Render<ProjectBrowserComponent>(parameters => parameters
                .Add(browser => browser.SelectedNodeChanged, changedNode =>
                {
                    selectedNode = changedNode;

                    return Task.CompletedTask;
                }));

            component.WaitForAssertion(() => Assert.That(viewModel.InitializeAsyncCallCount, Is.EqualTo(1)));
            component.WaitForAssertion(() => Assert.That(component.Markup, Does.Contain("Quantities")));

            Assert.That(selectedNode, Is.SameAs(node));
        }

        /// <summary>
        /// Verifies that selecting a parent node expands it and marks it as selected.
        /// </summary>
        [Test]
        public void VerifyHandleNodeSelectedSelectsAndExpandsParentNode()
        {
            ProjectBrowserNodeViewModel selectedNode = null;
            var child = ProjectBrowserNodeTestFactory.CreateNamespaceNode("quantities/length", "Length");
            var node = ProjectBrowserNodeTestFactory.CreateNamespaceNode(
                "quantities",
                "Quantities",
                child);

            var viewModel = new ProjectBrowserViewModelStub
            {
                IsLoaded = true,
                RootNodes = [node]
            };

            this.Services.AddSingleton<IProjectBrowserViewModel>(viewModel);

            var component = this.Render<ProjectBrowserComponent>(parameters => parameters
                .Add(browser => browser.SelectedNodeChanged, changedNode =>
                {
                    selectedNode = changedNode;

                    return Task.CompletedTask;
                }));

            component.Find("button").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(node.IsExpanded, Is.True);
                Assert.That(node.IsSelected, Is.True);
                Assert.That(selectedNode, Is.SameAs(node));
                Assert.That(component.Markup, Does.Contain("Length"));
            }
        }

        private sealed class ProjectBrowserViewModelStub : IProjectBrowserViewModel
        {
            /// <inheritdoc />
            public IReadOnlyList<ProjectBrowserNodeViewModel> RootNodes { get; set; } = [];

            /// <inheritdoc />
            public ProjectBrowserNodeViewModel SelectedNode { get; private set; }

            /// <inheritdoc />
            public bool IsLoading { get; set; }

            /// <inheritdoc />
            public bool IsLoaded { get; set; }

            /// <inheritdoc />
            public string ErrorMessage { get; set; } = string.Empty;

            /// <summary>
            /// Gets the number of times asynchronous initialization was requested.
            /// </summary>
            public int InitializeAsyncCallCount { get; private set; }

            /// <summary>
            /// Gets or sets the handler invoked during asynchronous initialization.
            /// </summary>
            public Func<Task> InitializeHandler { get; set; } = () => Task.CompletedTask;

            /// <inheritdoc />
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

            /// <inheritdoc />
            public void Initialize(INamespace model)
            {
                this.IsLoaded = true;
            }

            /// <inheritdoc />
            public void ToggleNode(ProjectBrowserNodeViewModel node)
            {
                node.IsExpanded = !node.IsExpanded;
            }

            /// <inheritdoc />
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
