// ------------------------------------------------------------------------------------------------
// <copyright file="NavMenuTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.Layout
{
    using System;
    using System.IO;

    using Mycelium.Bloom.Tests.Common;

    /// <summary>
    /// Tests the primary navigation source contracts.
    /// </summary>
    [TestFixture]
    public sealed class NavMenuTestFixture
    {
        /// <summary>
        /// Verifies that reduced motion crosses the NavLink component boundary and removes the transition.
        /// </summary>
        [Test]
        public void VerifyReducedMotionTargetsRenderedNavLinks()
        {
            var stylePath = Path.Combine(
                TestRepository.GetRootPath(),
                "Mycelium.Bloom",
                "Components",
                "Layout",
                "NavMenu.razor.css");
            var styles = File.ReadAllText(stylePath);
            var reducedMotionStart = styles.IndexOf("@media (prefers-reduced-motion: reduce)", StringComparison.Ordinal);

            Assert.That(reducedMotionStart, Is.GreaterThanOrEqualTo(0));

            var reducedMotionStyles = styles[reducedMotionStart..];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reducedMotionStyles, Does.Contain(".mb-nav-menu__links ::deep .mb-nav-menu__link"));
                Assert.That(reducedMotionStyles, Does.Contain("transition: none;"));
            }
        }
    }
}
