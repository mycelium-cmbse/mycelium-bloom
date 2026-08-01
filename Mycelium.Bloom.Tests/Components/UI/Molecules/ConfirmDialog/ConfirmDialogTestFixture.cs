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
    using System.Linq;
    using System.Threading.Tasks;

    using BlazorBlueprint.Primitives.Services;

    using Bunit;

    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;

    using ConfirmDialogComponent = Mycelium.Bloom.Components.UI.Molecules.ConfirmDialog.ConfirmDialog;

    /// <summary>
    /// Tests the <see cref="ConfirmDialogComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ConfirmDialogTestFixture : BunitContext
    {
        private static readonly string[] ExpectedDescriptionMessages =
        [
            "First confirmation message.",
            "Second confirmation message."
        ];

        private readonly IRenderedComponent<BbPortalHost> portalHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfirmDialogTestFixture" /> class.
        /// </summary>
        public ConfirmDialogTestFixture()
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

            this.portalHost.WaitForElements("[role='dialog'] button", 2)[1].Click();

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

            this.portalHost.WaitForElements("[role='dialog'] button", 2)[1].Click();

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

            this.portalHost.WaitForElements("[role='dialog'] button", 2)[0].Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cancellationCount, Is.EqualTo(1));
                Assert.That(changedState, Is.False);
            }
        }

        /// <summary>
        /// Verifies that the optional confirmation description renders once and describes the dialog.
        /// </summary>
        [Test]
        public void VerifyDescriptionRendersOnceAndDescribesDialog()
        {
            var component = this.Render<ConfirmDialogComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.Description, "This action cannot be undone."));

            var dialog = this.portalHost.WaitForElement("[role='dialog']");
            var descriptionId = dialog.GetAttribute("aria-describedby");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(descriptionId, Is.Not.Null.And.Not.Empty);
                Assert.That(descriptionId, Does.Not.Contain(" "));
                Assert.That(this.portalHost.FindAll($"#{descriptionId}"), Has.Count.EqualTo(1));
                Assert.That(this.portalHost.Find($"#{descriptionId}").TextContent, Is.EqualTo("This action cannot be undone."));
                Assert.That(this.portalHost.FindAll(".mb-modal__description"), Has.Count.EqualTo(1));
                Assert.That(this.portalHost.FindAll(".mb-confirm-dialog__description"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies that multiple confirmation dialogs generate independent description relationships.
        /// </summary>
        [Test]
        public void VerifyMultipleInstancesGenerateUniqueDescriptionIds()
        {
            var firstComponent = this.Render<ConfirmDialogComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.Description, "First confirmation message."));
            var secondComponent = this.Render<ConfirmDialogComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.Description, "Second confirmation message."));

            var dialogs = this.portalHost.WaitForElements("[role='dialog']", 2);
            var descriptionIds = dialogs
                .Select(dialog => dialog.GetAttribute("aria-describedby"))
                .ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(descriptionIds.All(id => !string.IsNullOrWhiteSpace(id)), Is.True);
                Assert.That(descriptionIds, Is.Unique);
                Assert.That(descriptionIds.All(id => this.portalHost.FindAll($"#{id}").Count == 1), Is.True);
                Assert.That(
                    descriptionIds.Select(id => this.portalHost.Find($"#{id}").TextContent),
                    Is.EquivalentTo(ExpectedDescriptionMessages));
            }
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

            var actions = this.portalHost.WaitForElements("[role='dialog'] button", 2);
            var firstConfirmation = actions[1].ClickAsync();

            await callbackStarted.Task;

            actions = this.portalHost.FindAll("[role='dialog'] button");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(actions[0].HasAttribute("disabled"), Is.True);
                Assert.That(actions[1].HasAttribute("disabled"), Is.True);
                Assert.That(actions[1].GetAttribute("aria-busy"), Is.EqualTo("true"));
            }

            actions[1].Click();
            actions[0].Click();

            releaseCallback.SetResult();
            await firstConfirmation;

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

            var buttons = this.portalHost.WaitForElements("[role='dialog'] button", 2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.portalHost.Find("[role='dialog']").GetAttribute("class"), Does.Contain(expectedDialogClass));
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

            var buttons = this.portalHost.WaitForElements("[role='dialog'] button", 2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(buttons, Has.Count.EqualTo(2));
                Assert.That(buttons[0].HasAttribute("disabled"), Is.True);
                Assert.That(buttons[1].HasAttribute("disabled"), Is.True);
                Assert.That(this.portalHost.FindAll(".mb-button__spinner"), Has.Count.EqualTo(1));
            }
        }
    }
}
