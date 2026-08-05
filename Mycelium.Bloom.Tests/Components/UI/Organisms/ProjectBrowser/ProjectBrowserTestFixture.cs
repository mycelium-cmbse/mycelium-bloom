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
    using System.Threading;
    using System.Threading.Tasks;

    using Bunit;

    using Microsoft.Extensions.DependencyInjection;

    using Moq;

    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Core.Selection;
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
                IsLoaded = true
            };

            viewModel.ReplaceRootNodes(node);

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
        /// Verifies that the project browser initializes an unloaded view model without a synthetic selection callback.
        /// </summary>
        [Test]
        public void VerifyOnInitializedAsyncInitializesViewModel()
        {
            ProjectBrowserNodeViewModel selectedNode = null;
            var node = ProjectBrowserNodeTestFactory.CreateNamespaceNode("quantities", "Quantities");
            var viewModel = new ProjectBrowserViewModelStub();

            viewModel.InitializeHandler = () =>
            {
                viewModel.ReplaceRootNodes(node);
                viewModel.IsLoaded = true;
                viewModel.ApplySelection(node);

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

            Assert.That(selectedNode, Is.Null);
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
                IsLoaded = true
            };

            viewModel.ReplaceRootNodes(node);

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

        /// <summary>
        /// Verifies a Project Browser interaction publishes its source element to the shared service.
        /// </summary>
        [Test]
        public void VerifyNodeSelectionPublishesSourceElement()
        {
            var model = new Namespace();
            var selectionService = new ElementSelectionService();
            var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(model).Object,
                selectionService);
            var initializationCompleted = ObserveSuccessfulInitialization(viewModel);

            this.Services.AddSingleton<IProjectBrowserViewModel>(viewModel);

            var component = this.Render<ProjectBrowserComponent>();

            Assert.That(initializationCompleted.Task.Wait(System.TimeSpan.FromSeconds(10)), Is.True);

            selectionService.SelectedElement = null;

            component.WaitForState(() =>
                component.Find("[role='treeitem']").GetAttribute("aria-selected") == "false");

            component.Find("button").Click();

            component.WaitForState(() =>
                object.ReferenceEquals(selectionService.SelectedElement, viewModel.RootNodes[0].SourceElement));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectionService.SelectedElement, Is.SameAs(viewModel.RootNodes[0].SourceElement));
                Assert.That(viewModel.SelectedNode, Is.SameAs(viewModel.RootNodes[0]));
            }
        }

        /// <summary>
        /// Verifies external shared selection updates Project Browser visual selection.
        /// </summary>
        [Test]
        public void VerifyExternalSelectionUpdatesVisualHighlight()
        {
            var model = new Namespace();
            var selectionService = new ElementSelectionService();
            var callbackCount = 0;
            var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(model).Object,
                selectionService);
            var initializationCompleted = ObserveSuccessfulInitialization(viewModel);

            this.Services.AddSingleton<IProjectBrowserViewModel>(viewModel);

            var component = this.Render<ProjectBrowserComponent>(parameters => parameters
                .Add(browser => browser.SelectedNodeChanged, _ =>
                {
                    callbackCount++;
                }));

            Assert.That(initializationCompleted.Task.Wait(System.TimeSpan.FromSeconds(10)), Is.True);

            selectionService.SelectedElement = null;
            selectionService.SelectedElement = viewModel.RootNodes[0].SourceElement;

            component.WaitForAssertion(() =>
                Assert.That(component.Find("[role='treeitem']").GetAttribute("aria-selected"), Is.EqualTo("true")));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("button").ClassList, Does.Contain("mb-project-browser-node__row--selected"));
                Assert.That(callbackCount, Is.Zero);
            }
        }

        /// <summary>
        /// Verifies component disposal deactivates Project Browser selection subscriptions.
        /// </summary>
        [Test]
        public void VerifyDisposedComponentDoesNotObserveSelection()
        {
            var model = new Namespace();
            var selectionService = new ElementSelectionService();
            var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(model).Object,
                selectionService);
            var initializationCompleted = ObserveSuccessfulInitialization(viewModel);

            this.Services.AddSingleton<IProjectBrowserViewModel>(viewModel);

            var component = this.Render<ProjectBrowserComponent>();

            Assert.That(initializationCompleted.Task.Wait(System.TimeSpan.FromSeconds(10)), Is.True);

            var rootNode = viewModel.RootNodes[0];
            selectionService.SelectedElement = null;
            component.Instance.Dispose();

            selectionService.SelectedElement = rootNode.SourceElement;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectionService.SelectedElement, Is.SameAs(rootNode.SourceElement));
                Assert.That(viewModel.SelectedNode, Is.Null);
            }
        }

        /// <summary>
        /// Verifies disposal cancels in-flight initialization before it can expose or publish the loaded model.
        /// </summary>
        [Test]
        public void VerifyDisposalCancelsInFlightInitialization()
        {
            using var loadStarted = new ManualResetEventSlim();
            using var releaseLoad = new ManualResetEventSlim();
            using var loadFinished = new ManualResetEventSlim();
            using var initializationFinished = new ManualResetEventSlim();

            var modelLoaderService = new Mock<IModelLoaderService>();

            modelLoaderService
                .Setup(x => x.LoadQuantitiesModel())
                .Returns(() =>
                {
                    loadStarted.Set();

                    if (!releaseLoad.Wait(System.TimeSpan.FromSeconds(10)))
                    {
                        throw new System.TimeoutException("The test did not release model loading.");
                    }

                    loadFinished.Set();

                    return new Namespace { DeclaredName = "Replacement" };
                });

            var selectionService = new ElementSelectionService();
            var selectionCallbackCount = 0;
            var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object, selectionService);

            viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(viewModel.IsLoading)
                    && !viewModel.IsLoading
                    && loadStarted.IsSet)
                {
                    initializationFinished.Set();
                }
            };

            this.Services.AddSingleton<IProjectBrowserViewModel>(viewModel);

            var component = this.Render<ProjectBrowserComponent>(parameters => parameters
                .Add(browser => browser.SelectedNodeChanged, _ => selectionCallbackCount++));

            Assert.That(loadStarted.Wait(System.TimeSpan.FromSeconds(10)), Is.True);

            component.Instance.Dispose();

            try
            {
                Assert.That(initializationFinished.Wait(System.TimeSpan.FromSeconds(10)), Is.True);
            }
            finally
            {
                releaseLoad.Set();
            }

            Assert.That(loadFinished.Wait(System.TimeSpan.FromSeconds(10)), Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectionService.SelectedElement, Is.Null);
                Assert.That(selectionCallbackCount, Is.Zero);
                Assert.That(viewModel.RootNodes, Is.Empty);
                Assert.That(viewModel.SelectedNode, Is.Null);
                Assert.That(viewModel.IsLoaded, Is.False);
                Assert.That(viewModel.IsLoading, Is.False);
                Assert.That(viewModel.ErrorMessage, Is.Empty);
                modelLoaderService.Verify(x => x.LoadQuantitiesModel(), Times.Once);
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
        /// Observes successful completion of command-based Project Browser initialization.
        /// </summary>
        /// <param name="viewModel">The view model to observe.</param>
        /// <returns>A completion source signaled after loading has completed successfully.</returns>
        private static TaskCompletionSource<bool> ObserveSuccessfulInitialization(
            ProjectBrowserViewModel viewModel)
        {
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            viewModel.PropertyChanged += (_, _) =>
            {
                if (viewModel.IsLoaded && !viewModel.IsLoading)
                {
                    completion.TrySetResult(true);
                }
            };

            return completion;
        }
    }
}
