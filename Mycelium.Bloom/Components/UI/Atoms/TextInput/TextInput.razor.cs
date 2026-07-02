// ------------------------------------------------------------------------------------------------
// <copyright file="TextInput.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Atoms.TextInput
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Represents a reusable text input component for Bloom forms and dialogs.
    /// </summary>
    public partial class TextInput : ComponentBase
    {
        /// <summary>
        /// The generated fallback identifier of the input element.
        /// </summary>
        private readonly string generatedId = $"mb-text-input-{Guid.NewGuid():N}";

        /// <summary>
        /// Gets or sets the identifier of the input element.
        /// </summary>
        [Parameter]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the input element.
        /// </summary>
        [Parameter]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional input label.
        /// </summary>
        [Parameter]
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current input value.
        /// </summary>
        [Parameter]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the callback invoked when the input value changes.
        /// </summary>
        [Parameter]
        public EventCallback<string> ValueChanged { get; set; }

        /// <summary>
        /// Gets or sets the placeholder text displayed when the input is empty.
        /// </summary>
        [Parameter]
        public string Placeholder { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the HTML input type.
        /// </summary>
        [Parameter]
        public string Type { get; set; } = "text";

        /// <summary>
        /// Gets or sets the optional help text rendered under the input when no error is present.
        /// </summary>
        [Parameter]
        public string HelpText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional error text rendered under the input.
        /// </summary>
        [Parameter]
        public string ErrorText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the input is disabled.
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the input is required.
        /// </summary>
        [Parameter]
        public bool Required { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the input is read-only.
        /// </summary>
        [Parameter]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets the visual size of the input.
        /// </summary>
        [Parameter]
        public TextInputSize Size { get; set; } = TextInputSize.Medium;

        /// <summary>
        /// Gets or sets optional content rendered before the input field.
        /// </summary>
        [Parameter]
        public RenderFragment StartContent { get; set; }

        /// <summary>
        /// Gets or sets optional content rendered after the input field.
        /// </summary>
        [Parameter]
        public RenderFragment EndContent { get; set; }

        /// <summary>
        /// Gets or sets additional CSS classes applied to the input wrapper.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional unmatched attributes applied to the input element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the final CSS class list applied to the text input wrapper.
        /// </summary>
        /// <returns>The text input CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-text-input",
                this.GetSizeClass(),
                CssClassBuilder.When("mb-text-input--disabled", this.Disabled),
                CssClassBuilder.When("mb-text-input--readonly", this.ReadOnly),
                CssClassBuilder.When("mb-text-input--error", this.HasError()),
                this.Class);

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class matching the selected text input size.
        /// </summary>
        /// <returns>The text input size CSS class.</returns>
        private string GetSizeClass()
        {
            var cssClass = this.Size switch
            {
                TextInputSize.Small => "mb-text-input--small",
                TextInputSize.Large => "mb-text-input--large",
                _ => "mb-text-input--medium"
            };

            return cssClass;
        }

        /// <summary>
        /// Gets the effective identifier of the input element.
        /// </summary>
        /// <returns>The explicit or generated input identifier.</returns>
        private string GetInputId()
        {
            var inputId = string.IsNullOrWhiteSpace(this.Id)
                ? this.generatedId
                : this.Id;

            return inputId;
        }

        /// <summary>
        /// Gets the effective input name when provided.
        /// </summary>
        /// <returns>The input name, or null when no name is configured.</returns>
        private string GetInputName()
        {
            var inputName = string.IsNullOrWhiteSpace(this.Name)
                ? null
                : this.Name;

            return inputName;
        }

        /// <summary>
        /// Gets the help text identifier.
        /// </summary>
        /// <returns>The help text identifier.</returns>
        private string GetHelpId()
        {
            var helpId = $"{this.GetInputId()}-help";

            return helpId;
        }

        /// <summary>
        /// Gets the error text identifier.
        /// </summary>
        /// <returns>The error text identifier.</returns>
        private string GetErrorId()
        {
            var errorId = $"{this.GetInputId()}-error";

            return errorId;
        }

        /// <summary>
        /// Gets the identifier of the active input description.
        /// </summary>
        /// <returns>The error or help text identifier when description text is rendered.</returns>
        private string GetDescriptionId()
        {
            if (this.HasError())
            {
                return this.GetErrorId();
            }

            var descriptionId = this.HasHelpText()
                ? this.GetHelpId()
                : null;

            return descriptionId;
        }

        /// <summary>
        /// Gets the aria-invalid value when the input is in an error state.
        /// </summary>
        /// <returns>The aria-invalid value when an error is present; otherwise, null.</returns>
        private string GetAriaInvalid()
        {
            var ariaInvalid = this.HasError()
                ? "true"
                : null;

            return ariaInvalid;
        }

        /// <summary>
        /// Gets a value indicating whether the input has a visible label.
        /// </summary>
        /// <returns>True when a label is provided; otherwise, false.</returns>
        private bool HasLabel()
        {
            var hasLabel = !string.IsNullOrWhiteSpace(this.Label);

            return hasLabel;
        }

        /// <summary>
        /// Gets a value indicating whether help text should be rendered.
        /// </summary>
        /// <returns>True when help text is provided; otherwise, false.</returns>
        private bool HasHelpText()
        {
            var hasHelpText = !string.IsNullOrWhiteSpace(this.HelpText);

            return hasHelpText;
        }

        /// <summary>
        /// Gets a value indicating whether the input has an error.
        /// </summary>
        /// <returns>True when error text is provided; otherwise, false.</returns>
        private bool HasError()
        {
            var hasError = !string.IsNullOrWhiteSpace(this.ErrorText);

            return hasError;
        }

        /// <summary>
        /// Handles input changes and forwards the updated value to the parent component.
        /// </summary>
        /// <param name="args">The input change event arguments.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleInputAsync(ChangeEventArgs args)
        {
            var value = args.Value?.ToString() ?? string.Empty;

            this.Value = value;

            await this.ValueChanged.InvokeAsync(value);
        }
    }
}
