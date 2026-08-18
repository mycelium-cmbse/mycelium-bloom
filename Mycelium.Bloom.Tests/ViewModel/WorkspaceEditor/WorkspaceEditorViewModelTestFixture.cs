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

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.ViewModel.WorkspaceEditor;

    [TestFixture]
    public sealed class WorkspaceEditorViewModelTestFixture
    {
        [Test]
        public void VerifyConstructorCreatesInitialWorkspaceState()
        {
            var viewModel = new WorkspaceEditorViewModel();
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
        public void VerifyTryAddGroupAppendsEmptyGroupsAndFocusesNewestGroup()
        {
            var viewModel = new WorkspaceEditorViewModel();
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
            var viewModel = new WorkspaceEditorViewModel();
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
        public void VerifyGroupsExposeStableOrderedCollectionNotifications()
        {
            var viewModel = new WorkspaceEditorViewModel();
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
            var viewModel = new WorkspaceEditorViewModel();
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
            var viewModel = new WorkspaceEditorViewModel();
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
        public void VerifyEditorTabItemExposesImmutableIdentityAndMetadata()
        {
            var propertyNames = new[]
            {
                nameof(EditorTabItem.Id),
                nameof(EditorTabItem.Title),
                nameof(EditorTabItem.ViewTypeKey)
            };

            foreach (var propertyName in propertyNames)
            {
                var property = typeof(EditorTabItem).GetProperty(propertyName);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(property, Is.Not.Null);
                    Assert.That(property.SetMethod, Is.Null);
                }
            }
        }

        [Test]
        public void VerifyTryOpenTabPreservesMetadataAndFocusesTargetGroup()
        {
            var viewModel = new WorkspaceEditorViewModel();
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
            var viewModel = new WorkspaceEditorViewModel();
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

        [Test]
        public void VerifyTryOpenTabRejectsNullViewTypeKeyBeforeGroupLookup()
        {
            var viewModel = new WorkspaceEditorViewModel();
            var groups = viewModel.Groups;

            var exception = Assert.Throws<ArgumentNullException>(() =>
                viewModel.TryOpenTab(Guid.NewGuid(), "Title", null, out _));

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
            var viewModel = new WorkspaceEditorViewModel();

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
            var viewModel = new WorkspaceEditorViewModel();

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
            var viewModel = new WorkspaceEditorViewModel();
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
            var viewModel = new WorkspaceEditorViewModel();
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
            var viewModel = new WorkspaceEditorViewModel();
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
            var viewModel = new WorkspaceEditorViewModel();
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
                Assert.That(workspacePropertyNotifications, Is.EqualTo(new[] { nameof(viewModel.FocusedGroup) }));
                Assert.That(secondGroup.ActiveTab, Is.Null);
            }
        }

        [Test]
        public void VerifyFocusGroupChangesOnlyFocusAndSuppressesIdempotentNotifications()
        {
            var viewModel = new WorkspaceEditorViewModel();
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
                Assert.That(workspacePropertyNotifications, Is.EqualTo(new[] { nameof(viewModel.FocusedGroup) }));
                Assert.That(firstGroupPropertyNotifications, Is.Empty);
                Assert.That(secondGroupPropertyNotifications, Is.Empty);
            }
        }

        [Test]
        public void VerifyUnavailableIdentifiersAreRejectedWithoutMutationOrNotifications()
        {
            var viewModel = new WorkspaceEditorViewModel();
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
            var viewModel = new WorkspaceEditorViewModel();
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
            var viewModel = new WorkspaceEditorViewModel();
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
            var viewModel = new WorkspaceEditorViewModel();
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
            var viewModel = new WorkspaceEditorViewModel();
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
            var viewModel = new WorkspaceEditorViewModel();
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
                Assert.That(workspacePropertyNotifications, Is.Empty);
            }
        }

        [Test]
        public void VerifyCloseTabRetainsFinalEmptyFocusedGroup()
        {
            var viewModel = new WorkspaceEditorViewModel();
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
            var viewModel = new WorkspaceEditorViewModel();
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
            var viewModel = new WorkspaceEditorViewModel();
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
            var viewModel = new WorkspaceEditorViewModel();
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
        public void VerifyMoveTabTransfersSameInstanceAndUpdatesSourceAndDestinationState()
        {
            var viewModel = new WorkspaceEditorViewModel();
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
            var viewModel = new WorkspaceEditorViewModel();
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
            var viewModel = new WorkspaceEditorViewModel();
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
            var viewModel = new WorkspaceEditorViewModel();
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
            var viewModel = new WorkspaceEditorViewModel();
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
            var viewModel = new WorkspaceEditorViewModel();
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
            var firstViewModel = new WorkspaceEditorViewModel();
            var secondViewModel = new WorkspaceEditorViewModel();
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
