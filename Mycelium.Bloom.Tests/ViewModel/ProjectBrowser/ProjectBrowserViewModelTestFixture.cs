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
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
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
        /// The expected display-name order of the root node's children.
        /// </summary>
        private static readonly string[] ExpectedRootChildDisplayNames =
            ["First child", "Second child"];

        /// <summary>
        /// Verifies that the constructor rejects a null model loader service.
        /// </summary>
        [Test]
        public void VerifyConstructorThrowsExceptionWhenModelLoaderServiceIsNull()
        {
            Assert.That(
                () =>
                {
                    using var viewModel = new ProjectBrowserViewModel(null, new ElementSelectionService());
                },
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
                () =>
                {
                    using var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object, null);
                },
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
            using var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object, new ElementSelectionService());

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

            using var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object, new ElementSelectionService());

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
            using var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object, selectionService);

            using var activation = viewModel.Activator.Activate();

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

            using var firstViewModel = new ProjectBrowserViewModel(modelLoaderService.Object, new ElementSelectionService());
            using var secondViewModel = new ProjectBrowserViewModel(modelLoaderService.Object, new ElementSelectionService());

            using var firstActivation = firstViewModel.Activator.Activate();
            using var secondActivation = secondViewModel.Activator.Activate();

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
        public void VerifyInitializeCommandCapturesModelLoadingErrors()
        {
            var modelLoaderService = new Mock<IModelLoaderService>();

            modelLoaderService
                .Setup(x => x.LoadQuantitiesModel())
                .Throws(new InvalidOperationException("Model load failed"));

            using var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object, new ElementSelectionService());

            Assert.That(
                async () => await viewModel.InitializeCommand.Execute(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo("Model load failed"));

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

            using var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object, new ElementSelectionService());

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
            using var viewModel = new ProjectBrowserViewModel(CreateModelLoader(model).Object, selectionService);

            using var activation = viewModel.Activator.Activate();

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
            using var viewModel = new ProjectBrowserViewModel(CreateModelLoader(model).Object, selectionService);

            await viewModel.InitializeCommand.Execute();

            using var activation = viewModel.Activator.Activate();

            var node = viewModel.RootNodes[0].Children[0];
            selectionService.SelectedElement = node.SourceElement;

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
            using var viewModel = new ProjectBrowserViewModel(CreateModelLoader(model).Object, selectionService);

            await viewModel.InitializeCommand.Execute();

            using var activation = viewModel.Activator.Activate();

            var node = viewModel.RootNodes[0].Children[0];
            selectionService.SelectedElement = node.SourceElement;
            selectionService.SelectedElement = null;

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
            using var viewModel = new ProjectBrowserViewModel(CreateModelLoader(model).Object, selectionService);

            await viewModel.InitializeCommand.Execute();

            var activation = viewModel.Activator.Activate();
            selectionService.SelectedElement = null;
            activation.Dispose();

            var node = viewModel.RootNodes[0].Children[0];
            selectionService.SelectedElement = node.SourceElement;

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
            using var firstViewModel = new ProjectBrowserViewModel(modelLoaderService.Object, selectionService);
            using var secondViewModel = new ProjectBrowserViewModel(modelLoaderService.Object, selectionService);

            await firstViewModel.InitializeCommand.Execute();
            await secondViewModel.InitializeCommand.Execute();

            using var firstActivation = firstViewModel.Activator.Activate();
            using var secondActivation = secondViewModel.Activator.Activate();

            selectionService.SelectedElement = null;
            var selectedElement = firstViewModel.RootNodes[0].Children[0].SourceElement;
            selectionService.SelectedElement = selectedElement;

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
            using var viewModel = new ProjectBrowserViewModel(CreateModelLoader(model).Object, selectionService);

            using var activation = viewModel.Activator.Activate();

            selectionService.SelectedElement = externalElement;
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
            using var viewModel = new ProjectBrowserViewModel(CreateModelLoader(model).Object, selectionService);

            await viewModel.InitializeCommand.Execute();

            using var activation = viewModel.Activator.Activate();

            selectionService.SelectedElement = distinctElement;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectionService.SelectedElement, Is.SameAs(distinctElement));
                Assert.That(viewModel.SelectedNode, Is.Null);
                Assert.That(viewModel.RootNodes[0].IsSelected, Is.False);
            }
        }

        /// <summary>
        /// Verifies the root collection is read-only, remains stable, and receives ordered tree content in place.
        /// </summary>
        [Test]
        public async Task VerifyInitializeCommandBindsOrderedRootsIntoReadOnlyCollection()
        {
            var model = CreateMinimalModel();
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(model).Object,
                new ElementSelectionService());
            var exposedRoots = viewModel.RootNodes;

            using var activation = viewModel.Activator.Activate();

            await viewModel.InitializeCommand.Execute();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exposedRoots, Is.InstanceOf<ReadOnlyObservableCollection<ProjectBrowserNodeViewModel>>());
                Assert.That(((IList<ProjectBrowserNodeViewModel>)exposedRoots).IsReadOnly, Is.True);
                Assert.That(viewModel.RootNodes, Is.SameAs(exposedRoots));
                Assert.That(exposedRoots, Has.Count.EqualTo(1));
                Assert.That(
                    exposedRoots[0].Children.Select(node => node.DisplayName),
                    Is.EqualTo(ExpectedRootChildDisplayNames));
            }
        }

        /// <summary>
        /// Verifies local selection has no imperative visual shortcut while the reactive projection is inactive.
        /// </summary>
        [Test]
        public async Task VerifySelectNodeCommandProjectsOnlyThroughActiveSelectionSubscription()
        {
            var model = CreateMinimalModel();
            var selectionService = new ElementSelectionService();
            using var viewModel = new ProjectBrowserViewModel(CreateModelLoader(model).Object, selectionService);

            await viewModel.InitializeCommand.Execute();

            var node = viewModel.RootNodes[0].Children[0];
            await viewModel.SelectNodeCommand.Execute(node);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectionService.SelectedElement, Is.SameAs(node.SourceElement));
                Assert.That(viewModel.SelectedNode, Is.Null);
                Assert.That(node.IsSelected, Is.False);
            }

            using var activation = viewModel.Activator.Activate();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.SelectedNode, Is.SameAs(node));
                Assert.That(node.IsSelected, Is.True);
            }
        }

        /// <summary>
        /// Verifies one local selection produces one visual projection notification.
        /// </summary>
        [Test]
        public async Task VerifySelectNodeCommandDoesNotDuplicateSelectionProjection()
        {
            var model = CreateMinimalModel();
            var selectionService = new ElementSelectionService();
            using var viewModel = new ProjectBrowserViewModel(CreateModelLoader(model).Object, selectionService);

            using var activation = viewModel.Activator.Activate();

            await viewModel.InitializeCommand.Execute();

            var selectionNotifications = 0;

            viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(viewModel.SelectedNode))
                {
                    selectionNotifications++;
                }
            };

            var node = viewModel.RootNodes[0].Children[0];
            await viewModel.SelectNodeCommand.Execute(node);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.SelectedNode, Is.SameAs(node));
                Assert.That(selectionNotifications, Is.EqualTo(1));
            }
        }

        /// <summary>
        /// Verifies command execution state is the source of the ViewModel loading state.
        /// </summary>
        [Test]
        public async Task VerifyInitializeCommandIsExecutingDrivesIsLoading()
        {
            using var loadStarted = new ManualResetEventSlim();
            using var releaseLoad = new ManualResetEventSlim();
            var modelLoaderService = new Mock<IModelLoaderService>();

            modelLoaderService
                .Setup(x => x.LoadQuantitiesModel())
                .Returns(() =>
                {
                    loadStarted.Set();

                    if (!releaseLoad.Wait(TimeSpan.FromSeconds(10)))
                    {
                        throw new TimeoutException("The test did not release model loading.");
                    }

                    return CreateMinimalModel();
                });

            using var viewModel = new ProjectBrowserViewModel(
                modelLoaderService.Object,
                new ElementSelectionService());
            using var activation = viewModel.Activator.Activate();
            var observedStates = new List<(bool IsExecuting, bool IsLoading)>();

            using var stateSubscription = System.ObservableExtensions.Subscribe(
                viewModel.InitializeCommand.IsExecuting,
                isExecuting => observedStates.Add((isExecuting, viewModel.IsLoading)));

            var initialization = Task.Run(async () => await viewModel.InitializeCommand.Execute());

            try
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(loadStarted.Wait(TimeSpan.FromSeconds(10)), Is.True);
                    Assert.That(viewModel.IsLoading, Is.True);
                }
            }
            finally
            {
                releaseLoad.Set();
            }

            await initialization;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(observedStates, Does.Contain((true, true)));
                Assert.That(observedStates[observedStates.Count - 1], Is.EqualTo((false, false)));
                Assert.That(viewModel.IsLoaded, Is.True);
                Assert.That(viewModel.ErrorMessage, Is.Empty);
            }
        }

        /// <summary>
        /// Verifies deactivation removes subscriptions and reactivation restores them without replacing commands.
        /// </summary>
        [Test]
        public async Task VerifyDeactivationAndReactivationPreserveCommandsAndRestoreProjection()
        {
            var model = CreateMinimalModel();
            var selectionService = new ElementSelectionService();
            using var viewModel = new ProjectBrowserViewModel(CreateModelLoader(model).Object, selectionService);
            var initializeCommand = viewModel.InitializeCommand;
            var toggleCommand = viewModel.ToggleNodeCommand;
            var selectCommand = viewModel.SelectNodeCommand;
            var activation = viewModel.Activator.Activate();

            await viewModel.InitializeCommand.Execute();

            activation.Dispose();

            var rootNode = viewModel.RootNodes[0];
            var childNode = rootNode.Children[0];
            selectionService.SelectedElement = childNode.SourceElement;

            Assert.That(viewModel.SelectedNode, Is.SameAs(rootNode));

            using var reactivation = viewModel.Activator.Activate();

            await viewModel.ToggleNodeCommand.Execute(rootNode);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.InitializeCommand, Is.SameAs(initializeCommand));
                Assert.That(viewModel.ToggleNodeCommand, Is.SameAs(toggleCommand));
                Assert.That(viewModel.SelectNodeCommand, Is.SameAs(selectCommand));
                Assert.That(viewModel.SelectedNode, Is.SameAs(childNode));
                Assert.That(childNode.IsSelected, Is.True);
                Assert.That(rootNode.IsSelected, Is.False);
                Assert.That(rootNode.IsExpanded, Is.False);
            }
        }

        /// <summary>
        /// Verifies final ViewModel disposal deterministically disposes all three commands.
        /// </summary>
        [Test]
        public void VerifyDisposeOwnsCommandsAndIsIdempotent()
        {
            using var viewModel = new ProjectBrowserViewModel(
                new Mock<IModelLoaderService>().Object,
                new ElementSelectionService());
            var initializeCommand = viewModel.InitializeCommand;
            var toggleCommand = viewModel.ToggleNodeCommand;
            var selectCommand = viewModel.SelectNodeCommand;

            Assert.That(viewModel.Dispose, Throws.Nothing);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.InitializeCommand, Is.SameAs(initializeCommand));
                Assert.That(viewModel.ToggleNodeCommand, Is.SameAs(toggleCommand));
                Assert.That(viewModel.SelectNodeCommand, Is.SameAs(selectCommand));
                Assert.That(viewModel.Dispose, Throws.Nothing);
            }
        }

        /// <summary>
        /// Verifies deactivation releases activation-owned subscriptions before final ViewModel disposal.
        /// </summary>
        [Test]
        public async Task VerifyDeactivationBeforeDisposeReleasesActivationSubscriptions()
        {
            var selectionService = new ElementSelectionService();
            ProjectBrowserNodeViewModel rootNode;
            ProjectBrowserNodeViewModel childNode;

            using (var viewModel = new ProjectBrowserViewModel(
                       CreateModelLoader(CreateMinimalModel()).Object,
                       selectionService))
            {
                using (viewModel.Activator.Activate())
                {
                    await viewModel.InitializeCommand.Execute();

                    rootNode = viewModel.RootNodes[0];
                    childNode = rootNode.Children[0];
                }
            }

            selectionService.SelectedElement = childNode.SourceElement;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rootNode.IsSelected, Is.True);
                Assert.That(childNode.IsSelected, Is.False);
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
            var firstChild = new Mock<INamespace>();
            firstChild.SetupGet(x => x.ElementId).Returns("first-child");
            firstChild.SetupGet(x => x.DeclaredName).Returns("First child");
            firstChild.SetupGet(x => x.ownedElement).Returns([]);

            var secondChild = new Mock<INamespace>();
            secondChild.SetupGet(x => x.ElementId).Returns("second-child");
            secondChild.SetupGet(x => x.DeclaredName).Returns("Second child");
            secondChild.SetupGet(x => x.ownedElement).Returns([]);

            var root = new Mock<INamespace>();
            root.SetupGet(x => x.ElementId).Returns("root");
            root.SetupGet(x => x.DeclaredName).Returns("Root");
            root.SetupGet(x => x.ownedElement).Returns([firstChild.Object, secondChild.Object]);

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
