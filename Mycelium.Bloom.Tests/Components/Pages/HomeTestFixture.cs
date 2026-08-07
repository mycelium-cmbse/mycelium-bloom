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
    using System.Threading.Tasks;

    using Bunit;

    using Microsoft.AspNetCore.Components;
    using Microsoft.Extensions.DependencyInjection;

    using Moq;

    using Mycelium.Bloom.Components.Pages;
    using Mycelium.Bloom.Components.UI.Organisms.DetailsPanel;
    using Mycelium.Bloom.Components.UI.Organisms.WorkspaceShell;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using SysML2.NET.Core.POCO.Kernel.Packages;
    using SysML2.NET.Core.POCO.Root.Elements;

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
            this.RegisterServices(projectBrowserViewModel.Object);

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
                Assert.That(detailsPanel.Instance.Element, Is.Null);
                Assert.That(detailsPanel.Find(".mb-details-panel__empty").TextContent.Trim(),
                    Is.EqualTo("Select an element to display its details."));
                Assert.That(component.Instance, Is.AssignableTo<ComponentBase>());
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
            var selectionService = new ElementSelectionService();
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
                Assert.That(component.FindComponent<DetailsPanel>().Instance.Element, Is.SameAs(node.SourceElement)));

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
        /// Verifies Home reacts renderer-safely to external service selection.
        /// </summary>
        [Test]
        public void VerifyExternalSelectionRerendersHome()
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
            var renderCount = component.RenderCount;
            var selectedElement = new LibraryPackage();

            selectionService.SelectedElement = selectedElement;

            component.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.FindComponent<DetailsPanel>().Instance.Element, Is.SameAs(selectedElement));
                    Assert.That(component.FindAll(".mb-details-panel__empty"), Is.Empty);
                    Assert.That(component.RenderCount, Is.GreaterThan(renderCount));
                }
            });
        }

        /// <summary>
        /// Verifies Home reads the selected element directly from the shared service without retaining stale state.
        /// </summary>
        [Test]
        public void VerifyHomeReadsSelectedElementDirectlyFromService()
        {
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel>();
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var projectBrowserViewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            projectBrowserViewModel.SetupGet(x => x.RootNodes).Returns(roots);
            projectBrowserViewModel.SetupGet(x => x.IsLoaded).Returns(false);
            projectBrowserViewModel.SetupGet(x => x.IsLoading).Returns(true);
            projectBrowserViewModel.Setup(x => x.Dispose());
            var firstElement = CreateElement("First", " ", " ");
            var secondElement = CreateElement("Second", " ", " ");
            var selectionService = new ElementSelectionService { SelectedElement = firstElement };
            this.RegisterServices(projectBrowserViewModel.Object, selectionService);

            using var component = this.Render<Home>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Instance.ElementSelectionService, Is.SameAs(selectionService));
                Assert.That(component.FindComponent<DetailsPanel>().Instance.Element, Is.SameAs(firstElement));
            }

            selectionService.SelectedElement = secondElement;
            component.WaitForAssertion(() =>
                Assert.That(component.FindComponent<DetailsPanel>().Instance.Element, Is.SameAs(secondElement)));

            selectionService.SelectedElement = null;
            component.WaitForAssertion(() =>
            {
                var detailsPanel = component.FindComponent<DetailsPanel>();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(detailsPanel.Instance.Element, Is.Null);
                    Assert.That(detailsPanel.Find(".mb-details-panel__empty").TextContent.Trim(),
                        Is.EqualTo("Select an element to display its details."));
                }
            });
        }

        /// <summary>
        /// Verifies Home disposal removes selection observation and prevents later renders.
        /// </summary>
        [Test]
        public async Task VerifyDisposedHomeIgnoresSelectionChanges()
        {
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel>();
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var projectBrowserViewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            projectBrowserViewModel.SetupGet(x => x.RootNodes).Returns(roots);
            projectBrowserViewModel.SetupGet(x => x.IsLoaded).Returns(false);
            projectBrowserViewModel.SetupGet(x => x.IsLoading).Returns(true);
            projectBrowserViewModel.Setup(x => x.Dispose());
            var selectionService = this.RegisterServices(projectBrowserViewModel.Object);
            var component = this.Render<Home>();
            var home = component.Instance;

            await this.DisposeComponentsAsync();
            home.Dispose();
            var renderCountAfterDisposal = component.RenderCount;

            await this.Renderer.Dispatcher.InvokeAsync(() =>
            {
                selectionService.SelectedElement = new LibraryPackage();
            });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectionService.SelectedElement, Is.TypeOf<LibraryPackage>());
                Assert.That(component.RenderCount, Is.EqualTo(renderCountAfterDisposal));
                projectBrowserViewModel.Verify(x => x.Dispose(), Times.Once);
            }
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
            selectionService ??= new ElementSelectionService();

            this.Services.AddSingleton(selectionService);
            this.Services.AddSingleton(projectBrowserViewModel);

            return selectionService;
        }

        /// <summary>
        /// Creates an element with controlled display-name values.
        /// </summary>
        /// <param name="declaredName">The declared name.</param>
        /// <param name="name">The effective name.</param>
        /// <param name="qualifiedName">The qualified name.</param>
        /// <returns>The configured element.</returns>
        private static IElement CreateElement(string declaredName, string name, string qualifiedName)
        {
            var element = new Mock<IElement>();
            element.SetupGet(x => x.DeclaredName).Returns(declaredName);
            element.SetupGet(x => x.name).Returns(name);
            element.SetupGet(x => x.qualifiedName).Returns(qualifiedName);

            return element.Object;
        }
    }
}
