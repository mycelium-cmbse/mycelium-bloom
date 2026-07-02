// ------------------------------------------------------------------------------------------------
// <copyright file="SelectInput.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Atoms.SelectInput
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Represents a reusable select input component for Bloom forms and dialogs.
    /// </summary>
    public partial class SelectInput : ComponentBase
    {
        /// <summary>
        /// The generated fallback identifier of the select input.
        /// </summary>
        private readonly string generatedId = $"mb-select-input-{Guid.NewGuid():N}";

        /// <summary>
        /// Gets or sets a value indicating whether the custom dropdown is open.
        /// </summary>
        private bool IsOpen { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the select input button.
        /// </summary>
        [Parameter]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the select element.
        /// </summary>
        [Parameter]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional select label.
        /// </summary>
        [Parameter]
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current selected value.
        /// </summary>
        [Parameter]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the callback invoked when the selected value changes.
        /// </summary>
        [Parameter]
        public EventCallback<string> ValueChanged { get; set; }

        /// <summary>
        /// Gets or sets the options rendered by the select input.
        /// </summary>
        [Parameter]
        public IReadOnlyList<SelectInputOption> Options { get; set; } = [];

        /// <summary>
        /// Gets or sets the optional placeholder text rendered as the first disabled option.
        /// </summary>
        [Parameter]
        public string Placeholder { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional help text rendered under the select when no error is present.
        /// </summary>
        [Parameter]
        public string HelpText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional error text rendered under the select.
        /// </summary>
        [Parameter]
        public string ErrorText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the select is disabled.
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the select is required.
        /// </summary>
        [Parameter]
        public bool Required { get; set; }

        /// <summary>
        /// Gets or sets the visual size of the select.
        /// </summary>
        [Parameter]
        public SelectInputSize Size { get; set; } = SelectInputSize.Medium;

        /// <summary>
        /// Gets or sets optional content rendered before the select field.
        /// </summary>
        [Parameter]
        public RenderFragment StartContent { get; set; }

        /// <summary>
        /// Gets or sets additional CSS classes applied to the select wrapper.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional unmatched attributes applied to the select input button.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the final CSS class list applied to the select input wrapper.
        /// </summary>
        /// <returns>The select input CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-select-input",
                this.GetSizeClass(),
                CssClassBuilder.When("mb-select-input--disabled", this.Disabled),
                CssClassBuilder.When("mb-select-input--error", this.HasError()),
                CssClassBuilder.When("mb-select-input--open", this.IsOpen),
                this.Class);

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class matching the selected select input size.
        /// </summary>
        /// <returns>The select input size CSS class.</returns>
        private string GetSizeClass()
        {
            var cssClass = this.Size switch
            {
                SelectInputSize.Small => "mb-select-input--small",
                SelectInputSize.Large => "mb-select-input--large",
                _ => "mb-select-input--medium"
            };

            return cssClass;
        }

        /// <summary>
        /// Gets the effective identifier of the select input button.
        /// </summary>
        /// <returns>The explicit or generated select input button identifier.</returns>
        private string GetButtonId()
        {
            var buttonId = string.IsNullOrWhiteSpace(this.Id)
                ? this.generatedId
                : this.Id;

            return buttonId;
        }

        /// <summary>
        /// Gets the listbox identifier.
        /// </summary>
        /// <returns>The listbox identifier.</returns>
        private string GetListboxId()
        {
            var listboxId = $"{this.GetButtonId()}-listbox";

            return listboxId;
        }

        /// <summary>
        /// Gets the help text identifier.
        /// </summary>
        /// <returns>The help text identifier.</returns>
        private string GetHelpId()
        {
            var helpId = $"{this.GetButtonId()}-help";

            return helpId;
        }

        /// <summary>
        /// Gets the error text identifier.
        /// </summary>
        /// <returns>The error text identifier.</returns>
        private string GetErrorId()
        {
            var errorId = $"{this.GetButtonId()}-error";

            return errorId;
        }

        /// <summary>
        /// Gets the identifier of the active select description.
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
        /// Gets the aria-invalid value when the select is in an error state.
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
        /// Gets the aria-expanded value matching the current dropdown state.
        /// </summary>
        /// <returns>The aria-expanded value.</returns>
        private string GetAriaExpanded()
        {
            var ariaExpanded = this.IsOpen
                ? "true"
                : "false";

            return ariaExpanded;
        }

        /// <summary>
        /// Gets the aria-required value matching the required state.
        /// </summary>
        /// <returns>The aria-required value.</returns>
        private string GetAriaRequired()
        {
            var ariaRequired = this.Required
                ? "true"
                : "false";

            return ariaRequired;
        }

        /// <summary>
        /// Gets a value indicating whether the select has a visible label.
        /// </summary>
        /// <returns>True when a label is provided; otherwise, false.</returns>
        private bool HasLabel()
        {
            var hasLabel = !string.IsNullOrWhiteSpace(this.Label);

            return hasLabel;
        }

        /// <summary>
        /// Gets a value indicating whether the select has a form name.
        /// </summary>
        /// <returns>True when a name is provided; otherwise, false.</returns>
        private bool HasName()
        {
            var hasName = !string.IsNullOrWhiteSpace(this.Name);

            return hasName;
        }

        /// <summary>
        /// Gets a value indicating whether placeholder text is configured.
        /// </summary>
        /// <returns>True when placeholder text is provided; otherwise, false.</returns>
        private bool HasPlaceholder()
        {
            var hasPlaceholder = !string.IsNullOrWhiteSpace(this.Placeholder);

            return hasPlaceholder;
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
        /// Gets a value indicating whether the select has an error.
        /// </summary>
        /// <returns>True when error text is provided; otherwise, false.</returns>
        private bool HasError()
        {
            var hasError = !string.IsNullOrWhiteSpace(this.ErrorText);

            return hasError;
        }

        /// <summary>
        /// Gets a value indicating whether the current value matches a select option.
        /// </summary>
        /// <returns>True when the current value matches an option; otherwise, false.</returns>
        private bool HasSelectedOption()
        {
            var hasSelectedOption = this.GetSelectedOption() is not null;

            return hasSelectedOption;
        }

        /// <summary>
        /// Gets a value indicating whether the placeholder should be displayed in the control.
        /// </summary>
        /// <returns>True when value is empty and placeholder text is provided; otherwise, false.</returns>
        private bool ShouldShowPlaceholder()
        {
            var shouldShowPlaceholder = string.IsNullOrWhiteSpace(this.Value) && this.HasPlaceholder();

            return shouldShowPlaceholder;
        }

        /// <summary>
        /// Gets the selected option label.
        /// </summary>
        /// <returns>The selected option label, or an empty string when no option is selected.</returns>
        private string GetSelectedOptionLabel()
        {
            var selectedOption = this.GetSelectedOption();
            var selectedOptionLabel = selectedOption?.Label ?? string.Empty;

            return selectedOptionLabel;
        }

        /// <summary>
        /// Gets the placeholder text displayed when no option is selected.
        /// </summary>
        /// <returns>The configured placeholder text, or an empty fallback.</returns>
        private string GetPlaceholderText()
        {
            var placeholderText = this.HasPlaceholder()
                ? this.Placeholder
                : string.Empty;

            return placeholderText;
        }

        /// <summary>
        /// Gets the selected option.
        /// </summary>
        /// <returns>The selected option, or null when no option matches the current value.</returns>
        private SelectInputOption GetSelectedOption()
        {
            if (string.IsNullOrWhiteSpace(this.Value))
            {
                return null;
            }

            var selectedOption = this.Options.FirstOrDefault(option => option.Value == this.Value);

            return selectedOption;
        }

        /// <summary>
        /// Gets a value indicating whether the provided option is selected.
        /// </summary>
        /// <param name="option">The option to evaluate.</param>
        /// <returns>True when the option is selected; otherwise, false.</returns>
        private bool IsSelectedOption(SelectInputOption option)
        {
            var isSelectedOption = option.Value == this.Value;

            return isSelectedOption;
        }

        /// <summary>
        /// Gets the aria-selected value for the provided option.
        /// </summary>
        /// <param name="option">The option to evaluate.</param>
        /// <returns>The aria-selected value.</returns>
        private string GetAriaSelected(SelectInputOption option)
        {
            var ariaSelected = this.IsSelectedOption(option)
                ? "true"
                : "false";

            return ariaSelected;
        }

        /// <summary>
        /// Gets the final CSS class list applied to an option.
        /// </summary>
        /// <param name="option">The option to evaluate.</param>
        /// <returns>The option CSS class list.</returns>
        private string GetOptionCssClass(SelectInputOption option)
        {
            var cssClass = CssClassBuilder.Build(
                "mb-select-input__option",
                CssClassBuilder.When("mb-select-input__option--selected", this.IsSelectedOption(option)),
                CssClassBuilder.When("mb-select-input__option--disabled", option.Disabled));

            return cssClass;
        }

        /// <summary>
        /// Opens or closes the dropdown menu.
        /// </summary>
        private void ToggleOpen()
        {
            if (this.Disabled)
            {
                return;
            }

            this.IsOpen = !this.IsOpen;
        }

        /// <summary>
        /// Selects an option and forwards the updated value to the parent component.
        /// </summary>
        /// <param name="option">The selected option.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SelectOptionAsync(SelectInputOption option)
        {
            if (option.Disabled)
            {
                return;
            }

            this.Value = option.Value;
            this.IsOpen = false;

            await this.ValueChanged.InvokeAsync(option.Value);
        }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (this.Disabled)
            {
                this.IsOpen = false;
            }
        }
    }
}
