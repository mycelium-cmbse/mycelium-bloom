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
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;

    /// <summary>
    /// Reusable compact zoom controls for workspace canvas surfaces.
    /// </summary>
    public partial class ZoomControls : ComponentBase
    {
        /// <summary>
        /// Gets or sets the current zoom percentage.
        /// </summary>
        [Parameter]
        public int ZoomPercentage { get; set; } = 100;

        /// <summary>
        /// Gets or sets the minimum allowed zoom percentage.
        /// </summary>
        [Parameter]
        public int MinZoomPercentage { get; set; } = 25;

        /// <summary>
        /// Gets or sets the maximum allowed zoom percentage.
        /// </summary>
        [Parameter]
        public int MaxZoomPercentage { get; set; } = 200;

        /// <summary>
        /// Gets or sets the amount zoom controls change the zoom percentage.
        /// </summary>
        [Parameter]
        public int StepPercentage { get; set; } = 10;

        /// <summary>
        /// Gets or sets the callback invoked when the zoom percentage changes.
        /// </summary>
        [Parameter]
        public EventCallback<int> ZoomPercentageChanged { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when fit-to-view is selected.
        /// </summary>
        [Parameter]
        public EventCallback FitToView { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether all zoom controls are disabled.
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets additional CSS classes.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets unmatched attributes passed to the controls container.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the final CSS class list applied to the zoom controls.
        /// </summary>
        /// <returns>The zoom controls CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-zoom-controls",
                CssClassBuilder.When("mb-zoom-controls--disabled", this.Disabled),
                this.Class);

            return cssClass;
        }

        /// <summary>
        /// Gets the formatted zoom label.
        /// </summary>
        /// <returns>The formatted zoom percentage label.</returns>
        private string GetZoomLabel()
        {
            return $"{this.GetClampedZoomPercentage()}%";
        }

        /// <summary>
        /// Gets a value indicating whether zooming out is disabled.
        /// </summary>
        /// <returns>A value indicating whether the zoom out button is disabled.</returns>
        private bool IsZoomOutDisabled()
        {
            return this.Disabled || this.GetClampedZoomPercentage() <= this.GetMinimumZoomPercentage();
        }

        /// <summary>
        /// Gets a value indicating whether zooming in is disabled.
        /// </summary>
        /// <returns>A value indicating whether the zoom in button is disabled.</returns>
        private bool IsZoomInDisabled()
        {
            return this.Disabled || this.GetClampedZoomPercentage() >= this.GetMaximumZoomPercentage();
        }

        /// <summary>
        /// Decreases the zoom percentage.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ZoomOutAsync()
        {
            await this.SetZoomPercentageAsync(this.ZoomPercentage - this.GetStepPercentage());
        }

        /// <summary>
        /// Increases the zoom percentage.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ZoomInAsync()
        {
            await this.SetZoomPercentageAsync(this.ZoomPercentage + this.GetStepPercentage());
        }

        /// <summary>
        /// Invokes the fit-to-view callback when enabled.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task FitToViewAsync()
        {
            if (!this.Disabled)
            {
                await this.FitToView.InvokeAsync();
            }
        }

        /// <summary>
        /// Sets the zoom percentage when the clamped value changes.
        /// </summary>
        /// <param name="zoomPercentage">The requested zoom percentage.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SetZoomPercentageAsync(int zoomPercentage)
        {
            if (this.Disabled)
            {
                return;
            }

            var nextZoomPercentage = Math.Clamp(
                zoomPercentage,
                this.GetMinimumZoomPercentage(),
                this.GetMaximumZoomPercentage());

            if (nextZoomPercentage != this.ZoomPercentage)
            {
                this.ZoomPercentage = nextZoomPercentage;

                await this.ZoomPercentageChanged.InvokeAsync(nextZoomPercentage);
            }
        }

        /// <summary>
        /// Gets the current zoom percentage clamped to the configured range.
        /// </summary>
        /// <returns>The clamped zoom percentage.</returns>
        private int GetClampedZoomPercentage()
        {
            return Math.Clamp(
                this.ZoomPercentage,
                this.GetMinimumZoomPercentage(),
                this.GetMaximumZoomPercentage());
        }

        /// <summary>
        /// Gets the normalized minimum zoom percentage.
        /// </summary>
        /// <returns>The normalized minimum zoom percentage.</returns>
        private int GetMinimumZoomPercentage()
        {
            return Math.Min(this.MinZoomPercentage, this.MaxZoomPercentage);
        }

        /// <summary>
        /// Gets the normalized maximum zoom percentage.
        /// </summary>
        /// <returns>The normalized maximum zoom percentage.</returns>
        private int GetMaximumZoomPercentage()
        {
            return Math.Max(this.MinZoomPercentage, this.MaxZoomPercentage);
        }

        /// <summary>
        /// Gets the normalized zoom step percentage.
        /// </summary>
        /// <returns>The normalized zoom step percentage.</returns>
        private int GetStepPercentage()
        {
            return Math.Max(1, this.StepPercentage);
        }
    }
}
