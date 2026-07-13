// ------------------------------------------------------------------------------------------------
// <copyright file="LoadingStateTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Molecules.LoadingState
{
    using Bunit;

    using Mycelium.Bloom.Model.Enum;

    using LoadingStateComponent = Mycelium.Bloom.Components.UI.Molecules.LoadingState.LoadingState;
    using SkeletonComponent = Mycelium.Bloom.Components.UI.Atoms.Skeleton.Skeleton;

    /// <summary>
    /// Tests the <see cref="LoadingStateComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class LoadingStateTestFixture : BunitContext
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
        /// Verifies that title, description, and optional custom content are rendered.
        /// </summary>
        [Test]
        public void VerifyConfiguredContentRenders()
        {
            var component = this.Render<LoadingStateComponent>(parameters => parameters
                .Add(component => component.Title, "Loading model")
                .Add(component => component.Description, "Preparing the selected content.")
                .AddChildContent("<span class='custom-loading-content'>Custom content</span>"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-loading-state__title").TextContent, Is.EqualTo("Loading model"));
                Assert.That(component.Find(".mb-loading-state__description").TextContent, Is.EqualTo("Preparing the selected content."));
                Assert.That(component.Find(".custom-loading-content").TextContent, Is.EqualTo("Custom content"));
            }
        }

        /// <summary>
        /// Verifies that spinner and skeleton content remain hidden when disabled.
        /// </summary>
        [Test]
        public void VerifySpinnerAndSkeletonCanBeHidden()
        {
            var component = this.Render<LoadingStateComponent>(parameters => parameters
                .Add(component => component.ShowSpinner, false)
                .Add(component => component.ShowSkeleton, false));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(".mb-loading-state__spinner"), Is.Empty);
                Assert.That(component.FindAll(".mb-loading-state__skeleton"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies that the spinner and custom skeleton content render when enabled.
        /// </summary>
        [Test]
        public void VerifySpinnerAndSkeletonRenderWhenEnabled()
        {
            var component = this.Render<LoadingStateComponent>(parameters => parameters
                .Add(component => component.ShowSpinner, true)
                .Add(component => component.ShowSkeleton, true)
                .Add(component => component.SkeletonContent, "<span class='custom-skeleton'>Placeholder</span>"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(".mb-loading-state__spinner"), Has.Count.EqualTo(1));
                Assert.That(component.Find(".custom-skeleton").TextContent, Is.EqualTo("Placeholder"));
            }
        }

        /// <summary>
        /// Verifies that enabling skeleton content without a custom fragment renders the default placeholders.
        /// </summary>
        [Test]
        public void VerifyDefaultSkeletonContentRenders()
        {
            var component = this.Render<LoadingStateComponent>(parameters => parameters
                .Add(component => component.ShowSkeleton, true));

            var skeletons = component.FindComponents<SkeletonComponent>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(skeletons, Has.Count.EqualTo(2));
                Assert.That(skeletons[0].Instance.Lines, Is.EqualTo(3));
                Assert.That(skeletons[1].Instance.Variant, Is.EqualTo(SkeletonVariant.Rectangle));
                Assert.That(skeletons[1].Instance.Height, Is.EqualTo("72px"));
            }
        }
    }
}
