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
    using System.Linq;
    using System.Threading.Tasks;

    using BlazorBlueprint.Components;
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
            _ = this.Render<ModalShellComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.Title, "Edit selection")
                .Add(component => component.Description, "Update the selected item.")
                .AddChildContent("<div class='custom-body'>Body</div>")
                .Add(component => component.FooterContent, "<span class='custom-footer'>Footer</span>"));

            var dialog = this.portalHost.WaitForElement("[role='dialog']");
            var titleId = dialog.GetAttribute("aria-labelledby");
            var descriptionId = dialog.GetAttribute("aria-describedby");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dialog.GetAttribute("aria-label"), Is.Null);
                Assert.That(this.portalHost.Find($"#{titleId}").TextContent, Is.EqualTo("Edit selection"));
                Assert.That(this.portalHost.Find($"#{descriptionId}").TextContent, Is.EqualTo("Update the selected item."));
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
            _ = this.Render<ModalShellComponent>(parameters => parameters
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
        /// Verifies that multiple modal instances own unique accessible relationships.
        /// </summary>
        [Test]
        public void VerifyMultipleInstancesGenerateUniqueAccessibleRelationships()
        {
            _ = this.Render<ModalShellComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.Title, "First dialog")
                .Add(component => component.Description, "First description"));
            _ = this.Render<ModalShellComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.Title, "Second dialog")
                .Add(component => component.Description, "Second description"));

            var dialogs = this.portalHost.WaitForElements("[role='dialog']", 2);
            var titleIds = dialogs.Select(dialog => dialog.GetAttribute("aria-labelledby")).ToArray();
            var descriptionIds = dialogs.Select(dialog => dialog.GetAttribute("aria-describedby")).ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(titleIds.All(id => !string.IsNullOrWhiteSpace(id)), Is.True);
                Assert.That(titleIds, Is.Unique);
                Assert.That(descriptionIds.All(id => !string.IsNullOrWhiteSpace(id)), Is.True);
                Assert.That(descriptionIds, Is.Unique);
                Assert.That(titleIds.All(id => this.portalHost.FindAll($"#{id}").Count == 1), Is.True);
                Assert.That(descriptionIds.All(id => this.portalHost.FindAll($"#{id}").Count == 1), Is.True);
            }
        }

        /// <summary>
        /// Verifies that the close button invokes both close callbacks.
        /// </summary>
        [Test]
        public async Task VerifyCloseButtonInvokesCloseBehavior()
        {
            var changedState = true;
            var closeCount = 0;

            _ = this.Render<ModalShellComponent>(parameters => parameters
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

            await closeButton.ClickAsync();

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
        /// Verifies that backdrop dismissal policy is mapped to the styled Blueprint dialog.
        /// </summary>
        /// <param name="closeOnBackdropClick">A value indicating whether backdrop closing is enabled.</param>
        [TestCase(true)]
        [TestCase(false)]
        public void VerifyBackdropDismissalPolicy(bool closeOnBackdropClick)
        {
            var component = this.Render<ModalShellComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.CloseOnBackdropClick, closeOnBackdropClick));

            var dialogContent = component.FindComponent<BbDialogContent>();

            Assert.That(dialogContent.Instance.CloseOnOverlayClick, Is.EqualTo(closeOnBackdropClick));
        }

        /// <summary>
        /// Verifies that the selected modal size renders its expected CSS class.
        /// </summary>
        /// <param name="size">The modal size.</param>
        /// <param name="expectedCssClass">The expected CSS class.</param>
        [TestCase(ModalSize.Small, "max-w-[22.5rem]")]
        [TestCase(ModalSize.Medium, "max-w-[30rem]")]
        [TestCase(ModalSize.Large, "max-w-[40rem]")]
        [TestCase(ModalSize.Wide, "max-w-[52.5rem]")]
        public void VerifySelectedSizeRendersExpectedClass(ModalSize size, string expectedCssClass)
        {
            _ = this.Render<ModalShellComponent>(parameters => parameters
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
