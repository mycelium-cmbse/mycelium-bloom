// ------------------------------------------------------------------------------------------------
// <copyright file="ToastContainerTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.ToastContainer
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Bunit;

    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.Tests.Common;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    using ToastContainerComponent = Mycelium.Bloom.Components.UI.Organisms.ToastContainer.ToastContainer;

    /// <summary>
    /// Tests the <see cref="ToastContainerComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ToastContainerTestFixture : BunitContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToastContainerTestFixture" /> class.
        /// </summary>
        public ToastContainerTestFixture()
        {
            BlueprintTestSetup.ConfigureWithPortalHost(this);
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
        /// Verifies that the container renders one toast for each supplied notification.
        /// </summary>
        [Test]
        public void VerifyMultipleNotificationsRender()
        {
            var notifications = new[]
            {
                new ToastNotification { Id = "first", Title = "First" },
                new ToastNotification { Id = "second", Title = "Second", Variant = ToastNotificationVariant.Warning }
            };

            var component = this.Render<ToastContainerComponent>(parameters => parameters
                .Add(component => component.Notifications, notifications));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(".mb-toast-container__item"), Has.Count.EqualTo(2));
                Assert.That(component.FindAll("article.mb-notification-toast"), Has.Count.EqualTo(2));
            }
        }

        /// <summary>
        /// Verifies that a child toast dismissal is forwarded with the correct identifier.
        /// </summary>
        [Test]
        public async Task VerifyDismissedNotificationIsForwarded()
        {
            var dismissedIdentifier = string.Empty;
            var notifications = new[]
            {
                new ToastNotification { Id = "first", Title = "First" },
                new ToastNotification { Id = "second", Title = "Second" }
            };

            var component = this.Render<ToastContainerComponent>(parameters => parameters
                .Add(component => component.Notifications, notifications)
                .Add(component => component.Dismissed, (string identifier) => dismissedIdentifier = identifier));

            await component.Find("button[aria-label='Dismiss First']").ClickAsync(new MouseEventArgs());

            Assert.That(dismissedIdentifier, Is.EqualTo("first"));
        }

        /// <summary>
        /// Verifies that an empty collection renders no toast items.
        /// </summary>
        [Test]
        public void VerifyEmptyCollectionRendersNoToastItems()
        {
            var component = this.Render<ToastContainerComponent>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(".mb-toast-container__item"), Is.Empty);
                Assert.That(component.FindAll("article.mb-notification-toast"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies that a null notification collection is rejected before rendering.
        /// </summary>
        [Test]
        public void VerifyNullCollectionIsRejected()
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                this.Render<ToastContainerComponent>(parameters => parameters
                    .Add(component => component.Notifications, default(IReadOnlyList<ToastNotification>))));

            Assert.That(exception.Message, Does.Contain("cannot be null"));
        }

        /// <summary>
        /// Verifies that null notification items are rejected before rendering.
        /// </summary>
        [Test]
        public void VerifyNullNotificationItemIsRejected()
        {
            var notifications = new ToastNotification[] { null };

            var exception = Assert.Throws<InvalidOperationException>(() =>
                this.Render<ToastContainerComponent>(parameters => parameters
                    .Add(component => component.Notifications, notifications)));

            Assert.That(exception.Message, Does.Contain("cannot contain null items"));
        }

        /// <summary>
        /// Verifies that notifications without stable identifiers are rejected before rendering.
        /// </summary>
        [Test]
        public void VerifyNotificationWithoutIdentifierIsRejected()
        {
            var notifications = new[]
            {
                new ToastNotification { Title = "Missing identifier" }
            };

            var exception = Assert.Throws<InvalidOperationException>(() =>
                this.Render<ToastContainerComponent>(parameters => parameters
                    .Add(component => component.Notifications, notifications)));

            Assert.That(exception.Message, Does.Contain("non-empty identifier"));
        }

        /// <summary>
        /// Verifies that duplicate notification identifiers are rejected before keyed rendering.
        /// </summary>
        [Test]
        public void VerifyDuplicateNotificationIdentifiersAreRejected()
        {
            var notifications = new[]
            {
                new ToastNotification { Id = "duplicate", Title = "First" },
                new ToastNotification { Id = "duplicate", Title = "Second" }
            };

            var exception = Assert.Throws<InvalidOperationException>(() =>
                this.Render<ToastContainerComponent>(parameters => parameters
                    .Add(component => component.Notifications, notifications)));

            Assert.That(exception.Message, Does.Contain("must be unique"));
        }
    }
}
