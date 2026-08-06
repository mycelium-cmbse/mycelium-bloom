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
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Threading.Tasks;

    using Bunit;

    using Microsoft.Extensions.DependencyInjection;

    using Moq;

    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using ReactiveUI;

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
            using var context = new ProjectBrowserViewModelMockContext();
            context.SetIsLoading(true);
            this.RegisterViewModel(context);

            using var component = this.Render<ProjectBrowserComponent>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Markup, Does.Contain("Loading Quantities model"));
                Assert.That(component.Markup, Does.Contain("Preparing the SysML project browser..."));
                Assert.That(component.Markup, Does.Contain("mb-project-browser__state"));
                Assert.That(context.InitializeCallCount, Is.Zero);
            }
        }

        /// <summary>
        /// Verifies that the project browser renders a compact error state when loading fails.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysErrorState()
        {
            using var context = new ProjectBrowserViewModelMockContext();
            context.SetErrorMessage("Model load failed");
            this.RegisterViewModel(context);

            using var component = this.Render<ProjectBrowserComponent>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Markup, Does.Contain("Unable to load project browser"));
                Assert.That(component.Markup, Does.Contain("Model load failed"));
                Assert.That(component.Find("[role='alert']"), Is.Not.Null);
                Assert.That(context.InitializeCallCount, Is.Zero);
            }
        }

        /// <summary>
        /// Verifies that the project browser renders tree nodes when the view model has loaded.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysLoadedTree()
        {
            var node = ProjectBrowserNodeTestFactory.CreateNamespaceNode("quantities", "Quantities");
            using var context = new ProjectBrowserViewModelMockContext();
            context.ReplaceRootNodes(node);
            context.SetIsLoaded(true);
            this.RegisterViewModel(context);

            using var component = this.Render<ProjectBrowserComponent>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Markup, Does.Contain("Quantities"));
                Assert.That(component.Find(".mb-project-browser__tree").GetAttribute("role"), Is.EqualTo("tree"));
                Assert.That(component.Markup, Does.Not.Contain("Loading Quantities model"));
                Assert.That(context.InitializeCallCount, Is.Zero);
            }
        }

        /// <summary>
        /// Verifies that component initialization invokes the ViewModel's real initialization command.
        /// </summary>
        [Test]
        public void VerifyOnInitializedAsyncExecutesInitializeCommand()
        {
            ProjectBrowserNodeViewModel selectedNode = null;
            var node = ProjectBrowserNodeTestFactory.CreateNamespaceNode("quantities", "Quantities");
            using var context = new ProjectBrowserViewModelMockContext();

            context.InitializeHandler = _ =>
            {
                context.ReplaceRootNodes(node);
                context.SetIsLoaded(true);

                return Task.FromResult(true);
            };

            this.RegisterViewModel(context);

            using var component = this.Render<ProjectBrowserComponent>(parameters => parameters
                .Add(browser => browser.SelectedNodeChanged, changedNode =>
                {
                    selectedNode = changedNode;

                    return Task.CompletedTask;
                }));

            component.WaitForAssertion(() => Assert.That(context.InitializeCallCount, Is.EqualTo(1)));
            component.WaitForAssertion(() => Assert.That(component.Markup, Does.Contain("Quantities")));

            Assert.That(selectedNode, Is.Null);
        }

        /// <summary>
        /// Verifies parent-node interaction invokes the ViewModel commands and the local callback.
        /// </summary>
        [Test]
        public void VerifyHandleNodeSelectedExecutesToggleAndSelectCommands()
        {
            ProjectBrowserNodeViewModel selectedNode = null;
            var child = ProjectBrowserNodeTestFactory.CreateNamespaceNode("quantities/length", "Length");
            var node = ProjectBrowserNodeTestFactory.CreateNamespaceNode(
                "quantities",
                "Quantities",
                child);
            using var context = new ProjectBrowserViewModelMockContext();
            context.ReplaceRootNodes(node);
            context.SetIsLoaded(true);
            this.RegisterViewModel(context);

            using var component = this.Render<ProjectBrowserComponent>(parameters => parameters
                .Add(browser => browser.SelectedNodeChanged, changedNode =>
                {
                    selectedNode = changedNode;

                    return Task.CompletedTask;
                }));

            component.Find("button").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(context.ToggleCallCount, Is.EqualTo(1));
                Assert.That(context.SelectCallCount, Is.EqualTo(1));
                Assert.That(context.LastToggledNode, Is.SameAs(node));
                Assert.That(context.LastSelectedNode, Is.SameAs(node));
                Assert.That(selectedNode, Is.SameAs(node));
                Assert.That(component.Find("[role='treeitem']").GetAttribute("aria-expanded"), Is.EqualTo("false"));
                Assert.That(component.Find("[role='treeitem']").GetAttribute("aria-selected"), Is.EqualTo("false"));
            }
        }

        /// <summary>
        /// Verifies a leaf interaction invokes selection without invoking the inapplicable toggle command.
        /// </summary>
        [Test]
        public void VerifyHandleNodeSelectedSkipsToggleCommandForLeaf()
        {
            var node = ProjectBrowserNodeTestFactory.CreateNamespaceNode("quantities", "Quantities");
            using var context = new ProjectBrowserViewModelMockContext();
            context.ReplaceRootNodes(node);
            context.SetIsLoaded(true);
            this.RegisterViewModel(context);

            using var component = this.Render<ProjectBrowserComponent>();

            component.Find("button").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(context.ToggleCallCount, Is.Zero);
                Assert.That(context.SelectCallCount, Is.EqualTo(1));
                Assert.That(context.LastSelectedNode, Is.SameAs(node));
            }
        }

        /// <summary>
        /// Verifies a reactive IsLoading change rerenders the real component.
        /// </summary>
        [Test]
        public void VerifyReactiveIsLoadingChangeRerendersComponent()
        {
            using var context = new ProjectBrowserViewModelMockContext();
            context.SetIsLoaded(true);
            context.SetIsLoading(true);
            this.RegisterViewModel(context);

            using var component = this.Render<ProjectBrowserComponent>();
            Assert.That(component.Markup, Does.Contain("Loading Quantities model"));

            context.SetIsLoading(false);

            component.WaitForAssertion(() =>
                Assert.That(component.Markup, Does.Contain("No model elements available.")));
        }

        /// <summary>
        /// Verifies a reactive IsLoaded change rerenders the real component.
        /// </summary>
        [Test]
        public void VerifyReactiveIsLoadedChangeRerendersComponent()
        {
            using var context = new ProjectBrowserViewModelMockContext();
            context.SetIsLoaded(true);
            this.RegisterViewModel(context);

            using var component = this.Render<ProjectBrowserComponent>();
            Assert.That(component.Markup, Does.Contain("No model elements available."));

            context.SetIsLoaded(false);

            component.WaitForAssertion(() =>
                Assert.That(component.Markup, Does.Contain("Loading Quantities model")));
        }

        /// <summary>
        /// Verifies a reactive loading error change rerenders the real component.
        /// </summary>
        [Test]
        public void VerifyReactiveErrorMessageChangeRerendersComponent()
        {
            using var context = new ProjectBrowserViewModelMockContext();
            context.SetIsLoaded(true);
            this.RegisterViewModel(context);

            using var component = this.Render<ProjectBrowserComponent>();

            context.SetErrorMessage("Reactive model failure");

            component.WaitForAssertion(() =>
                Assert.That(component.Find("[role='alert']").TextContent, Does.Contain("Reactive model failure")));
        }

        /// <summary>
        /// Verifies one SourceList transaction updates the read-only roots rendered by the component.
        /// </summary>
        [Test]
        public void VerifyReactiveRootNodesChangeRerendersComponent()
        {
            var firstNode = ProjectBrowserNodeTestFactory.CreateNamespaceNode("first", "First");
            var secondNode = ProjectBrowserNodeTestFactory.CreateNamespaceNode("second", "Second");
            var thirdNode = ProjectBrowserNodeTestFactory.CreateNamespaceNode("third", "Third");
            using var context = new ProjectBrowserViewModelMockContext();
            context.ReplaceRootNodes(firstNode);
            context.SetIsLoaded(true);
            this.RegisterViewModel(context);

            using var component = this.Render<ProjectBrowserComponent>();
            Assert.That(component.Find(".mb-project-browser-node__title").TextContent, Is.EqualTo("First"));

            var reactiveObject = (IReactiveObject)context.Object;
            var rootCountsDuringChanging = new List<int>();
            var rootCountsDuringChanged = new List<int>();

            using var changingSubscription = System.ObservableExtensions.Subscribe(
                reactiveObject.GetChangingObservable(),
                args =>
                {
                    if (args.PropertyName == nameof(IProjectBrowserViewModel.RootNodes))
                    {
                        rootCountsDuringChanging.Add(context.Object.RootNodes.Count);
                    }
                });
            using var changedSubscription = System.ObservableExtensions.Subscribe(
                reactiveObject.GetChangedObservable(),
                args =>
                {
                    if (args.PropertyName == nameof(IProjectBrowserViewModel.RootNodes))
                    {
                        rootCountsDuringChanged.Add(context.Object.RootNodes.Count);
                    }
                });

            context.ReplaceRootNodes(secondNode, thirdNode);

            component.WaitForAssertion(() =>
            {
                var titles = component.FindAll(".mb-project-browser-node__title");

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(titles, Has.Count.EqualTo(2));
                    Assert.That(titles[0].TextContent, Is.EqualTo("Second"));
                    Assert.That(titles[1].TextContent, Is.EqualTo("Third"));
                }
            });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rootCountsDuringChanging, Has.Count.EqualTo(1));
                Assert.That(rootCountsDuringChanging[0], Is.EqualTo(1));
                Assert.That(rootCountsDuringChanged, Has.Count.EqualTo(1));
                Assert.That(rootCountsDuringChanged[0], Is.EqualTo(2));
            }
        }

        /// <summary>
        /// Verifies node presentation reacts to its own selected-state notification.
        /// </summary>
        [Test]
        public void VerifyReactiveSelectedNodeVisualChangeRerendersComponent()
        {
            var node = ProjectBrowserNodeTestFactory.CreateNamespaceNode("quantities", "Quantities");
            using var context = new ProjectBrowserViewModelMockContext();
            context.ReplaceRootNodes(node);
            context.SetIsLoaded(true);
            this.RegisterViewModel(context);

            using var component = this.Render<ProjectBrowserComponent>();
            Assert.That(component.Find("[role='treeitem']").GetAttribute("aria-selected"), Is.EqualTo("false"));

            node.IsSelected = true;
            context.SetSelectedNode(node);

            component.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.Find("[role='treeitem']").GetAttribute("aria-selected"), Is.EqualTo("true"));
                    Assert.That(component.Find("button").ClassList, Does.Contain("mb-project-browser-node__row--selected"));
                }
            });
        }

        /// <summary>
        /// Verifies assigning an unchanged mocked reactive value emits no notification or rerender.
        /// </summary>
        [Test]
        public void VerifyUnchangedReactiveValueDoesNotRerenderComponent()
        {
            using var context = new ProjectBrowserViewModelMockContext();
            context.SetIsLoaded(true);
            this.RegisterViewModel(context);

            using var component = this.Render<ProjectBrowserComponent>();
            var renderCount = component.RenderCount;

            context.SetIsLoaded(true);

            Assert.That(component.RenderCount, Is.EqualTo(renderCount));
        }

        /// <summary>
        /// Verifies the Moq-backed context emits the complete ReactiveUI contract for every reactive property.
        /// </summary>
        [Test]
        public void VerifyMockContextEmitsReactiveNotificationContract()
        {
            using var context = new ProjectBrowserViewModelMockContext();
            var selectedNode = ProjectBrowserNodeTestFactory.CreateNamespaceNode("selected", "Selected");

            VerifyReactiveNotificationContract(
                context,
                nameof(IProjectBrowserViewModel.IsLoading),
                () => context.Object.IsLoading,
                context.SetIsLoading,
                false,
                true);
            VerifyReactiveNotificationContract(
                context,
                nameof(IProjectBrowserViewModel.IsLoaded),
                () => context.Object.IsLoaded,
                context.SetIsLoaded,
                false,
                true);
            VerifyReactiveNotificationContract(
                context,
                nameof(IProjectBrowserViewModel.ErrorMessage),
                () => context.Object.ErrorMessage,
                context.SetErrorMessage,
                string.Empty,
                "Reactive failure");
            VerifyReactiveNotificationContract(
                context,
                nameof(IProjectBrowserViewModel.SelectedNode),
                () => context.Object.SelectedNode,
                context.SetSelectedNode,
                null,
                selectedNode);
        }

        /// <summary>
        /// Verifies selected-node changes use reference identity and remain silent for the same reference.
        /// </summary>
        [Test]
        public void VerifyMockContextSelectedNodeUsesReferenceIdentity()
        {
            using var context = new ProjectBrowserViewModelMockContext();
            var firstNode = ProjectBrowserNodeTestFactory.CreateNamespaceNode("shared", "Shared");
            var distinctNode = ProjectBrowserNodeTestFactory.CreateNamespaceNode("shared", "Shared");
            var observedSelections = new List<ProjectBrowserNodeViewModel>();
            var changingCount = 0;
            var reactiveObject = (IReactiveObject)context.Object;

            using var changingSubscription = System.ObservableExtensions.Subscribe(
                reactiveObject.GetChangingObservable(),
                args =>
                {
                    if (args.PropertyName == nameof(IProjectBrowserViewModel.SelectedNode))
                    {
                        changingCount++;
                    }
                });
            using var changedSubscription = System.ObservableExtensions.Subscribe(
                reactiveObject.GetChangedObservable(),
                args =>
                {
                    if (args.PropertyName == nameof(IProjectBrowserViewModel.SelectedNode))
                    {
                        observedSelections.Add(context.Object.SelectedNode);
                    }
                });

            context.SetSelectedNode(firstNode);
            context.SetSelectedNode(firstNode);
            context.SetSelectedNode(distinctNode);
            context.SetSelectedNode(null);
            context.SetSelectedNode(null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(changingCount, Is.EqualTo(3));
                Assert.That(observedSelections, Has.Count.EqualTo(3));
                Assert.That(observedSelections[0], Is.SameAs(firstNode));
                Assert.That(observedSelections[1], Is.SameAs(distinctNode));
                Assert.That(observedSelections[2], Is.Null);
            }
        }

        /// <summary>
        /// Verifies the component disposes its ViewModel boundary before command resources are released by the context.
        /// </summary>
        [Test]
        public void VerifyComponentDisposesMockedViewModelBoundary()
        {
            using var context = new ProjectBrowserViewModelMockContext();
            context.SetIsLoaded(true);
            this.RegisterViewModel(context);

            using var component = this.Render<ProjectBrowserComponent>();
            Assert.That(component.Markup, Does.Contain("No model elements available."));

            component.Instance.Dispose();

            context.Mock.Verify(x => x.Dispose(), Times.Once);
        }

        /// <summary>
        /// Verifies one property change is emitted before assignment and one after assignment.
        /// </summary>
        /// <typeparam name="T">The reactive property's value type.</typeparam>
        /// <param name="context">The configured mock context.</param>
        /// <param name="propertyName">The property being changed.</param>
        /// <param name="getValue">Reads the controlled backing value.</param>
        /// <param name="setValue">Changes the controlled backing value.</param>
        /// <param name="originalValue">The expected value before assignment.</param>
        /// <param name="replacementValue">The expected value after assignment.</param>
        private static void VerifyReactiveNotificationContract<T>(
            ProjectBrowserViewModelMockContext context,
            string propertyName,
            Func<T> getValue,
            Action<T> setValue,
            T originalValue,
            T replacementValue)
        {
            var reactiveObject = (IReactiveObject)context.Object;
            var classicChanging = new List<string>();
            var classicChanged = new List<string>();
            var reactiveChanging = new List<string>();
            var reactiveChanged = new List<string>();
            var classicValueDuringChanging = default(T);
            var classicValueDuringChanged = default(T);
            var reactiveValueDuringChanging = default(T);
            var reactiveValueDuringChanged = default(T);
            PropertyChangingEventHandler changingHandler = (_, args) =>
            {
                classicChanging.Add(args.PropertyName);
                classicValueDuringChanging = getValue();
            };
            PropertyChangedEventHandler changedHandler = (_, args) =>
            {
                classicChanged.Add(args.PropertyName);
                classicValueDuringChanged = getValue();
            };

            reactiveObject.PropertyChanging += changingHandler;
            context.Object.PropertyChanged += changedHandler;

            using var changingSubscription = System.ObservableExtensions.Subscribe(
                reactiveObject.GetChangingObservable(),
                args =>
                {
                    reactiveChanging.Add(args.PropertyName);
                    reactiveValueDuringChanging = getValue();
                });
            using var changedSubscription = System.ObservableExtensions.Subscribe(
                reactiveObject.GetChangedObservable(),
                args =>
                {
                    reactiveChanged.Add(args.PropertyName);
                    reactiveValueDuringChanged = getValue();
                });

            try
            {
                setValue(replacementValue);
                setValue(replacementValue);
            }
            finally
            {
                reactiveObject.PropertyChanging -= changingHandler;
                context.Object.PropertyChanged -= changedHandler;
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(classicChanging, Has.Count.EqualTo(1));
                Assert.That(classicChanging[0], Is.EqualTo(propertyName));
                Assert.That(classicChanged, Has.Count.EqualTo(1));
                Assert.That(classicChanged[0], Is.EqualTo(propertyName));
                Assert.That(reactiveChanging, Has.Count.EqualTo(1));
                Assert.That(reactiveChanging[0], Is.EqualTo(propertyName));
                Assert.That(reactiveChanged, Has.Count.EqualTo(1));
                Assert.That(reactiveChanged[0], Is.EqualTo(propertyName));
                Assert.That(classicValueDuringChanging, Is.EqualTo(originalValue));
                Assert.That(reactiveValueDuringChanging, Is.EqualTo(originalValue));
                Assert.That(classicValueDuringChanged, Is.EqualTo(replacementValue));
                Assert.That(reactiveValueDuringChanged, Is.EqualTo(replacementValue));
            }
        }

        /// <summary>
        /// Registers the mocked Project Browser contract for the component under test.
        /// </summary>
        /// <param name="context">The Moq-backed test context.</param>
        private void RegisterViewModel(ProjectBrowserViewModelMockContext context)
        {
            this.Services.AddSingleton(context.Object);
        }
    }
}
