// ------------------------------------------------------------------------------------------------
// <copyright file="NavigationRailViewModelTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.NavigationRail
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Reactive.Subjects;
    using System.Runtime.CompilerServices;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.NavigationRail;

    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;

    [TestFixture]
    public sealed class NavigationRailViewModelTestFixture
    {
        private static readonly NavigationRailItem Overview = new()
        {
            Id = "overview",
            Label = "Overview",
            IconName = "layout-dashboard"
        };

        private static readonly NavigationRailItem Review = new()
        {
            Id = "review",
            Label = "Review",
            IconName = "messages-square"
        };

        private static readonly NavigationRailItem Activity = new()
        {
            Id = "activity",
            Label = "Activity",
            IconName = "history"
        };

        private static readonly NavigationRailItem Settings = new()
        {
            Id = "settings",
            Label = "Settings",
            IconName = "settings"
        };

        private static readonly NavigationRailItem[] PreparationItems = [Overview, Review];

        private static readonly NavigationRailItem[] OpenItems = [Review, Activity];

        private static readonly NavigationRailItem[] ReviewItems = [Activity, Settings];

        private static readonly NavigationRailItem[] ArchivedItems = [Settings, Overview];

        private static readonly NavigationRailItem[] NoItems = [];

        private static readonly string[] ExpectedLifecycleStateNames =
        [
            "Preparation",
            "Open",
            "Review",
            "Archived"
        ];

        private static readonly object[][] LifecycleInventoryCases =
        [
            [ProjectLifecycleState.Preparation, PreparationItems],
            [ProjectLifecycleState.Open, OpenItems],
            [ProjectLifecycleState.Review, ReviewItems],
            [ProjectLifecycleState.Archived, ArchivedItems]
        ];

        [TestCaseSource(nameof(LifecycleInventoryCases))]
        public void VerifyProjectLifecycleStateDerivesNavigationInventoryReactively(
            ProjectLifecycleState lifecycleState,
            NavigationRailItem[] expectedItems)
        {
            using var contexts = new BehaviorSubject<NavigationRailContext>(
                CreateContext(ProjectLifecycleState.Preparation));
            using var viewModel = new NavigationRailViewModel(contexts, SelectItemsByLifecycleState);
            var inconsistentSnapshots = new List<string>();
            INotifyCollectionChanged observableItems = viewModel.NavigationItems;

            viewModel.PropertyChanging += (_, _) => RecordInconsistentState(viewModel, inconsistentSnapshots);
            viewModel.PropertyChanged += (_, _) => RecordInconsistentState(viewModel, inconsistentSnapshots);
            observableItems.CollectionChanged += (_, _) => RecordInconsistentState(viewModel, inconsistentSnapshots);

            contexts.OnNext(CreateContext(lifecycleState));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.NavigationItems, Is.EqualTo(expectedItems));
                Assert.That(inconsistentSnapshots, Is.Empty);
            }
        }

        [Test]
        public void VerifyProjectLifecycleStateMatchesSoftwareSystemSpecification()
        {
            Assert.That(Enum.GetNames<ProjectLifecycleState>(), Is.EqualTo(ExpectedLifecycleStateNames));
        }

        [Test]
        public void VerifySelectedElementChangesDeriveNavigationInventoryReactively()
        {
            using var contexts = new BehaviorSubject<NavigationRailContext>(
                CreateContext(ProjectLifecycleState.Open));
            using var viewModel = new NavigationRailViewModel(
                contexts,
                context => context.SelectedElement is null ? PreparationItems : OpenItems);

            Assert.That(viewModel.NavigationItems, Is.EqualTo(PreparationItems));

            contexts.OnNext(CreateContext(ProjectLifecycleState.Open, new Namespace()));

            Assert.That(viewModel.NavigationItems, Is.EqualTo(OpenItems));
        }

        [Test]
        public void VerifyCombinedContextChangesPublishCoherentInventoryAndSelection()
        {
            using var contexts = new BehaviorSubject<NavigationRailContext>(
                CreateContext(ProjectLifecycleState.Preparation));
            using var viewModel = new NavigationRailViewModel(
                contexts,
                SelectItemsByCombinedContext);
            viewModel.SelectItem("review");
            var inconsistentSnapshots = new List<string>();
            INotifyCollectionChanged observableItems = viewModel.NavigationItems;

            viewModel.PropertyChanging += (_, _) => RecordInconsistentState(viewModel, inconsistentSnapshots);
            viewModel.PropertyChanged += (_, _) => RecordInconsistentState(viewModel, inconsistentSnapshots);
            observableItems.CollectionChanged += (_, _) => RecordInconsistentState(viewModel, inconsistentSnapshots);

            contexts.OnNext(CreateContext(ProjectLifecycleState.Open, new Namespace()));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.NavigationItems, Is.EqualTo(ReviewItems));
                Assert.That(viewModel.SelectedItemId, Is.EqualTo("activity"));
                Assert.That(inconsistentSnapshots, Is.Empty);
            }
        }

        [Test]
        public void VerifySelectionIsPreservedWhenStableIdentifierRemains()
        {
            using var contexts = new BehaviorSubject<NavigationRailContext>(
                CreateContext(ProjectLifecycleState.Preparation));
            using var viewModel = new NavigationRailViewModel(contexts, SelectItemsByLifecycleState);
            viewModel.SelectItem("review");

            contexts.OnNext(CreateContext(ProjectLifecycleState.Open));

            Assert.That(viewModel.SelectedItemId, Is.EqualTo("review"));
        }

        [Test]
        public void VerifySelectionFallsBackWhenDestinationDisappears()
        {
            using var contexts = new BehaviorSubject<NavigationRailContext>(
                CreateContext(ProjectLifecycleState.Preparation));
            using var viewModel = new NavigationRailViewModel(contexts, SelectItemsByLifecycleState);
            viewModel.SelectItem("review");

            contexts.OnNext(CreateContext(ProjectLifecycleState.Review));

            Assert.That(viewModel.SelectedItemId, Is.EqualTo("activity"));
        }

        [Test]
        public void VerifySelectionClearsWhenInventoryBecomesEmpty()
        {
            using var contexts = new BehaviorSubject<NavigationRailContext>(
                CreateContext(ProjectLifecycleState.Preparation));
            using var viewModel = new NavigationRailViewModel(
                contexts,
                SelectItemsWithEmptyReviewState);
            viewModel.SelectItem("review");

            contexts.OnNext(CreateContext(ProjectLifecycleState.Review));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.NavigationItems, Is.Empty);
                Assert.That(viewModel.SelectedItemId, Is.Empty);
            }
        }

        [Test]
        public void VerifySelectionOnlyAcceptsAvailableStableIdentifiers()
        {
            using var contexts = new BehaviorSubject<NavigationRailContext>(
                CreateContext(ProjectLifecycleState.Preparation));
            using var viewModel = new NavigationRailViewModel(contexts, SelectItemsByLifecycleState);

            viewModel.SelectItem("review");
            viewModel.SelectItem("missing");
            viewModel.SelectItem(string.Empty);

            Assert.That(viewModel.SelectedItemId, Is.EqualTo("review"));
        }

        [Test]
        public void VerifyNavigationRailItemsExposeInitOnlyConfiguration()
        {
            var propertyNames = new[]
            {
                nameof(NavigationRailItem.Id),
                nameof(NavigationRailItem.Label),
                nameof(NavigationRailItem.IconName),
                nameof(NavigationRailItem.StartsNewSection)
            };

            foreach (var propertyName in propertyNames)
            {
                var property = typeof(NavigationRailItem).GetProperty(propertyName);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(property, Is.Not.Null);
                    Assert.That(property.SetMethod, Is.Not.Null);
                    Assert.That(
                        property.SetMethod.ReturnParameter.GetRequiredCustomModifiers(),
                        Does.Contain(typeof(IsExternalInit)));
                }
            }
        }

        [Test]
        public void VerifyPresentationModeAndHoverTransitionsRemainViewModelOwned()
        {
            using var contexts = new BehaviorSubject<NavigationRailContext>(
                CreateContext(ProjectLifecycleState.Preparation));
            using var viewModel = new NavigationRailViewModel(contexts, SelectItemsByLifecycleState);

            viewModel.SetPresentationMode(NavigationRailPresentationMode.ExpandOnHover);
            viewModel.HandlePointerEntered();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.PresentationMode, Is.EqualTo(NavigationRailPresentationMode.ExpandOnHover));
                Assert.That(viewModel.IsCollapsed, Is.False);
            }

            viewModel.HandlePointerExited();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.PresentationMode, Is.EqualTo(NavigationRailPresentationMode.ExpandOnHover));
                Assert.That(viewModel.IsCollapsed, Is.True);
            }

            viewModel.TogglePresentation();

            Assert.That(viewModel.PresentationMode, Is.EqualTo(NavigationRailPresentationMode.Expanded));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                viewModel.SetPresentationMode((NavigationRailPresentationMode)999));
        }

        [Test]
        public void VerifyInstancesOwnIndependentStateAndRenderingProjections()
        {
            using var firstContexts = new BehaviorSubject<NavigationRailContext>(
                CreateContext(ProjectLifecycleState.Preparation));
            using var secondContexts = new BehaviorSubject<NavigationRailContext>(
                CreateContext(ProjectLifecycleState.Review));
            using var first = new NavigationRailViewModel(firstContexts, SelectItemsByLifecycleState);
            using var second = new NavigationRailViewModel(secondContexts, SelectItemsByLifecycleState);

            first.SelectItem("review");
            first.SetPresentationMode(NavigationRailPresentationMode.Expanded);
            firstContexts.OnNext(CreateContext(ProjectLifecycleState.Open));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.NavigationItems, Is.Not.SameAs(second.NavigationItems));
                Assert.That(first.NavigationItems, Is.EqualTo(OpenItems));
                Assert.That(second.NavigationItems, Is.EqualTo(ReviewItems));
                Assert.That(first.SelectedItemId, Is.EqualTo("review"));
                Assert.That(second.SelectedItemId, Is.EqualTo("activity"));
                Assert.That(first.PresentationMode, Is.EqualTo(NavigationRailPresentationMode.Expanded));
                Assert.That(second.PresentationMode, Is.EqualTo(NavigationRailPresentationMode.Collapsed));
            }
        }

        [Test]
        public void VerifyDisposalIsIdempotentAndStopsContextSubscriptions()
        {
            using var contexts = new BehaviorSubject<NavigationRailContext>(
                CreateContext(ProjectLifecycleState.Preparation));
            var viewModel = new NavigationRailViewModel(contexts, SelectItemsByLifecycleState);

            viewModel.Dispose();

            Assert.DoesNotThrow(viewModel.Dispose);
            Assert.DoesNotThrow(() =>
                contexts.OnNext(CreateContext(ProjectLifecycleState.Archived)));
            Assert.DoesNotThrow(viewModel.TogglePresentation);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.NavigationItems, Is.EqualTo(PreparationItems));
                Assert.That(viewModel.SelectedItemId, Is.EqualTo("overview"));
                Assert.That(viewModel.PresentationMode, Is.EqualTo(NavigationRailPresentationMode.Collapsed));
            }
        }

        private static NavigationRailItem[] SelectItemsByLifecycleState(NavigationRailContext context)
        {
            return context.LifecycleState switch
            {
                ProjectLifecycleState.Preparation => PreparationItems,
                ProjectLifecycleState.Open => OpenItems,
                ProjectLifecycleState.Review => ReviewItems,
                ProjectLifecycleState.Archived => ArchivedItems,
                _ => throw new ArgumentOutOfRangeException(nameof(context), context.LifecycleState, null)
            };
        }

        private static NavigationRailItem[] SelectItemsByCombinedContext(
            NavigationRailContext context)
        {
            return context.LifecycleState switch
            {
                ProjectLifecycleState.Open when context.SelectedElement is not null => ReviewItems,
                ProjectLifecycleState.Preparation => PreparationItems,
                ProjectLifecycleState.Open => PreparationItems,
                ProjectLifecycleState.Review => PreparationItems,
                ProjectLifecycleState.Archived => PreparationItems,
                _ => throw new ArgumentOutOfRangeException(nameof(context), context.LifecycleState, null)
            };
        }

        private static NavigationRailItem[] SelectItemsWithEmptyReviewState(
            NavigationRailContext context)
        {
            return context.LifecycleState switch
            {
                ProjectLifecycleState.Preparation => PreparationItems,
                ProjectLifecycleState.Open => PreparationItems,
                ProjectLifecycleState.Review => NoItems,
                ProjectLifecycleState.Archived => PreparationItems,
                _ => throw new ArgumentOutOfRangeException(nameof(context), context.LifecycleState, null)
            };
        }

        private static NavigationRailContext CreateContext(
            ProjectLifecycleState lifecycleState,
            IElement selectedElement = null)
        {
            return new NavigationRailContext
            {
                LifecycleState = lifecycleState,
                SelectedElement = selectedElement
            };
        }

        private static void RecordInconsistentState(
            NavigationRailViewModel viewModel,
            List<string> inconsistentSnapshots)
        {
            var isCoherent = viewModel.NavigationItems.Count == 0
                ? string.IsNullOrWhiteSpace(viewModel.SelectedItemId)
                : viewModel.NavigationItems.Any(item => string.Equals(
                    item.Id,
                    viewModel.SelectedItemId,
                    StringComparison.Ordinal));

            if (!isCoherent)
            {
                inconsistentSnapshots.Add(
                    $"Selection '{viewModel.SelectedItemId}' is not valid for the published inventory.");
            }
        }
    }
}
