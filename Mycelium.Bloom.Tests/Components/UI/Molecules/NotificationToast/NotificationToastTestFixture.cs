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
    using Bunit;

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
        /// <summary>
        /// Disposes the bUnit test context after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            this.Dispose();
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
        public void VerifyDismissInvokesCallbackWithIdentifier()
        {
            var dismissedIdentifier = string.Empty;
            var notification = new ToastNotification
            {
                Id = "dismiss-me",
                Title = "Dismissible notification"
            };

            var component = this.Render<NotificationToastComponent>(parameters => parameters
                .Add(component => component.Notification, notification)
                .Add(component => component.Dismissed, (string identifier) => dismissedIdentifier = identifier));

            component.Find("button").Click();

            Assert.That(dismissedIdentifier, Is.EqualTo("dismiss-me"));
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
