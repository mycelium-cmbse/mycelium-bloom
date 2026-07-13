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
        /// Verifies checked and unchecked switch semantics.
        /// </summary>
        /// <param name="isChecked">The checked state to render.</param>
        /// <param name="ariaChecked">The expected accessible checked state.</param>
        [TestCase(true, "true")]
        [TestCase(false, "false")]
        public void VerifyRenderDisplaysCheckedState(bool isChecked, string ariaChecked)
        {
            var component = this.Render<ToggleComponent>(parameters => parameters
                .Add(component => component.Checked, isChecked));

            var input = component.Find("input");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(input.GetAttribute("type"), Is.EqualTo("checkbox"));
                Assert.That(input.GetAttribute("role"), Is.EqualTo("switch"));
                Assert.That(input.GetAttribute("aria-checked"), Is.EqualTo(ariaChecked));
                Assert.That(input.HasAttribute("checked"), Is.EqualTo(isChecked));
            }
        }

        /// <summary>
        /// Verifies that native changes use the checked binding callback.
        /// </summary>
        [Test]
        public void VerifyChangeInvokesCheckedChanged()
        {
            var changedValue = false;

            var component = this.Render<ToggleComponent>(parameters => parameters
                .Add(component => component.CheckedChanged, value => changedValue = value));

            component.Find("input").Change(true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(changedValue, Is.True);
                Assert.That(component.Find("input").GetAttribute("aria-checked"), Is.EqualTo("true"));
            }
        }

        /// <summary>
        /// Verifies that the native checkbox preserves keyboard-focusable switch behavior.
        /// </summary>
        [Test]
        public void VerifyRenderUsesKeyboardAccessibleNativeInput()
        {
            var component = this.Render<ToggleComponent>();
            var input = component.Find("input");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(input.GetAttribute("type"), Is.EqualTo("checkbox"));
                Assert.That(input.HasAttribute("tabindex"), Is.False);
                Assert.That(input.HasAttribute("disabled"), Is.False);
            }
        }

        /// <summary>
        /// Verifies label association, description metadata, and disabled state.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysConfiguredState()
        {
            var component = this.Render<ToggleComponent>(parameters => parameters
                .Add(component => component.Id, "auto-save")
                .Add(component => component.Name, "autoSave")
                .Add(component => component.Label, "Auto-save")
                .Add(component => component.Description, "Save changes automatically.")
                .Add(component => component.Disabled, true)
                .AddUnmatched("data-testid", "toggle"));

            var input = component.Find("input");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("label").GetAttribute("for"), Is.EqualTo("auto-save"));
                Assert.That(component.Find(".mb-toggle__label").TextContent, Is.EqualTo("Auto-save"));
                Assert.That(component.Find("#auto-save-description").TextContent, Is.EqualTo("Save changes automatically."));
                Assert.That(input.GetAttribute("name"), Is.EqualTo("autoSave"));
                Assert.That(input.GetAttribute("aria-describedby"), Is.EqualTo("auto-save-description"));
                Assert.That(input.GetAttribute("data-testid"), Is.EqualTo("toggle"));
                Assert.That(input.HasAttribute("disabled"), Is.True);
                Assert.That(component.Find(".mb-toggle").ClassList, Does.Contain("mb-toggle--disabled"));
            }
        }
    }
}
