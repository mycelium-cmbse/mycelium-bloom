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
    using System;
    using System.Threading.Tasks;

    using BlazorBlueprint.Primitives.Services;

    using Bunit;

    using Microsoft.AspNetCore.Components;
    using Microsoft.Extensions.DependencyInjection;

    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;

    using ModalShellComponent = Mycelium.Bloom.Components.UI.Molecules.ModalShell.ModalShell;

    /// <summary>
    /// Tests the <see cref="ModalShellComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ModalShellTestFixture : BunitContext
    {
        private readonly IRenderedComponent<BbPortalHost> portalHost;

        private readonly RecordingFocusManager focusManager = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ModalShellTestFixture" /> class.
        /// </summary>
        public ModalShellTestFixture()
        {
            BlueprintTestSetup.Configure(this);
            this.Services.AddSingleton<IFocusManager>(this.focusManager);
            this.portalHost = this.Render<BbPortalHost>();
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
        /// Verifies that dialog content is not rendered while the modal is closed.
        /// </summary>
        [Test]
        public void VerifyClosedModalRendersNothing()
        {
            var component = this.Render<ModalShellComponent>(parameters => parameters
                .Add(component => component.IsOpen, false)
                .AddChildContent("Dialog content"));

            Assert.That(this.portalHost.FindAll("[role='dialog']"), Is.Empty);
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
                Assert.That(this.portalHost.WaitForElement("[role='dialog']").GetAttribute("aria-label"), Is.EqualTo("Edit selection"));
                Assert.That(this.portalHost.Find(".custom-header").TextContent, Is.EqualTo("Custom header"));
                Assert.That(this.portalHost.Find(".custom-body").TextContent, Is.EqualTo("Body"));
                Assert.That(this.portalHost.Find(".custom-footer").TextContent, Is.EqualTo("Footer"));
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

            var dialog = this.portalHost.WaitForElement("[role='dialog']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dialog.Id, Is.EqualTo("edit-dialog"));
                Assert.That(dialog.GetAttribute("aria-labelledby"), Is.EqualTo("edit-dialog-title"));
                Assert.That(dialog.GetAttribute("aria-describedby"), Is.EqualTo("edit-dialog-description"));
                Assert.That(this.portalHost.Find("#edit-dialog-title").TextContent, Is.EqualTo("Edit selection"));
                Assert.That(this.portalHost.Find("#edit-dialog-description").TextContent, Is.EqualTo("Update the selected item."));
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

            var closeButton = this.portalHost.WaitForElement("button[aria-label='Close dialog']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(closeButton.GetAttribute("title"), Is.EqualTo("Close dialog"));
                Assert.That(this.portalHost.FindAll("[role='tooltip']"), Is.Empty);
            }

            closeButton.Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(changedState, Is.False);
                Assert.That(closeCount, Is.EqualTo(1));
            }
        }

        /// <summary>
        /// Verifies that controlled closing restores the per-open-cycle focus target and supports reopening.
        /// </summary>
        [Test]
        public void VerifyControlledCloseRestoresFocusAndSupportsReopening()
        {
            ElementReference focusReturnTarget = default;

            var component = this.Render<ModalShellComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.FocusReturnTarget, focusReturnTarget)
                .Add(component => component.ShowCloseButton, false));

            component.Render(parameters => parameters
                .Add(component => component.IsOpen, false)
                .Add(component => component.FocusReturnTarget, focusReturnTarget)
                .Add(component => component.ShowCloseButton, false));

            Assert.That(this.focusManager.RestoreFocusCallCount, Is.EqualTo(1));

            component.Render(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.FocusReturnTarget, focusReturnTarget)
                .Add(component => component.ShowCloseButton, false));
            component.Render(parameters => parameters
                .Add(component => component.IsOpen, false)
                .Add(component => component.FocusReturnTarget, focusReturnTarget)
                .Add(component => component.ShowCloseButton, false));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.focusManager.RestoreFocusCallCount, Is.EqualTo(2));
                Assert.That(this.portalHost.FindAll("[role='dialog']"), Is.Empty);
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

            var firstClose = this.portalHost.WaitForElement("button[aria-label='Close dialog']").ClickAsync();

            await callbackStarted.Task;

            var closeButton = this.portalHost.Find("button[aria-label='Close dialog']");
            Assert.That(closeButton.HasAttribute("disabled"), Is.True);

            closeButton.Click();

            releaseCallback.SetResult();
            await firstClose;

            component.WaitForAssertion(() => Assert.That(closeCount, Is.EqualTo(1)));

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

            this.portalHost.WaitForElement(".mb-modal__backdrop").Click();

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

            Assert.That(this.portalHost.WaitForElement("[role='dialog']").GetAttribute("class"), Does.Contain(expectedCssClass));
        }

        private sealed class RecordingFocusManager : IFocusManager
        {
            /// <summary>
            /// Gets the number of focus-restoration requests.
            /// </summary>
            internal int RestoreFocusCallCount { get; private set; }

            /// <inheritdoc />
            public Task<IAsyncDisposable> TrapFocus(ElementReference container)
            {
                return Task.FromResult<IAsyncDisposable>(new EmptyAsyncDisposable());
            }

            /// <inheritdoc />
            public Task RestoreFocus(ElementReference? previousElement)
            {
                this.RestoreFocusCallCount++;
                return Task.CompletedTask;
            }

            /// <inheritdoc />
            public Task FocusFirst(ElementReference container)
            {
                return Task.CompletedTask;
            }

            /// <inheritdoc />
            public Task FocusLast(ElementReference container)
            {
                return Task.CompletedTask;
            }
        }

        private sealed class EmptyAsyncDisposable : IAsyncDisposable
        {
            /// <inheritdoc />
            public ValueTask DisposeAsync()
            {
                return ValueTask.CompletedTask;
            }
        }
    }
}
