// ------------------------------------------------------------------------------------------------
// <copyright file="NotificationToastTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Molecules.NotificationToast
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using BlazorBlueprint.Primitives.Services;

    using Bunit;

    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.Tests.Common;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    using NotificationToastComponent = Mycelium.Bloom.Components.UI.Molecules.NotificationToast.NotificationToast;

    /// <summary>
    /// Tests the <see cref="NotificationToastComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class NotificationToastTestFixture : BunitContext
    {
        private readonly IRenderedComponent<BbPortalHost> portalHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationToastTestFixture" /> class.
        /// </summary>
        public NotificationToastTestFixture()
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
        /// Verifies that the supplied notification content renders.
        /// </summary>
        [Test]
        public void VerifySuppliedNotificationRenders()
        {
            var notification = new ToastNotification
            {
                Id = "model-saved",
                Title = "Model saved",
                Message = "The latest changes are available.",
                Variant = ToastNotificationVariant.Success
            };

            var component = this.Render<NotificationToastComponent>(parameters => parameters
                .Add(component => component.Notification, notification));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-notification-toast__title").TextContent, Is.EqualTo("Model saved"));
                Assert.That(component.Find(".mb-notification-toast__message").TextContent, Is.EqualTo("The latest changes are available."));
                Assert.That(component.Find("article").GetAttribute("class"), Does.Contain("mb-notification-toast--success"));
            }
        }

        /// <summary>
        /// Verifies that each notification variant uses its expected visual class and accessibility role.
        /// </summary>
        /// <param name="variant">The notification variant.</param>
        /// <param name="expectedCssClass">The expected CSS class.</param>
        /// <param name="expectedRole">The expected accessibility role.</param>
        [TestCase(ToastNotificationVariant.Info, "mb-notification-toast--info", "status")]
        [TestCase(ToastNotificationVariant.Success, "mb-notification-toast--success", "status")]
        [TestCase(ToastNotificationVariant.Warning, "mb-notification-toast--warning", "alert")]
        [TestCase(ToastNotificationVariant.Danger, "mb-notification-toast--danger", "alert")]
        public void VerifySelectedVariantRendersExpectedClassAndRole(
            ToastNotificationVariant variant,
            string expectedCssClass,
            string expectedRole)
        {
            var notification = new ToastNotification
            {
                Id = "notification",
                Title = "Notification",
                Variant = variant
            };

            var component = this.Render<NotificationToastComponent>(parameters => parameters
                .Add(component => component.Notification, notification));

            var toast = component.Find("article");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(toast.GetAttribute("class"), Does.Contain(expectedCssClass));
                Assert.That(toast.GetAttribute("role"), Is.EqualTo(expectedRole));
            }
        }

        /// <summary>
        /// Verifies that dismissal invokes the callback with the supplied notification identifier.
        /// </summary>
        [Test]
        public async Task VerifyDismissInvokesCallbackWithIdentifier()
        {
            var callbackCount = 0;
            var dismissedIdentifier = string.Empty;
            var notification = new ToastNotification
            {
                Id = "dismiss-me",
                Title = "Dismissible notification"
            };

            var component = this.Render<NotificationToastComponent>(parameters => parameters
                .Add(component => component.Notification, notification)
                .Add(component => component.Dismissed, async (string identifier) =>
                {
                    await Task.Yield();
                    callbackCount++;
                    dismissedIdentifier = identifier;
                }));

            var dismissButton = component.Find("button");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dismissButton.GetAttribute("aria-label"), Is.EqualTo("Dismiss Dismissible notification"));
                Assert.That(dismissButton.GetAttribute("title"), Is.EqualTo("Dismiss Dismissible notification"));
                Assert.That(component.FindAll("[role='tooltip']"), Is.Empty);
                Assert.That(this.portalHost.FindAll("[role='tooltip']"), Is.Empty);
            }

            await dismissButton.ClickAsync(new MouseEventArgs());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dismissedIdentifier, Is.EqualTo("dismiss-me"));
                Assert.That(callbackCount, Is.EqualTo(1));
            }
        }

        /// <summary>
        /// Verifies that multiple toast instances retain their own identifiers and callback owners.
        /// </summary>
        [Test]
        public async Task VerifyMultipleInstancesKeepDismissCallbacksIndependent()
        {
            var firstDismissals = new List<string>();
            var secondDismissals = new List<string>();
            var firstNotification = new ToastNotification
            {
                Id = "first-toast",
                Title = "First toast"
            };
            var secondNotification = new ToastNotification
            {
                Id = "second-toast",
                Title = "Second toast"
            };

            var firstComponent = this.Render<NotificationToastComponent>(parameters => parameters
                .Add(component => component.Notification, firstNotification)
                .Add(component => component.Dismissed, (string identifier) => firstDismissals.Add(identifier)));
            var secondComponent = this.Render<NotificationToastComponent>(parameters => parameters
                .Add(component => component.Notification, secondNotification)
                .Add(component => component.Dismissed, (string identifier) => secondDismissals.Add(identifier)));

            await firstComponent.Find("button").ClickAsync(new MouseEventArgs());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstDismissals, Is.EqualTo(new[] { "first-toast" }));
                Assert.That(secondDismissals, Is.Empty);
            }

            await secondComponent.Find("button").ClickAsync(new MouseEventArgs());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstDismissals, Is.EqualTo(new[] { "first-toast" }));
                Assert.That(secondDismissals, Is.EqualTo(new[] { "second-toast" }));
            }
        }

        /// <summary>
        /// Verifies that disposing an untouched toast cannot invoke its dismissal callback.
        /// </summary>
        [Test]
        public void VerifyDisposalBeforeDismissDoesNotInvokeCallback()
        {
            var callbackCount = 0;
            var notification = new ToastNotification
            {
                Id = "dispose-without-dismiss",
                Title = "Dispose without dismiss"
            };

            var component = this.Render<NotificationToastComponent>(parameters => parameters
                .Add(component => component.Notification, notification)
                .Add(component => component.Dismissed, (string _) => callbackCount++));

            component.Dispose();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.IsDisposed, Is.True);
                Assert.That(callbackCount, Is.Zero);
            }
        }

        /// <summary>
        /// Verifies that disposal cannot erase or repeat a completed dismissal callback.
        /// </summary>
        [Test]
        public async Task VerifyDisposalAfterDismissPreservesCompletedCallback()
        {
            var dismissedIdentifiers = new List<string>();
            var notification = new ToastNotification
            {
                Id = "dismiss-before-dispose",
                Title = "Dismiss before dispose"
            };

            var component = this.Render<NotificationToastComponent>(parameters => parameters
                .Add(component => component.Notification, notification)
                .Add(component => component.Dismissed, (string identifier) => dismissedIdentifiers.Add(identifier)));

            await component.Find("button").ClickAsync(new MouseEventArgs());
            component.Dispose();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.IsDisposed, Is.True);
                Assert.That(dismissedIdentifiers, Is.EqualTo(new[] { "dismiss-before-dispose" }));
            }
        }

        /// <summary>
        /// Verifies that a non-dismissible notification does not render a dismiss action.
        /// </summary>
        [Test]
        public void VerifyNonDismissibleNotificationHidesDismissAction()
        {
            var notification = new ToastNotification
            {
                Id = "persistent",
                Title = "Persistent notification",
                IsDismissible = false
            };

            var component = this.Render<NotificationToastComponent>(parameters => parameters
                .Add(component => component.Notification, notification));

            Assert.That(component.FindAll("button"), Is.Empty);
        }
    }
}
