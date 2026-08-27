// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceEditorViewModelTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.WorkspaceEditor
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;

    using Microsoft.Extensions.Options;

    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.ViewModel.WorkspaceEditor;

    [TestFixture]
    public sealed class WorkspaceEditorViewModelTestFixture
    {
        private const int DefaultMaximumGroupCount = 3;

        private static readonly string[] ExpectedInitialTabTitles = ["First", "Second"];

        [Test]
        public void VerifyConstructorCreatesInitialWorkspaceState()
        {
            var viewModel = CreateViewModel();
            var initialGroup = viewModel.Groups.Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.MaximumGroupCount, Is.EqualTo(3));
                Assert.That(viewModel.Groups, Is.TypeOf<ReadOnlyObservableCollection<EditorGroupViewModel>>());
                Assert.That(viewModel.Groups, Has.Count.EqualTo(1));
                Assert.That(initialGroup.Id, Is.Not.EqualTo(Guid.Empty));
                Assert.That(initialGroup.Tabs, Is.TypeOf<ReadOnlyObservableCollection<EditorTabItem>>());
                Assert.That(initialGroup.Tabs, Is.Empty);
                Assert.That(initialGroup.ActiveTab, Is.Null);
                Assert.That(viewModel.FocusedGroup, Is.SameAs(initialGroup));
            }
        }

        [Test]
        public void VerifyConstructorRejectsNullOptions()
        {
            IOptions<WorkspaceEditorOptions> options = null;

            var exception = Assert.Throws<ArgumentNullException>(() => new WorkspaceEditorViewModel(options));

            Assert.That(exception.ParamName, Is.EqualTo(nameof(options)));
        }

        [Test]
        public void VerifyTryAddGroupAppendsEmptyGroupsAndFocusesNewestGroup()
        {
            var viewModel = CreateViewModel();
            var initialGroup = viewModel.Groups[0];

            var firstResult = viewModel.TryAddGroup(out var secondGroup);
            var secondResult = viewModel.TryAddGroup(out var thirdGroup);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstResult, Is.True);
                Assert.That(secondResult, Is.True);
                Assert.That(viewModel.Groups, Is.EqualTo(new[] { initialGroup, secondGroup, thirdGroup }));
                Assert.That(viewModel.Groups.Select(group => group.Id), Does.Not.Contain(Guid.Empty));
                Assert.That(viewModel.Groups.Select(group => group.Id).Distinct().Count(), Is.EqualTo(3));
                Assert.That(viewModel.Groups.SelectMany(group => group.Tabs), Is.Empty);
                Assert.That(viewModel.Groups.Select(group => group.ActiveTab), Is.All.Null);
                Assert.That(viewModel.FocusedGroup, Is.SameAs(thirdGroup));
            }
        }

        [Test]
        public void VerifyTryAddGroupRejectsFourthGroupAtomically()
        {
            var viewModel = CreateViewModel();
            AddGroup(viewModel);
            AddGroup(viewModel);
            var groups = viewModel.Groups;
            var expectedGroups = groups.ToArray();
            var focusedGroup = viewModel.FocusedGroup;
            var collectionNotifications = new List<NotifyCollectionChangedEventArgs>();
            var propertyNotifications = new List<string>();
            INotifyCollectionChanged observableGroups = groups;
            observableGroups.CollectionChanged += (_, args) => collectionNotifications.Add(args);
            viewModel.PropertyChanged += (_, args) => propertyNotifications.Add(args.PropertyName);

            var result = viewModel.TryAddGroup(out var rejectedGroup);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.False);
                Assert.That(rejectedGroup, Is.Null);
                Assert.That(viewModel.Groups, Is.SameAs(groups));
                Assert.That(viewModel.Groups, Is.EqualTo(expectedGroups));
                Assert.That(viewModel.FocusedGroup, Is.SameAs(focusedGroup));
                Assert.That(collectionNotifications, Is.Empty);
                Assert.That(propertyNotifications, Is.Empty);
            }
        }

        [Test]
        public void VerifyTryAddGroupUsesConfiguredMaximumGroupCount()
        {
            const int maximumGroupCount = 5;
            var viewModel = CreateViewModel(maximumGroupCount);
            var expectedGroups = new List<EditorGroupViewModel> { viewModel.Groups[0] };

            for (var groupIndex = 1; groupIndex < maximumGroupCount; groupIndex++)
            {
                expectedGroups.Add(AddGroup(viewModel));
            }

            var result = viewModel.TryAddGroup(out var rejectedGroup);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.MaximumGroupCount, Is.EqualTo(maximumGroupCount));
                Assert.That(viewModel.Groups, Is.EqualTo(expectedGroups));
                Assert.That(viewModel.FocusedGroup, Is.SameAs(expectedGroups[^1]));
                Assert.That(result, Is.False);
                Assert.That(rejectedGroup, Is.Null);
            }
        }

        [Test]
        public void VerifyTrySplitGroupInsertsEmptyFocusedGroupImmediatelyAfterRequestedGroup()
        {
            using var viewModel = CreateViewModel(maximumGroupCount: 5);
            var firstGroup = viewModel.Groups[0];
            var secondGroup = AddGroup(viewModel);
            var thirdGroup = AddGroup(viewModel);

            var result = viewModel.TrySplitGroup(firstGroup.Id, out var splitGroup);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(viewModel.Groups, Is.EqualTo(new[]
                {
                    firstGroup,
                    splitGroup,
                    secondGroup,
                    thirdGroup
                }));
                Assert.That(splitGroup, Is.Not.Null);
                Assert.That(splitGroup.Tabs, Is.Empty);
                Assert.That(splitGroup.ActiveTab, Is.Null);
                Assert.That(viewModel.FocusedGroup, Is.SameAs(splitGroup));
            }
        }

        [Test]
        public void VerifyTrySplitGroupAppendsAfterFinalGroup()
        {
            using var viewModel = CreateViewModel(maximumGroupCount: 4);
            var firstGroup = viewModel.Groups[0];
            var finalGroup = AddGroup(viewModel);

            var result = viewModel.TrySplitGroup(finalGroup.Id, out var splitGroup);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(viewModel.Groups, Is.EqualTo(new[] { firstGroup, finalGroup, splitGroup }));
                Assert.That(viewModel.Groups[^1], Is.SameAs(splitGroup));
                Assert.That(viewModel.FocusedGroup, Is.SameAs(splitGroup));
            }
        }

        [Test]
        public void VerifyTrySplitGroupRejectsInvalidIdentifiersWithoutMutationOrNotifications()
        {
            using var viewModel = CreateViewModel();
            var expectedGroups = viewModel.Groups.ToArray();
            var expectedFocus = viewModel.FocusedGroup;
            var expectedRenderState = viewModel.RenderState;
            var collectionNotificationCount = 0;
            var propertyNotificationCount = 0;
            INotifyCollectionChanged observableGroups = viewModel.Groups;
            observableGroups.CollectionChanged += (_, _) => collectionNotificationCount++;
            viewModel.PropertyChanged += (_, _) => propertyNotificationCount++;

            var emptyResult = viewModel.TrySplitGroup(Guid.Empty, out var emptyResultGroup);
            var unknownResult = viewModel.TrySplitGroup(Guid.NewGuid(), out var unknownResultGroup);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(emptyResult, Is.False);
                Assert.That(unknownResult, Is.False);
                Assert.That(emptyResultGroup, Is.Null);
                Assert.That(unknownResultGroup, Is.Null);
                Assert.That(viewModel.Groups, Is.EqualTo(expectedGroups));
                Assert.That(viewModel.FocusedGroup, Is.SameAs(expectedFocus));
                Assert.That(viewModel.RenderState, Is.SameAs(expectedRenderState));
                Assert.That(collectionNotificationCount, Is.Zero);
                Assert.That(propertyNotificationCount, Is.Zero);
            }
        }

        [Test]
        public void VerifyTrySplitGroupEnforcesMaximumWithoutMutationOrNotifications()
        {
            using var viewModel = CreateViewModel(maximumGroupCount: 2);
            var firstGroup = viewModel.Groups[0];
            var secondGroup = AddGroup(viewModel);
            var expectedRenderState = viewModel.RenderState;
            var collectionNotificationCount = 0;
            var propertyNotificationCount = 0;
            INotifyCollectionChanged observableGroups = viewModel.Groups;
            observableGroups.CollectionChanged += (_, _) => collectionNotificationCount++;
            viewModel.PropertyChanged += (_, _) => propertyNotificationCount++;

            var result = viewModel.TrySplitGroup(firstGroup.Id, out var rejectedGroup);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.False);
                Assert.That(rejectedGroup, Is.Null);
                Assert.That(viewModel.Groups, Is.EqualTo(new[] { firstGroup, secondGroup }));
                Assert.That(viewModel.FocusedGroup, Is.SameAs(secondGroup));
                Assert.That(viewModel.RenderState, Is.SameAs(expectedRenderState));
                Assert.That(collectionNotificationCount, Is.Zero);
                Assert.That(propertyNotificationCount, Is.Zero);
            }
        }

        [Test]
        public void VerifyTrySplitGroupPublishesOneCoherentRenderState()
        {
            using var viewModel = CreateViewModel(maximumGroupCount: 4);
            var leftGroup = viewModel.Groups[0];
            var rightGroup = AddGroup(viewModel);
            var initialRevision = viewModel.RenderState.Revision;
            var publishedStates = new List<WorkspaceEditorRenderState>();
            viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(viewModel.RenderState))
                {
                    publishedStates.Add(viewModel.RenderState);
                }
            };

            var result = viewModel.TrySplitGroup(leftGroup.Id, out var splitGroup);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(publishedStates, Has.Count.EqualTo(1));
                Assert.That(publishedStates[0].Revision, Is.EqualTo(initialRevision + 1));
                Assert.That(publishedStates[0].FocusedGroupId, Is.EqualTo(splitGroup.Id));
                Assert.That(
                    publishedStates[0].Groups.Select(group => group.Id),
                    Is.EqualTo(new[] { leftGroup.Id, splitGroup.Id, rightGroup.Id }));
                Assert.That(publishedStates[0].Groups[1].Tabs, Is.Empty);
                Assert.That(publishedStates[0].Groups[1].ActiveTabId, Is.Null);
            }
        }

        [Test]
        public void VerifyGroupsExposeStableOrderedCollectionNotifications()
        {
            var viewModel = CreateViewModel();
            var groups = viewModel.Groups;
            var initialGroup = groups[0];
            var notifications = new List<NotifyCollectionChangedEventArgs>();
            INotifyCollectionChanged observableGroups = groups;
            observableGroups.CollectionChanged += (_, args) => notifications.Add(args);

            var secondGroup = AddGroup(viewModel);
            var thirdGroup = AddGroup(viewModel);
            var thirdTab = OpenTab(viewModel, thirdGroup, "Third", "third-view");
            Assert.That(viewModel.CloseTab(thirdGroup.Id, thirdTab.Id), Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.Groups, Is.SameAs(groups));
                Assert.That(viewModel.Groups, Is.EqualTo(new[] { initialGroup, secondGroup }));
                Assert.That(notifications.Select(args => args.Action), Is.EqualTo(new[]
                {
                    NotifyCollectionChangedAction.Add,
                    NotifyCollectionChangedAction.Add,
                    NotifyCollectionChangedAction.Remove
                }));
                Assert.That(notifications.Select(args => args.NewStartingIndex), Is.EqualTo(new[] { 1, 2, -1 }));
                Assert.That(notifications.Select(args => args.OldStartingIndex), Is.EqualTo(new[] { -1, -1, 2 }));
                Assert.That(notifications[0].NewItems[0], Is.SameAs(secondGroup));
                Assert.That(notifications[1].NewItems[0], Is.SameAs(thirdGroup));
                Assert.That(notifications[2].OldItems[0], Is.SameAs(thirdGroup));
            }
        }

        [Test]
        public void VerifyTryOpenTabSupportsUnlimitedOrderedTabs()
        {
            const int tabCount = 128;
            var viewModel = CreateViewModel();
            var group = viewModel.Groups[0];
            var expectedTabs = new List<EditorTabItem>();

            for (var index = 0; index < tabCount; index++)
            {
                expectedTabs.Add(OpenTab(viewModel, group, $"Tab {index}", "shared-view"));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(group.Tabs, Has.Count.EqualTo(tabCount));
                Assert.That(group.Tabs, Is.EqualTo(expectedTabs));
                Assert.That(group.ActiveTab, Is.SameAs(expectedTabs[^1]));
                Assert.That(viewModel.FocusedGroup, Is.SameAs(group));
            }
        }

        [Test]
        public void VerifyTryOpenTabCreatesIndependentDuplicateViewInstances()
        {
            var viewModel = CreateViewModel();
            var group = viewModel.Groups[0];

            var firstTab = OpenTab(viewModel, group, "Duplicate", "shared-view");
            var secondTab = OpenTab(viewModel, group, "Duplicate", "shared-view");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstTab, Is.Not.SameAs(secondTab));
                Assert.That(firstTab.Id, Is.Not.EqualTo(Guid.Empty));
                Assert.That(secondTab.Id, Is.Not.EqualTo(Guid.Empty));
                Assert.That(secondTab.Id, Is.Not.EqualTo(firstTab.Id));
                Assert.That(firstTab.Title, Is.EqualTo(secondTab.Title));
                Assert.That(firstTab.ViewTypeKey, Is.EqualTo(secondTab.ViewTypeKey));
            }
        }

        [Test]
        public void VerifyEditorTabItemExposesMutableTitleAndImmutableIdentityAndViewTypeKey()
        {
            var immutablePropertyNames = new[]
            {
                nameof(EditorTabItem.Id),
                nameof(EditorTabItem.ViewTypeKey)
            };

            foreach (var propertyName in immutablePropertyNames)
            {
                var property = typeof(EditorTabItem).GetProperty(propertyName);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(property, Is.Not.Null);
                    Assert.That(property.SetMethod, Is.Null);
                }
            }

            var titleProperty = typeof(EditorTabItem).GetProperty(nameof(EditorTabItem.Title));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(titleProperty, Is.Not.Null);
                Assert.That(titleProperty.SetMethod, Is.Not.Null);
                Assert.That(titleProperty.SetMethod.IsPublic, Is.True);
            }
        }

        [Test]
        public void VerifyEditorTabItemTitleRaisesNotificationWithoutChangingWorkspaceState()
        {
            var viewModel = CreateViewModel();
            var group = viewModel.Groups[0];
            var tab = OpenTab(viewModel, group, "Diagram - Engine", "diagram");
            var focusedGroup = AddGroup(viewModel);
            var groups = viewModel.Groups;
            var tabs = group.Tabs;
            var tabId = tab.Id;
            var viewTypeKey = tab.ViewTypeKey;
            var tabPropertyNotifications = new List<string>();
            var groupPropertyNotifications = new List<string>();
            var workspacePropertyNotifications = new List<string>();
            var collectionNotificationCount = 0;
            INotifyCollectionChanged observableGroups = groups;
            INotifyCollectionChanged observableTabs = tabs;
            observableGroups.CollectionChanged += (_, _) => collectionNotificationCount++;
            observableTabs.CollectionChanged += (_, _) => collectionNotificationCount++;
            tab.PropertyChanged += (_, args) => tabPropertyNotifications.Add(args.PropertyName);
            group.PropertyChanged += (_, args) => groupPropertyNotifications.Add(args.PropertyName);
            viewModel.PropertyChanged += (_, args) => workspacePropertyNotifications.Add(args.PropertyName);

            tab.Title = "  Diagram - Propulsion System  ";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tab.Title, Is.EqualTo("  Diagram - Propulsion System  "));
                Assert.That(tab.Id, Is.EqualTo(tabId));
                Assert.That(tab.ViewTypeKey, Is.EqualTo(viewTypeKey));
                Assert.That(viewModel.Groups, Is.SameAs(groups));
                Assert.That(group.Tabs, Is.SameAs(tabs));
                Assert.That(group.Tabs.Single(), Is.SameAs(tab));
                Assert.That(group.ActiveTab, Is.SameAs(tab));
                Assert.That(viewModel.FocusedGroup, Is.SameAs(focusedGroup));
                Assert.That(tabPropertyNotifications, Is.EqualTo(new[] { nameof(EditorTabItem.Title) }));
                Assert.That(groupPropertyNotifications, Is.Empty);
                Assert.That(
                    workspacePropertyNotifications,
                    Is.EqualTo(new[] { nameof(viewModel.RenderState) }));
                Assert.That(collectionNotificationCount, Is.Zero);
            }
        }

        [Test]
        public void VerifyEditorTabItemTitleRejectsNullWithoutMutation()
        {
            var viewModel = CreateViewModel();
            var group = viewModel.Groups[0];
            var tab = OpenTab(viewModel, group, "Diagram - Engine", "diagram");
            var notifications = new List<string>();
            tab.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

            var exception = Assert.Throws<ArgumentNullException>(() => tab.Title = null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exception.ParamName, Is.EqualTo("title"));
                Assert.That(tab.Title, Is.EqualTo("Diagram - Engine"));
                Assert.That(group.Tabs.Single(), Is.SameAs(tab));
                Assert.That(group.ActiveTab, Is.SameAs(tab));
                Assert.That(notifications, Is.Empty);
            }
        }

        [TestCase("")]
        [TestCase("   ")]
        public void VerifyEditorTabItemTitleRejectsWhitespaceWithoutMutation(string title)
        {
            var viewModel = CreateViewModel();
            var group = viewModel.Groups[0];
            var tab = OpenTab(viewModel, group, "Diagram - Engine", "diagram");
            var notifications = new List<string>();
            tab.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

            var exception = Assert.Throws<ArgumentException>(() => tab.Title = title);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exception.ParamName, Is.EqualTo("title"));
                Assert.That(tab.Title, Is.EqualTo("Diagram - Engine"));
                Assert.That(group.Tabs.Single(), Is.SameAs(tab));
                Assert.That(group.ActiveTab, Is.SameAs(tab));
                Assert.That(notifications, Is.Empty);
            }
        }

        [Test]
        public void VerifyEditorTabItemTitleSuppressesSameValueNotification()
        {
            var viewModel = CreateViewModel();
            var group = viewModel.Groups[0];
            var tab = OpenTab(viewModel, group, "Diagram - Engine", "diagram");
            var notifications = new List<string>();
            tab.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

            tab.Title = "Diagram - Engine";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tab.Title, Is.EqualTo("Diagram - Engine"));
                Assert.That(group.Tabs.Single(), Is.SameAs(tab));
                Assert.That(group.ActiveTab, Is.SameAs(tab));
                Assert.That(notifications, Is.Empty);
            }
        }

        [Test]
        public void VerifyTryOpenTabPreservesMetadataAndFocusesTargetGroup()
        {
            var viewModel = CreateViewModel();
            var targetGroup = AddGroup(viewModel);
            Assert.That(viewModel.FocusGroup(viewModel.Groups[0].Id), Is.True);

            var result = viewModel.TryOpenTab(
                targetGroup.Id,
                "  Preserved title  ",
                "  preserved-view-key  ",
                out var tab);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(tab.Title, Is.EqualTo("  Preserved title  "));
                Assert.That(tab.ViewTypeKey, Is.EqualTo("  preserved-view-key  "));
                Assert.That(targetGroup.Tabs.Single(), Is.SameAs(tab));
                Assert.That(targetGroup.ActiveTab, Is.SameAs(tab));
                Assert.That(viewModel.FocusedGroup, Is.SameAs(targetGroup));
            }
        }

        [Test]
        public void VerifyTryOpenTabRejectsNullTitleBeforeGroupLookup()
        {
            var viewModel = CreateViewModel();
            var groups = viewModel.Groups;

            var exception = Assert.Throws<ArgumentNullException>(() =>
                viewModel.TryOpenTab(Guid.NewGuid(), null, "view-key", out _));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exception.ParamName, Is.EqualTo("title"));
                Assert.That(viewModel.Groups, Is.SameAs(groups));
                Assert.That(viewModel.Groups.Single().Tabs, Is.Empty);
            }
        }

        [TestCase("Title")]
        [TestCase("   ")]
        public void VerifyTryOpenTabRejectsNullViewTypeKeyBeforeGroupLookup(string title)
        {
            var viewModel = CreateViewModel();
            var groups = viewModel.Groups;

            var exception = Assert.Throws<ArgumentNullException>(() =>
                viewModel.TryOpenTab(Guid.NewGuid(), title, null, out _));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exception.ParamName, Is.EqualTo("viewTypeKey"));
                Assert.That(viewModel.Groups, Is.SameAs(groups));
                Assert.That(viewModel.Groups.Single().Tabs, Is.Empty);
            }
        }

        [TestCase("")]
        [TestCase("   ")]
        public void VerifyTryOpenTabRejectsWhitespaceTitleBeforeGroupLookup(string title)
        {
            var viewModel = CreateViewModel();

            var exception = Assert.Throws<ArgumentException>(() =>
                viewModel.TryOpenTab(Guid.NewGuid(), title, "view-key", out _));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exception.ParamName, Is.EqualTo("title"));
                Assert.That(viewModel.Groups.Single().Tabs, Is.Empty);
            }
        }

        [TestCase("")]
        [TestCase("   ")]
        public void VerifyTryOpenTabRejectsWhitespaceViewTypeKeyBeforeGroupLookup(string viewTypeKey)
        {
            var viewModel = CreateViewModel();

            var exception = Assert.Throws<ArgumentException>(() =>
                viewModel.TryOpenTab(Guid.NewGuid(), "Title", viewTypeKey, out _));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exception.ParamName, Is.EqualTo("viewTypeKey"));
                Assert.That(viewModel.Groups.Single().Tabs, Is.Empty);
            }
        }

        [Test]
        public void VerifyTryOpenTabRejectsUnknownGroupWithoutMutationOrNotifications()
        {
            var viewModel = CreateViewModel();
            var group = viewModel.Groups[0];
            var groups = viewModel.Groups;
            var tabs = group.Tabs;
            var collectionNotificationCount = 0;
            var workspacePropertyNotificationCount = 0;
            var groupPropertyNotificationCount = 0;
            INotifyCollectionChanged observableGroups = groups;
            INotifyCollectionChanged observableTabs = tabs;
            observableGroups.CollectionChanged += (_, _) => collectionNotificationCount++;
            observableTabs.CollectionChanged += (_, _) => collectionNotificationCount++;
            viewModel.PropertyChanged += (_, _) => workspacePropertyNotificationCount++;
            group.PropertyChanged += (_, _) => groupPropertyNotificationCount++;

            var result = viewModel.TryOpenTab(Guid.NewGuid(), "Valid", "valid-view", out var rejectedTab);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.False);
                Assert.That(rejectedTab, Is.Null);
                Assert.That(viewModel.Groups, Is.SameAs(groups));
                Assert.That(group.Tabs, Is.SameAs(tabs));
                Assert.That(group.Tabs, Is.Empty);
                Assert.That(group.ActiveTab, Is.Null);
                Assert.That(viewModel.FocusedGroup, Is.SameAs(group));
                Assert.That(collectionNotificationCount, Is.Zero);
                Assert.That(workspacePropertyNotificationCount, Is.Zero);
                Assert.That(groupPropertyNotificationCount, Is.Zero);
            }
        }

        [Test]
        public void VerifyTabsExposeStableOrderedCollectionNotifications()
        {
            var viewModel = CreateViewModel();
            var group = viewModel.Groups[0];
            var tabs = group.Tabs;
            var notifications = new List<NotifyCollectionChangedEventArgs>();
            INotifyCollectionChanged observableTabs = tabs;
            observableTabs.CollectionChanged += (_, args) => notifications.Add(args);

            var firstTab = OpenTab(viewModel, group, "First", "first-view");
            var secondTab = OpenTab(viewModel, group, "Second", "second-view");
            Assert.That(viewModel.CloseTab(group.Id, firstTab.Id), Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(group.Tabs, Is.SameAs(tabs));
                Assert.That(group.Tabs, Is.EqualTo(new[] { secondTab }));
                Assert.That(notifications.Select(args => args.Action), Is.EqualTo(new[]
                {
                    NotifyCollectionChangedAction.Add,
                    NotifyCollectionChangedAction.Add,
                    NotifyCollectionChangedAction.Remove
                }));
                Assert.That(notifications.Select(args => args.NewStartingIndex), Is.EqualTo(new[] { 0, 1, -1 }));
                Assert.That(notifications.Select(args => args.OldStartingIndex), Is.EqualTo(new[] { -1, -1, 0 }));
                Assert.That(notifications[0].NewItems[0], Is.SameAs(firstTab));
                Assert.That(notifications[1].NewItems[0], Is.SameAs(secondTab));
                Assert.That(notifications[2].OldItems[0], Is.SameAs(firstTab));
            }
        }

        [Test]
        public void VerifyGroupsMaintainIndependentActiveTabs()
        {
            var viewModel = CreateViewModel();
            var firstGroup = viewModel.Groups[0];
            var firstTab = OpenTab(viewModel, firstGroup, "First A", "first-a");
            var secondFirstTab = OpenTab(viewModel, firstGroup, "First B", "first-b");
            var secondGroup = AddGroup(viewModel);
            OpenTab(viewModel, secondGroup, "Second A", "second-a");
            var secondActiveTab = OpenTab(viewModel, secondGroup, "Second B", "second-b");

            Assert.That(viewModel.ActivateTab(firstGroup.Id, firstTab.Id), Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstGroup.ActiveTab, Is.SameAs(firstTab));
                Assert.That(firstGroup.ActiveTab, Is.Not.SameAs(secondFirstTab));
                Assert.That(secondGroup.ActiveTab, Is.SameAs(secondActiveTab));
                Assert.That(viewModel.FocusedGroup, Is.SameAs(firstGroup));
            }
        }

        [Test]
        public void VerifyActivateTabFocusesOwningGroupAndSuppressesIdempotentNotifications()
        {
            var viewModel = CreateViewModel();
            var firstGroup = viewModel.Groups[0];
            var targetTab = OpenTab(viewModel, firstGroup, "First", "first-view");
            OpenTab(viewModel, firstGroup, "Second", "second-view");
            var secondGroup = AddGroup(viewModel);
            var groupPropertyNotifications = new List<string>();
            var workspacePropertyNotifications = new List<string>();
            firstGroup.PropertyChanged += (_, args) => groupPropertyNotifications.Add(args.PropertyName);
            viewModel.PropertyChanged += (_, args) => workspacePropertyNotifications.Add(args.PropertyName);

            var firstResult = viewModel.ActivateTab(firstGroup.Id, targetTab.Id);
            var secondResult = viewModel.ActivateTab(firstGroup.Id, targetTab.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstResult, Is.True);
                Assert.That(secondResult, Is.True);
                Assert.That(firstGroup.ActiveTab, Is.SameAs(targetTab));
                Assert.That(viewModel.FocusedGroup, Is.SameAs(firstGroup));
                Assert.That(groupPropertyNotifications, Is.EqualTo(new[] { nameof(firstGroup.ActiveTab) }));
                Assert.That(
                    workspacePropertyNotifications,
                    Is.EqualTo(new[]
                    {
                        nameof(viewModel.FocusedGroup),
                        nameof(viewModel.RenderState)
                    }));
                Assert.That(secondGroup.ActiveTab, Is.Null);
            }
        }

        [Test]
        public void VerifyFocusGroupChangesOnlyFocusAndSuppressesIdempotentNotifications()
        {
            var viewModel = CreateViewModel();
            var firstGroup = viewModel.Groups[0];
            var firstActiveTab = OpenTab(viewModel, firstGroup, "First", "first-view");
            var secondGroup = AddGroup(viewModel);
            var secondActiveTab = OpenTab(viewModel, secondGroup, "Second", "second-view");
            var workspacePropertyNotifications = new List<string>();
            var firstGroupPropertyNotifications = new List<string>();
            var secondGroupPropertyNotifications = new List<string>();
            viewModel.PropertyChanged += (_, args) => workspacePropertyNotifications.Add(args.PropertyName);
            firstGroup.PropertyChanged += (_, args) => firstGroupPropertyNotifications.Add(args.PropertyName);
            secondGroup.PropertyChanged += (_, args) => secondGroupPropertyNotifications.Add(args.PropertyName);

            var firstResult = viewModel.FocusGroup(firstGroup.Id);
            var secondResult = viewModel.FocusGroup(firstGroup.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstResult, Is.True);
                Assert.That(secondResult, Is.True);
                Assert.That(viewModel.FocusedGroup, Is.SameAs(firstGroup));
                Assert.That(firstGroup.ActiveTab, Is.SameAs(firstActiveTab));
                Assert.That(secondGroup.ActiveTab, Is.SameAs(secondActiveTab));
                Assert.That(
                    workspacePropertyNotifications,
                    Is.EqualTo(new[]
                    {
                        nameof(viewModel.FocusedGroup),
                        nameof(viewModel.RenderState)
                    }));
                Assert.That(firstGroupPropertyNotifications, Is.Empty);
                Assert.That(secondGroupPropertyNotifications, Is.Empty);
            }
        }

        [Test]
        public void VerifyUnavailableIdentifiersAreRejectedWithoutMutationOrNotifications()
        {
            var viewModel = CreateViewModel();
            var retainedGroup = viewModel.Groups[0];
            var staleTab = OpenTab(viewModel, retainedGroup, "Stale", "stale-view");
            Assert.That(viewModel.CloseTab(retainedGroup.Id, staleTab.Id), Is.True);
            var removedGroup = AddGroup(viewModel);
            var removedTab = OpenTab(viewModel, removedGroup, "Removed", "removed-view");
            Assert.That(viewModel.CloseTab(removedGroup.Id, removedTab.Id), Is.True);
            var destinationGroup = AddGroup(viewModel);
            var unknownGroupId = Guid.NewGuid();
            var unknownTabId = Guid.NewGuid();
            var groups = viewModel.Groups;
            var expectedGroups = groups.ToArray();
            var focusedGroup = viewModel.FocusedGroup;
            var collectionNotificationCount = 0;
            var propertyNotificationCount = 0;
            INotifyCollectionChanged observableGroups = groups;
            INotifyCollectionChanged observableRetainedTabs = retainedGroup.Tabs;
            INotifyCollectionChanged observableDestinationTabs = destinationGroup.Tabs;
            observableGroups.CollectionChanged += (_, _) => collectionNotificationCount++;
            observableRetainedTabs.CollectionChanged += (_, _) => collectionNotificationCount++;
            observableDestinationTabs.CollectionChanged += (_, _) => collectionNotificationCount++;
            viewModel.PropertyChanged += (_, _) => propertyNotificationCount++;
            retainedGroup.PropertyChanged += (_, _) => propertyNotificationCount++;
            destinationGroup.PropertyChanged += (_, _) => propertyNotificationCount++;

            var openEmptyGroupResult = viewModel.TryOpenTab(Guid.Empty, "Valid", "valid-view", out var emptyGroupTab);
            var openStaleGroupResult = viewModel.TryOpenTab(removedGroup.Id, "Valid", "valid-view", out var staleGroupTab);
            var results = new[]
            {
                viewModel.FocusGroup(Guid.Empty),
                viewModel.FocusGroup(unknownGroupId),
                viewModel.FocusGroup(removedGroup.Id),
                openEmptyGroupResult,
                openStaleGroupResult,
                viewModel.ActivateTab(Guid.Empty, Guid.Empty),
                viewModel.ActivateTab(unknownGroupId, unknownTabId),
                viewModel.ActivateTab(retainedGroup.Id, Guid.Empty),
                viewModel.ActivateTab(retainedGroup.Id, unknownTabId),
                viewModel.ActivateTab(retainedGroup.Id, staleTab.Id),
                viewModel.CloseTab(Guid.Empty, Guid.Empty),
                viewModel.CloseTab(unknownGroupId, unknownTabId),
                viewModel.CloseTab(retainedGroup.Id, Guid.Empty),
                viewModel.CloseTab(retainedGroup.Id, unknownTabId),
                viewModel.CloseTab(retainedGroup.Id, staleTab.Id),
                viewModel.MoveTab(Guid.Empty, Guid.Empty, destinationGroup.Id),
                viewModel.MoveTab(unknownGroupId, unknownTabId, destinationGroup.Id),
                viewModel.MoveTab(retainedGroup.Id, Guid.Empty, destinationGroup.Id),
                viewModel.MoveTab(retainedGroup.Id, unknownTabId, destinationGroup.Id),
                viewModel.MoveTab(removedGroup.Id, removedTab.Id, destinationGroup.Id),
                viewModel.MoveTab(retainedGroup.Id, staleTab.Id, destinationGroup.Id),
                viewModel.MoveTab(retainedGroup.Id, staleTab.Id, unknownGroupId)
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(results, Is.All.False);
                Assert.That(emptyGroupTab, Is.Null);
                Assert.That(staleGroupTab, Is.Null);
                Assert.That(viewModel.Groups, Is.SameAs(groups));
                Assert.That(viewModel.Groups, Is.EqualTo(expectedGroups));
                Assert.That(viewModel.FocusedGroup, Is.SameAs(focusedGroup));
                Assert.That(retainedGroup.Tabs, Is.Empty);
                Assert.That(retainedGroup.ActiveTab, Is.Null);
                Assert.That(destinationGroup.Tabs, Is.Empty);
                Assert.That(destinationGroup.ActiveTab, Is.Null);
                Assert.That(collectionNotificationCount, Is.Zero);
                Assert.That(propertyNotificationCount, Is.Zero);
            }
        }

        [Test]
        public void VerifyWrongGroupTabOwnershipIsRejectedWithoutMutationOrNotifications()
        {
            var viewModel = CreateViewModel();
            var firstGroup = viewModel.Groups[0];
            var firstTab = OpenTab(viewModel, firstGroup, "First", "first-view");
            var secondGroup = AddGroup(viewModel);
            var secondTab = OpenTab(viewModel, secondGroup, "Second", "second-view");
            var firstTabs = firstGroup.Tabs.ToArray();
            var secondTabs = secondGroup.Tabs.ToArray();
            var focusedGroup = viewModel.FocusedGroup;
            var collectionNotificationCount = 0;
            var propertyNotificationCount = 0;
            INotifyCollectionChanged observableFirstTabs = firstGroup.Tabs;
            INotifyCollectionChanged observableSecondTabs = secondGroup.Tabs;
            observableFirstTabs.CollectionChanged += (_, _) => collectionNotificationCount++;
            observableSecondTabs.CollectionChanged += (_, _) => collectionNotificationCount++;
            firstGroup.PropertyChanged += (_, _) => propertyNotificationCount++;
            secondGroup.PropertyChanged += (_, _) => propertyNotificationCount++;
            viewModel.PropertyChanged += (_, _) => propertyNotificationCount++;

            var results = new[]
            {
                viewModel.ActivateTab(firstGroup.Id, secondTab.Id),
                viewModel.CloseTab(firstGroup.Id, secondTab.Id),
                viewModel.MoveTab(firstGroup.Id, secondTab.Id, secondGroup.Id)
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(results, Is.All.False);
                Assert.That(firstGroup.Tabs, Is.EqualTo(firstTabs));
                Assert.That(secondGroup.Tabs, Is.EqualTo(secondTabs));
                Assert.That(firstGroup.ActiveTab, Is.SameAs(firstTab));
                Assert.That(secondGroup.ActiveTab, Is.SameAs(secondTab));
                Assert.That(viewModel.FocusedGroup, Is.SameAs(focusedGroup));
                Assert.That(collectionNotificationCount, Is.Zero);
                Assert.That(propertyNotificationCount, Is.Zero);
            }
        }

        [Test]
        public void VerifyCloseTabSelectsRightNeighborWhenClosingActiveMiddleTab()
        {
            var viewModel = CreateViewModel();
            var group = viewModel.Groups[0];
            var firstTab = OpenTab(viewModel, group, "A", "a");
            var secondTab = OpenTab(viewModel, group, "B", "b");
            var thirdTab = OpenTab(viewModel, group, "C", "c");
            var fourthTab = OpenTab(viewModel, group, "D", "d");
            Assert.That(viewModel.ActivateTab(group.Id, thirdTab.Id), Is.True);

            var result = viewModel.CloseTab(group.Id, thirdTab.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(group.Tabs, Is.EqualTo(new[] { firstTab, secondTab, fourthTab }));
                Assert.That(group.ActiveTab, Is.SameAs(fourthTab));
            }
        }

        [Test]
        public void VerifyCloseTabSelectsLeftNeighborWhenClosingActiveLastTab()
        {
            var viewModel = CreateViewModel();
            var group = viewModel.Groups[0];
            var firstTab = OpenTab(viewModel, group, "A", "a");
            var secondTab = OpenTab(viewModel, group, "B", "b");
            var thirdTab = OpenTab(viewModel, group, "C", "c");

            var result = viewModel.CloseTab(group.Id, thirdTab.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(group.Tabs, Is.EqualTo(new[] { firstTab, secondTab }));
                Assert.That(group.ActiveTab, Is.SameAs(secondTab));
            }
        }

        [Test]
        public void VerifyCloseTabSelectsRightNeighborWhenClosingActiveFirstTab()
        {
            var viewModel = CreateViewModel();
            var group = viewModel.Groups[0];
            var firstTab = OpenTab(viewModel, group, "A", "a");
            var secondTab = OpenTab(viewModel, group, "B", "b");
            var thirdTab = OpenTab(viewModel, group, "C", "c");
            Assert.That(viewModel.ActivateTab(group.Id, firstTab.Id), Is.True);

            var result = viewModel.CloseTab(group.Id, firstTab.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(group.Tabs, Is.EqualTo(new[] { secondTab, thirdTab }));
                Assert.That(group.ActiveTab, Is.SameAs(secondTab));
            }
        }

        [Test]
        public void VerifyCloseTabPreservesActiveTabAndWorkspaceFocusWhenClosingInactiveTab()
        {
            var viewModel = CreateViewModel();
            var firstGroup = viewModel.Groups[0];
            var inactiveTab = OpenTab(viewModel, firstGroup, "Inactive", "inactive-view");
            var activeTab = OpenTab(viewModel, firstGroup, "Active", "active-view");
            var focusedGroup = AddGroup(viewModel);
            var firstGroupPropertyNotifications = new List<string>();
            var workspacePropertyNotifications = new List<string>();
            firstGroup.PropertyChanged += (_, args) => firstGroupPropertyNotifications.Add(args.PropertyName);
            viewModel.PropertyChanged += (_, args) => workspacePropertyNotifications.Add(args.PropertyName);

            var result = viewModel.CloseTab(firstGroup.Id, inactiveTab.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(firstGroup.Tabs.Single(), Is.SameAs(activeTab));
                Assert.That(firstGroup.ActiveTab, Is.SameAs(activeTab));
                Assert.That(viewModel.FocusedGroup, Is.SameAs(focusedGroup));
                Assert.That(firstGroupPropertyNotifications, Is.Empty);
                Assert.That(
                    workspacePropertyNotifications,
                    Is.EqualTo(new[] { nameof(viewModel.RenderState) }));
            }
        }

        [Test]
        public void VerifyCloseTabRetainsFinalEmptyFocusedGroup()
        {
            var viewModel = CreateViewModel();
            var group = viewModel.Groups[0];
            var groups = viewModel.Groups;
            var tab = OpenTab(viewModel, group, "Only", "only-view");
            var groupCollectionNotifications = new List<NotifyCollectionChangedEventArgs>();
            INotifyCollectionChanged observableGroups = groups;
            observableGroups.CollectionChanged += (_, args) => groupCollectionNotifications.Add(args);

            var result = viewModel.CloseTab(group.Id, tab.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(viewModel.Groups, Is.SameAs(groups));
                Assert.That(viewModel.Groups.Single(), Is.SameAs(group));
                Assert.That(group.Tabs, Is.Empty);
                Assert.That(group.ActiveTab, Is.Null);
                Assert.That(viewModel.FocusedGroup, Is.SameAs(group));
                Assert.That(groupCollectionNotifications, Is.Empty);
            }
        }

        [Test]
        public void VerifyCloseTabRemovesRedundantGroupAfterMakingItCoherent()
        {
            var viewModel = CreateViewModel();
            var retainedGroup = viewModel.Groups[0];
            var removedGroup = AddGroup(viewModel);
            var removedTabs = removedGroup.Tabs;
            var tab = OpenTab(viewModel, removedGroup, "Only", "only-view");
            Assert.That(viewModel.FocusGroup(retainedGroup.Id), Is.True);
            var removalState = new List<(int TabCount, EditorTabItem ActiveTab)>();
            INotifyCollectionChanged observableGroups = viewModel.Groups;
            observableGroups.CollectionChanged += (_, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Remove)
                {
                    removalState.Add((removedGroup.Tabs.Count, removedGroup.ActiveTab));
                }
            };

            var result = viewModel.CloseTab(removedGroup.Id, tab.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(viewModel.Groups.Single(), Is.SameAs(retainedGroup));
                Assert.That(viewModel.FocusedGroup, Is.SameAs(retainedGroup));
                Assert.That(removedGroup.Tabs, Is.SameAs(removedTabs));
                Assert.That(removedGroup.Tabs, Is.Empty);
                Assert.That(removedGroup.ActiveTab, Is.Null);
                Assert.That(removalState, Has.Count.EqualTo(1));
                Assert.That(removalState[0].TabCount, Is.Zero);
                Assert.That(removalState[0].ActiveTab, Is.Null);
            }
        }

        [Test]
        public void VerifyCloseTabFocusesGroupAtRemovedIndexWhenAvailable()
        {
            var viewModel = CreateViewModel();
            var firstGroup = viewModel.Groups[0];
            var removedGroup = AddGroup(viewModel);
            var lastGroup = AddGroup(viewModel);
            var tab = OpenTab(viewModel, removedGroup, "Only", "only-view");

            Assert.That(viewModel.CloseTab(removedGroup.Id, tab.Id), Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.Groups, Is.EqualTo(new[] { firstGroup, lastGroup }));
                Assert.That(viewModel.FocusedGroup, Is.SameAs(lastGroup));
                Assert.That(removedGroup.Tabs, Is.Empty);
                Assert.That(removedGroup.ActiveTab, Is.Null);
            }
        }

        [Test]
        public void VerifyCloseTabFocusesPreviousGroupWhenRemovedIndexHasNoSuccessor()
        {
            var viewModel = CreateViewModel();
            var firstGroup = viewModel.Groups[0];
            var middleGroup = AddGroup(viewModel);
            var removedGroup = AddGroup(viewModel);
            var tab = OpenTab(viewModel, removedGroup, "Only", "only-view");

            Assert.That(viewModel.CloseTab(removedGroup.Id, tab.Id), Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.Groups, Is.EqualTo(new[] { firstGroup, middleGroup }));
                Assert.That(viewModel.FocusedGroup, Is.SameAs(middleGroup));
                Assert.That(removedGroup.Tabs, Is.Empty);
                Assert.That(removedGroup.ActiveTab, Is.Null);
            }
        }

        [Test]
        public void VerifyMoveTabReordersBeforeEarlierTabPreservingActiveTabFocusAndIdentity()
        {
            using var viewModel = CreateViewModel();
            var group = viewModel.Groups[0];
            var firstTab = OpenTab(viewModel, group, "A", "a");
            var secondTab = OpenTab(viewModel, group, "B", "b");
            var movedTab = OpenTab(viewModel, group, "C", "c");
            var focusedGroup = AddGroup(viewModel);
            var initialRevision = viewModel.RenderState.Revision;
            var collectionNotifications = new List<NotifyCollectionChangedEventArgs>();
            var publishedStates = new List<WorkspaceEditorRenderState>();
            INotifyCollectionChanged observableTabs = group.Tabs;
            observableTabs.CollectionChanged += (_, args) => collectionNotifications.Add(args);
            viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(viewModel.RenderState))
                {
                    publishedStates.Add(viewModel.RenderState);
                }
            };

            var result = viewModel.MoveTab(group.Id, movedTab.Id, group.Id, secondTab.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(group.Tabs, Is.EqualTo(new[] { firstTab, movedTab, secondTab }));
                Assert.That(group.Tabs[1], Is.SameAs(movedTab));
                Assert.That(group.ActiveTab, Is.SameAs(movedTab));
                Assert.That(viewModel.FocusedGroup, Is.SameAs(focusedGroup));
                Assert.That(collectionNotifications, Has.Count.EqualTo(1));
                Assert.That(collectionNotifications[0].Action, Is.EqualTo(NotifyCollectionChangedAction.Move));
                Assert.That(collectionNotifications[0].OldStartingIndex, Is.EqualTo(2));
                Assert.That(collectionNotifications[0].NewStartingIndex, Is.EqualTo(1));
                Assert.That(collectionNotifications[0].NewItems[0], Is.SameAs(movedTab));
                Assert.That(publishedStates, Has.Count.EqualTo(1));
                Assert.That(publishedStates[0].Revision, Is.EqualTo(initialRevision + 1));
                Assert.That(
                    publishedStates[0].Groups[0].Tabs.Select(tab => tab.Item),
                    Is.EqualTo(new[] { firstTab, movedTab, secondTab }));
            }
        }

        [Test]
        public void VerifyMoveTabReordersBeforeLaterTabAndAppendsWithinSameGroup()
        {
            using var viewModel = CreateViewModel();
            var group = viewModel.Groups[0];
            var firstTab = OpenTab(viewModel, group, "A", "a");
            var secondTab = OpenTab(viewModel, group, "B", "b");
            var thirdTab = OpenTab(viewModel, group, "C", "c");
            var activeTab = OpenTab(viewModel, group, "D", "d");

            var moveBeforeResult = viewModel.MoveTab(group.Id, secondTab.Id, group.Id, activeTab.Id);
            var orderAfterMoveBefore = group.Tabs.ToArray();
            var appendResult = viewModel.MoveTab(group.Id, thirdTab.Id, group.Id, null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(moveBeforeResult, Is.True);
                Assert.That(appendResult, Is.True);
                Assert.That(orderAfterMoveBefore, Is.EqualTo(new[]
                {
                    firstTab,
                    thirdTab,
                    secondTab,
                    activeTab
                }));
                Assert.That(group.Tabs, Is.EqualTo(new[] { firstTab, secondTab, activeTab, thirdTab }));
                Assert.That(group.Tabs[0], Is.SameAs(firstTab));
                Assert.That(group.Tabs[1], Is.SameAs(secondTab));
                Assert.That(group.Tabs[2], Is.SameAs(activeTab));
                Assert.That(group.Tabs[3], Is.SameAs(thirdTab));
                Assert.That(group.ActiveTab, Is.SameAs(activeTab));
                Assert.That(viewModel.FocusedGroup, Is.SameAs(group));
            }
        }

        [Test]
        public void VerifyMoveTabSuppressesEquivalentSameGroupPositions()
        {
            using var viewModel = CreateViewModel();
            var group = viewModel.Groups[0];
            var firstTab = OpenTab(viewModel, group, "A", "a");
            var secondTab = OpenTab(viewModel, group, "B", "b");
            var finalTab = OpenTab(viewModel, group, "C", "c");
            var expectedRenderState = viewModel.RenderState;
            var collectionNotificationCount = 0;
            var propertyNotificationCount = 0;
            INotifyCollectionChanged observableTabs = group.Tabs;
            observableTabs.CollectionChanged += (_, _) => collectionNotificationCount++;
            group.PropertyChanged += (_, _) => propertyNotificationCount++;
            viewModel.PropertyChanged += (_, _) => propertyNotificationCount++;

            var alreadyBeforeResult = viewModel.MoveTab(group.Id, secondTab.Id, group.Id, finalTab.Id);
            var alreadyFinalResult = viewModel.MoveTab(group.Id, finalTab.Id, group.Id, null);
            var beforeSelfResult = viewModel.MoveTab(group.Id, firstTab.Id, group.Id, firstTab.Id);
            var invalidSourceResult = viewModel.MoveTab(group.Id, Guid.NewGuid(), group.Id, null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(alreadyBeforeResult, Is.False);
                Assert.That(alreadyFinalResult, Is.False);
                Assert.That(beforeSelfResult, Is.False);
                Assert.That(invalidSourceResult, Is.False);
                Assert.That(group.Tabs, Is.EqualTo(new[] { firstTab, secondTab, finalTab }));
                Assert.That(group.ActiveTab, Is.SameAs(finalTab));
                Assert.That(viewModel.RenderState, Is.SameAs(expectedRenderState));
                Assert.That(collectionNotificationCount, Is.Zero);
                Assert.That(propertyNotificationCount, Is.Zero);
            }
        }

        [Test]
        public void VerifyMoveTabRejectsInvalidDestinationAnchorsBeforeMutation()
        {
            using var viewModel = CreateViewModel();
            var sourceGroup = viewModel.Groups[0];
            var firstSourceTab = OpenTab(viewModel, sourceGroup, "Source A", "source-a");
            var secondSourceTab = OpenTab(viewModel, sourceGroup, "Source B", "source-b");
            var destinationGroup = AddGroup(viewModel);
            var destinationTab = OpenTab(viewModel, destinationGroup, "Destination", "destination");
            var expectedRenderState = viewModel.RenderState;
            var collectionNotificationCount = 0;
            var propertyNotificationCount = 0;
            INotifyCollectionChanged observableSourceTabs = sourceGroup.Tabs;
            INotifyCollectionChanged observableDestinationTabs = destinationGroup.Tabs;
            observableSourceTabs.CollectionChanged += (_, _) => collectionNotificationCount++;
            observableDestinationTabs.CollectionChanged += (_, _) => collectionNotificationCount++;
            sourceGroup.PropertyChanged += (_, _) => propertyNotificationCount++;
            destinationGroup.PropertyChanged += (_, _) => propertyNotificationCount++;
            viewModel.PropertyChanged += (_, _) => propertyNotificationCount++;

            var sameGroupWrongAnchorResult = viewModel.MoveTab(
                sourceGroup.Id,
                firstSourceTab.Id,
                sourceGroup.Id,
                destinationTab.Id);

            var crossGroupWrongAnchorResult = viewModel.MoveTab(
                sourceGroup.Id,
                firstSourceTab.Id,
                destinationGroup.Id,
                secondSourceTab.Id);

            var unknownAnchorResult = viewModel.MoveTab(
                sourceGroup.Id,
                firstSourceTab.Id,
                destinationGroup.Id,
                Guid.NewGuid());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sameGroupWrongAnchorResult, Is.False);
                Assert.That(crossGroupWrongAnchorResult, Is.False);
                Assert.That(unknownAnchorResult, Is.False);
                Assert.That(sourceGroup.Tabs, Is.EqualTo(new[] { firstSourceTab, secondSourceTab }));
                Assert.That(destinationGroup.Tabs.Single(), Is.SameAs(destinationTab));
                Assert.That(sourceGroup.ActiveTab, Is.SameAs(secondSourceTab));
                Assert.That(destinationGroup.ActiveTab, Is.SameAs(destinationTab));
                Assert.That(viewModel.RenderState, Is.SameAs(expectedRenderState));
                Assert.That(collectionNotificationCount, Is.Zero);
                Assert.That(propertyNotificationCount, Is.Zero);
            }
        }

        [TestCase(0, TestName = "VerifyMoveTabCrossGroupBeforeFirstDestinationTab")]
        [TestCase(1, TestName = "VerifyMoveTabCrossGroupBeforeMiddleDestinationTab")]
        [TestCase(2, TestName = "VerifyMoveTabCrossGroupBeforeFinalDestinationTab")]
        [TestCase(3, TestName = "VerifyMoveTabCrossGroupAppendsAfterFinalDestinationTab")]
        public void VerifyMoveTabSupportsEveryCrossGroupInsertionPosition(int insertionIndex)
        {
            using var viewModel = CreateViewModel();
            var sourceGroup = viewModel.Groups[0];
            var movedTab = OpenTab(viewModel, sourceGroup, "Source", "source");
            var destinationGroup = AddGroup(viewModel);
            var destinationTabs = new[]
            {
                OpenTab(viewModel, destinationGroup, "A", "a"),
                OpenTab(viewModel, destinationGroup, "B", "b"),
                OpenTab(viewModel, destinationGroup, "C", "c")
            };
            Assert.That(viewModel.FocusGroup(sourceGroup.Id), Is.True);
            var initialRevision = viewModel.RenderState.Revision;
            var publishedStates = new List<WorkspaceEditorRenderState>();
            viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(viewModel.RenderState))
                {
                    publishedStates.Add(viewModel.RenderState);
                }
            };

            Guid? beforeTabId = insertionIndex < destinationTabs.Length
                ? destinationTabs[insertionIndex].Id
                : null;
            var expectedOrder = destinationTabs.ToList();
            expectedOrder.Insert(insertionIndex, movedTab);

            var result = viewModel.MoveTab(
                sourceGroup.Id,
                movedTab.Id,
                destinationGroup.Id,
                beforeTabId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(viewModel.Groups.Single(), Is.SameAs(destinationGroup));
                Assert.That(sourceGroup.Tabs, Is.Empty);
                Assert.That(sourceGroup.ActiveTab, Is.Null);
                Assert.That(destinationGroup.Tabs, Is.EqualTo(expectedOrder));
                Assert.That(destinationGroup.Tabs[insertionIndex], Is.SameAs(movedTab));
                Assert.That(destinationGroup.Tabs, Is.Unique);
                Assert.That(destinationGroup.ActiveTab, Is.SameAs(movedTab));
                Assert.That(viewModel.FocusedGroup, Is.SameAs(destinationGroup));
                Assert.That(publishedStates, Has.Count.EqualTo(1));
                Assert.That(publishedStates[0].Revision, Is.EqualTo(initialRevision + 1));
                Assert.That(
                    publishedStates[0].Groups.Single().Tabs.Select(tab => tab.Item),
                    Is.EqualTo(expectedOrder));
            }
        }

        [TestCase(
            0,
            2,
            1,
            0,
            2,
            3,
            TestName = "VerifyMoveTabSameGroupMovesFirstTabBeforeThirdTab")]
        [TestCase(
            3,
            1,
            0,
            3,
            1,
            2,
            TestName = "VerifyMoveTabSameGroupMovesFinalTabBeforeSecondTab")]
        [TestCase(
            1,
            -1,
            0,
            2,
            3,
            1,
            TestName = "VerifyMoveTabSameGroupMovesSecondTabToEnd")]
        public void VerifyMoveTabSupportsEveryRequiredSameGroupInsertionPosition(
            int movedTabIndex,
            int beforeTabIndex,
            int firstExpectedIndex,
            int secondExpectedIndex,
            int thirdExpectedIndex,
            int fourthExpectedIndex)
        {
            using var viewModel = CreateViewModel();
            var group = viewModel.Groups[0];
            var tabs = new[]
            {
                OpenTab(viewModel, group, "A", "a"),
                OpenTab(viewModel, group, "B", "b"),
                OpenTab(viewModel, group, "C", "c"),
                OpenTab(viewModel, group, "D", "d")
            };
            var expectedActiveTab = group.ActiveTab;
            var focusedGroup = AddGroup(viewModel);
            var initialRevision = viewModel.RenderState.Revision;
            var publishedStates = new List<WorkspaceEditorRenderState>();
            viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(viewModel.RenderState))
                {
                    publishedStates.Add(viewModel.RenderState);
                }
            };
            Guid? beforeTabId = beforeTabIndex >= 0
                ? tabs[beforeTabIndex].Id
                : null;
            var expectedOrder = new[]
            {
                tabs[firstExpectedIndex],
                tabs[secondExpectedIndex],
                tabs[thirdExpectedIndex],
                tabs[fourthExpectedIndex]
            };

            var result = viewModel.MoveTab(
                group.Id,
                tabs[movedTabIndex].Id,
                group.Id,
                beforeTabId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(group.Tabs, Is.EqualTo(expectedOrder));
                Assert.That(group.Tabs, Is.Unique);
                Assert.That(group.ActiveTab, Is.SameAs(expectedActiveTab));
                Assert.That(viewModel.FocusedGroup, Is.SameAs(focusedGroup));
                Assert.That(publishedStates, Has.Count.EqualTo(1));
                Assert.That(publishedStates[0].Revision, Is.EqualTo(initialRevision + 1));
                Assert.That(
                    publishedStates[0].Groups[0].Tabs.Select(tab => tab.Item),
                    Is.EqualTo(expectedOrder));
            }
        }

        [Test]
        public void VerifyMoveTabInsertsBeforeDestinationTabAtMaximumGroupCountCoherently()
        {
            using var viewModel = CreateViewModel(maximumGroupCount: 2);
            var sourceGroup = viewModel.Groups[0];
            var retainedSourceTab = OpenTab(viewModel, sourceGroup, "Source A", "source-a");
            var movedTab = OpenTab(viewModel, sourceGroup, "Source B", "source-b");
            var destinationGroup = AddGroup(viewModel);
            var firstDestinationTab = OpenTab(viewModel, destinationGroup, "Destination A", "destination-a");
            var destinationAnchor = OpenTab(viewModel, destinationGroup, "Destination B", "destination-b");
            Assert.That(viewModel.FocusGroup(sourceGroup.Id), Is.True);
            var initialRevision = viewModel.RenderState.Revision;
            var publishedStates = new List<WorkspaceEditorRenderState>();
            viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(viewModel.RenderState))
                {
                    publishedStates.Add(viewModel.RenderState);
                }
            };

            var result = viewModel.MoveTab(
                sourceGroup.Id,
                movedTab.Id,
                destinationGroup.Id,
                destinationAnchor.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(viewModel.Groups, Has.Count.EqualTo(viewModel.MaximumGroupCount));
                Assert.That(sourceGroup.Tabs.Single(), Is.SameAs(retainedSourceTab));
                Assert.That(sourceGroup.ActiveTab, Is.SameAs(retainedSourceTab));
                Assert.That(
                    destinationGroup.Tabs,
                    Is.EqualTo(new[] { firstDestinationTab, movedTab, destinationAnchor }));
                Assert.That(destinationGroup.Tabs[1], Is.SameAs(movedTab));
                Assert.That(destinationGroup.ActiveTab, Is.SameAs(movedTab));
                Assert.That(viewModel.FocusedGroup, Is.SameAs(destinationGroup));
                Assert.That(publishedStates, Has.Count.EqualTo(1));
                Assert.That(publishedStates[0].Revision, Is.EqualTo(initialRevision + 1));
                Assert.That(publishedStates[0].FocusedGroupId, Is.EqualTo(destinationGroup.Id));
                Assert.That(
                    publishedStates[0].Groups[1].Tabs.Select(tab => tab.Item),
                    Is.EqualTo(new[] { firstDestinationTab, movedTab, destinationAnchor }));
            }
        }

        [Test]
        public void VerifyMoveTabTransfersSameInstanceAndUpdatesSourceAndDestinationState()
        {
            var viewModel = CreateViewModel();
            var sourceGroup = viewModel.Groups[0];
            var firstTab = OpenTab(viewModel, sourceGroup, "A", "a");
            var movedTab = OpenTab(viewModel, sourceGroup, "B", "b");
            var thirdTab = OpenTab(viewModel, sourceGroup, "C", "c");
            Assert.That(viewModel.ActivateTab(sourceGroup.Id, movedTab.Id), Is.True);
            var destinationGroup = AddGroup(viewModel);
            var destinationTab = OpenTab(viewModel, destinationGroup, "Destination", "destination");
            Assert.That(viewModel.FocusGroup(sourceGroup.Id), Is.True);
            var movedTabId = movedTab.Id;
            var sourceNotifications = new List<NotifyCollectionChangedEventArgs>();
            var destinationNotifications = new List<NotifyCollectionChangedEventArgs>();
            INotifyCollectionChanged observableSourceTabs = sourceGroup.Tabs;
            INotifyCollectionChanged observableDestinationTabs = destinationGroup.Tabs;
            observableSourceTabs.CollectionChanged += (_, args) => sourceNotifications.Add(args);
            observableDestinationTabs.CollectionChanged += (_, args) => destinationNotifications.Add(args);

            var result = viewModel.MoveTab(sourceGroup.Id, movedTab.Id, destinationGroup.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(sourceGroup.Tabs, Is.EqualTo(new[] { firstTab, thirdTab }));
                Assert.That(sourceGroup.ActiveTab, Is.SameAs(thirdTab));
                Assert.That(destinationGroup.Tabs, Is.EqualTo(new[] { destinationTab, movedTab }));
                Assert.That(destinationGroup.Tabs[^1], Is.SameAs(movedTab));
                Assert.That(destinationGroup.Tabs[^1].Id, Is.EqualTo(movedTabId));
                Assert.That(destinationGroup.ActiveTab, Is.SameAs(movedTab));
                Assert.That(viewModel.FocusedGroup, Is.SameAs(destinationGroup));
                Assert.That(sourceNotifications, Has.Count.EqualTo(1));
                Assert.That(sourceNotifications[0].Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
                Assert.That(sourceNotifications[0].OldStartingIndex, Is.EqualTo(1));
                Assert.That(sourceNotifications[0].OldItems[0], Is.SameAs(movedTab));
                Assert.That(destinationNotifications, Has.Count.EqualTo(1));
                Assert.That(destinationNotifications[0].Action, Is.EqualTo(NotifyCollectionChangedAction.Add));
                Assert.That(destinationNotifications[0].NewStartingIndex, Is.EqualTo(1));
                Assert.That(destinationNotifications[0].NewItems[0], Is.SameAs(movedTab));
            }
        }

        [Test]
        public void VerifyMoveTabSelectsLeftNeighborWhenMovingActiveLastTab()
        {
            var viewModel = CreateViewModel();
            var sourceGroup = viewModel.Groups[0];
            var firstTab = OpenTab(viewModel, sourceGroup, "A", "a");
            var secondTab = OpenTab(viewModel, sourceGroup, "B", "b");
            var movedTab = OpenTab(viewModel, sourceGroup, "C", "c");
            var destinationGroup = AddGroup(viewModel);

            Assert.That(viewModel.MoveTab(sourceGroup.Id, movedTab.Id, destinationGroup.Id), Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sourceGroup.Tabs, Is.EqualTo(new[] { firstTab, secondTab }));
                Assert.That(sourceGroup.ActiveTab, Is.SameAs(secondTab));
                Assert.That(destinationGroup.Tabs.Single(), Is.SameAs(movedTab));
                Assert.That(destinationGroup.ActiveTab, Is.SameAs(movedTab));
            }
        }

        [Test]
        public void VerifyMoveTabPreservesInactiveSourceTabSelection()
        {
            var viewModel = CreateViewModel();
            var sourceGroup = viewModel.Groups[0];
            var movedTab = OpenTab(viewModel, sourceGroup, "Move", "move-view");
            var activeTab = OpenTab(viewModel, sourceGroup, "Active", "active-view");
            var destinationGroup = AddGroup(viewModel);
            var sourcePropertyNotifications = new List<string>();
            sourceGroup.PropertyChanged += (_, args) => sourcePropertyNotifications.Add(args.PropertyName);

            Assert.That(viewModel.MoveTab(sourceGroup.Id, movedTab.Id, destinationGroup.Id), Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sourceGroup.Tabs.Single(), Is.SameAs(activeTab));
                Assert.That(sourceGroup.ActiveTab, Is.SameAs(activeTab));
                Assert.That(sourcePropertyNotifications, Is.Empty);
                Assert.That(destinationGroup.Tabs.Single(), Is.SameAs(movedTab));
            }
        }

        [Test]
        public void VerifyMoveTabRemovesEmptySourceAfterMakingItCoherent()
        {
            var viewModel = CreateViewModel();
            var sourceGroup = viewModel.Groups[0];
            var sourceTabs = sourceGroup.Tabs;
            var movedTab = OpenTab(viewModel, sourceGroup, "Only", "only-view");
            var destinationGroup = AddGroup(viewModel);
            var removalState = new List<(int TabCount, EditorTabItem ActiveTab)>();
            INotifyCollectionChanged observableGroups = viewModel.Groups;
            observableGroups.CollectionChanged += (_, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Remove)
                {
                    removalState.Add((sourceGroup.Tabs.Count, sourceGroup.ActiveTab));
                }
            };

            var result = viewModel.MoveTab(sourceGroup.Id, movedTab.Id, destinationGroup.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(viewModel.Groups.Single(), Is.SameAs(destinationGroup));
                Assert.That(viewModel.FocusedGroup, Is.SameAs(destinationGroup));
                Assert.That(sourceGroup.Tabs, Is.SameAs(sourceTabs));
                Assert.That(sourceGroup.Tabs, Is.Empty);
                Assert.That(sourceGroup.ActiveTab, Is.Null);
                Assert.That(destinationGroup.Tabs.Single(), Is.SameAs(movedTab));
                Assert.That(destinationGroup.ActiveTab, Is.SameAs(movedTab));
                Assert.That(removalState, Has.Count.EqualTo(1));
                Assert.That(removalState[0].TabCount, Is.Zero);
                Assert.That(removalState[0].ActiveTab, Is.Null);
            }
        }

        [Test]
        public void VerifyMoveTabRejectsSameGroupWithoutMutationOrNotifications()
        {
            var viewModel = CreateViewModel();
            var group = viewModel.Groups[0];
            var firstTab = OpenTab(viewModel, group, "First", "first-view");
            var secondTab = OpenTab(viewModel, group, "Second", "second-view");
            var tabs = group.Tabs;
            var expectedTabs = tabs.ToArray();
            var activeTab = group.ActiveTab;
            var focusedGroup = viewModel.FocusedGroup;
            var collectionNotificationCount = 0;
            var propertyNotificationCount = 0;
            INotifyCollectionChanged observableTabs = tabs;
            observableTabs.CollectionChanged += (_, _) => collectionNotificationCount++;
            group.PropertyChanged += (_, _) => propertyNotificationCount++;
            viewModel.PropertyChanged += (_, _) => propertyNotificationCount++;

            var result = viewModel.MoveTab(group.Id, firstTab.Id, group.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.False);
                Assert.That(group.Tabs, Is.SameAs(tabs));
                Assert.That(group.Tabs, Is.EqualTo(expectedTabs));
                Assert.That(group.Tabs[0], Is.SameAs(firstTab));
                Assert.That(group.Tabs[1], Is.SameAs(secondTab));
                Assert.That(group.ActiveTab, Is.SameAs(activeTab));
                Assert.That(viewModel.FocusedGroup, Is.SameAs(focusedGroup));
                Assert.That(collectionNotificationCount, Is.Zero);
                Assert.That(propertyNotificationCount, Is.Zero);
            }
        }

        [Test]
        public void VerifyMoveTabRejectsUnknownDestinationWithoutMutationOrNotifications()
        {
            var viewModel = CreateViewModel();
            var sourceGroup = viewModel.Groups[0];
            var tab = OpenTab(viewModel, sourceGroup, "Only", "only-view");
            var tabs = sourceGroup.Tabs;
            var collectionNotificationCount = 0;
            var propertyNotificationCount = 0;
            INotifyCollectionChanged observableGroups = viewModel.Groups;
            INotifyCollectionChanged observableTabs = tabs;
            observableGroups.CollectionChanged += (_, _) => collectionNotificationCount++;
            observableTabs.CollectionChanged += (_, _) => collectionNotificationCount++;
            sourceGroup.PropertyChanged += (_, _) => propertyNotificationCount++;
            viewModel.PropertyChanged += (_, _) => propertyNotificationCount++;

            var result = viewModel.MoveTab(sourceGroup.Id, tab.Id, Guid.NewGuid());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.False);
                Assert.That(sourceGroup.Tabs, Is.SameAs(tabs));
                Assert.That(sourceGroup.Tabs.Single(), Is.SameAs(tab));
                Assert.That(sourceGroup.ActiveTab, Is.SameAs(tab));
                Assert.That(viewModel.Groups.Single(), Is.SameAs(sourceGroup));
                Assert.That(viewModel.FocusedGroup, Is.SameAs(sourceGroup));
                Assert.That(collectionNotificationCount, Is.Zero);
                Assert.That(propertyNotificationCount, Is.Zero);
            }
        }

        [Test]
        public void VerifyWorkspaceEditorViewModelInstancesOwnIndependentState()
        {
            var firstViewModel = CreateViewModel();
            var secondViewModel = CreateViewModel();
            var firstInitialGroup = firstViewModel.Groups[0];
            var secondInitialGroup = secondViewModel.Groups[0];
            var firstTab = OpenTab(firstViewModel, firstInitialGroup, "First", "shared-view");
            var addedFirstGroup = AddGroup(firstViewModel);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstViewModel.Groups, Is.Not.SameAs(secondViewModel.Groups));
                Assert.That(firstInitialGroup, Is.Not.SameAs(secondInitialGroup));
                Assert.That(firstInitialGroup.Id, Is.Not.EqualTo(secondInitialGroup.Id));
                Assert.That(firstViewModel.Groups, Has.Count.EqualTo(2));
                Assert.That(secondViewModel.Groups, Has.Count.EqualTo(1));
                Assert.That(firstInitialGroup.Tabs.Single(), Is.SameAs(firstTab));
                Assert.That(secondInitialGroup.Tabs, Is.Empty);
                Assert.That(firstViewModel.FocusedGroup, Is.SameAs(addedFirstGroup));
                Assert.That(secondViewModel.FocusedGroup, Is.SameAs(secondInitialGroup));
            }
        }

        [Test]
        public void VerifyRenderStateCapturesCoherentImmutableOrderedSnapshot()
        {
            using var viewModel = CreateViewModel();
            var firstGroup = viewModel.Groups[0];
            var firstTab = OpenTab(viewModel, firstGroup, "First", "shared-view");
            var secondTab = OpenTab(viewModel, firstGroup, "Second", "shared-view");
            Assert.That(viewModel.ActivateTab(firstGroup.Id, firstTab.Id), Is.True);
            var secondGroup = AddGroup(viewModel);
            var thirdTab = OpenTab(viewModel, secondGroup, "Third", "shared-view");
            Assert.That(viewModel.FocusGroup(firstGroup.Id), Is.True);
            var snapshot = viewModel.RenderState;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot.Revision, Is.GreaterThan(0));
                Assert.That(snapshot.FocusedGroupId, Is.EqualTo(firstGroup.Id));
                Assert.That(snapshot.Groups.Select(group => group.Id),
                    Is.EqualTo(new[] { firstGroup.Id, secondGroup.Id }));
                Assert.That(snapshot.Groups[0].ActiveTabId, Is.EqualTo(firstTab.Id));
                Assert.That(snapshot.Groups[0].Tabs.Select(tab => tab.Id),
                    Is.EqualTo(new[] { firstTab.Id, secondTab.Id }));
                Assert.That(snapshot.Groups[0].Tabs.Select(tab => tab.Title),
                    Is.EqualTo(ExpectedInitialTabTitles));
                Assert.That(snapshot.Groups[0].Tabs[0].Item, Is.SameAs(firstTab));
                Assert.That(snapshot.Groups[0].Tabs[1].Item, Is.SameAs(secondTab));
                Assert.That(snapshot.Groups[1].ActiveTabId, Is.EqualTo(thirdTab.Id));
                Assert.That(snapshot.Groups[1].Tabs.Single().Item, Is.SameAs(thirdTab));
            }

            secondTab.Title = "Captured later";
            var updatedSnapshot = viewModel.RenderState;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(updatedSnapshot, Is.Not.SameAs(snapshot));
                Assert.That(updatedSnapshot.Revision, Is.EqualTo(snapshot.Revision + 1));
                Assert.That(snapshot.Groups[0].Tabs[1].Title, Is.EqualTo("Second"));
                Assert.That(updatedSnapshot.Groups[0].Tabs[1].Title, Is.EqualTo("Captured later"));
                Assert.That(updatedSnapshot.Groups[0].Tabs[1].Item, Is.SameAs(secondTab));
            }
        }

        [Test]
        public void VerifyRenderStatePublishesOncePerCompoundMutationAndSuppressesNoOps()
        {
            using var viewModel = CreateViewModel(maximumGroupCount: 2);
            var revisions = new List<long>();
            viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(viewModel.RenderState))
                {
                    revisions.Add(viewModel.RenderState.Revision);
                }
            };

            var firstGroup = viewModel.Groups[0];
            var initialRevision = viewModel.RenderState.Revision;
            var secondGroup = AddGroup(viewModel);
            var revisionAfterGroup = viewModel.RenderState.Revision;
            var tab = OpenTab(viewModel, secondGroup, "Editor", "editor-view");
            var revisionAfterTab = viewModel.RenderState.Revision;

            var focusResult = viewModel.FocusGroup(secondGroup.Id);
            var activateResult = viewModel.ActivateTab(secondGroup.Id, tab.Id);
            var addGroupResult = viewModel.TryAddGroup(out var rejectedGroup);
            var closeResult = viewModel.CloseTab(firstGroup.Id, Guid.NewGuid());
            var moveResult = viewModel.MoveTab(secondGroup.Id, tab.Id, secondGroup.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(focusResult, Is.True);
                Assert.That(activateResult, Is.True);
                Assert.That(addGroupResult, Is.False);
                Assert.That(closeResult, Is.False);
                Assert.That(moveResult, Is.False);
                Assert.That(revisionAfterGroup, Is.EqualTo(initialRevision + 1));
                Assert.That(revisionAfterTab, Is.EqualTo(revisionAfterGroup + 1));
                Assert.That(viewModel.RenderState.Revision, Is.EqualTo(revisionAfterTab));
                Assert.That(revisions, Is.EqualTo(new[] { revisionAfterGroup, revisionAfterTab }));
                Assert.That(rejectedGroup, Is.Null);
            }
        }

        [Test]
        public void VerifyRemovedChildrenAndDisposalStopRenderPublication()
        {
            var viewModel = CreateViewModel();
            var retainedGroup = viewModel.Groups[0];
            _ = OpenTab(viewModel, retainedGroup, "Retained", "retained-view");
            var removedGroup = AddGroup(viewModel);
            var removedTab = OpenTab(viewModel, removedGroup, "Removed", "removed-view");

            Assert.That(viewModel.CloseTab(removedGroup.Id, removedTab.Id), Is.True);
            var revisionAfterRemoval = viewModel.RenderState.Revision;

            removedTab.Title = "Detached";
            Assert.That(viewModel.RenderState.Revision, Is.EqualTo(revisionAfterRemoval));

            viewModel.Dispose();
            var retainedTab = retainedGroup.Tabs.Single();
            retainedTab.Title = "After disposal";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.RenderState.Revision, Is.EqualTo(revisionAfterRemoval));
                Assert.That(viewModel.TryAddGroup(out var disposedGroup), Is.False);
                Assert.That(disposedGroup, Is.Null);
                Assert.DoesNotThrow(viewModel.Dispose);
            }
        }

        private static WorkspaceEditorViewModel CreateViewModel(
            int maximumGroupCount = DefaultMaximumGroupCount)
        {
            return new WorkspaceEditorViewModel(
                Options.Create(new WorkspaceEditorOptions
                {
                    MaximumGroupCount = maximumGroupCount
                }));
        }

        private static EditorGroupViewModel AddGroup(WorkspaceEditorViewModel viewModel)
        {
            Assert.That(viewModel.TryAddGroup(out var group), Is.True);

            return group;
        }

        private static EditorTabItem OpenTab(
            WorkspaceEditorViewModel viewModel,
            EditorGroupViewModel group,
            string title,
            string viewTypeKey)
        {
            Assert.That(viewModel.TryOpenTab(group.Id, title, viewTypeKey, out var tab), Is.True);

            return tab;
        }
    }
}
