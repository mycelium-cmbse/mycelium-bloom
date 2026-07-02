// ------------------------------------------------------------------------------------------------
// <copyright file="Checkbox.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Atoms.Checkbox
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Represents a reusable checkbox component for Bloom forms and settings.
    /// </summary>
    public partial class Checkbox : ComponentBase
    {
        /// <summary>
        /// The generated fallback identifier of the checkbox input.
        /// </summary>
        private readonly string generatedId = $"mb-checkbox-{Guid.NewGuid():N}";

        /// <summary>
        /// Gets or sets the identifier of the checkbox input.
        /// </summary>
        [Parameter]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the checkbox input.
        /// </summary>
        [Parameter]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional checkbox label.
        /// </summary>
        [Parameter]
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional checkbox description.
        /// </summary>
        [Parameter]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the checkbox is checked.
        /// </summary>
        [Parameter]
        public bool Checked { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the checked state changes.
        /// </summary>
        [Parameter]
        public EventCallback<bool> CheckedChanged { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the checkbox is disabled.
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the checkbox is required.
        /// </summary>
        [Parameter]
        public bool Required { get; set; }

        /// <summary>
        /// Gets or sets the visual size of the checkbox.
        /// </summary>
        [Parameter]
        public CheckboxSize Size { get; set; } = CheckboxSize.Medium;

        /// <summary>
        /// Gets or sets additional CSS classes applied to the checkbox wrapper.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional unmatched attributes applied to the checkbox input.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the final CSS class list applied to the checkbox wrapper.
        /// </summary>
        /// <returns>The checkbox CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-checkbox",
                this.GetSizeClass(),
                CssClassBuilder.When("mb-checkbox--checked", this.Checked),
                CssClassBuilder.When("mb-checkbox--disabled", this.Disabled),
                this.Class);

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class matching the selected checkbox size.
        /// </summary>
        /// <returns>The checkbox size CSS class.</returns>
        private string GetSizeClass()
        {
            var cssClass = this.Size switch
            {
                CheckboxSize.Small => "mb-checkbox--small",
                _ => "mb-checkbox--medium"
            };

            return cssClass;
        }

        /// <summary>
        /// Gets the effective identifier of the checkbox input.
        /// </summary>
        /// <returns>The explicit or generated checkbox identifier.</returns>
        private string GetCheckboxId()
        {
            var checkboxId = string.IsNullOrWhiteSpace(this.Id)
                ? this.generatedId
                : this.Id;

            return checkboxId;
        }

        /// <summary>
        /// Gets the effective checkbox name when provided.
        /// </summary>
        /// <returns>The checkbox name, or null when no name is configured.</returns>
        private string GetCheckboxName()
        {
            var checkboxName = string.IsNullOrWhiteSpace(this.Name)
                ? null
                : this.Name;

            return checkboxName;
        }

        /// <summary>
        /// Gets the description identifier.
        /// </summary>
        /// <returns>The description identifier, or null when no description is rendered.</returns>
        private string GetDescriptionId()
        {
            var descriptionId = this.HasDescription()
                ? $"{this.GetCheckboxId()}-description"
                : null;

            return descriptionId;
        }

        /// <summary>
        /// Gets a value indicating whether the checkbox has a visible label.
        /// </summary>
        /// <returns>True when a label is provided; otherwise, false.</returns>
        private bool HasLabel()
        {
            var hasLabel = !string.IsNullOrWhiteSpace(this.Label);

            return hasLabel;
        }

        /// <summary>
        /// Gets a value indicating whether the checkbox has a visible description.
        /// </summary>
        /// <returns>True when a description is provided; otherwise, false.</returns>
        private bool HasDescription()
        {
            var hasDescription = !string.IsNullOrWhiteSpace(this.Description);

            return hasDescription;
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
