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
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using SysML2.NET.Core.POCO.Kernel.Packages;
    using SysML2.NET.Core.POCO.Root.Elements;
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
        /// Verifies that the home page displays the expected workspace content and null selection fallback.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysHomeContentAndNullSelection()
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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Markup, Does.Contain("Project Browser"));
                Assert.That(component.Markup, Does.Contain("Quantities model"));
                Assert.That(component.Markup, Does.Contain("Loading Quantities model"));
                Assert.That(component.Markup, Does.Contain("Selected element"));
                Assert.That(component.Find("main h2").TextContent.Trim(), Is.EqualTo("None"));
                Assert.That(component.Find(".mb-project-browser"), Is.Not.Null);
                Assert.That(component.Instance, Is.AssignableTo<ComponentBase>());
                projectBrowserViewModel.Verify(
                    x => x.InitializeAsync(It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        /// <summary>
        /// Verifies the selected-element display-name precedence implemented by Home.
        /// </summary>
        /// <param name="declaredName">The declared name.</param>
        /// <param name="name">The effective name.</param>
        /// <param name="qualifiedName">The qualified name.</param>
        /// <param name="expectedName">The expected displayed name.</param>
        [TestCase("Declared", " ", " ", "Declared", TestName = "VerifySelectedElementDisplaysDeclaredName")]
        [TestCase(" ", "Name", "Qualified", "Name", TestName = "VerifySelectedElementUsesNameFallback")]
        [TestCase(" ", " ", "Qualified", "Qualified", TestName = "VerifySelectedElementUsesQualifiedNameFallback")]
        [TestCase("Declared", "Name", "Qualified", "Declared", TestName = "VerifySelectedElementNamePrecedence")]
        public void VerifySelectedElementDisplayName(
            string declaredName,
            string name,
            string qualifiedName,
            string expectedName)
        {
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel>();
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var projectBrowserViewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            projectBrowserViewModel.SetupGet(x => x.RootNodes).Returns(roots);
            projectBrowserViewModel.SetupGet(x => x.IsLoaded).Returns(false);
            projectBrowserViewModel.SetupGet(x => x.IsLoading).Returns(true);
            projectBrowserViewModel.Setup(x => x.Dispose());
            var selectionService = new ElementSelectionService
            {
                SelectedElement = CreateElement(declaredName, name, qualifiedName)
            };
            this.RegisterServices(projectBrowserViewModel.Object, selectionService);

            using var component = this.Render<Home>();

            Assert.That(component.Find("main h2").TextContent.Trim(), Is.EqualTo(expectedName));
        }

        /// <summary>
        /// Verifies Home uses the runtime type when the selected element has no display name.
        /// </summary>
        [Test]
        public void VerifySelectedElementUsesRuntimeTypeFallback()
        {
            var mutableRoots = new ObservableCollection<ProjectBrowserNodeViewModel>();
            var roots = new ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>(mutableRoots);
            var projectBrowserViewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);
            projectBrowserViewModel.SetupGet(x => x.RootNodes).Returns(roots);
            projectBrowserViewModel.SetupGet(x => x.IsLoaded).Returns(false);
            projectBrowserViewModel.SetupGet(x => x.IsLoading).Returns(true);
            projectBrowserViewModel.Setup(x => x.Dispose());
            var selectionService = new ElementSelectionService
            {
                SelectedElement = new Namespace { DeclaredName = " " }
            };
            this.RegisterServices(projectBrowserViewModel.Object, selectionService);

            using var component = this.Render<Home>();

            Assert.That(component.Find("main h2").TextContent.Trim(), Is.EqualTo(nameof(Namespace)));
        }

        /// <summary>
        /// Verifies the Project Browser child boundary updates Home through the shared scoped service.
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
                Assert.That(component.Find("main h2").TextContent.Trim(), Is.EqualTo(nameof(Namespace))));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectionService.SelectedElement, Is.SameAs(node.SourceElement));
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

            selectionService.SelectedElement = new LibraryPackage();

            component.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.Find("main h2").TextContent.Trim(), Is.EqualTo(nameof(LibraryPackage)));
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

            Assert.That(component.Instance.ElementSelectionService, Is.SameAs(selectionService));
            Assert.That(component.Find("main h2").TextContent.Trim(), Is.EqualTo("First"));

            selectionService.SelectedElement = secondElement;
            component.WaitForAssertion(() =>
                Assert.That(component.Find("main h2").TextContent.Trim(), Is.EqualTo("Second")));

            selectionService.SelectedElement = null;
            component.WaitForAssertion(() =>
                Assert.That(component.Find("main h2").TextContent.Trim(), Is.EqualTo("None")));
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
