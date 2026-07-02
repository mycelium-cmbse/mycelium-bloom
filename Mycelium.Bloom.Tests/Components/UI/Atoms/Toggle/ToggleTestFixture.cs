// ------------------------------------------------------------------------------------------------
// <copyright file="ToggleTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Atoms.Toggle
{
    using Bunit;

    using Mycelium.Bloom.Model.Enum;

    using ToggleComponent = Mycelium.Bloom.Components.UI.Atoms.Toggle.Toggle;

    /// <summary>
    /// Tests the <see cref="ToggleComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ToggleTestFixture : BunitContext
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
        /// Verifies that toggle changes update the checked state and invoke the callback.
        /// </summary>
        [Test]
        public void VerifyChangeUpdatesCheckedState()
        {
            bool? changedValue = null;

            var component = this.Render<ToggleComponent>(parameters => parameters
                .Add(component => component.CheckedChanged, value => changedValue = value));

            component.Find("input").Change(true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(changedValue, Is.True);
                Assert.That(component.Find("input").HasAttribute("checked"), Is.True);
                Assert.That(component.Find("input").GetAttribute("aria-checked"), Is.EqualTo("true"));
            }
        }

        /// <summary>
        /// Verifies that configured toggle content, attributes, and checked state are rendered.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysConfiguredToggle()
        {
            var component = this.Render<ToggleComponent>(parameters => parameters
                .Add(component => component.Id, "live-sync")
                .Add(component => component.Name, "liveSync")
                .Add(component => component.Label, "Live sync")
                .Add(component => component.Description, "Share model changes automatically.")
                .Add(component => component.Checked, true)
                .Add(component => component.OnText, "On")
                .Add(component => component.OffText, "Off")
                .Add(component => component.Size, ToggleSize.Small)
                .Add(component => component.Class, "custom-toggle")
                .AddUnmatched("data-testid", "live-sync-toggle"));

            var wrapper = component.Find(".mb-toggle");
            var input = component.Find("input");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(wrapper.GetAttribute("class"), Does.Contain("mb-toggle--small"));
                Assert.That(wrapper.GetAttribute("class"), Does.Contain("mb-toggle--checked"));
                Assert.That(wrapper.GetAttribute("class"), Does.Contain("custom-toggle"));
                Assert.That(input.GetAttribute("id"), Is.EqualTo("live-sync"));
                Assert.That(input.GetAttribute("name"), Is.EqualTo("liveSync"));
                Assert.That(input.GetAttribute("role"), Is.EqualTo("switch"));
                Assert.That(input.GetAttribute("aria-checked"), Is.EqualTo("true"));
                Assert.That(input.GetAttribute("aria-describedby"), Is.EqualTo("live-sync-description"));
                Assert.That(input.GetAttribute("data-testid"), Is.EqualTo("live-sync-toggle"));
                Assert.That(component.Find(".mb-toggle__state-text").TextContent.Trim(), Is.EqualTo("On"));
                Assert.That(component.Find(".mb-toggle__label").TextContent.Trim(), Is.EqualTo("Live sync"));
                Assert.That(component.Find(".mb-toggle__description").TextContent.Trim(), Is.EqualTo("Share model changes automatically."));
            }
        }

        /// <summary>
        /// Verifies that disabled and unchecked state renders without optional content.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysDisabledUncheckedState()
        {
            var component = this.Render<ToggleComponent>(parameters => parameters
                .Add(component => component.Disabled, true)
                .Add(component => component.OffText, "Off"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-toggle").GetAttribute("class"), Does.Contain("mb-toggle--disabled"));
                Assert.That(component.Find(".mb-toggle").GetAttribute("class"), Does.Contain("mb-toggle--medium"));
                Assert.That(component.Find("input").GetAttribute("aria-checked"), Is.EqualTo("false"));
                Assert.That(component.Find("input").HasAttribute("disabled"), Is.True);
                Assert.That(component.Find(".mb-toggle__state-text").TextContent.Trim(), Is.EqualTo("Off"));
                Assert.That(component.FindAll(".mb-toggle__content"), Is.Empty);
            }
        }
    }
}
