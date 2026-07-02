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
    using Bunit;

    using Mycelium.Bloom.Model.Enum;

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
        /// Verifies that cancellation invokes the cancel and open-state callbacks.
        /// </summary>
        [Test]
        public void VerifyCancelInvokesCallbacks()
        {
            var cancelledCount = 0;
            bool? isOpen = null;

            var component = this.Render<ConfirmDialogComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.IsOpenChanged, value => isOpen = value)
                .Add(component => component.Title, "Discard changes")
                .Add(component => component.Message, "Discard pending model changes?")
                .Add(component => component.CancelText, "Keep editing")
                .Add(component => component.ConfirmText, "Discard")
                .Add(component => component.Cancelled, () => cancelledCount++)
                .Add(component => component.Variant, ConfirmDialogVariant.Warning)
                .Add(component => component.Class, "custom-confirm")
                .AddUnmatched("data-testid", "confirm-dialog"));

            component.FindAll(".mb-confirm-dialog__footer .mb-button")[0].Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cancelledCount, Is.EqualTo(1));
                Assert.That(isOpen, Is.False);
                Assert.That(component.Find(".mb-modal__panel").GetAttribute("data-testid"), Is.EqualTo("confirm-dialog"));
                Assert.That(component.Find(".mb-modal__panel").GetAttribute("class"), Does.Contain("custom-confirm"));
                Assert.That(component.Find(".mb-confirm-dialog__icon").GetAttribute("class"), Does.Contain("mb-confirm-dialog__icon--warning"));
                Assert.That(component.Find(".mb-confirm-dialog__message").TextContent.Trim(), Is.EqualTo("Discard pending model changes?"));
            }
        }

        /// <summary>
        /// Verifies that confirmation invokes the confirm callback and closes by default.
        /// </summary>
        [Test]
        public void VerifyConfirmInvokesCallbacks()
        {
            var confirmedCount = 0;
            bool? isOpen = null;

            var component = this.Render<ConfirmDialogComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.IsOpenChanged, value => isOpen = value)
                .Add(component => component.Message, "Delete this item?")
                .Add(component => component.Confirmed, () => confirmedCount++)
                .Add(component => component.Variant, ConfirmDialogVariant.Danger));

            component.FindAll(".mb-confirm-dialog__footer .mb-button")[1].Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(confirmedCount, Is.EqualTo(1));
                Assert.That(isOpen, Is.False);
                Assert.That(component.Find(".mb-confirm-dialog__icon").GetAttribute("class"), Does.Contain("mb-confirm-dialog__icon--danger"));
                Assert.That(component.FindAll(".mb-button")[1].GetAttribute("class"), Does.Contain("mb-button--danger"));
            }
        }

        /// <summary>
        /// Verifies confirming can leave the dialog open and confirmation buttons can be disabled.
        /// </summary>
        [Test]
        public void VerifyConfirmCanRemainOpen()
        {
            var confirmedCount = 0;
            bool? isOpen = null;

            var component = this.Render<ConfirmDialogComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.IsOpenChanged, value => isOpen = value)
                .Add(component => component.CloseOnConfirm, false)
                .Add(component => component.Confirmed, () => confirmedCount++)
                .Add(component => component.Variant, ConfirmDialogVariant.Default));

            component.FindAll(".mb-confirm-dialog__footer .mb-button")[1].Click();

            var confirmingComponent = this.Render<ConfirmDialogComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.IsConfirming, true));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(confirmedCount, Is.EqualTo(1));
                Assert.That(isOpen, Is.Null);
                Assert.That(component.Find(".mb-confirm-dialog__icon").TextContent.Trim(), Is.EqualTo("?"));
                Assert.That(confirmingComponent.FindAll(".mb-confirm-dialog__footer .mb-button"), Has.All.Matches<AngleSharp.Dom.IElement>(button => button.HasAttribute("disabled")));
            }
        }
    }
}
