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
    using Bunit;

    using Microsoft.Extensions.DependencyInjection;

    using Mycelium.Bloom.Components.Pages;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using ReactiveUI.Blazor;

    using SysML2.NET.Core.POCO.Kernel.Packages;
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
        public void VerifyRenderDisplaysHomeContentWithoutCodeBehind()
        {
            using var context = new ProjectBrowserViewModelMockContext();
            context.SetIsLoading(true);
            this.RegisterServices(context.Object);

            using var component = this.Render<Home>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Markup, Does.Contain("Project Browser"));
                Assert.That(component.Markup, Does.Contain("Quantities model"));
                Assert.That(component.Markup, Does.Contain("Loading Quantities model"));
                Assert.That(component.Markup, Does.Contain("Selected element"));
                Assert.That(component.Markup, Does.Contain("None"));
                Assert.That(component.Find(".mb-project-browser"), Is.Not.Null);
                Assert.That(component.Instance, Is.AssignableTo<ReactiveInjectableComponentBase<HomeViewModel>>());
                Assert.That(context.InitializeCallCount, Is.Zero);
            }
        }

        /// <summary>
        /// Verifies the Project Browser child boundary updates Home through the shared scoped service.
        /// </summary>
        [Test]
        public void VerifyProjectBrowserSelectionUpdatesHome()
        {
            var selectionService = new ElementSelectionService();
            var node = ProjectBrowserNodeTestFactory.CreateNamespaceNode("quantities", "Quantities");
            using var context = new ProjectBrowserViewModelMockContext();
            context.ReplaceRootNodes(node);
            context.SetIsLoaded(true);
            context.SelectHandler = selectedNode =>
                selectionService.SelectedElement = selectedNode.SourceElement;
            this.RegisterServices(context.Object, selectionService);

            using var component = this.Render<Home>();

            component.Find(".mb-project-browser-node__row").Click();

            component.WaitForAssertion(() =>
                Assert.That(component.Find("main h2").TextContent.Trim(), Is.EqualTo(nameof(Namespace))));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectionService.SelectedElement, Is.SameAs(node.SourceElement));
                Assert.That(context.SelectCallCount, Is.EqualTo(1));
                Assert.That(context.LastSelectedNode, Is.SameAs(node));
            }
        }

        /// <summary>
        /// Verifies Home reacts renderer-safely to external service selection.
        /// </summary>
        [Test]
        public void VerifyExternalSelectionRerendersHome()
        {
            using var context = new ProjectBrowserViewModelMockContext();
            context.SetIsLoading(true);
            var selectionService = this.RegisterServices(context.Object);

            using var component = this.Render<Home>();

            selectionService.SelectedElement = new LibraryPackage();

            component.WaitForAssertion(() => Assert.That(component.Markup, Does.Contain(nameof(LibraryPackage))));
        }

        /// <summary>
        /// Registers the shared selection service and reactive view models for Home component tests.
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
            this.Services.AddTransient<HomeViewModel>();
            this.Services.AddSingleton(projectBrowserViewModel);

            return selectionService;
        }
    }
}
