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
    using System.Threading.Tasks;

    using Bunit;

    using Microsoft.Extensions.DependencyInjection;

    using Moq;

    using Mycelium.Bloom.Components.Pages;
    using Mycelium.Bloom.Core.ModelLoading;
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
            var viewModel = new ProjectBrowserViewModelStub
            {
                IsLoading = true
            };

            this.RegisterServices(viewModel);

            var component = this.Render<Home>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Markup, Does.Contain("Project Browser"));
                Assert.That(component.Markup, Does.Contain("Quantities model"));
                Assert.That(component.Markup, Does.Contain("Loading Quantities model"));
                Assert.That(component.Markup, Does.Contain("Selected element"));
                Assert.That(component.Markup, Does.Contain("None"));
                Assert.That(component.Find(".mb-project-browser"), Is.Not.Null);
                Assert.That(component.Instance, Is.AssignableTo<ReactiveInjectableComponentBase<HomeViewModel>>());
                Assert.That(viewModel.InitializeAsyncCallCount, Is.Zero);
            }
        }

        /// <summary>
        /// Verifies Project Browser publication updates Home through the shared scoped service.
        /// </summary>
        [Test]
        public void VerifyProjectBrowserSelectionUpdatesHome()
        {
            var model = new Namespace();
            var modelLoaderService = new Mock<IModelLoaderService>();

            modelLoaderService
                .Setup(x => x.LoadQuantitiesModel())
                .Returns(model);

            var selectionService = new ElementSelectionService();
            var viewModel = new ProjectBrowserViewModel(
                modelLoaderService.Object,
                selectionService);
            var initializationCompleted = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            viewModel.PropertyChanged += (_, _) =>
            {
                if (viewModel.IsLoaded && !viewModel.IsLoading)
                {
                    initializationCompleted.TrySetResult(true);
                }
            };

            this.RegisterServices(viewModel, selectionService);

            var component = this.Render<Home>();

            Assert.That(initializationCompleted.Task.Wait(System.TimeSpan.FromSeconds(10)), Is.True);

            selectionService.SelectedElement = null;

            component.WaitForState(() =>
                component.Find("[role='treeitem']").GetAttribute("aria-selected") == "false");

            component.Find(".mb-project-browser-node__row").Click();

            component.WaitForAssertion(() => Assert.That(component.Markup, Does.Contain("Namespace")));

            var node = viewModel.RootNodes[0];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectionService.SelectedElement, Is.SameAs(node.SourceElement));
                Assert.That(viewModel.SelectedNode, Is.SameAs(node));
            }
        }

        /// <summary>
        /// Verifies Home reacts renderer-safely to external service selection.
        /// </summary>
        [Test]
        public void VerifyExternalSelectionRerendersHome()
        {
            var viewModel = new ProjectBrowserViewModelStub
            {
                IsLoading = true
            };

            var selectionService = this.RegisterServices(viewModel);

            var component = this.Render<Home>();

            selectionService.SelectedElement = new LibraryPackage();

            component.WaitForAssertion(() => Assert.That(component.Markup, Does.Contain(nameof(LibraryPackage))));
        }

        /// <summary>
        /// Registers the shared selection service and reactive view models for Home component tests.
        /// </summary>
        /// <param name="projectBrowserViewModel">The Project Browser test view model.</param>
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
