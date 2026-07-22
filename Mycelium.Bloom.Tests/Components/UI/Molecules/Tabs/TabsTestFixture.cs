// ------------------------------------------------------------------------------------------------
// <copyright file="TabsTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Molecules.Tabs
{
    using Bunit;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Tests.Common;

    using TabsComponent = Mycelium.Bloom.Components.UI.Molecules.Tabs.Tabs;

    /// <summary>
    /// Tests the <see cref="TabsComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class TabsTestFixture : BunitContext
    {
        /// <summary>
        /// Configures the shared element-scoped keyboard-default module.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            JavaScriptInteropTestSetup.SetUpKeyboardDefaults(this.JSInterop);
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
        /// Verifies that clicking a disabled tab does not invoke the active value change callback.
        /// </summary>
        [Test]
        public void Click_DisabledTabDoesNotInvokeActiveValueChanged()
        {
            var activeValue = string.Empty;

            var component = this.Render<TabsComponent>(parameters => parameters
                .Add(component => component.Items, new[]
                {
                    new TabItem { Value = "overview", Label = "Overview" },
                    new TabItem { Value = "history", Label = "History", Disabled = true }
                })
                .Add(component => component.ActiveValue, "overview")
                .Add(component => component.ActiveValueChanged, value => activeValue = value));

            component.FindAll("[role='tab']")[1].Click();

            Assert.That(activeValue, Is.Empty);
        }

        /// <summary>
        /// Verifies that clicking an enabled tab invokes the active value change callback.
        /// </summary>
        [Test]
        public void Click_EnabledTabInvokesActiveValueChanged()
        {
            var activeValue = string.Empty;

            var component = this.Render<TabsComponent>(parameters => parameters
                .Add(component => component.Items, new[]
                {
                    new TabItem { Value = "overview", Label = "Overview" },
                    new TabItem { Value = "history", Label = "History" }
                })
                .Add(component => component.ActiveValue, "overview")
                .Add(component => component.ActiveValueChanged, value => activeValue = value));

            component.FindAll("[role='tab']")[1].Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(activeValue, Is.EqualTo("history"));
                Assert.That(component.FindAll("[role='tab']")[0].GetAttribute("aria-selected"), Is.EqualTo("true"));
            }

            component.Render(parameters => parameters
                .Add(tabComponent => tabComponent.ActiveValue, activeValue));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("[role='tab']")[1].GetAttribute("aria-selected"), Is.EqualTo("true"));
                Assert.That(component.FindAll("[role='tab']")[1].GetAttribute("tabindex"), Is.EqualTo("0"));
            }
        }

        /// <summary>
        /// Verifies that horizontal arrow navigation moves in both directions and wraps.
        /// </summary>
        [Test]
        public void VerifyKeyDownMovesWithHorizontalArrows()
        {
            this.JSInterop.Mode = JSRuntimeMode.Loose;
            var activeValue = string.Empty;
            var component = this.Render<TabsComponent>(parameters => parameters
                .Add(tabComponent => tabComponent.Items, new[]
                {
                    new TabItem { Value = "overview", Label = "Overview" },
                    new TabItem { Value = "properties", Label = "Properties" }
                })
                .Add(tabComponent => tabComponent.ActiveValue, "overview")
                .Add(tabComponent => tabComponent.ActiveValueChanged, value => activeValue = value));

            component.FindAll("[role='tab']")[0].KeyDown("ArrowRight");

            Assert.That(activeValue, Is.EqualTo("properties"));

            component.Render(parameters => parameters
                .Add(tabComponent => tabComponent.ActiveValue, activeValue));
            component.FindAll("[role='tab']")[1].KeyDown("ArrowLeft");

            Assert.That(activeValue, Is.EqualTo("overview"));
        }

        /// <summary>
        /// Verifies that arrow navigation skips disabled tabs.
        /// </summary>
        [Test]
        public void VerifyKeyDownSkipsDisabledTabs()
        {
            this.JSInterop.Mode = JSRuntimeMode.Loose;
            var activeValue = string.Empty;
            var component = this.Render<TabsComponent>(parameters => parameters
                .Add(tabComponent => tabComponent.Items, new[]
                {
                    new TabItem { Value = "overview", Label = "Overview" },
                    new TabItem { Value = "history", Label = "History", Disabled = true },
                    new TabItem { Value = "properties", Label = "Properties" }
                })
                .Add(tabComponent => tabComponent.ActiveValue, "overview")
                .Add(tabComponent => tabComponent.ActiveValueChanged, value => activeValue = value));

            component.FindAll("[role='tab']")[0].KeyDown("ArrowRight");

            Assert.That(activeValue, Is.EqualTo("properties"));
        }

        /// <summary>
        /// Verifies that Home and End select the first and last enabled tabs.
        /// </summary>
        [Test]
        public void VerifyKeyDownMovesWithHomeAndEnd()
        {
            this.JSInterop.Mode = JSRuntimeMode.Loose;
            var activeValue = string.Empty;
            var component = this.Render<TabsComponent>(parameters => parameters
                .Add(tabComponent => tabComponent.Items, new[]
                {
                    new TabItem { Value = "disabled-first", Label = "Disabled first", Disabled = true },
                    new TabItem { Value = "overview", Label = "Overview" },
                    new TabItem { Value = "properties", Label = "Properties" },
                    new TabItem { Value = "disabled-last", Label = "Disabled last", Disabled = true }
                })
                .Add(tabComponent => tabComponent.ActiveValue, "overview")
                .Add(tabComponent => tabComponent.ActiveValueChanged, value => activeValue = value));

            component.FindAll("[role='tab']")[1].KeyDown("End");

            Assert.That(activeValue, Is.EqualTo("properties"));

            component.Render(parameters => parameters
                .Add(tabComponent => tabComponent.ActiveValue, activeValue));
            component.FindAll("[role='tab']")[2].KeyDown("Home");

            Assert.That(activeValue, Is.EqualTo("overview"));
        }

        /// <summary>
        /// Verifies that tabs display configured items, classes, selection state, and attributes.
        /// </summary>
        [Test]
        public void Render_DisplaysConfiguredTabs()
        {
            var component = this.Render<TabsComponent>(parameters => parameters
                .Add(component => component.Items, new[]
                {
                    new TabItem { Value = "overview", Label = "Overview" },
                    new TabItem { Value = "history", Label = "History", Disabled = true }
                })
                .Add(component => component.ActiveValue, "overview")
                .Add(component => component.FullWidth, true)
                .Add(component => component.Class, "custom-tabs")
                .AddUnmatched("data-testid", "detail-tabs"));

            var tabList = component.Find("[role='tablist']");
            var tabs = component.FindAll("[role='tab']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tabList.GetAttribute("data-testid"), Is.EqualTo("detail-tabs"));
                Assert.That(tabList.GetAttribute("aria-orientation"), Is.EqualTo("horizontal"));
                Assert.That(tabList.GetAttribute("class"), Does.Contain("mb-tabs--full-width"));
                Assert.That(tabList.GetAttribute("class"), Does.Contain("custom-tabs"));
                Assert.That(tabs, Has.Count.EqualTo(2));
                Assert.That(tabs[0].TextContent.Trim(), Is.EqualTo("Overview"));
                Assert.That(tabs[0].GetAttribute("aria-selected"), Is.EqualTo("true"));
                Assert.That(tabs[0].GetAttribute("tabindex"), Is.EqualTo("0"));
                Assert.That(tabs[0].GetAttribute("class"), Does.Contain("mb-tabs__item--active"));
                Assert.That(tabs[1].TextContent.Trim(), Is.EqualTo("History"));
                Assert.That(tabs[1].GetAttribute("aria-selected"), Is.EqualTo("false"));
                Assert.That(tabs[1].GetAttribute("tabindex"), Is.EqualTo("-1"));
                Assert.That(tabs[1].HasAttribute("disabled"), Is.True);
                Assert.That(tabs[1].GetAttribute("class"), Does.Contain("mb-tabs__item--disabled"));
            }
        }
    }
}
