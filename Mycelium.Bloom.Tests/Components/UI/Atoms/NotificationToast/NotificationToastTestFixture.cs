// ------------------------------------------------------------------------------------------------
// <copyright file="NotificationToastTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Atoms.NotificationToast
{
    using Bunit;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    using NotificationToastComponent = Mycelium.Bloom.Components.UI.Atoms.NotificationToast.NotificationToast;

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
        /// Verifies that dismissing a toast invokes the callback with the notification identifier.
        /// </summary>
        [Test]
        public void VerifyDismissInvokesCallback()
        {
            var dismissedId = string.Empty;

            var component = this.Render<NotificationToastComponent>(parameters => parameters
                .Add(component => component.Notification, new ToastNotification
                {
                    Id = "save-complete",
                    Title = "Saved",
                    Message = "Project changes were saved.",
                    Variant = ToastNotificationVariant.Success
                })
                .Add(component => component.Dismissed, id => dismissedId = id)
                .Add(component => component.Class, "custom-toast")
                .AddUnmatched("data-testid", "toast"));

            component.Find(".mb-notification-toast__close").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dismissedId, Is.EqualTo("save-complete"));
                Assert.That(component.Find("article").GetAttribute("role"), Is.EqualTo("status"));
                Assert.That(component.Find("article").GetAttribute("data-testid"), Is.EqualTo("toast"));
                Assert.That(component.Find("article").GetAttribute("class"), Does.Contain("mb-notification-toast--success"));
                Assert.That(component.Find("article").GetAttribute("class"), Does.Contain("custom-toast"));
                Assert.That(component.Find(".mb-notification-toast__title").TextContent.Trim(), Is.EqualTo("Saved"));
                Assert.That(component.Find(".mb-notification-toast__message").TextContent.Trim(), Is.EqualTo("Project changes were saved."));
                Assert.That(component.Find(".mb-notification-toast__close").GetAttribute("aria-label"), Is.EqualTo("Dismiss Saved"));
            }
        }

        /// <summary>
        /// Verifies that toast variants and non-dismissible state are rendered.
        /// </summary>
        /// <param name="variant">The toast variant.</param>
        /// <param name="expectedCssClass">The expected CSS class.</param>
        [TestCase(ToastNotificationVariant.Info, "mb-notification-toast--info")]
        [TestCase(ToastNotificationVariant.Warning, "mb-notification-toast--warning")]
        [TestCase(ToastNotificationVariant.Danger, "mb-notification-toast--danger")]
        public void VerifyRenderUsesExpectedVariantClass(ToastNotificationVariant variant, string expectedCssClass)
        {
            var component = this.Render<NotificationToastComponent>(parameters => parameters
                .Add(component => component.Notification, new ToastNotification
                {
                    Message = "Notification body",
                    Variant = variant,
                    IsDismissible = false
                }));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("article").GetAttribute("class"), Does.Contain(expectedCssClass));
                Assert.That(component.FindAll(".mb-notification-toast__close"), Is.Empty);
            }
        }
    }
}
