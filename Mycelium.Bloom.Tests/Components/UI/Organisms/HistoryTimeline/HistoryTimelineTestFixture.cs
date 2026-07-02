// ------------------------------------------------------------------------------------------------
// <copyright file="HistoryTimelineTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.HistoryTimeline
{
    using System.Collections.Generic;

    using Bunit;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    using HistoryTimelineComponent = Mycelium.Bloom.Components.UI.Organisms.HistoryTimeline.HistoryTimeline;

    /// <summary>
    /// Tests the <see cref="HistoryTimelineComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class HistoryTimelineTestFixture : BunitContext
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
        /// Verifies that history items render configured content and attributes.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysHistoryItems()
        {
            var component = this.Render<HistoryTimelineComponent>(parameters => parameters
                .Add(component => component.Items, GetItems())
                .Add(component => component.Compact, false)
                .Add(component => component.Class, "custom-history")
                .AddUnmatched("data-testid", "history-timeline"));

            var timeline = component.Find(".mb-history-timeline");
            var items = component.FindAll(".mb-history-timeline__item");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(timeline.GetAttribute("data-testid"), Is.EqualTo("history-timeline"));
                Assert.That(timeline.GetAttribute("class"), Does.Contain("custom-history"));
                Assert.That(timeline.GetAttribute("class"), Does.Not.Contain("mb-history-timeline--compact"));
                Assert.That(items, Has.Count.EqualTo(2));
                Assert.That(component.Find(".mb-history-timeline__title").TextContent.Trim(), Is.EqualTo("Created element"));
                Assert.That(component.Find(".mb-history-timeline__timestamp").TextContent.Trim(), Is.EqualTo("Yesterday"));
                Assert.That(component.Find(".mb-history-timeline__description").TextContent.Trim(), Is.EqualTo("Initial requirement added."));
                Assert.That(component.Find(".mb-history-timeline__actor-name").TextContent.Trim(), Is.EqualTo("Model Reviewer"));
            }
        }

        /// <summary>
        /// Verifies that an empty history timeline renders the configured empty text.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysEmptyState()
        {
            var component = this.Render<HistoryTimelineComponent>(parameters => parameters
                .Add(component => component.EmptyText, "No element history."));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-history-timeline").GetAttribute("class"), Does.Contain("mb-history-timeline--compact"));
                Assert.That(component.Find(".mb-history-timeline__empty").TextContent.Trim(), Is.EqualTo("No element history."));
                Assert.That(component.FindAll(".mb-history-timeline__item"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies that history variants render the expected marker class.
        /// </summary>
        /// <param name="variant">The history variant.</param>
        /// <param name="expectedCssClass">The expected marker CSS class.</param>
        [TestCase(HistoryTimelineItemVariant.Created, "mb-history-timeline__marker--created")]
        [TestCase(HistoryTimelineItemVariant.Updated, "mb-history-timeline__marker--updated")]
        [TestCase(HistoryTimelineItemVariant.Deleted, "mb-history-timeline__marker--deleted")]
        [TestCase(HistoryTimelineItemVariant.Commented, "mb-history-timeline__marker--commented")]
        [TestCase(HistoryTimelineItemVariant.Reviewed, "mb-history-timeline__marker--reviewed")]
        [TestCase(HistoryTimelineItemVariant.Synced, "mb-history-timeline__marker--synced")]
        [TestCase(HistoryTimelineItemVariant.Neutral, "mb-history-timeline__marker--neutral")]
        public void VerifyRenderUsesExpectedVariantMarker(HistoryTimelineItemVariant variant, string expectedCssClass)
        {
            var component = this.Render<HistoryTimelineComponent>(parameters => parameters
                .Add(component => component.Items, new[]
                {
                    new HistoryTimelineItem
                    {
                        Id = "history",
                        Title = "History",
                        ActorName = "Model Reviewer",
                        TimestampText = "Now",
                        Variant = variant
                    }
                }));

            Assert.That(component.Find(".mb-history-timeline__marker").GetAttribute("class"), Does.Contain(expectedCssClass));
        }

        /// <summary>
        /// Gets sample history timeline items.
        /// </summary>
        /// <returns>The sample history timeline items.</returns>
        private static IReadOnlyList<HistoryTimelineItem> GetItems()
        {
            return
            [
                new()
                {
                    Id = "created",
                    Title = "Created element",
                    Description = "Initial requirement added.",
                    ActorName = "Model Reviewer",
                    ActorInitials = "MR",
                    ActorColor = "#123456",
                    TimestampText = "Yesterday",
                    Variant = HistoryTimelineItemVariant.Created
                },
                new()
                {
                    Id = "reviewed",
                    Title = "Reviewed element",
                    ActorName = "Project Lead",
                    TimestampText = "Today",
                    Variant = HistoryTimelineItemVariant.Reviewed
                }
            ];
        }
    }
}
