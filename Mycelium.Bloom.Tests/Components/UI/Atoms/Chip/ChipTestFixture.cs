// // ------------------------------------------------------------------------------------------------
// // <copyright file="ChipTestFixture.cs" company="Starion Group S.A.">
// //
// //   Copyright 2026 Starion Group S.A.
// //   SPDX-License-Identifier: Apache-2.0
// //
// // </copyright>
// // ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Atoms.Chip
{
    using Bunit;

    using Mycelium.Bloom.Model.Enum;

    using ChipComponent = Mycelium.Bloom.Components.UI.Atoms.Chip.Chip;

    /// <summary>
    /// Tests the <see cref="ChipComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ChipTestFixture : BunitContext
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
        /// Verifies that a custom color chip displays a color dot and CSS variable.
        /// </summary>
        [Test]
        public void Render_CustomColorDisplaysDotAndStyle()
        {
            var component = this.Render<ChipComponent>(parameters => parameters
                .Add(component => component.Color, "#008577")
                .AddChildContent("Lifecycle"));

            var chip = component.Find(".mb-chip");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(chip.GetAttribute("class"), Does.Contain("mb-chip--custom-color"));
                Assert.That(chip.GetAttribute("style"), Is.EqualTo("--mb-chip-color: #008577;"));
                Assert.That(component.FindAll(".mb-chip__dot"), Has.Count.EqualTo(1));
            }
        }

        /// <summary>
        /// Verifies that the chip displays configured content, classes, and attributes.
        /// </summary>
        [Test]
        public void Render_DisplaysConfiguredChip()
        {
            var component = this.Render<ChipComponent>(parameters => parameters
                .Add(component => component.Variant, ChipVariant.Info)
                .Add(component => component.Class, "custom-chip")
                .AddChildContent("Reviewed")
                .AddUnmatched("data-testid", "status-chip"));

            var chip = component.Find(".mb-chip");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(chip.TextContent.Trim(), Is.EqualTo("Reviewed"));
                Assert.That(chip.GetAttribute("data-testid"), Is.EqualTo("status-chip"));
                Assert.That(chip.GetAttribute("class"), Does.Contain("mb-chip--info"));
                Assert.That(chip.GetAttribute("class"), Does.Contain("custom-chip"));
                Assert.That(component.FindAll(".mb-chip__dot"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies that the chip uses the expected variant class.
        /// </summary>
        /// <param name="variant">The chip variant.</param>
        /// <param name="expectedCssClass">The expected CSS class.</param>
        [TestCase(ChipVariant.Default, "mb-chip--default")]
        [TestCase(ChipVariant.Success, "mb-chip--success")]
        [TestCase(ChipVariant.Warning, "mb-chip--warning")]
        [TestCase(ChipVariant.Danger, "mb-chip--danger")]
        [TestCase(ChipVariant.Info, "mb-chip--info")]
        [TestCase(ChipVariant.Ownership, "mb-chip--ownership")]
        [TestCase(ChipVariant.Lifecycle, "mb-chip--lifecycle")]
        public void Render_UsesExpectedVariantClass(ChipVariant variant, string expectedCssClass)
        {
            var component = this.Render<ChipComponent>(parameters => parameters
                .Add(component => component.Variant, variant)
                .AddChildContent("State"));

            Assert.That(component.Find(".mb-chip").GetAttribute("class"), Does.Contain(expectedCssClass));
        }
    }
}
