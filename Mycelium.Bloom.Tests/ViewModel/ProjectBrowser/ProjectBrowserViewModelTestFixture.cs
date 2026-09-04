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
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    using Moq;

    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using ReactiveUI;

    using SysML2.NET.Core.POCO.Root.Annotations;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Parts;

    using static Mycelium.Bloom.Tests.Common.ProjectBrowserNodeTestFactory;

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
        /// The expected ordered IsLoading values during initialization.
        /// </summary>
        private static readonly bool[] ExpectedInitializationLoadingStates =
            [false, true, false];

        /// <summary>
        /// The expected ordered IsLoaded values during successful initialization.
        /// </summary>
        private static readonly bool[] ExpectedInitializationLoadedStates =
            [false, true];

        /// <summary>
        /// The expected ordered ErrorMessage values during failed initialization.
        /// </summary>
        private static readonly string[] ExpectedInitializationErrorMessages =
            [string.Empty, "Model load failed"];

        /// <summary>
        /// Verifies that the constructor rejects a null model loader service.
        /// </summary>
        [Test]
        public void VerifyConstructorThrowsExceptionWhenModelLoaderServiceIsNull()
        {
            Assert.That(
                () =>
                {
                    using var viewModel = new ProjectBrowserViewModel(null, new ContextAwareService());
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
        /// Verifies that initialization builds the complete Quantities tree.
        /// </summary>
        [Test]
        public async Task VerifyInitializeAsyncBuildsTreeFromNamespace()
        {
            var model = LoadQuantitiesModel();
            var modelLoaderService = CreateModelLoader(model);
            using var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object, new ContextAwareService());

            var initialized = await viewModel.InitializeAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(initialized, Is.True);
                Assert.That(viewModel.RootNodes, Has.Count.EqualTo(1));
                Assert.That(viewModel.RootNodes[0].DisplayName, Is.Not.Empty);
                Assert.That(viewModel.RootNodes[0].ElementType, Is.EqualTo(typeof(Namespace)));
                Assert.That(viewModel.RootNodes[0].Children, Is.Not.Empty);
                Assert.That(viewModel.IsLoaded, Is.True);
                Assert.That(viewModel.IsLoading, Is.False);
                Assert.That(viewModel.ErrorMessage, Is.Empty);
                modelLoaderService.Verify(x => x.LoadQuantitiesModel(), Times.Once);
            }
        }

        /// <summary>
        /// Verifies that constructing the ViewModel does not load the Quantities model.
        /// </summary>
        [Test]
        public void VerifyConstructorDefersModelLoading()
        {
            var modelLoaderService = new Mock<IModelLoaderService>();

            using var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object, new ContextAwareService());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.RootNodes, Is.Empty);
                Assert.That(viewModel.SelectedNode, Is.Null);
                Assert.That(viewModel.IsLoaded, Is.False);
                Assert.That(viewModel.IsLoading, Is.False);
                Assert.That(viewModel.ErrorMessage, Is.Empty);
                modelLoaderService.Verify(x => x.LoadQuantitiesModel(), Times.Never);
            }
        }

        /// <summary>
        /// Verifies successful initialization loads, selects, and expands a local default root.
        /// </summary>
        [Test]
        public async Task VerifyInitializeAsyncLoadsQuantitiesModelAndSelectsLocalDefaultRoot()
        {
            var model = CreateMinimalModel();
            var modelLoaderService = CreateModelLoader(model);
            var selectionService = new ContextAwareService();
            using var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object, selectionService);

            var initialized = await viewModel.InitializeAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(initialized, Is.True);
                Assert.That(viewModel.RootNodes, Has.Count.EqualTo(1));
                Assert.That(viewModel.RootNodes[0].Children, Is.Not.Empty);
                Assert.That(viewModel.RootNodes[0].IsExpanded, Is.True);
                Assert.That(viewModel.SelectedNode, Is.SameAs(viewModel.RootNodes[0]));
                Assert.That(selectionService.SelectedElement, Is.Null);
                Assert.That(viewModel.IsLoaded, Is.True);
                Assert.That(viewModel.IsLoading, Is.False);
                Assert.That(viewModel.ErrorMessage, Is.Empty);
                modelLoaderService.Verify(x => x.LoadQuantitiesModel(), Times.Once);
            }
        }

        /// <summary>
        /// Verifies that separate Project Browser ViewModels keep independent expansion state over shared selection.
        /// </summary>
        [Test]
        public async Task VerifyProjectBrowserViewModelsKeepIndependentExpansionState()
        {
            var model = CreateMinimalModel();
            var modelLoaderService = CreateModelLoader(model);
            var selectionService = new ContextAwareService();
            using var firstViewModel = new ProjectBrowserViewModel(
                modelLoaderService.Object,
                selectionService);
            using var secondViewModel = new ProjectBrowserViewModel(
                modelLoaderService.Object,
                selectionService);

            await Task.WhenAll(
                firstViewModel.InitializeAsync(CancellationToken.None),
                secondViewModel.InitializeAsync(CancellationToken.None));

            var firstRootNode = firstViewModel.RootNodes[0];
            var secondRootNode = secondViewModel.RootNodes[0];

            if (firstRootNode.IsExpanded)
            {
                firstViewModel.ToggleNode(firstRootNode);
            }

            if (secondRootNode.IsExpanded)
            {
                secondViewModel.ToggleNode(secondRootNode);
            }

            firstViewModel.ToggleNode(firstRootNode);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(secondViewModel, Is.Not.SameAs(firstViewModel));
                Assert.That(secondRootNode, Is.Not.SameAs(firstRootNode));
                Assert.That(secondRootNode.Children[0], Is.Not.SameAs(firstRootNode.Children[0]));
                Assert.That(firstRootNode.IsExpanded, Is.True);
                Assert.That(secondRootNode.IsExpanded, Is.False);
                Assert.That(firstViewModel.SelectedNode, Is.SameAs(firstRootNode));
                Assert.That(secondViewModel.SelectedNode, Is.SameAs(secondRootNode));
                Assert.That(firstViewModel.SelectedNode.SourceElement,
                    Is.SameAs(secondViewModel.SelectedNode.SourceElement));
                Assert.That(selectionService.SelectedElement, Is.Null);
                modelLoaderService.Verify(x => x.LoadQuantitiesModel(), Times.Exactly(2));
            }
        }

        /// <summary>
        /// Verifies genuine loading failures are handled once by the ViewModel.
        /// </summary>
        [Test]
        public async Task VerifyInitializeAsyncCapturesModelLoadingErrors()
        {
            var modelLoaderService = new Mock<IModelLoaderService>();
            modelLoaderService
                .Setup(x => x.LoadQuantitiesModel())
                .Throws(new InvalidOperationException("Model load failed"));
            using var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object, new ContextAwareService());
            var observedErrors = new List<string>();
            var observedLoadingStates = new List<bool>();

            using var errorSubscription = System.ObservableExtensions.Subscribe(
                viewModel.WhenAnyValue(modelView => modelView.ErrorMessage),
                observedErrors.Add);
            using var loadingSubscription = System.ObservableExtensions.Subscribe(
                viewModel.WhenAnyValue(modelView => modelView.IsLoading),
                observedLoadingStates.Add);

            var initialized = await viewModel.InitializeAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(initialized, Is.False);
                Assert.That(viewModel.RootNodes, Is.Empty);
                Assert.That(viewModel.SelectedNode, Is.Null);
                Assert.That(viewModel.IsLoaded, Is.False);
                Assert.That(viewModel.IsLoading, Is.False);
                Assert.That(viewModel.ErrorMessage, Is.EqualTo("Model load failed"));
                Assert.That(observedErrors, Is.EqualTo(ExpectedInitializationErrorMessages));
                Assert.That(observedLoadingStates, Is.EqualTo(ExpectedInitializationLoadingStates));
                modelLoaderService.Verify(x => x.LoadQuantitiesModel(), Times.Once);
            }
        }

        /// <summary>
        /// Verifies a missing root model becomes controlled Project Browser failure state.
        /// </summary>
        [Test]
        public async Task VerifyInitializeAsyncHandlesMissingRootModel()
        {
            var modelLoaderService = new Mock<IModelLoaderService>();
            modelLoaderService
                .Setup(x => x.LoadQuantitiesModel())
                .Returns((INamespace)null);
            using var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object, new ContextAwareService());

            var initialized = await viewModel.InitializeAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(initialized, Is.False);
                Assert.That(viewModel.RootNodes, Is.Empty);
                Assert.That(viewModel.SelectedNode, Is.Null);
                Assert.That(viewModel.IsLoaded, Is.False);
                Assert.That(viewModel.IsLoading, Is.False);
                Assert.That(viewModel.ErrorMessage, Is.EqualTo("The Quantities model is unavailable."));
                modelLoaderService.Verify(x => x.LoadQuantitiesModel(), Times.Once);
            }
        }

        /// <summary>
        /// Verifies repeated initialization does not reload an already loaded browser.
        /// </summary>
        [Test]
        public async Task VerifyInitializeAsyncReturnsEarlyWhenAlreadyLoaded()
        {
            var modelLoaderService = CreateModelLoader(CreateMinimalModel());
            using var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object, new ContextAwareService());

            var firstResult = await viewModel.InitializeAsync(CancellationToken.None);
            var secondResult = await viewModel.InitializeAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstResult, Is.True);
                Assert.That(secondResult, Is.False);
                Assert.That(viewModel.IsLoaded, Is.True);
                Assert.That(viewModel.IsLoading, Is.False);
                modelLoaderService.Verify(x => x.LoadQuantitiesModel(), Times.Once);
            }
        }

        /// <summary>
        /// Verifies a concurrent initialization attempt returns without starting another load.
        /// </summary>
        [Test]
        public async Task VerifyInitializeAsyncReturnsEarlyWhenAlreadyInitializing()
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
            using var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object, new ContextAwareService());

            var firstInitialization = viewModel.InitializeAsync(CancellationToken.None);

            try
            {
                Assert.That(loadStarted.Wait(TimeSpan.FromSeconds(10)), Is.True);
                var concurrentResult = await viewModel.InitializeAsync(CancellationToken.None);

                Assert.That(concurrentResult, Is.False);
            }
            finally
            {
                releaseLoad.Set();
            }

            Assert.That(await firstInitialization, Is.True);
            modelLoaderService.Verify(x => x.LoadQuantitiesModel(), Times.Once);
        }

        /// <summary>
        /// Verifies ordinary selection publishes the node's source element and projects it once.
        /// </summary>
        [Test]
        public async Task VerifySelectNodePublishesSourceElement()
        {
            var selectionService = new ContextAwareService();
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateMinimalModel()).Object,
                selectionService);
            await viewModel.InitializeAsync(CancellationToken.None);
            var node = viewModel.RootNodes[0].Children[0];

            viewModel.SelectNode(node);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectionService.SelectedElement, Is.SameAs(node.SourceElement));
                Assert.That(viewModel.SelectedNode, Is.SameAs(node));
            }
        }

        /// <summary>
        /// Verifies ordinary toggle changes only nodes with children.
        /// </summary>
        [Test]
        public async Task VerifyToggleNodeTogglesOnlyNodesWithChildren()
        {
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateMinimalModel()).Object,
                new ContextAwareService());
            await viewModel.InitializeAsync(CancellationToken.None);
            var rootNode = viewModel.RootNodes[0];
            var leafNode = rootNode.Children[0];

            viewModel.ToggleNode(rootNode);
            viewModel.ToggleNode(leafNode);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rootNode.IsExpanded, Is.False);
                Assert.That(leafNode.IsExpanded, Is.False);
            }
        }

        /// <summary>
        /// Verifies the default filter is inactive and exposes every canonical node.
        /// </summary>
        [Test]
        public async Task VerifyFilterDefaultsAreInactiveAndExposeCanonicalTree()
        {
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateFilterModel()).Object,
                new ContextAwareService());

            Assert.That(await viewModel.InitializeAsync(CancellationToken.None), Is.True);

            var rootNode = viewModel.RootNodes[0];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.FilterText, Is.Empty);
                Assert.That(viewModel.SelectedElementTypes, Is.Empty);
                Assert.That(viewModel.FilterPresentation.IsActive, Is.False);
                Assert.That(Flatten(rootNode).All(viewModel.FilterPresentation.IsVisible), Is.True);
            }
        }

        /// <summary>
        /// Verifies available Type choices come from distinct concrete non-relationship elements in the loaded model.
        /// </summary>
        [Test]
        public async Task VerifyInitializeAsyncDiscoversAvailableConcreteElementTypes()
        {
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateFilterModel()).Object,
                new ContextAwareService());

            Assert.That(await viewModel.InitializeAsync(CancellationToken.None), Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    viewModel.AvailableElementTypes,
                    Is.EquivalentTo(new[] { typeof(Namespace), typeof(PartDefinition), typeof(PartUsage) }));
                Assert.That(
                    viewModel.AvailableElementTypes.Count(type => type == typeof(PartDefinition)),
                    Is.EqualTo(1));
                Assert.That(viewModel.AvailableElementTypes, Does.Not.Contain(typeof(Documentation)));
                Assert.That(viewModel.AvailableElementTypes, Does.Not.Contain(typeof(Membership)));
                Assert.That(
                    Flatten(viewModel.RootNodes[0]).Any(node => node.SourceElement is Membership),
                    Is.True);
                Assert.That(((IList<Type>)viewModel.AvailableElementTypes).IsReadOnly, Is.True);
            }
        }

        /// <summary>
        /// Verifies that the committed text criterion is exposed as ordinary reactive state.
        /// </summary>
        [Test]
        public void VerifyFilterTextPublishesCurrentAndChangedValues()
        {
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateFilterModel()).Object,
                new ContextAwareService());
            var observedValues = new List<string>();
            using var subscription = System.ObservableExtensions.Subscribe(
                viewModel.WhenAnyValue(owner => owner.FilterText),
                observedValues.Add);

            viewModel.FilterText = "Deep target";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(observedValues, Is.EqualTo(new[] { string.Empty, "Deep target" }));
                Assert.That(viewModel.FilterPresentation.IsActive, Is.True);
            }
        }

        /// <summary>
        /// Verifies whitespace-only text is preserved as entered but remains an inactive criterion.
        /// </summary>
        [Test]
        public async Task VerifyFilterTextNormalizesNullAndTreatsWhitespaceAsInactive()
        {
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateFilterModel()).Object,
                new ContextAwareService());
            await viewModel.InitializeAsync(CancellationToken.None);
            var rootNode = viewModel.RootNodes[0];

            viewModel.FilterText = " \t ";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.FilterText, Is.EqualTo(" \t "));
                Assert.That(viewModel.FilterPresentation.IsActive, Is.False);
                Assert.That(Flatten(rootNode).All(viewModel.FilterPresentation.IsVisible), Is.True);
            }

            viewModel.FilterText = null;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.FilterText, Is.Empty);
                Assert.That(viewModel.FilterPresentation.IsActive, Is.False);
            }
        }

        /// <summary>
        /// Verifies display-name matching is an ordinal-ignore-case substring match with outer whitespace ignored.
        /// </summary>
        [Test]
        public async Task VerifyFilterTextMatchesDisplayNameIgnoringCaseAndOuterWhitespace()
        {
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateFilterModel()).Object,
                new ContextAwareService());
            await viewModel.InitializeAsync(CancellationToken.None);
            var rootNode = viewModel.RootNodes[0];
            var branchNode = FindNode(rootNode, "Subsystem alpha");
            var targetNode = FindNode(rootNode, "Deep target");

            viewModel.FilterText = "  DEEP Tar  ";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.FilterText, Is.EqualTo("  DEEP Tar  "));
                Assert.That(viewModel.FilterPresentation.IsActive, Is.True);
                Assert.That(viewModel.FilterPresentation.IsVisible(rootNode), Is.True);
                Assert.That(viewModel.FilterPresentation.IsVisible(branchNode), Is.True);
                Assert.That(viewModel.FilterPresentation.IsVisible(targetNode), Is.True);
            }
        }

        /// <summary>
        /// Verifies text filtering can match only the qualified name.
        /// </summary>
        [Test]
        public async Task VerifyFilterTextMatchesQualifiedName()
        {
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateQualifiedNameModel()).Object,
                new ContextAwareService());
            await viewModel.InitializeAsync(CancellationToken.None);
            var rootNode = viewModel.RootNodes[0];
            var qualifiedNameNode = FindNode(rootNode, "Friendly label");

            viewModel.FilterText = "qualifiedneedle";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(qualifiedNameNode.DisplayName, Does.Not.Contain("qualifiedneedle").IgnoreCase);
                Assert.That(viewModel.FilterPresentation.IsVisible(rootNode), Is.True);
                Assert.That(viewModel.FilterPresentation.IsVisible(qualifiedNameNode), Is.True);
            }
        }

        /// <summary>
        /// Verifies text filtering does not use the concrete runtime type metadata.
        /// </summary>
        [Test]
        public async Task VerifyFilterTextDoesNotMatchRuntimeTypeName()
        {
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateFilterModel()).Object,
                new ContextAwareService());
            await viewModel.InitializeAsync(CancellationToken.None);
            var rootNode = viewModel.RootNodes[0];
            var targetNode = FindNode(rootNode, "Deep target");

            viewModel.FilterText = targetNode.ElementType.Name;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(targetNode.ElementType.Name, Is.Not.Empty);
                Assert.That(viewModel.FilterPresentation.IsActive, Is.True);
                Assert.That(viewModel.FilterPresentation.IsVisible(rootNode), Is.False);
                Assert.That(viewModel.FilterPresentation.IsVisible(targetNode), Is.False);
            }
        }

        /// <summary>
        /// Verifies element-type filtering uses multi-select OR semantics and an empty all-types state.
        /// </summary>
        [Test]
        public async Task VerifyElementTypeFiltersSupportMultipleSelectionsAndEmptyAllTypes()
        {
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateFilterModel()).Object,
                new ContextAwareService());
            await viewModel.InitializeAsync(CancellationToken.None);
            var rootNode = viewModel.RootNodes[0];
            var partDefinitionNode = FindNode(rootNode, "Mystery element");
            var namespaceSibling = FindNode(rootNode, "Sibling branch");

            viewModel.ToggleElementTypeFilter(typeof(Namespace));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.FilterPresentation.IsActive, Is.True);
                Assert.That(viewModel.FilterPresentation.IsVisible(rootNode), Is.True);
                Assert.That(viewModel.FilterPresentation.IsVisible(namespaceSibling), Is.True);
                Assert.That(viewModel.FilterPresentation.IsVisible(partDefinitionNode), Is.False);
            }

            viewModel.ToggleElementTypeFilter(typeof(PartDefinition));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.FilterPresentation.IsActive, Is.True);
                Assert.That(viewModel.FilterPresentation.IsVisible(rootNode), Is.True);
                Assert.That(viewModel.FilterPresentation.IsVisible(partDefinitionNode), Is.True);
                Assert.That(viewModel.FilterPresentation.IsVisible(namespaceSibling), Is.True);
                Assert.That(
                    viewModel.SelectedElementTypes,
                    Is.EquivalentTo(new[] { typeof(Namespace), typeof(PartDefinition) }));
            }

            viewModel.ToggleElementTypeFilter(typeof(Namespace));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.FilterPresentation.IsActive, Is.True);
                Assert.That(viewModel.FilterPresentation.IsVisible(rootNode), Is.True);
                Assert.That(viewModel.FilterPresentation.IsVisible(partDefinitionNode), Is.True);
                Assert.That(viewModel.FilterPresentation.IsVisible(namespaceSibling), Is.False);
                Assert.That(viewModel.SelectedElementTypes, Is.EquivalentTo(new[] { typeof(PartDefinition) }));
            }

            viewModel.ToggleElementTypeFilter(typeof(PartDefinition));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.FilterPresentation.IsActive, Is.False);
                Assert.That(viewModel.SelectedElementTypes, Is.Empty);
                Assert.That(Flatten(rootNode).All(viewModel.FilterPresentation.IsVisible), Is.True);
            }
        }

        /// <summary>
        /// Verifies text and element-type criteria use AND semantics.
        /// </summary>
        [Test]
        public async Task VerifyTextAndElementTypeFiltersUseAndSemantics()
        {
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateFilterModel()).Object,
                new ContextAwareService());
            await viewModel.InitializeAsync(CancellationToken.None);
            var rootNode = viewModel.RootNodes[0];
            var targetNode = FindNode(rootNode, "Deep target");

            viewModel.FilterText = "Deep target";
            viewModel.ToggleElementTypeFilter(typeof(Namespace));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.FilterPresentation.IsActive, Is.True);
                Assert.That(viewModel.FilterPresentation.IsVisible(rootNode), Is.False);
                Assert.That(viewModel.FilterPresentation.IsVisible(targetNode), Is.False);
            }

            viewModel.ToggleElementTypeFilter(typeof(PartDefinition));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.FilterPresentation.IsVisible(rootNode), Is.True);
                Assert.That(viewModel.FilterPresentation.IsVisible(targetNode), Is.True);
            }

            viewModel.FilterText = string.Empty;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.FilterText, Is.Empty);
                Assert.That(viewModel.SelectedElementTypes, Has.Count.EqualTo(2));
                Assert.That(viewModel.SelectedElementTypes, Does.Contain(typeof(Namespace)));
                Assert.That(viewModel.SelectedElementTypes, Does.Contain(typeof(PartDefinition)));
                Assert.That(viewModel.FilterPresentation.IsActive, Is.True);
                Assert.That(viewModel.FilterPresentation.IsVisible(rootNode), Is.True);
                Assert.That(viewModel.FilterPresentation.IsVisible(targetNode), Is.True);
            }
        }

        /// <summary>
        /// Verifies deep matches retain only their ancestor chain while the canonical tree remains unchanged.
        /// </summary>
        [Test]
        public async Task VerifyFilterPresentationPreservesAncestorsAndExcludesUnrelatedBranches()
        {
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateFilterModel()).Object,
                new ContextAwareService());
            await viewModel.InitializeAsync(CancellationToken.None);
            var rootNode = viewModel.RootNodes[0];
            var canonicalChildren = rootNode.Children.ToArray();
            var branchNode = FindNode(rootNode, "Subsystem alpha");
            var targetNode = FindNode(rootNode, "Deep target");
            var unrelatedLeaf = FindNode(rootNode, "Unrelated leaf");
            var unrelatedSibling = FindNode(rootNode, "Sibling branch");

            viewModel.FilterText = "Deep target";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.FilterPresentation.IsVisible(rootNode), Is.True);
                Assert.That(viewModel.FilterPresentation.IsVisible(branchNode), Is.True);
                Assert.That(viewModel.FilterPresentation.IsVisible(targetNode), Is.True);
                Assert.That(viewModel.FilterPresentation.IsVisible(unrelatedLeaf), Is.False);
                Assert.That(viewModel.FilterPresentation.IsVisible(unrelatedSibling), Is.False);
                Assert.That(viewModel.RootNodes, Has.Count.EqualTo(1));
                Assert.That(rootNode.Children, Is.EqualTo(canonicalChildren));
            }
        }

        /// <summary>
        /// Verifies a direct parent match does not automatically expose nonmatching descendants.
        /// </summary>
        [Test]
        public async Task VerifyDirectMatchDoesNotExposeNonmatchingDescendants()
        {
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateFilterModel()).Object,
                new ContextAwareService());
            await viewModel.InitializeAsync(CancellationToken.None);
            var rootNode = viewModel.RootNodes[0];
            var branchNode = FindNode(rootNode, "Subsystem alpha");

            viewModel.FilterText = "Subsystem alpha";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.FilterPresentation.IsVisible(rootNode), Is.True);
                Assert.That(viewModel.FilterPresentation.IsVisible(branchNode), Is.True);
                Assert.That(branchNode.Children.All(viewModel.FilterPresentation.IsVisible), Is.False);
                Assert.That(branchNode.Children.Any(viewModel.FilterPresentation.IsVisible), Is.False);
            }
        }

        /// <summary>
        /// Verifies an active query with no matches publishes a valid empty presentation.
        /// </summary>
        [Test]
        public async Task VerifyFilterPresentationSupportsNoResults()
        {
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateFilterModel()).Object,
                new ContextAwareService());
            await viewModel.InitializeAsync(CancellationToken.None);

            viewModel.FilterText = "No element has this text";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.RootNodes, Has.Count.EqualTo(1));
                Assert.That(viewModel.FilterPresentation.IsActive, Is.True);
                Assert.That(viewModel.RootNodes.Any(viewModel.FilterPresentation.IsVisible), Is.False);
            }
        }

        /// <summary>
        /// Verifies filtering and guarded toggles never mutate durable expansion state, including after clear.
        /// </summary>
        [Test]
        public async Task VerifyFilteringPreservesDurableExpansionStateAcrossClear()
        {
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateFilterModel()).Object,
                new ContextAwareService());
            await viewModel.InitializeAsync(CancellationToken.None);
            var rootNode = viewModel.RootNodes[0];
            var branchNode = FindNode(rootNode, "Subsystem alpha");
            rootNode.IsExpanded = false;
            branchNode.IsExpanded = true;

            viewModel.FilterText = "Deep target";
            viewModel.ToggleNode(rootNode);
            viewModel.ToggleNode(branchNode);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rootNode.IsExpanded, Is.False);
                Assert.That(branchNode.IsExpanded, Is.True);
            }

            viewModel.ClearFilter();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.FilterText, Is.Empty);
                Assert.That(viewModel.SelectedElementTypes, Is.Empty);
                Assert.That(viewModel.FilterPresentation.IsActive, Is.False);
                Assert.That(rootNode.IsExpanded, Is.False);
                Assert.That(branchNode.IsExpanded, Is.True);
                Assert.That(Flatten(rootNode).All(viewModel.FilterPresentation.IsVisible), Is.True);
            }
        }

        /// <summary>
        /// Verifies clear resets reactive criteria and repeated clears are no-ops.
        /// </summary>
        [Test]
        public async Task VerifyClearFilterResetsReactiveCriteriaAndIsIdempotent()
        {
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateFilterModel()).Object,
                new ContextAwareService());
            await viewModel.InitializeAsync(CancellationToken.None);
            viewModel.FilterText = "Deep target";
            viewModel.ToggleElementTypeFilter(typeof(PartDefinition));
            var changedProperties = new List<string>();
            var selectedTypeChanges = 0;
            PropertyChangedEventHandler propertyHandler = (_, args) => changedProperties.Add(args.PropertyName);
            NotifyCollectionChangedEventHandler selectedTypeHandler = (_, _) => selectedTypeChanges++;
            viewModel.PropertyChanged += propertyHandler;
            ((INotifyCollectionChanged)viewModel.SelectedElementTypes).CollectionChanged += selectedTypeHandler;

            try
            {
                viewModel.ClearFilter();
                var changedPropertyCount = changedProperties.Count;
                var selectedTypeChangeCount = selectedTypeChanges;
                viewModel.ClearFilter();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(changedProperties, Has.Count.EqualTo(changedPropertyCount));
                    Assert.That(selectedTypeChanges, Is.EqualTo(selectedTypeChangeCount));
                }
            }
            finally
            {
                viewModel.PropertyChanged -= propertyHandler;
                ((INotifyCollectionChanged)viewModel.SelectedElementTypes).CollectionChanged -= selectedTypeHandler;
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.FilterText, Is.Empty);
                Assert.That(viewModel.SelectedElementTypes, Is.Empty);
                Assert.That(viewModel.FilterPresentation.IsActive, Is.False);
                Assert.That(changedProperties.Count(name => name == nameof(viewModel.FilterText)), Is.EqualTo(1));
                Assert.That(selectedTypeChanges, Is.EqualTo(1));
                Assert.That(changedProperties, Does.Not.Contain(nameof(viewModel.SelectedElementTypes)));
            }
        }

        /// <summary>
        /// Verifies equivalent matching semantics reuse the immutable presentation and exact repeated values are no-ops.
        /// </summary>
        [Test]
        public async Task VerifyEquivalentAndIdenticalFilterTextAvoidUnnecessaryPresentationPublication()
        {
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateFilterModel()).Object,
                new ContextAwareService());
            await viewModel.InitializeAsync(CancellationToken.None);
            viewModel.FilterText = "Deep target";
            var initialPresentation = viewModel.FilterPresentation;
            var changedProperties = new List<string>();
            PropertyChangedEventHandler propertyHandler = (_, args) => changedProperties.Add(args.PropertyName);
            viewModel.PropertyChanged += propertyHandler;

            try
            {
                viewModel.FilterText = "  DEEP TARGET  ";
                viewModel.FilterText = "  DEEP TARGET  ";
            }
            finally
            {
                viewModel.PropertyChanged -= propertyHandler;
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.FilterText, Is.EqualTo("  DEEP TARGET  "));
                Assert.That(viewModel.FilterPresentation, Is.SameAs(initialPresentation));
                Assert.That(changedProperties, Is.EqualTo(new[] { nameof(viewModel.FilterText) }));
            }
        }

        /// <summary>
        /// Verifies a filter set before initialization is applied before canonical roots are observed.
        /// </summary>
        [Test]
        public async Task VerifyInitializeAsyncAppliesPreexistingFilterCoherently()
        {
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateFilterModel()).Object,
                new ContextAwareService());
            viewModel.FilterText = "Deep target";
            var collectionPublicationWasCoherent = false;
            var notifyingRoots = (INotifyCollectionChanged)viewModel.RootNodes;
            NotifyCollectionChangedEventHandler collectionHandler = (_, _) =>
            {
                collectionPublicationWasCoherent = viewModel.RootNodes.Count == 1
                                                   && viewModel.FilterPresentation.IsActive
                                                   && viewModel.FilterPresentation.IsVisible(
                                                       FindNode(viewModel.RootNodes[0], "Deep target"))
                                                   && !viewModel.FilterPresentation.IsVisible(
                                                       FindNode(viewModel.RootNodes[0], "Sibling branch"));
            };
            notifyingRoots.CollectionChanged += collectionHandler;

            try
            {
                Assert.That(await viewModel.InitializeAsync(CancellationToken.None), Is.True);
            }
            finally
            {
                notifyingRoots.CollectionChanged -= collectionHandler;
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(collectionPublicationWasCoherent, Is.True);
                Assert.That(viewModel.RootNodes[0].Children, Has.Count.EqualTo(4));
                Assert.That(viewModel.FilterPresentation.IsActive, Is.True);
            }
        }

        /// <summary>
        /// Verifies an active no-results presentation is installed before newly published roots are observed.
        /// </summary>
        [Test]
        public async Task VerifyInitializeAsyncPublishesActiveEmptyPresentationCoherently()
        {
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateFilterModel()).Object,
                new ContextAwareService());
            viewModel.FilterText = "No matching element";
            var collectionPublicationWasCoherent = false;
            var notifyingRoots = (INotifyCollectionChanged)viewModel.RootNodes;
            NotifyCollectionChangedEventHandler collectionHandler = (_, _) =>
            {
                collectionPublicationWasCoherent = viewModel.RootNodes.Count == 1
                                                   && viewModel.FilterPresentation.IsActive
                                                   && !viewModel.FilterPresentation.IsVisible(
                                                       viewModel.RootNodes[0]);
            };
            notifyingRoots.CollectionChanged += collectionHandler;

            try
            {
                Assert.That(await viewModel.InitializeAsync(CancellationToken.None), Is.True);
            }
            finally
            {
                notifyingRoots.CollectionChanged -= collectionHandler;
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(collectionPublicationWasCoherent, Is.True);
                Assert.That(viewModel.RootNodes, Has.Count.EqualTo(1));
                Assert.That(viewModel.FilterPresentation.IsActive, Is.True);
            }
        }

        /// <summary>
        /// Verifies model-load failure retains a valid active no-results presentation for the empty canonical tree.
        /// </summary>
        [Test]
        public async Task VerifyInitializationErrorRetainsActiveEmptyFilterPresentation()
        {
            var modelLoaderService = new Mock<IModelLoaderService>();
            modelLoaderService
                .Setup(x => x.LoadQuantitiesModel())
                .Throws(new InvalidOperationException("Model load failed"));
            using var viewModel = new ProjectBrowserViewModel(
                modelLoaderService.Object,
                new ContextAwareService())
            {
                FilterText = "Deep target"
            };

            Assert.That(await viewModel.InitializeAsync(CancellationToken.None), Is.False);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.RootNodes, Is.Empty);
                Assert.That(viewModel.FilterPresentation.IsActive, Is.True);
                Assert.That(viewModel.ErrorMessage, Is.EqualTo("Model load failed"));
            }
        }

        /// <summary>
        /// Verifies filtering does not clear hidden selection and visible filtered selection still uses the shared service.
        /// </summary>
        [Test]
        public async Task VerifyFilteringPreservesHiddenSelectionAndVisibleSelectionFlow()
        {
            var selectionService = new ContextAwareService();
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateFilterModel()).Object,
                selectionService);
            await viewModel.InitializeAsync(CancellationToken.None);
            var rootNode = viewModel.RootNodes[0];
            var hiddenNode = FindNode(rootNode, "Sibling branch");
            var visibleNode = FindNode(rootNode, "Deep target");
            viewModel.SelectNode(hiddenNode);

            viewModel.FilterText = "Deep target";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.FilterPresentation.IsVisible(hiddenNode), Is.False);
                Assert.That(viewModel.SelectedNode, Is.SameAs(hiddenNode));
                Assert.That(selectionService.SelectedElement, Is.SameAs(hiddenNode.SourceElement));
            }

            viewModel.SelectNode(visibleNode);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.SelectedNode, Is.SameAs(visibleNode));
                Assert.That(selectionService.SelectedElement, Is.SameAs(visibleNode.SourceElement));
            }
        }

        /// <summary>
        /// Verifies each Project Browser ViewModel owns independent filter criteria and presentation state.
        /// </summary>
        [Test]
        public async Task VerifyProjectBrowserViewModelsKeepIndependentFilterState()
        {
            var model = CreateFilterModel();
            var modelLoaderService = CreateModelLoader(model);
            using var firstViewModel = new ProjectBrowserViewModel(
                modelLoaderService.Object,
                new ContextAwareService());
            using var secondViewModel = new ProjectBrowserViewModel(
                modelLoaderService.Object,
                new ContextAwareService());
            await firstViewModel.InitializeAsync(CancellationToken.None);
            await secondViewModel.InitializeAsync(CancellationToken.None);

            firstViewModel.FilterText = "Deep target";
            secondViewModel.ToggleElementTypeFilter(typeof(Namespace));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstViewModel.FilterText, Is.EqualTo("Deep target"));
                Assert.That(firstViewModel.SelectedElementTypes, Is.Empty);
                Assert.That(secondViewModel.FilterText, Is.Empty);
                Assert.That(
                    secondViewModel.SelectedElementTypes,
                    Is.EquivalentTo(new[] { typeof(Namespace) }));
                Assert.That(firstViewModel.FilterPresentation, Is.Not.SameAs(secondViewModel.FilterPresentation));
            }
        }

        /// <summary>
        /// Verifies repeated commit, remove, Type, and clear mutations never corrupt canonical browser state.
        /// </summary>
        [Test]
        public async Task VerifyFilterFlushAndRebuildStressPreservesCanonicalState()
        {
            const int iterationCount = 100;
            var selectionService = new ContextAwareService();
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateFilterModel()).Object,
                selectionService);
            await viewModel.InitializeAsync(CancellationToken.None);
            var rootNode = viewModel.RootNodes[0];
            var branchNode = FindNode(rootNode, "Subsystem alpha");
            var targetNode = FindNode(rootNode, "Deep target");
            var canonicalNodes = Flatten(rootNode).ToArray();
            var canonicalChildren = canonicalNodes.ToDictionary(
                node => node,
                node => node.Children.ToArray(),
                ReferenceEqualityComparer.Instance);
            rootNode.IsExpanded = false;
            branchNode.IsExpanded = true;
            viewModel.SelectNode(targetNode);

            for (var iteration = 0; iteration < iterationCount; iteration++)
            {
                viewModel.FilterText = "Deep target";
                viewModel.ToggleElementTypeFilter(typeof(PartDefinition));

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(viewModel.FilterPresentation.IsActive, Is.True, $"active commit at {iteration}");
                    Assert.That(viewModel.FilterPresentation.IsVisible(targetNode), Is.True, $"visible target at {iteration}");
                    Assert.That(viewModel.SelectedElementTypes, Has.Count.EqualTo(1), $"single Type at {iteration}");
                }

                viewModel.FilterText = string.Empty;
                viewModel.ToggleElementTypeFilter(typeof(PartDefinition));
                viewModel.ClearFilter();
                viewModel.ClearFilter();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(viewModel.FilterText, Is.Empty, $"cleared Contains at {iteration}");
                    Assert.That(viewModel.SelectedElementTypes, Is.Empty, $"cleared Types at {iteration}");
                    Assert.That(viewModel.FilterPresentation.IsActive, Is.False, $"inactive presentation at {iteration}");
                    Assert.That(Flatten(rootNode), Is.EqualTo(canonicalNodes), $"canonical nodes at {iteration}");
                    Assert.That(
                        canonicalNodes.All(node => node.Children.SequenceEqual(canonicalChildren[node])),
                        Is.True,
                        $"canonical children at {iteration}");
                    Assert.That(rootNode.IsExpanded, Is.False, $"root expansion at {iteration}");
                    Assert.That(branchNode.IsExpanded, Is.True, $"branch expansion at {iteration}");
                    Assert.That(viewModel.SelectedNode, Is.SameAs(targetNode), $"local selection at {iteration}");
                    Assert.That(selectionService.SelectedElement, Is.SameAs(targetNode.SourceElement), $"shared context at {iteration}");
                }
            }
        }

        /// <summary>
        /// Verifies repeated criteria and selection mutations remain isolated across five real browser ViewModels.
        /// </summary>
        [Test]
        public async Task VerifyMultipleProjectBrowserFilterStressRemainsIsolated()
        {
            const int iterationCount = 50;
            const int browserCount = 5;
            var selectionService = new ContextAwareService();
            var modelLoaderService = CreateModelLoader(CreateFilterModel());
            var viewModels = Enumerable.Range(0, browserCount)
                .Select(_ => new ProjectBrowserViewModel(modelLoaderService.Object, selectionService))
                .ToArray();

            try
            {
                foreach (var viewModel in viewModels)
                {
                    await viewModel.InitializeAsync(CancellationToken.None);
                }

                for (var iteration = 0; iteration < iterationCount; iteration++)
                {
                    var expectedFilterTexts = Enumerable.Repeat(string.Empty, viewModels.Length).ToArray();

                    for (var browserIndex = 0; browserIndex < viewModels.Length; browserIndex++)
                    {
                        var viewModel = viewModels[browserIndex];
                        var criterion = $"browser-{browserIndex}-iteration-{iteration}";
                        var selectedType = browserIndex % 2 == 0
                            ? typeof(PartDefinition)
                            : typeof(Namespace);
                        viewModel.FilterText = criterion;
                        viewModel.ToggleElementTypeFilter(selectedType);
                        expectedFilterTexts[browserIndex] = criterion;

                        Assert.That(
                            viewModels.Select(candidate => candidate.FilterText),
                            Is.EqualTo(expectedFilterTexts),
                            $"filter isolation at iteration {iteration}, browser {browserIndex}");
                    }

                    for (var browserIndex = 0; browserIndex < viewModels.Length; browserIndex++)
                    {
                        var selectingViewModel = viewModels[browserIndex];
                        var targetNode = FindNode(selectingViewModel.RootNodes[0], "Deep target");
                        var otherSelections = viewModels
                            .Where(viewModel => !ReferenceEquals(viewModel, selectingViewModel))
                            .ToDictionary(viewModel => viewModel, viewModel => viewModel.SelectedNode);

                        selectingViewModel.SelectNode(targetNode);

                        using (Assert.EnterMultipleScope())
                        {
                            Assert.That(selectionService.SelectedElement, Is.SameAs(targetNode.SourceElement));
                            Assert.That(selectingViewModel.SelectedNode, Is.SameAs(targetNode));
                            Assert.That(
                                otherSelections.All(pair => ReferenceEquals(pair.Key.SelectedNode, pair.Value)),
                                Is.True,
                                $"selection isolation at iteration {iteration}, browser {browserIndex}");
                        }
                    }

                    foreach (var viewModel in viewModels)
                    {
                        viewModel.ClearFilter();

                        using (Assert.EnterMultipleScope())
                        {
                            Assert.That(viewModel.FilterText, Is.Empty);
                            Assert.That(viewModel.SelectedElementTypes, Is.Empty);
                            Assert.That(viewModel.FilterPresentation.IsActive, Is.False);
                        }
                    }
                }
            }
            finally
            {
                foreach (var viewModel in viewModels)
                {
                    viewModel.Dispose();
                }
            }
        }

        /// <summary>
        /// Verifies filter setters and clear do not publish after final disposal.
        /// </summary>
        [Test]
        public void VerifyFilterMethodsDoNotMutateOrPublishAfterDisposal()
        {
            var viewModel = new ProjectBrowserViewModel(
                new Mock<IModelLoaderService>().Object,
                new ContextAwareService());
            var propertyNotifications = new List<string>();
            PropertyChangedEventHandler propertyHandler = (_, args) => propertyNotifications.Add(args.PropertyName);
            viewModel.PropertyChanged += propertyHandler;
            viewModel.Dispose();

            try
            {
                viewModel.FilterText = "Deep target";
                viewModel.ToggleElementTypeFilter(typeof(PartDefinition));
                viewModel.ClearFilter();
            }
            finally
            {
                viewModel.PropertyChanged -= propertyHandler;
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.FilterText, Is.Empty);
                Assert.That(viewModel.SelectedElementTypes, Is.Empty);
                Assert.That(viewModel.FilterPresentation.IsActive, Is.False);
                Assert.That(propertyNotifications, Is.Empty);
            }
        }

        /// <summary>
        /// Verifies ordinary methods preserve their null argument validation.
        /// </summary>
        [Test]
        public void VerifyNodeMethodsRejectNullNodes()
        {
            using var viewModel = new ProjectBrowserViewModel(
                new Mock<IModelLoaderService>().Object,
                new ContextAwareService());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(() => viewModel.ToggleNode(null), Throws.ArgumentNullException);
                Assert.That(() => viewModel.SelectNode(null), Throws.ArgumentNullException);
            }
        }

        /// <summary>
        /// Verifies Type filter state accepts only available non-relationship element types.
        /// </summary>
        [Test]
        public async Task VerifyToggleElementTypeFilterRejectsUnavailableTypes()
        {
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateFilterModel()).Object,
                new ContextAwareService());
            await viewModel.InitializeAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(() => viewModel.ToggleElementTypeFilter(null), Throws.ArgumentNullException);
                Assert.That(
                    () => viewModel.ToggleElementTypeFilter(typeof(Documentation)),
                    Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(
                    () => viewModel.ToggleElementTypeFilter(typeof(Membership)),
                    Throws.TypeOf<ArgumentOutOfRangeException>());
            }
        }

        /// <summary>
        /// Verifies workspace selection does not replace this browser's local visual selection.
        /// </summary>
        [Test]
        public async Task VerifyExternalSelectionDoesNotChangeLocalVisualSelection()
        {
            var selectionService = new ContextAwareService();
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateMinimalModel()).Object,
                selectionService);
            await viewModel.InitializeAsync(CancellationToken.None);
            var rootNode = viewModel.RootNodes[0];
            var node = viewModel.RootNodes[0].Children[0];

            selectionService.SelectedElement = node.SourceElement;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectionService.SelectedElement, Is.SameAs(node.SourceElement));
                Assert.That(viewModel.SelectedNode, Is.SameAs(rootNode));
            }
        }

        /// <summary>
        /// Verifies clearing workspace selection does not clear this browser's local selection.
        /// </summary>
        [Test]
        public async Task VerifyExternalClearSelectionPreservesLocalVisualSelection()
        {
            var selectionService = new ContextAwareService();
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateMinimalModel()).Object,
                selectionService);
            await viewModel.InitializeAsync(CancellationToken.None);
            var node = viewModel.RootNodes[0].Children[0];

            viewModel.SelectNode(node);
            selectionService.SelectedElement = null;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectionService.SelectedElement, Is.Null);
                Assert.That(viewModel.SelectedNode, Is.SameAs(node));
            }
        }

        /// <summary>
        /// Verifies final disposal preserves both local presentation and independently changing workspace context.
        /// </summary>
        [Test]
        public async Task VerifyDisposedViewModelPreservesLocalSelectionAndSharedContext()
        {
            var selectionService = new ContextAwareService();
            var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateMinimalModel()).Object,
                selectionService);
            await viewModel.InitializeAsync(CancellationToken.None);
            var node = viewModel.RootNodes[0].Children[0];
            viewModel.SelectNode(node);
            var externalElement = new Namespace { ElementId = "external" };

            viewModel.Dispose();
            selectionService.SelectedElement = externalElement;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectionService.SelectedElement, Is.SameAs(externalElement));
                Assert.That(viewModel.SelectedNode, Is.SameAs(node));
            }
        }

        /// <summary>
        /// Verifies two Project Browsers keep local selection while publishing shared workspace context.
        /// </summary>
        [Test]
        public async Task VerifyTwoViewModelsKeepIndependentLocalSelectionAndSharedContext()
        {
            var model = CreateSelectionModel();
            var selectionService = new ContextAwareService();
            var modelLoaderService = CreateModelLoader(model);
            using var firstViewModel = new ProjectBrowserViewModel(modelLoaderService.Object, selectionService);
            using var secondViewModel = new ProjectBrowserViewModel(modelLoaderService.Object, selectionService);

            await Task.WhenAll(
                firstViewModel.InitializeAsync(CancellationToken.None),
                secondViewModel.InitializeAsync(CancellationToken.None));

            var firstRootNode = firstViewModel.RootNodes[0];
            var secondRootNode = secondViewModel.RootNodes[0];
            var firstThrusterNode = firstRootNode.Children.Single(node => node.DisplayName == "Thruster");
            var secondThrusterNode = secondRootNode.Children.Single(node => node.DisplayName == "Thruster");
            var firstTankNode = firstRootNode.Children.Single(node => node.DisplayName == "Tank");
            var secondTankNode = secondRootNode.Children.Single(node => node.DisplayName == "Tank");

            firstViewModel.SelectNode(firstThrusterNode);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstThrusterNode, Is.Not.SameAs(secondThrusterNode));
                Assert.That(firstTankNode, Is.Not.SameAs(secondTankNode));
                Assert.That(firstThrusterNode.SourceElement, Is.SameAs(secondThrusterNode.SourceElement));
                Assert.That(firstViewModel.SelectedNode, Is.SameAs(firstThrusterNode));
                Assert.That(selectionService.SelectedElement, Is.SameAs(firstThrusterNode.SourceElement));
                Assert.That(secondViewModel.SelectedNode, Is.SameAs(secondRootNode));
            }

            secondViewModel.SelectNode(secondTankNode);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(secondViewModel.SelectedNode, Is.SameAs(secondTankNode));
                Assert.That(selectionService.SelectedElement, Is.SameAs(secondTankNode.SourceElement));
                Assert.That(firstViewModel.SelectedNode, Is.SameAs(firstThrusterNode));
            }
        }

        /// <summary>
        /// Verifies initialization applies a local default without overwriting external workspace context.
        /// </summary>
        [Test]
        public async Task VerifyInitializeAsyncSelectsLocalDefaultAndPreservesExternalContext()
        {
            var externalElement = new Namespace { ElementId = "external" };
            var selectionService = new ContextAwareService
            {
                SelectedElement = externalElement
            };
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateMinimalModel()).Object,
                selectionService);

            var initialized = await viewModel.InitializeAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(initialized, Is.True);
                Assert.That(selectionService.SelectedElement, Is.SameAs(externalElement));
                Assert.That(viewModel.RootNodes, Has.Count.EqualTo(1));
                Assert.That(viewModel.SelectedNode, Is.SameAs(viewModel.RootNodes[0]));
                Assert.That(viewModel.RootNodes[0].IsExpanded, Is.True);
                Assert.That(viewModel.IsLoaded, Is.True);
            }
        }

        /// <summary>
        /// Verifies initialization does not project a matching workspace selection into local presentation.
        /// </summary>
        [Test]
        public async Task VerifyInitializeAsyncDoesNotProjectExistingWorkspaceSelection()
        {
            var model = CreateSelectionModel();
            var thrusterElement = model.ownedElement.Single(element => element.ElementId == "thruster");
            var selectionService = new ContextAwareService
            {
                SelectedElement = thrusterElement
            };
            using var viewModel = new ProjectBrowserViewModel(CreateModelLoader(model).Object, selectionService);

            var initialized = await viewModel.InitializeAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(initialized, Is.True);
                Assert.That(viewModel.SelectedNode, Is.SameAs(viewModel.RootNodes[0]));
                Assert.That(viewModel.RootNodes[0].IsExpanded, Is.True);
                Assert.That(selectionService.SelectedElement, Is.SameAs(thrusterElement));
            }
        }

        /// <summary>
        /// Verifies a background browser completing initialization does not replace another browser's selection.
        /// </summary>
        [Test]
        public async Task VerifyBackgroundInitializationPreservesLocalAndSharedSelection()
        {
            using var loadStarted = new ManualResetEventSlim();
            using var releaseLoad = new ManualResetEventSlim();
            var model = CreateSelectionModel();
            var backgroundModelLoaderService = new Mock<IModelLoaderService>();
            backgroundModelLoaderService
                .Setup(service => service.LoadQuantitiesModel())
                .Returns(() =>
                {
                    loadStarted.Set();

                    if (!releaseLoad.Wait(TimeSpan.FromSeconds(10)))
                    {
                        throw new TimeoutException("The test did not release background model loading.");
                    }

                    return model;
                });
            var selectionService = new ContextAwareService();
            using var foregroundViewModel = new ProjectBrowserViewModel(
                CreateModelLoader(model).Object,
                selectionService);
            using var backgroundViewModel = new ProjectBrowserViewModel(
                backgroundModelLoaderService.Object,
                selectionService);
            var backgroundInitialization = backgroundViewModel.InitializeAsync(CancellationToken.None);

            try
            {
                var backgroundLoadStarted = loadStarted.Wait(TimeSpan.FromSeconds(10));
                var foregroundInitialized = await foregroundViewModel.InitializeAsync(CancellationToken.None);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(backgroundLoadStarted, Is.True);
                    Assert.That(foregroundInitialized, Is.True);
                }

                var foregroundThrusterNode = foregroundViewModel.RootNodes[0].Children
                    .Single(node => node.DisplayName == "Thruster");
                foregroundViewModel.SelectNode(foregroundThrusterNode);

                releaseLoad.Set();
                var backgroundInitialized = await backgroundInitialization;
                var backgroundRootNode = backgroundViewModel.RootNodes[0];
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(backgroundInitialized, Is.True);
                    Assert.That(selectionService.SelectedElement, Is.SameAs(foregroundThrusterNode.SourceElement));
                    Assert.That(foregroundViewModel.SelectedNode, Is.SameAs(foregroundThrusterNode));
                    Assert.That(backgroundViewModel.SelectedNode, Is.SameAs(backgroundRootNode));
                }
            }
            finally
            {
                releaseLoad.Set();
            }
        }

        /// <summary>
        /// Verifies roots remain read-only, stable, and intrinsically ordered.
        /// </summary>
        [Test]
        public async Task VerifyInitializeAsyncBindsOrderedRootsIntoReadOnlyCollection()
        {
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateMinimalModel()).Object,
                new ContextAwareService());
            var exposedRoots = viewModel.RootNodes;

            await viewModel.InitializeAsync(CancellationToken.None);

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
        /// Verifies SourceList publication uses collection changes without replacing or notifying the root property.
        /// </summary>
        [Test]
        public async Task VerifyInitializeAsyncPublishesRootCollectionChanges()
        {
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateMinimalModel()).Object,
                new ContextAwareService());
            var exposedRoots = viewModel.RootNodes;
            var collectionChanges = new List<NotifyCollectionChangedEventArgs>();
            var rootPropertyChanges = new List<string>();
            var notifyingCollection = (INotifyCollectionChanged)exposedRoots;
            NotifyCollectionChangedEventHandler collectionHandler = (_, args) => collectionChanges.Add(args);
            PropertyChangedEventHandler propertyHandler = (_, args) => rootPropertyChanges.Add(args.PropertyName);
            notifyingCollection.CollectionChanged += collectionHandler;
            viewModel.PropertyChanged += propertyHandler;

            try
            {
                await viewModel.InitializeAsync(CancellationToken.None);
            }
            finally
            {
                notifyingCollection.CollectionChanged -= collectionHandler;
                viewModel.PropertyChanged -= propertyHandler;
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(collectionChanges, Is.Not.Empty);
                Assert.That(viewModel.RootNodes, Is.SameAs(exposedRoots));
                Assert.That(exposedRoots, Has.Count.EqualTo(1));
                Assert.That(
                    exposedRoots[0].Children.Select(node => node.DisplayName),
                    Is.EqualTo(ExpectedRootChildDisplayNames));
                Assert.That(rootPropertyChanges, Does.Not.Contain(nameof(ProjectBrowserViewModel.RootNodes)));
            }
        }

        /// <summary>
        /// Verifies one local selection produces one local selection notification.
        /// </summary>
        [Test]
        public async Task VerifySelectNodeDoesNotDuplicateLocalSelectionNotification()
        {
            var selectionService = new ContextAwareService();
            using var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateMinimalModel()).Object,
                selectionService);
            await viewModel.InitializeAsync(CancellationToken.None);
            var observedSelections = new List<ProjectBrowserNodeViewModel>();

            using var selectionSubscription = System.ObservableExtensions.Subscribe(
                viewModel.WhenAnyValue(modelView => modelView.SelectedNode),
                observedSelections.Add);
            var notificationCountBeforeSelection = observedSelections.Count;
            var node = viewModel.RootNodes[0].Children[0];

            viewModel.SelectNode(node);
            viewModel.SelectNode(node);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.SelectedNode, Is.SameAs(node));
                Assert.That(observedSelections, Has.Count.EqualTo(notificationCountBeforeSelection + 1));
                Assert.That(observedSelections[observedSelections.Count - 1], Is.SameAs(node));
            }
        }

        /// <summary>
        /// Verifies ordinary initialization reactively reports loading and loaded state.
        /// </summary>
        [Test]
        public async Task VerifyInitializeAsyncNotifiesLoadingAndLoadedState()
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
                new ContextAwareService());
            var observedLoadingStates = new List<bool>();
            var observedLoadedStates = new List<bool>();

            using var loadingSubscription = System.ObservableExtensions.Subscribe(
                viewModel.WhenAnyValue(modelView => modelView.IsLoading),
                observedLoadingStates.Add);
            using var loadedSubscription = System.ObservableExtensions.Subscribe(
                viewModel.WhenAnyValue(modelView => modelView.IsLoaded),
                observedLoadedStates.Add);

            var initialization = viewModel.InitializeAsync(CancellationToken.None);

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

            Assert.That(await initialization, Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(observedLoadingStates, Is.EqualTo(ExpectedInitializationLoadingStates));
                Assert.That(observedLoadedStates, Is.EqualTo(ExpectedInitializationLoadedStates));
                Assert.That(viewModel.IsLoaded, Is.True);
                Assert.That(viewModel.ErrorMessage, Is.Empty);
            }
        }

        /// <summary>
        /// Verifies caller cancellation is non-error and quarantines a late synchronous loader result.
        /// </summary>
        [Test]
        public async Task VerifyInitializeAsyncCancellationIsNonErrorAndQuarantinesResult()
        {
            using var loadStarted = new ManualResetEventSlim();
            using var releaseLoad = new ManualResetEventSlim();
            using var loadFinished = new ManualResetEventSlim();
            using var cancellation = new CancellationTokenSource();
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

                    loadFinished.Set();

                    return CreateMinimalModel();
                });
            var selectionService = new ContextAwareService();
            using var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object, selectionService);
            var initialization = viewModel.InitializeAsync(cancellation.Token);

            Assert.That(loadStarted.Wait(TimeSpan.FromSeconds(10)), Is.True);
            await cancellation.CancelAsync();

            var initialized = await initialization;
            releaseLoad.Set();
            Assert.That(loadFinished.Wait(TimeSpan.FromSeconds(10)), Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(initialized, Is.False);
                Assert.That(selectionService.SelectedElement, Is.Null);
                Assert.That(viewModel.RootNodes, Is.Empty);
                Assert.That(viewModel.SelectedNode, Is.Null);
                Assert.That(viewModel.IsLoaded, Is.False);
                Assert.That(viewModel.IsLoading, Is.False);
                Assert.That(viewModel.ErrorMessage, Is.Empty);
            }
        }

        /// <summary>
        /// Verifies disposal during a blocked load quarantines its result and emits no later state changes.
        /// </summary>
        [Test]
        public async Task VerifyDisposeCancelsInFlightInitializationAndQuarantinesResult()
        {
            using var loadStarted = new ManualResetEventSlim();
            using var releaseLoad = new ManualResetEventSlim();
            using var loadFinished = new ManualResetEventSlim();
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

                    loadFinished.Set();

                    return CreateMinimalModel();
                });
            var selectionService = new ContextAwareService();
            var viewModel = new ProjectBrowserViewModel(modelLoaderService.Object, selectionService);
            var observedState = new List<(bool IsLoading, bool IsLoaded, string ErrorMessage)>();
            using var stateSubscription = System.ObservableExtensions.Subscribe(
                viewModel.WhenAnyValue(
                    modelView => modelView.IsLoading,
                    modelView => modelView.IsLoaded,
                    modelView => modelView.ErrorMessage,
                    (isLoading, isLoaded, errorMessage) => (isLoading, isLoaded, errorMessage)),
                observedState.Add);
            var initialization = viewModel.InitializeAsync(CancellationToken.None);

            Assert.That(loadStarted.Wait(TimeSpan.FromSeconds(10)), Is.True);
            viewModel.Dispose();
            var stateCountAfterDisposal = observedState.Count;
            var initialized = await initialization;
            releaseLoad.Set();
            Assert.That(loadFinished.Wait(TimeSpan.FromSeconds(10)), Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(initialized, Is.False);
                Assert.That(observedState, Has.Count.EqualTo(stateCountAfterDisposal));
                Assert.That(selectionService.SelectedElement, Is.Null);
                Assert.That(viewModel.RootNodes, Is.Empty);
                Assert.That(viewModel.SelectedNode, Is.Null);
                Assert.That(viewModel.IsLoaded, Is.False);
                Assert.That(viewModel.IsLoading, Is.True);
                Assert.That(viewModel.ErrorMessage, Is.Empty);
                modelLoaderService.Verify(x => x.LoadQuantitiesModel(), Times.Once);
            }
        }

        /// <summary>
        /// Verifies final disposal is idempotent and does not clear scoped selection.
        /// </summary>
        [Test]
        public async Task VerifyDisposeIsIdempotentAndPreservesSharedSelection()
        {
            var selectionService = new ContextAwareService();
            var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateMinimalModel()).Object,
                selectionService);
            await viewModel.InitializeAsync(CancellationToken.None);
            viewModel.SelectNode(viewModel.RootNodes[0].Children[0]);
            var selectedElement = selectionService.SelectedElement;

            Assert.That(viewModel.Dispose, Throws.Nothing);
            Assert.That(viewModel.Dispose, Throws.Nothing);

            Assert.That(selectionService.SelectedElement, Is.SameAs(selectedElement));
        }

        /// <summary>
        /// Verifies ordinary methods do not mutate state after final disposal.
        /// </summary>
        [Test]
        public async Task VerifyMethodsDoNotMutateStateAfterDisposal()
        {
            var selectionService = new ContextAwareService();
            var viewModel = new ProjectBrowserViewModel(
                CreateModelLoader(CreateMinimalModel()).Object,
                selectionService);
            await viewModel.InitializeAsync(CancellationToken.None);
            var rootNode = viewModel.RootNodes[0];
            var childNode = rootNode.Children[0];
            var selectedElement = selectionService.SelectedElement;
            var wasExpanded = rootNode.IsExpanded;

            viewModel.Dispose();
            viewModel.ToggleNode(rootNode);
            viewModel.SelectNode(childNode);
            viewModel.FocusElement(childNode.SourceElement);
            var initialized = await viewModel.InitializeAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(initialized, Is.False);
                Assert.That(rootNode.IsExpanded, Is.EqualTo(wasExpanded));
                Assert.That(selectionService.SelectedElement, Is.SameAs(selectedElement));
                Assert.That(viewModel.SelectedNode, Is.SameAs(rootNode));
            }
        }

        /// <summary>
        /// Verifies root collection publication observes an already coherent local selection.
        /// </summary>
        [Test]
        public async Task VerifyRootPublicationNotifiesOnlyAfterLocalSelectionIsCoherent()
        {
            var model = CreateMinimalModel();
            var workspaceElement = new Namespace { ElementId = "workspace-selection" };
            var selectionService = new ContextAwareService
            {
                SelectedElement = workspaceElement
            };
            using var viewModel = new ProjectBrowserViewModel(CreateModelLoader(model).Object, selectionService);
            var coherentPublicationObserved = false;
            ((INotifyCollectionChanged)viewModel.RootNodes).CollectionChanged += (_, _) =>
            {
                coherentPublicationObserved = viewModel.RootNodes.Count == 1
                                              && ReferenceEquals(
                                                  viewModel.SelectedNode,
                                                  viewModel.RootNodes[0]);
            };

            Assert.That(await viewModel.InitializeAsync(CancellationToken.None), Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(coherentPublicationObserved, Is.True);
                Assert.That(viewModel.SelectedNode, Is.SameAs(viewModel.RootNodes[0]));
                Assert.That(selectionService.SelectedElement, Is.SameAs(workspaceElement));
            }
        }

        /// <summary>
        /// Verifies URL focus received before initialization resolves after the canonical tree is published.
        /// </summary>
        [Test]
        public async Task VerifyFocusElementBeforeInitializationRestoresLocalSelectionAndAncestors()
        {
            var model = CreateFilterModel();
            var matchingBranch = model.ownedElement.Single(element => element.ElementId == "matching-branch");
            var deepTarget = matchingBranch.ownedElement.Single(element => element.ElementId == "deep-target");
            var sharedSelection = new Namespace { ElementId = "shared-selection" };
            var selectionService = new ContextAwareService { SelectedElement = sharedSelection };
            using var viewModel = new ProjectBrowserViewModel(CreateModelLoader(model).Object, selectionService);

            viewModel.FocusElement(deepTarget);
            var initialized = await viewModel.InitializeAsync(CancellationToken.None);
            var rootNode = viewModel.RootNodes[0];
            var branchNode = rootNode.Children.Single(node => node.ElementId == "matching-branch");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(initialized, Is.True);
                Assert.That(viewModel.SelectedNode.ElementId, Is.EqualTo("deep-target"));
                Assert.That(rootNode.IsExpanded, Is.True);
                Assert.That(branchNode.IsExpanded, Is.True);
                Assert.That(selectionService.SelectedElement, Is.SameAs(sharedSelection));
            }
        }

        /// <summary>
        /// Verifies local URL focus does not mutate text, type, or visibility filter authority.
        /// </summary>
        [Test]
        public async Task VerifyFocusElementPreservesActiveFilterState()
        {
            var model = CreateFilterModel();
            var matchingBranch = model.ownedElement.Single(element => element.ElementId == "matching-branch");
            var deepTarget = matchingBranch.ownedElement.Single(element => element.ElementId == "deep-target");
            var selectionService = new ContextAwareService();
            using var viewModel = new ProjectBrowserViewModel(CreateModelLoader(model).Object, selectionService);
            await viewModel.InitializeAsync(CancellationToken.None);
            viewModel.FilterText = "Sibling branch";
            viewModel.ToggleElementTypeFilter(typeof(PartUsage));

            viewModel.FocusElement(deepTarget);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.SelectedNode.ElementId, Is.EqualTo("deep-target"));
                Assert.That(viewModel.FilterText, Is.EqualTo("Sibling branch"));
                Assert.That(viewModel.SelectedElementTypes, Is.EqualTo(new[] { typeof(PartUsage) }));
                Assert.That(viewModel.FilterPresentation.IsVisible(viewModel.SelectedNode), Is.False);
                Assert.That(selectionService.SelectedElement, Is.Null);
            }
        }

        /// <summary>
        /// Verifies URL focus affects only the explicitly targeted local Project Browser instance.
        /// </summary>
        [Test]
        public async Task VerifyFocusElementPreservesOtherBrowserLocalSelection()
        {
            var model = CreateSelectionModel();
            var thruster = model.ownedElement.Single(element => element.ElementId == "thruster");
            var sharedSelection = new ContextAwareService();
            using var first = new ProjectBrowserViewModel(CreateModelLoader(model).Object, sharedSelection);
            using var second = new ProjectBrowserViewModel(CreateModelLoader(model).Object, sharedSelection);
            await first.InitializeAsync(CancellationToken.None);
            await second.InitializeAsync(CancellationToken.None);
            var secondTankNode = second.RootNodes[0].Children.Single(node => node.ElementId == "tank");
            second.SelectNode(secondTankNode);

            first.FocusElement(thruster);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.SelectedNode.ElementId, Is.EqualTo("thruster"));
                Assert.That(second.SelectedNode, Is.SameAs(secondTankNode));
                Assert.That(sharedSelection.SelectedElement, Is.SameAs(secondTankNode.SourceElement));
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
        /// Creates a model with two elements used to prove independent local browser selection.
        /// </summary>
        /// <returns>The selection-focused namespace model.</returns>
        private static Namespace CreateSelectionModel()
        {
            var thruster = CreateElement<PartDefinition>("thruster", "Thruster");
            var tank = CreateElement<PartUsage>("tank", "Tank");

            return CreateElement<Namespace>("root", "Root", thruster, tank);
        }

        /// <summary>
        /// Creates a small model with two selectable children.
        /// </summary>
        /// <returns>The minimal namespace model.</returns>
        private static Namespace CreateMinimalModel()
        {
            var firstChild = CreateElement<PartDefinition>("first-child", "First child");
            var secondChild = CreateElement<PartUsage>("second-child", "Second child");

            return CreateElement<Namespace>("root", "Root", firstChild, secondChild);
        }

        /// <summary>
        /// Creates a canonical hierarchy with concrete definition, usage, namespace, and relationship nodes.
        /// </summary>
        /// <returns>The filter-focused namespace model.</returns>
        private static Namespace CreateFilterModel()
        {
            var deepTarget = CreateElement<PartDefinition>(
                "deep-target",
                "Deep target");
            var unrelatedLeaf = CreateElement<PartUsage>(
                "unrelated-leaf",
                "Unrelated leaf");
            var matchingBranch = CreateElement<Namespace>(
                "matching-branch",
                "Subsystem alpha",
                deepTarget,
                unrelatedLeaf);
            var siblingBranch = CreateElement<Namespace>(
                "sibling-branch",
                "Sibling branch");
            var duplicateDefinition = CreateElement<PartDefinition>(
                "duplicate-definition",
                "Mystery element");
            var relationship = CreateElement<Membership>(
                "membership",
                "Owned membership");

            return CreateElement<Namespace>(
                "filter-root",
                "Root project",
                matchingBranch,
                siblingBranch,
                duplicateDefinition,
                relationship);
        }

        /// <summary>
        /// Creates a model whose child matches only through qualified-name metadata.
        /// </summary>
        /// <returns>The qualified-name-focused namespace model.</returns>
        private static Namespace CreateQualifiedNameModel()
        {
            var qualifiedNameOnly = CreateElement<QualifiedNamePartDefinition>(
                "qualified-name-only",
                "Friendly label");

            return CreateElement<Namespace>("qualified-root", "Root project", qualifiedNameOnly);
        }

        /// <summary>
        /// Provides deterministic qualified-name metadata while retaining SDK containment behavior.
        /// </summary>
        private sealed class QualifiedNamePartDefinition : PartDefinition, IElement
        {
            /// <summary>
            /// Gets the qualified name used by the qualified-name-only filter scenario.
            /// </summary>
            string IElement.qualifiedName => "Model::QualifiedNeedle";
        }

        /// <summary>
        /// Finds one canonical node by its display name.
        /// </summary>
        /// <param name="rootNode">The root node to search.</param>
        /// <param name="displayName">The exact display name.</param>
        /// <returns>The matching canonical node.</returns>
        private static ProjectBrowserNodeViewModel FindNode(
            ProjectBrowserNodeViewModel rootNode,
            string displayName)
        {
            return Flatten(rootNode).Single(node => node.DisplayName == displayName);
        }

        /// <summary>
        /// Enumerates a canonical tree in pre-order.
        /// </summary>
        /// <param name="rootNode">The root node.</param>
        /// <returns>The root and every descendant in canonical order.</returns>
        private static IEnumerable<ProjectBrowserNodeViewModel> Flatten(ProjectBrowserNodeViewModel rootNode)
        {
            yield return rootNode;

            foreach (var childNode in rootNode.Children)
            {
                foreach (var descendantNode in Flatten(childNode))
                {
                    yield return descendantNode;
                }
            }
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
