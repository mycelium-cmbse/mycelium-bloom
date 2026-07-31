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
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using BlazorBlueprint.Primitives.Services;

    using Bunit;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Tests.Common;

    using SelectInputComponent = Mycelium.Bloom.Components.UI.Atoms.SelectInput.SelectInput;

    /// <summary>
    /// Tests Bloom's public field and value mapping onto the Blueprint select primitive.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class SelectInputTestFixture : BunitContext
    {
        private static readonly IReadOnlyCollection<SelectInputOption> Options =
        [
            new() { Value = "first", Label = "First option" },
            new() { Value = "second", Label = "Disabled second option", Disabled = true },
            new() { Value = "third", Label = "Third option" },
            new() { Value = "fourth", Label = "Fourth option with a predictably long engineering label" }
        ];

        private readonly IRenderedComponent<BbPortalHost> portalHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectInputTestFixture" /> class.
        /// </summary>
        public SelectInputTestFixture()
        {
            this.portalHost = BlueprintTestSetup.ConfigureWithPortalHost(this);
        }

        /// <summary>
        /// Disposes the bUnit test context after each test.
        /// </summary>
        [TearDown]
        public System.Threading.Tasks.Task TearDown()
        {
            return this.DisposeAsync().AsTask();
        }

        /// <summary>
        /// Verifies the controlled selection and portalled listbox semantics.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysCurrentSelectionAndOptions()
        {
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(select => select.Id, "selection")
                .Add(select => select.Options, Options)
                .Add(select => select.Value, "third")
                .Add(select => select.DefaultOpen, true));

            var trigger = component.Find("[role='combobox']");
            var listbox = this.portalHost.WaitForElement("[role='listbox']");
            var options = this.portalHost.FindAll("[role='option']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(trigger.TextContent, Does.Contain("Third option"));
                Assert.That(trigger.GetAttribute("aria-haspopup"), Is.EqualTo("listbox"));
                Assert.That(trigger.GetAttribute("aria-expanded"), Is.EqualTo("true"));
                Assert.That(listbox.Id, Is.EqualTo(trigger.GetAttribute("aria-controls")));
                Assert.That(options, Has.Count.EqualTo(4));
                Assert.That(options[2].GetAttribute("aria-selected"), Is.EqualTo("true"));
                Assert.That(options[1].GetAttribute("aria-disabled"), Is.EqualTo("true"));
            }
        }

        /// <summary>
        /// Verifies the placeholder remains presentation text rather than a selectable option.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysNonSelectablePlaceholder()
        {
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(select => select.Placeholder, "Choose an option")
                .Add(select => select.Options, Options)
                .Add(select => select.DefaultOpen, true));

            var options = this.portalHost.WaitForElements("[role='option']", Options.Count);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("[role='combobox']").TextContent, Does.Contain("Choose an option"));
                Assert.That(component.Find(".mb-select-input__value").ClassList,
                    Does.Contain("mb-select-input__value--placeholder"));
                Assert.That(options.Any(option => option.TextContent.Contains("Choose an option", StringComparison.Ordinal)), Is.False);
            }
        }

        /// <summary>
        /// Verifies selection is reported while the wrapper parameter remains parent-owned.
        /// </summary>
        [Test]
        public void VerifySelectionReportsControlledValue()
        {
            var changedValue = string.Empty;
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(select => select.Options, Options)
                .Add(select => select.Value, "first")
                .Add(select => select.DefaultOpen, true)
                .Add(select => select.ValueChanged, value => changedValue = value));

            this.portalHost.WaitForElements("[role='option']", Options.Count)[2].Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(changedValue, Is.EqualTo("third"));
                Assert.That(component.Instance.Value, Is.EqualTo("first"));
                Assert.That(component.Find("[role='combobox']").GetAttribute("aria-expanded"), Is.EqualTo("false"));
            }

            var triggerId = component.Find("[role='combobox']").Id;
            var controlsId = component.Find("[role='combobox']").GetAttribute("aria-controls");

            component.Render(parameters => parameters
                .Add(select => select.Options, Options)
                .Add(select => select.Value, changedValue));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("[role='combobox']").TextContent, Does.Contain("Third option"));
                Assert.That(component.Find("[role='combobox']").Id, Is.EqualTo(triggerId));
                Assert.That(component.Find("[role='combobox']").GetAttribute("aria-controls"), Is.EqualTo(controlsId));
            }
        }

        /// <summary>
        /// Verifies disabled options and disabled controls cannot report a new value.
        /// </summary>
        [Test]
        public void VerifyDisabledStatesPreventSelection()
        {
            var selectionCount = 0;
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(select => select.Options, Options)
                .Add(select => select.DefaultOpen, true)
                .Add(select => select.ValueChanged, _ => selectionCount++));

            this.portalHost.WaitForElements("[role='option']", Options.Count)[1].Click();

            Assert.That(selectionCount, Is.Zero);

            component.Render(parameters => parameters
                .Add(select => select.Options, Options)
                .Add(select => select.Disabled, true));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("[role='combobox']").HasAttribute("disabled"), Is.True);
                Assert.That(component.Find("[role='combobox']").GetAttribute("aria-expanded"), Is.EqualTo("false"));
            }
        }

        /// <summary>
        /// Verifies labels, descriptions, required state, and errors remain associated with the combobox.
        /// </summary>
        [Test]
        public void VerifyAccessibleFieldRelationships()
        {
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(select => select.Id, "review-state")
                .Add(select => select.Label, "Review state")
                .Add(select => select.HelpText, "Choose the current lifecycle state.")
                .Add(select => select.ErrorText, "A lifecycle state is required.")
                .Add(select => select.Required, true)
                .Add(select => select.Options, Options));

            var trigger = component.Find("[role='combobox']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("label").GetAttribute("for"), Is.EqualTo("review-state"));
                Assert.That(trigger.GetAttribute("aria-required"), Is.EqualTo("true"));
                Assert.That(trigger.GetAttribute("aria-invalid"), Is.EqualTo("true"));
                Assert.That(trigger.GetAttribute("aria-describedby"), Does.Contain("review-state-help"));
                Assert.That(trigger.GetAttribute("aria-describedby"), Does.Contain("review-state-error"));
                Assert.That(component.Find("#review-state-error").TextContent, Is.EqualTo("A lifecycle state is required."));
            }
        }

        /// <summary>
        /// Verifies enabled named selects submit the current controlled value without claiming native validation.
        /// </summary>
        [Test]
        public void VerifyNamedSelectRendersSubmissionProxy()
        {
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(select => select.Id, "project-state")
                .Add(select => select.Name, "state")
                .Add(select => select.Options, Options)
                .Add(select => select.Value, "third")
                .Add(select => select.Required, true));

            var formValue = component.Find("input[type='hidden']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(formValue.Id, Is.EqualTo("project-state-form-value"));
                Assert.That(formValue.GetAttribute("name"), Is.EqualTo("state"));
                Assert.That(formValue.GetAttribute("value"), Is.EqualTo("third"));
                Assert.That(formValue.HasAttribute("required"), Is.False);
            }
        }

        /// <summary>
        /// Verifies a disabled select cannot submit a stale controlled value.
        /// </summary>
        [Test]
        public void VerifyDisabledSelectOmitsSubmissionProxy()
        {
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(select => select.Name, "state")
                .Add(select => select.Options, Options)
                .Add(select => select.Value, "third")
                .Add(select => select.Disabled, true));

            Assert.That(component.FindAll("input[name='state']"), Is.Empty);
        }

        /// <summary>
        /// Verifies generated control relationships are stable and independent.
        /// </summary>
        [Test]
        public void VerifyGeneratedIdentifiersAreStableAndIndependent()
        {
            var first = this.Render<SelectInputComponent>(parameters => parameters
                .Add(select => select.Options, Options)
                .Add(select => select.Name, "first"));
            var second = this.Render<SelectInputComponent>(parameters => parameters
                .Add(select => select.Options, Options)
                .Add(select => select.Name, "second"));
            var firstId = first.Find("[role='combobox']").Id;
            var firstControls = first.Find("[role='combobox']").GetAttribute("aria-controls");

            first.Render(parameters => parameters
                .Add(select => select.Options, Options)
                .Add(select => select.Name, "first")
                .Add(select => select.Value, "third"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.Find("[role='combobox']").Id, Is.EqualTo(firstId));
                Assert.That(first.Find("[role='combobox']").GetAttribute("aria-controls"), Is.EqualTo(firstControls));
                Assert.That(second.Find("[role='combobox']").Id, Is.Not.EqualTo(firstId));
                Assert.That(first.Find("input[name='first']").Id, Is.EqualTo($"{firstId}-form-value"));
            }
        }
    }
}
