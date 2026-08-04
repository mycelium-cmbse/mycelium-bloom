// ------------------------------------------------------------------------------------------------
// <copyright file="BrandMarkTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Atoms.BrandMark
{
    using System.IO;

    using Bunit;

    using Mycelium.Bloom.Tests.Common;

    using BrandMarkComponent = Mycelium.Bloom.Components.UI.Atoms.BrandMark.BrandMark;

    /// <summary>
    /// Tests the <see cref="BrandMarkComponent" /> component and its shared SVG source.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class BrandMarkTestFixture : BunitContext
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
        /// Verifies that adjacent text can own the accessible brand name without duplicate image output.
        /// </summary>
        [Test]
        public void VerifyRenderUsesDecorativeSvgByDefault()
        {
            var component = this.Render<BrandMarkComponent>();
            var image = component.Find("img");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(image.GetAttribute("src"), Is.EqualTo("brand/bloom-mark.svg"));
                Assert.That(image.GetAttribute("alt"), Is.Empty);
                Assert.That(image.GetAttribute("aria-hidden"), Is.EqualTo("true"));
                Assert.That(image.GetAttribute("data-brand"), Is.EqualTo("bloom"));
                Assert.That(image.GetAttribute("draggable"), Is.EqualTo("false"));
            }
        }

        /// <summary>
        /// Verifies that standalone brand imagery can expose a concise accessible name.
        /// </summary>
        [Test]
        public void VerifyRenderUsesConfiguredAccessibleName()
        {
            var component = this.Render<BrandMarkComponent>(parameters => parameters
                .Add(parameter => parameter.AccessibleName, "Bloom"));

            var image = component.Find("img");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(image.GetAttribute("alt"), Is.EqualTo("Bloom"));
                Assert.That(image.HasAttribute("aria-hidden"), Is.False);
            }
        }

        /// <summary>
        /// Verifies that the supplied vector is the single source used by the component and SVG favicon.
        /// </summary>
        [Test]
        public void VerifySharedSvgAssetAndFaviconReference()
        {
            var projectRoot = Path.Combine(TestRepository.GetRootPath(), "Mycelium.Bloom");
            var svg = File.ReadAllText(Path.Combine(projectRoot, "wwwroot", "brand", "bloom-mark.svg"));
            var app = File.ReadAllText(Path.Combine(projectRoot, "Components", "App.razor"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(svg, Does.Contain("viewBox=\"0 0 475 342\""));
                Assert.That(svg, Does.Contain("fill=\"#0D9488\""));
                Assert.That(svg, Does.Contain("M166.95 0.191986"));
                Assert.That(app, Does.Contain("type=\"image/svg+xml\""));
                Assert.That(app, Does.Contain("this.Assets[\"brand/bloom-mark.svg\"]"));
            }
        }
    }
}
