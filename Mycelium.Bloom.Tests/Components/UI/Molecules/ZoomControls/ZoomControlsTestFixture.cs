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
    using System.Collections.Generic;
    using System.Linq;

    using BlazorBlueprint.Components;
    using BlazorBlueprint.Primitives.Services;

    using Bunit;

    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.Tests.Common;

    using ZoomControlsComponent = Mycelium.Bloom.Components.UI.Molecules.ZoomControls.ZoomControls;

    /// <summary>
    /// Tests the <see cref="ZoomControlsComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ZoomControlsTestFixture : BunitContext
    {
        /// <summary>
        /// The expected controlled zoom requests.
        /// </summary>
        private static readonly double[] ExpectedZoomRequests = [150d, 100d];

        private readonly IRenderedComponent<BbPortalHost> portalHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZoomControlsTestFixture" /> class.
        /// </summary>
        public ZoomControlsTestFixture()
        {
            this.portalHost = BlueprintTestSetup.ConfigureWithPortalHost(this);
        }

        /// <summary>
        /// Disposes the bUnit test context after each test.
        /// </summary>
        [TearDown]
        public System.Threading.Tasks.Task TearDown()
        {
            return this.DisposeAsync().AsTask();
        }

        /// <summary>
        /// Verifies the current value and bounded zoom requests use the controlled API.
        /// </summary>
        [Test]
        public void VerifyZoomActionsRequestControlledValues()
        {
            var requestedZooms = new List<double>();
            var component = this.Render<ZoomControlsComponent>(parameters => parameters
                .Add(component => component.Zoom, 125d)
                .Add(component => component.MinimumZoom, 50d)
                .Add(component => component.MaximumZoom, 200d)
                .Add(component => component.ZoomStep, 25d)
                .Add(component => component.ZoomChanged, value => requestedZooms.Add(value)));

            component.Find("button[aria-label='Zoom in']").Click();
            component.Find("button[aria-label='Zoom out']").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("output").TextContent.Trim(), Is.EqualTo("125%"));
                Assert.That(requestedZooms, Is.EqualTo(ExpectedZoomRequests));
                Assert.That(component.Find("[role='toolbar']").GetAttribute("aria-label"),
                    Is.EqualTo("Canvas zoom controls"));
            }
        }

        /// <summary>
        /// Verifies minimum and maximum limits disable their corresponding actions.
        /// </summary>
        [Test]
        public async System.Threading.Tasks.Task VerifyMinimumAndMaximumDisableActions()
        {
            var callbackCount = 0;
            var component = this.Render<ZoomControlsComponent>(parameters => parameters
                .Add(component => component.Zoom, 50d)
                .Add(component => component.MinimumZoom, 50d)
                .Add(component => component.MaximumZoom, 150d)
                .Add(component => component.ZoomChanged, _ => callbackCount++));

            var zoomOut = component.Find("button[aria-label='Zoom out']");
            var zoomOutComponent = component.FindComponents<BbButton>()
                .Single(button => button.Instance.AriaLabel == "Zoom out");

            await component.InvokeAsync(() => zoomOutComponent.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            Assert.That(zoomOut.HasAttribute("disabled"), Is.True);

            component.Render(parameters => parameters.Add(component => component.Zoom, 150d));

            var zoomIn = component.Find("button[aria-label='Zoom in']");
            var zoomInComponent = component.FindComponents<BbButton>()
                .Single(button => button.Instance.AriaLabel == "Zoom in");

            await component.InvokeAsync(() => zoomInComponent.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(zoomIn.HasAttribute("disabled"), Is.True);
                Assert.That(callbackCount, Is.Zero);
            }
        }

        /// <summary>
        /// Verifies every icon action keeps an explicit name and pointer hint without mounting a Tooltip.
        /// </summary>
        [Test]
        public void VerifyActionsRemainNamedWithoutTooltips()
        {
            var component = this.Render<ZoomControlsComponent>();
            var buttons = component.FindAll("button");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(buttons, Has.Count.EqualTo(4));
                Assert.That(buttons.All(button => !string.IsNullOrWhiteSpace(button.GetAttribute("aria-label"))), Is.True);
                Assert.That(buttons.All(button =>
                    string.Equals(
                        button.GetAttribute("title"),
                        button.GetAttribute("aria-label"),
                        System.StringComparison.Ordinal)), Is.True);
                Assert.That(component.FindAll("[role='tooltip']"), Is.Empty);
                Assert.That(this.portalHost.FindAll("[role='tooltip']"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies reset and fit actions are forwarded and disabled controls invoke nothing.
        /// </summary>
        [Test]
        public void VerifyResetAndFitActionsRespectDisabledState()
        {
            var resetCount = 0;
            var fitCount = 0;
            var component = this.Render<ZoomControlsComponent>(parameters => parameters
                .Add(component => component.OnResetZoom, () => resetCount++)
                .Add(component => component.OnFitToView, () => fitCount++));

            component.Find("button[aria-label='Reset zoom']").Click();
            component.Find("button[aria-label='Fit to view']").Click();

            component.Render(parameters => parameters.Add(component => component.Disabled, true));
            component.Find("button[aria-label='Reset zoom']").Click();
            component.Find("button[aria-label='Fit to view']").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resetCount, Is.EqualTo(1));
                Assert.That(fitCount, Is.EqualTo(1));
                Assert.That(component.FindAll("button[disabled]"), Has.Count.EqualTo(4));
            }
        }

        /// <summary>
        /// Verifies invalid ranges and values normalize to safe percentages.
        /// </summary>
        [Test]
        public void VerifyInvalidValuesNormalizeSafely()
        {
            var component = this.Render<ZoomControlsComponent>(parameters => parameters
                .Add(component => component.Zoom, double.NaN)
                .Add(component => component.MinimumZoom, 150d)
                .Add(component => component.MaximumZoom, 50d)
                .Add(component => component.ZoomStep, -5d));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("output").TextContent.Trim(), Is.EqualTo("150%"));
                Assert.That(component.Find("button[aria-label='Zoom out']").HasAttribute("disabled"), Is.True);
                Assert.That(component.Find("button[aria-label='Zoom in']").HasAttribute("disabled"), Is.True);
            }
        }

        /// <summary>
        /// Verifies separate zoom-control instances return requests through their own callbacks.
        /// </summary>
        [Test]
        public void VerifyInstancesRemainIndependent()
        {
            var firstRequest = 0d;
            var secondRequest = 0d;
            var first = this.Render<ZoomControlsComponent>(parameters => parameters
                .Add(component => component.Zoom, 100d)
                .Add(component => component.ZoomStep, 10d)
                .Add(component => component.ZoomChanged, value => firstRequest = value));
            var second = this.Render<ZoomControlsComponent>(parameters => parameters
                .Add(component => component.Zoom, 200d)
                .Add(component => component.ZoomStep, 25d)
                .Add(component => component.ZoomChanged, value => secondRequest = value));

            first.Find("button[aria-label='Zoom in']").Click();
            second.Find("button[aria-label='Zoom out']").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstRequest, Is.EqualTo(110d));
                Assert.That(secondRequest, Is.EqualTo(175d));
            }
        }
    }
}
