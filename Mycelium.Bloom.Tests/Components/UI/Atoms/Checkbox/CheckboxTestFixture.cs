// ------------------------------------------------------------------------------------------------
// <copyright file="CheckboxTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Atoms.Checkbox
{
    using Bunit;

    using Mycelium.Bloom.Model.Enum;

    using CheckboxComponent = Mycelium.Bloom.Components.UI.Atoms.Checkbox.Checkbox;

    /// <summary>
    /// Tests the <see cref="CheckboxComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class CheckboxTestFixture : BunitContext
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
        /// Verifies that checkbox changes update the checked state and invoke the callback.
        /// </summary>
        [Test]
        public void VerifyChangeUpdatesCheckedState()
        {
            bool? changedValue = null;

            var component = this.Render<CheckboxComponent>(parameters => parameters
                .Add(component => component.CheckedChanged, value => changedValue = value));

            component.Find("input").Change(true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(changedValue, Is.True);
                Assert.That(component.Find("input").HasAttribute("checked"), Is.True);
            }
        }

        /// <summary>
        /// Verifies that configured checkbox content, attributes, and checked state are rendered.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysConfiguredCheckbox()
        {
            var component = this.Render<CheckboxComponent>(parameters => parameters
                .Add(component => component.Id, "branch-protection")
                .Add(component => component.Name, "branchProtection")
                .Add(component => component.Label, "Require approval")
                .Add(component => component.Description, "Protect changes before merge.")
                .Add(component => component.Checked, true)
                .Add(component => component.Required, true)
                .Add(component => component.Size, CheckboxSize.Small)
                .Add(component => component.Class, "custom-checkbox")
                .AddUnmatched("data-testid", "approval-checkbox"));

            var wrapper = component.Find(".mb-checkbox");
            var input = component.Find("input");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(wrapper.GetAttribute("class"), Does.Contain("mb-checkbox--small"));
                Assert.That(wrapper.GetAttribute("class"), Does.Contain("mb-checkbox--checked"));
                Assert.That(wrapper.GetAttribute("class"), Does.Contain("custom-checkbox"));
                Assert.That(input.GetAttribute("id"), Is.EqualTo("branch-protection"));
                Assert.That(input.GetAttribute("name"), Is.EqualTo("branchProtection"));
                Assert.That(input.GetAttribute("aria-describedby"), Is.EqualTo("branch-protection-description"));
                Assert.That(input.GetAttribute("data-testid"), Is.EqualTo("approval-checkbox"));
                Assert.That(input.HasAttribute("required"), Is.True);
                Assert.That(component.Find(".mb-checkbox__label").TextContent.Trim(), Is.EqualTo("Require approval*"));
                Assert.That(component.Find(".mb-checkbox__description").TextContent.Trim(), Is.EqualTo("Protect changes before merge."));
            }
        }

        /// <summary>
        /// Verifies that disabled checkbox state renders without optional content.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysDisabledState()
        {
            var component = this.Render<CheckboxComponent>(parameters => parameters
                .Add(component => component.Disabled, true));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-checkbox").GetAttribute("class"), Does.Contain("mb-checkbox--medium"));
                Assert.That(component.Find(".mb-checkbox").GetAttribute("class"), Does.Contain("mb-checkbox--disabled"));
                Assert.That(component.Find("input").HasAttribute("disabled"), Is.True);
                Assert.That(component.FindAll(".mb-checkbox__content"), Is.Empty);
            }
        }
    }
}
