// ------------------------------------------------------------------------------------------------
// <copyright file="TextAreaTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Atoms.TextArea
{
    using Bunit;

    using Mycelium.Bloom.Model.Enum;

    using TextAreaComponent = Mycelium.Bloom.Components.UI.Atoms.TextArea.TextArea;

    /// <summary>
    /// Tests the <see cref="TextAreaComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class TextAreaTestFixture : BunitContext
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
        /// Verifies that textarea input changes update the value and invoke the callback.
        /// </summary>
        [Test]
        public void VerifyInputUpdatesValueAndInvokesCallback()
        {
            var changedValue = string.Empty;

            var component = this.Render<TextAreaComponent>(parameters => parameters
                .Add(component => component.Value, "Initial note")
                .Add(component => component.ValueChanged, value => changedValue = value));

            component.Find("textarea").Input("Updated note");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(changedValue, Is.EqualTo("Updated note"));
                Assert.That(component.Find("textarea").GetAttribute("value"), Is.EqualTo("Updated note"));
            }
        }

        /// <summary>
        /// Verifies that configured textarea content, attributes, and footer are rendered.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysConfiguredTextArea()
        {
            var component = this.Render<TextAreaComponent>(parameters => parameters
                .Add(component => component.Id, "description")
                .Add(component => component.Name, "description")
                .Add(component => component.Label, "Description")
                .Add(component => component.Value, "Initial note")
                .Add(component => component.Placeholder, "Add details")
                .Add(component => component.HelpText, "Visible to collaborators.")
                .Add(component => component.Required, true)
                .Add(component => component.ReadOnly, true)
                .Add(component => component.Rows, 6)
                .Add(component => component.MaxLength, 120)
                .Add(component => component.ShowCharacterCount, true)
                .Add(component => component.Size, TextAreaSize.Small)
                .Add(component => component.Class, "custom-text-area")
                .AddUnmatched("data-testid", "description-input"));

            var wrapper = component.Find(".mb-text-area");
            var textarea = component.Find("textarea");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(wrapper.GetAttribute("class"), Does.Contain("mb-text-area--small"));
                Assert.That(wrapper.GetAttribute("class"), Does.Contain("mb-text-area--readonly"));
                Assert.That(wrapper.GetAttribute("class"), Does.Contain("custom-text-area"));
                Assert.That(component.Find("label").GetAttribute("for"), Is.EqualTo("description"));
                Assert.That(textarea.GetAttribute("id"), Is.EqualTo("description"));
                Assert.That(textarea.GetAttribute("name"), Is.EqualTo("description"));
                Assert.That(textarea.GetAttribute("value"), Is.EqualTo("Initial note"));
                Assert.That(textarea.GetAttribute("placeholder"), Is.EqualTo("Add details"));
                Assert.That(textarea.GetAttribute("rows"), Is.EqualTo("6"));
                Assert.That(textarea.GetAttribute("maxlength"), Is.EqualTo("120"));
                Assert.That(textarea.GetAttribute("aria-describedby"), Is.EqualTo("description-help description-count"));
                Assert.That(textarea.GetAttribute("data-testid"), Is.EqualTo("description-input"));
                Assert.That(textarea.HasAttribute("required"), Is.True);
                Assert.That(textarea.HasAttribute("readonly"), Is.True);
                Assert.That(component.Find(".mb-text-area__help").TextContent.Trim(), Is.EqualTo("Visible to collaborators."));
                Assert.That(component.Find(".mb-text-area__count").TextContent.Trim(), Is.EqualTo("12 / 120"));
            }
        }

        /// <summary>
        /// Verifies that textarea error state overrides help text and can render without a character count.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysErrorState()
        {
            var component = this.Render<TextAreaComponent>(parameters => parameters
                .Add(component => component.Id, "description")
                .Add(component => component.HelpText, "Help text")
                .Add(component => component.ErrorText, "Description is required.")
                .Add(component => component.Disabled, true)
                .Add(component => component.Size, TextAreaSize.Large));

            var wrapper = component.Find(".mb-text-area");
            var textarea = component.Find("textarea");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(wrapper.GetAttribute("class"), Does.Contain("mb-text-area--large"));
                Assert.That(wrapper.GetAttribute("class"), Does.Contain("mb-text-area--disabled"));
                Assert.That(wrapper.GetAttribute("class"), Does.Contain("mb-text-area--error"));
                Assert.That(textarea.GetAttribute("aria-invalid"), Is.EqualTo("true"));
                Assert.That(textarea.GetAttribute("aria-describedby"), Is.EqualTo("description-error"));
                Assert.That(textarea.HasAttribute("disabled"), Is.True);
                Assert.That(component.Find(".mb-text-area__error").TextContent.Trim(), Is.EqualTo("Description is required."));
                Assert.That(component.FindAll(".mb-text-area__help"), Is.Empty);
                Assert.That(component.FindAll(".mb-text-area__count"), Is.Empty);
            }
        }
    }
}
