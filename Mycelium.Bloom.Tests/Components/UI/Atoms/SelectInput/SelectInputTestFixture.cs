// ------------------------------------------------------------------------------------------------
// <copyright file="SelectInputTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Atoms.SelectInput
{
    using System.Collections.Generic;

    using Bunit;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    using SelectInputComponent = Mycelium.Bloom.Components.UI.Atoms.SelectInput.SelectInput;

    /// <summary>
    /// Tests the <see cref="SelectInputComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class SelectInputTestFixture : BunitContext
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
        /// Verifies that configured select content, attributes, and selected state are rendered.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysConfiguredSelectInput()
        {
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(component => component.Id, "lifecycle")
                .Add(component => component.Name, "lifecycle")
                .Add(component => component.Label, "Lifecycle")
                .Add(component => component.Value, "active")
                .Add(component => component.Options, GetOptions())
                .Add(component => component.HelpText, "Select the current project state.")
                .Add(component => component.Required, true)
                .Add(component => component.Size, SelectInputSize.Large)
                .Add(component => component.StartContent, "<span>State</span>")
                .Add(component => component.Class, "custom-select")
                .AddUnmatched("data-testid", "lifecycle-select"));

            var wrapper = component.Find(".mb-select-input");
            var button = component.Find(".mb-select-input__button");

            button.Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(wrapper.GetAttribute("class"), Does.Contain("mb-select-input--large"));
                Assert.That(wrapper.GetAttribute("class"), Does.Contain("custom-select"));
                Assert.That(component.Find("label").GetAttribute("for"), Is.EqualTo("lifecycle"));
                Assert.That(button.GetAttribute("id"), Is.EqualTo("lifecycle"));
                Assert.That(button.GetAttribute("aria-expanded"), Is.EqualTo("true"));
                Assert.That(button.GetAttribute("aria-required"), Is.EqualTo("true"));
                Assert.That(button.GetAttribute("aria-describedby"), Is.EqualTo("lifecycle-help"));
                Assert.That(button.GetAttribute("data-testid"), Is.EqualTo("lifecycle-select"));
                Assert.That(component.Find(".mb-select-input__value").TextContent.Trim(), Is.EqualTo("Active"));
                Assert.That(component.Find(".mb-select-input__start").TextContent.Trim(), Is.EqualTo("State"));
                Assert.That(component.Find("input[type='hidden']").GetAttribute("value"), Is.EqualTo("active"));
                Assert.That(component.Find(".mb-select-input__help").TextContent.Trim(), Is.EqualTo("Select the current project state."));
                Assert.That(component.FindAll("[role='option']"), Has.Count.EqualTo(3));
                Assert.That(component.FindAll("[role='option']")[1].GetAttribute("aria-selected"), Is.EqualTo("true"));
                Assert.That(component.FindAll("[role='option']")[2].HasAttribute("disabled"), Is.True);
            }
        }

        /// <summary>
        /// Verifies that selecting an enabled option invokes the value change callback and closes the menu.
        /// </summary>
        [Test]
        public void VerifySelectOptionInvokesCallback()
        {
            var selectedValue = string.Empty;

            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(component => component.Options, GetOptions())
                .Add(component => component.Placeholder, "Choose lifecycle")
                .Add(component => component.ValueChanged, value => selectedValue = value));

            component.Find(".mb-select-input__button").Click();
            component.FindAll("[role='option']")[1].Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectedValue, Is.EqualTo("active"));
                Assert.That(component.Find(".mb-select-input__button").GetAttribute("aria-expanded"), Is.EqualTo("false"));
                Assert.That(component.Find(".mb-select-input__value").TextContent.Trim(), Is.EqualTo("Active"));
                Assert.That(component.FindAll(".mb-select-input__menu"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies that disabled select state prevents opening and renders errors.
        /// </summary>
        [Test]
        public void VerifyDisabledSelectDoesNotOpenAndDisplaysError()
        {
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(component => component.Id, "lifecycle")
                .Add(component => component.Options, GetOptions())
                .Add(component => component.Value, "unknown")
                .Add(component => component.HelpText, "Help text")
                .Add(component => component.ErrorText, "Lifecycle is required.")
                .Add(component => component.Disabled, true)
                .Add(component => component.Size, SelectInputSize.Small));

            var button = component.Find(".mb-select-input__button");

            button.Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-select-input").GetAttribute("class"), Does.Contain("mb-select-input--small"));
                Assert.That(component.Find(".mb-select-input").GetAttribute("class"), Does.Contain("mb-select-input--disabled"));
                Assert.That(component.Find(".mb-select-input").GetAttribute("class"), Does.Contain("mb-select-input--error"));
                Assert.That(button.GetAttribute("aria-expanded"), Is.EqualTo("false"));
                Assert.That(button.GetAttribute("aria-invalid"), Is.EqualTo("true"));
                Assert.That(button.GetAttribute("aria-describedby"), Is.EqualTo("lifecycle-error"));
                Assert.That(button.HasAttribute("disabled"), Is.True);
                Assert.That(component.Find(".mb-select-input__value").TextContent.Trim(), Is.EqualTo("unknown"));
                Assert.That(component.Find(".mb-select-input__error").TextContent.Trim(), Is.EqualTo("Lifecycle is required."));
                Assert.That(component.FindAll(".mb-select-input__menu"), Is.Empty);
                Assert.That(component.FindAll(".mb-select-input__help"), Is.Empty);
            }
        }

        /// <summary>
        /// Gets sample select options.
        /// </summary>
        /// <returns>The sample select options.</returns>
        private static IReadOnlyList<SelectInputOption> GetOptions()
        {
            return
            [
                new() { Value = "draft", Label = "Draft" },
                new() { Value = "active", Label = "Active" },
                new() { Value = "archived", Label = "Archived", Disabled = true }
            ];
        }
    }
}
