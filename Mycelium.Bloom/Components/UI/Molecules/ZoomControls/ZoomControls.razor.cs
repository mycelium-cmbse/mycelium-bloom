// ------------------------------------------------------------------------------------------------
// <copyright file="ZoomControls.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Molecules.ZoomControls
{
    using System.Globalization;

    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Common;

    /// <summary>
    /// Represents controlled actions for changing a parent-owned canvas zoom value.
    /// </summary>
    public partial class ZoomControls : BloomComponentBase
    {
        /// <summary>
        /// The fallback minimum zoom percentage.
        /// </summary>
        private const double DefaultMinimumZoom = 10d;

        /// <summary>
        /// The fallback maximum zoom percentage.
        /// </summary>
        private const double DefaultMaximumZoom = 400d;

        /// <summary>
        /// The fallback zoom step percentage.
        /// </summary>
        private const double DefaultZoomStep = 10d;

        /// <summary>
        /// Gets or sets the accessible label of the zoom toolbar.
        /// </summary>
        [Parameter]
        public string AriaLabel { get; set; } = "Canvas zoom controls";

        /// <summary>
        /// Gets or sets the parent-owned zoom percentage.
        /// </summary>
        [Parameter]
        public double Zoom { get; set; } = 100d;

        /// <summary>
        /// Gets or sets the callback invoked with a requested zoom percentage.
        /// </summary>
        [Parameter]
        public EventCallback<double> ZoomChanged { get; set; }

        /// <summary>
        /// Gets or sets the minimum supported zoom percentage.
        /// </summary>
        [Parameter]
        public double MinimumZoom { get; set; } = DefaultMinimumZoom;

        /// <summary>
        /// Gets or sets the maximum supported zoom percentage.
        /// </summary>
        [Parameter]
        public double MaximumZoom { get; set; } = DefaultMaximumZoom;

        /// <summary>
        /// Gets or sets the amount requested by each zoom step.
        /// </summary>
        [Parameter]
        public double ZoomStep { get; set; } = DefaultZoomStep;

        /// <summary>
        /// Gets or sets the callback invoked when reset is requested.
        /// </summary>
        [Parameter]
        public EventCallback OnResetZoom { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when fit-to-view is requested.
        /// </summary>
        [Parameter]
        public EventCallback OnFitToView { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether every zoom action is disabled.
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether compact spacing is used.
        /// </summary>
        [Parameter]
        public bool Compact { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the current zoom value is displayed.
        /// </summary>
        [Parameter]
        public bool ShowZoomValue { get; set; } = true;

        /// <summary>
        /// Gets the final CSS class list applied to the zoom-controls root.
        /// </summary>
        /// <returns>The zoom-controls CSS class list.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass(
                "mb-zoom-controls",
                CssClassBuilder.When("mb-zoom-controls--compact", this.Compact),
                CssClassBuilder.When("mb-zoom-controls--disabled", this.Disabled));
        }

        /// <summary>
        /// Gets a normalized percentage string for display.
        /// </summary>
        /// <returns>The current zoom formatted as a percentage.</returns>
        private string GetFormattedZoom()
        {
            return $"{this.GetEffectiveZoom().ToString("0.#", CultureInfo.InvariantCulture)}%";
        }

        /// <summary>
        /// Gets a value indicating whether zooming out is unavailable.
        /// </summary>
        /// <returns>True when zoom-out should be disabled; otherwise, false.</returns>
        private bool IsZoomOutDisabled()
        {
            return this.Disabled || this.GetEffectiveZoom() <= this.GetEffectiveMinimumZoom();
        }

        /// <summary>
        /// Gets a value indicating whether zooming in is unavailable.
        /// </summary>
        /// <returns>True when zoom-in should be disabled; otherwise, false.</returns>
        private bool IsZoomInDisabled()
        {
            return this.Disabled || this.GetEffectiveZoom() >= this.GetEffectiveMaximumZoom();
        }

        /// <summary>
        /// Requests the next lower controlled zoom value.
        /// </summary>
        /// <param name="args">The source mouse event arguments.</param>
        /// <returns>A task representing the asynchronous callback.</returns>
        private async Task HandleZoomOutAsync(MouseEventArgs args)
        {
            if (this.IsZoomOutDisabled())
            {
                return;
            }

            await this.ZoomChanged.InvokeAsync(this.GetRequestedZoom(-this.GetEffectiveZoomStep()));
        }

        /// <summary>
        /// Requests the next higher controlled zoom value.
        /// </summary>
        /// <param name="args">The source mouse event arguments.</param>
        /// <returns>A task representing the asynchronous callback.</returns>
        private async Task HandleZoomInAsync(MouseEventArgs args)
        {
            if (this.IsZoomInDisabled())
            {
                return;
            }

            await this.ZoomChanged.InvokeAsync(this.GetRequestedZoom(this.GetEffectiveZoomStep()));
        }

        /// <summary>
        /// Forwards an enabled reset request to the parent.
        /// </summary>
        /// <param name="args">The source mouse event arguments.</param>
        /// <returns>A task representing the asynchronous callback.</returns>
        private async Task HandleResetZoomAsync(MouseEventArgs args)
        {
            if (!this.Disabled)
            {
                await this.OnResetZoom.InvokeAsync();
            }
        }

        /// <summary>
        /// Forwards an enabled fit-to-view request to the parent.
        /// </summary>
        /// <param name="args">The source mouse event arguments.</param>
        /// <returns>A task representing the asynchronous callback.</returns>
        private async Task HandleFitToViewAsync(MouseEventArgs args)
        {
            if (!this.Disabled)
            {
                await this.OnFitToView.InvokeAsync();
            }
        }

        /// <summary>
        /// Gets a bounded zoom request from the current controlled value and requested delta.
        /// </summary>
        /// <param name="delta">The signed zoom delta.</param>
        /// <returns>The requested zoom percentage.</returns>
        private double GetRequestedZoom(double delta)
        {
            var requestedZoom = this.GetEffectiveZoom() + delta;

            return Math.Clamp(
                requestedZoom,
                this.GetEffectiveMinimumZoom(),
                this.GetEffectiveMaximumZoom());
        }

        /// <summary>
        /// Gets the safe minimum zoom percentage.
        /// </summary>
        /// <returns>The effective minimum zoom.</returns>
        private double GetEffectiveMinimumZoom()
        {
            return double.IsFinite(this.MinimumZoom) && this.MinimumZoom > 0d
                ? this.MinimumZoom
                : DefaultMinimumZoom;
        }

        /// <summary>
        /// Gets the safe maximum zoom percentage.
        /// </summary>
        /// <returns>The effective maximum zoom.</returns>
        private double GetEffectiveMaximumZoom()
        {
            var minimumZoom = this.GetEffectiveMinimumZoom();
            var maximumZoom = double.IsFinite(this.MaximumZoom) && this.MaximumZoom > 0d
                ? this.MaximumZoom
                : DefaultMaximumZoom;

            return Math.Max(minimumZoom, maximumZoom);
        }

        /// <summary>
        /// Gets the safe positive zoom step.
        /// </summary>
        /// <returns>The effective zoom step.</returns>
        private double GetEffectiveZoomStep()
        {
            return double.IsFinite(this.ZoomStep) && this.ZoomStep > 0d
                ? this.ZoomStep
                : DefaultZoomStep;
        }

        /// <summary>
        /// Gets the current zoom normalized into the effective supported range.
        /// </summary>
        /// <returns>The normalized zoom percentage.</returns>
        private double GetEffectiveZoom()
        {
            var zoom = double.IsFinite(this.Zoom) ? this.Zoom : 100d;

            return Math.Clamp(
                zoom,
                this.GetEffectiveMinimumZoom(),
                this.GetEffectiveMaximumZoom());
        }
    }
}
