// ------------------------------------------------------------------------------------------------
// <copyright file="TextInputTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Atoms.TextInput
{
    using Bunit;

    using Mycelium.Bloom.Model.Enum;

    using TextInputComponent = Mycelium.Bloom.Components.UI.Atoms.TextInput.TextInput;

    /// <summary>
    /// Tests the <see cref="TextInputComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class TextInputTestFixture : BunitContext
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
        /// Verifies that text input changes update the value and invoke the callback.
        /// </summary>
        [Test]
        public void VerifyInputUpdatesValueAndInvokesCallback()
        {
            var changedValue = string.Empty;

            var component = this.Render<TextInputComponent>(parameters => parameters
                .Add(component => component.Value, "Initial")
                .Add(component => component.ValueChanged, value => changedValue = value));

            component.Find("input").Input("Updated");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(changedValue, Is.EqualTo("Updated"));
                Assert.That(component.Find("input").GetAttribute("value"), Is.EqualTo("Updated"));
            }
        }

        /// <summary>
        /// Verifies that configured input content, attributes, and state are rendered.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysConfiguredTextInput()
        {
            var component = this.Render<TextInputComponent>(parameters => parameters
                .Add(component => component.Id, "element-name")
                .Add(component => component.Name, "elementName")
                .Add(component => component.Label, "Element name")
                .Add(component => component.Value, "Requirement")
                .Add(component => component.Placeholder, "Enter a name")
                .Add(component => component.Type, "search")
                .Add(component => component.HelpText, "Use a short display name.")
                .Add(component => component.Required, true)
                .Add(component => component.ReadOnly, true)
                .Add(component => component.Size, TextInputSize.Small)
                .Add(component => component.StartContent, "<span>Start</span>")
                .Add(component => component.EndContent, "<span>End</span>")
                .Add(component => component.Class, "custom-text-input")
                .AddUnmatched("data-testid", "element-name-input"));

            var wrapper = component.Find(".mb-text-input");
            var input = component.Find("input");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(wrapper.GetAttribute("class"), Does.Contain("mb-text-input--small"));
                Assert.That(wrapper.GetAttribute("class"), Does.Contain("mb-text-input--readonly"));
                Assert.That(wrapper.GetAttribute("class"), Does.Contain("custom-text-input"));
                Assert.That(component.Find("label").GetAttribute("for"), Is.EqualTo("element-name"));
                Assert.That(input.GetAttribute("id"), Is.EqualTo("element-name"));
                Assert.That(input.GetAttribute("name"), Is.EqualTo("elementName"));
                Assert.That(input.GetAttribute("type"), Is.EqualTo("search"));
                Assert.That(input.GetAttribute("value"), Is.EqualTo("Requirement"));
                Assert.That(input.GetAttribute("placeholder"), Is.EqualTo("Enter a name"));
                Assert.That(input.GetAttribute("aria-describedby"), Is.EqualTo("element-name-help"));
                Assert.That(input.GetAttribute("data-testid"), Is.EqualTo("element-name-input"));
                Assert.That(input.HasAttribute("required"), Is.True);
                Assert.That(input.HasAttribute("readonly"), Is.True);
                Assert.That(component.Find(".mb-text-input__start").TextContent.Trim(), Is.EqualTo("Start"));
                Assert.That(component.Find(".mb-text-input__end").TextContent.Trim(), Is.EqualTo("End"));
                Assert.That(component.Find(".mb-text-input__help").TextContent.Trim(), Is.EqualTo("Use a short display name."));
            }
        }

        /// <summary>
        /// Verifies that error text sets error state and overrides help text.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysErrorState()
        {
            var component = this.Render<TextInputComponent>(parameters => parameters
                .Add(component => component.Id, "element-name")
                .Add(component => component.HelpText, "Help text")
                .Add(component => component.ErrorText, "Name is required.")
                .Add(component => component.Disabled, true)
                .Add(component => component.Size, TextInputSize.Large));

            var wrapper = component.Find(".mb-text-input");
            var input = component.Find("input");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(wrapper.GetAttribute("class"), Does.Contain("mb-text-input--large"));
                Assert.That(wrapper.GetAttribute("class"), Does.Contain("mb-text-input--disabled"));
                Assert.That(wrapper.GetAttribute("class"), Does.Contain("mb-text-input--error"));
                Assert.That(input.GetAttribute("aria-invalid"), Is.EqualTo("true"));
                Assert.That(input.GetAttribute("aria-describedby"), Is.EqualTo("element-name-error"));
                Assert.That(input.HasAttribute("disabled"), Is.True);
                Assert.That(component.Find(".mb-text-input__error").TextContent.Trim(), Is.EqualTo("Name is required."));
                Assert.That(component.FindAll(".mb-text-input__help"), Is.Empty);
            }
        }
    }
}
