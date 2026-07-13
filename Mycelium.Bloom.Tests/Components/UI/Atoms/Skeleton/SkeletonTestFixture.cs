// ------------------------------------------------------------------------------------------------
// <copyright file="SkeletonTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Atoms.Skeleton
{
    using Bunit;

    using Mycelium.Bloom.Model.Enum;

    using SkeletonComponent = Mycelium.Bloom.Components.UI.Atoms.Skeleton.Skeleton;

    /// <summary>
    /// Tests the <see cref="SkeletonComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class SkeletonTestFixture : BunitContext
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
        /// Verifies that the selected skeleton variant renders its expected CSS class.
        /// </summary>
        /// <param name="variant">The skeleton variant.</param>
        /// <param name="expectedCssClass">The expected CSS class.</param>
        [TestCase(SkeletonVariant.Text, "mb-skeleton--text")]
        [TestCase(SkeletonVariant.Circle, "mb-skeleton--circle")]
        [TestCase(SkeletonVariant.Rectangle, "mb-skeleton--rectangle")]
        public void VerifySelectedVariantRendersExpectedClass(SkeletonVariant variant, string expectedCssClass)
        {
            var component = this.Render<SkeletonComponent>(parameters => parameters
                .Add(component => component.Variant, variant));

            Assert.That(component.Find(".mb-skeleton").GetAttribute("class"), Does.Contain(expectedCssClass));
        }

        /// <summary>
        /// Verifies that a text skeleton renders the requested number of lines.
        /// </summary>
        [Test]
        public void VerifyTextVariantRendersRequestedLineCount()
        {
            var component = this.Render<SkeletonComponent>(parameters => parameters
                .Add(component => component.Variant, SkeletonVariant.Text)
                .Add(component => component.Lines, 4));

            Assert.That(component.FindAll(".mb-skeleton__item"), Has.Count.EqualTo(4));
        }

        /// <summary>
        /// Verifies that non-text variants render a single placeholder item.
        /// </summary>
        [Test]
        public void VerifyNonTextVariantRendersSingleItem()
        {
            var component = this.Render<SkeletonComponent>(parameters => parameters
                .Add(component => component.Variant, SkeletonVariant.Circle)
                .Add(component => component.Lines, 4));

            Assert.That(component.FindAll(".mb-skeleton__item"), Has.Count.EqualTo(1));
        }

        /// <summary>
        /// Verifies that optional skeleton dimensions are applied to the placeholder item.
        /// </summary>
        [Test]
        public void VerifyOptionalDimensionsAreApplied()
        {
            var component = this.Render<SkeletonComponent>(parameters => parameters
                .Add(component => component.Width, "12rem")
                .Add(component => component.Height, "48px"));

            var style = component.Find(".mb-skeleton__item").GetAttribute("style");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(style, Does.Contain("width: 12rem;"));
                Assert.That(style, Does.Contain("height: 48px;"));
            }
        }
    }
}
