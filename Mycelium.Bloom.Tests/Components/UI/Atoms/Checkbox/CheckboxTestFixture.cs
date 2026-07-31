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
    using System.Threading.Tasks;

    using Bunit;

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
        /// Verifies checked and unchecked native states.
        /// </summary>
        /// <param name="isChecked">The checked state to render.</param>
        [TestCase(true)]
        [TestCase(false)]
        public void VerifyRenderDisplaysCheckedState(bool isChecked)
        {
            var component = this.Render<CheckboxComponent>(parameters => parameters
                .Add(component => component.Checked, isChecked));

            Assert.That(component.Find("input").HasAttribute("checked"), Is.EqualTo(isChecked));
        }

        /// <summary>
        /// Verifies that native changes use the checked binding callback.
        /// </summary>
        [Test]
        public async Task VerifyChangeInvokesCheckedChanged()
        {
            var changedValue = false;

            var component = this.Render<CheckboxComponent>(parameters => parameters
                .Add(component => component.CheckedChanged, value => changedValue = value));

            await component.Find("input").ChangeAsync(true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(changedValue, Is.True);
                Assert.That(component.Find("input").HasAttribute("checked"), Is.True);
            }
        }

        /// <summary>
        /// Verifies that the label is associated with the input and description metadata is rendered.
        /// </summary>
        [Test]
        public void VerifyRenderAssociatesLabelAndDescription()
        {
            var component = this.Render<CheckboxComponent>(parameters => parameters
                .Add(component => component.Id, "notifications")
                .Add(component => component.Label, "Notifications")
                .Add(component => component.HelpText, "Receive project updates."));

            var input = component.Find("input");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("label").GetAttribute("for"), Is.EqualTo("notifications"));
                Assert.That(component.Find(".mb-checkbox__label").TextContent, Does.Contain("Notifications"));
                Assert.That(component.Find("#notifications-help").TextContent, Is.EqualTo("Receive project updates."));
                Assert.That(input.GetAttribute("aria-describedby"), Is.EqualTo("notifications-help"));
            }
        }

        /// <summary>
        /// Verifies that rich label and description content are rendered.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysRichContent()
        {
            var component = this.Render<CheckboxComponent>(parameters => parameters
                .Add(component => component.LabelContent, "<strong>Rich label</strong>")
                .Add(component => component.DescriptionContent, "<em>Rich description</em>"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-checkbox__label strong").TextContent, Is.EqualTo("Rich label"));
                Assert.That(component.Find(".mb-checkbox__description em").TextContent, Is.EqualTo("Rich description"));
            }
        }

        /// <summary>
        /// Verifies disabled, required, and invalid native states.
        /// </summary>
        [Test]
        public void VerifyRenderAppliesFieldState()
        {
            var component = this.Render<CheckboxComponent>(parameters => parameters
                .Add(component => component.Id, "agreement")
                .Add(component => component.Label, "Agreement")
                .Add(component => component.Disabled, true)
                .Add(component => component.Required, true)
                .Add(component => component.ErrorText, "Agreement is required."));

            var input = component.Find("input");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(input.HasAttribute("disabled"), Is.True);
                Assert.That(input.HasAttribute("required"), Is.True);
                Assert.That(input.GetAttribute("aria-invalid"), Is.EqualTo("true"));
                Assert.That(input.GetAttribute("aria-describedby"), Is.EqualTo("agreement-error"));
                Assert.That(component.Find(".mb-checkbox__required").TextContent, Is.EqualTo("*"));
                Assert.That(component.Find("#agreement-error").TextContent, Is.EqualTo("Agreement is required."));
            }
        }
    }
}
