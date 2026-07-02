// ------------------------------------------------------------------------------------------------
// <copyright file="ActivityFeedTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.ActivityFeed
{
    using System.Collections.Generic;

    using Bunit;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    using ActivityFeedComponent = Mycelium.Bloom.Components.UI.Organisms.ActivityFeed.ActivityFeed;

    /// <summary>
    /// Tests the <see cref="ActivityFeedComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ActivityFeedTestFixture : BunitContext
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
        /// Verifies that activity items render configured content and attributes.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysActivityItems()
        {
            var component = this.Render<ActivityFeedComponent>(parameters => parameters
                .Add(component => component.Items, GetItems())
                .Add(component => component.Compact, false)
                .Add(component => component.Class, "custom-feed")
                .AddUnmatched("data-testid", "activity-feed"));

            var feed = component.Find(".mb-activity-feed");
            var items = component.FindAll(".mb-activity-feed__item");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feed.GetAttribute("data-testid"), Is.EqualTo("activity-feed"));
                Assert.That(feed.GetAttribute("class"), Does.Contain("custom-feed"));
                Assert.That(feed.GetAttribute("class"), Does.Not.Contain("mb-activity-feed--compact"));
                Assert.That(items, Has.Count.EqualTo(2));
                Assert.That(component.Find(".mb-activity-feed__title").TextContent.Trim(), Is.EqualTo("Element updated"));
                Assert.That(component.Find(".mb-activity-feed__timestamp").TextContent.Trim(), Is.EqualTo("5 min ago"));
                Assert.That(component.Find(".mb-activity-feed__description").TextContent.Trim(), Is.EqualTo("Updated requirement text."));
                Assert.That(component.Find(".mb-activity-feed__actor").TextContent.Trim(), Is.EqualTo("Model Reviewer"));
                Assert.That(component.Find(".mb-activity-feed__target").TextContent.Trim(), Is.EqualTo("Power budget"));
                Assert.That(component.Find(".mb-activity-feed__qualified-name").TextContent.Trim(), Is.EqualTo("System::Power::Budget"));
                Assert.That(component.FindAll(".mb-activity-feed__avatar"), Has.Count.EqualTo(1));
            }
        }

        /// <summary>
        /// Verifies that an empty activity feed renders the configured empty text.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysEmptyState()
        {
            var component = this.Render<ActivityFeedComponent>(parameters => parameters
                .Add(component => component.EmptyText, "No project activity."));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-activity-feed").GetAttribute("class"), Does.Contain("mb-activity-feed--compact"));
                Assert.That(component.Find(".mb-activity-feed__empty").TextContent.Trim(), Is.EqualTo("No project activity."));
                Assert.That(component.FindAll(".mb-activity-feed__item"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies that activity variants render the expected marker class.
        /// </summary>
        /// <param name="variant">The activity variant.</param>
        /// <param name="expectedCssClass">The expected marker CSS class.</param>
        [TestCase(ActivityFeedItemVariant.Created, "mb-activity-feed__marker--created")]
        [TestCase(ActivityFeedItemVariant.Updated, "mb-activity-feed__marker--updated")]
        [TestCase(ActivityFeedItemVariant.Deleted, "mb-activity-feed__marker--deleted")]
        [TestCase(ActivityFeedItemVariant.Commented, "mb-activity-feed__marker--commented")]
        [TestCase(ActivityFeedItemVariant.Reviewed, "mb-activity-feed__marker--reviewed")]
        [TestCase(ActivityFeedItemVariant.Synced, "mb-activity-feed__marker--synced")]
        [TestCase(ActivityFeedItemVariant.Joined, "mb-activity-feed__marker--joined")]
        [TestCase(ActivityFeedItemVariant.Left, "mb-activity-feed__marker--left")]
        [TestCase(ActivityFeedItemVariant.Neutral, "mb-activity-feed__marker--neutral")]
        public void VerifyRenderUsesExpectedVariantMarker(ActivityFeedItemVariant variant, string expectedCssClass)
        {
            var component = this.Render<ActivityFeedComponent>(parameters => parameters
                .Add(component => component.Items, new[]
                {
                    new ActivityFeedItem
                    {
                        Id = "activity",
                        Title = "Activity",
                        ActorName = "Model Reviewer",
                        TimestampText = "Now",
                        Variant = variant
                    }
                }));

            Assert.That(component.Find(".mb-activity-feed__marker").GetAttribute("class"), Does.Contain(expectedCssClass));
        }

        /// <summary>
        /// Gets sample activity feed items.
        /// </summary>
        /// <returns>The sample activity feed items.</returns>
        private static IReadOnlyList<ActivityFeedItem> GetItems()
        {
            return
            [
                new()
                {
                    Id = "updated",
                    Title = "Element updated",
                    Description = "Updated requirement text.",
                    ActorName = "Model Reviewer",
                    ActorInitials = "MR",
                    ActorColor = "#123456",
                    TimestampText = "5 min ago",
                    TargetName = "Power budget",
                    TargetQualifiedName = "System::Power::Budget",
                    Variant = ActivityFeedItemVariant.Updated
                },
                new()
                {
                    Id = "synced",
                    Title = "Model synchronized",
                    ActorName = "Project Lead",
                    TimestampText = "1 min ago",
                    Variant = ActivityFeedItemVariant.Synced
                }
            ];
        }
    }
}
