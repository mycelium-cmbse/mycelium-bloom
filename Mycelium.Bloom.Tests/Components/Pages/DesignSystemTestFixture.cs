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
    using System.Linq;

    using Bunit;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Pages;
    using Mycelium.Bloom.Tests.Common;

    /// <summary>
    /// Tests the <see cref="DesignSystem" /> development showcase page.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class DesignSystemTestFixture : BunitContext
    {
        /// <summary>
        /// The expected top-level showcase section order.
        /// </summary>
        private static readonly string[] ExpectedSectionOrder =
        [
            "foundation",
            "atoms",
            "molecules",
            "organisms",
            "workspace"
        ];

        /// <summary>
        /// The expected in-page showcase navigation targets.
        /// </summary>
        private static readonly string[] ExpectedSectionLinks =
        [
            "/design-system#foundation-heading",
            "/design-system#atoms-heading",
            "/design-system#molecules-heading",
            "/design-system#organisms-heading",
            "/design-system#workspace-heading"
        ];

        /// <summary>
        /// Configures the focused input and outside-click JavaScript helpers.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            var searchModule = this.JSInterop.SetupModule("./Components/UI/Atoms/SearchInput/SearchInput.razor.js");
            var registerHandler = searchModule.SetupVoid("registerSearchShortcut", invocation => true);
            var disposeHandler = searchModule.SetupVoid("disposeSearchShortcut", invocation => true);

            registerHandler.SetVoidResult();
            disposeHandler.SetVoidResult();

            JavaScriptInteropTestSetup.SetUpKeyboardDefaults(this.JSInterop);
            JavaScriptInteropTestSetup.SetUpOutsideClick(this.JSInterop);
        }

        /// <summary>
        /// Disposes the bUnit test context after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            this.Dispose();
        }

        /// <summary>
        /// Verifies that the routed page and representative components render without service registration.
        /// </summary>
        [Test]
        public void VerifyRenderRequiresNoBackendServices()
        {
            var component = this.Render<DesignSystem>();
            var sectionOrder = component
                .FindAll("main > section[data-section]")
                .Select(section => section.GetAttribute("data-section"))
                .ToArray();
            var sectionLinks = component
                .FindAll("nav[aria-label='Design system sections'] a")
                .Select(link => link.GetAttribute("href"))
                .ToArray();
            var route = typeof(DesignSystem)
                .GetCustomAttributes(typeof(RouteAttribute), false)
                .Cast<RouteAttribute>()
                .Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(route.Template, Is.EqualTo("/design-system"));
                Assert.That(component.FindAll("main.mb-design-system"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("[data-section='foundation']"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("[data-section='atoms']"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("[data-section='molecules']"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("[data-section='organisms']"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("[data-section='workspace']"), Has.Count.EqualTo(1));
                Assert.That(sectionOrder, Is.EqualTo(ExpectedSectionOrder));
                Assert.That(sectionLinks, Is.EqualTo(ExpectedSectionLinks));
                Assert.That(component.FindAll(".mb-avatar"), Is.Not.Empty);
                Assert.That(component.FindAll(".mb-tabs"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("#showcase-search-shortcut"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("#showcase-search-shortcut-secondary"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("[data-component='search-input'] kbd"), Has.Count.EqualTo(2));
                Assert.That(component.FindAll("#showcase-text-area-count"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("#showcase-toggle + .mb-toggle__track + .mb-toggle__state-text"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("[data-component='select-input'] [role='combobox']"), Has.Count.EqualTo(5));
                Assert.That(component.FindAll("#showcase-select-secondary"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("[data-component='toast-container']"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll(".mb-toast-container"), Is.Empty);
                Assert.That(component.FindAll("[data-component='app-header']"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("[data-component='workspace-shell']"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("[data-component='canvas-toolbar']"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("[data-component='zoom-controls']"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("[data-component='status-bar']"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("[data-testid='split-button-normal']"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("[data-testid='split-button-disabled'] button:disabled"), Has.Count.EqualTo(2));
                Assert.That(component.FindAll("[data-testid='split-button-loading'] .mb-button__spinner"), Has.Count.EqualTo(1));
            }
        }

        /// <summary>
        /// Verifies that workspace showcase controls update local zoom, action, search, and panel state.
        /// </summary>
        [Test]
        public void VerifyWorkspaceExamplesUpdateLocalState()
        {
            var component = this.Render<DesignSystem>();
            var zoomExample = component.Find("[data-component='zoom-controls']");

            zoomExample.QuerySelector("button[aria-label='Zoom in']").Click();
            component.Find("#workspace-header-search").Input("interfaces");
            component.Find("[data-component='canvas-toolbar'] button[aria-label='Move canvas']").Click();
            component.Find("[data-component='status-bar'] button[aria-label='Open status details']").Click();
            component.Find("#toggle-workspace-left-panel").Click();
            component.Find("#toggle-workspace-right-panel").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#zoom-controls-result").TextContent, Does.Contain("125%"));
                Assert.That(component.Find("#app-header-result").TextContent, Does.Contain("interfaces"));
                Assert.That(component.Find("#canvas-toolbar-result").TextContent, Does.Contain("Move"));
                Assert.That(component.Find("[data-component='canvas-toolbar'] button[aria-label='Move canvas']")
                    .GetAttribute("aria-pressed"), Is.EqualTo("true"));
                Assert.That(component.Find("[data-component='canvas-toolbar'] button[aria-label='Select element']")
                    .GetAttribute("aria-pressed"), Is.EqualTo("false"));
                Assert.That(component.Find("#status-bar-result").TextContent, Does.Contain("requested"));
                Assert.That(component.FindAll("[data-component='workspace-shell'] .mb-workspace-shell__left-panel"), Is.Empty);
                Assert.That(component.FindAll("[data-component='workspace-shell'] .mb-workspace-shell__right-panel"), Is.Empty);
                Assert.That(component.Find("#workspace-shell-result").TextContent, Does.Contain("Left panel: hidden"));
                Assert.That(component.Find("#workspace-shell-result").TextContent, Does.Contain("Right panel: hidden"));
                Assert.That(component.Find("[data-component='workspace-shell'] .mb-workspace-shell__main").TextContent,
                    Does.Contain("Thermal control"));
            }
        }

        /// <summary>
        /// Verifies reset and fit-to-view callbacks update the shared local zoom value.
        /// </summary>
        [Test]
        public void VerifyZoomExampleResetAndFitActions()
        {
            var component = this.Render<DesignSystem>();
            var zoomExample = component.Find("[data-component='zoom-controls']");

            zoomExample.QuerySelector("button[aria-label='Zoom in']").Click();
            zoomExample = component.Find("[data-component='zoom-controls']");
            zoomExample.QuerySelector("button[aria-label='Reset zoom']").Click();

            Assert.That(component.Find("#zoom-controls-result").TextContent, Does.Contain("reset to 100%"));

            zoomExample = component.Find("[data-component='zoom-controls']");
            zoomExample.QuerySelector("button[aria-label='Fit to view']").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#zoom-controls-result").TextContent, Does.Contain("75%"));
                Assert.That(component.Find("[data-component='zoom-controls'] output").TextContent.Trim(),
                    Is.EqualTo("75%"));
            }
        }

        /// <summary>
        /// Verifies that form controls update their page-owned display state.
        /// </summary>
        [Test]
        public void VerifyFormExamplesUpdateDisplayedState()
        {
            var component = this.Render<DesignSystem>();

            component.Find("#showcase-search-input").Input("interfaces");
            component.Find("#showcase-text-input").Input("Power subsystem");
            component.Find("#showcase-select-input").Click();
            component.FindAll("[data-component='select-input'] [role='option']")
                .Single(option => option.TextContent.Trim() == "Open")
                .Click();
            component.Find("#showcase-text-area").Input("Updated review note");
            component.Find("#showcase-checkbox").Change(false);
            component.Find("#showcase-toggle").Change(true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#search-input-result").TextContent, Does.Contain("interfaces"));
                Assert.That(component.Find("#text-input-result").TextContent, Does.Contain("Power subsystem"));
                Assert.That(component.Find("#select-input-result").TextContent, Does.Contain("open"));
                Assert.That(component.Find("#text-area-result").TextContent, Does.Contain("Updated review note"));
                Assert.That(component.Find("#showcase-text-area-count").TextContent, Does.Contain("19"));
                Assert.That(component.Find("#checkbox-result").TextContent, Does.Contain("hidden"));
                Assert.That(component.Find("#toggle-result").TextContent, Does.Contain("on"));
                Assert.That(component.Find(".mb-toggle__state-text").TextContent, Is.EqualTo("Active"));
            }
        }

        /// <summary>
        /// Verifies that the custom select examples open and update their parent-owned values independently.
        /// </summary>
        [Test]
        public void VerifySelectExamplesRemainIndependent()
        {
            var component = this.Render<DesignSystem>();

            component.Find("#showcase-select-secondary").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("#showcase-select-secondary + [role='listbox']"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("#showcase-select-input + [role='listbox']"), Is.Empty);
            }

            component.FindAll("[data-component='select-input'] [role='option']")
                .Single(option => option.TextContent.Contains("multiple engineering workspaces"))
                .Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#select-input-result").TextContent, Does.Contain("review / verification"));
                Assert.That(component.Find("#showcase-select-input").TextContent, Does.Contain("In review"));
                Assert.That(component.Find("#showcase-select-secondary").TextContent,
                    Does.Contain("Verification pending"));
            }
        }

        /// <summary>
        /// Verifies that the shortcut showcase can dispose and restore the newest registration target.
        /// </summary>
        [Test]
        public void VerifyShortcutExamplesExposeRegistrationLifecycle()
        {
            var component = this.Render<DesignSystem>();

            component.Find("#showcase-search-shortcut").Input("primary value");
            component.Find("#showcase-search-shortcut-secondary").Input("secondary value");
            component.Find("#toggle-secondary-shortcut-search").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#showcase-search-shortcut").GetAttribute("value"),
                    Is.EqualTo("primary value"));
                Assert.That(component.FindAll("#showcase-search-shortcut-secondary"), Is.Empty);
                Assert.That(component.Find("#search-shortcut-result").TextContent,
                    Does.Contain("primary (restored)"));
            }

            component.Find("#toggle-secondary-shortcut-search").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("#showcase-search-shortcut-secondary"), Has.Count.EqualTo(1));
                Assert.That(component.Find("#showcase-search-shortcut-secondary").GetAttribute("value"),
                    Is.EqualTo("secondary value"));
                Assert.That(component.Find("#search-shortcut-result").TextContent,
                    Does.Contain("secondary (newest)"));
            }
        }

        /// <summary>
        /// Verifies that tabs and breadcrumbs return their selected local values.
        /// </summary>
        [Test]
        public void VerifyNavigationExamplesUpdateDisplayedState()
        {
            var component = this.Render<DesignSystem>();

            component
                .FindAll("[role='tab']")
                .Single(tab => tab.TextContent.Contains("Properties"))
                .Click();
            component
                .FindAll("nav[aria-label='Showcase hierarchy'] button")
                .Single(button => button.TextContent.Contains("Projects"))
                .Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#tabs-result").TextContent, Does.Contain("properties"));
                Assert.That(component.Find("#breadcrumbs-result").TextContent, Does.Contain("Projects"));
            }
        }

        /// <summary>
        /// Verifies that independent action menus retain separate open state and return selections.
        /// </summary>
        [Test]
        public void VerifyActionMenuExamplesMaintainIndependentState()
        {
            var component = this.Render<DesignSystem>();
            var primaryMenu = component.Find("[data-testid='action-menu-primary']");
            var secondaryMenu = component.Find("[data-testid='action-menu-secondary']");

            primaryMenu.QuerySelector("button").Click();
            secondaryMenu.QuerySelector("button").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(primaryMenu.QuerySelectorAll("[role='menu']"), Has.Count.EqualTo(1));
                Assert.That(secondaryMenu.QuerySelectorAll("[role='menu']"), Has.Count.EqualTo(1));
                Assert.That(primaryMenu.QuerySelector("[role='menu']").ClassList,
                    Does.Contain("mb-action-menu__menu--start"));
                Assert.That(secondaryMenu.QuerySelector("[role='menu']").ClassList,
                    Does.Contain("mb-action-menu__menu--end"));
                Assert.That(primaryMenu.QuerySelectorAll(".mb-action-menu__item-icon"),
                    Has.Count.EqualTo(4));
                Assert.That(primaryMenu.QuerySelectorAll(".mb-action-menu__item-label")
                    .Any(label => label.TextContent.Contains("another architecture workspace")), Is.True);
                Assert.That(secondaryMenu.QuerySelectorAll(".mb-action-menu__trigger-content"), Has.Count.EqualTo(1));
                Assert.That(secondaryMenu.QuerySelectorAll(".mb-action-menu__chevron svg"), Has.Count.EqualTo(1));
            }

            component.Find("[data-testid='action-menu-primary'] [role='menuitem']").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#action-menu-result").TextContent, Does.Contain("Open details"));
                Assert.That(component.FindAll("[data-testid='action-menu-primary'] [role='menu']"), Is.Empty);
                Assert.That(component.FindAll("[data-testid='action-menu-secondary'] [role='menu']"), Has.Count.EqualTo(1));
            }
        }

        /// <summary>
        /// Verifies that split-button and user-menu actions update their page-owned result state.
        /// </summary>
        [Test]
        public void VerifyAdditionalMenuExamplesUpdateDisplayedState()
        {
            var component = this.Render<DesignSystem>();
            var splitButtonExample = component.Find("[data-component='split-button']");

            splitButtonExample.QuerySelectorAll("button")[0].Click();

            Assert.That(component.Find("#split-button-result").TextContent, Does.Contain("Save"));

            splitButtonExample = component.Find("[data-component='split-button']");
            splitButtonExample.QuerySelectorAll("button")[1].Click();
            component.Find("[data-component='split-button'] [role='menuitem']").Click();

            var userMenuExample = component.Find("[data-component='user-menu']");
            userMenuExample.QuerySelector("button").Click();
            component.Find("[data-component='user-menu'] [role='menuitem']").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#split-button-result").TextContent, Does.Contain("Save as draft"));
                Assert.That(component.Find("#user-menu-result").TextContent, Does.Contain("Profile"));
            }
        }

        /// <summary>
        /// Verifies that a local project selection updates the controlled switcher and result text.
        /// </summary>
        [Test]
        public void VerifyProjectSwitcherUpdatesSelectedProject()
        {
            var component = this.Render<DesignSystem>();
            var switcher = component.Find("[data-testid='project-switcher-primary']");

            switcher.QuerySelector("button").Click();

            var lunarProject = component
                .FindAll("[data-testid='project-switcher-primary'] [role='menuitemradio']")
                .Single(item => item.TextContent.Contains("Lunar Habitat"));

            lunarProject.Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("[data-testid='project-switcher-primary'] .mb-project-switcher__name").TextContent,
                    Is.EqualTo("Lunar Habitat"));
                Assert.That(component.Find("#project-switcher-result").TextContent, Does.Contain("Lunar Habitat"));
                Assert.That(component.Find("[data-testid='project-switcher-secondary'] .mb-project-switcher__name").TextContent,
                    Is.EqualTo("Lunar Habitat"));
            }

            var secondarySwitcher = component.Find("[data-testid='project-switcher-secondary']");
            secondarySwitcher.QuerySelector("button").Click();

            var orbitalProject = component
                .FindAll("[data-testid='project-switcher-secondary'] [role='menuitemradio']")
                .Single(item => item.TextContent.Contains("Orbital Platform"));

            orbitalProject.Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("[data-testid='project-switcher-primary'] .mb-project-switcher__name").TextContent,
                    Is.EqualTo("Lunar Habitat"));
                Assert.That(component.Find("[data-testid='project-switcher-secondary'] .mb-project-switcher__name").TextContent,
                    Is.EqualTo("Orbital Platform"));
                Assert.That(component.Find("#project-switcher-result").TextContent, Does.Contain("Orbital Platform"));
            }
        }

        /// <summary>
        /// Verifies that modal examples open with the requested size and close through the component callback.
        /// </summary>
        [Test]
        public void VerifyModalShellOpensAndCloses()
        {
            var component = this.Render<DesignSystem>();

            component.Find("#open-compact-modal").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("#showcase-modal"), Has.Count.EqualTo(1));
                Assert.That(component.Find("#showcase-modal").ClassList, Does.Contain("mb-modal__panel--small"));
            }

            component.Find("button[aria-label='Close dialog']").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("#showcase-modal"), Is.Empty);
                Assert.That(component.Find("#modal-result").TextContent, Does.Contain("Closed modal"));
            }

            component.Find("#open-wide-modal").Click();

            Assert.That(component.Find("#showcase-modal").ClassList, Does.Contain("mb-modal__panel--wide"));

            component
                .FindAll("#showcase-modal button")
                .Single(button => button.TextContent.Contains("Apply locally"))
                .Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("#showcase-modal"), Is.Empty);
                Assert.That(component.Find("#modal-result").TextContent, Does.Contain("Closed modal"));
            }
        }

        /// <summary>
        /// Verifies that confirmation and cancellation callbacks update the displayed result.
        /// </summary>
        [Test]
        public void VerifyConfirmDialogReturnsActions()
        {
            var component = this.Render<DesignSystem>();

            component.Find("#open-warning-confirm").Click();
            component.FindAll("[role='dialog'] button").Single(button => button.TextContent.Contains("Confirm action")).Click();

            Assert.That(component.Find("#confirm-result").TextContent, Does.Contain("Confirmed warning action"));

            component.Find("#open-danger-confirm").Click();
            component.FindAll("[role='dialog'] button").Single(button => button.TextContent.Contains("Cancel action")).Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#confirm-result").TextContent, Does.Contain("Cancelled danger action"));
                Assert.That(component.FindAll("[role='dialog']"), Is.Empty);
            }

            component.Find("#open-default-confirm").Click();
            component.FindAll("[role='dialog'] button").Single(button => button.TextContent.Contains("Confirm action")).Click();

            Assert.That(component.Find("#confirm-result").TextContent, Does.Contain("Confirmed default action"));
        }

        /// <summary>
        /// Verifies that a standalone notification can be dismissed from local page state.
        /// </summary>
        [Test]
        public void VerifyStandaloneNotificationCanBeDismissed()
        {
            var component = this.Render<DesignSystem>();

            component.Find("button[aria-label='Dismiss Information']").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("button[aria-label='Dismiss Information']"), Is.Empty);
                Assert.That(component.FindAll("[data-component='notification-toast'] .mb-notification-toast"),
                    Has.Count.EqualTo(3));
            }
        }

        /// <summary>
        /// Verifies that a toast-container notification can be dismissed from local page state.
        /// </summary>
        [Test]
        public void VerifyToastContainerNotificationCanBeDismissed()
        {
            var component = this.Render<DesignSystem>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#toast-count-result").TextContent, Does.Contain("0"));
                Assert.That(component.FindAll(".mb-toast-container__item"), Is.Empty);
            }

            component.Find("#add-toast-notification").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#toast-count-result").TextContent, Does.Contain("1"));
                Assert.That(component.Markup, Does.Contain("Sample notification 1"));
            }

            component.Find("#add-toast-notification").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#toast-count-result").TextContent, Does.Contain("2"));
                Assert.That(component.Markup, Does.Contain("Sample notification 2"));
            }

            component.Find("#reset-toast-notifications").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#toast-count-result").TextContent, Does.Contain("2"));
                Assert.That(component.FindAll(".mb-toast-container__item"), Has.Count.EqualTo(2));
            }

            component.Find("button[aria-label='Dismiss Model synchronized']").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Markup, Does.Not.Contain("Model synchronized"));
                Assert.That(component.Find("#toast-count-result").TextContent, Does.Contain("1"));
                Assert.That(component.FindAll(".mb-toast-container__item"), Has.Count.EqualTo(1));
            }
        }
    }
}
