// ------------------------------------------------------------------------------------------------
// <copyright file="CssStyleBuilderTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.Common
{
    using Mycelium.Bloom.Components.Common;

    /// <summary>
    /// Tests the <see cref="CssStyleBuilder" /> helper.
    /// </summary>
    [TestFixture]
    public sealed class CssStyleBuilderTestFixture
    {
        /// <summary>
        /// Verifies that valid declarations are formatted and combined.
        /// </summary>
        [Test]
        public void VerifyBuildCombinesValidDeclarations()
        {
            var style = CssStyleBuilder.Build(
                ("width", "24px"),
                ("--mb-component-color", "var(--mb-color-brand-500)"));

            Assert.That(style, Is.EqualTo("width: 24px; --mb-component-color: var(--mb-color-brand-500);"));
        }

        /// <summary>
        /// Verifies that declarations with empty properties or values are omitted.
        /// </summary>
        [Test]
        public void VerifyBuildOmitsEmptyDeclarations()
        {
            var style = CssStyleBuilder.Build(
                ("width", " "),
                (string.Empty, "24px"),
                ("height", "48px"));

            Assert.That(style, Is.EqualTo("height: 48px;"));
        }

        /// <summary>
        /// Verifies that an empty declaration set produces an empty style.
        /// </summary>
        [Test]
        public void VerifyBuildReturnsEmptyForNoDeclarations()
        {
            Assert.That(CssStyleBuilder.Build(), Is.Empty);
        }
    }
}
