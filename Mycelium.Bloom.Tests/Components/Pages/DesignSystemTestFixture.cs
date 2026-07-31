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
    using System.Linq;

    using BlazorBlueprint.Components;
    using BlazorBlueprint.Primitives.Services;

    using Bunit;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Pages;
    using Mycelium.Bloom.Tests.Common;

    /// <summary>
    /// Tests the canonical Bloom production-component showcase.
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

        private static readonly string[] FormerTooltipControlNames =
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
        private readonly JSRuntimeInvocationHandler applyThemeHandler;
        private readonly JSRuntimeInvocationHandler releaseThemeHandler;

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

            this.themeModule = this.JSInterop.SetupModule("./Components/Pages/DesignSystem.razor.js");
            this.applyThemeHandler = this.themeModule.SetupVoid("applyTheme", invocation => true);
            this.releaseThemeHandler = this.themeModule.SetupVoid("releaseTheme", invocation => true);
            this.applyThemeHandler.SetVoidResult();
            this.releaseThemeHandler.SetVoidResult();
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
        /// Verifies route, section order, production examples, theme control, and legacy-reference link.
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
                Assert.That(component.FindAll("[role='tablist']"), Has.Count.EqualTo(2));
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
        /// Verifies the page applies the initial light theme at the document root and reports dark selection.
        /// </summary>
        [Test]
        public async System.Threading.Tasks.Task VerifyThemeControlAppliesDocumentLevelTheme()
        {
            var component = this.Render<DesignSystem>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.applyThemeHandler.Invocations, Has.Count.EqualTo(1));
                Assert.That(this.applyThemeHandler.Invocations["applyTheme"][0].Arguments[1], Is.EqualTo("light"));
            }

            await component.FindAll("[role='group'][aria-label='Preview color theme'] button")
                .Single(button => button.TextContent.Trim() == "Dark")
                .ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.applyThemeHandler.Invocations, Has.Count.EqualTo(2));
                Assert.That(this.applyThemeHandler.Invocations["applyTheme"][1].Arguments[1], Is.EqualTo("dark"));
                Assert.That(component.FindAll("[role='group'][aria-label='Preview color theme'] button")
                    .Single(button => button.TextContent.Trim() == "Dark")
                    .GetAttribute("aria-pressed"), Is.EqualTo("true"));
                Assert.That(component.FindAll("[role='group'][aria-label='Preview color theme'] button")
                    .Single(button => button.TextContent.Trim() == "Light")
                    .GetAttribute("aria-pressed"), Is.EqualTo("false"));
            }
        }

        /// <summary>
        /// Verifies page disposal releases only the theme preview owned by that page instance.
        /// </summary>
        [Test]
        public async System.Threading.Tasks.Task VerifyThemePreviewIsReleasedOnDispose()
        {
            var component = this.Render<DesignSystem>();
            var ownerId = this.applyThemeHandler.Invocations["applyTheme"][0].Arguments[0];

            await component.Instance.DisposeAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.releaseThemeHandler.Invocations, Has.Count.EqualTo(1));
                Assert.That(this.releaseThemeHandler.Invocations["releaseTheme"][0].Arguments[0], Is.EqualTo(ownerId));
            }
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
            var menu = this.portalHost.WaitForElement("[role='menu']");
            var menuRendered = menu.GetAttribute("role") == "menu";
            await component.Find("[data-testid='action-menu-primary'] button").ClickAsync();
            await component.Find("#showcase-select-input").ClickAsync();
            var listbox = this.portalHost.WaitForElement("[role='listbox']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.applyThemeHandler.Invocations["applyTheme"].Last().Arguments[1], Is.EqualTo("dark"));
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
            await component.FindAll("[role='tab']")
                .Single(tab => tab.TextContent.Contains("Properties", StringComparison.Ordinal))
                .ClickAsync();

            await component.Find("#showcase-select-input").ClickAsync();
            await this.portalHost.WaitForElements("[role='option']")
                .Single(option => option.TextContent.Trim() == "Open")
                .ClickAsync();

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
            var tabLists = component.FindAll("[role='tablist']");
            var horizontalTabs = component.Find("[data-testid='tabs-horizontal']");
            var verticalTabs = component.Find("[data-testid='tabs-vertical']");

            await horizontalTabs.QuerySelectorAll("[role='tab']")
                .Single(tab => tab.TextContent.Trim() == "Properties")
                .ClickAsync();

            tabLists = component.FindAll("[role='tablist']");
            var horizontalTabElements = tabLists[0].QuerySelectorAll("[role='tab']");
            var verticalTabElements = tabLists[1].QuerySelectorAll("[role='tab']");
            var renderedTabs = component.FindAll("[role='tab']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tabLists[0].GetAttribute("aria-label"), Is.EqualTo("Element detail sections"));
                Assert.That(tabLists[0].GetAttribute("aria-orientation"), Is.EqualTo("horizontal"));
                Assert.That(tabLists[1].GetAttribute("aria-label"), Is.EqualTo("Element review sections"));
                Assert.That(tabLists[1].GetAttribute("aria-orientation"), Is.EqualTo("vertical"));
                Assert.That(horizontalTabElements.Single(tab => tab.TextContent.Trim() == "Properties")
                    .GetAttribute("aria-selected"), Is.EqualTo("true"));
                Assert.That(component.FindAll("[role='tabpanel']")
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

            verticalTabs = component.Find("[data-testid='tabs-vertical']");

            await verticalTabs.QuerySelectorAll("[role='tab']")
                .Single(tab => tab.TextContent.Trim() == "Verification")
                .ClickAsync();

            horizontalTabElements = component.FindAll("[role='tablist']")[0].QuerySelectorAll("[role='tab']");

            var tabs = component.FindAll("[role='tab']");
            var tabIds = tabs.Select(tab => tab.Id).ToArray();
            var panelIds = tabs.Select(tab => tab.GetAttribute("aria-controls")).ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#tabs-result").TextContent, Does.Contain("properties"));
                Assert.That(component.Find("#vertical-tabs-result").TextContent, Does.Contain("verification"));
                Assert.That(horizontalTabElements.Single(tab => tab.TextContent.Trim() == "Properties")
                    .GetAttribute("aria-selected"), Is.EqualTo("true"));
                Assert.That(component.FindAll("[role='tabpanel']")
                    .Any(panel => panel.TextContent.Contains("Verification panel", StringComparison.Ordinal)), Is.True);
                Assert.That(tabIds.All(id => !string.IsNullOrWhiteSpace(id)), Is.True);
                Assert.That(tabIds.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(tabIds.Length));
                Assert.That(panelIds.All(id => !string.IsNullOrWhiteSpace(id)), Is.True);
                Assert.That(panelIds.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(panelIds.Length));
            }

            await component.FindAll("button")
                .Single(button => button.TextContent.Trim() == "Select overview externally")
                .ClickAsync();
            await component.FindAll("button")
                .Single(button => button.TextContent.Trim() == "Select summary externally")
                .ClickAsync();

            tabLists = component.FindAll("[role='tablist']");
            horizontalTabElements = tabLists[0].QuerySelectorAll("[role='tab']");
            verticalTabElements = tabLists[1].QuerySelectorAll("[role='tab']");
            renderedTabs = component.FindAll("[role='tab']");

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
                Assert.That(component.FindAll("[role='tabpanel']")
                    .Any(panel => panel.TextContent.Contains("Overview panel", StringComparison.Ordinal)), Is.True);
                Assert.That(component.FindAll("[role='tabpanel']")
                    .Any(panel => panel.TextContent.Contains("Summary panel", StringComparison.Ordinal)), Is.True);
                Assert.That(renderedTabs.All(tab => tab.Attributes.Count(attribute =>
                    string.Equals(attribute.Name, "aria-selected", StringComparison.OrdinalIgnoreCase)) == 1), Is.True);
                Assert.That(renderedTabs.All(tab =>
                    tab.GetAttribute("aria-selected") is "true" or "false"), Is.True);
            }
        }

        /// <summary>
        /// Verifies former Tooltip triggers retain explicit names and supplementary pointer hints without Tooltip markup.
        /// </summary>
        [Test]
        public void VerifyFormerTooltipControlsRemainExplicitlyNamed()
        {
            var component = this.Render<DesignSystem>();

            foreach (var accessibleName in FormerTooltipControlNames)
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

            await breadcrumb.QuerySelector("button").ClickAsync();

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
                Assert.That(component.Find("#breadcrumbs-result").TextContent, Does.Contain("Workspace"));
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
