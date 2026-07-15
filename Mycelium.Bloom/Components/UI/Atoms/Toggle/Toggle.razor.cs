// ------------------------------------------------------------------------------------------------
// <copyright file="Toggle.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Atoms.Toggle
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Common;

    /// <summary>
    /// Represents a reusable native checkbox rendered as a compact switch.
    /// </summary>
    public sealed partial class Toggle : BloomComponentBase
    {
        /// <summary>
        /// The generated fallback identifier of the toggle input.
        /// </summary>
        private readonly string generatedId = CreateGeneratedId("mb-toggle");

        /// <summary>
        /// Gets or sets the identifier of the toggle input.
        /// </summary>
        [Parameter]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the toggle input.
        /// </summary>
        [Parameter]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the visible toggle label.
        /// </summary>
        [Parameter]
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional toggle description.
        /// </summary>
        [Parameter]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the toggle is checked.
        /// </summary>
        [Parameter]
        public bool Checked { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the checked state changes.
        /// </summary>
        [Parameter]
        public EventCallback<bool> CheckedChanged { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the toggle is disabled.
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets the final CSS class list applied to the toggle wrapper.
        /// </summary>
        /// <returns>The toggle CSS class list.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass(
                "mb-toggle",
                CssClassBuilder.When("mb-toggle--checked", this.Checked),
                CssClassBuilder.When("mb-toggle--disabled", this.Disabled));
        }

        /// <summary>
        /// Gets the effective identifier of the toggle input.
        /// </summary>
        /// <returns>The explicit or generated toggle identifier.</returns>
        private string GetToggleId()
        {
            return string.IsNullOrWhiteSpace(this.Id) ? this.generatedId : this.Id;
        }

        /// <summary>
        /// Gets the effective toggle name when configured.
        /// </summary>
        /// <returns>The configured toggle name, or null when omitted.</returns>
        private string GetToggleName()
        {
            return string.IsNullOrWhiteSpace(this.Name) ? null : this.Name;
        }

        /// <summary>
        /// Gets the identifier of the toggle description when rendered.
        /// </summary>
        /// <returns>The description identifier, or null when no description is rendered.</returns>
        private string GetDescriptionId()
        {
            return this.HasDescription() ? $"{this.GetToggleId()}-description" : null;
        }

        /// <summary>
        /// Gets the accessible checked state of the switch.
        /// </summary>
        /// <returns>True when checked; otherwise, false.</returns>
        private string GetAriaChecked()
        {
            return this.Checked ? "true" : "false";
        }

        /// <summary>
        /// Gets a value indicating whether a label is configured.
        /// </summary>
        /// <returns>True when a label is configured; otherwise, false.</returns>
        private bool HasLabel()
        {
            return !string.IsNullOrWhiteSpace(this.Label);
        }

        /// <summary>
        /// Gets a value indicating whether a description is configured.
        /// </summary>
        /// <returns>True when a description is configured; otherwise, false.</returns>
        private bool HasDescription()
        {
            return !string.IsNullOrWhiteSpace(this.Description);
        }

        /// <summary>
        /// Gets a value indicating whether label or description content is configured.
        /// </summary>
        /// <returns>True when content is configured; otherwise, false.</returns>
        private bool HasContent()
        {
            return this.HasLabel() || this.HasDescription();
        }

        /// <summary>
        /// Handles checked state changes and forwards the updated value to the parent component.
        /// </summary>
        /// <param name="args">The input change event arguments.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleCheckedChangedAsync(ChangeEventArgs args)
        {
            var isChecked = args.Value is bool value && value;

            this.Checked = isChecked;

            await this.CheckedChanged.InvokeAsync(isChecked);
        }
    }
}
