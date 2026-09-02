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
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using Moq;

    using Mycelium.Bloom.Core.Context;
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

        private static readonly NavigationRailItem OpenReview = new()
        {
            Id = "review",
            Label = "Review open project",
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

        private static readonly NavigationRailItem[] OpenItems = [OpenReview, Activity];

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

        private static readonly string[] ExpectedPresentationModeNames =
        [
            "Expanded",
            "Collapsed",
            "ExpandOnHover"
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
            var contextService = CreateContextService(ProjectLifecycleState.Preparation);
            using var viewModel = new NavigationRailViewModel(
                contextService,
                CreateNavigationRailItemProvider(
                    static (lifecycleState, _) => SelectItemsByLifecycleState(lifecycleState)));

            contextService.LifecycleState = lifecycleState;

            Assert.That(viewModel.NavigationItems, Is.EqualTo(expectedItems));
        }

        [Test]
        public void VerifyProjectLifecycleStateMatchesSoftwareSystemSpecification()
        {
            Assert.That(Enum.GetNames<ProjectLifecycleState>(), Is.EqualTo(ExpectedLifecycleStateNames));
        }

        [Test]
        public void VerifySelectedElementChangesDeriveNavigationInventoryReactively()
        {
            var contextService = CreateContextService(ProjectLifecycleState.Open);
            using var viewModel = new NavigationRailViewModel(
                contextService,
                CreateNavigationRailItemProvider(
                    (_, selectedElement) => selectedElement is null ? PreparationItems : OpenItems));

            Assert.That(viewModel.NavigationItems, Is.EqualTo(PreparationItems));

            contextService.SelectedElement = new Namespace();

            Assert.That(viewModel.NavigationItems, Is.EqualTo(OpenItems));
        }

        [Test]
        public void VerifyCombinedContextChangesUpdateInventoryAndSelection()
        {
            var contextService = CreateContextService(ProjectLifecycleState.Preparation);
            using var viewModel = new NavigationRailViewModel(
                contextService,
                CreateNavigationRailItemProvider(SelectItemsByCombinedContext));
            viewModel.SelectedItem = Review;

            contextService.LifecycleState = ProjectLifecycleState.Open;
            contextService.SelectedElement = new Namespace();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.NavigationItems, Is.EqualTo(ReviewItems));
                Assert.That(viewModel.SelectedItem, Is.SameAs(Activity));
            }
        }

        [Test]
        public void VerifySelectionIsPreservedWhenStableIdentifierRemains()
        {
            var contextService = CreateContextService(ProjectLifecycleState.Preparation);
            using var viewModel = new NavigationRailViewModel(
                contextService,
                CreateNavigationRailItemProvider(
                    static (lifecycleState, _) => SelectItemsByLifecycleState(lifecycleState)));
            viewModel.SelectedItem = Review;
            NavigationRailItem[] publishedItems = null;
            NavigationRailItem publishedSelection = null;
            viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(viewModel.NavigationItems))
                {
                    publishedItems = viewModel.NavigationItems.ToArray();
                    publishedSelection = viewModel.SelectedItem;
                }
            };

            contextService.LifecycleState = ProjectLifecycleState.Open;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.SelectedItem, Is.SameAs(OpenReview));
                Assert.That(publishedItems, Is.EqualTo(OpenItems));
                Assert.That(publishedSelection, Is.SameAs(OpenReview));
            }
        }

        [Test]
        public void VerifySelectedItemUsesReactivePropertySemantics()
        {
            var contextService = CreateContextService(ProjectLifecycleState.Preparation);
            using var viewModel = new NavigationRailViewModel(
                contextService,
                CreateNavigationRailItemProvider(
                    static (lifecycleState, _) => SelectItemsByLifecycleState(lifecycleState)));
            var changedProperties = new List<string>();
            viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

            viewModel.SelectedItem = Review;
            viewModel.SelectedItem = Review;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.SelectedItem, Is.SameAs(Review));
                Assert.That(changedProperties, Is.EqualTo(new[] { nameof(viewModel.SelectedItem) }));
            }
        }

        [Test]
        public void VerifySelectedItemCanBeClearedExplicitly()
        {
            var contextService = CreateContextService(ProjectLifecycleState.Preparation);
            using var viewModel = new NavigationRailViewModel(
                contextService,
                CreateNavigationRailItemProvider(
                    static (lifecycleState, _) => SelectItemsByLifecycleState(lifecycleState)));

            viewModel.SelectedItem = null;

            Assert.That(viewModel.SelectedItem, Is.Null);
        }

        [Test]
        public void VerifySelectionFallsBackWhenDestinationDisappears()
        {
            var contextService = CreateContextService(ProjectLifecycleState.Preparation);
            using var viewModel = new NavigationRailViewModel(
                contextService,
                CreateNavigationRailItemProvider(
                    static (lifecycleState, _) => SelectItemsByLifecycleState(lifecycleState)));
            viewModel.SelectedItem = Review;

            contextService.LifecycleState = ProjectLifecycleState.Review;

            Assert.That(viewModel.SelectedItem, Is.SameAs(Activity));
        }

        [Test]
        public void VerifySelectionClearsWhenInventoryBecomesEmpty()
        {
            var contextService = CreateContextService(ProjectLifecycleState.Preparation);
            using var viewModel = new NavigationRailViewModel(
                contextService,
                CreateNavigationRailItemProvider(
                    static (lifecycleState, _) => SelectItemsWithEmptyReviewState(lifecycleState)));
            viewModel.SelectedItem = Review;

            contextService.LifecycleState = ProjectLifecycleState.Review;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.NavigationItems, Is.Empty);
                Assert.That(viewModel.SelectedItem, Is.Null);
            }
        }

        [Test]
        public void VerifyNavigationRailItemsExposeInitOnlyConfiguration()
        {
            var propertyNames = new[]
            {
                nameof(NavigationRailItem.Id),
                nameof(NavigationRailItem.Label),
                nameof(NavigationRailItem.IconName),
                nameof(NavigationRailItem.GroupKey)
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
        public void VerifyPresentationModeDefinesThreeChoicesAndDefaultsToExpandOnHover()
        {
            var contextService = CreateContextService(ProjectLifecycleState.Preparation);
            using var viewModel = new NavigationRailViewModel(
                contextService,
                CreateNavigationRailItemProvider(
                    static (lifecycleState, _) => SelectItemsByLifecycleState(lifecycleState)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Enum.GetNames<NavigationRailPresentationMode>(),
                    Is.EqualTo(ExpectedPresentationModeNames));
                Assert.That(viewModel.PresentationMode,
                    Is.EqualTo(NavigationRailPresentationMode.ExpandOnHover));
            }
        }

        [Test]
        public void VerifyPresentationModeIsPubliclySettableMutuallyExclusiveAndValidated()
        {
            var contextService = CreateContextService(ProjectLifecycleState.Preparation);
            using var viewModel = new NavigationRailViewModel(
                contextService,
                CreateNavigationRailItemProvider(
                    static (lifecycleState, _) => SelectItemsByLifecycleState(lifecycleState)));
            var changedProperties = new List<string>();
            viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

            viewModel.PresentationMode = NavigationRailPresentationMode.Expanded;
            viewModel.PresentationMode = NavigationRailPresentationMode.Collapsed;
            viewModel.PresentationMode = NavigationRailPresentationMode.ExpandOnHover;
            var exception = Assert.Throws<InvalidEnumArgumentException>(() =>
                viewModel.PresentationMode = (NavigationRailPresentationMode)999);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.PresentationMode,
                    Is.EqualTo(NavigationRailPresentationMode.ExpandOnHover));
                Assert.That(changedProperties, Is.EqualTo(new[]
                {
                    nameof(viewModel.PresentationMode),
                    nameof(viewModel.PresentationMode),
                    nameof(viewModel.PresentationMode)
                }));
                Assert.That(exception.ParamName, Is.EqualTo("value"));
            }
        }

        [Test]
        public void VerifyInstancesOwnIndependentStateAndRenderingProjections()
        {
            var firstContextService = CreateContextService(ProjectLifecycleState.Preparation);
            var secondContextService = CreateContextService(ProjectLifecycleState.Review);
            using var first = new NavigationRailViewModel(
                firstContextService,
                CreateNavigationRailItemProvider(
                    static (lifecycleState, _) => SelectItemsByLifecycleState(lifecycleState)));
            using var second = new NavigationRailViewModel(
                secondContextService,
                CreateNavigationRailItemProvider(
                    static (lifecycleState, _) => SelectItemsByLifecycleState(lifecycleState)));

            first.SelectedItem = Review;
            first.PresentationMode = NavigationRailPresentationMode.Expanded;
            firstContextService.LifecycleState = ProjectLifecycleState.Open;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.NavigationItems, Is.Not.SameAs(second.NavigationItems));
                Assert.That(first.NavigationItems, Is.EqualTo(OpenItems));
                Assert.That(second.NavigationItems, Is.EqualTo(ReviewItems));
                Assert.That(first.SelectedItem, Is.SameAs(OpenReview));
                Assert.That(second.SelectedItem, Is.SameAs(Activity));
                Assert.That(first.PresentationMode, Is.EqualTo(NavigationRailPresentationMode.Expanded));
                Assert.That(second.PresentationMode,
                    Is.EqualTo(NavigationRailPresentationMode.ExpandOnHover));
            }
        }

        [Test]
        public void VerifyDisposalIsIdempotentAndStopsContextSubscriptions()
        {
            var contextService = CreateContextService(ProjectLifecycleState.Preparation);
            var viewModel = new NavigationRailViewModel(
                contextService,
                CreateNavigationRailItemProvider(
                    static (lifecycleState, _) => SelectItemsByLifecycleState(lifecycleState)));

            viewModel.Dispose();

            Assert.DoesNotThrow(viewModel.Dispose);
            Assert.DoesNotThrow(() =>
                contextService.LifecycleState = ProjectLifecycleState.Archived);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.NavigationItems, Is.EqualTo(PreparationItems));
                Assert.That(viewModel.SelectedItem, Is.SameAs(Overview));
                Assert.That(viewModel.PresentationMode,
                    Is.EqualTo(NavigationRailPresentationMode.ExpandOnHover));
            }
        }

        private static NavigationRailItem[] SelectItemsByLifecycleState(
            ProjectLifecycleState lifecycleState)
        {
            return lifecycleState switch
            {
                ProjectLifecycleState.Preparation => PreparationItems,
                ProjectLifecycleState.Open => OpenItems,
                ProjectLifecycleState.Review => ReviewItems,
                ProjectLifecycleState.Archived => ArchivedItems,
                _ => throw new ArgumentOutOfRangeException(nameof(lifecycleState), lifecycleState, null)
            };
        }

        private static NavigationRailItem[] SelectItemsByCombinedContext(
            ProjectLifecycleState lifecycleState,
            IElement selectedElement)
        {
            return lifecycleState switch
            {
                ProjectLifecycleState.Open when selectedElement is not null => ReviewItems,
                ProjectLifecycleState.Preparation => PreparationItems,
                ProjectLifecycleState.Open => PreparationItems,
                ProjectLifecycleState.Review => PreparationItems,
                ProjectLifecycleState.Archived => PreparationItems,
                _ => throw new ArgumentOutOfRangeException(nameof(lifecycleState), lifecycleState, null)
            };
        }

        private static NavigationRailItem[] SelectItemsWithEmptyReviewState(
            ProjectLifecycleState lifecycleState)
        {
            return lifecycleState switch
            {
                ProjectLifecycleState.Preparation => PreparationItems,
                ProjectLifecycleState.Open => PreparationItems,
                ProjectLifecycleState.Review => NoItems,
                ProjectLifecycleState.Archived => PreparationItems,
                _ => throw new ArgumentOutOfRangeException(nameof(lifecycleState), lifecycleState, null)
            };
        }

        private static ContextAwareService CreateContextService(
            ProjectLifecycleState lifecycleState,
            IElement selectedElement = null)
        {
            return new ContextAwareService
            {
                LifecycleState = lifecycleState,
                SelectedElement = selectedElement
            };
        }

        private static INavigationRailItemProvider CreateNavigationRailItemProvider(
            Func<ProjectLifecycleState, IElement, IReadOnlyList<NavigationRailItem>> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);

            var provider = new Mock<INavigationRailItemProvider>(MockBehavior.Strict);

            provider.Setup(x => x.GetNavigationItems(
                    It.IsAny<ProjectLifecycleState>(),
                    It.IsAny<IElement>()))
                .Returns((ProjectLifecycleState lifecycleState, IElement selectedElement) =>
                    selector(lifecycleState, selectedElement));

            return provider.Object;
        }
    }
}
