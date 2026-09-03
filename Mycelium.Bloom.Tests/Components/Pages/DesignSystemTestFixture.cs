// ------------------------------------------------------------------------------------------------
// <copyright file="DesignSystemTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.Pages
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    using BlazorBlueprint.Components;
    using BlazorBlueprint.Primitives.Services;

    using Bunit;

    using Microsoft.AspNetCore.Components;
    using Microsoft.Extensions.DependencyInjection;

    using Moq;

    using Mycelium.Bloom.Components.Pages;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.NavigationRail;
    using Mycelium.Bloom.ViewModel.WorkspaceEditor;

    using EditorWorkspaceComponent = Mycelium.Bloom.Components.UI.Organisms.EditorWorkspace.EditorWorkspace;

    /// <summary>
    /// Tests the Bloom developer component showcase.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class DesignSystemTestFixture : BunitContext
    {
        private static readonly string[] ExpectedSectionOrder =
        [
            "foundation",
            "atoms",
            "molecules",
            "organisms",
            "workspace"
        ];

        private static readonly double[] ExpectedCanonicalGroupWeights =
        [
            300d,
            320d,
            868d
        ];

        private static readonly string[] NamedControlNames =
        [
            "Add model element",
            "Edit selection",
            "Remove selection",
            "Unavailable action",
            "Undo latest edit",
            "Share workspace",
            "Open compact header action",
            "Select element",
            "Add note",
            "Move canvas",
            "Center selection",
            "Open status details",
            "Select shell element",
            "Connect shell elements",
            "Zoom out",
            "Zoom in",
            "Reset zoom",
            "Fit to view"
        ];

        private readonly IRenderedComponent<BbPortalHost> portalHost;
        private readonly BunitJSModuleInterop themeModule;
        private readonly JSRuntimeInvocationHandler applyDarkModeHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="DesignSystemTestFixture" /> class.
        /// </summary>
        public DesignSystemTestFixture()
        {
            this.portalHost = BlueprintTestSetup.ConfigureWithPortalHost(this);

            var searchModule = this.JSInterop.SetupModule("./Components/UI/Atoms/SearchInput/SearchInput.razor.js");
            searchModule.SetupVoid("registerSearchShortcut", invocation => true).SetVoidResult();
            searchModule.SetupVoid("disposeSearchShortcut", invocation => true).SetVoidResult();

            var selectModule = this.JSInterop.SetupModule("./Components/UI/Atoms/SelectInput/SelectInput.razor.js");
            selectModule.SetupVoid("registerSelectCompatibility", invocation => true).SetVoidResult();
            selectModule.SetupVoid("disposeSelectCompatibility", invocation => true).SetVoidResult();

            var editorWorkspaceModule = this.JSInterop.SetupModule(
                "./Components/UI/Organisms/EditorWorkspace/EditorWorkspace.razor.js");
            editorWorkspaceModule.SetupVoid("releasePointer", invocation => true).SetVoidResult();
            editorWorkspaceModule.Setup<bool>("focusElementById", invocation => true).SetResult(true);
            editorWorkspaceModule.Setup<bool>("registerKeydownGuards", invocation => true).SetResult(true);
            editorWorkspaceModule.SetupVoid("unregisterKeydownGuards", invocation => true).SetVoidResult();

            this.themeModule = this.JSInterop.SetupModule(
                "./_content/BlazorBlueprint.Components/js/theme.js");
            this.themeModule.SetupVoid("applyTheme", invocation => true).SetVoidResult();
            this.applyDarkModeHandler = this.themeModule.SetupVoid("applyDarkMode", invocation => true);
            this.themeModule.SetupVoid("saveTheme", invocation => true).SetVoidResult();
            this.applyDarkModeHandler.SetVoidResult();
        }

        /// <summary>
        /// Disposes the bUnit test context after each test.
        /// </summary>
        [TearDown]
        public System.Threading.Tasks.Task TearDown()
        {
            return this.DisposeAsync().AsTask();
        }

        /// <summary>
        /// Verifies route, section order, component examples, and the theme control.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysCanonicalShowcase()
        {
            var component = this.Render<DesignSystem>();
            var route = typeof(DesignSystem)
                .GetCustomAttributes(typeof(RouteAttribute), false)
                .Cast<RouteAttribute>()
                .Single();
            var sectionOrder = component
                .FindAll(".mb-design-system > section[data-section]")
                .Select(section => section.GetAttribute("data-section"))
                .ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(route.Template, Is.EqualTo("/design-system"));
                Assert.That(sectionOrder, Is.EqualTo(ExpectedSectionOrder));
                Assert.That(component.FindAll("[data-component='avatar'] .mb-design-system__avatar-fallback"),
                    Has.Count.EqualTo(4));
                Assert.That(component.FindAll("[data-component='tabs'] [role='tablist']"), Has.Count.EqualTo(2));
                Assert.That(component.FindAll("[data-component='select-input'] [role='combobox']"), Has.Count.EqualTo(5));
                Assert.That(component.FindAll("[data-component='action-menu']"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("[data-component='split-button'] .mb-split-button"), Has.Count.EqualTo(4));
                Assert.That(component.FindAll("[data-component='project-switcher'] .mb-project-switcher"), Has.Count.EqualTo(2));
                Assert.That(component.FindAll("[data-component='user-menu'] .mb-user-menu"), Has.Count.EqualTo(2));
                Assert.That(component.FindAll("[data-component='app-header']"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("[data-component='workspace-shell']"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("[data-component='workspace-shell-optional-regions'] .mb-workspace-shell"),
                    Has.Count.EqualTo(3));
                Assert.That(component.Find("[role='group'][aria-label='Preview color theme']"), Is.Not.Null);
                Assert.That(component.FindAll("[role='group'][aria-label='Preview color theme'] button"), Has.Count.EqualTo(2));
                Assert.That(component.Find("[role='group'][aria-label='Preview color theme'] button[aria-pressed='true']")
                    .TextContent.Trim(), Is.EqualTo("Light"));
                Assert.That(component.FindAll("main"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies the editor-workspace examples use independent real state, canonical proportions, and generic content.
        /// </summary>
        [Test]
        public async Task VerifyEditorWorkspaceExamplesPreservePreviewBoundaries()
        {
            var component = this.Render<DesignSystem>();
            var editorWorkspaces = component.FindComponents<EditorWorkspaceComponent>();
            var canonicalWorkspace = editorWorkspaces.Single(workspace =>
                workspace.Instance.AriaLabel == "Three-group editor workspace preview");
            var compactWorkspace = editorWorkspaces.Single(workspace =>
                workspace.Instance.AriaLabel == "Compact editor workspace preview");
            var canonicalGroupIds = canonicalWorkspace.Instance.ViewModel.Groups
                .Select(group => group.Id)
                .ToArray();
            var canonicalWeights = canonicalGroupIds
                .Select(groupId => canonicalWorkspace.Instance.InitialGroupWeights[groupId])
                .ToArray();
            var duplicateViewTypeExists = canonicalWorkspace.Instance.ViewModel.Groups
                .SelectMany(group => group.Tabs)
                .GroupBy(tab => tab.ViewTypeKey, StringComparer.Ordinal)
                .Any(group => group.Count() > 1);
            var canonicalStage = component.Find("[data-testid='editor-workspace-canonical']");
            var compactStage = component.Find("[data-testid='editor-workspace-compact']");
            var requestedGroup = canonicalWorkspace.Instance.ViewModel.Groups[1];
            var requestedGroupInitialTabCount = requestedGroup.Tabs.Count;
            var canonicalTabCount = canonicalWorkspace.Instance.ViewModel.Groups.Sum(group => group.Tabs.Count);
            var untouchedGroup = canonicalWorkspace.Instance.ViewModel.Groups[0];
            var untouchedGroupInitialTabCount = untouchedGroup.Tabs.Count;
            var compactTabCount = compactWorkspace.Instance.ViewModel.Groups.Sum(group => group.Tabs.Count);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(editorWorkspaces, Has.Count.EqualTo(2));
                Assert.That(canonicalWorkspace.Instance.ViewModel, Is.TypeOf<WorkspaceEditorViewModel>());
                Assert.That(compactWorkspace.Instance.ViewModel, Is.TypeOf<WorkspaceEditorViewModel>());
                Assert.That(compactWorkspace.Instance.ViewModel,
                    Is.Not.SameAs(canonicalWorkspace.Instance.ViewModel));
                Assert.That(canonicalWorkspace.Instance.ViewModel.Groups, Has.Count.EqualTo(3));
                Assert.That(compactWorkspace.Instance.ViewModel.Groups, Has.Count.EqualTo(3));
                Assert.That(canonicalWorkspace.Instance.InitialGroupWeights.Keys,
                    Is.EquivalentTo(canonicalGroupIds));
                Assert.That(canonicalWeights, Is.EqualTo(ExpectedCanonicalGroupWeights));
                Assert.That(duplicateViewTypeExists, Is.True);
                Assert.That(canonicalWorkspace.Instance.AddTabRequested.HasDelegate, Is.True);
                Assert.That(compactWorkspace.Instance.AddTabRequested.HasDelegate, Is.True);
                Assert.That(canonicalStage.QuerySelectorAll("[data-preview-tab-id]"), Has.Count.EqualTo(3));
                Assert.That(compactStage.QuerySelectorAll("[data-preview-tab-id]"), Has.Count.EqualTo(3));
                Assert.That(compactStage.ClassList,
                    Does.Contain("mb-design-system__editor-workspace-stage--compact"));
            }

            var requestedGroupElement = canonicalStage.QuerySelector(
                $"[data-testid='editor-workspace-group'][data-group-id='{requestedGroup.Id}']");
            var addTabButton = requestedGroupElement.QuerySelector(
                "[data-testid='editor-workspace-add-tab']");

            await addTabButton.ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(requestedGroup.Tabs, Has.Count.EqualTo(requestedGroupInitialTabCount + 1));
                Assert.That(canonicalWorkspace.Instance.ViewModel.Groups.Sum(group => group.Tabs.Count),
                    Is.EqualTo(canonicalTabCount + 1));
                Assert.That(requestedGroup.ActiveTab.Title, Is.EqualTo("Untitled editor"));
                Assert.That(requestedGroup.ActiveTab.ViewTypeKey, Is.EqualTo("generic-placeholder"));
                Assert.That(untouchedGroup.Tabs, Has.Count.EqualTo(untouchedGroupInitialTabCount));
                Assert.That(compactWorkspace.Instance.ViewModel.Groups.Sum(group => group.Tabs.Count),
                    Is.EqualTo(compactTabCount));
            }
        }

        [Test]
        public async Task VerifyNavigationPreviewBoundaryObservesAndDetachesWithoutOwningViewModel()
        {
            var firstItem = new NavigationRailItem
            {
                Label = "First"
            };
            var secondItem = new NavigationRailItem
            {
                Label = "Second"
            };
            var selectedItem = firstItem;
            var viewModel = new Mock<INavigationRailViewModel>(MockBehavior.Strict);
            viewModel.SetupGet(model => model.SelectedItem).Returns(() => selectedItem);
            RenderFragment<INavigationRailViewModel> content = model => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "data-testid", "preview-selection");
                builder.AddContent(2, model.SelectedItem?.Label);
                builder.CloseElement();
            };
            var component = this.Render<DesignSystemNavigationRailPreview>(parameters => parameters
                .Add(preview => preview.ViewModel, viewModel.Object)
                .Add(preview => preview.ChildContent, content));

            selectedItem = secondItem;
            viewModel.Raise(
                model => model.PropertyChanged += null,
                new PropertyChangedEventArgs(nameof(INavigationRailViewModel.SelectedItem)));

            await component.WaitForAssertionAsync(() =>
                Assert.That(component.Find("[data-testid='preview-selection']").TextContent,
                    Is.EqualTo("Second")));
            component.Dispose();
            var renderCountAfterDisposal = component.RenderCount;
            selectedItem = firstItem;
            viewModel.Raise(
                model => model.PropertyChanged += null,
                new PropertyChangedEventArgs(nameof(INavigationRailViewModel.SelectedItem)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.RenderCount, Is.EqualTo(renderCountAfterDisposal));
                viewModel.Verify(model => model.Dispose(), Times.Never);
            }
        }

        [Test]
        public async Task VerifyPageDisposesItsOwnedEditorPreviewViewModels()
        {
            var component = this.Render<DesignSystem>();
            var editorViewModel = (WorkspaceEditorViewModel)typeof(DesignSystem)
                .GetProperty(
                    "EditorWorkspacePreviewViewModel",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(component.Instance);
            var compactEditorViewModel = (WorkspaceEditorViewModel)typeof(DesignSystem)
                .GetProperty(
                    "CompactEditorWorkspacePreviewViewModel",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(component.Instance);

            await component.Instance.DisposeAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(editorViewModel.TryAddGroup(out _), Is.False);
                Assert.That(compactEditorViewModel.TryAddGroup(out _), Is.False);
            }
        }

        /// <summary>
        /// Verifies the page applies Light and Dark through the shared application theme service.
        /// </summary>
        [Test]
        public async System.Threading.Tasks.Task VerifyThemeControlAppliesDocumentLevelTheme()
        {
            var component = this.Render<DesignSystem>();
            var themeService = this.Services.GetRequiredService<ThemeService>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(themeService.IsDarkMode, Is.False);
                Assert.That(this.themeModule.Invocations["applyTheme"], Has.Count.EqualTo(1));
                Assert.That(this.themeModule.Invocations["applyTheme"][0].Arguments[0], Is.EqualTo(false));
                Assert.That(this.themeModule.Invocations["applyTheme"][0].Arguments[3], Is.EqualTo(0.375d));
            }

            await component.FindAll("[role='group'][aria-label='Preview color theme'] button")
                .Single(button => button.TextContent.Trim() == "Dark")
                .ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(themeService.IsDarkMode, Is.True);
                Assert.That(this.applyDarkModeHandler.Invocations, Has.Count.EqualTo(1));
                Assert.That(this.applyDarkModeHandler.Invocations["applyDarkMode"][0].Arguments[0], Is.EqualTo(true));
                Assert.That(component.FindAll("[role='group'][aria-label='Preview color theme'] button")
                    .Single(button => button.TextContent.Trim() == "Dark")
                    .GetAttribute("aria-pressed"), Is.EqualTo("true"));
                Assert.That(component.FindAll("[role='group'][aria-label='Preview color theme'] button")
                    .Single(button => button.TextContent.Trim() == "Light")
                    .GetAttribute("aria-pressed"), Is.EqualTo("false"));
            }

            await component.FindAll("[role='group'][aria-label='Preview color theme'] button")
                .Single(button => button.TextContent.Trim() == "Light")
                .ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(themeService.IsDarkMode, Is.False);
                Assert.That(this.applyDarkModeHandler.Invocations, Has.Count.EqualTo(2));
                Assert.That(this.applyDarkModeHandler.Invocations["applyDarkMode"][1].Arguments[0], Is.EqualTo(false));
                Assert.That(component.FindAll("[role='group'][aria-label='Preview color theme'] button")
                    .Single(button => button.TextContent.Trim() == "Light")
                    .GetAttribute("aria-pressed"), Is.EqualTo("true"));
                Assert.That(component.FindAll("[role='group'][aria-label='Preview color theme'] button")
                    .Single(button => button.TextContent.Trim() == "Dark")
                    .GetAttribute("aria-pressed"), Is.EqualTo("false"));
            }
        }

        /// <summary>
        /// Verifies page disposal does not release or replace the application-owned theme state.
        /// </summary>
        [Test]
        public async System.Threading.Tasks.Task VerifyThemeStateRemainsApplicationOwnedOnDispose()
        {
            var component = this.Render<DesignSystem>();
            var themeService = this.Services.GetRequiredService<ThemeService>();
            await component.FindAll("[role='group'][aria-label='Preview color theme'] button")
                .Single(button => button.TextContent.Trim() == "Dark")
                .ClickAsync();

            await component.Instance.DisposeAsync();

            Assert.That(themeService.IsDarkMode, Is.True);
        }

        /// <summary>
        /// Verifies the canonical showcase does not mount any interactive overlay on initial render.
        /// </summary>
        [Test]
        public void VerifyInitialOverlayExamplesAreClosed()
        {
            var component = this.Render<DesignSystem>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.portalHost.FindAll("[role='menu']"), Is.Empty);
                Assert.That(this.portalHost.FindAll("[role='listbox']"), Is.Empty);
                Assert.That(this.portalHost.FindAll("[role='dialog']"), Is.Empty);
                Assert.That(this.portalHost.FindAll("[role='tooltip']"), Is.Empty);
                Assert.That(component.FindAll("[role='tooltip']"), Is.Empty);
                Assert.That(component.FindAll("[aria-expanded='true']"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies dark selection and a Blueprint overlay coexist under the document-level theme bridge.
        /// </summary>
        [Test]
        public async System.Threading.Tasks.Task VerifyDarkThemeSelectionCoversPortalledOverlays()
        {
            var component = this.Render<DesignSystem>();
            await component.FindAll("[role='group'][aria-label='Preview color theme'] button")
                .Single(button => button.TextContent.Trim() == "Dark")
                .ClickAsync();
            await component.Find("[data-testid='action-menu-primary'] button").ClickAsync();
            var menu = await this.portalHost.WaitForElementAsync("[role='menu']");
            var menuRendered = menu.GetAttribute("role") == "menu";
            await component.Find("[data-testid='action-menu-primary'] button").ClickAsync();
            await component.Find("#showcase-select-input").ClickAsync();
            var listbox = await this.portalHost.WaitForElementAsync("[role='listbox']");
            var applyDarkModeInvocations = this.applyDarkModeHandler.Invocations["applyDarkMode"];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(applyDarkModeInvocations[applyDarkModeInvocations.Count - 1].Arguments[0], Is.EqualTo(true));
                Assert.That(menuRendered, Is.True);
                Assert.That(listbox.ClassList, Does.Contain("mb-select-input__listbox"));
            }
        }

        /// <summary>
        /// Verifies representative controlled form and navigation examples update page-owned state.
        /// </summary>
        [Test]
        public async System.Threading.Tasks.Task VerifyInteractiveExamplesUpdatePageState()
        {
            var component = this.Render<DesignSystem>();
            var blueprintInputs = component.FindComponents<BbInputGroupInput>();

            await component.InvokeAsync(() => blueprintInputs
                .Single(input => input.Instance.Id == "showcase-search-input")
                .Instance.JsOnInput("interfaces"));
            await component.InvokeAsync(() => blueprintInputs
                .Single(input => input.Instance.Id == "showcase-text-input")
                .Instance.JsOnInput("Power subsystem"));
            await component.Find("#showcase-text-area").InputAsync("Updated review note");
            await component.Find("#showcase-checkbox").ChangeAsync(false);
            await component.Find("#showcase-toggle").ClickAsync();
            await component.Find("[data-component='tabs']").QuerySelectorAll("[role='tab']")
                .Single(tab => tab.TextContent.Contains("Properties", StringComparison.Ordinal))
                .ClickAsync();

            await component.Find("#showcase-select-input").ClickAsync();
            var options = await this.portalHost.WaitForElementsAsync("[role='option']");
            await options.Single(option => option.TextContent.Trim() == "Open").ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#search-input-result").TextContent, Does.Contain("interfaces"));
                Assert.That(component.Find("#text-input-result").TextContent, Does.Contain("Power subsystem"));
                Assert.That(component.Find("#select-input-result").TextContent, Does.Contain("open"));
                Assert.That(component.Find("#text-area-result").TextContent, Does.Contain("Updated review note"));
                Assert.That(component.Find("#checkbox-result").TextContent, Does.Contain("hidden"));
                Assert.That(component.Find("#toggle-result").TextContent, Does.Contain("on"));
                Assert.That(component.Find("#tabs-result").TextContent, Does.Contain("properties"));
            }
        }

        /// <summary>
        /// Verifies direct Blueprint tabs preserve names, controlled pointer selection, disabled state, and identifiers.
        /// </summary>
        [Test]
        public async System.Threading.Tasks.Task VerifyDirectTabsPreserveConsumerContracts()
        {
            var component = this.Render<DesignSystem>();
            var tabsExample = component.Find("[data-component='tabs']");
            var horizontalTabs = tabsExample.QuerySelector("[data-testid='tabs-horizontal']");

            await horizontalTabs.QuerySelectorAll("[role='tab']")
                .Single(tab => tab.TextContent.Trim() == "Properties")
                .ClickAsync();

            tabsExample = component.Find("[data-component='tabs']");
            var tabLists = tabsExample.QuerySelectorAll("[role='tablist']");
            var horizontalTabElements = tabLists[0].QuerySelectorAll("[role='tab']");
            var verticalTabElements = tabLists[1].QuerySelectorAll("[role='tab']");
            var renderedTabs = tabsExample.QuerySelectorAll("[role='tab']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tabLists[0].GetAttribute("aria-label"), Is.EqualTo("Element detail sections"));
                Assert.That(tabLists[0].GetAttribute("aria-orientation"), Is.EqualTo("horizontal"));
                Assert.That(tabLists[1].GetAttribute("aria-label"), Is.EqualTo("Element review sections"));
                Assert.That(tabLists[1].GetAttribute("aria-orientation"), Is.EqualTo("vertical"));
                Assert.That(horizontalTabElements.Single(tab => tab.TextContent.Trim() == "Properties")
                    .GetAttribute("aria-selected"), Is.EqualTo("true"));
                Assert.That(tabsExample.QuerySelectorAll("[role='tabpanel']")
                    .Any(panel => panel.TextContent.Contains("Properties panel", StringComparison.Ordinal)), Is.True);
                Assert.That(verticalTabElements.Single(tab => tab.TextContent.Trim() == "Summary")
                    .GetAttribute("aria-selected"), Is.EqualTo("true"));
                Assert.That(horizontalTabElements.Single(tab => tab.TextContent.Trim() == "History")
                    .HasAttribute("disabled"), Is.True);
                Assert.That(horizontalTabElements.Single(tab => tab.TextContent.Trim() == "History")
                    .GetAttribute("aria-disabled"), Is.EqualTo("true"));
                Assert.That(verticalTabElements.Single(tab => tab.TextContent.Trim() == "Archive")
                    .HasAttribute("disabled"), Is.True);
                Assert.That(verticalTabElements.Single(tab => tab.TextContent.Trim() == "Archive")
                    .GetAttribute("aria-disabled"), Is.EqualTo("true"));
                Assert.That(renderedTabs.All(tab => tab.Attributes.Count(attribute =>
                    string.Equals(attribute.Name, "aria-selected", StringComparison.OrdinalIgnoreCase)) == 1), Is.True);
                Assert.That(renderedTabs.All(tab =>
                    tab.GetAttribute("aria-selected") is "true" or "false"), Is.True);
            }

            var verticalTabs = component.Find("[data-component='tabs'] [data-testid='tabs-vertical']");

            await verticalTabs.QuerySelectorAll("[role='tab']")
                .Single(tab => tab.TextContent.Trim() == "Verification")
                .ClickAsync();

            tabsExample = component.Find("[data-component='tabs']");
            horizontalTabElements = tabsExample.QuerySelectorAll("[role='tablist']")[0]
                .QuerySelectorAll("[role='tab']");

            var tabs = tabsExample.QuerySelectorAll("[role='tab']");
            var tabIds = tabs.Select(tab => tab.Id).ToArray();
            var panelIds = tabs.Select(tab => tab.GetAttribute("aria-controls")).ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#tabs-result").TextContent, Does.Contain("properties"));
                Assert.That(component.Find("#vertical-tabs-result").TextContent, Does.Contain("verification"));
                Assert.That(horizontalTabElements.Single(tab => tab.TextContent.Trim() == "Properties")
                    .GetAttribute("aria-selected"), Is.EqualTo("true"));
                Assert.That(tabsExample.QuerySelectorAll("[role='tabpanel']")
                    .Any(panel => panel.TextContent.Contains("Verification panel", StringComparison.Ordinal)), Is.True);
                Assert.That(tabIds.All(id => !string.IsNullOrWhiteSpace(id)), Is.True);
                Assert.That(tabIds.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(tabIds.Length));
                Assert.That(panelIds.All(id => !string.IsNullOrWhiteSpace(id)), Is.True);
                Assert.That(panelIds.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(panelIds.Length));
            }

            await tabsExample.QuerySelectorAll("button")
                .Single(button => button.TextContent.Trim() == "Select overview externally")
                .ClickAsync();
            tabsExample = component.Find("[data-component='tabs']");
            await tabsExample.QuerySelectorAll("button")
                .Single(button => button.TextContent.Trim() == "Select summary externally")
                .ClickAsync();

            tabsExample = component.Find("[data-component='tabs']");
            tabLists = tabsExample.QuerySelectorAll("[role='tablist']");
            horizontalTabElements = tabLists[0].QuerySelectorAll("[role='tab']");
            verticalTabElements = tabLists[1].QuerySelectorAll("[role='tab']");
            renderedTabs = tabsExample.QuerySelectorAll("[role='tab']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(horizontalTabElements.Single(tab => tab.TextContent.Trim() == "Overview")
                    .GetAttribute("aria-selected"), Is.EqualTo("true"));
                Assert.That(horizontalTabElements.Single(tab => tab.TextContent.Trim() == "Properties")
                    .GetAttribute("aria-selected"), Is.EqualTo("false"));
                Assert.That(verticalTabElements.Single(tab => tab.TextContent.Trim() == "Summary")
                    .GetAttribute("aria-selected"), Is.EqualTo("true"));
                Assert.That(verticalTabElements.Single(tab => tab.TextContent.Trim() == "Verification")
                    .GetAttribute("aria-selected"), Is.EqualTo("false"));
                Assert.That(tabsExample.QuerySelectorAll("[role='tabpanel']")
                    .Any(panel => panel.TextContent.Contains("Overview panel", StringComparison.Ordinal)), Is.True);
                Assert.That(tabsExample.QuerySelectorAll("[role='tabpanel']")
                    .Any(panel => panel.TextContent.Contains("Summary panel", StringComparison.Ordinal)), Is.True);
                Assert.That(renderedTabs.All(tab => tab.Attributes.Count(attribute =>
                    string.Equals(attribute.Name, "aria-selected", StringComparison.OrdinalIgnoreCase)) == 1), Is.True);
                Assert.That(renderedTabs.All(tab =>
                    tab.GetAttribute("aria-selected") is "true" or "false"), Is.True);
            }
        }

        /// <summary>
        /// Verifies named controls retain supplementary pointer hints without rendering Tooltip markup.
        /// </summary>
        [Test]
        public void VerifyNamedControlsDoNotRenderTooltips()
        {
            var component = this.Render<DesignSystem>();

            foreach (var accessibleName in NamedControlNames)
            {
                var matchingButtons = component.FindAll($"button[aria-label='{accessibleName}']");

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(matchingButtons, Is.Not.Empty, $"Missing control named '{accessibleName}'.");
                    Assert.That(matchingButtons.All(button =>
                        string.Equals(button.GetAttribute("title"), accessibleName, StringComparison.Ordinal)), Is.True);
                }
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("[role='tooltip']"), Is.Empty);
                Assert.That(this.portalHost.FindAll("[role='tooltip']"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies direct Blueprint consumers preserve page-owned names, relationships, native states, and workflows.
        /// </summary>
        [Test]
        public async System.Threading.Tasks.Task VerifyDirectBlueprintConsumersPreserveAccessibility()
        {
            var component = this.Render<DesignSystem>();
            var textInput = component.Find("#showcase-text-input");
            var invalidInput = component.Find("#showcase-text-error");
            var disabledInput = component.Find("#showcase-text-disabled");
            var readOnlyInput = component.Find("#showcase-text-readonly");
            var toggle = component.Find("#showcase-toggle");
            var disabledToggle = component.Find("#showcase-toggle-disabled");
            var breadcrumb = component.Find("nav[aria-label='Showcase hierarchy']");

            var breadcrumbButtons = breadcrumb.QuerySelectorAll("button");

            await breadcrumbButtons[0].ClickAsync();
            Assert.That(component.Find("#breadcrumbs-result").TextContent, Does.Contain("Workspace"));

            await breadcrumbButtons[1].ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("label[for='showcase-text-input']").TextContent,
                    Does.Contain("Element name"));
                Assert.That(textInput.HasAttribute("required"), Is.True);
                Assert.That(textInput.GetAttribute("aria-describedby"), Is.EqualTo("showcase-text-input-help"));
                Assert.That(invalidInput.GetAttribute("aria-invalid"), Is.EqualTo("true"));
                Assert.That(invalidInput.GetAttribute("aria-describedby"), Is.EqualTo("showcase-text-error-error"));
                Assert.That(disabledInput.HasAttribute("disabled"), Is.True);
                Assert.That(readOnlyInput.HasAttribute("readonly"), Is.True);
                Assert.That(toggle.GetAttribute("aria-label"), Is.EqualTo("Live collaboration"));
                Assert.That(toggle.GetAttribute("aria-describedby"), Is.EqualTo("showcase-toggle-description"));
                Assert.That(disabledToggle.HasAttribute("disabled"), Is.True);
                Assert.That(component.Find("[data-component='icon-button'] button[aria-label='Add model element']"),
                    Is.Not.Null);
                Assert.That(component.Find("[data-component='loading-state'] [role='status']")
                    .GetAttribute("aria-busy"), Is.EqualTo("true"));
                Assert.That(component.FindAll("[role='toolbar'][aria-label='Element editing tools']"),
                    Has.Count.EqualTo(1));
                Assert.That(component.FindAll("[role='toolbar'][aria-label='Horizontal canvas tools']"),
                    Has.Count.EqualTo(1));
                Assert.That(component.Find("[role='toolbar'][aria-label='Vertical canvas tools']")
                    .GetAttribute("aria-orientation"), Is.EqualTo("vertical"));
                Assert.That(component.FindAll("[data-component='empty-state'] h3")
                    .Select(element => element.TextContent), Does.Contain("No relationships"));
                Assert.That(component.Find("#breadcrumbs-result").TextContent, Does.Contain("Projects"));
            }
        }

        /// <summary>
        /// Verifies workspace controls preserve controlled state and optional-region semantics.
        /// </summary>
        [Test]
        public async System.Threading.Tasks.Task VerifyWorkspaceExamplesPreserveLayoutBehavior()
        {
            var component = this.Render<DesignSystem>();
            var zoomExample = component.Find("[data-component='zoom-controls']");

            await zoomExample.QuerySelector("button[aria-label='Zoom in']").ClickAsync();
            await component.Find("#toggle-workspace-left-panel").ClickAsync();
            await component.Find("#toggle-workspace-right-panel").ClickAsync();

            var optionalWorkspaces = component.FindAll(
                "[data-component='workspace-shell-optional-regions'] .mb-workspace-shell");
            var narrowWorkspace = optionalWorkspaces[2];
            var detailsButton = narrowWorkspace.QuerySelectorAll(".mb-workspace-shell__pane-button")
                .Single(button => button.TextContent.Trim() == "Details");

            await detailsButton.ClickAsync();
            narrowWorkspace = component.FindAll(
                "[data-component='workspace-shell-optional-regions'] .mb-workspace-shell")[2];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#zoom-controls-result").TextContent, Does.Contain("125%"));
                Assert.That(component.FindAll("[data-component='workspace-shell'] .mb-workspace-shell__left-panel"), Is.Empty);
                Assert.That(component.FindAll("[data-component='workspace-shell'] .mb-workspace-shell__right-panel"), Is.Empty);
                Assert.That(component.Find("#workspace-shell-result").TextContent, Does.Contain("Left panel: hidden"));
                Assert.That(component.Find("#workspace-shell-result").TextContent, Does.Contain("Right panel: hidden"));
                Assert.That(optionalWorkspaces, Has.Count.EqualTo(3));
                Assert.That(optionalWorkspaces.SelectMany(workspace => workspace.QuerySelectorAll("main")), Is.Empty);
                Assert.That(optionalWorkspaces[0].QuerySelectorAll("header"), Is.Empty);
                Assert.That(optionalWorkspaces[0].QuerySelectorAll("footer"), Is.Empty);
                Assert.That(optionalWorkspaces[1].QuerySelectorAll("header"), Has.Count.EqualTo(1));
                Assert.That(optionalWorkspaces[2].QuerySelectorAll("footer"), Has.Count.EqualTo(1));
                Assert.That(narrowWorkspace.QuerySelector(".mb-workspace-shell__right-panel")
                    .GetAttribute("data-narrow-active"), Is.EqualTo("true"));
                Assert.That(narrowWorkspace.QuerySelector(".mb-workspace-shell__main")
                    .GetAttribute("data-narrow-active"), Is.EqualTo("false"));
            }
        }

        /// <summary>
        /// Verifies shortcut registrations and controlled select examples retain independent page-owned values.
        /// </summary>
        [Test]
        public async System.Threading.Tasks.Task VerifyShortcutAndSelectExamplesMaintainIndependentState()
        {
            var component = this.Render<DesignSystem>();
            var blueprintInputs = component.FindComponents<BbInputGroupInput>();

            await component.InvokeAsync(() => blueprintInputs
                .Single(input => input.Instance.Id == "showcase-search-shortcut")
                .Instance.JsOnInput("primary target"));
            await component.InvokeAsync(() => blueprintInputs
                .Single(input => input.Instance.Id == "showcase-search-shortcut-secondary")
                .Instance.JsOnInput("secondary target"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#showcase-search-shortcut").GetAttribute("value"),
                    Is.EqualTo("primary target"));
                Assert.That(component.Find("#showcase-search-shortcut-secondary").GetAttribute("value"),
                    Is.EqualTo("secondary target"));
            }

            await component.Find("#toggle-secondary-shortcut-search").ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("#showcase-search-shortcut-secondary"), Is.Empty);
                Assert.That(component.Find("#search-shortcut-result").TextContent, Does.Contain("primary (restored)"));
            }

            await component.Find("#toggle-secondary-shortcut-search").ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#showcase-search-shortcut-secondary").GetAttribute("value"),
                    Is.EqualTo("secondary target"));
                Assert.That(component.Find("#search-shortcut-result").TextContent, Does.Contain("secondary (newest)"));
            }

            await component.Find("#showcase-select-secondary").ClickAsync();
            var options = await this.portalHost.WaitForElementsAsync("[role='option']");
            await options.Single(option => option.TextContent.Trim() == "In review").ClickAsync();

            Assert.That(component.Find("#select-input-result").TextContent, Does.Contain("review / review"));

            await component.Find("[data-testid='set-primary-select-open']").ClickAsync();

            Assert.That(component.Find("#select-input-result").TextContent, Does.Contain("open / review"));

            await component.Find("[data-testid='reset-primary-select']").ClickAsync();

            Assert.That(component.Find("#select-input-result").TextContent, Does.Contain("review / review"));
        }

        /// <summary>
        /// Verifies the workspace compositions forward search, project, action, tool, zoom, and status callbacks.
        /// </summary>
        [Test]
        public async System.Threading.Tasks.Task VerifyWorkspaceCallbacksUpdatePageOwnedState()
        {
            var component = this.Render<DesignSystem>();
            var workspaceSearch = component.FindComponents<BbInputGroupInput>()
                .Single(input => input.Instance.Id == "workspace-header-search");
            var appHeaderExample = component.Find("[data-component='app-header']");

            await component.InvokeAsync(() => workspaceSearch.Instance.JsOnInput("interfaces"));

            Assert.That(component.Find("#app-header-result").TextContent, Does.Contain("Search: interfaces"));

            await appHeaderExample.QuerySelector("button[aria-label^='Select header showcase project']").ClickAsync();
            var projectMenuItems = await this.portalHost.WaitForElementsAsync("[role='menuitem']");
            await projectMenuItems.Single(item =>
                item.TextContent.Contains("Lunar Habitat", StringComparison.Ordinal)).ClickAsync();

            Assert.That(component.Find("#app-header-result").TextContent, Does.Contain("Selected Lunar Habitat"));

            await component.Find("button[aria-label='Share workspace']").ClickAsync();
            Assert.That(component.Find("#app-header-result").TextContent, Does.Contain("Share requested"));

            await appHeaderExample.QuerySelectorAll("button")
                .Single(button => button.TextContent.Trim() == "Validate")
                .ClickAsync();
            Assert.That(component.Find("#app-header-result").TextContent, Does.Contain("Validation requested"));

            await component.Find("button[aria-label='Open compact header action']").ClickAsync();
            Assert.That(component.Find("#app-header-result").TextContent, Does.Contain("Compact action requested"));

            await component.Find("button[aria-label='Select element']").ClickAsync();
            await component.FindAll("button")
                .Single(button => button.TextContent.Trim() == "Connect")
                .ClickAsync();
            await component.Find("button[aria-label='Add note']").ClickAsync();
            await component.Find("button[aria-label='Move canvas']").ClickAsync();
            await component.Find("button[aria-label='Center selection']").ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#canvas-toolbar-result").TextContent, Does.Contain("Center"));
                Assert.That(component.Find("button[aria-label='Center selection']").GetAttribute("aria-pressed"),
                    Is.EqualTo("true"));
                Assert.That(component.Find("button[aria-label='Select element']").GetAttribute("aria-pressed"),
                    Is.EqualTo("false"));
            }

            var zoomExample = component.Find("[data-component='zoom-controls']");
            await zoomExample.QuerySelector("button[aria-label='Reset zoom']").ClickAsync();
            Assert.That(component.Find("#zoom-controls-result").TextContent, Does.Contain("Zoom reset to 100%"));

            await zoomExample.QuerySelector("button[aria-label='Fit to view']").ClickAsync();
            Assert.That(component.Find("#zoom-controls-result").TextContent, Does.Contain("Fit to view at 75%"));

            await component.Find("[data-component='status-bar'] button[aria-label='Open status details']").ClickAsync();
            Assert.That(component.Find("#status-bar-result").TextContent, Does.Contain("Status details requested"));
        }

        /// <summary>
        /// Verifies action-menu delivery and dialog workflows update their page-owned result state.
        /// </summary>
        [Test]
        public async System.Threading.Tasks.Task VerifyMenuAndDialogCallbacksUpdatePageOwnedState()
        {
            var component = this.Render<DesignSystem>();

            await component.Find("[data-testid='action-menu-primary'] button").ClickAsync();
            var menuItems = await this.portalHost.WaitForElementsAsync("[role='menuitem']");
            await menuItems.Single(item =>
                item.TextContent.Contains("Open details", StringComparison.Ordinal)).ClickAsync();

            Assert.That(component.Find("#action-menu-result").TextContent,
                Does.Contain("Latest action: Open details. Selection count: 1"));

            await component.Find("#open-compact-modal").ClickAsync();
            var dialog = await this.portalHost.WaitForElementAsync("[role='dialog']");

            Assert.That(dialog.ClassList, Does.Contain("max-w-[22.5rem]"));

            await dialog.QuerySelectorAll("button")
                .Single(button => button.TextContent.Trim() == "Close")
                .ClickAsync();
            Assert.That(component.Find("#modal-result").TextContent, Does.Contain("Closed modal"));

            await component.Find("#open-wide-modal").ClickAsync();
            dialog = await this.portalHost.WaitForElementAsync("[role='dialog']");

            Assert.That(dialog.ClassList, Does.Contain("max-w-[52.5rem]"));

            await dialog.QuerySelectorAll("button")
                .Single(button => button.TextContent.Trim() == "Apply locally")
                .ClickAsync();

            await component.Find("#open-default-confirm").ClickAsync();
            dialog = await this.portalHost.WaitForElementAsync("[role='dialog']");
            await dialog.QuerySelectorAll("button")
                .Single(button => button.TextContent.Trim() == "Cancel action")
                .ClickAsync();

            Assert.That(component.Find("#confirm-result").TextContent, Does.Contain("Cancelled default action"));

            await component.Find("#open-warning-confirm").ClickAsync();
            dialog = await this.portalHost.WaitForElementAsync("[role='dialog']");
            await dialog.QuerySelectorAll("button")
                .Single(button => button.TextContent.Trim() == "Confirm action")
                .ClickAsync();

            Assert.That(component.Find("#confirm-result").TextContent, Does.Contain("Confirmed warning action"));

            await component.Find("#open-danger-confirm").ClickAsync();
            dialog = await this.portalHost.WaitForElementAsync("[role='dialog']");
            await dialog.QuerySelectorAll("button")
                .Single(button => button.TextContent.Trim() == "Cancel action")
                .ClickAsync();

            Assert.That(component.Find("#confirm-result").TextContent, Does.Contain("Cancelled danger action"));

            await component.Find("#open-loading-confirm").ClickAsync();
            dialog = await this.portalHost.WaitForElementAsync("[role='dialog']");
            var loadingActions = dialog.QuerySelectorAll("button");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(loadingActions, Has.Count.EqualTo(2));
                Assert.That(loadingActions.All(button => button.HasAttribute("disabled")), Is.True);
                Assert.That(
                    loadingActions.Single(button => button.TextContent.Contains("Confirm action", StringComparison.Ordinal))
                        .GetAttribute("aria-busy"),
                    Is.EqualTo("true"));
            }
        }

        /// <summary>
        /// Verifies rendered page and portal markup do not introduce duplicate identifiers.
        /// </summary>
        [Test]
        public void VerifyRenderedShowcaseHasNoDuplicateIdentifiers()
        {
            var component = this.Render<DesignSystem>();
            var ids = component.FindAll("[id]")
                .Concat(this.portalHost.FindAll("[id]"))
                .Select(element => element.Id)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToArray();
            var duplicates = ids
                .GroupBy(id => id, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();

            Assert.That(duplicates, Is.Empty, $"Duplicate ids: {string.Join(", ", duplicates)}");
        }
    }
}
