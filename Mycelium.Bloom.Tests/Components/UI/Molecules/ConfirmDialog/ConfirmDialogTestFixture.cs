// ------------------------------------------------------------------------------------------------
// <copyright file="ConfirmDialogTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Molecules.ConfirmDialog
{
    using System.Threading.Tasks;

    using Bunit;

    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.Model.Enum;

    using ButtonComponent = Mycelium.Bloom.Components.UI.Atoms.Button.Button;
    using ConfirmDialogComponent = Mycelium.Bloom.Components.UI.Molecules.ConfirmDialog.ConfirmDialog;

    /// <summary>
    /// Tests the <see cref="ConfirmDialogComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ConfirmDialogTestFixture : BunitContext
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
        /// Verifies that selecting confirmation invokes the callback and closes by default.
        /// </summary>
        [Test]
        public void VerifyConfirmationInvokesCallbackAndClosesByDefault()
        {
            var confirmationCount = 0;
            var changedState = true;

            var component = this.Render<ConfirmDialogComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.Title, "Apply changes")
                .Add(component => component.Confirmed, () => confirmationCount++)
                .Add(component => component.IsOpenChanged, (bool isOpen) => changedState = isOpen));

            component.FindAll("button")[1].Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(confirmationCount, Is.EqualTo(1));
                Assert.That(changedState, Is.False);
            }
        }

        /// <summary>
        /// Verifies that confirmation leaves the dialog open when automatic closing is disabled.
        /// </summary>
        [Test]
        public void VerifyConfirmationRespectsCloseOnConfirm()
        {
            var openStateChangeCount = 0;

            var component = this.Render<ConfirmDialogComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.CloseOnConfirm, false)
                .Add(component => component.IsOpenChanged, (bool _) => openStateChangeCount++));

            component.FindAll("button")[1].Click();

            Assert.That(openStateChangeCount, Is.Zero);
        }

        /// <summary>
        /// Verifies that selecting cancellation invokes the callback and closes the dialog.
        /// </summary>
        [Test]
        public void VerifyCancellationInvokesCallbackAndCloses()
        {
            var cancellationCount = 0;
            var changedState = true;

            var component = this.Render<ConfirmDialogComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.Cancelled, () => cancellationCount++)
                .Add(component => component.IsOpenChanged, (bool isOpen) => changedState = isOpen));

            component.FindAll("button")[0].Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cancellationCount, Is.EqualTo(1));
                Assert.That(changedState, Is.False);
            }
        }

        /// <summary>
        /// Verifies that the optional confirmation description renders when provided.
        /// </summary>
        [Test]
        public void VerifyDescriptionRendersWhenProvided()
        {
            var component = this.Render<ConfirmDialogComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.Description, "This action cannot be undone."));

            Assert.That(
                component.Find(".mb-confirm-dialog__description").TextContent,
                Is.EqualTo("This action cannot be undone."));
        }

        /// <summary>
        /// Verifies that repeated actions are ignored while confirmation is pending.
        /// </summary>
        [Test]
        public async Task VerifyPendingConfirmationIgnoresRepeatedActions()
        {
            var confirmationCount = 0;
            var cancellationCount = 0;
            var callbackStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var component = this.Render<ConfirmDialogComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.Confirmed, async () =>
                {
                    confirmationCount++;
                    callbackStarted.TrySetResult();
                    await releaseCallback.Task;
                })
                .Add(component => component.Cancelled, () => cancellationCount++));

            var actions = component.FindComponents<ButtonComponent>();
            var confirmAction = actions[1].Instance.OnClick;
            var cancelAction = actions[0].Instance.OnClick;
            var firstConfirmation = component.InvokeAsync(() => confirmAction.InvokeAsync(new MouseEventArgs()));

            await callbackStarted.Task;

            actions = component.FindComponents<ButtonComponent>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(actions[0].Instance.Disabled, Is.True);
                Assert.That(actions[1].Instance.IsLoading, Is.True);
            }

            var repeatedConfirmation = component.InvokeAsync(() => confirmAction.InvokeAsync(new MouseEventArgs()));
            var cancellation = component.InvokeAsync(() => cancelAction.InvokeAsync(new MouseEventArgs()));

            releaseCallback.SetResult();
            await Task.WhenAll(firstConfirmation, repeatedConfirmation, cancellation);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(confirmationCount, Is.EqualTo(1));
                Assert.That(cancellationCount, Is.Zero);
            }
        }

        /// <summary>
        /// Verifies that the selected dialog variant and confirmation button use their expected CSS classes.
        /// </summary>
        /// <param name="variant">The confirmation dialog variant.</param>
        /// <param name="expectedDialogClass">The expected dialog CSS class.</param>
        /// <param name="expectedButtonClass">The expected confirmation button CSS class.</param>
        [TestCase(ConfirmDialogVariant.Default, "mb-confirm-dialog--default", "mb-button--primary")]
        [TestCase(ConfirmDialogVariant.Warning, "mb-confirm-dialog--warning", "mb-button--primary")]
        [TestCase(ConfirmDialogVariant.Danger, "mb-confirm-dialog--danger", "mb-button--danger")]
        public void VerifySelectedVariantRendersExpectedClasses(
            ConfirmDialogVariant variant,
            string expectedDialogClass,
            string expectedButtonClass)
        {
            var component = this.Render<ConfirmDialogComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.Variant, variant));

            var buttons = component.FindAll("button");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("[role='dialog']").GetAttribute("class"), Does.Contain(expectedDialogClass));
                Assert.That(buttons[1].GetAttribute("class"), Does.Contain(expectedButtonClass));
            }
        }

        /// <summary>
        /// Verifies that both actions are disabled and confirmation shows progress while confirming.
        /// </summary>
        [Test]
        public void VerifyConfirmingStateDisablesActions()
        {
            var component = this.Render<ConfirmDialogComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.IsConfirming, true));

            var buttons = component.FindAll("button");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(buttons, Has.Count.EqualTo(2));
                Assert.That(buttons[0].HasAttribute("disabled"), Is.True);
                Assert.That(buttons[1].HasAttribute("disabled"), Is.True);
                Assert.That(component.FindAll(".mb-button__spinner"), Has.Count.EqualTo(1));
            }
        }
    }
}
