// ------------------------------------------------------------------------------------------------
// <copyright file="HomeTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.Pages
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Bunit;

    using Microsoft.Extensions.DependencyInjection;

    using Mycelium.Bloom.Components.Pages;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using SysML2.NET.Core.POCO.Root.Namespaces;

    /// <summary>
    /// Tests the <see cref="Home" /> page.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class HomeTestFixture : BunitContext
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
        /// Verifies that the home page displays the expected workspace content.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysHomeContent()
        {
            var viewModel = this.RegisterProjectBrowserViewModel();

            var component = this.Render<Home>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Markup, Does.Contain("Project Browser"));
                Assert.That(component.Markup, Does.Contain("Quantities model"));
                Assert.That(component.Markup, Does.Contain("Loading Quantities model"));
                Assert.That(component.Markup, Does.Contain("Selected element"));
                Assert.That(component.Find(".mb-project-browser"), Is.Not.Null);
                Assert.That(viewModel.InitializeAsyncCallCount, Is.Zero);
            }
        }

        private ProjectBrowserViewModelStub RegisterProjectBrowserViewModel()
        {
            var viewModel = new ProjectBrowserViewModelStub
            {
                IsLoading = true
            };

            this.Services.AddSingleton<IProjectBrowserViewModel>(viewModel);

            return viewModel;
        }

        private sealed class ProjectBrowserViewModelStub : IProjectBrowserViewModel
        {
            public IReadOnlyList<ProjectBrowserNodeViewModel> RootNodes { get; } = [];

            public ProjectBrowserNodeViewModel SelectedNode { get; private set; }

            public bool IsLoading { get; set; }

            public bool IsLoaded { get; }

            public string ErrorMessage { get; } = string.Empty;

            public int InitializeAsyncCallCount { get; private set; }

            public Task InitializeAsync()
            {
                this.InitializeAsyncCallCount++;

                return Task.CompletedTask;
            }

            public void Initialize(INamespace model)
            {
            }

            public void ToggleNode(ProjectBrowserNodeViewModel node)
            {
            }

            public void SelectNode(ProjectBrowserNodeViewModel node)
            {
                this.SelectedNode = node;
            }
        }
    }
}
