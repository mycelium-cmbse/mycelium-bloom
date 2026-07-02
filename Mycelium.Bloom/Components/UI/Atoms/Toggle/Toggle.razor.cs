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
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Represents a reusable toggle component for Bloom forms and settings.
    /// </summary>
    public partial class Toggle : ComponentBase
    {
        /// <summary>
        /// The generated fallback identifier of the toggle input.
        /// </summary>
        private readonly string generatedId = $"mb-toggle-{Guid.NewGuid():N}";

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
        /// Gets or sets the optional toggle label.
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
        /// Gets or sets the visual size of the toggle.
        /// </summary>
        [Parameter]
        public ToggleSize Size { get; set; } = ToggleSize.Medium;

        /// <summary>
        /// Gets or sets the text rendered when the toggle is checked.
        /// </summary>
        [Parameter]
        public string OnText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the text rendered when the toggle is unchecked.
        /// </summary>
        [Parameter]
        public string OffText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional CSS classes applied to the toggle wrapper.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional unmatched attributes applied to the toggle input.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the final CSS class list applied to the toggle wrapper.
        /// </summary>
        /// <returns>The toggle CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-toggle",
                this.GetSizeClass(),
                CssClassBuilder.When("mb-toggle--checked", this.Checked),
                CssClassBuilder.When("mb-toggle--disabled", this.Disabled),
                this.Class);

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class matching the selected toggle size.
        /// </summary>
        /// <returns>The toggle size CSS class.</returns>
        private string GetSizeClass()
        {
            var cssClass = this.Size switch
            {
                ToggleSize.Small => "mb-toggle--small",
                _ => "mb-toggle--medium"
            };

            return cssClass;
        }

        /// <summary>
        /// Gets the effective identifier of the toggle input.
        /// </summary>
        /// <returns>The explicit or generated toggle identifier.</returns>
        private string GetToggleId()
        {
            var toggleId = string.IsNullOrWhiteSpace(this.Id)
                ? this.generatedId
                : this.Id;

            return toggleId;
        }

        /// <summary>
        /// Gets the effective toggle name when provided.
        /// </summary>
        /// <returns>The toggle name, or null when no name is configured.</returns>
        private string GetToggleName()
        {
            var toggleName = string.IsNullOrWhiteSpace(this.Name)
                ? null
                : this.Name;

            return toggleName;
        }

        /// <summary>
        /// Gets the description identifier.
        /// </summary>
        /// <returns>The description identifier, or null when no description is rendered.</returns>
        private string GetDescriptionId()
        {
            var descriptionId = this.HasDescription()
                ? $"{this.GetToggleId()}-description"
                : null;

            return descriptionId;
        }

        /// <summary>
        /// Gets the aria-checked value matching the current toggle state.
        /// </summary>
        /// <returns>The aria-checked value.</returns>
        private string GetAriaChecked()
        {
            var ariaChecked = this.Checked
                ? "true"
                : "false";

            return ariaChecked;
        }

        /// <summary>
        /// Gets the state text matching the current toggle state.
        /// </summary>
        /// <returns>The active state text.</returns>
        private string GetStateText()
        {
            var stateText = this.Checked
                ? this.OnText
                : this.OffText;

            return stateText;
        }

        /// <summary>
        /// Gets a value indicating whether the toggle has a visible label.
        /// </summary>
        /// <returns>True when a label is provided; otherwise, false.</returns>
        private bool HasLabel()
        {
            var hasLabel = !string.IsNullOrWhiteSpace(this.Label);

            return hasLabel;
        }

        /// <summary>
        /// Gets a value indicating whether the toggle has a visible description.
        /// </summary>
        /// <returns>True when a description is provided; otherwise, false.</returns>
        private bool HasDescription()
        {
            var hasDescription = !string.IsNullOrWhiteSpace(this.Description);

            return hasDescription;
        }

        /// <summary>
        /// Gets a value indicating whether state text should be rendered.
        /// </summary>
        /// <returns>True when active state text is provided; otherwise, false.</returns>
        private bool HasStateText()
        {
            var hasStateText = !string.IsNullOrWhiteSpace(this.GetStateText());

            return hasStateText;
        }

        /// <summary>
        /// Gets a value indicating whether label or description content should be rendered.
        /// </summary>
        /// <returns>True when label or description content is provided; otherwise, false.</returns>
        private bool HasContent()
        {
            var hasContent = this.HasLabel() || this.HasDescription();

            return hasContent;
        }

        /// <summary>
        /// Handles checked state changes and forwards the updated value to the parent component.
        /// </summary>
        /// <param name="args">The input change event arguments.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleChangedAsync(ChangeEventArgs args)
        {
            var isChecked = args.Value is bool value && value;

            this.Checked = isChecked;

            await this.CheckedChanged.InvokeAsync(isChecked);
        }
    }
}
