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
    using System.Threading.Tasks;

    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    using Moq;

    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

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
                () => new ProjectBrowserViewModel(null),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Property("ParamName").EqualTo("modelLoaderService"));
        }

        /// <summary>
        /// Verifies that the view model can initialize from a provided namespace.
        /// </summary>
        [Test]
        public void VerifyInitializeBuildsTreeFromNamespace()
        {
            var modelLoaderService = new Mock<IModelLoaderService>();
            var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object);

            viewModel.Initialize(LoadQuantitiesModel());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.RootNodes, Has.Count.EqualTo(1));
                Assert.That(viewModel.RootNodes[0].DisplayName, Is.Not.Empty);
                Assert.That(viewModel.RootNodes[0].Children, Is.Not.Empty);
                Assert.That(viewModel.IsLoaded, Is.True);
                Assert.That(viewModel.IsLoading, Is.False);
                Assert.That(viewModel.ErrorMessage, Is.Empty);
                modelLoaderService.Verify(x => x.LoadQuantitiesModel(), Times.Never);
            }
        }

        /// <summary>
        /// Verifies that constructing the view model does not load the Quantities model.
        /// </summary>
        [Test]
        public void VerifyConstructorDefersModelLoading()
        {
            var modelLoaderService = new Mock<IModelLoaderService>();

            var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object);

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
        public async Task VerifyInitializeAsyncLoadsQuantitiesModel()
        {
            var model = LoadQuantitiesModel();
            var modelLoaderService = new Mock<IModelLoaderService>();

            modelLoaderService
                .Setup(x => x.LoadQuantitiesModel())
                .Returns(model);

            var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object);

            await viewModel.InitializeAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.RootNodes, Has.Count.EqualTo(1));
                Assert.That(viewModel.RootNodes[0].DisplayName, Is.Not.Empty);
                Assert.That(viewModel.RootNodes[0].Children, Is.Not.Empty);
                Assert.That(viewModel.RootNodes[0].IsSelected, Is.True);
                Assert.That(viewModel.RootNodes[0].IsExpanded, Is.True);
                Assert.That(viewModel.SelectedNode, Is.SameAs(viewModel.RootNodes[0]));
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
            var model = LoadQuantitiesModel();
            var modelLoaderService = new Mock<IModelLoaderService>();

            modelLoaderService
                .Setup(x => x.LoadQuantitiesModel())
                .Returns(model);

            var firstViewModel = new ProjectBrowserViewModel(modelLoaderService.Object);
            var secondViewModel = new ProjectBrowserViewModel(modelLoaderService.Object);

            await firstViewModel.InitializeAsync();
            await secondViewModel.InitializeAsync();

            var firstRootNode = firstViewModel.RootNodes[0];
            var secondRootNode = secondViewModel.RootNodes[0];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstRootNode.Children, Is.Not.Empty);
                Assert.That(secondRootNode.Children, Is.Not.Empty);
            }

            firstViewModel.ToggleNode(firstRootNode);
            firstViewModel.SelectNode(firstRootNode.Children[0]);

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
        public async Task VerifyInitializeAsyncCapturesModelLoadingErrors()
        {
            var modelLoaderService = new Mock<IModelLoaderService>();

            modelLoaderService
                .Setup(x => x.LoadQuantitiesModel())
                .Throws(new InvalidOperationException("Model load failed"));

            var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object);

            await viewModel.InitializeAsync();

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
        public async Task VerifyInitializeAsyncReturnsEarlyWhenAlreadyLoaded()
        {
            var model = LoadQuantitiesModel();
            var modelLoaderService = new Mock<IModelLoaderService>();

            modelLoaderService
                .Setup(x => x.LoadQuantitiesModel())
                .Returns(model);

            var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object);

            await viewModel.InitializeAsync();
            await viewModel.InitializeAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.IsLoaded, Is.True);
                Assert.That(viewModel.IsLoading, Is.False);
                Assert.That(viewModel.ErrorMessage, Is.Empty);
                modelLoaderService.Verify(x => x.LoadQuantitiesModel(), Times.Once);
            }
        }

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
