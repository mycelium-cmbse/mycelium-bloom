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
    using System.Threading.Tasks;

    using Bunit;

    using Mycelium.Bloom.Tests.Common;

    using BrandMarkComponent = Mycelium.Bloom.Components.UI.Atoms.BrandMark.BrandMark;

    [TestFixture]
    public sealed class BrandMarkTestFixture
    {
        private BunitContext context;

        [SetUp]
        public void SetUp()
        {
            this.context = new BunitContext();
        }

        [TearDown]
        public async Task TearDown()
        {
            await this.context.DisposeAsync();
        }

        [Test]
        public void VerifyRenderUsesDecorativeSvgByDefault()
        {
            var component = this.context.Render<BrandMarkComponent>();
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

        [Test]
        public void VerifyRenderUsesConfiguredAccessibleName()
        {
            var component = this.context.Render<BrandMarkComponent>(parameters => parameters
                .Add(parameter => parameter.AccessibleName, "Bloom"));

            var image = component.Find("img");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(image.GetAttribute("alt"), Is.EqualTo("Bloom"));
                Assert.That(image.HasAttribute("aria-hidden"), Is.False);
            }
        }

        [Test]
        public void VerifySharedSvgAssetAndFaviconReference()
        {
            var projectRoot = Path.Combine(TestRepository.GetRootPath(), "Mycelium.Bloom");
            var svg = File.ReadAllText(Path.Combine(projectRoot, "wwwroot", "brand", "bloom-mark.svg"));
            var app = File.ReadAllText(Path.Combine(projectRoot, "Components", "App.razor"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(svg, Does.Contain("viewBox=\"0 0 22 20\""));
                Assert.That(svg, Does.Contain("fill=\"#0D9488\""));
                Assert.That(svg, Does.Contain("M20.7499 3.55169"));
                Assert.That(app, Does.Contain("type=\"image/svg+xml\""));
                Assert.That(app, Does.Contain("this.Assets[\"brand/bloom-mark.svg\"]"));
            }
        }
    }
}
