// ------------------------------------------------------------------------------------------------
// <copyright file="ModalShellTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Molecules.ModalShell
{
    using System.Threading.Tasks;

    using Bunit;

    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.Model.Enum;

    using ButtonComponent = BlazorBlueprint.Components.BbButton;
    using ModalShellComponent = Mycelium.Bloom.Components.UI.Molecules.ModalShell.ModalShell;

    /// <summary>
    /// Tests the <see cref="ModalShellComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ModalShellTestFixture : BunitContext
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
        /// Verifies that dialog content is not rendered while the modal is closed.
        /// </summary>
        [Test]
        public void VerifyClosedModalRendersNothing()
        {
            var component = this.Render<ModalShellComponent>(parameters => parameters
                .Add(component => component.IsOpen, false)
                .AddChildContent("Dialog content"));

            Assert.That(component.FindAll("[role='dialog']"), Is.Empty);
        }

        /// <summary>
        /// Verifies that named dialog content renders while the modal is open.
        /// </summary>
        [Test]
        public void VerifyOpenModalRendersNamedContent()
        {
            var component = this.Render<ModalShellComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.Title, "Edit selection")
                .Add(component => component.Description, "Update the selected item.")
                .Add(component => component.HeaderContent, "<span class='custom-header'>Custom header</span>")
                .AddChildContent("<div class='custom-body'>Body</div>")
                .Add(component => component.FooterContent, "<span class='custom-footer'>Footer</span>"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("[role='dialog']").GetAttribute("aria-label"), Is.EqualTo("Edit selection"));
                Assert.That(component.Find(".custom-header").TextContent, Is.EqualTo("Custom header"));
                Assert.That(component.Find(".custom-body").TextContent, Is.EqualTo("Body"));
                Assert.That(component.Find(".custom-footer").TextContent, Is.EqualTo("Footer"));
            }
        }

        /// <summary>
        /// Verifies that the configured identifier labels the default title and description elements.
        /// </summary>
        [Test]
        public void VerifyConfiguredIdLabelsDefaultHeadingContent()
        {
            var component = this.Render<ModalShellComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.Id, "edit-dialog")
                .Add(component => component.Title, "Edit selection")
                .Add(component => component.Description, "Update the selected item.")
                .Add(component => component.ShowCloseButton, false));

            var dialog = component.Find("[role='dialog']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dialog.Id, Is.EqualTo("edit-dialog"));
                Assert.That(dialog.GetAttribute("aria-labelledby"), Is.EqualTo("edit-dialog-title"));
                Assert.That(dialog.GetAttribute("aria-describedby"), Is.EqualTo("edit-dialog-description"));
                Assert.That(component.Find("#edit-dialog-title").TextContent, Is.EqualTo("Edit selection"));
                Assert.That(component.Find("#edit-dialog-description").TextContent, Is.EqualTo("Update the selected item."));
            }
        }

        /// <summary>
        /// Verifies that the close button invokes both close callbacks.
        /// </summary>
        [Test]
        public void VerifyCloseButtonInvokesCloseBehavior()
        {
            var changedState = true;
            var closeCount = 0;

            var component = this.Render<ModalShellComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.Title, "Dialog")
                .Add(component => component.IsOpenChanged, (bool isOpen) => changedState = isOpen)
                .Add(component => component.OnClose, () => closeCount++));

            component.Find("button[aria-label='Close dialog']").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(changedState, Is.False);
                Assert.That(closeCount, Is.EqualTo(1));
            }
        }

        /// <summary>
        /// Verifies that repeated close actions are ignored while close callbacks are pending.
        /// </summary>
        [Test]
        public async Task VerifyPendingCloseIgnoresRepeatedActions()
        {
            var stateChangeCount = 0;
            var closeCount = 0;
            var callbackStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var component = this.Render<ModalShellComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.Title, "Dialog")
                .Add(component => component.IsOpenChanged, async (bool _) =>
                {
                    stateChangeCount++;
                    callbackStarted.TrySetResult();
                    await releaseCallback.Task;
                })
                .Add(component => component.OnClose, () => closeCount++));

            var closeButton = component.FindComponent<ButtonComponent>();
            var closeAction = closeButton.Instance.OnClick;
            var firstClose = component.InvokeAsync(() => closeAction.InvokeAsync(new MouseEventArgs()));

            await callbackStarted.Task;

            closeButton = component.FindComponent<ButtonComponent>();
            Assert.That(closeButton.Instance.Disabled, Is.True);

            var repeatedClose = component.InvokeAsync(() => closeAction.InvokeAsync(new MouseEventArgs()));

            releaseCallback.SetResult();
            await Task.WhenAll(firstClose, repeatedClose);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stateChangeCount, Is.EqualTo(1));
                Assert.That(closeCount, Is.EqualTo(1));
            }
        }

        /// <summary>
        /// Verifies that a backdrop click follows the configured close behavior.
        /// </summary>
        /// <param name="closeOnBackdropClick">A value indicating whether backdrop closing is enabled.</param>
        /// <param name="expectedCloseCount">The expected callback invocation count.</param>
        [TestCase(true, 1)]
        [TestCase(false, 0)]
        public void VerifyBackdropClickFollowsConfiguration(bool closeOnBackdropClick, int expectedCloseCount)
        {
            var closeCount = 0;

            var component = this.Render<ModalShellComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.CloseOnBackdropClick, closeOnBackdropClick)
                .Add(component => component.OnClose, () => closeCount++));

            component.Find(".mb-modal__backdrop").Click();

            Assert.That(closeCount, Is.EqualTo(expectedCloseCount));
        }

        /// <summary>
        /// Verifies that the selected modal size renders its expected CSS class.
        /// </summary>
        /// <param name="size">The modal size.</param>
        /// <param name="expectedCssClass">The expected CSS class.</param>
        [TestCase(ModalSize.Small, "mb-modal__panel--small")]
        [TestCase(ModalSize.Medium, "mb-modal__panel--medium")]
        [TestCase(ModalSize.Large, "mb-modal__panel--large")]
        [TestCase(ModalSize.Wide, "mb-modal__panel--wide")]
        public void VerifySelectedSizeRendersExpectedClass(ModalSize size, string expectedCssClass)
        {
            var component = this.Render<ModalShellComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.Size, size));

            Assert.That(component.Find("[role='dialog']").GetAttribute("class"), Does.Contain(expectedCssClass));
        }
    }
}
