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

    using SelectInputComponent = Mycelium.Bloom.Components.UI.Atoms.SelectInput.SelectInput;

    /// <summary>
    /// Tests the <see cref="SelectInputComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class SelectInputTestFixture : BunitContext
    {
        /// <summary>
        /// The options used by select input tests.
        /// </summary>
        private static readonly IReadOnlyCollection<SelectInputOption> Options =
        [
            new() { Value = "first", Label = "First option" },
            new() { Value = "second", Label = "Second option", Disabled = true }
        ];

        /// <summary>
        /// Disposes the bUnit test context after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            this.Dispose();
        }

        /// <summary>
        /// Verifies that the placeholder and all configured options are rendered.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysPlaceholderAndOptions()
        {
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(component => component.Placeholder, "Choose an option")
                .Add(component => component.Options, Options)
                .Add(component => component.Value, "first"));

            var options = component.FindAll("option");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(options, Has.Count.EqualTo(3));
                Assert.That(options[0].TextContent.Trim(), Is.EqualTo("Choose an option"));
                Assert.That(options[1].TextContent.Trim(), Is.EqualTo("First option"));
                Assert.That(options[1].HasAttribute("selected"), Is.True);
                Assert.That(options[2].TextContent.Trim(), Is.EqualTo("Second option"));
                Assert.That(options[2].HasAttribute("disabled"), Is.True);
            }
        }

        /// <summary>
        /// Verifies that native selection changes use the value binding callback.
        /// </summary>
        [Test]
        public void VerifyChangeInvokesValueChanged()
        {
            var changedValue = string.Empty;

            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(component => component.Options, Options)
                .Add(component => component.ValueChanged, value => changedValue = value));

            component.Find("select").Change("first");

            Assert.That(changedValue, Is.EqualTo("first"));
        }

        /// <summary>
        /// Verifies that field state, help, and error metadata are rendered accessibly.
        /// </summary>
        [Test]
        public void VerifyRenderAppliesFieldState()
        {
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(component => component.Id, "selection")
                .Add(component => component.Label, "Selection")
                .Add(component => component.Placeholder, "Choose")
                .Add(component => component.Options, Options)
                .Add(component => component.HelpText, "Choose one option.")
                .Add(component => component.ErrorText, "Selection is required.")
                .Add(component => component.Disabled, true)
                .Add(component => component.Required, true));

            var select = component.Find("select");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("label").GetAttribute("for"), Is.EqualTo("selection"));
                Assert.That(select.HasAttribute("disabled"), Is.True);
                Assert.That(select.HasAttribute("required"), Is.True);
                Assert.That(select.GetAttribute("aria-invalid"), Is.EqualTo("true"));
                Assert.That(select.GetAttribute("aria-describedby"), Is.EqualTo("selection-help selection-error"));
                Assert.That(component.Find("#selection-help").TextContent, Is.EqualTo("Choose one option."));
                Assert.That(component.Find("#selection-error").TextContent, Is.EqualTo("Selection is required."));
                Assert.That(component.FindAll("option")[0].HasAttribute("disabled"), Is.True);
            }
        }
    }
}
