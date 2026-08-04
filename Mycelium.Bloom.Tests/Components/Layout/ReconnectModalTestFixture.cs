// ------------------------------------------------------------------------------------------------
// <copyright file="ReconnectModalTestFixture.cs" company="Starion Group S.A.">
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

    using Bunit;

    using Mycelium.Bloom.Components.Layout;
    using Mycelium.Bloom.Tests.Common;

    /// <summary>
    /// Tests the <see cref="ReconnectModal" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ReconnectModalTestFixture : BunitContext
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
        /// Verifies that the reconnect modal displays the expected reconnect states.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysReconnectStates()
        {
            var component = this.Render<ReconnectModal>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#components-reconnect-modal"), Is.Not.Null);
                Assert.That(component.Markup, Does.Contain("Rejoining the server..."));
                Assert.That(component.Markup, Does.Contain("Failed to rejoin."));
                Assert.That(component.Markup, Does.Contain("The session has been paused by the server."));
            }
        }

        /// <summary>
        /// Verifies reduced motion disables modal movement and leaves a static reconnect indicator visible.
        /// </summary>
        [Test]
        public void VerifyReducedMotionDisablesReconnectMovement()
        {
            var style = File.ReadAllText(Path.Combine(
                TestRepository.GetRootPath(),
                "Mycelium.Bloom",
                "Components",
                "Layout",
                "ReconnectModal.razor.css"));
            var reducedMotionStart = style.IndexOf("@media (prefers-reduced-motion: reduce)", StringComparison.Ordinal);

            Assert.That(reducedMotionStart, Is.GreaterThanOrEqualTo(0));

            var reducedMotionStyle = style[reducedMotionStart..];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reducedMotionStyle, Does.Contain("#components-reconnect-modal[open]"));
                Assert.That(reducedMotionStyle, Does.Contain("#components-reconnect-modal::backdrop"));
                Assert.That(reducedMotionStyle, Does.Contain(".components-rejoining-animation div"));
                Assert.That(reducedMotionStyle, Does.Contain("animation: none;"));
                Assert.That(reducedMotionStyle, Does.Contain("transform: none;"));
                Assert.That(reducedMotionStyle, Does.Contain("transition: none;"));
                Assert.That(reducedMotionStyle, Does.Contain("opacity: 1;"));
                Assert.That(reducedMotionStyle, Does.Contain("width: 80px;"));
                Assert.That(reducedMotionStyle, Does.Contain("height: 80px;"));
            }
        }
    }
}
