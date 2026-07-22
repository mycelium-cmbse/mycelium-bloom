// ------------------------------------------------------------------------------------------------
// <copyright file="CanvasToolbarTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Molecules.CanvasToolbar
{
    using Bunit;

    using Mycelium.Bloom.Model.Enum;

    using CanvasToolbarComponent = Mycelium.Bloom.Components.UI.Molecules.CanvasToolbar.CanvasToolbar;

    /// <summary>
    /// Tests the <see cref="CanvasToolbarComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class CanvasToolbarTestFixture : BunitContext
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
        /// Verifies supplied content, vertical orientation, compact state, and toolbar reuse.
        /// </summary>
        [Test]
        public void VerifyConfiguredToolbarRenders()
        {
            var component = this.Render<CanvasToolbarComponent>(parameters => parameters
                .Add(component => component.ChildContent, "<button>Selection</button>")
                .Add(component => component.Orientation, ToolbarOrientation.Vertical)
                .Add(component => component.Compact, true)
                .Add(component => component.AriaLabel, "Diagram tools")
                .Add(component => component.Class, "custom-canvas-toolbar")
                .AddUnmatched("data-testid", "canvas-toolbar"));

            var root = component.Find(".mb-canvas-toolbar");
            var toolbar = component.Find("[role='toolbar']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root.ClassList, Does.Contain("mb-canvas-toolbar--vertical"));
                Assert.That(root.ClassList, Does.Contain("mb-canvas-toolbar--compact"));
                Assert.That(root.ClassList, Does.Contain("custom-canvas-toolbar"));
                Assert.That(root.GetAttribute("data-testid"), Is.EqualTo("canvas-toolbar"));
                Assert.That(toolbar.ClassList, Does.Contain("mb-toolbar"));
                Assert.That(toolbar.ClassList, Does.Contain("mb-toolbar--compact"));
                Assert.That(toolbar.GetAttribute("aria-label"), Is.EqualTo("Diagram tools"));
                Assert.That(toolbar.GetAttribute("aria-orientation"), Is.EqualTo("vertical"));
                Assert.That(component.Find(".mb-toolbar__main").TextContent.Trim(), Is.EqualTo("Selection"));
            }
        }

        /// <summary>
        /// Verifies horizontal orientation is the default presentation.
        /// </summary>
        [Test]
        public void VerifyHorizontalOrientationIsDefault()
        {
            var component = this.Render<CanvasToolbarComponent>(parameters => parameters
                .Add(component => component.ChildContent, "<span>Tools</span>"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-canvas-toolbar").ClassList,
                    Does.Not.Contain("mb-canvas-toolbar--vertical"));
                Assert.That(component.Find("[role='toolbar']").GetAttribute("aria-orientation"),
                    Is.EqualTo("horizontal"));
            }
        }
    }
}
