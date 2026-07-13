// ------------------------------------------------------------------------------------------------
// <copyright file="EmptyStateTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Molecules.EmptyState
{
    using Bunit;

    using EmptyStateComponent = Mycelium.Bloom.Components.UI.Molecules.EmptyState.EmptyState;

    /// <summary>
    /// Tests the <see cref="EmptyStateComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class EmptyStateTestFixture : BunitContext
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
        /// Verifies that the configured title and description render.
        /// </summary>
        [Test]
        public void VerifyTitleAndDescriptionRender()
        {
            var component = this.Render<EmptyStateComponent>(parameters => parameters
                .Add(component => component.Title, "No results")
                .Add(component => component.Description, "Try changing the current filters."));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-empty-state__title").TextContent, Is.EqualTo("No results"));
                Assert.That(component.Find(".mb-empty-state__description").TextContent, Is.EqualTo("Try changing the current filters."));
            }
        }

        /// <summary>
        /// Verifies that optional icon and action content render.
        /// </summary>
        [Test]
        public void VerifyOptionalIconAndActionContentRender()
        {
            var component = this.Render<EmptyStateComponent>(parameters => parameters
                .Add(component => component.Title, "Nothing here")
                .Add(component => component.IconContent, "<span class='test-icon'>Icon</span>")
                .Add(component => component.ActionContent, "<button class='test-action'>Create</button>"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".test-icon").TextContent, Is.EqualTo("Icon"));
                Assert.That(component.Find(".test-action").TextContent, Is.EqualTo("Create"));
            }
        }
    }
}
