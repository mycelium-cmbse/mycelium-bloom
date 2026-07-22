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
    using System.Linq;
    using System.Threading.Tasks;

    using Bunit;

    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Tests.Common;

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
            new() { Value = "second", Label = "Disabled second option", Disabled = true },
            new() { Value = "third", Label = "Third option" },
            new() { Value = "fourth", Label = "Fourth option with a predictably long engineering label" }
        ];

        /// <summary>
        /// Configures the element-scoped keyboard and outside-click helpers used by SelectInput.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            JavaScriptInteropTestSetup.SetUpKeyboardDefaults(this.JSInterop);
            JavaScriptInteropTestSetup.SetUpOutsideClick(this.JSInterop);
        }

        /// <summary>
        /// Disposes the bUnit test context after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            this.Dispose();
        }

        /// <summary>
        /// Verifies the current controlled selection and custom listbox semantics.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysCurrentSelectionAndListboxOptions()
        {
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(selectInput => selectInput.Id, "selection")
                .Add(selectInput => selectInput.Options, Options)
                .Add(selectInput => selectInput.Value, "third"));

            var trigger = component.Find("[role='combobox']");
            trigger.Click();

            var listbox = component.Find("[role='listbox']");
            var options = component.FindAll("[role='option']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(trigger.TextContent, Does.Contain("Third option"));
                Assert.That(trigger.GetAttribute("aria-haspopup"), Is.EqualTo("listbox"));
                Assert.That(trigger.GetAttribute("aria-expanded"), Is.EqualTo("true"));
                Assert.That(listbox.Id, Is.EqualTo(trigger.GetAttribute("aria-controls")));
                Assert.That(options, Has.Count.EqualTo(4));
                Assert.That(options[2].GetAttribute("aria-selected"), Is.EqualTo("true"));
                Assert.That(options[1].GetAttribute("aria-disabled"), Is.EqualTo("true"));
                Assert.That(options[1].HasAttribute("disabled"), Is.True);
            }
        }

        /// <summary>
        /// Verifies that the placeholder is presentation only and not a listbox option.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysNonSelectablePlaceholder()
        {
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(selectInput => selectInput.Placeholder, "Choose an option")
                .Add(selectInput => selectInput.Options, Options));

            var trigger = component.Find("[role='combobox']");
            trigger.Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(trigger.TextContent, Does.Contain("Choose an option"));
                Assert.That(component.Find(".mb-select-input__value").ClassList,
                    Does.Contain("mb-select-input__value--placeholder"));
                Assert.That(component.FindAll("[role='option']"), Has.Count.EqualTo(Options.Count));
                Assert.That(component.FindAll("[role='option']")
                    .Any(option => option.TextContent.Contains("Choose an option")), Is.False);
            }
        }

        /// <summary>
        /// Verifies pointer activation toggles the popup for an enabled select.
        /// </summary>
        [Test]
        public void VerifyTriggerOpensAndClosesListbox()
        {
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(selectInput => selectInput.Options, Options));

            var trigger = component.Find("[role='combobox']");
            trigger.Click();

            Assert.That(component.FindAll("[role='listbox']"), Has.Count.EqualTo(1));

            trigger.Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("[role='listbox']"), Is.Empty);
                Assert.That(trigger.GetAttribute("aria-expanded"), Is.EqualTo("false"));
            }
        }

        /// <summary>
        /// Verifies that an outside pointer callback closes only the open listbox.
        /// </summary>
        [Test]
        public async Task VerifyOutsideClickDismissesOpenListbox()
        {
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(selectInput => selectInput.Options, Options));

            component.Find("[role='combobox']").Click();

            await component.InvokeAsync(component.Instance.DismissFromOutsideClickAsync);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("[role='combobox']").GetAttribute("aria-expanded"), Is.EqualTo("false"));
                Assert.That(component.FindAll("[role='listbox']"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies selection requests remain controlled until the parent supplies a new value.
        /// </summary>
        [Test]
        public void VerifySelectionCallbackDoesNotMutateControlledValue()
        {
            var changedValue = string.Empty;
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(selectInput => selectInput.Options, Options)
                .Add(selectInput => selectInput.Value, "first")
                .Add(selectInput => selectInput.ValueChanged, value => changedValue = value));

            component.Find("[role='combobox']").Click();
            component.FindAll("[role='option']")[2].Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(changedValue, Is.EqualTo("third"));
                Assert.That(component.Find("[role='combobox']").TextContent, Does.Contain("First option"));
                Assert.That(component.FindAll("[role='listbox']"), Is.Empty);
            }

            component.Render(parameters => parameters
                .Add(selectInput => selectInput.Options, Options)
                .Add(selectInput => selectInput.Value, changedValue));

            Assert.That(component.Find("[role='combobox']").TextContent, Does.Contain("Third option"));
        }

        /// <summary>
        /// Verifies disabled options and a disabled select cannot request selection.
        /// </summary>
        [Test]
        public void VerifyDisabledStatesPreventInteraction()
        {
            var selectionCount = 0;
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(selectInput => selectInput.Options, Options)
                .Add(selectInput => selectInput.ValueChanged, _ => selectionCount++));

            component.Find("[role='combobox']").Click();
            component.FindAll("[role='option']")[1].Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectionCount, Is.Zero);
                Assert.That(component.FindAll("[role='listbox']"), Has.Count.EqualTo(1));
            }

            component.Render(parameters => parameters
                .Add(selectInput => selectInput.Options, Options)
                .Add(selectInput => selectInput.Disabled, true));

            component.Find("[role='combobox']").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("[role='combobox']").HasAttribute("disabled"), Is.True);
                Assert.That(component.FindAll("[role='listbox']"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies arrow navigation skips disabled options and Home and End reach enabled boundaries.
        /// </summary>
        [Test]
        public void VerifyKeyboardNavigationMovesActiveOption()
        {
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(selectInput => selectInput.Options, Options)
                .Add(selectInput => selectInput.Value, "first"));

            var trigger = component.Find("[role='combobox']");
            trigger.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

            Assert.That(trigger.GetAttribute("aria-activedescendant"), Does.EndWith("-option-0"));

            trigger.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
            Assert.That(trigger.GetAttribute("aria-activedescendant"), Does.EndWith("-option-2"));

            trigger.KeyDown(new KeyboardEventArgs { Key = "End" });
            Assert.That(trigger.GetAttribute("aria-activedescendant"), Does.EndWith("-option-3"));

            trigger.KeyDown(new KeyboardEventArgs { Key = "Home" });
            Assert.That(trigger.GetAttribute("aria-activedescendant"), Does.EndWith("-option-0"));

            trigger.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
            Assert.That(trigger.GetAttribute("aria-activedescendant"), Does.EndWith("-option-0"));
        }

        /// <summary>
        /// Verifies Arrow Up opens at the last appropriate enabled option.
        /// </summary>
        [Test]
        public void VerifyArrowUpOpensAtLastEnabledOption()
        {
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(selectInput => selectInput.Options, Options));

            var trigger = component.Find("[role='combobox']");
            trigger.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });

            Assert.That(trigger.GetAttribute("aria-activedescendant"), Does.EndWith("-option-3"));
        }

        /// <summary>
        /// Verifies Enter and Space select the active enabled option.
        /// </summary>
        /// <param name="key">The activation key.</param>
        [TestCase("Enter")]
        [TestCase(" ")]
        public void VerifyKeyboardActivationSelectsActiveOption(string key)
        {
            var changedValue = string.Empty;
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(selectInput => selectInput.Options, Options)
                .Add(selectInput => selectInput.ValueChanged, value => changedValue = value));

            var trigger = component.Find("[role='combobox']");
            trigger.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
            trigger.KeyDown(new KeyboardEventArgs { Key = key });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(changedValue, Is.EqualTo("first"));
                Assert.That(component.FindAll("[role='listbox']"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies Escape and Tab close without changing the controlled value.
        /// </summary>
        /// <param name="key">The closing key.</param>
        [TestCase("Escape")]
        [TestCase("Tab")]
        public void VerifyKeyboardDismissalClosesWithoutSelection(string key)
        {
            var selectionCount = 0;
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(selectInput => selectInput.Options, Options)
                .Add(selectInput => selectInput.Value, "first")
                .Add(selectInput => selectInput.ValueChanged, _ => selectionCount++));

            var trigger = component.Find("[role='combobox']");
            trigger.Click();
            trigger.KeyDown(new KeyboardEventArgs { Key = key });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("[role='listbox']"), Is.Empty);
                Assert.That(selectionCount, Is.Zero);
                Assert.That(trigger.TextContent, Does.Contain("First option"));
            }
        }

        /// <summary>
        /// Verifies label, help, error, required, validation, and form-name relationships.
        /// </summary>
        [Test]
        public void VerifyRenderAppliesAccessibleFieldMetadata()
        {
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(selectInput => selectInput.Id, "selection")
                .Add(selectInput => selectInput.Name, "selection-value")
                .Add(selectInput => selectInput.Label, "Selection")
                .Add(selectInput => selectInput.Options, Options)
                .Add(selectInput => selectInput.Value, "first")
                .Add(selectInput => selectInput.HelpText, "Choose one option.")
                .Add(selectInput => selectInput.ErrorText, "Selection is required.")
                .Add(selectInput => selectInput.Required, true));

            var trigger = component.Find("[role='combobox']");
            var hiddenInput = component.Find("input[type='hidden']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("label").GetAttribute("for"), Is.EqualTo("selection"));
                Assert.That(trigger.GetAttribute("aria-required"), Is.EqualTo("true"));
                Assert.That(trigger.GetAttribute("aria-invalid"), Is.EqualTo("true"));
                Assert.That(trigger.GetAttribute("aria-describedby"), Is.EqualTo("selection-help selection-error"));
                Assert.That(component.Find("#selection-help").TextContent, Is.EqualTo("Choose one option."));
                Assert.That(component.Find("#selection-error").TextContent, Is.EqualTo("Selection is required."));
                Assert.That(hiddenInput.Id, Is.EqualTo("selection-form-value"));
                Assert.That(hiddenInput.GetAttribute("name"), Is.EqualTo("selection-value"));
                Assert.That(hiddenInput.GetAttribute("value"), Is.EqualTo("first"));
                Assert.That(hiddenInput.HasAttribute("required"), Is.False);
            }
        }

        /// <summary>
        /// Verifies an enabled named component renders the current controlled value as a successful form proxy.
        /// </summary>
        [Test]
        public void VerifyEnabledFormValueRendersSubmissionProxy()
        {
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(selectInput => selectInput.Id, "project-state")
                .Add(selectInput => selectInput.Name, "state")
                .Add(selectInput => selectInput.Options, Options)
                .Add(selectInput => selectInput.Value, "third"));

            var formValue = component.Find("input[type='hidden']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(formValue.Id, Is.EqualTo("project-state-form-value"));
                Assert.That(formValue.GetAttribute("name"), Is.EqualTo("state"));
                Assert.That(formValue.GetAttribute("value"), Is.EqualTo("third"));
                Assert.That(formValue.HasAttribute("disabled"), Is.False);
            }
        }

        /// <summary>
        /// Verifies a disabled component omits its form proxy and cannot submit a stale controlled value.
        /// </summary>
        [Test]
        public void VerifyDisabledFormValueIsNotSuccessful()
        {
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(selectInput => selectInput.Name, "state")
                .Add(selectInput => selectInput.Options, Options)
                .Add(selectInput => selectInput.Value, "third")
                .Add(selectInput => selectInput.Disabled, true));

            Assert.That(component.FindAll("input[name='state']"), Is.Empty);

            component.Render(parameters => parameters
                .Add(selectInput => selectInput.Name, "state")
                .Add(selectInput => selectInput.Options, Options)
                .Add(selectInput => selectInput.Value, "first")
                .Add(selectInput => selectInput.Disabled, false));

            Assert.That(component.Find("input[name='state']").GetAttribute("value"), Is.EqualTo("first"));
        }

        /// <summary>
        /// Verifies a blank required controlled value is exposed as invalid without claiming native constraint validation.
        /// </summary>
        [Test]
        public void VerifyEmptyRequiredValueUsesAccessibleValidationState()
        {
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(selectInput => selectInput.Name, "state")
                .Add(selectInput => selectInput.Options, Options)
                .Add(selectInput => selectInput.Required, true));

            var trigger = component.Find("[role='combobox']");
            var formValue = component.Find("input[type='hidden']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(trigger.GetAttribute("aria-required"), Is.EqualTo("true"));
                Assert.That(trigger.GetAttribute("aria-invalid"), Is.EqualTo("true"));
                Assert.That(formValue.GetAttribute("value"), Is.Empty);
                Assert.That(formValue.HasAttribute("required"), Is.False);
            }
        }

        /// <summary>
        /// Verifies a selected required controlled value clears the missing-value state.
        /// </summary>
        [Test]
        public void VerifySelectedRequiredValueIsAccessibleValid()
        {
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(selectInput => selectInput.Name, "state")
                .Add(selectInput => selectInput.Options, Options)
                .Add(selectInput => selectInput.Value, "first")
                .Add(selectInput => selectInput.Required, true));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("[role='combobox']").HasAttribute("aria-invalid"), Is.False);
                Assert.That(component.Find("input[name='state']").GetAttribute("value"), Is.EqualTo("first"));
            }
        }

        /// <summary>
        /// Verifies separate component instances own independent state and identifiers.
        /// </summary>
        [Test]
        public void VerifyMultipleInstancesRemainIndependent()
        {
            var first = this.Render<SelectInputComponent>(parameters => parameters
                .Add(selectInput => selectInput.Options, Options));
            var second = this.Render<SelectInputComponent>(parameters => parameters
                .Add(selectInput => selectInput.Options, Options));

            first.Find("[role='combobox']").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.FindAll("[role='listbox']"), Has.Count.EqualTo(1));
                Assert.That(second.FindAll("[role='listbox']"), Is.Empty);
                Assert.That(first.Find("[role='combobox']").Id,
                    Is.Not.EqualTo(second.Find("[role='combobox']").Id));
            }
        }

        /// <summary>
        /// Verifies stable instance identifiers survive ordinary parameter rerenders.
        /// </summary>
        [Test]
        public void VerifyGeneratedIdentifiersRemainStableAcrossRerenders()
        {
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(selectInput => selectInput.Options, Options)
                .Add(selectInput => selectInput.Name, "selection")
                .Add(selectInput => selectInput.Value, "first"));

            var triggerId = component.Find("[role='combobox']").Id;
            var listboxId = component.Find("[role='combobox']").GetAttribute("aria-controls");
            var formValueId = component.Find("input[name='selection']").Id;

            component.Render(parameters => parameters
                .Add(selectInput => selectInput.Options, Options)
                .Add(selectInput => selectInput.Name, "selection")
                .Add(selectInput => selectInput.Value, "third"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("[role='combobox']").Id, Is.EqualTo(triggerId));
                Assert.That(component.Find("[role='combobox']").GetAttribute("aria-controls"), Is.EqualTo(listboxId));
                Assert.That(component.Find("input[name='selection']").Id, Is.EqualTo(formValueId));
                Assert.That(formValueId, Is.EqualTo($"{triggerId}-form-value"));
            }
        }

        /// <summary>
        /// Verifies overlapping keyboard selection requests invoke one asynchronous callback.
        /// </summary>
        [Test]
        public async Task VerifyPendingSelectionPreventsDuplicateCallbacks()
        {
            var selectionCount = 0;
            var callbackStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var component = this.Render<SelectInputComponent>(parameters => parameters
                .Add(selectInput => selectInput.Options, Options)
                .Add(selectInput => selectInput.ValueChanged, async _ =>
                {
                    selectionCount++;
                    callbackStarted.TrySetResult();
                    await releaseCallback.Task;
                }));

            var trigger = component.Find("[role='combobox']");
            trigger.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

            var firstSelection = trigger.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });
            await callbackStarted.Task;

            var repeatedSelection = trigger.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });
            releaseCallback.SetResult();

            await Task.WhenAll(firstSelection, repeatedSelection);

            Assert.That(selectionCount, Is.EqualTo(1));
        }
    }
}
