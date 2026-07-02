// ------------------------------------------------------------------------------------------------
// <copyright file="ZoomControlsTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Molecules.ZoomControls
{
    using Bunit;

    using ZoomControlsComponent = Mycelium.Bloom.Components.UI.Molecules.ZoomControls.ZoomControls;

    /// <summary>
    /// Tests the <see cref="ZoomControlsComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ZoomControlsTestFixture : BunitContext
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
        /// Verifies that zoom buttons clamp values and invoke callbacks.
        /// </summary>
        [Test]
        public void VerifyZoomButtonsInvokeCallbacks()
        {
            var zoomValue = 0;
            var fitToViewCount = 0;

            var component = this.Render<ZoomControlsComponent>(parameters => parameters
                .Add(component => component.ZoomPercentage, 100)
                .Add(component => component.MinZoomPercentage, 80)
                .Add(component => component.MaxZoomPercentage, 120)
                .Add(component => component.StepPercentage, 15)
                .Add(component => component.ZoomPercentageChanged, value => zoomValue = value)
                .Add(component => component.FitToView, () => fitToViewCount++)
                .Add(component => component.Class, "custom-zoom")
                .AddUnmatched("data-testid", "zoom-controls"));

            var buttons = component.FindAll("button");

            buttons[0].Click();
            buttons[1].Click();
            buttons[2].Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(zoomValue, Is.EqualTo(100));
                Assert.That(fitToViewCount, Is.EqualTo(1));
                Assert.That(component.Find(".mb-zoom-controls").GetAttribute("data-testid"), Is.EqualTo("zoom-controls"));
                Assert.That(component.Find(".mb-zoom-controls").GetAttribute("class"), Does.Contain("custom-zoom"));
                Assert.That(component.Find(".mb-zoom-controls__value").TextContent.Trim(), Is.EqualTo("100%"));
            }
        }

        /// <summary>
        /// Verifies that disabled and boundary states disable the expected buttons.
        /// </summary>
        [Test]
        public void VerifyDisabledAndBoundaryStates()
        {
            var disabledComponent = this.Render<ZoomControlsComponent>(parameters => parameters
                .Add(component => component.ZoomPercentage, 25)
                .Add(component => component.Disabled, true));

            var maxComponent = this.Render<ZoomControlsComponent>(parameters => parameters
                .Add(component => component.ZoomPercentage, 300)
                .Add(component => component.MinZoomPercentage, 200)
                .Add(component => component.MaxZoomPercentage, 25)
                .Add(component => component.StepPercentage, 0));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(disabledComponent.Find(".mb-zoom-controls").GetAttribute("class"), Does.Contain("mb-zoom-controls--disabled"));
                Assert.That(disabledComponent.FindAll("button"), Has.All.Matches<AngleSharp.Dom.IElement>(button => button.HasAttribute("disabled")));
                Assert.That(maxComponent.Find(".mb-zoom-controls__value").TextContent.Trim(), Is.EqualTo("200%"));
                Assert.That(maxComponent.FindAll("button")[1].HasAttribute("disabled"), Is.True);
            }
        }
    }
}
