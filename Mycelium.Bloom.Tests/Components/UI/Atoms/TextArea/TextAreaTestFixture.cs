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
        /// Verifies that the supplied value, row count, and maximum length are rendered.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysConfiguredTextArea()
        {
            var component = this.Render<TextAreaComponent>(parameters => parameters
                .Add(component => component.Id, "description")
                .Add(component => component.Label, "Description")
                .Add(component => component.Value, "Existing description")
                .Add(component => component.Placeholder, "Add a description")
                .Add(component => component.Rows, 5)
                .Add(component => component.MaxLength, 240));

            var textArea = component.Find("textarea");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("label").GetAttribute("for"), Is.EqualTo("description"));
                Assert.That(textArea.TextContent, Is.EqualTo("Existing description"));
                Assert.That(textArea.GetAttribute("placeholder"), Is.EqualTo("Add a description"));
                Assert.That(textArea.GetAttribute("rows"), Is.EqualTo("5"));
                Assert.That(textArea.GetAttribute("maxlength"), Is.EqualTo("240"));
            }
        }

        /// <summary>
        /// Verifies that the optional character counter is hidden by default.
        /// </summary>
        [Test]
        public void VerifyRenderHidesCharacterCounterByDefault()
        {
            var component = this.Render<TextAreaComponent>(parameters => parameters
                .Add(textArea => textArea.Value, "Review note")
                .Add(textArea => textArea.MaxLength, 120));

            Assert.That(component.FindAll(".mb-text-area__count"), Is.Empty);
        }

        /// <summary>
        /// Verifies that the character counter includes the configured maximum and accessible description.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysCharacterCounterWithMaximum()
        {
            var component = this.Render<TextAreaComponent>(parameters => parameters
                .Add(textArea => textArea.Id, "review-note")
                .Add(textArea => textArea.Value, "Review")
                .Add(textArea => textArea.HelpText, "Keep the note concise.")
                .Add(textArea => textArea.MaxLength, 120)
                .Add(textArea => textArea.ShowCharacterCount, true));

            var counter = component.Find("#review-note-count");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(counter.TextContent, Does.Match(@"^\s*6\s*/\s*120\s*$"));
                Assert.That(counter.GetAttribute("aria-label"), Is.EqualTo("6 of 120 characters"));
                Assert.That(component.Find("textarea").GetAttribute("aria-describedby"),
                    Is.EqualTo("review-note-help review-note-count"));
            }
        }

        /// <summary>
        /// Verifies that the counter derives from the controlled value and omits an unconfigured maximum.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysControlledCharacterCountWithoutMaximum()
        {
            var component = this.Render<TextAreaComponent>(parameters => parameters
                .Add(textArea => textArea.Id, "summary")
                .Add(textArea => textArea.Value, "Draft")
                .Add(textArea => textArea.ShowCharacterCount, true));

            component.Render(parameters => parameters
                .Add(textArea => textArea.Value, "Approved"));

            var counter = component.Find("#summary-count");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(counter.TextContent.Trim(), Is.EqualTo("8"));
                Assert.That(counter.GetAttribute("aria-label"), Is.EqualTo("8 characters"));
                Assert.That(component.Find("textarea").HasAttribute("maxlength"), Is.False);
                Assert.That(component.Find("textarea").GetAttribute("aria-describedby"), Is.EqualTo("summary-count"));
            }
        }

        /// <summary>
        /// Verifies that input updates use the value binding callback.
        /// </summary>
        [Test]
        public void VerifyInputInvokesValueChanged()
        {
            var changedValue = string.Empty;

            var component = this.Render<TextAreaComponent>(parameters => parameters
                .Add(component => component.ValueChanged, value => changedValue = value));

            component.Find("textarea").Input("Updated description");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(changedValue, Is.EqualTo("Updated description"));
                Assert.That(component.Find("textarea").TextContent, Is.EqualTo("Updated description"));
            }
        }

        /// <summary>
        /// Verifies that native field states and fixed resizing are applied.
        /// </summary>
        [Test]
        public void VerifyRenderAppliesNativeStates()
        {
            var component = this.Render<TextAreaComponent>(parameters => parameters
                .Add(component => component.Disabled, true)
                .Add(component => component.Required, true)
                .Add(component => component.ReadOnly, true)
                .Add(component => component.Resizable, false));

            var textArea = component.Find("textarea");
            var root = component.Find(".mb-text-area");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(textArea.HasAttribute("disabled"), Is.True);
                Assert.That(textArea.HasAttribute("required"), Is.True);
                Assert.That(textArea.HasAttribute("readonly"), Is.True);
                Assert.That(root.ClassList, Does.Contain("mb-text-area--disabled"));
                Assert.That(root.ClassList, Does.Contain("mb-text-area--readonly"));
                Assert.That(root.ClassList, Does.Contain("mb-text-area--fixed"));
            }
        }

        /// <summary>
        /// Verifies that help and error text are rendered and described accessibly.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysHelpAndErrorState()
        {
            var component = this.Render<TextAreaComponent>(parameters => parameters
                .Add(component => component.Id, "notes")
                .Add(component => component.HelpText, "Add useful context.")
                .Add(component => component.ErrorText, "The notes are too long."));

            var textArea = component.Find("textarea");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#notes-help").TextContent, Is.EqualTo("Add useful context."));
                Assert.That(component.Find("#notes-error").TextContent, Is.EqualTo("The notes are too long."));
                Assert.That(textArea.GetAttribute("aria-invalid"), Is.EqualTo("true"));
                Assert.That(textArea.GetAttribute("aria-describedby"), Is.EqualTo("notes-help notes-error"));
            }
        }
    }
}
