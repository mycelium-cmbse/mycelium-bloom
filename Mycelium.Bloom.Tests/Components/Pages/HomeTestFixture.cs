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
    using System.Collections.ObjectModel;
    using System.Threading;

    using Bunit;

    using Microsoft.Extensions.DependencyInjection;

    using Moq;

    using Mycelium.Bloom.Components.Pages;
    using Mycelium.Bloom.Components.UI.Organisms.DetailsPanel;
    using Mycelium.Bloom.Components.UI.Organisms.WorkspaceShell;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using SysML2.NET.Core.POCO.Kernel.Packages;

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
        /// Verifies Home composes the workspace regions and displays an empty details panel.
        /// </summary>
        [Test]
        public void VerifyRenderComposesWorkspaceShellWithEmptyDetailsPanel()
        {
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel>();
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var projectBrowserViewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            projectBrowserViewModel.SetupGet(x => x.RootNodes).Returns(roots);
            projectBrowserViewModel.SetupGet(x => x.IsLoaded).Returns(false);
            projectBrowserViewModel.SetupGet(x => x.IsLoading).Returns(true);
            projectBrowserViewModel.Setup(x => x.Dispose());
            var selectionService = this.RegisterServices(projectBrowserViewModel.Object);

            using var component = this.Render<Home>();
            var detailsPanel = component.FindComponent<DetailsPanel>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindComponents<WorkspaceShell>(), Has.Count.EqualTo(1));
                Assert.That(component.Find("aside.mb-workspace-shell__left-panel").TextContent,
                    Does.Contain("Project Browser"));
                Assert.That(component.Find("aside.mb-workspace-shell__left-panel").TextContent,
                    Does.Contain("Quantities model"));
                Assert.That(component.Find(".mb-project-browser"), Is.Not.Null);
                Assert.That(component.Find(".mb-workspace-shell__main").TextContent, Does.Contain("Workspace"));
                Assert.That(component.Find("aside.mb-workspace-shell__right-panel .mb-details-panel"), Is.Not.Null);
                Assert.That(detailsPanel.Instance.ViewModel, Is.SameAs(selectionService));
                Assert.That(selectionService.SelectedElement, Is.Null);
                Assert.That(detailsPanel.Find(".mb-details-panel__empty").TextContent.Trim(),
                    Is.EqualTo("Select an element to display its details."));
                projectBrowserViewModel.Verify(
                    x => x.InitializeAsync(It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        /// <summary>
        /// Verifies the Project Browser child boundary updates the details panel through the real selection service.
        /// </summary>
        [Test]
        public void VerifyProjectBrowserSelectionUpdatesHome()
        {
            var selectionService = new ContextAwareService();
            var node = ProjectBrowserNodeTestFactory.CreateNamespaceNode("quantities", "Quantities");
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel> { node };
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var projectBrowserViewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            projectBrowserViewModel.SetupGet(x => x.RootNodes).Returns(roots);
            projectBrowserViewModel.SetupGet(x => x.IsLoaded).Returns(true);
            projectBrowserViewModel.SetupGet(x => x.IsLoading).Returns(false);
            projectBrowserViewModel.SetupGet(x => x.ErrorMessage).Returns(string.Empty);
            projectBrowserViewModel
                .Setup(x => x.SelectNode(node))
                .Callback<ProjectBrowserNodeViewModel>(selectedNode =>
                    selectionService.SelectedElement = selectedNode.SourceElement);
            projectBrowserViewModel.Setup(x => x.Dispose());
            this.RegisterServices(projectBrowserViewModel.Object, selectionService);

            using var component = this.Render<Home>();

            component.Find(".mb-project-browser-node__row").Click();

            component.WaitForAssertion(() =>
                Assert.That(component.FindAll("aside.mb-workspace-shell__right-panel dl"), Has.Count.EqualTo(1)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectionService.SelectedElement, Is.SameAs(node.SourceElement));
                Assert.That(component.Find("aside.mb-workspace-shell__right-panel dl"), Is.Not.Null);
                projectBrowserViewModel.Verify(x => x.SelectNode(node), Times.Once);
                projectBrowserViewModel.Verify(
                    x => x.ToggleNode(It.IsAny<ProjectBrowserNodeViewModel>()),
                    Times.Never);
            }
        }

        /// <summary>
        /// Verifies the DetailsPanel reacts to external selection through the shared service.
        /// </summary>
        [Test]
        public void VerifyExternalSelectionRerendersDetailsPanel()
        {
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel>();
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var projectBrowserViewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            projectBrowserViewModel.SetupGet(x => x.RootNodes).Returns(roots);
            projectBrowserViewModel.SetupGet(x => x.IsLoaded).Returns(false);
            projectBrowserViewModel.SetupGet(x => x.IsLoading).Returns(true);
            projectBrowserViewModel.Setup(x => x.Dispose());
            var selectionService = this.RegisterServices(projectBrowserViewModel.Object);

            using var component = this.Render<Home>();
            var detailsPanel = component.FindComponent<DetailsPanel>();
            var detailsPanelRenderCount = detailsPanel.RenderCount;
            var selectedElement = new LibraryPackage();

            selectionService.SelectedElement = selectedElement;

            detailsPanel.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(detailsPanel.Instance.ViewModel, Is.SameAs(selectionService));
                    Assert.That(detailsPanel.FindAll(".mb-details-panel__empty"), Is.Empty);
                    Assert.That(detailsPanel.RenderCount, Is.GreaterThan(detailsPanelRenderCount));
                }
            });
        }

        /// <summary>
        /// Registers the shared selection service and nested Project Browser boundary for Home component tests.
        /// </summary>
        /// <param name="projectBrowserViewModel">The mocked Project Browser contract.</param>
        /// <param name="selectionService">The shared selection service, when a preconfigured instance is required.</param>
        /// <returns>The selection service registered for the component scope.</returns>
        private IElementSelectionService RegisterServices(
            IProjectBrowserViewModel projectBrowserViewModel,
            IElementSelectionService selectionService = null)
        {
            selectionService ??= new ContextAwareService();

            this.Services.AddSingleton(selectionService);
            this.Services.AddSingleton(projectBrowserViewModel);

            return selectionService;
        }
    }
}
