// ------------------------------------------------------------------------------------------------
// <copyright file="CanvasToolbarTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.CanvasToolbar
{
    using System.Collections.Generic;

    using Bunit;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    using CanvasToolbarComponent = Mycelium.Bloom.Components.UI.Organisms.CanvasToolbar.CanvasToolbar;

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
        /// Verifies that toolbar interactions invoke their callbacks.
        /// </summary>
        [Test]
        public void VerifyToolbarActionsInvokeCallbacks()
        {
            var selectedBreadcrumb = string.Empty;
            var selectedTool = CanvasTool.Select;
            var zoomPercentage = 0;
            var fitToViewCount = 0;

            var component = this.Render<CanvasToolbarComponent>(parameters => parameters
                .Add(component => component.BreadcrumbItems, GetBreadcrumbs())
                .Add(component => component.BreadcrumbSelected, value => selectedBreadcrumb = value)
                .Add(component => component.ActiveTool, CanvasTool.Select)
                .Add(component => component.ActiveToolChanged, tool => selectedTool = tool)
                .Add(component => component.ZoomPercentage, 100)
                .Add(component => component.ZoomPercentageChanged, value => zoomPercentage = value)
                .Add(component => component.FitToView, () => fitToViewCount++)
                .Add(component => component.EndContent, "<span>End tools</span>")
                .Add(component => component.Class, "custom-toolbar")
                .AddUnmatched("data-testid", "canvas-toolbar"));

            component.Find(".mb-breadcrumbs__button").Click();
            component.Find("button[aria-label='Pan tool']").Click();
            component.Find("button[aria-label='Zoom in']").Click();
            component.Find("button[aria-label='Fit canvas to view']").Click();

            var toolbar = component.Find(".mb-canvas-toolbar");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectedBreadcrumb, Is.EqualTo("package"));
                Assert.That(selectedTool, Is.EqualTo(CanvasTool.Pan));
                Assert.That(zoomPercentage, Is.EqualTo(110));
                Assert.That(fitToViewCount, Is.EqualTo(1));
                Assert.That(toolbar.GetAttribute("data-testid"), Is.EqualTo("canvas-toolbar"));
                Assert.That(toolbar.GetAttribute("class"), Does.Contain("custom-toolbar"));
                Assert.That(component.Find("button[aria-label='Pan tool']").GetAttribute("aria-pressed"), Is.EqualTo("true"));
                Assert.That(component.Find("button[aria-label='Pan tool']").TextContent.Trim(), Is.EqualTo("P"));
                Assert.That(component.Find("button[aria-label='Inspect tool']").TextContent.Trim(), Is.EqualTo("I"));
                Assert.That(component.Find("button[aria-label='Select tool']").TextContent.Trim(), Is.EqualTo("S"));
                Assert.That(component.Find(".mb-canvas-toolbar__end").TextContent, Does.Contain("End tools"));
            }
        }

        /// <summary>
        /// Verifies that custom start content replaces breadcrumbs.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysCustomStartContent()
        {
            var component = this.Render<CanvasToolbarComponent>(parameters => parameters
                .Add(component => component.BreadcrumbItems, GetBreadcrumbs())
                .Add(component => component.StartContent, "<span>Custom start</span>"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-canvas-toolbar__start").TextContent.Trim(), Is.EqualTo("Custom start"));
                Assert.That(component.FindAll(".mb-breadcrumbs"), Is.Empty);
            }
        }

        /// <summary>
        /// Gets sample breadcrumb items.
        /// </summary>
        /// <returns>The sample breadcrumb items.</returns>
        private static IReadOnlyList<BreadcrumbItem> GetBreadcrumbs()
        {
            return
            [
                new() { Value = "package", Label = "Package" },
                new() { Value = "element", Label = "Element", IsCurrent = true }
            ];
        }
    }
}
