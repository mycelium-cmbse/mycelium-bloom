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

    using BlazorBlueprint.Components;
    using BlazorBlueprint.Primitives.Services;

    using Bunit;

    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;

    using ConfirmDialogComponent = Mycelium.Bloom.Components.UI.Molecules.ConfirmDialog.ConfirmDialog;
    using BlueprintButtonSize = BlazorBlueprint.Components.ButtonSize;
    using BlueprintButtonVariant = BlazorBlueprint.Components.ButtonVariant;

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
        public async Task VerifyConfirmationInvokesCallbackAndClosesByDefault()
        {
            var confirmationCount = 0;
            var changedState = true;

            var component = this.Render<ConfirmDialogComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.Title, "Apply changes")
                .Add(component => component.ConfirmText, "Apply")
                .Add(component => component.CancelText, "Keep editing")
                .Add(component => component.Confirmed, () => confirmationCount++)
                .Add(component => component.IsOpenChanged, (bool isOpen) => changedState = isOpen));

            await this.FindActionButton("Apply").ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(confirmationCount, Is.EqualTo(1));
                Assert.That(changedState, Is.False);
                Assert.That(this.FindActionButton("Keep editing").GetAttribute("type"), Is.EqualTo("button"));
            }
        }

        /// <summary>
        /// Verifies that confirmation leaves the dialog open when automatic closing is disabled.
        /// </summary>
        [Test]
        public async Task VerifyConfirmationRespectsCloseOnConfirm()
        {
            var openStateChangeCount = 0;

            var component = this.Render<ConfirmDialogComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.CloseOnConfirm, false)
                .Add(component => component.IsOpenChanged, (bool _) => openStateChangeCount++));

            await this.FindActionButton("Confirm").ClickAsync();

            Assert.That(openStateChangeCount, Is.Zero);
        }

        /// <summary>
        /// Verifies that selecting cancellation invokes the callback and closes the dialog.
        /// </summary>
        [Test]
        public async Task VerifyCancellationInvokesCallbackAndCloses()
        {
            var cancellationCount = 0;
            var changedState = true;

            var component = this.Render<ConfirmDialogComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.Cancelled, () => cancellationCount++)
                .Add(component => component.IsOpenChanged, (bool isOpen) => changedState = isOpen));

            await this.FindActionButton("Cancel").ClickAsync();

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
                Assert.That(
                    dialog.TextContent.Split("This action cannot be undone.", System.StringSplitOptions.None).Length - 1,
                    Is.EqualTo(1));
            }
        }

        /// <summary>
        /// Verifies that multiple confirmation dialogs generate independent description relationships.
        /// </summary>
        [Test]
        public void VerifyMultipleInstancesGenerateUniqueDescriptionIds()
        {
            _ = this.Render<ConfirmDialogComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.Title, "First confirmation")
                .Add(component => component.Description, "First confirmation message."));
            _ = this.Render<ConfirmDialogComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.Title, "Second confirmation")
                .Add(component => component.Description, "Second confirmation message."));

            var dialogs = this.portalHost.WaitForElements("[role='dialog']", 2);
            var titleIds = dialogs
                .Select(dialog => dialog.GetAttribute("aria-labelledby"))
                .ToArray();
            var descriptionIds = dialogs
                .Select(dialog => dialog.GetAttribute("aria-describedby"))
                .ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(titleIds.All(id => !string.IsNullOrWhiteSpace(id)), Is.True);
                Assert.That(titleIds, Is.Unique);
                Assert.That(titleIds.All(id => this.portalHost.FindAll($"#{id}").Count == 1), Is.True);
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

            await actions[1].ClickAsync();
            await actions[0].ClickAsync();

            releaseCallback.SetResult();
            await firstConfirmation;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(confirmationCount, Is.EqualTo(1));
                Assert.That(cancellationCount, Is.Zero);
            }
        }

        /// <summary>
        /// Verifies that confirmation workflows map to supported styled Blueprint button variants.
        /// </summary>
        /// <param name="variant">The confirmation dialog variant.</param>
        /// <param name="expectedButtonVariant">The expected styled Blueprint button variant.</param>
        [TestCase(ConfirmDialogVariant.Default, BlueprintButtonVariant.Default)]
        [TestCase(ConfirmDialogVariant.Warning, BlueprintButtonVariant.Default)]
        [TestCase(ConfirmDialogVariant.Danger, BlueprintButtonVariant.Destructive)]
        public void VerifySelectedVariantMapsToBlueprintButton(
            ConfirmDialogVariant variant,
            BlueprintButtonVariant expectedButtonVariant)
        {
            var component = this.Render<ConfirmDialogComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.Variant, variant));

            var buttons = this.portalHost.FindComponents<BbButton>();
            var cancelButton = buttons.Single(button => button.Instance.Variant == BlueprintButtonVariant.Secondary);
            var confirmButton = buttons.Single(button => button.Instance.Variant != BlueprintButtonVariant.Secondary);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cancelButton.Instance.Type, Is.EqualTo(ButtonType.Button));
                Assert.That(cancelButton.Instance.Size, Is.EqualTo(BlueprintButtonSize.Small));
                Assert.That(confirmButton.Instance.Type, Is.EqualTo(ButtonType.Button));
                Assert.That(confirmButton.Instance.Size, Is.EqualTo(BlueprintButtonSize.Small));
                Assert.That(confirmButton.Instance.Variant, Is.EqualTo(expectedButtonVariant));
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
            var confirmButton = this.portalHost.FindComponents<BbButton>()
                .Single(button => button.Instance.Variant != BlueprintButtonVariant.Secondary);
            var progressStatus = this.portalHost.Find("[role='status'][aria-label='Confirmation in progress']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(buttons, Has.Count.EqualTo(2));
                Assert.That(buttons[0].HasAttribute("disabled"), Is.True);
                Assert.That(buttons[1].HasAttribute("disabled"), Is.True);
                Assert.That(buttons[1].GetAttribute("aria-busy"), Is.EqualTo("true"));
                Assert.That(confirmButton.Instance.Loading, Is.True);
                Assert.That(progressStatus.GetAttribute("tabindex"), Is.EqualTo("0"));
                Assert.That(progressStatus.HasAttribute("aria-hidden"), Is.False);
            }
        }

        /// <summary>
        /// Finds a confirmation action by its application-owned label.
        /// </summary>
        /// <param name="label">The exact action label.</param>
        /// <returns>The matching action button.</returns>
        private AngleSharp.Dom.IElement FindActionButton(string label)
        {
            return this.portalHost.WaitForElements("[role='dialog'] button", 2)
                .Single(button => string.Equals(button.TextContent.Trim(), label, System.StringComparison.Ordinal));
        }
    }
}
