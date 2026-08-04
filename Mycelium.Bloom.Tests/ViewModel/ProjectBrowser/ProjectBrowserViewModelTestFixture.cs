// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserViewModelTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.ProjectBrowser
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    using Moq;

    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using ReactiveUI.Primitives.Signals;

    using SysML2.NET.Core.POCO.Root.Namespaces;

    /// <summary>
    /// Tests the <see cref="ProjectBrowserViewModel" />.
    /// </summary>
    [TestFixture]
    public sealed class ProjectBrowserViewModelTestFixture
    {
        /// <summary>
        /// Verifies that the constructor rejects a null model loader service.
        /// </summary>
        [Test]
        public void VerifyConstructorThrowsExceptionWhenModelLoaderServiceIsNull()
        {
            Assert.That(
                () => new ProjectBrowserViewModel(null, new ElementSelectionService()),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Property("ParamName").EqualTo("modelLoaderService"));
        }

        /// <summary>
        /// Verifies that the constructor rejects a null selection service.
        /// </summary>
        [Test]
        public void VerifyConstructorThrowsExceptionWhenSelectionServiceIsNull()
        {
            var modelLoaderService = new Mock<IModelLoaderService>();

            Assert.That(
                () => new ProjectBrowserViewModel(modelLoaderService.Object, null),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Property("ParamName").EqualTo("elementSelectionService"));
        }

        /// <summary>
        /// Verifies that the initialization command builds the complete Quantities tree.
        /// </summary>
        [Test]
        public async Task VerifyInitializeCommandBuildsTreeFromNamespace()
        {
            var model = LoadQuantitiesModel();
            var modelLoaderService = CreateModelLoader(model);
            var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object, new ElementSelectionService());

            await viewModel.InitializeCommand.Execute();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.RootNodes, Has.Count.EqualTo(1));
                Assert.That(viewModel.RootNodes[0].DisplayName, Is.Not.Empty);
                Assert.That(viewModel.RootNodes[0].ElementKind, Is.EqualTo(SysmlModelElementKind.Namespace));
                Assert.That(viewModel.RootNodes[0].Children, Is.Not.Empty);
                Assert.That(viewModel.IsLoaded, Is.True);
                Assert.That(viewModel.IsLoading, Is.False);
                Assert.That(viewModel.ErrorMessage, Is.Empty);
                modelLoaderService.Verify(x => x.LoadQuantitiesModel(), Times.Once);
            }
        }

        /// <summary>
        /// Verifies that constructing the view model does not load the Quantities model.
        /// </summary>
        [Test]
        public void VerifyConstructorDefersModelLoading()
        {
            var modelLoaderService = new Mock<IModelLoaderService>();

            var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object, new ElementSelectionService());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel, Is.Not.Null);
                Assert.That(viewModel.RootNodes, Is.Empty);
                Assert.That(viewModel.IsLoaded, Is.False);
                Assert.That(viewModel.IsLoading, Is.False);
                Assert.That(viewModel.ErrorMessage, Is.Empty);
                modelLoaderService.Verify(x => x.LoadQuantitiesModel(), Times.Never);
            }
        }

        /// <summary>
        /// Verifies that the Quantities project browser view model loads the model asynchronously.
        /// </summary>
        [Test]
        public async Task VerifyInitializeCommandLoadsQuantitiesModel()
        {
            var model = CreateMinimalModel();
            var modelLoaderService = CreateModelLoader(model);

            var selectionService = new ElementSelectionService();
            var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object, selectionService);

            await viewModel.InitializeCommand.Execute();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.RootNodes, Has.Count.EqualTo(1));
                Assert.That(viewModel.RootNodes[0].DisplayName, Is.Not.Empty);
                Assert.That(viewModel.RootNodes[0].Children, Is.Not.Empty);
                Assert.That(viewModel.RootNodes[0].IsSelected, Is.True);
                Assert.That(viewModel.RootNodes[0].IsExpanded, Is.True);
                Assert.That(viewModel.SelectedNode, Is.SameAs(viewModel.RootNodes[0]));
                Assert.That(selectionService.SelectedElement, Is.SameAs(viewModel.RootNodes[0].SourceElement));
                Assert.That(viewModel.IsLoaded, Is.True);
                Assert.That(viewModel.IsLoading, Is.False);
                Assert.That(viewModel.ErrorMessage, Is.Empty);
                modelLoaderService.Verify(x => x.LoadQuantitiesModel(), Times.Once);
            }
        }

        /// <summary>
        /// Verifies that separate project browser view model instances keep independent state.
        /// </summary>
        [Test]
        public async Task VerifyProjectBrowserViewModelsKeepIndependentState()
        {
            var model = CreateMinimalModel();
            var modelLoaderService = CreateModelLoader(model);

            var firstViewModel = new ProjectBrowserViewModel(modelLoaderService.Object, new ElementSelectionService());
            var secondViewModel = new ProjectBrowserViewModel(modelLoaderService.Object, new ElementSelectionService());

            await firstViewModel.InitializeCommand.Execute();
            await secondViewModel.InitializeCommand.Execute();

            var firstRootNode = firstViewModel.RootNodes[0];
            var secondRootNode = secondViewModel.RootNodes[0];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstRootNode.Children, Is.Not.Empty);
                Assert.That(secondRootNode.Children, Is.Not.Empty);
            }

            await firstViewModel.ToggleNodeCommand.Execute(firstRootNode);
            await firstViewModel.SelectNodeCommand.Execute(firstRootNode.Children[0]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(secondViewModel, Is.Not.SameAs(firstViewModel));
                Assert.That(secondRootNode, Is.Not.SameAs(firstRootNode));
                Assert.That(firstRootNode.IsExpanded, Is.False);
                Assert.That(secondRootNode.IsExpanded, Is.True);
                Assert.That(firstRootNode.Children[0].IsSelected, Is.True);
                Assert.That(secondRootNode.Children[0].IsSelected, Is.False);
                Assert.That(firstViewModel.SelectedNode, Is.SameAs(firstRootNode.Children[0]));
                Assert.That(secondViewModel.SelectedNode, Is.SameAs(secondRootNode));
                modelLoaderService.Verify(x => x.LoadQuantitiesModel(), Times.Exactly(2));
            }
        }

        /// <summary>
        /// Verifies that model loading exceptions are captured in the project browser view model.
        /// </summary>
        [Test]
        public async Task VerifyInitializeCommandCapturesModelLoadingErrors()
        {
            var modelLoaderService = new Mock<IModelLoaderService>();

            modelLoaderService
                .Setup(x => x.LoadQuantitiesModel())
                .Throws(new InvalidOperationException("Model load failed"));

            var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object, new ElementSelectionService());

            await viewModel.InitializeCommand.Execute();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.RootNodes, Is.Empty);
                Assert.That(viewModel.SelectedNode, Is.Null);
                Assert.That(viewModel.IsLoaded, Is.False);
                Assert.That(viewModel.IsLoading, Is.False);
                Assert.That(viewModel.ErrorMessage, Is.EqualTo("Model load failed"));
                modelLoaderService.Verify(x => x.LoadQuantitiesModel(), Times.Once);
            }
        }

        /// <summary>
        /// Verifies that repeated initialization does not reload an already loaded project browser.
        /// </summary>
        [Test]
        public async Task VerifyInitializeCommandReturnsEarlyWhenAlreadyLoaded()
        {
            var model = CreateMinimalModel();
            var modelLoaderService = CreateModelLoader(model);

            var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object, new ElementSelectionService());

            await viewModel.InitializeCommand.Execute();
            await viewModel.InitializeCommand.Execute();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.IsLoaded, Is.True);
                Assert.That(viewModel.IsLoading, Is.False);
                Assert.That(viewModel.ErrorMessage, Is.Empty);
                modelLoaderService.Verify(x => x.LoadQuantitiesModel(), Times.Once);
            }
        }

        /// <summary>
        /// Verifies the selection command publishes the node's source element.
        /// </summary>
        [Test]
        public async Task VerifySelectNodeCommandPublishesSourceElement()
        {
            var model = CreateMinimalModel();
            var selectionService = new ElementSelectionService();
            var viewModel = new ProjectBrowserViewModel(CreateModelLoader(model).Object, selectionService);

            await viewModel.InitializeCommand.Execute();

            var node = viewModel.RootNodes[0].Children[0];
            await viewModel.SelectNodeCommand.Execute(node);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectionService.SelectedElement, Is.SameAs(node.SourceElement));
                Assert.That(viewModel.SelectedNode, Is.SameAs(node));
                Assert.That(node.IsSelected, Is.True);
            }
        }

        /// <summary>
        /// Verifies external selection updates the Project Browser visual projection while activated.
        /// </summary>
        [Test]
        public async Task VerifyExternalSelectionUpdatesVisualProjectionWhileActivated()
        {
            var model = CreateMinimalModel();
            var selectionService = new ElementSelectionService();
            var viewModel = new ProjectBrowserViewModel(CreateModelLoader(model).Object, selectionService);

            await viewModel.InitializeCommand.Execute();

            using var activation = viewModel.Activator.Activate();

            var node = viewModel.RootNodes[0].Children[0];
            selectionService.SelectElement(node.SourceElement);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.SelectedNode, Is.SameAs(node));
                Assert.That(node.IsSelected, Is.True);
                Assert.That(viewModel.RootNodes[0].IsSelected, Is.False);
            }
        }

        /// <summary>
        /// Verifies externally clearing selection clears the Project Browser visual projection.
        /// </summary>
        [Test]
        public async Task VerifyExternalClearSelectionClearsVisualProjection()
        {
            var model = CreateMinimalModel();
            var selectionService = new ElementSelectionService();
            var viewModel = new ProjectBrowserViewModel(CreateModelLoader(model).Object, selectionService);

            await viewModel.InitializeCommand.Execute();

            using var activation = viewModel.Activator.Activate();

            var node = viewModel.RootNodes[0].Children[0];
            selectionService.SelectElement(node.SourceElement);
            selectionService.ClearSelection();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.SelectedNode, Is.Null);
                Assert.That(node.IsSelected, Is.False);
            }
        }

        /// <summary>
        /// Verifies deactivation removes the Project Browser's external selection subscription.
        /// </summary>
        [Test]
        public async Task VerifyDeactivatedViewModelDoesNotObserveExternalSelection()
        {
            var model = CreateMinimalModel();
            var selectionService = new ElementSelectionService();
            var viewModel = new ProjectBrowserViewModel(CreateModelLoader(model).Object, selectionService);

            await viewModel.InitializeCommand.Execute();

            var activation = viewModel.Activator.Activate();
            selectionService.ClearSelection();
            activation.Dispose();

            var node = viewModel.RootNodes[0].Children[0];
            selectionService.SelectElement(node.SourceElement);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectionService.SelectedElement, Is.SameAs(node.SourceElement));
                Assert.That(viewModel.SelectedNode, Is.Null);
                Assert.That(node.IsSelected, Is.False);
            }
        }

        /// <summary>
        /// Verifies two Project Browser consumers synchronize through one scoped selection service.
        /// </summary>
        [Test]
        public async Task VerifyTwoViewModelsObserveSameSelectionService()
        {
            var model = CreateMinimalModel();
            var selectionService = new ElementSelectionService();
            var modelLoaderService = CreateModelLoader(model);
            var firstViewModel = new ProjectBrowserViewModel(modelLoaderService.Object, selectionService);
            var secondViewModel = new ProjectBrowserViewModel(modelLoaderService.Object, selectionService);

            await firstViewModel.InitializeCommand.Execute();
            await secondViewModel.InitializeCommand.Execute();

            using var firstActivation = firstViewModel.Activator.Activate();
            using var secondActivation = secondViewModel.Activator.Activate();

            selectionService.ClearSelection();
            var selectedElement = firstViewModel.RootNodes[0].Children[0].SourceElement;
            selectionService.SelectElement(selectedElement);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstViewModel.SelectedNode.SourceElement, Is.SameAs(selectedElement));
                Assert.That(secondViewModel.SelectedNode.SourceElement, Is.SameAs(selectedElement));
                Assert.That(firstViewModel.SelectedNode, Is.Not.SameAs(secondViewModel.SelectedNode));
            }
        }

        /// <summary>
        /// Verifies initialization preserves an external selection that is absent from the loaded tree.
        /// </summary>
        [Test]
        public async Task VerifyInitializeCommandPreservesExternalSelectionAbsentFromTree()
        {
            var model = CreateMinimalModel();
            var externalElement = new Namespace { ElementId = "external" };
            var selectionService = new ElementSelectionService();
            var viewModel = new ProjectBrowserViewModel(CreateModelLoader(model).Object, selectionService);

            selectionService.SelectElement(externalElement);
            await viewModel.InitializeCommand.Execute();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectionService.SelectedElement, Is.SameAs(externalElement));
                Assert.That(viewModel.RootNodes, Has.Count.EqualTo(1));
                Assert.That(viewModel.SelectedNode, Is.Null);
                Assert.That(viewModel.RootNodes[0].IsSelected, Is.False);
                Assert.That(viewModel.IsLoaded, Is.True);
                Assert.That(viewModel.ErrorMessage, Is.Empty);
            }
        }

        /// <summary>
        /// Verifies a distinct external object with the same ElementId does not match a visual node.
        /// </summary>
        [Test]
        public async Task VerifyExternalSelectionUsesReferenceIdentity()
        {
            var model = new Namespace { ElementId = "shared-id" };
            var distinctElement = new Namespace { ElementId = "shared-id" };
            var selectionService = new ElementSelectionService();
            var viewModel = new ProjectBrowserViewModel(CreateModelLoader(model).Object, selectionService);

            await viewModel.InitializeCommand.Execute();

            using var activation = viewModel.Activator.Activate();

            selectionService.SelectElement(distinctElement);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectionService.SelectedElement, Is.SameAs(distinctElement));
                Assert.That(viewModel.SelectedNode, Is.Null);
                Assert.That(viewModel.RootNodes[0].IsSelected, Is.False);
            }
        }

        /// <summary>
        /// Creates a model loader that returns the provided namespace.
        /// </summary>
        /// <param name="model">The namespace returned by the loader.</param>
        /// <returns>The configured model loader mock.</returns>
        private static Mock<IModelLoaderService> CreateModelLoader(INamespace model)
        {
            var modelLoaderService = new Mock<IModelLoaderService>();

            modelLoaderService
                .Setup(x => x.LoadQuantitiesModel())
                .Returns(model);

            return modelLoaderService;
        }

        /// <summary>
        /// Creates a small model with one selectable child.
        /// </summary>
        /// <returns>The minimal namespace model.</returns>
        private static INamespace CreateMinimalModel()
        {
            var child = new Mock<INamespace>();
            child.SetupGet(x => x.ElementId).Returns("child");
            child.SetupGet(x => x.DeclaredName).Returns("Child");
            child.SetupGet(x => x.ownedElement).Returns([]);

            var root = new Mock<INamespace>();
            root.SetupGet(x => x.ElementId).Returns("root");
            root.SetupGet(x => x.DeclaredName).Returns("Root");
            root.SetupGet(x => x.ownedElement).Returns([child.Object]);

            return root.Object;
        }

        /// <summary>
        /// Loads the real Quantities model from application resources.
        /// </summary>
        /// <returns>The loaded Quantities namespace model.</returns>
        private static INamespace LoadQuantitiesModel()
        {
            var applicationPath = TestRepository.GetDirectoryPath("Mycelium.Bloom");

            var hostEnvironment = new Mock<IHostEnvironment>();
            hostEnvironment.Setup(x => x.ContentRootPath).Returns(applicationPath);

            using var memoryCache = new MemoryCache(new MemoryCacheOptions());
            using var loggerFactory = LoggerFactory.Create(_ => { });

            var modelLoaderService = new ModelLoaderService(hostEnvironment.Object, loggerFactory, memoryCache);

            return modelLoaderService.LoadQuantitiesModel();
        }
    }
}
