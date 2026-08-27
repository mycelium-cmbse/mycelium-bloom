// ------------------------------------------------------------------------------------------------
// <copyright file="EditorWorkspaceTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.EditorWorkspace
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using BlazorBlueprint.Components;
    using BlazorBlueprint.Primitives;
    using BlazorBlueprint.Primitives.Services;

    using Bunit;

    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;
    using Microsoft.Extensions.Options;

    using Moq;

    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.WorkspaceEditor;

    using ActionMenuComponent = Mycelium.Bloom.Components.UI.Molecules.ActionMenu.ActionMenu;
    using EditorWorkspaceComponent = Mycelium.Bloom.Components.UI.Organisms.EditorWorkspace.EditorWorkspace;

    /// <summary>
    /// Tests the <see cref="EditorWorkspaceComponent" /> component as a consumer of the workspace editor contract.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class EditorWorkspaceTestFixture : BunitContext
    {
        private const string JavaScriptModulePath =
            "./Components/UI/Organisms/EditorWorkspace/EditorWorkspace.razor.js";

        private const string WorkspaceSelector = "[data-testid='editor-workspace']";

        private const string GroupSelector = "[data-testid='editor-workspace-group']";

        private const string TabListSelector = "[data-testid='editor-workspace-tablist']";

        private const string TabSelector = "[data-testid='editor-workspace-tab']";

        private const string CloseSelector = "[data-testid='editor-workspace-tab-close']";

        private const string AddTabSelector = "[data-testid='editor-workspace-add-tab']";

        private const string AddTabHostSelector = "[data-testid='editor-workspace-add-tab-host']";

        private const string SplitterSelector = "[data-testid='editor-workspace-splitter']";

        private const string SplitAddSelector = "[data-testid='editor-workspace-split-add']";

        private const string RightEdgeSplitAddSelector = "[data-testid='editor-workspace-right-edge-split-add']";

        private const string DropHitZoneSelector = "[data-testid='editor-workspace-tab-drop-hit-zone']";

        private const string DropMarkerSelector = "[data-testid='editor-workspace-tab-drop-marker']";

        private const string ActiveDropMarkerSelector =
            DropMarkerSelector + ".mb-editor-workspace__tab-drop-marker--active";

        private const string GroupDropSurfaceSelector =
            "[data-testid='editor-workspace-group-drop-surface']";

        private const string TabPanelSelector = "[data-testid='editor-workspace-tabpanel']";

        private static readonly string[] ExpectedDuplicateCompactSwitcherTitles =
        [
            "Shared",
            "Shared"
        ];

        private static readonly string[] ExpectedDuplicateCompactSwitcherAriaLabels =
        [
            "Editor group 1: Shared",
            "Editor group 2: Shared"
        ];

        private readonly IRenderedComponent<BbPortalHost> portalHost;

        public EditorWorkspaceTestFixture()
        {
            this.portalHost = BlueprintTestSetup.ConfigureWithPortalHost(this);
            this.JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [TearDown]
        public Task TearDown()
        {
            return this.DisposeAsync().AsTask();
        }

        [Test]
        public void VerifyRenderUsesIndependentTabSemanticsAndExactActiveItems()
        {
            var state = CreateViewModel();
            var firstGroup = state.Groups[0];
            var inactiveTab = OpenTab(state, firstGroup, "Model Browser", "shared-view");
            var firstActiveTab = OpenTab(state, firstGroup, "Part Tree", "shared-view");
            var secondGroup = AddGroup(state);
            var secondActiveTab = OpenTab(state, secondGroup, "Diagram", "shared-view");
            var renderedItems = new List<EditorTabItem>();
            var viewModel = CreateConsumerMock(state);

            using var component = this.RenderWorkspace(
                viewModel.Object,
                tab => builder =>
                {
                    renderedItems.Add(tab);
                    builder.OpenElement(0, "span");
                    builder.AddAttribute(1, "data-rendered-tab-id", tab.Id);
                    builder.AddContent(2, $"Rendered {tab.Title}");
                    builder.CloseElement();
                });

            var groups = component.FindAll(GroupSelector);
            var tabLists = component.FindAll(TabListSelector);
            var tabs = component.FindAll(TabSelector);
            var panels = component.FindAll(TabPanelSelector);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(WorkspaceSelector).GetAttribute("role"), Is.EqualTo("region"));
                Assert.That(groups, Has.Count.EqualTo(2));
                Assert.That(groups.All(group => group.GetAttribute("role") == "region"), Is.True);
                Assert.That(tabLists, Has.Count.EqualTo(2));
                Assert.That(tabLists.All(tabList => tabList.GetAttribute("role") == "tablist"), Is.True);
                Assert.That(tabs, Has.Count.EqualTo(3));
                Assert.That(tabs.All(tab => tab.GetAttribute("role") == "tab"), Is.True);
                Assert.That(panels, Has.Count.EqualTo(2));
                Assert.That(panels.All(panel => panel.GetAttribute("role") == "tabpanel"), Is.True);
                Assert.That(renderedItems.Any(item => ReferenceEquals(item, firstActiveTab)), Is.True);
                Assert.That(renderedItems.Any(item => ReferenceEquals(item, secondActiveTab)), Is.True);
                Assert.That(
                    renderedItems.All(item =>
                        ReferenceEquals(item, firstActiveTab) || ReferenceEquals(item, secondActiveTab)),
                    Is.True);
                Assert.That(renderedItems.Any(item => ReferenceEquals(item, inactiveTab)), Is.False);
                Assert.That(component.FindAll($"[data-rendered-tab-id='{inactiveTab.Id}']"), Is.Empty);
            }

            foreach (var tab in tabs)
            {
                var tabId = Guid.Parse(tab.GetAttribute("data-tab-id"));
                var isActive = tabId == firstActiveTab.Id || tabId == secondActiveTab.Id;

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(tab.Id, Is.Not.Empty);
                    Assert.That(tab.GetAttribute("aria-selected"), Is.EqualTo(isActive ? "true" : "false"));
                    Assert.That(tab.GetAttribute("tabindex"), Is.EqualTo(isActive ? "0" : "-1"));
                    Assert.That(tab.GetAttribute("draggable"), Is.EqualTo("true"));
                    Assert.That(tab.QuerySelector(CloseSelector), Is.Null);
                }

                if (isActive)
                {
                    var panel = component.Find($"#{tab.GetAttribute("aria-controls")}");
                    Assert.That(panel.GetAttribute("aria-labelledby"), Is.EqualTo(tab.Id));
                }
            }

            Assert.Multiple(() =>
            {
                Assert.That(tabs.Select(tab => tab.Id), Is.Unique);
                Assert.That(panels.Select(panel => panel.Id), Is.Unique);
                Assert.That(component.FindAll(DropHitZoneSelector), Has.Count.EqualTo(6));
                Assert.That(component.FindAll(DropMarkerSelector), Has.Count.EqualTo(5));
                Assert.That(component.FindAll(
                        $"{DropMarkerSelector}.mb-editor-workspace__tab-drop-marker--active"),
                    Is.Empty);
                Assert.That(component.FindAll(GroupDropSurfaceSelector), Has.Count.EqualTo(2));
                Assert.That(component.FindAll(GroupDropSurfaceSelector)
                    .All(surface => surface.GetAttribute("tabindex") is null), Is.True);
            });
        }

        [Test]
        public void VerifyRenderingUsesOnlyCapturedRootSnapshotDurableState()
        {
            using var liveGraph = CreateViewModel();
            var liveTab = OpenTab(liveGraph, liveGraph.Groups[0], "Live graph title", "live-view");
            using var snapshotOwner = CreateViewModel();
            var snapshotGroup = snapshotOwner.Groups[0];
            var snapshotTab = OpenTab(snapshotOwner, snapshotGroup, "Captured snapshot title", "snapshot-view");
            var viewModel = new Mock<IWorkspaceEditorViewModel>();
            viewModel.SetupGet(model => model.Groups).Returns(liveGraph.Groups);
            viewModel.SetupGet(model => model.FocusedGroup).Returns(liveGraph.FocusedGroup);
            viewModel.SetupGet(model => model.RenderState).Returns(snapshotOwner.RenderState);
            var renderedItems = new List<EditorTabItem>();

            using var component = this.RenderWorkspace(
                viewModel.Object,
                tab => builder =>
                {
                    renderedItems.Add(tab);
                    builder.AddContent(0, tab.Title);
                });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(GroupSelector), Has.Count.EqualTo(1));
                Assert.That(component.Find(GroupSelector).GetAttribute("data-group-id"),
                    Is.EqualTo(snapshotGroup.Id.ToString()));
                Assert.That(component.Find(TabSelector).GetAttribute("data-tab-id"),
                    Is.EqualTo(snapshotTab.Id.ToString()));
                Assert.That(component.Find(TabSelector).TextContent, Does.Contain("Captured snapshot title"));
                Assert.That(component.Markup, Does.Not.Contain("Live graph title"));
                Assert.That(component.Markup, Does.Not.Contain(liveTab.Id.ToString()));
                Assert.That(renderedItems, Is.All.SameAs(snapshotTab));
            }
        }

        [Test]
        public void VerifyEditorWorkspaceStyleContracts()
        {
            var componentDirectory = Path.Combine(
                TestRepository.GetRootPath(),
                "Mycelium.Bloom",
                "Components",
                "UI",
                "Organisms",
                "EditorWorkspace");
            var style = File.ReadAllText(Path.Combine(componentDirectory, "EditorWorkspace.razor.css"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-editor-workspace__group\s*\{[^}]*grid-template-rows:\s*38px\s+minmax\(0,\s*1fr\);"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-editor-workspace__tab-bar\s*\{[^}]*height:\s*38px;[^}]*padding:\s*0;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-editor-workspace__tablist\s*\{[^}]*padding:\s*0;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-editor-workspace__tab-item\s*\{[^}]*max-width:\s*100%;[^}]*height:\s*38px;[^}]*padding:\s*0\s+14px;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-editor-workspace__tab-leading-content\s*\{[^}]*display:\s*inline-flex;[^}]*flex:\s*0\s+0\s+auto;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-editor-workspace__tab-item--active::before\s*\{[^}]*top:\s*0;[^}]*height:\s*2px;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-editor-workspace__add-tab\s*\{[^}]*width:\s*32px;[^}]*height:\s*38px;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-editor-workspace__splitter\s*\{[^}]*width:\s*16px;[^}]*transform:\s*translateX\(-8px\);"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-editor-workspace__splitter-line\s*\{[^}]*width:\s*var\(--mb-border-width-sm\);[^}]*height:\s*100%;"));
                Assert.That(style, Does.Contain("@container mb-editor-workspace (max-width: 45rem)"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-editor-workspace__tab-drop-hit-zone\s*\{[^}]*position:\s*absolute;[^}]*z-index:\s*3;[^}]*width:\s*50%;[^}]*pointer-events:\s*none;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-editor-workspace__tab-item--dragging\s*\{[^}]*z-index:\s*3;[^}]*opacity:\s*0\.55;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-editor-workspace__tab-drop-hit-zone--left\s*\{[^}]*left:\s*0;[^}]*\}\s*\.mb-editor-workspace__tab-drop-hit-zone--right\s*\{[^}]*right:\s*0;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-editor-workspace__group-drop-surface\s*\{[^}]*position:\s*absolute;[^}]*z-index:\s*2;[^}]*inset:\s*0;[^}]*pointer-events:\s*none;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-editor-workspace__tab-drop-marker\s*\{[^}]*width:\s*var\(--mb-spacing-4\);[^}]*pointer-events:\s*none;[^}]*transform:\s*scaleX\(0\);"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-editor-workspace__tab-drop-marker--before\s*\{[^}]*left:\s*0;[^}]*border-left:\s*var\(--mb-border-width-sm\)\s+solid\s+var\(--mb-color-border-selected\);[^}]*transform-origin:\s*left\s+center;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-editor-workspace__tab-drop-marker--end\s*\{[^}]*right:\s*0;[^}]*border-right:\s*var\(--mb-border-width-sm\)\s+solid\s+var\(--mb-color-border-selected\);[^}]*transform-origin:\s*right\s+center;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-editor-workspace__tab-drop-marker--active\s*\{[^}]*opacity:\s*1;[^}]*transform:\s*scaleX\(1\);"));
                Assert.That(style, Does.Contain("mb-editor-workspace__split-add-host"));
                Assert.That(style, Does.Contain("mb-editor-workspace__right-edge-split-add-host"));
            }
        }

        [Test]
        public async Task VerifyActiveAndInactiveTitleChangesRerenderWithIdentityAndSelectionCoherence()
        {
            var state = CreateViewModel();
            var group = state.Groups[0];
            var inactiveTab = OpenTab(state, group, "Inactive", "shared-view");
            var activeTab = OpenTab(state, group, "Active", "shared-view");
            var inactiveTabId = inactiveTab.Id;
            var activeTabId = activeTab.Id;
            var renderedItems = new List<EditorTabItem>();

            using var component = this.RenderWorkspace(
                state,
                tab => builder =>
                {
                    renderedItems.Add(tab);
                    builder.OpenElement(0, "span");
                    builder.AddAttribute(1, "data-rendered-tab-id", tab.Id);
                    builder.AddContent(2, tab.Title);
                    builder.CloseElement();
                });
            var initialRenderCount = component.RenderCount;

            inactiveTab.Title = "Renamed inactive";

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.Find(TabSelectorFor(group.Id, inactiveTab.Id)).TextContent,
                        Does.Contain("Renamed inactive"));
                    Assert.That(component.Find(TabSelectorFor(group.Id, inactiveTab.Id))
                        .GetAttribute("aria-selected"), Is.EqualTo("false"));
                    Assert.That(component.Find(CloseSelectorFor(group.Id, inactiveTab.Id))
                        .GetAttribute("aria-label"), Is.EqualTo("Close Renamed inactive"));
                    Assert.That(inactiveTab.Id, Is.EqualTo(inactiveTabId));
                    Assert.That(group.ActiveTab, Is.SameAs(activeTab));
                    Assert.That(renderedItems.All(item => ReferenceEquals(item, activeTab)), Is.True);
                    Assert.That(component.RenderCount, Is.GreaterThan(initialRenderCount));
                }
            });
            var inactiveUpdateRenderCount = component.RenderCount;

            activeTab.Title = "Renamed active";

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.Find(TabSelectorFor(group.Id, activeTab.Id)).TextContent,
                        Does.Contain("Renamed active"));
                    Assert.That(component.Find(TabSelectorFor(group.Id, activeTab.Id))
                        .GetAttribute("aria-selected"), Is.EqualTo("true"));
                    Assert.That(component.Find(TabPanelSelector).TextContent, Does.Contain("Renamed active"));
                    Assert.That(component.Find(TabPanelSelector).GetAttribute("data-tab-id"),
                        Is.EqualTo(activeTab.Id.ToString()));
                    Assert.That(component.Find(CloseSelectorFor(group.Id, activeTab.Id))
                        .GetAttribute("aria-label"), Is.EqualTo("Close Renamed active"));
                    Assert.That(activeTab.Id, Is.EqualTo(activeTabId));
                    Assert.That(group.ActiveTab, Is.SameAs(activeTab));
                    Assert.That(renderedItems.All(item => ReferenceEquals(item, activeTab)), Is.True);
                    Assert.That(component.RenderCount, Is.GreaterThan(inactiveUpdateRenderCount));
                }
            });
        }

        [Test]
        public async Task VerifyTabActivationCloseAndGroupFocusDelegateToViewModel()
        {
            var state = CreateViewModel();
            var group = state.Groups[0];
            var firstTab = OpenTab(state, group, "First", "first-view");
            var activeTab = OpenTab(state, group, "Active", "active-view");
            var viewModel = CreateConsumerMock(state);

            using var component = this.RenderWorkspace(viewModel.Object);

            await component.Find(TabSelectorFor(group.Id, firstTab.Id)).ClickAsync();
            await component.Find(CloseSelectorFor(group.Id, activeTab.Id)).ClickAsync();
            await component.Find(GroupSelectorFor(group.Id)).FocusInAsync(new FocusEventArgs());

            using (Assert.EnterMultipleScope())
            {
                viewModel.Verify(x => x.ActivateTab(group.Id, firstTab.Id), Times.Once);
                viewModel.Verify(x => x.CloseTab(group.Id, activeTab.Id), Times.Once);
                viewModel.Verify(x => x.FocusGroup(group.Id), Times.AtLeastOnce);
            }
        }

        [Test]
        public async Task VerifyAddTabControlRendersPerGroupOutsideTablistsAndRequestsExactGroup()
        {
            var state = CreateViewModel();
            var firstGroup = state.Groups[0];
            var secondGroup = AddGroup(state);
            Assert.That(state.FocusGroup(firstGroup.Id), Is.True);
            var viewModel = CreateConsumerMock(state);
            Guid? requestedGroupId = null;
            EditorGroupViewModel focusedGroupAtRequest = null;
            var callbackEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseCallback = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var callback = EventCallback.Factory.Create<Guid>(
                this,
                async groupId =>
                {
                    requestedGroupId = groupId;
                    focusedGroupAtRequest = state.FocusedGroup;
                    callbackEntered.SetResult(true);
                    await releaseCallback.Task;
                });

            using var component = this.RenderWorkspace(
                viewModel.Object,
                addTabRequested: callback);
            var controls = component.FindAll(AddTabSelector);
            var secondControl = component.Find(AddTabSelectorFor(secondGroup.Id));

            var clickTask = secondControl.ClickAsync();
            await callbackEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
            var callbackWasAwaited = !clickTask.IsCompleted;
            releaseCallback.SetResult(true);
            await clickTask;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(controls, Has.Count.EqualTo(2));
                Assert.That(controls.Select(control => control.Id), Is.Unique);
                Assert.That(controls.Select(control => Guid.Parse(control.GetAttribute("data-group-id"))),
                    Is.EquivalentTo(new[] { firstGroup.Id, secondGroup.Id }));
                Assert.That(controls.All(control => control.Closest(TabListSelector) is null), Is.True);
                Assert.That(component.FindAll(TabListSelector)
                    .All(tabList => tabList.QuerySelector(AddTabSelector) is null), Is.True);
                Assert.That(requestedGroupId, Is.EqualTo(secondGroup.Id));
                Assert.That(state.FocusedGroup, Is.SameAs(secondGroup));
                Assert.That(focusedGroupAtRequest, Is.SameAs(secondGroup));
                Assert.That(callbackWasAwaited, Is.True);
                Assert.That(secondControl.Id, Does.Contain(secondGroup.Id.ToString("N")));
                viewModel.Verify(x => x.FocusGroup(secondGroup.Id), Times.Once);
            }
        }

        [Test]
        public void VerifyAddTabControlsAreOmittedWithoutCallback()
        {
            var state = CreateViewModel();
            AddGroup(state);

            using var component = this.RenderWorkspace(state);

            Assert.That(component.FindAll(AddTabSelector), Is.Empty);
        }

        [Test]
        public void VerifyInactiveDropTargetsDoNotReserveSpaceBeforeAddTabControl()
        {
            var state = CreateViewModel();
            var group = state.Groups[0];
            RenderFragment<Guid> addTabControl = _ => builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddContent(1, "Add");
                builder.CloseElement();
            };

            using var component = this.RenderWorkspace(state, addTabControl: addTabControl);
            var emptyTabList = component.Find(TabListSelector);
            var groupDropSurface = component.Find(GroupDropSurfaceSelectorFor(group.Id));
            var addTabHost = component.Find(AddTabHostSelector);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(emptyTabList.Children, Is.Empty);
                Assert.That(groupDropSurface.ClassList,
                    Does.Not.Contain("mb-editor-workspace__group-drop-surface--available"));
                Assert.That(groupDropSurface.ClassList,
                    Does.Contain("mb-editor-workspace__group-drop-surface--empty"));
                Assert.That(groupDropSurface.GetAttribute("aria-hidden"), Is.EqualTo("true"));
                Assert.That(groupDropSurface.GetAttribute("role"), Is.EqualTo("presentation"));
                Assert.That(groupDropSurface.GetAttribute("tabindex"), Is.Null);
                Assert.That(groupDropSurface.PreviousElementSibling?.GetAttribute("data-testid"),
                    Is.EqualTo("editor-workspace-tablist"));
                Assert.That(addTabHost.PreviousElementSibling?.GetAttribute("data-testid"),
                    Is.EqualTo("editor-workspace-group-drop-surface"));
            }

            var tab = OpenTab(state, group, "Editor", "editor");

            component.WaitForAssertion(() =>
            {
                var populatedTabList = component.Find(TabListSelector);
                var tabItem = component.Find(TabSelectorFor(group.Id, tab.Id)).ParentElement;
                var hitZones = component.FindAll(TabDropHitZoneSelectorFor(group.Id, tab.Id));
                var dropMarkers = component.FindAll(DropMarkerSelector);
                groupDropSurface = component.Find(GroupDropSurfaceSelectorFor(group.Id));

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(populatedTabList.Children, Has.Count.EqualTo(1));
                    Assert.That(populatedTabList.Children[0].GetAttribute("data-tab-id"),
                        Is.EqualTo(tab.Id.ToString()));
                    Assert.That(tabItem.ClassList, Does.Contain("mb-editor-workspace__tab-item"));
                    Assert.That(hitZones, Has.Count.EqualTo(2));
                    Assert.That(hitZones.All(zone => zone.GetAttribute("tabindex") is null), Is.True);
                    Assert.That(hitZones.All(zone => zone.GetAttribute("role") == "presentation"), Is.True);
                    Assert.That(hitZones.All(zone => !zone.ClassList
                        .Contains("mb-editor-workspace__tab-drop-hit-zone--available")), Is.True);
                    Assert.That(dropMarkers, Has.Count.EqualTo(2));
                    Assert.That(dropMarkers.Count(marker => marker.ClassList
                        .Contains("mb-editor-workspace__tab-drop-marker--before")), Is.EqualTo(1));
                    Assert.That(dropMarkers.Count(marker => marker.ClassList
                        .Contains("mb-editor-workspace__tab-drop-marker--end")), Is.EqualTo(1));
                    Assert.That(dropMarkers.All(marker => marker.GetAttribute("aria-hidden") == "true"),
                        Is.True);
                    Assert.That(dropMarkers.All(marker => marker.GetAttribute("role") == "presentation"),
                        Is.True);
                    Assert.That(dropMarkers.All(marker => marker.GetAttribute("tabindex") is null), Is.True);
                    Assert.That(dropMarkers.All(marker => marker.ParentElement == tabItem), Is.True);
                    Assert.That(dropMarkers.All(marker => !marker.ClassList
                        .Contains("mb-editor-workspace__tab-drop-marker--active")), Is.True);
                    Assert.That(component.FindAll(GroupDropSurfaceSelector), Has.Count.EqualTo(1));
                    Assert.That(groupDropSurface.ClassList,
                        Does.Not.Contain("mb-editor-workspace__group-drop-surface--empty"));
                    Assert.That(groupDropSurface.ClassList,
                        Does.Not.Contain("mb-editor-workspace__group-drop-surface--available"));
                    Assert.That(groupDropSurface.GetAttribute("tabindex"), Is.Null);
                    Assert.That(component.Find(AddTabHostSelector).PreviousElementSibling?
                            .GetAttribute("data-testid"),
                        Is.EqualTo("editor-workspace-group-drop-surface"));
                }
            });
        }

        [Test]
        public async Task VerifyTemplatedAddControlRendersPerGroupAndTakesPrecedenceOverFallback()
        {
            var state = CreateViewModel();
            var firstGroup = state.Groups[0];
            var secondGroup = AddGroup(state);
            Guid? selectedGroupId = null;
            var fallbackRequests = 0;
            var fallback = EventCallback.Factory.Create<Guid>(this, _ => fallbackRequests++);
            RenderFragment<Guid> addTabControl = groupId => builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "type", "button");
                builder.AddAttribute(2, "aria-label", $"Add tab to group {groupId}");
                builder.AddAttribute(3, "data-template-add-group-id", groupId);
                builder.AddAttribute(
                    4,
                    "onclick",
                    EventCallback.Factory.Create(this, () => selectedGroupId = groupId));
                builder.AddContent(5, "+");
                builder.CloseElement();
            };

            using var component = this.RenderWorkspace(
                state,
                addTabRequested: fallback,
                addTabControl: addTabControl);
            var hosts = component.FindAll(AddTabHostSelector);
            var secondControl = component.Find($"[data-template-add-group-id='{secondGroup.Id}']");

            await secondControl.ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(hosts, Has.Count.EqualTo(2));
                Assert.That(hosts.Select(host => Guid.Parse(host.GetAttribute("data-group-id"))),
                    Is.EquivalentTo(new[] { firstGroup.Id, secondGroup.Id }));
                Assert.That(hosts.All(host => host.Closest(TabListSelector) is null), Is.True);
                Assert.That(component.FindAll(AddTabSelector), Is.Empty);
                Assert.That(selectedGroupId, Is.EqualTo(secondGroup.Id));
                Assert.That(fallbackRequests, Is.Zero);
            }
        }

        [Test]
        public void VerifyNativeDropTargetsOwnPropagationWithoutKeyboardStops()
        {
            var componentDirectory = Path.Combine(
                TestRepository.GetRootPath(),
                "Mycelium.Bloom",
                "Components",
                "UI",
                "Organisms",
                "EditorWorkspace");
            var razor = File.ReadAllText(Path.Combine(componentDirectory, "EditorWorkspace.razor"));
            var hitZoneTails = razor
                .Split("data-testid=\"editor-workspace-tab-drop-hit-zone\"", StringSplitOptions.None)
                .Skip(1)
                .Select(fragment => fragment[..fragment.IndexOf("</span>", StringComparison.Ordinal)])
                .ToArray();
            var groupSurfaceTail = razor
                .Split("data-testid=\"editor-workspace-group-drop-surface\"", StringSplitOptions.None)[1];
            groupSurfaceTail = groupSurfaceTail[..groupSurfaceTail.IndexOf("</span>", StringComparison.Ordinal)];
            var tabItemOpeningTag = razor
                .Split("<div @key=\"tab.Id\"", StringSplitOptions.None)[1];
            tabItemOpeningTag = tabItemOpeningTag[..tabItemOpeningTag.IndexOf('>')];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(hitZoneTails, Has.Length.EqualTo(2));
                Assert.That(hitZoneTails.All(tail => tail.Contains(
                    "@ondragenter:stopPropagation",
                    StringComparison.Ordinal)), Is.True);
                Assert.That(hitZoneTails.All(tail => tail.Contains(
                    "@ondragover:preventDefault",
                    StringComparison.Ordinal)), Is.True);
                Assert.That(hitZoneTails.All(tail => tail.Contains(
                    "@ondragover:stopPropagation",
                    StringComparison.Ordinal)), Is.True);
                Assert.That(hitZoneTails.All(tail => tail.Contains(
                    "@ondragleave:stopPropagation",
                    StringComparison.Ordinal)), Is.True);
                Assert.That(hitZoneTails.All(tail => !tail.Contains("tabindex", StringComparison.Ordinal)), Is.True);
                Assert.That(groupSurfaceTail, Does.Contain("@ondragleave:stopPropagation"));
                Assert.That(groupSurfaceTail, Does.Contain("@ondrop:preventDefault"));
                Assert.That(groupSurfaceTail, Does.Contain("@ondrop:stopPropagation"));
                Assert.That(groupSurfaceTail, Does.Not.Contain("tabindex"));
                Assert.That(tabItemOpeningTag, Does.Contain("@ondrop:stopPropagation"));
            }
        }

        [Test]
        public async Task VerifyTemplatedAddControlHostsAccessibleActionMenuTrigger()
        {
            var state = CreateViewModel();
            var group = state.Groups[0];
            var actions = new[]
            {
                new ActionMenuItem { Id = "empty", Label = "Empty editor" },
                new ActionMenuItem { Id = "browser", Label = "Project Browser", Disabled = true }
            };
            Guid? selectedGroupId = null;
            ActionMenuItem selectedAction = null;
            RenderFragment<Guid> addTabControl = groupId => builder =>
            {
                builder.OpenComponent<ActionMenuComponent>(0);
                builder.AddAttribute(1, nameof(ActionMenuComponent.Items), actions);
                builder.AddAttribute(
                    2,
                    nameof(ActionMenuComponent.TriggerAriaLabel),
                    "Add tab to Editor group 1");
                builder.AddAttribute(3, nameof(ActionMenuComponent.TriggerTitle), "Add tab");
                builder.AddAttribute(4, nameof(ActionMenuComponent.TriggerClass), "mb-editor-workspace__add-tab");
                builder.AddAttribute(
                    5,
                    nameof(ActionMenuComponent.TriggerContent),
                    (RenderFragment)(triggerBuilder => triggerBuilder.AddContent(0, "+")));
                builder.AddAttribute(
                    6,
                    nameof(ActionMenuComponent.ItemSelected),
                    EventCallback.Factory.Create<ActionMenuItem>(this, item =>
                    {
                        selectedGroupId = groupId;
                        selectedAction = item;
                    }));
                builder.CloseComponent();
            };

            using var component = this.RenderWorkspace(state, addTabControl: addTabControl);
            var actionMenu = component.FindComponent<ActionMenuComponent>();
            var trigger = actionMenu.Find("button");

            await trigger.ClickAsync();
            var menuItems = await this.portalHost.WaitForElementsAsync("[role='menuitem']", 2);
            await menuItems[0].ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(AddTabHostSelector).GetAttribute("data-group-id"),
                    Is.EqualTo(group.Id.ToString()));
                Assert.That(trigger.GetAttribute("aria-label"), Is.EqualTo("Add tab to Editor group 1"));
                Assert.That(trigger.GetAttribute("title"), Is.EqualTo("Add tab"));
                Assert.That(selectedGroupId, Is.EqualTo(group.Id));
                Assert.That(selectedAction, Is.SameAs(actions[0]));
                Assert.That(actions[1].Disabled, Is.True);
            }
        }

        [Test]
        public async Task VerifyFallbackAddRequestDoesNotDirectlyMutateWorkspaceState()
        {
            var state = CreateViewModel();
            var group = state.Groups[0];
            OpenTab(state, group, "Editor", "editor-view");
            AddGroup(state);
            var viewModel = CreateConsumerMock(state);
            var callback = EventCallback.Factory.Create<Guid>(this, (Guid _) => { });

            using var component = this.RenderWorkspace(
                viewModel.Object,
                addTabRequested: callback);

            await component.Find(AddTabSelectorFor(group.Id)).ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll($"{TabSelector}[draggable='true']"), Has.Count.EqualTo(1));
                viewModel.Verify(
                    x => x.TryAddGroup(out It.Ref<EditorGroupViewModel>.IsAny),
                    Times.Never);
                viewModel.Verify(
                    x => x.TryOpenTab(
                        It.IsAny<Guid>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        out It.Ref<EditorTabItem>.IsAny),
                    Times.Never);
                viewModel.Verify(
                    x => x.MoveTab(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>()),
                    Times.Never);
                viewModel.Verify(
                    x => x.MoveTab(
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>(),
                        It.IsAny<Guid?>()),
                    Times.Never);
            }
        }

        [Test]
        public async Task VerifyKeyboardNavigationActivatesTabsWithoutDeleteShortcut()
        {
            var state = CreateViewModel();
            var group = state.Groups[0];
            var firstTab = OpenTab(state, group, "First", "same-view");
            var middleTab = OpenTab(state, group, "Middle", "same-view");
            var lastTab = OpenTab(state, group, "Last", "same-view");
            var viewModel = CreateConsumerMock(state);

            using var component = this.RenderWorkspace(viewModel.Object);
            var middleElement = component.Find(TabSelectorFor(group.Id, middleTab.Id));

            await middleElement.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowLeft" });
            await middleElement.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });
            await middleElement.KeyDownAsync(new KeyboardEventArgs { Key = "Home" });
            await middleElement.KeyDownAsync(new KeyboardEventArgs { Key = "End" });
            await middleElement.KeyDownAsync(new KeyboardEventArgs { Key = "Delete" });

            using (Assert.EnterMultipleScope())
            {
                viewModel.Verify(x => x.ActivateTab(group.Id, firstTab.Id), Times.Exactly(2));
                viewModel.Verify(x => x.ActivateTab(group.Id, lastTab.Id), Times.Exactly(2));
                viewModel.Verify(x => x.CloseTab(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
            }
        }

        [Test]
        public void VerifyGenericTabLeadingContentRendersCanonicalTabWithoutInterpretingViewTypeKey()
        {
            var state = CreateViewModel();
            var group = state.Groups[0];
            var tab = OpenTab(state, group, "Opaque", "custom-composition-view");
            RenderFragment<EditorTabItem> leadingContent = item => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "data-testid", "caller-tab-leading-content");
                builder.AddAttribute(2, "data-caller-tab-id", item.Id);
                builder.AddContent(3, "Caller icon");
                builder.CloseElement();
            };

            using var component = this.RenderWorkspace(
                state,
                tabLeadingContent: leadingContent);
            var renderedLeadingContent = component.Find(
                $"[data-testid='editor-workspace-tab-leading-content'][data-tab-id='{tab.Id}']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(renderedLeadingContent.QuerySelector("[data-testid='caller-tab-leading-content']"),
                    Is.Not.Null);
                Assert.That(renderedLeadingContent.GetAttribute("data-group-id"), Is.EqualTo(group.Id.ToString()));
                Assert.That(component.Find(TabSelectorFor(group.Id, tab.Id)).TextContent,
                    Does.Contain("Caller icon"));
            }
        }

        [Test]
        public async Task VerifySplittersExposeSeparatorSemanticsAndKeyboardResizing()
        {
            var module = this.JSInterop.SetupModule(JavaScriptModulePath);
            var measureHandler = module.Setup<double>("measureAdjacentPairWidth", invocation => true);
            measureHandler.SetResult(960d);
            var state = CreateViewModel();
            AddGroup(state);
            AddGroup(state);
            var viewModel = CreateConsumerMock(state);

            using var component = this.RenderWorkspace(viewModel.Object);
            var splitters = component.FindAll(SplitterSelector);
            var initialStyles = component.FindAll(GroupSelector)
                .Select(group => group.GetAttribute("style"))
                .ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(splitters, Has.Count.EqualTo(2));
                Assert.That(splitters.All(splitter => splitter.GetAttribute("role") == "separator"), Is.True);
                Assert.That(splitters.All(splitter => splitter.GetAttribute("aria-orientation") == "vertical"), Is.True);
                Assert.That(splitters.All(splitter => splitter.GetAttribute("tabindex") == "0"), Is.True);
            }

            await splitters[0].KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });

            var resizedStyles = component.FindAll(GroupSelector)
                .Select(group => group.GetAttribute("style"))
                .ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resizedStyles[0], Is.Not.EqualTo(initialStyles[0]));
                Assert.That(resizedStyles[1], Is.Not.EqualTo(initialStyles[1]));
                Assert.That(resizedStyles[2], Is.EqualTo(initialStyles[2]));
                viewModel.Verify(
                    x => x.FocusGroup(It.IsAny<Guid>()),
                    Times.Never);
            }
        }

        [Test]
        public async Task VerifyNativeDragReordersSameGroupBeforeCanonicalTab()
        {
            var state = CreateViewModel();
            var group = state.Groups[0];
            var firstTab = OpenTab(state, group, "First", "first-view");
            var secondTab = OpenTab(state, group, "Second", "second-view");
            var draggedTab = OpenTab(state, group, "Dragged", "dragged-view");
            var viewModel = CreateConsumerMock(state);

            using var component = this.RenderWorkspace(viewModel.Object);
            var draggedElement = component.Find(TabSelectorFor(group.Id, draggedTab.Id));

            await draggedElement.TriggerEventAsync("ondragstart", new DragEventArgs());
            var beforeTarget = component.Find(TabDropHitZoneSelectorFor(group.Id, secondTab.Id, "left"));
            Assert.That(beforeTarget.ClassList,
                Does.Contain("mb-editor-workspace__tab-drop-hit-zone--available"));
            await beforeTarget.TriggerEventAsync("ondragenter", new DragEventArgs());
            var destinationItem = component.Find(TabSelectorFor(group.Id, secondTab.Id)).ParentElement;
            Assert.That(
                component.Find(ActiveDropMarkerSelector).GetAttribute("data-tab-id"),
                Is.EqualTo(secondTab.Id.ToString()));
            await destinationItem.TriggerEventAsync("ondrop", new DragEventArgs());

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(group.Tabs, Is.EqualTo(new[] { firstTab, draggedTab, secondTab }));
                    Assert.That(group.ActiveTab, Is.SameAs(draggedTab));
                    Assert.That(component.FindAll(ActiveDropMarkerSelector), Is.Empty);
                    Assert.That(
                        component.FindAll(TabSelector)
                            .Select(tab => Guid.Parse(tab.GetAttribute("data-tab-id"))),
                        Is.EqualTo(new[] { firstTab.Id, draggedTab.Id, secondTab.Id }));
                }
            });
            viewModel.Verify(
                x => x.MoveTab(group.Id, draggedTab.Id, group.Id, secondTab.Id),
                Times.Once);
        }

        [Test]
        public async Task VerifyEquivalentSameGroupDropDoesNotInvokeOwnerMutation()
        {
            var state = CreateViewModel();
            var group = state.Groups[0];
            var firstTab = OpenTab(state, group, "First", "first-view");
            var middleTab = OpenTab(state, group, "Middle", "middle-view");
            var finalTab = OpenTab(state, group, "Final", "final-view");
            var viewModel = CreateConsumerMock(state);

            using var component = this.RenderWorkspace(viewModel.Object);
            var draggedElement = component.Find(TabSelectorFor(group.Id, middleTab.Id));

            await draggedElement.TriggerEventAsync("ondragstart", new DragEventArgs());
            await component.Find(TabDropHitZoneSelectorFor(group.Id, firstTab.Id, "left"))
                .TriggerEventAsync("ondragenter", new DragEventArgs());
            var equivalentTarget = component.Find(TabDropHitZoneSelectorFor(group.Id, finalTab.Id, "left"));
            Assert.That(equivalentTarget.ClassList,
                Does.Contain("mb-editor-workspace__tab-drop-hit-zone--available"));
            await equivalentTarget.TriggerEventAsync("ondragenter", new DragEventArgs());
            var finalItem = component.Find(TabSelectorFor(group.Id, finalTab.Id)).ParentElement;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(ActiveDropMarkerSelector), Has.Count.EqualTo(1));
                Assert.That(component.Find(ActiveDropMarkerSelector).GetAttribute("data-tab-id"),
                    Is.EqualTo(finalTab.Id.ToString()));
            }

            await finalItem.TriggerEventAsync("ondrop", new DragEventArgs());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(ActiveDropMarkerSelector), Is.Empty);
                viewModel.Verify(
                    x => x.MoveTab(
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>(),
                        It.IsAny<Guid?>()),
                    Times.Never);
            }
        }

        [Test]
        public async Task VerifySelfDropDoesNotInvokeOwnerMutation()
        {
            var state = CreateViewModel();
            var group = state.Groups[0];
            var draggedTab = OpenTab(state, group, "Dragged", "dragged-view");
            var viewModel = CreateConsumerMock(state);

            using var component = this.RenderWorkspace(viewModel.Object);
            var draggedElement = component.Find(TabSelectorFor(group.Id, draggedTab.Id));

            await draggedElement.TriggerEventAsync("ondragstart", new DragEventArgs());
            var selfTarget = component.Find(TabDropHitZoneSelectorFor(group.Id, draggedTab.Id, "left"));
            Assert.That(selfTarget.ClassList,
                Does.Contain("mb-editor-workspace__tab-drop-hit-zone--available"));
            await selfTarget.TriggerEventAsync("ondragenter", new DragEventArgs());
            var draggedItem = component.Find(TabSelectorFor(group.Id, draggedTab.Id)).ParentElement;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(ActiveDropMarkerSelector), Has.Count.EqualTo(1));
                Assert.That(component.Find(ActiveDropMarkerSelector).GetAttribute("data-tab-id"),
                    Is.EqualTo(draggedTab.Id.ToString()));
            }

            await draggedItem.TriggerEventAsync("ondrop", new DragEventArgs());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(ActiveDropMarkerSelector), Is.Empty);
                Assert.That(group.Tabs.Single(), Is.SameAs(draggedTab));
                viewModel.Verify(
                    x => x.MoveTab(
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>(),
                        It.IsAny<Guid?>()),
                    Times.Never);
            }
        }

        [Test]
        public async Task VerifyNativeDragAppendsWithinSameGroupWithoutChangingActiveTab()
        {
            var state = CreateViewModel();
            var group = state.Groups[0];
            var draggedTab = OpenTab(state, group, "Dragged", "dragged-view");
            var middleTab = OpenTab(state, group, "Middle", "middle-view");
            var activeTab = OpenTab(state, group, "Active", "active-view");
            var viewModel = CreateConsumerMock(state);

            using var component = this.RenderWorkspace(viewModel.Object);
            var draggedElement = component.Find(TabSelectorFor(group.Id, draggedTab.Id));

            await draggedElement.TriggerEventAsync("ondragstart", new DragEventArgs());
            var endTarget = component.Find(TabDropHitZoneSelectorFor(group.Id, activeTab.Id, "right"));
            await endTarget.TriggerEventAsync("ondragenter", new DragEventArgs());
            var activeItem = component.Find(TabSelectorFor(group.Id, activeTab.Id)).ParentElement;

            using (Assert.EnterMultipleScope())
            {
                var activeMarker = component.Find(ActiveDropMarkerSelector);
                Assert.That(activeMarker.GetAttribute("data-tab-id"), Is.EqualTo(activeTab.Id.ToString()));
                Assert.That(activeMarker.ClassList,
                    Does.Contain("mb-editor-workspace__tab-drop-marker--end"));
            }

            await activeItem.TriggerEventAsync("ondrop", new DragEventArgs());

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(group.Tabs, Is.EqualTo(new[] { middleTab, activeTab, draggedTab }));
                    Assert.That(group.ActiveTab, Is.SameAs(activeTab));
                    Assert.That(component.Find(TabSelectorFor(group.Id, activeTab.Id))
                        .GetAttribute("aria-selected"), Is.EqualTo("true"));
                    Assert.That(component.FindAll(ActiveDropMarkerSelector), Is.Empty);
                }
            });
            viewModel.Verify(
                x => x.MoveTab(group.Id, draggedTab.Id, group.Id, null),
                Times.Once);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public async Task VerifyNativeDragMovesTabAcrossGroupsBeforeEveryDestinationAnchor(int destinationIndex)
        {
            var state = CreateViewModel();
            var sourceGroup = state.Groups[0];
            var sourceFirst = OpenTab(state, sourceGroup, "Source first", "source-first");
            var movedTab = OpenTab(state, sourceGroup, "Moved", "moved");
            var destinationGroup = AddGroup(state);
            var destinationFirst = OpenTab(state, destinationGroup, "Destination first", "destination-first");
            var destinationMiddle = OpenTab(state, destinationGroup, "Destination middle", "destination-middle");
            var destinationLast = OpenTab(state, destinationGroup, "Destination last", "destination-last");
            var destinationTabs = new[] { destinationFirst, destinationMiddle, destinationLast };
            var destinationAnchor = destinationTabs[destinationIndex];
            var viewModel = CreateConsumerMock(state);

            using var component = this.RenderWorkspace(viewModel.Object);
            var draggedElement = component.Find(TabSelectorFor(sourceGroup.Id, movedTab.Id));

            await draggedElement.TriggerEventAsync("ondragstart", new DragEventArgs());
            var beforeTarget = component.Find(
                TabDropHitZoneSelectorFor(destinationGroup.Id, destinationAnchor.Id, "left"));
            Assert.That(beforeTarget.ClassList,
                Does.Contain("mb-editor-workspace__tab-drop-hit-zone--available"));
            await beforeTarget.TriggerEventAsync("ondragenter", new DragEventArgs());
            var destinationItem = component.Find(
                TabSelectorFor(destinationGroup.Id, destinationAnchor.Id)).ParentElement;
            Assert.That(
                component.Find(ActiveDropMarkerSelector).GetAttribute("data-tab-id"),
                Is.EqualTo(destinationAnchor.Id.ToString()));
            await destinationItem.TriggerEventAsync("ondrop", new DragEventArgs());

            await component.WaitForAssertionAsync(() =>
            {
                var movedElement = component.Find(TabSelectorFor(destinationGroup.Id, movedTab.Id));
                var movedPanel = component.Find($"#{movedElement.GetAttribute("aria-controls")}");

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(sourceGroup.Tabs, Is.EqualTo(new[] { sourceFirst }));
                    Assert.That(
                        destinationGroup.Tabs,
                        Is.EqualTo(destinationTabs.Take(destinationIndex)
                            .Append(movedTab)
                            .Concat(destinationTabs.Skip(destinationIndex))));
                    Assert.That(destinationGroup.ActiveTab, Is.SameAs(movedTab));
                    Assert.That(state.FocusedGroup, Is.SameAs(destinationGroup));
                    Assert.That(movedElement.GetAttribute("aria-selected"), Is.EqualTo("true"));
                    Assert.That(movedElement.GetAttribute("tabindex"), Is.EqualTo("0"));
                    Assert.That(movedPanel.GetAttribute("role"), Is.EqualTo("tabpanel"));
                    Assert.That(movedPanel.GetAttribute("aria-labelledby"), Is.EqualTo(movedElement.Id));
                    Assert.That(movedPanel.GetAttribute("data-tab-id"), Is.EqualTo(movedTab.Id.ToString()));
                    Assert.That(component.Find(TabSelectorFor(sourceGroup.Id, sourceFirst.Id))
                        .GetAttribute("aria-selected"), Is.EqualTo("true"));
                }
            });
            viewModel.Verify(
                x => x.MoveTab(
                    sourceGroup.Id,
                    movedTab.Id,
                    destinationGroup.Id,
                    destinationAnchor.Id),
                Times.Once);
        }

        [Test]
        public async Task VerifyMidpointHitZonesConvergeOnOneCanonicalCandidate()
        {
            var state = CreateViewModel();
            var sourceGroup = state.Groups[0];
            var retainedTab = OpenTab(state, sourceGroup, "Retained", "retained");
            var movedTab = OpenTab(state, sourceGroup, "Moved", "moved");
            var destinationGroup = AddGroup(state);
            var firstTab = OpenTab(state, destinationGroup, "First", "first");
            var middleTab = OpenTab(state, destinationGroup, "Middle", "middle");
            var finalTab = OpenTab(state, destinationGroup, "Final", "final");
            var viewModel = CreateConsumerMock(state);

            using var component = this.RenderWorkspace(viewModel.Object);
            await component.Find(TabSelectorFor(sourceGroup.Id, movedTab.Id))
                .TriggerEventAsync("ondragstart", new DragEventArgs());

            var rightOfFirst = component.Find(
                TabDropHitZoneSelectorFor(destinationGroup.Id, firstTab.Id, "right"));
            var leftOfMiddle = component.Find(
                TabDropHitZoneSelectorFor(destinationGroup.Id, middleTab.Id, "left"));
            var draggedTabZones = component.FindAll(
                TabDropHitZoneSelectorFor(sourceGroup.Id, movedTab.Id));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(leftOfMiddle.ClassList,
                    Does.Contain("mb-editor-workspace__tab-drop-hit-zone--left"));
                Assert.That(rightOfFirst.ClassList,
                    Does.Contain("mb-editor-workspace__tab-drop-hit-zone--right"));
                Assert.That(draggedTabZones, Has.Count.EqualTo(2));
                Assert.That(draggedTabZones.All(zone => zone.ClassList
                    .Contains("mb-editor-workspace__tab-drop-hit-zone--available")), Is.True);
            }

            await rightOfFirst.TriggerEventAsync("ondragenter", new DragEventArgs());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(ActiveDropMarkerSelector), Has.Count.EqualTo(1));
                var activeMarker = component.Find(ActiveDropMarkerSelector);
                Assert.That(activeMarker.GetAttribute("data-tab-id"), Is.EqualTo(middleTab.Id.ToString()));
                Assert.That(activeMarker.ClassList,
                    Does.Contain("mb-editor-workspace__tab-drop-marker--before"));
            }

            component.Render(parameters => parameters.Add(workspace => workspace.ViewModel, viewModel.Object));
            Assert.That(component.Find(ActiveDropMarkerSelector).GetAttribute("data-tab-id"),
                Is.EqualTo(middleTab.Id.ToString()));
            await component.Find(TabDropHitZoneSelectorFor(destinationGroup.Id, middleTab.Id, "left"))
                .TriggerEventAsync("ondragenter", new DragEventArgs());
            var middleItem = component.Find(TabSelectorFor(destinationGroup.Id, middleTab.Id)).ParentElement;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(ActiveDropMarkerSelector), Has.Count.EqualTo(1));
                Assert.That(component.Find(ActiveDropMarkerSelector).GetAttribute("data-tab-id"),
                    Is.EqualTo(middleTab.Id.ToString()));
            }

            await middleItem.TriggerEventAsync("ondrop", new DragEventArgs());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sourceGroup.Tabs, Is.EqualTo(new[] { retainedTab }));
                Assert.That(destinationGroup.Tabs,
                    Is.EqualTo(new[] { firstTab, movedTab, middleTab, finalTab }));
                Assert.That(component.FindAll(ActiveDropMarkerSelector), Is.Empty);
            }

            viewModel.Verify(
                x => x.MoveTab(sourceGroup.Id, movedTab.Id, destinationGroup.Id, middleTab.Id),
                Times.Once);
        }

        [Test]
        public async Task VerifyGroupSurfaceReplacesTabCandidateAndCommitsOnce()
        {
            var state = CreateViewModel();
            var sourceGroup = state.Groups[0];
            var retainedTab = OpenTab(state, sourceGroup, "Retained", "retained");
            var movedTab = OpenTab(state, sourceGroup, "Moved", "moved");
            var destinationGroup = AddGroup(state);
            var destinationTab = OpenTab(state, destinationGroup, "Destination", "destination");
            var viewModel = CreateConsumerMock(state);

            using var component = this.RenderWorkspace(viewModel.Object);
            await component.Find(TabSelectorFor(sourceGroup.Id, movedTab.Id))
                .TriggerEventAsync("ondragstart", new DragEventArgs());

            var beforeZone = component.Find(
                TabDropHitZoneSelectorFor(destinationGroup.Id, destinationTab.Id, "left"));
            await beforeZone.TriggerEventAsync("ondragenter", new DragEventArgs());

            Assert.That(component.FindAll(ActiveDropMarkerSelector), Has.Count.EqualTo(1));

            var groupDropSurface = component.Find(GroupDropSurfaceSelectorFor(destinationGroup.Id));
            await groupDropSurface.TriggerEventAsync("ondragenter", new DragEventArgs());
            groupDropSurface = component.Find(GroupDropSurfaceSelectorFor(destinationGroup.Id));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(ActiveDropMarkerSelector), Has.Count.EqualTo(1));
                Assert.That(component.Find(ActiveDropMarkerSelector).ClassList,
                    Does.Contain("mb-editor-workspace__tab-drop-marker--end"));
            }

            await groupDropSurface.TriggerEventAsync("ondrop", new DragEventArgs());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sourceGroup.Tabs, Is.EqualTo(new[] { retainedTab }));
                Assert.That(destinationGroup.Tabs, Is.EqualTo(new[] { destinationTab, movedTab }));
                Assert.That(component.FindAll(ActiveDropMarkerSelector), Is.Empty);
                viewModel.Verify(
                    x => x.MoveTab(sourceGroup.Id, movedTab.Id, destinationGroup.Id, null),
                    Times.Once);
            }
        }

        [Test]
        public async Task VerifyNativeDragAppendsAtEndOfDestinationGroup()
        {
            var state = CreateViewModel();
            var sourceGroup = state.Groups[0];
            var retainedTab = OpenTab(state, sourceGroup, "Retained", "retained");
            var movedTab = OpenTab(state, sourceGroup, "Moved", "moved");
            var destinationGroup = AddGroup(state);
            var destinationTab = OpenTab(state, destinationGroup, "Destination", "destination");
            var viewModel = CreateConsumerMock(state);

            using var component = this.RenderWorkspace(viewModel.Object);
            var draggedElement = component.Find(TabSelectorFor(sourceGroup.Id, movedTab.Id));

            await draggedElement.TriggerEventAsync("ondragstart", new DragEventArgs());
            var groupDropSurface = component.Find(GroupDropSurfaceSelectorFor(destinationGroup.Id));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(groupDropSurface.ClassList,
                    Does.Contain("mb-editor-workspace__group-drop-surface--available"));
                Assert.That(groupDropSurface.GetAttribute("tabindex"), Is.Null);
            }

            await groupDropSurface.TriggerEventAsync("ondragenter", new DragEventArgs());
            groupDropSurface = component.Find(GroupDropSurfaceSelectorFor(destinationGroup.Id));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(groupDropSurface.ClassList,
                    Does.Contain("mb-editor-workspace__group-drop-surface--active"));
                var activeMarker = component.Find(ActiveDropMarkerSelector);
                Assert.That(activeMarker.GetAttribute("data-tab-id"), Is.EqualTo(destinationTab.Id.ToString()));
                Assert.That(activeMarker.ClassList,
                    Does.Contain("mb-editor-workspace__tab-drop-marker--end"));
            }

            await groupDropSurface.TriggerEventAsync("ondrop", new DragEventArgs());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sourceGroup.Tabs, Is.EqualTo(new[] { retainedTab }));
                Assert.That(destinationGroup.Tabs, Is.EqualTo(new[] { destinationTab, movedTab }));
                Assert.That(component.FindAll(ActiveDropMarkerSelector), Is.Empty);
            }

            viewModel.Verify(
                x => x.MoveTab(sourceGroup.Id, movedTab.Id, destinationGroup.Id, null),
                Times.Once);
        }

        [Test]
        public async Task VerifyNativeDragMovesOnlyTabIntoEmptyGroupAndReconcilesSource()
        {
            var state = CreateViewModel();
            var sourceGroup = state.Groups[0];
            var movedTab = OpenTab(state, sourceGroup, "Only tab", "only-tab");
            var destinationGroup = AddGroup(state);

            using var component = this.RenderWorkspace(state);
            var draggedElement = component.Find(TabSelectorFor(sourceGroup.Id, movedTab.Id));

            await draggedElement.TriggerEventAsync("ondragstart", new DragEventArgs());
            var groupDropSurface = component.Find(GroupDropSurfaceSelectorFor(destinationGroup.Id));
            Assert.That(groupDropSurface.ClassList,
                Does.Contain("mb-editor-workspace__group-drop-surface--available"));
            await groupDropSurface.TriggerEventAsync("ondragenter", new DragEventArgs());
            groupDropSurface = component.Find(GroupDropSurfaceSelectorFor(destinationGroup.Id));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(groupDropSurface.ClassList,
                    Does.Contain("mb-editor-workspace__group-drop-surface--empty"));
                Assert.That(groupDropSurface.ClassList,
                    Does.Contain("mb-editor-workspace__group-drop-surface--active"));
                Assert.That(groupDropSurface.GetAttribute("tabindex"), Is.Null);
            }

            await groupDropSurface.TriggerEventAsync("ondrop", new DragEventArgs());

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(state.Groups, Has.Count.EqualTo(1));
                    Assert.That(state.Groups[0], Is.SameAs(destinationGroup));
                    Assert.That(destinationGroup.Tabs, Is.EqualTo(new[] { movedTab }));
                    Assert.That(destinationGroup.ActiveTab, Is.SameAs(movedTab));
                    Assert.That(component.FindAll(GroupSelector), Has.Count.EqualTo(1));
                    Assert.That(component.Find(TabSelector).GetAttribute("data-tab-id"),
                        Is.EqualTo(movedTab.Id.ToString()));
                }
            });
        }

        [Test]
        public async Task VerifyNativeDragStateClearsOnDragEndAndTopologyChange()
        {
            var state = CreateViewModel();
            var group = state.Groups[0];
            var firstTab = OpenTab(state, group, "First", "first");
            var middleTab = OpenTab(state, group, "Middle", "middle");
            var draggedTab = OpenTab(state, group, "Dragged", "dragged");

            using var component = this.RenderWorkspace(state);
            var draggedElement = component.Find(TabSelectorFor(group.Id, draggedTab.Id));

            await draggedElement.TriggerEventAsync("ondragstart", new DragEventArgs());
            var target = component.Find(TabDropHitZoneSelectorFor(group.Id, firstTab.Id, "left"));
            await target.TriggerEventAsync("ondragenter", new DragEventArgs());
            var middleTarget = component.Find(TabDropHitZoneSelectorFor(group.Id, middleTab.Id, "left"));
            await middleTarget.TriggerEventAsync("ondragenter", new DragEventArgs());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(ActiveDropMarkerSelector), Has.Count.EqualTo(1));
                Assert.That(component.Find(ActiveDropMarkerSelector)
                    .GetAttribute("data-tab-id"), Is.EqualTo(middleTab.Id.ToString()));
            }

            await component.Find(TabListSelector).ParentElement
                .TriggerEventAsync("ondragleave", new DragEventArgs());
            Assert.That(component.FindAll(ActiveDropMarkerSelector), Is.Empty);

            middleTarget = component.Find(TabDropHitZoneSelectorFor(group.Id, middleTab.Id, "left"));
            await middleTarget.TriggerEventAsync("ondragenter", new DragEventArgs());
            draggedElement = component.Find(TabSelectorFor(group.Id, draggedTab.Id));
            await draggedElement.TriggerEventAsync("ondragend", new DragEventArgs());
            Assert.That(component.FindAll(ActiveDropMarkerSelector), Is.Empty);

            draggedElement = component.Find(TabSelectorFor(group.Id, draggedTab.Id));
            await draggedElement.TriggerEventAsync("ondragstart", new DragEventArgs());
            target = component.Find(TabDropHitZoneSelectorFor(group.Id, firstTab.Id, "left"));
            await target.TriggerEventAsync("ondragenter", new DragEventArgs());
            Assert.That(state.CloseTab(group.Id, firstTab.Id), Is.True);

            await component.WaitForAssertionAsync(() =>
                Assert.That(component.FindAll(ActiveDropMarkerSelector), Is.Empty));
        }

        [Test]
        public async Task VerifyViewModelReplacementClearsNativeDragPresentation()
        {
            var state = CreateViewModel();
            var group = state.Groups[0];
            var firstTab = OpenTab(state, group, "First", "first");
            var draggedTab = OpenTab(state, group, "Dragged", "dragged");

            using var component = this.RenderWorkspace(state);
            await component.Find(TabSelectorFor(group.Id, draggedTab.Id))
                .TriggerEventAsync("ondragstart", new DragEventArgs());
            await component.Find(TabDropHitZoneSelectorFor(group.Id, firstTab.Id, "left"))
                .TriggerEventAsync("ondragenter", new DragEventArgs());
            Assert.That(component.FindAll(ActiveDropMarkerSelector), Has.Count.EqualTo(1));

            var replacementState = CreateViewModel();
            var replacementGroup = replacementState.Groups[0];
            var replacementTab = OpenTab(replacementState, replacementGroup, "Replacement", "replacement");
            component.Render(parameters => parameters.Add(workspace => workspace.ViewModel, replacementState));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(ActiveDropMarkerSelector), Is.Empty);
                Assert.That(component.Find(TabSelector).GetAttribute("data-tab-id"),
                    Is.EqualTo(replacementTab.Id.ToString()));
            }
        }

        [Test]
        public async Task VerifyInternalSplitActionInvokesExactLeftGroupWithoutStartingResize()
        {
            var module = this.JSInterop.SetupModule(JavaScriptModulePath);
            var captureHandler = module.Setup<double[]>("capturePointer", invocation => true);
            captureHandler.SetResult([300d, 320d, 620d]);
            var state = CreateViewModel();
            var leftGroup = state.Groups[0];
            var rightGroup = AddGroup(state);
            var viewModel = CreateConsumerMock(state);

            using var component = this.RenderWorkspace(viewModel.Object);
            var splitHost = component.Find(SplitAddSelector);
            var splitButton = splitHost.QuerySelector("button");

            await splitButton.ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(splitHost.GetAttribute("data-left-group-id"), Is.EqualTo(leftGroup.Id.ToString()));
                Assert.That(splitButton.GetAttribute("aria-label"), Is.EqualTo("Split editor here"));
                Assert.That(splitButton.GetAttribute("title"), Is.EqualTo("Split editor here"));
                Assert.That(splitButton.Closest(SplitterSelector), Is.Null);
                Assert.That(state.Groups, Has.Count.EqualTo(3));
                Assert.That(state.Groups[0], Is.SameAs(leftGroup));
                Assert.That(state.Groups[2], Is.SameAs(rightGroup));
                Assert.That(captureHandler.Invocations, Is.Empty);
                viewModel.Verify(
                    x => x.TrySplitGroup(leftGroup.Id, out It.Ref<EditorGroupViewModel>.IsAny),
                    Times.Once);
            }
        }

        [Test]
        public async Task VerifyInternalSplitHalvesLeftWeightWithoutDisturbingUnrelatedGroups()
        {
            var state = CreateViewModel();
            var leftGroup = state.Groups[0];
            var rightGroup = AddGroup(state);
            var finalGroup = AddGroup(state);

            using var component = this.Render<EditorWorkspaceComponent>(parameters => parameters
                .Add(workspace => workspace.ViewModel, state)
                .Add(workspace => workspace.EditorContent, CreateContent())
                .Add(workspace => workspace.InitialGroupWeights, new Dictionary<Guid, double>
                {
                    [leftGroup.Id] = 0.2d,
                    [rightGroup.Id] = 0.3d,
                    [finalGroup.Id] = 0.5d
                }));
            var splitButton = component.Find(
                    $"{SplitAddSelector}[data-left-group-id='{leftGroup.Id}']")
                .QuerySelector("button");

            await splitButton.ClickAsync();
            var splitGroup = state.Groups[1];

            await component.WaitForAssertionAsync(() =>
            {
                var weights = GetRenderedWeights(component);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(state.Groups, Is.EqualTo(new[] { leftGroup, splitGroup, rightGroup, finalGroup }));
                    Assert.That(weights[leftGroup.Id], Is.EqualTo(0.1d).Within(1e-9));
                    Assert.That(weights[splitGroup.Id], Is.EqualTo(0.1d).Within(1e-9));
                    Assert.That(weights[rightGroup.Id], Is.EqualTo(0.3d).Within(1e-9));
                    Assert.That(weights[finalGroup.Id], Is.EqualTo(0.5d).Within(1e-9));
                    Assert.That(weights.Values.Sum(), Is.EqualTo(1d).Within(1e-9));
                }
            });
        }

        [Test]
        public async Task VerifyRightEdgeSplitActionCreatesSecondGroupFromSingleGroup()
        {
            var state = CreateViewModel();
            var finalGroup = state.Groups[0];

            using var component = this.RenderWorkspace(state);
            var splitHost = component.Find(RightEdgeSplitAddSelector);
            var splitButton = splitHost.QuerySelector("button");
            var requestedLeftGroupId = splitHost.GetAttribute("data-left-group-id");

            await splitButton.ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(requestedLeftGroupId, Is.EqualTo(finalGroup.Id.ToString()));
                Assert.That(state.Groups, Has.Count.EqualTo(2));
                Assert.That(state.Groups[0], Is.SameAs(finalGroup));
                Assert.That(state.Groups[1].Tabs, Is.Empty);
                Assert.That(state.FocusedGroup, Is.SameAs(state.Groups[1]));
                Assert.That(component.FindAll(GroupSelector), Has.Count.EqualTo(2));
            }
        }

        [Test]
        public void VerifySplitActionsAreOmittedAtMaximumGroupCount()
        {
            var state = CreateViewModel(2);
            AddGroup(state);

            using var component = this.RenderWorkspace(state);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(SplitAddSelector), Is.Empty);
                Assert.That(component.FindAll(RightEdgeSplitAddSelector), Is.Empty);
                Assert.That(component.FindAll(SplitterSelector), Has.Count.EqualTo(1));
            }
        }

        [TestCase(1d, 99d, "1", "0", "100")]
        [TestCase(99d, 1d, "99", "0", "100")]
        public void VerifyExtremeInitialWeightsExposeReachableSeparatorAriaRange(
            double leftSeed,
            double rightSeed,
            string expectedValue,
            string expectedMinimum,
            string expectedMaximum)
        {
            var state = CreateViewModel();
            var leftGroup = state.Groups[0];
            var rightGroup = AddGroup(state);

            using var component = this.Render<EditorWorkspaceComponent>(parameters => parameters
                .Add(workspace => workspace.ViewModel, state)
                .Add(workspace => workspace.EditorContent, CreateContent())
                .Add(workspace => workspace.InitialGroupWeights, new Dictionary<Guid, double>
                {
                    [leftGroup.Id] = leftSeed,
                    [rightGroup.Id] = rightSeed
                }));
            var splitter = component.Find(SplitterSelector);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(splitter.GetAttribute("aria-valuenow"), Is.EqualTo(expectedValue));
                Assert.That(splitter.GetAttribute("aria-valuemin"), Is.EqualTo(expectedMinimum));
                Assert.That(splitter.GetAttribute("aria-valuemax"), Is.EqualTo(expectedMaximum));
            }
        }

        [Test]
        public void VerifyDefaultGroupWeightsAreEqual()
        {
            var state = CreateViewModel();
            AddGroup(state);
            AddGroup(state);

            using var component = this.RenderWorkspace(state);
            var weights = GetRenderedWeights(component);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(weights, Has.Count.EqualTo(3));
                Assert.That(weights.Values.Sum(), Is.EqualTo(1d).Within(1e-9));
                Assert.That(weights.Values.All(weight => Math.Abs(weight - (1d / 3d)) < 1e-9), Is.True);
            }
        }

        [Test]
        public void VerifyInitialWeightsNormalizeMatchingPositiveFiniteSeeds()
        {
            var state = CreateViewModel();
            var firstGroup = state.Groups[0];
            var secondGroup = AddGroup(state);
            var thirdGroup = AddGroup(state);
            var seeds = new Dictionary<Guid, double>
            {
                [firstGroup.Id] = 300d,
                [secondGroup.Id] = 320d,
                [thirdGroup.Id] = 868d,
                [Guid.NewGuid()] = 10_000d
            };

            using var component = this.Render<EditorWorkspaceComponent>(parameters => parameters
                .Add(workspace => workspace.ViewModel, state)
                .Add(workspace => workspace.EditorContent, CreateContent())
                .Add(workspace => workspace.InitialGroupWeights, seeds));
            var weights = GetRenderedWeights(component);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(weights[firstGroup.Id], Is.EqualTo(300d / 1488d).Within(1e-9));
                Assert.That(weights[secondGroup.Id], Is.EqualTo(320d / 1488d).Within(1e-9));
                Assert.That(weights[thirdGroup.Id], Is.EqualTo(868d / 1488d).Within(1e-9));
                Assert.That(weights.Values.Sum(), Is.EqualTo(1d).Within(1e-9));
            }
        }

        [Test]
        public void VerifyInvalidAndMissingInitialWeightsUseNeutralDefaults()
        {
            var state = CreateViewModel();
            var firstGroup = state.Groups[0];
            var secondGroup = AddGroup(state);
            var thirdGroup = AddGroup(state);
            var seeds = new Dictionary<Guid, double>
            {
                [firstGroup.Id] = double.NaN,
                [secondGroup.Id] = -1d,
                [thirdGroup.Id] = 2d
            };

            using var component = this.Render<EditorWorkspaceComponent>(parameters => parameters
                .Add(workspace => workspace.ViewModel, state)
                .Add(workspace => workspace.EditorContent, CreateContent())
                .Add(workspace => workspace.InitialGroupWeights, seeds));
            var weights = GetRenderedWeights(component);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(weights[firstGroup.Id], Is.EqualTo(0.25d).Within(1e-9));
                Assert.That(weights[secondGroup.Id], Is.EqualTo(0.25d).Within(1e-9));
                Assert.That(weights[thirdGroup.Id], Is.EqualTo(0.5d).Within(1e-9));
            }
        }

        [Test]
        public async Task VerifyInitialWeightsAreNotReappliedOnOrdinaryParameterUpdates()
        {
            var module = this.JSInterop.SetupModule(JavaScriptModulePath);
            var measureHandler = module.Setup<double>("measureAdjacentPairWidth", invocation => true);
            measureHandler.SetResult(1200d);
            var state = CreateViewModel();
            var firstGroup = state.Groups[0];
            var secondGroup = AddGroup(state);
            var initialSeeds = new Dictionary<Guid, double>
            {
                [firstGroup.Id] = 0.3d,
                [secondGroup.Id] = 0.7d
            };

            using var component = this.Render<EditorWorkspaceComponent>(parameters => parameters
                .Add(workspace => workspace.ViewModel, state)
                .Add(workspace => workspace.EditorContent, CreateContent())
                .Add(workspace => workspace.InitialGroupWeights, initialSeeds));

            await component.Find(SplitterSelector)
                .KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });
            var resizedWeights = GetRenderedWeights(component);

            component.Render(parameters => parameters
                .Add(workspace => workspace.AriaLabel, "Updated workspace")
                .Add(workspace => workspace.InitialGroupWeights, new Dictionary<Guid, double>
                {
                    [firstGroup.Id] = 0.9d,
                    [secondGroup.Id] = 0.1d
                }));
            var retainedWeights = GetRenderedWeights(component);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resizedWeights[firstGroup.Id], Is.EqualTo(0.35d).Within(1e-9));
                Assert.That(resizedWeights[secondGroup.Id], Is.EqualTo(0.65d).Within(1e-9));
                Assert.That(retainedWeights[firstGroup.Id], Is.EqualTo(resizedWeights[firstGroup.Id]).Within(1e-9));
                Assert.That(retainedWeights[secondGroup.Id], Is.EqualTo(resizedWeights[secondGroup.Id]).Within(1e-9));
            }
        }

        [Test]
        public async Task VerifyAddedAndRemovedGroupsReconcileWeightsWithoutLosingRetainedRatios()
        {
            var state = CreateViewModel();
            var firstGroup = state.Groups[0];
            OpenTab(state, firstGroup, "First", "first-view");
            var removedGroup = AddGroup(state);
            var removedTab = OpenTab(state, removedGroup, "Removed", "removed-view");
            var seeds = new Dictionary<Guid, double>
            {
                [firstGroup.Id] = 0.3d,
                [removedGroup.Id] = 0.7d
            };

            using var component = this.Render<EditorWorkspaceComponent>(parameters => parameters
                .Add(workspace => workspace.ViewModel, state)
                .Add(workspace => workspace.EditorContent, CreateContent())
                .Add(workspace => workspace.InitialGroupWeights, seeds));
            var addedGroup = AddGroup(state);

            await component.WaitForAssertionAsync(() =>
            {
                var addedWeights = GetRenderedWeights(component);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(addedWeights[firstGroup.Id], Is.EqualTo(0.3d).Within(1e-9));
                    Assert.That(addedWeights[removedGroup.Id], Is.EqualTo(0.35d).Within(1e-9));
                    Assert.That(addedWeights[addedGroup.Id], Is.EqualTo(0.35d).Within(1e-9));
                }
            });

            Assert.That(state.CloseTab(removedGroup.Id, removedTab.Id), Is.True);

            await component.WaitForAssertionAsync(() =>
            {
                var removedWeights = GetRenderedWeights(component);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(removedWeights.Keys, Is.EquivalentTo(new[] { firstGroup.Id, addedGroup.Id }));
                    Assert.That(removedWeights[firstGroup.Id], Is.EqualTo(6d / 13d).Within(1e-9));
                    Assert.That(removedWeights[addedGroup.Id], Is.EqualTo(7d / 13d).Within(1e-9));
                }
            });
        }

        [Test]
        public async Task VerifyViewModelReplacementReinitializesWeightsFromReplacementSeeds()
        {
            var module = this.JSInterop.SetupModule(JavaScriptModulePath);
            var measureHandler = module.Setup<double>("measureAdjacentPairWidth", invocation => true);
            measureHandler.SetResult(1200d);
            var firstState = CreateViewModel();
            var firstGroup = firstState.Groups[0];
            var firstSecondGroup = AddGroup(firstState);

            using var component = this.Render<EditorWorkspaceComponent>(parameters => parameters
                .Add(workspace => workspace.ViewModel, firstState)
                .Add(workspace => workspace.EditorContent, CreateContent())
                .Add(workspace => workspace.InitialGroupWeights, new Dictionary<Guid, double>
                {
                    [firstGroup.Id] = 0.3d,
                    [firstSecondGroup.Id] = 0.7d
                }));
            await component.Find(SplitterSelector)
                .KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });

            var replacementState = CreateViewModel();
            var replacementFirstGroup = replacementState.Groups[0];
            var replacementSecondGroup = AddGroup(replacementState);
            var replacementThirdGroup = AddGroup(replacementState);
            component.Render(parameters => parameters
                .Add(workspace => workspace.ViewModel, replacementState)
                .Add(workspace => workspace.InitialGroupWeights, new Dictionary<Guid, double>
                {
                    [replacementFirstGroup.Id] = 300d,
                    [replacementSecondGroup.Id] = 320d,
                    [replacementThirdGroup.Id] = 868d
                }));
            var weights = GetRenderedWeights(component);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(weights[replacementFirstGroup.Id], Is.EqualTo(300d / 1488d).Within(1e-9));
                Assert.That(weights[replacementSecondGroup.Id], Is.EqualTo(320d / 1488d).Within(1e-9));
                Assert.That(weights[replacementThirdGroup.Id], Is.EqualTo(868d / 1488d).Within(1e-9));
            }
        }

        [Test]
        public async Task VerifyCompactSwitcherChangesPresentationWithoutChangingActiveTabs()
        {
            var state = CreateViewModel();
            var firstGroup = state.Groups[0];
            var firstActiveTab = OpenTab(state, firstGroup, "Browser", "browser-view");
            var secondGroup = AddGroup(state);
            var secondActiveTab = OpenTab(state, secondGroup, "Diagram", "diagram-view");
            var viewModel = CreateConsumerMock(state);

            using var component = this.RenderWorkspace(viewModel.Object);
            var switcher = component.Find("[data-testid='editor-workspace-compact-switcher']");
            var buttons = switcher.QuerySelectorAll("button");

            Assert.That(buttons[1].GetAttribute("aria-pressed"), Is.EqualTo("true"));
            await buttons[0].ClickAsync();
            buttons = component.Find("[data-testid='editor-workspace-compact-switcher']")
                .QuerySelectorAll("button");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(buttons[0].GetAttribute("aria-pressed"), Is.EqualTo("true"));
                Assert.That(buttons[1].GetAttribute("aria-pressed"), Is.EqualTo("false"));
                Assert.That(component.Find(GroupSelectorFor(firstGroup.Id))
                    .GetAttribute("data-compact-active"), Is.EqualTo("true"));
                Assert.That(component.Find(GroupSelectorFor(secondGroup.Id))
                    .GetAttribute("data-compact-active"), Is.EqualTo("false"));
                Assert.That(firstGroup.ActiveTab, Is.SameAs(firstActiveTab));
                Assert.That(secondGroup.ActiveTab, Is.SameAs(secondActiveTab));
                viewModel.Verify(x => x.FocusGroup(firstGroup.Id), Times.Once);
                viewModel.Verify(
                    x => x.ActivateTab(It.IsAny<Guid>(), It.IsAny<Guid>()),
                    Times.Never);
            }
        }

        [Test]
        public void VerifyCompactSwitcherUsesUniqueAccessibleLabelsForDuplicateTitles()
        {
            var state = CreateViewModel();
            OpenTab(state, state.Groups[0], "Shared", "first-view");
            var secondGroup = AddGroup(state);
            OpenTab(state, secondGroup, "Shared", "second-view");

            using var component = this.RenderWorkspace(state);
            var buttons = component.Find("[data-testid='editor-workspace-compact-switcher']")
                .QuerySelectorAll("button");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(buttons.Select(button => button.TextContent.Trim()),
                    Is.EqualTo(ExpectedDuplicateCompactSwitcherTitles));
                Assert.That(buttons.Select(button => button.GetAttribute("aria-label")),
                    Is.EqualTo(ExpectedDuplicateCompactSwitcherAriaLabels));
                Assert.That(buttons.Select(button => button.GetAttribute("aria-label")), Is.Unique);
            }
        }

        [Test]
        public void VerifyCompactSwitcherIsOmittedForOneGroup()
        {
            var state = CreateViewModel();
            OpenTab(state, state.Groups[0], "Only editor", "only-view");

            using var component = this.RenderWorkspace(state);

            Assert.That(component.FindAll("[data-testid='editor-workspace-compact-switcher']"), Is.Empty);
        }

        [Test]
        public void VerifyRootClassAttributesAndLabelsApplyWithoutBottomPanel()
        {
            var state = CreateViewModel();
            var tab = OpenTab(state, state.Groups[0], "Editor", "editor-view");
            AddGroup(state);
            var viewModel = CreateConsumerMock(state);

            using var component = this.Render<EditorWorkspaceComponent>(parameters => parameters
                .Add(workspace => workspace.ViewModel, viewModel.Object)
                .Add(workspace => workspace.EditorContent, CreateContent())
                .Add(workspace => workspace.AriaLabel, "Mission editors")
                .Add(workspace => workspace.GroupAriaLabel, "Mission editor")
                .Add(workspace => workspace.CompactGroupSwitcherAriaLabel, "Visible editor")
                .Add(workspace => workspace.Class, "custom-workspace")
                .AddUnmatched("data-owner", "test-suite"));
            var root = component.Find(WorkspaceSelector);
            var group = component.Find(GroupSelector);
            var compactSwitcher = component.Find("[data-testid='editor-workspace-compact-switcher']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root.ClassList, Does.Contain("custom-workspace"));
                Assert.That(root.GetAttribute("data-owner"), Is.EqualTo("test-suite"));
                Assert.That(root.GetAttribute("aria-label"), Is.EqualTo("Mission editors"));
                Assert.That(group.GetAttribute("aria-label"), Does.Contain("Mission editor"));
                Assert.That(compactSwitcher.GetAttribute("aria-label"), Is.EqualTo("Visible editor"));
                Assert.That(component.FindAll(".mb-editor-workspace__bottom-panel"), Is.Empty);
                Assert.That(component.FindAll($"[data-rendered-tab-id='{tab.Id}']"), Has.Count.EqualTo(1));
            }
        }

        [Test]
        public void VerifyDomIdentifiersAreStableAndIndependentPerInstance()
        {
            var state = CreateViewModel();
            OpenTab(state, state.Groups[0], "Shared", "shared-view");
            var viewModel = CreateConsumerMock(state);

            using var first = this.RenderWorkspace(viewModel.Object);
            var firstTabId = first.Find(TabSelector).Id;
            var firstPanelId = first.Find(TabPanelSelector).Id;

            first.Render(parameters => parameters.Add(workspace => workspace.AriaLabel, "Updated label"));

            using var second = this.RenderWorkspace(viewModel.Object);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.Find(TabSelector).Id, Is.EqualTo(firstTabId));
                Assert.That(first.Find(TabPanelSelector).Id, Is.EqualTo(firstPanelId));
                Assert.That(second.Find(TabSelector).Id, Is.Not.EqualTo(firstTabId));
                Assert.That(second.Find(TabPanelSelector).Id, Is.Not.EqualTo(firstPanelId));
            }
        }

        [Test]
        public async Task VerifyClosingActiveTabRequestsFocusForTheReconciledActiveTab()
        {
            var module = this.JSInterop.SetupModule(JavaScriptModulePath);
            var focusHandler = module.Setup<bool>("focusElementById", invocation => true);
            focusHandler.SetResult(true);
            var state = CreateViewModel();
            var group = state.Groups[0];
            var retainedTab = OpenTab(state, group, "Retained", "retained-view");
            var closedTab = OpenTab(state, group, "Close", "closed-view");

            using var component = this.RenderWorkspace(state);
            await component.Find(CloseSelectorFor(group.Id, closedTab.Id)).ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                var retainedTabElement = component.Find(TabSelectorFor(group.Id, retainedTab.Id));

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(focusHandler.Invocations, Has.Count.EqualTo(1));
                    Assert.That(focusHandler.Invocations["focusElementById"][0].Arguments[0],
                        Is.EqualTo(retainedTabElement.Id));
                }
            });

            await component.Instance.DisposeAsync();
        }

        [Test]
        public async Task VerifyClosingFinalTabRequestsFocusForTheGroupAddTabControl()
        {
            var module = this.JSInterop.SetupModule(JavaScriptModulePath);
            var focusHandler = module.Setup<bool>("focusElementById", invocation => true);
            focusHandler.SetResult(true);
            var state = CreateViewModel();
            var group = state.Groups[0];
            var finalTab = OpenTab(state, group, "Final", "final-view");
            var callback = EventCallback.Factory.Create<Guid>(this, (Guid _) => { });

            using var component = this.RenderWorkspace(
                state,
                addTabRequested: callback);
            var addTabControlId = component.Find(AddTabSelectorFor(group.Id)).Id;
            await component.Find(CloseSelectorFor(group.Id, finalTab.Id)).ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(group.Tabs, Is.Empty);
                    Assert.That(component.Find(AddTabSelectorFor(group.Id)).Id, Is.EqualTo(addTabControlId));
                    Assert.That(focusHandler.Invocations, Has.Count.EqualTo(1));
                    Assert.That(focusHandler.Invocations["focusElementById"][0].Arguments[0],
                        Is.EqualTo(addTabControlId));
                }
            });
        }

        [Test]
        public async Task VerifyClosingFinalTabFallsBackToGroupRegionFocusWithoutAddTabCallback()
        {
            var module = this.JSInterop.SetupModule(JavaScriptModulePath);
            var focusHandler = module.Setup<bool>("focusElementById", invocation => true);
            focusHandler.SetResult(true);
            var state = CreateViewModel();
            var group = state.Groups[0];
            var finalTab = OpenTab(state, group, "Final", "final-view");

            using var component = this.RenderWorkspace(state);
            var groupElement = component.Find(GroupSelectorFor(group.Id));
            var groupElementId = groupElement.Id;
            await component.Find(CloseSelectorFor(group.Id, finalTab.Id)).ClickAsync();

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(group.Tabs, Is.Empty);
                    Assert.That(component.FindAll(AddTabSelector), Is.Empty);
                    Assert.That(component.Find(GroupSelectorFor(group.Id)).Id, Is.EqualTo(groupElementId));
                    Assert.That(focusHandler.Invocations, Has.Count.EqualTo(1));
                    Assert.That(focusHandler.Invocations["focusElementById"][0].Arguments[0],
                        Is.EqualTo(groupElementId));
                }
            });
        }

        [Test]
        public async Task VerifyRemovedAndReplacedViewModelTabsDetachWhileReplacementUpdates()
        {
            var firstState = CreateViewModel();
            var firstGroup = firstState.Groups[0];
            var retainedTab = OpenTab(firstState, firstGroup, "Retained", "retained-view");
            var removedTab = OpenTab(firstState, firstGroup, "Removed", "removed-view");

            using var component = this.RenderWorkspace(firstState);
            Assert.That(firstState.CloseTab(firstGroup.Id, removedTab.Id), Is.True);

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.FindAll(TabSelectorFor(firstGroup.Id, removedTab.Id)), Is.Empty);
                    Assert.That(component.Find($"[data-rendered-tab-id='{retainedTab.Id}']"), Is.Not.Null);
                    Assert.That(firstGroup.ActiveTab, Is.SameAs(retainedTab));
                }
            });
            var renderCountAfterRemoval = component.RenderCount;

            removedTab.Title = "Detached removed tab";
            Assert.That(component.RenderCount, Is.EqualTo(renderCountAfterRemoval));

            var replacementState = CreateViewModel();
            var replacementGroup = replacementState.Groups[0];
            var replacementTab = OpenTab(
                replacementState,
                replacementGroup,
                "Replacement",
                "replacement-view");
            component.Render(parameters => parameters.Add(workspace => workspace.ViewModel, replacementState));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(GroupSelector), Has.Count.EqualTo(1));
                Assert.That(component.Find($"[data-rendered-tab-id='{replacementTab.Id}']"), Is.Not.Null);
            }

            var replacementRenderCount = component.RenderCount;
            retainedTab.Title = "Detached old ViewModel tab";
            Assert.That(component.RenderCount, Is.EqualTo(replacementRenderCount));

            replacementTab.Title = "Updated replacement";

            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.Find(TabSelectorFor(replacementGroup.Id, replacementTab.Id)).TextContent,
                        Does.Contain("Updated replacement"));
                    Assert.That(component.Find($"[data-rendered-tab-id='{replacementTab.Id}']").TextContent,
                        Does.Contain("Updated replacement"));
                    Assert.That(component.RenderCount, Is.GreaterThan(replacementRenderCount));
                }
            });
        }

        [Test]
        public async Task VerifyDisposalDetachesSubscriptionsWithoutDisposingCallerViewModel()
        {
            var state = CreateViewModel();
            var group = state.Groups[0];
            var tab = OpenTab(state, group, "Initial", "initial-view");
            var viewModel = CreateConsumerMock(state);
            var disposableViewModel = viewModel.As<IDisposable>();
            var asyncDisposableViewModel = viewModel.As<IAsyncDisposable>();

            var component = this.RenderWorkspace(viewModel.Object);
            await component.Instance.DisposeAsync();
            var renderCountAfterDisposal = component.RenderCount;

            tab.Title = "After disposal";
            OpenTab(state, group, "After disposal", "after-disposal-view");
            AddGroup(state);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.RenderCount, Is.EqualTo(renderCountAfterDisposal));
                viewModel.VerifyAdd(
                    x => x.PropertyChanged += It.IsAny<PropertyChangedEventHandler>(),
                    Times.Once);
                viewModel.VerifyRemove(
                    x => x.PropertyChanged -= It.IsAny<PropertyChangedEventHandler>(),
                    Times.Once);
                disposableViewModel.Verify(x => x.Dispose(), Times.Never);
                asyncDisposableViewModel.Verify(x => x.DisposeAsync(), Times.Never);
            }
        }

        [Test]
        public async Task VerifyPointerResizeUsesMeasuredBaselineAndChangesOnlyAdjacentWeights()
        {
            var module = this.JSInterop.SetupModule(JavaScriptModulePath);
            var captureHandler = module.Setup<double[]>("capturePointer", invocation => true);
            var releaseHandler = module.SetupVoid("releasePointer", invocation => true);
            captureHandler.SetResult([300d, 320d, 620d]);
            releaseHandler.SetVoidResult();
            var state = CreateViewModel();
            var firstGroup = state.Groups[0];
            var secondGroup = AddGroup(state);
            var thirdGroup = AddGroup(state);

            using var component = this.RenderWorkspace(state);
            var splitter = component.Find(SplitterSelector);
            var initialWeights = GetRenderedWeights(component);

            await splitter.PointerDownAsync(new PointerEventArgs
            {
                Button = 0,
                ClientX = 480d,
                PointerId = 27
            });
            await component.Find(SplitterSelector).PointerMoveAsync(new PointerEventArgs
            {
                ClientX = 542d,
                PointerId = 27
            });
            var resizedWeights = GetRenderedWeights(component);
            await component.Find(SplitterSelector).PointerUpAsync(new PointerEventArgs { PointerId = 27 });
            var resizedPairWeight = resizedWeights[firstGroup.Id] + resizedWeights[secondGroup.Id];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(captureHandler.Invocations, Has.Count.EqualTo(1));
                Assert.That(captureHandler.Invocations["capturePointer"][0].Arguments[0], Is.EqualTo(splitter.Id));
                Assert.That(captureHandler.Invocations["capturePointer"][0].Arguments[1], Is.EqualTo(27L));
                Assert.That(resizedWeights[firstGroup.Id], Is.GreaterThan(initialWeights[firstGroup.Id]));
                Assert.That(resizedWeights[secondGroup.Id], Is.LessThan(initialWeights[secondGroup.Id]));
                Assert.That(resizedWeights[thirdGroup.Id], Is.EqualTo(initialWeights[thirdGroup.Id]).Within(1e-9));
                Assert.That(resizedPairWeight,
                    Is.EqualTo(initialWeights[firstGroup.Id] + initialWeights[secondGroup.Id]).Within(1e-9));
                Assert.That(resizedWeights[firstGroup.Id] / resizedPairWeight,
                    Is.EqualTo(362d / 620d).Within(1e-9));
                Assert.That(releaseHandler.Invocations, Has.Count.EqualTo(1));
                Assert.That(releaseHandler.Invocations["releasePointer"][0].Arguments[0], Is.EqualTo(splitter.Id));
                Assert.That(releaseHandler.Invocations["releasePointer"][0].Arguments[1], Is.EqualTo(27L));
            }
        }

        [Test]
        public async Task VerifyPointerResizeUsesPracticalPixelMinimumInsteadOfFixedTenPercentShare()
        {
            var module = this.JSInterop.SetupModule(JavaScriptModulePath);
            var captureHandler = module.Setup<double[]>("capturePointer", invocation => true);
            var releaseHandler = module.SetupVoid("releasePointer", invocation => true);
            captureHandler.SetResult([1200d, 1200d, 2400d]);
            releaseHandler.SetVoidResult();
            var state = CreateViewModel();
            var leftGroup = state.Groups[0];
            var rightGroup = AddGroup(state);

            using var component = this.RenderWorkspace(state);
            var splitter = component.Find(SplitterSelector);

            await splitter.PointerDownAsync(new PointerEventArgs
            {
                Button = 0,
                ClientX = 1000d,
                PointerId = 61
            });
            await splitter.PointerMoveAsync(new PointerEventArgs
            {
                ClientX = 16d,
                PointerId = 61
            });
            var belowPreviousShare = GetRenderedWeights(component);

            await splitter.PointerMoveAsync(new PointerEventArgs
            {
                ClientX = -1000d,
                PointerId = 61
            });
            var clampedMinimumWeights = GetRenderedWeights(component);

            await splitter.PointerMoveAsync(new PointerEventArgs
            {
                ClientX = 3000d,
                PointerId = 61
            });
            var clampedMaximumWeights = GetRenderedWeights(component);
            await splitter.PointerUpAsync(new PointerEventArgs { PointerId = 61 });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(belowPreviousShare[leftGroup.Id], Is.EqualTo(0.09d).Within(1e-9));
                Assert.That(clampedMinimumWeights[leftGroup.Id], Is.EqualTo(192d / 2400d).Within(1e-9));
                Assert.That(clampedMinimumWeights[rightGroup.Id], Is.EqualTo(2208d / 2400d).Within(1e-9));
                Assert.That(clampedMaximumWeights[leftGroup.Id], Is.EqualTo(2208d / 2400d).Within(1e-9));
                Assert.That(clampedMaximumWeights[rightGroup.Id], Is.EqualTo(192d / 2400d).Within(1e-9));
                Assert.That(captureHandler.Invocations, Has.Count.EqualTo(1));
                Assert.That(releaseHandler.Invocations, Has.Count.EqualTo(1));
            }
        }

        [Test]
        public async Task VerifyPointerAndKeyboardResizeUseSafeMinimumForImpossibleNarrowPairs()
        {
            var module = this.JSInterop.SetupModule(JavaScriptModulePath);
            var captureHandler = module.Setup<double[]>("capturePointer", invocation => true);
            var measureHandler = module.Setup<double>("measureAdjacentPairWidth", invocation => true);
            var releaseHandler = module.SetupVoid("releasePointer", invocation => true);
            captureHandler.SetResult([150d, 150d, 300d]);
            measureHandler.SetResult(300d);
            releaseHandler.SetVoidResult();
            var state = CreateViewModel();
            var leftGroup = state.Groups[0];
            var rightGroup = AddGroup(state);

            using var component = this.RenderWorkspace(state);
            var splitter = component.Find(SplitterSelector);

            await splitter.PointerDownAsync(new PointerEventArgs
            {
                Button = 0,
                ClientX = 500d,
                PointerId = 67
            });
            await splitter.PointerMoveAsync(new PointerEventArgs
            {
                ClientX = 1000d,
                PointerId = 67
            });
            await splitter.PointerUpAsync(new PointerEventArgs { PointerId = 67 });
            var pointerWeights = GetRenderedWeights(component);

            await splitter.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowLeft" });
            var keyboardWeights = GetRenderedWeights(component);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(pointerWeights[leftGroup.Id], Is.EqualTo(0.5d).Within(1e-9));
                Assert.That(pointerWeights[rightGroup.Id], Is.EqualTo(0.5d).Within(1e-9));
                Assert.That(keyboardWeights[leftGroup.Id], Is.EqualTo(0.5d).Within(1e-9));
                Assert.That(keyboardWeights[rightGroup.Id], Is.EqualTo(0.5d).Within(1e-9));
                Assert.That(measureHandler.Invocations, Has.Count.EqualTo(1));
            }
        }

        [Test]
        public async Task VerifyKeyboardResizeUsesTheSamePracticalPixelMinimumAsPointerResize()
        {
            var module = this.JSInterop.SetupModule(JavaScriptModulePath);
            var measureHandler = module.Setup<double>("measureAdjacentPairWidth", invocation => true);
            measureHandler.SetResult(2400d);
            var state = CreateViewModel();
            var leftGroup = state.Groups[0];
            var rightGroup = AddGroup(state);

            using var component = this.RenderWorkspace(state);
            var splitter = component.Find(SplitterSelector);

            for (var keyPress = 0; keyPress < 10; keyPress++)
            {
                await splitter.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowLeft" });
            }

            var weights = GetRenderedWeights(component);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(weights[leftGroup.Id], Is.EqualTo(192d / 2400d).Within(1e-9));
                Assert.That(weights[rightGroup.Id], Is.EqualTo(2208d / 2400d).Within(1e-9));
                Assert.That(measureHandler.Invocations, Has.Count.EqualTo(10));
            }
        }

        [Test]
        public async Task VerifyPointerResizeRejectsConcurrentCaptureAndCancelsWhenGroupsChange()
        {
            var module = this.JSInterop.SetupModule(JavaScriptModulePath);
            var captureHandler = module.Setup<double[]>("capturePointer", invocation => true);
            var releaseHandler = module.SetupVoid("releasePointer", invocation => true);
            captureHandler.SetResult([300d, 320d, 620d]);
            releaseHandler.SetVoidResult();
            var state = CreateViewModel();
            AddGroup(state);
            AddGroup(state);

            using var component = this.RenderWorkspace(state);
            await component.FindAll(SplitterSelector)[0].PointerDownAsync(new PointerEventArgs
            {
                Button = 0,
                ClientX = 480d,
                PointerId = 41
            });
            await component.FindAll(SplitterSelector)[1].PointerDownAsync(new PointerEventArgs
            {
                Button = 0,
                ClientX = 800d,
                PointerId = 43
            });

            AddGroup(state);

            await component.WaitForAssertionAsync(() =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(releaseHandler.Invocations, Has.Count.EqualTo(1));
                    Assert.That(component.FindAll(GroupSelector), Has.Count.EqualTo(4));
                });
            });
            var weightsAfterAddition = GetRenderedWeights(component);
            await component.FindAll(SplitterSelector)[0].PointerMoveAsync(new PointerEventArgs
            {
                ClientX = 542d,
                PointerId = 41
            });
            var weightsAfterStaleMove = GetRenderedWeights(component);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(captureHandler.Invocations, Has.Count.EqualTo(1));
                Assert.That(releaseHandler.Invocations["releasePointer"][0].Arguments[1], Is.EqualTo(41L));
                Assert.That(weightsAfterStaleMove, Is.EqualTo(weightsAfterAddition));
                Assert.That(weightsAfterStaleMove.Values.Sum(), Is.EqualTo(1d).Within(1e-9));
            }
        }

        [Test]
        public async Task VerifyGroupChangesMutatePresentationOnlyOnRenderDispatcher()
        {
            var module = this.JSInterop.SetupModule(JavaScriptModulePath);
            var captureHandler = module.Setup<double[]>("capturePointer", invocation => true);
            var releaseHandler = module.SetupVoid("releasePointer", invocation => true);
            captureHandler.SetResult([300d, 320d, 620d]);
            releaseHandler.SetVoidResult();
            var state = CreateViewModel();
            AddGroup(state);

            using var component = this.RenderWorkspace(state);
            await component.Find(SplitterSelector).PointerDownAsync(new PointerEventArgs
            {
                Button = 0,
                ClientX = 480d,
                PointerId = 47
            });
            var renderCount = component.RenderCount;
            using var dispatcherEntered = new ManualResetEventSlim();
            using var releaseDispatcher = new ManualResetEventSlim();
            var dispatcherBlock = Task.Run(() => this.Renderer.Dispatcher.InvokeAsync(() =>
            {
                dispatcherEntered.Set();
                releaseDispatcher.Wait();
            }));

            try
            {
                Assert.That(dispatcherEntered.Wait(TimeSpan.FromSeconds(5)), Is.True);
                await Task.Run(() => AddGroup(state)).WaitAsync(TimeSpan.FromSeconds(5));

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(state.Groups, Has.Count.EqualTo(3));
                    Assert.That(component.RenderCount, Is.EqualTo(renderCount));
                    Assert.That(component.FindAll(GroupSelector), Has.Count.EqualTo(2));
                    Assert.That(releaseHandler.Invocations, Is.Empty);
                }
            }
            finally
            {
                releaseDispatcher.Set();
            }

            await dispatcherBlock;
            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.FindAll(GroupSelector), Has.Count.EqualTo(3));
                    Assert.That(releaseHandler.Invocations, Has.Count.EqualTo(1));
                }
            });
        }

        [Test]
        public async Task VerifyDisposalAwaitsNotificationTriggeredPointerReleaseBeforeJavaScriptCleanup()
        {
            var module = this.JSInterop.SetupModule(JavaScriptModulePath);
            var registerHandler = module.Setup<bool>("registerKeydownGuards", invocation => true);
            var unregisterHandler = module.SetupVoid("unregisterKeydownGuards", invocation => true);
            var captureHandler = module.Setup<double[]>("capturePointer", invocation => true);
            var releaseHandler = module.SetupVoid("releasePointer", invocation => true);
            registerHandler.SetResult(true);
            unregisterHandler.SetVoidResult();
            captureHandler.SetResult([300d, 320d, 620d]);
            var state = CreateViewModel();
            AddGroup(state);

            var component = this.RenderWorkspace(state);
            await component.Find(SplitterSelector).PointerDownAsync(new PointerEventArgs
            {
                Button = 0,
                ClientX = 480d,
                PointerId = 53
            });

            Task disposal = null;

            try
            {
                await Task.Run(() => AddGroup(state));
                await component.WaitForAssertionAsync(() =>
                    Assert.That(releaseHandler.Invocations, Has.Count.EqualTo(1)));

                disposal = component.Instance.DisposeAsync().AsTask();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(disposal.IsCompleted, Is.False);
                    Assert.That(unregisterHandler.Invocations, Is.Empty);
                }
            }
            finally
            {
                releaseHandler.SetVoidResult();
            }

            await disposal;

            Assert.That(unregisterHandler.Invocations, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task VerifyViewModelReplacementReleasesActivePointerCapture()
        {
            var module = this.JSInterop.SetupModule(JavaScriptModulePath);
            var captureHandler = module.Setup<double[]>("capturePointer", invocation => true);
            var releaseHandler = module.SetupVoid("releasePointer", invocation => true);
            captureHandler.SetResult([300d, 320d, 620d]);
            releaseHandler.SetVoidResult();
            var state = CreateViewModel();
            AddGroup(state);

            using var component = this.RenderWorkspace(state);
            var splitter = component.Find(SplitterSelector);
            var splitterId = splitter.Id;
            await splitter.PointerDownAsync(new PointerEventArgs
            {
                Button = 0,
                ClientX = 480d,
                PointerId = 31
            });

            var replacementState = CreateViewModel();
            AddGroup(replacementState);
            component.Render(parameters => parameters.Add(workspace => workspace.ViewModel, replacementState));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(releaseHandler.Invocations, Has.Count.EqualTo(1));
                Assert.That(releaseHandler.Invocations["releasePointer"][0].Arguments[0], Is.EqualTo(splitterId));
                Assert.That(releaseHandler.Invocations["releasePointer"][0].Arguments[1], Is.EqualTo(31L));
            }
        }

        [Test]
        public async Task VerifyDisposalReleasesActivePointerAndUnregistersRootKeyboardGuard()
        {
            var module = this.JSInterop.SetupModule(JavaScriptModulePath);
            var registerHandler = module.Setup<bool>("registerKeydownGuards", invocation => true);
            var unregisterHandler = module.SetupVoid("unregisterKeydownGuards", invocation => true);
            var captureHandler = module.Setup<double[]>("capturePointer", invocation => true);
            var releaseHandler = module.SetupVoid("releasePointer", invocation => true);
            registerHandler.SetResult(true);
            unregisterHandler.SetVoidResult();
            captureHandler.SetResult([300d, 320d, 620d]);
            releaseHandler.SetVoidResult();
            var state = CreateViewModel();
            AddGroup(state);

            var component = this.RenderWorkspace(state);
            var workspaceId = component.Find(WorkspaceSelector).Id;
            var splitter = component.Find(SplitterSelector);
            await splitter.PointerDownAsync(new PointerEventArgs
            {
                Button = 0,
                ClientX = 480d,
                PointerId = 37
            });
            await component.Instance.DisposeAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(registerHandler.Invocations, Has.Count.EqualTo(1));
                Assert.That(registerHandler.Invocations["registerKeydownGuards"][0].Arguments[0],
                    Is.EqualTo(workspaceId));
                Assert.That(unregisterHandler.Invocations, Has.Count.EqualTo(1));
                Assert.That(unregisterHandler.Invocations["unregisterKeydownGuards"][0].Arguments[0],
                    Is.EqualTo(workspaceId));
                Assert.That(releaseHandler.Invocations, Has.Count.EqualTo(1));
                Assert.That(releaseHandler.Invocations["releasePointer"][0].Arguments[0], Is.EqualTo(splitter.Id));
                Assert.That(releaseHandler.Invocations["releasePointer"][0].Arguments[1], Is.EqualTo(37L));
            }
        }

        private static Mock<IWorkspaceEditorViewModel> CreateConsumerMock(WorkspaceEditorViewModel state)
        {
            var viewModel = new Mock<IWorkspaceEditorViewModel>();
            viewModel.SetupGet(x => x.MaximumGroupCount).Returns(state.MaximumGroupCount);
            viewModel.SetupGet(x => x.Groups).Returns(state.Groups);
            viewModel.SetupGet(x => x.FocusedGroup).Returns(() => state.FocusedGroup);
            viewModel.SetupGet(x => x.RenderState).Returns(() => state.RenderState);
            viewModel.Setup(x => x.ActivateTab(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .Returns((Guid groupId, Guid tabId) => state.ActivateTab(groupId, tabId));
            viewModel.Setup(x => x.FocusGroup(It.IsAny<Guid>()))
                .Returns((Guid groupId) => state.FocusGroup(groupId));
            viewModel.Setup(x => x.CloseTab(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .Returns((Guid groupId, Guid tabId) => state.CloseTab(groupId, tabId));
            viewModel.Setup(x => x.TrySplitGroup(
                    It.IsAny<Guid>(),
                    out It.Ref<EditorGroupViewModel>.IsAny))
                .Returns((Guid groupId, out EditorGroupViewModel group) =>
                    state.TrySplitGroup(groupId, out group));
            viewModel.Setup(x => x.MoveTab(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid?>()))
                .Returns((Guid sourceGroupId, Guid tabId, Guid destinationGroupId, Guid? beforeTabId) =>
                    state.MoveTab(sourceGroupId, tabId, destinationGroupId, beforeTabId));
            state.PropertyChanged += (_, args) =>
                viewModel.Raise(x => x.PropertyChanged += null, args);

            return viewModel;
        }

        private static WorkspaceEditorViewModel CreateViewModel(int maximumGroupCount = 5)
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

        private static RenderFragment<EditorTabItem> CreateContent()
        {
            return tab => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "data-rendered-tab-id", tab.Id);
                builder.AddContent(2, tab.Title);
                builder.CloseElement();
            };
        }

        private static Dictionary<Guid, double> GetRenderedWeights(
            IRenderedComponent<EditorWorkspaceComponent> component)
        {
            return component.FindAll(GroupSelector).ToDictionary(
                group => Guid.Parse(group.GetAttribute("data-group-id")),
                group =>
                {
                    const string propertyName = "--mb-editor-group-weight:";
                    var style = group.GetAttribute("style");
                    var propertyIndex = style.IndexOf(propertyName, StringComparison.Ordinal);
                    Assert.That(propertyIndex, Is.GreaterThanOrEqualTo(0));
                    var valueStart = propertyIndex + propertyName.Length;
                    var valueEnd = style.IndexOf(';', valueStart);
                    Assert.That(valueEnd, Is.GreaterThan(valueStart));

                    return double.Parse(
                        style.AsSpan(valueStart, valueEnd - valueStart),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture);
                });
        }

        private IRenderedComponent<EditorWorkspaceComponent> RenderWorkspace(
            IWorkspaceEditorViewModel viewModel,
            RenderFragment<EditorTabItem> editorContent = null,
            EventCallback<Guid> addTabRequested = default,
            RenderFragment<Guid> addTabControl = null,
            RenderFragment<EditorTabItem> tabLeadingContent = null)
        {
            return this.Render<EditorWorkspaceComponent>(parameters => parameters
                .Add(workspace => workspace.ViewModel, viewModel)
                .Add(workspace => workspace.EditorContent, editorContent ?? CreateContent())
                .Add(workspace => workspace.AddTabRequested, addTabRequested)
                .Add(workspace => workspace.AddTabControl, addTabControl)
                .Add(workspace => workspace.TabLeadingContent, tabLeadingContent));
        }

        private static string GroupSelectorFor(Guid groupId)
        {
            return $"{GroupSelector}[data-group-id='{groupId}']";
        }

        private static string TabSelectorFor(Guid groupId, Guid tabId)
        {
            return $"{TabSelector}[data-group-id='{groupId}'][data-tab-id='{tabId}']";
        }

        private static string CloseSelectorFor(Guid groupId, Guid tabId)
        {
            return $"{CloseSelector}[data-group-id='{groupId}'][data-tab-id='{tabId}']";
        }

        private static string AddTabSelectorFor(Guid groupId)
        {
            return $"{AddTabSelector}[data-group-id='{groupId}']";
        }

        private static string TabDropHitZoneSelectorFor(Guid groupId, Guid tabId, string side)
        {
            return $"{DropHitZoneSelector}[data-group-id='{groupId}'][data-tab-id='{tabId}']" +
                   $".mb-editor-workspace__tab-drop-hit-zone--{side}";
        }

        private static string TabDropHitZoneSelectorFor(Guid groupId, Guid tabId)
        {
            return $"{DropHitZoneSelector}[data-group-id='{groupId}'][data-tab-id='{tabId}']";
        }

        private static string GroupDropSurfaceSelectorFor(Guid groupId)
        {
            return $"{GroupDropSurfaceSelector}[data-group-id='{groupId}']";
        }
    }
}
