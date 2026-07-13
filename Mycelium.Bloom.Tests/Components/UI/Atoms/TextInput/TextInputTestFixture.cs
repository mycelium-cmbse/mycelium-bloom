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
        /// Verifies that configured label, value, placeholder, and native attributes are rendered.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysConfiguredInput()
        {
            var component = this.Render<TextInputComponent>(parameters => parameters
                .Add(component => component.Id, "display-name")
                .Add(component => component.Name, "displayName")
                .Add(component => component.Label, "Display name")
                .Add(component => component.Value, "Bloom")
                .Add(component => component.Placeholder, "Enter a name")
                .Add(component => component.InputType, "email")
                .Add(component => component.Autocomplete, "email")
                .Add(component => component.MaxLength, 80)
                .AddUnmatched("data-testid", "text-input"));

            var input = component.Find("input");
            var label = component.Find("label");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(label.GetAttribute("for"), Is.EqualTo("display-name"));
                Assert.That(label.TextContent, Does.Contain("Display name"));
                Assert.That(input.GetAttribute("name"), Is.EqualTo("displayName"));
                Assert.That(input.GetAttribute("value"), Is.EqualTo("Bloom"));
                Assert.That(input.GetAttribute("placeholder"), Is.EqualTo("Enter a name"));
                Assert.That(input.GetAttribute("type"), Is.EqualTo("email"));
                Assert.That(input.GetAttribute("autocomplete"), Is.EqualTo("email"));
                Assert.That(input.GetAttribute("maxlength"), Is.EqualTo("80"));
                Assert.That(input.GetAttribute("data-testid"), Is.EqualTo("text-input"));
            }
        }

        /// <summary>
        /// Verifies that input updates use the value binding callback.
        /// </summary>
        [Test]
        public void VerifyInputInvokesValueChanged()
        {
            var changedValue = string.Empty;

            var component = this.Render<TextInputComponent>(parameters => parameters
                .Add(component => component.Value, "before")
                .Add(component => component.ValueChanged, value => changedValue = value));

            component.Find("input").Input("after");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(changedValue, Is.EqualTo("after"));
                Assert.That(component.Find("input").GetAttribute("value"), Is.EqualTo("after"));
            }
        }

        /// <summary>
        /// Verifies that help and error text are rendered and described accessibly.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysHelpAndErrorState()
        {
            var component = this.Render<TextInputComponent>(parameters => parameters
                .Add(component => component.Id, "project-name")
                .Add(component => component.HelpText, "Use a short name.")
                .Add(component => component.ErrorText, "A name is required."));

            var input = component.Find("input");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#project-name-help").TextContent, Is.EqualTo("Use a short name."));
                Assert.That(component.Find("#project-name-error").TextContent, Is.EqualTo("A name is required."));
                Assert.That(input.GetAttribute("aria-invalid"), Is.EqualTo("true"));
                Assert.That(input.GetAttribute("aria-describedby"), Is.EqualTo("project-name-help project-name-error"));
            }
        }

        /// <summary>
        /// Verifies that disabled, required, and read-only states use native attributes.
        /// </summary>
        [Test]
        public void VerifyRenderAppliesNativeStates()
        {
            var component = this.Render<TextInputComponent>(parameters => parameters
                .Add(component => component.Disabled, true)
                .Add(component => component.Required, true)
                .Add(component => component.ReadOnly, true));

            var input = component.Find("input");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(input.HasAttribute("disabled"), Is.True);
                Assert.That(input.HasAttribute("required"), Is.True);
                Assert.That(input.HasAttribute("readonly"), Is.True);
                Assert.That(component.Find(".mb-text-input").ClassList, Does.Contain("mb-text-input--disabled"));
                Assert.That(component.Find(".mb-text-input").ClassList, Does.Contain("mb-text-input--readonly"));
            }
        }

        /// <summary>
        /// Verifies that optional leading and trailing content are rendered.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysLeadingAndTrailingContent()
        {
            var component = this.Render<TextInputComponent>(parameters => parameters
                .Add(component => component.LeadingContent, "<span>Leading</span>")
                .Add(component => component.TrailingContent, "<button type=\"button\">Trailing</button>"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-text-input__leading").TextContent, Is.EqualTo("Leading"));
                Assert.That(component.Find(".mb-text-input__trailing").TextContent, Is.EqualTo("Trailing"));
            }
        }
    }
}
